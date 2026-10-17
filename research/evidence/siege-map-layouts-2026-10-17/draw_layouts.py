#!/usr/bin/env python3
"""Draws the siege map candidates of 17 October 2026 from above, at the scale of the game's
units, and measures each one the same way, beside the map the game has today.

Usage (from the repository root, with Pillow):

    .artifacts/tools-venv/bin/python research/evidence/siege-map-layouts-2026-10-17/draw_layouts.py \
        site/quantum-db.json research/evidence/siege-map-layouts-2026-10-17

The current map is read from the exported simulation assets (Map1: its static colliders, spawn
points, objective points and escape points); the candidates are the grids written below. One cell
is one unit of the simulation, a cell is centred on whole coordinates, +y is up on the pictures
(team 0, blue, at the bottom; team 1, red, at the top). A candidate is written for team 0's half
only (row 0 then the rows below it); the other half is the same turned by 180 degrees around the
centre, as the current map is, so both teams see the same picture from their own camera (the
camera of team 1 is turned around).

What is measured (numbers of the game: tower range 5, tower radius 0.6, hero radius 0.5, the
longest hero attack 9, hero speed 2 to 2.75 units a second, camera of GameEssentials.prefab with
LocalPlayerCameraFollow: 25 up, 7.5 behind, pitch 75, vertical field of view 35, x locked at 0):
see the README beside this file.
"""
import heapq
import json
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ONE = 65536.0
HERE = Path(__file__).resolve().parent
FONTS = HERE.parents[2] / "game" / "Assets" / "Chog" / "Resources" / "Fonts"

# The game's palette ("Chog night", ArenaPalettes.cs; team colours of make_chog_structures.py).
INK = (20, 15, 36)            # #140F24
FLOOR = (38, 32, 82)          # WoodDark 262052
GRID = (50, 43, 102)
GRID_FIVE = (68, 60, 128)
WALL = (44, 35, 88)           # Stone 2C2358
WALL_EDGE = (22, 17, 46)      # StoneDark 16112E
BLOCK = (63, 49, 146)         # BrownDark 3F3192 (the planks of a crate)
BLOCK_EDGE = (161, 148, 240)  # Metal A194F0 (its frame)
BUSH = (43, 196, 143)         # Green 2BC48F
BUSH_EDGE = (24, 120, 88)
BLUE = (64, 143, 255)
RED = (255, 77, 77)
GOLD = (245, 158, 11)
TEXT = (233, 228, 255)
MUTED = (170, 160, 220)

# Numbers of the simulation (Structures.cs, KCC, HealthSystem) and of the camera.
TOWER_RANGE = 5.0
TOWER_RADIUS = 0.6
HERO_RADIUS = 0.5
CORE_RADIUS_NOW = 0.75
CORE_RADIUS_NEW = 1.5          # proposed: the model drawn 1.5 times larger is 1.6 at the ground
LONGEST_ATTACK = 9.0
SPEED = 2.75                   # Knight and Archer prototypes; the Spellcaster has 2.0
CAMERA_UP, CAMERA_BACK, PITCH, FOV = 25.0, 7.5, 75.0, 35.0
CAMERA_MIN_Z, CAMERA_MAX_Z = -18.0, 3.0   # team 0; team 1 has the same, turned (InvertedMinZ -5, InvertedMaxZ 19)
ASPECT = 16.0 / 9.0

SCALE = 22                     # pixels per unit
PANEL = 520                    # width of the text panel

# ---------------------------------------------------------------------------------------------
# The candidates. Rows of team 0's half, from row 0 (the centre) down to the back wall; one
# character per cell from the left wall to the right one: "." floor, "#" block (blocks heroes and
# straight shots, Static layer), "b" bush (hides, blocks nothing). The top half is this turned by
# 180 degrees. Structures, spawns, objective and escape points are given for team 0 and turned
# for team 1 the same way.
# ---------------------------------------------------------------------------------------------
LAYOUTS = {}


def layout(key, **values):
    LAYOUTS[key] = values


def grid(half_w, half_h, items):
    """Rows of team 0's half (row 0 first) from rectangles (x0, x1, y0, y1, character), y <= 0,
    later rectangles over earlier ones."""
    cells = {}
    for x0, x1, y0, y1, character in items:
        for x in range(min(x0, x1), max(x0, x1) + 1):
            for y in range(min(y0, y1), max(y0, y1) + 1):
                cells[(x, y)] = character
    rows = []
    for y in range(0, -half_h - 1, -1):
        rows.append("".join(cells.get((x, y), ".") for x in range(-half_w, half_w + 1)))
    return rows


def mirrored(items):
    """The rectangles and their mirror across x = 0, for a half that is the same on both sides."""
    out = list(items)
    for x0, x1, y0, y1, character in items:
        out.append((-x1, -x0, y0, y1, character))
    return out


def fp(value):
    return value.get("RawValue", 0) / ONE if isinstance(value, dict) else 0.0


def vec(value):
    return (fp(value.get("X", {})), fp(value.get("Y", {}))) if isinstance(value, dict) else (0.0, 0.0)


def current_map(database):
    """The map of the game today, as cells, from the exported assets."""
    assets = json.loads(Path(database).read_text(encoding="utf-8-sig"))
    game_map = next(a for a in assets if a.get("$type", "").startswith("Quantum.Map,") and a.get("StaticColliders2D"))
    blocks, bushes = set(), set()
    for collider in game_map["StaticColliders2D"]:
        name = collider["StaticData"]["Name"]
        x, y = vec(collider["Position"])
        if name == "Collider":
            blocks.add((round(x), round(y)))
        elif name == "InvisibleProp":
            bushes.add((round(x), round(y)))
    spawns, objectives, escapes, position = {0: [], 1: []}, [], {0: [], 1: []}, None
    for reference in game_map.get("references", {}).get("RefIds", []):
        kind, data = reference["type"]["class"], reference["data"]
        if kind == "Transform2DPrototype":
            position = vec(data["Position"])
        elif kind == "SpawnPointPrototype":
            spawns[data["Team"]].append(position)
        elif kind == "ObjectivePointPrototype":
            objectives.append(position)
        elif kind == "EscapeRoutePrototype":
            escapes[data["TeamId"]].append(position)
    return {
        "blocks": blocks, "bushes": bushes, "half_w": 10, "half_h": 16,
        "cores": {0: (0.0, -12.5), 1: (0.0, 12.5)},
        "towers": {0: (0.5, -8.5), 1: (-0.5, 8.5)},
        "spawns": spawns, "objectives": objectives, "escapes": escapes,
    }


def build(spec):
    """Cells, structures and points of a candidate, both halves."""
    rows = spec["rows"]
    width = len(rows[0])
    half_w = width // 2
    half_h = len(rows) - 1
    assert all(len(row) == width for row in rows), "rows of different widths"
    assert rows[0] == rows[0][::-1], "row 0 must be the same turned around"
    blocks, bushes = set(), set()
    for k, row in enumerate(rows):
        y = -k
        for i, cell in enumerate(row):
            x = i - half_w
            for cx, cy in ((x, y), (-x, -y)):
                if cell == "#":
                    blocks.add((cx, cy))
                elif cell == "b":
                    bushes.add((cx, cy))
    turn = lambda p: (-p[0], -p[1])
    return {
        "blocks": blocks, "bushes": bushes, "half_w": half_w, "half_h": half_h,
        "cores": {0: spec["core"], 1: turn(spec["core"])},
        "towers": {0: spec["tower"], 1: turn(spec["tower"])},
        "spawns": {0: list(spec["spawns"]), 1: [turn(p) for p in spec["spawns"]]},
        "objectives": list(spec.get("objectives", [])) + [turn(p) for p in spec.get("objectives", []) if p != (0, 0)],
        "escapes": {0: list(spec["escapes"]), 1: [turn(p) for p in spec["escapes"]]},
        "lanes": spec.get("lanes", []),
    }


# ---------------------------------------------------------------------------------------------
# Geometry of what is seen and what is free.
# ---------------------------------------------------------------------------------------------
def camera_ground(player_y, team=0, aspect=ASPECT):
    """The four corners of the floor the match camera shows (16:9), the player at x 0."""
    pitch = math.radians(PITCH)
    ty = math.tan(math.radians(FOV / 2))
    tx = ty * aspect
    if team == 0:
        z = min(max(player_y - CAMERA_BACK, CAMERA_MIN_Z), CAMERA_MAX_Z)
        forward, up, right = (0.0, -math.sin(pitch), math.cos(pitch)), (0.0, math.cos(pitch), math.sin(pitch)), (1.0, 0.0, 0.0)
    else:
        z = min(max(player_y + CAMERA_BACK, -CAMERA_MAX_Z), -CAMERA_MIN_Z)
        forward, up, right = (0.0, -math.sin(pitch), -math.cos(pitch)), (0.0, math.cos(pitch), -math.sin(pitch)), (-1.0, 0.0, 0.0)
    corners = []
    for u, v in ((-1, 1), (1, 1), (1, -1), (-1, -1)):
        d = [forward[i] + u * tx * right[i] + v * ty * up[i] for i in range(3)]
        t = CAMERA_UP / -d[1]
        corners.append((t * d[0], z + t * d[2]))
    return corners


def inside(m, cell):
    return abs(cell[0]) <= m["half_w"] and abs(cell[1]) <= m["half_h"]


def solid_cells(m, with_structures=True):
    """Cells a hero cannot stand on: blocks, and those under a standing structure."""
    solid = set(m["blocks"])
    if with_structures:
        for centre, radius in structure_discs(m):
            for x in range(-m["half_w"], m["half_w"] + 1):
                for y in range(-m["half_h"], m["half_h"] + 1):
                    if math.hypot(x - centre[0], y - centre[1]) < radius + HERO_RADIUS - 0.05:
                        solid.add((x, y))
    return solid


def structure_discs(m, core_radius=CORE_RADIUS_NEW):
    discs = []
    for team in (0, 1):
        discs.append((m["cores"][team], core_radius))
        discs.append((m["towers"][team], TOWER_RADIUS))
    return discs


def segment_blocked(m, a, b, discs=()):
    """Whether a straight shot from a to b meets a block (or one of the given discs)."""
    length = math.hypot(b[0] - a[0], b[1] - a[1])
    steps = max(2, int(length * 12))
    for s in range(1, steps):
        t = s / steps
        x, y = a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t
        if (round(x), round(y)) in m["blocks"] and abs(x - round(x)) <= 0.5 and abs(y - round(y)) <= 0.5:
            return True
        for centre, radius in discs:
            if math.hypot(x - centre[0], y - centre[1]) < radius:
                return True
    return False


def walk(m, start, solid):
    """Shortest walking distances from a cell over free cells (8 ways, no corner cutting)."""
    best = {start: 0.0}
    queue = [(0.0, start)]
    while queue:
        cost, (x, y) = heapq.heappop(queue)
        if cost > best.get((x, y), 1e9):
            continue
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            nxt = (x + dx, y + dy)
            if not inside(m, nxt) or nxt in solid:
                continue
            if dx and dy and ((x + dx, y) in solid or (x, y + dy) in solid):
                continue
            step = cost + (1.4142 if dx and dy else 1.0)
            if step < best.get(nxt, 1e9):
                best[nxt] = step
                heapq.heappush(queue, (step, nxt))
    return best


def measure(m):
    """The numbers compared in the README, for team 0 attacking team 1 (the map is the same turned)."""
    solid = solid_cells(m)
    free = [(x, y) for x in range(-m["half_w"], m["half_w"] + 1) for y in range(-m["half_h"], m["half_h"] + 1)
            if (x, y) not in solid]
    tower, core = m["towers"][1], m["cores"][1]
    discs_tower = [(tower, TOWER_RADIUS)]
    poke = [c for c in free if TOWER_RANGE < math.hypot(c[0] - tower[0], c[1] - tower[1]) <= 8.0
            and not segment_blocked(m, c, tower)]
    core_chip = [c for c in free if math.hypot(c[0] - tower[0], c[1] - tower[1]) > TOWER_RANGE
                 and math.hypot(c[0] - core[0], c[1] - core[1]) - CORE_RADIUS_NEW <= LONGEST_ATTACK
                 and not segment_blocked(m, c, core, discs_tower)]
    # What can be reached from the centre without ever standing within the red tower's range
    # (the tower hurts a hero whose centre is within 5 of its own, line of sight or not).
    in_range = {c for c in free if math.hypot(c[0] - tower[0], c[1] - tower[1]) <= TOWER_RANGE}
    centre = min(free, key=lambda c: math.hypot(c[0], c[1]))
    safe = walk(m, centre, solid | in_range)
    core_chip_safe = [c for c in core_chip if c in safe]
    poke_safe = [c for c in poke if c in safe]
    spawn = min(m["spawns"][0], key=lambda p: abs(p[0]))
    start = (round(spawn[0]), round(spawn[1]))
    if start in solid:
        start = min(free, key=lambda c: math.hypot(c[0] - spawn[0], c[1] - spawn[1]))
    distance = walk(m, start, solid)

    def reach(target, radius):
        cells = [c for c in distance if math.hypot(c[0] - target[0], c[1] - target[1]) <= radius]
        return min((distance[c] for c in cells), default=float("nan"))

    # Passages one cell wide between blocks or a block and the border (structures aside).
    narrow = []
    walls = set(m["blocks"])
    shut = lambda c: c in walls or not inside(m, c)
    for x in range(-m["half_w"], m["half_w"] + 1):
        for y in range(-m["half_h"], m["half_h"] + 1):
            if (x, y) in walls:
                continue
            if (shut((x - 1, y)) and shut((x + 1, y))) or (shut((x, y - 1)) and shut((x, y + 1))):
                narrow.append((x, y))
    own_half = lambda c: c[1] < 0
    return {
        "size": f"{2 * m['half_w'] + 1} x {2 * m['half_h'] + 1}",
        "blocks": len(m["blocks"]),
        "bushes": len(m["bushes"]),
        "bushes_centre": sum(1 for c in m["bushes"] if abs(c[1]) <= 3),
        "bushes_near_own_tower": sum(1 for c in m["bushes"] if math.hypot(c[0] - m["towers"][0][0], c[1] - m["towers"][0][1]) <= TOWER_RANGE + 2),
        "blocks_near_own_tower": sum(1 for c in m["blocks"] if math.hypot(c[0] - m["towers"][0][0], c[1] - m["towers"][0][1]) <= TOWER_RANGE + 2),
        "poke_cells": len(poke),
        "poke_cells_safe": len(poke_safe),
        "core_chip_cells": len(core_chip),
        "core_chip_cells_safe": len(core_chip_safe),
        "walk_centre": reach((0, 0), 1.5),
        "walk_enemy_tower": reach(tower, TOWER_RADIUS + HERO_RADIUS + 1.0),
        "walk_enemy_core": reach(core, CORE_RADIUS_NEW + HERO_RADIUS + 1.0),
        "towers_see_each_other": not segment_blocked(m, m["towers"][0], m["towers"][1]),
        "narrow_cells": len(narrow), "narrow": narrow,
        "spawn_inside_core": any(math.hypot(p[0] - m["cores"][0][0], p[1] - m["cores"][0][1]) < CORE_RADIUS_NEW + HERO_RADIUS
                                 for p in m["spawns"][0]),
        "poke": poke, "core_chip": core_chip, "core_chip_safe": core_chip_safe, "poke_safe": poke_safe,
    }


# ---------------------------------------------------------------------------------------------
# Drawing.
# ---------------------------------------------------------------------------------------------
def font(name, size):
    try:
        return ImageFont.truetype(str(FONTS / name), size)
    except OSError:
        return ImageFont.load_default()


TITLE = font("LilitaOne-Regular.ttf", 30)
HEAD = font("LilitaOne-Regular.ttf", 20)
BODY = font("Nunito-ExtraBold.ttf", 15)
SMALL = font("Nunito-ExtraBold.ttf", 12)


def dashed_polygon(draw, points, fill, width=2, dash=8):
    for a, b in zip(points, points[1:] + points[:1]):
        length = math.hypot(b[0] - a[0], b[1] - a[1])
        n = max(1, int(length / dash))
        for i in range(0, n, 2):
            t0, t1 = i / n, min(1.0, (i + 1) / n)
            draw.line([(a[0] + (b[0] - a[0]) * t0, a[1] + (b[1] - a[1]) * t0),
                       (a[0] + (b[0] - a[0]) * t1, a[1] + (b[1] - a[1]) * t1)], fill=fill, width=width)


def dashed_circle(draw, centre, radius, fill, width=2, segments=48):
    points = [(centre[0] + radius * math.cos(2 * math.pi * i / segments), centre[1] + radius * math.sin(2 * math.pi * i / segments))
              for i in range(segments)]
    for i in range(0, segments, 2):
        draw.line([points[i], points[(i + 1) % segments]], fill=fill, width=width)


def wrap(text, width_px, typeface, draw):
    words, lines, line = text.split(), [], ""
    for word in words:
        trial = (line + " " + word).strip()
        if draw.textlength(trial, font=typeface) <= width_px:
            line = trial
        else:
            lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines


def render(m, numbers, title, subtitle, notes, out, show_poke=True, camera_rows=(-15.0, 0.0)):
    margin = 2.0
    # Wide enough for the floor the camera shows on each side of the arena.
    frame = max(abs(p[0]) for row in camera_rows for p in camera_ground(row, 0)) + 0.6
    side = max(m["half_w"] + 0.5 + margin, frame)
    left, right = -side, side
    bottom, top = -m["half_h"] - 0.5 - margin, m["half_h"] + 0.5 + margin
    map_w, map_h = int((right - left) * SCALE), int((top - bottom) * SCALE)
    header = 64
    image = Image.new("RGB", (map_w + PANEL, map_h + header), INK)
    # An RGB picture drawn in RGBA mode: what has an alpha is blended over what is under it.
    base = Image.new("RGB", (map_w, map_h), INK)
    draw = ImageDraw.Draw(base, "RGBA")

    def at(x, y):
        return ((x - left) * SCALE, (top - y) * SCALE)

    def cell_box(c, shrink=0.0):
        x0, y0 = at(c[0] - 0.5 + shrink, c[1] + 0.5 - shrink)
        x1, y1 = at(c[0] + 0.5 - shrink, c[1] - 0.5 + shrink)
        return [x0, y0, x1, y1]

    # Camera views first, under everything: the floor shown at the spawn and at the centre.
    # Floor and grid.
    hw, hh = m["half_w"] + 0.5, m["half_h"] + 0.5
    draw.rectangle([*at(-hw, hh), *at(hw, -hh)], fill=FLOOR)
    for x in range(-m["half_w"], m["half_w"] + 2):
        draw.line([at(x - 0.5, -hh), at(x - 0.5, hh)], fill=GRID, width=1)
    for y in range(-m["half_h"], m["half_h"] + 2):
        draw.line([at(-hw, y - 0.5), at(hw, y - 0.5)], fill=GRID, width=1)
    # The two halves tinted by team.
    draw.rectangle([*at(-hw, 0), *at(hw, -hh)], fill=BLUE + (14,))
    draw.rectangle([*at(-hw, hh), *at(hw, 0)], fill=RED + (14,))
    draw.line([at(-hw, 0), at(hw, 0)], fill=GRID_FIVE, width=1)
    # Border walls (0.75 thick, as the arena's QuadStaticColliders).
    t = 0.75
    for box in ([-hw - t, hh + t, hw + t, hh], [-hw - t, -hh, hw + t, -hh - t], [-hw - t, hh, -hw, -hh], [hw, hh, hw + t, -hh]):
        draw.rectangle([*at(box[0], box[1]), *at(box[2], box[3])], fill=WALL, outline=WALL_EDGE, width=2)

    # Tower ranges.
    for team, colour in ((0, BLUE), (1, RED)):
        cx, cy = at(*m["towers"][team])
        r = TOWER_RANGE * SCALE
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=colour + (34,))
        dashed_circle(draw, (cx, cy), r, colour + (220,), width=2)

    # Poke cells (free ground outside the red tower's range with a straight shot at it within 8).
    if show_poke:
        for c in numbers["poke"]:
            draw.rectangle(cell_box(c, 0.32), fill=GOLD + (200,))
        for c in numbers["core_chip"]:
            if c in numbers["core_chip_safe"]:
                draw.rectangle(cell_box(c, 0.30), fill=(255, 255, 255, 235))
            else:
                draw.rectangle(cell_box(c, 0.32), outline=(255, 255, 255, 150), width=1)

    # Bushes and blocks.
    for c in m["bushes"]:
        draw.rounded_rectangle(cell_box(c, 0.02), radius=6, fill=BUSH + (235,), outline=BUSH_EDGE, width=1)
    for c in m["blocks"]:
        draw.rectangle(cell_box(c, 0.0), fill=BLOCK, outline=BLOCK_EDGE, width=2)

    # Lanes: arrows for team 0's ways to the red base.
    for lane in m.get("lanes", []):
        points = [at(*p) for p in lane]
        draw.line(points, fill=GOLD + (150,), width=5, joint="curve")
        (x0, y0), (x1, y1) = points[-2], points[-1]
        angle = math.atan2(y1 - y0, x1 - x0)
        head = 14
        draw.polygon([(x1, y1), (x1 - head * math.cos(angle - 0.45), y1 - head * math.sin(angle - 0.45)),
                      (x1 - head * math.cos(angle + 0.45), y1 - head * math.sin(angle + 0.45))], fill=GOLD + (190,))

    # Structures: towers (collider 0.6, model 0.9) and cores (collider today 0.75 dashed, proposed 1.5).
    for team, colour in ((0, BLUE), (1, RED)):
        cx, cy = at(*m["towers"][team])
        r_model, r = 0.9 * SCALE, TOWER_RADIUS * SCALE
        draw.ellipse([cx - r_model, cy - r_model, cx + r_model, cy + r_model], fill=colour + (90,))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=colour + (255,), outline=(255, 255, 255, 255), width=2)
        draw.text((cx, cy), "T", font=SMALL, fill=INK, anchor="mm")
        cx, cy = at(*m["cores"][team])
        r_new, r_now = CORE_RADIUS_NEW * SCALE, CORE_RADIUS_NOW * SCALE
        draw.ellipse([cx - r_new, cy - r_new, cx + r_new, cy + r_new], fill=colour + (200,), outline=(255, 255, 255, 255), width=2)
        dashed_circle(draw, (cx, cy), r_now, INK + (255,), width=2, segments=20)
        draw.text((cx, cy), "CORE", font=SMALL, fill=INK, anchor="mm")

    # Spawn, objective and escape points.
    for team, colour in ((0, BLUE), (1, RED)):
        for p in m["spawns"][team]:
            cx, cy = at(*p)
            draw.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], outline=colour + (255,), width=3)
            draw.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=colour + (255,))
        for p in m["escapes"].get(team, []):
            cx, cy = at(*p)
            draw.polygon([(cx, cy - 6), (cx + 6, cy), (cx, cy + 6), (cx - 6, cy)], outline=colour + (200,))
    for p in m["objectives"]:
        cx, cy = at(*p)
        draw.line([cx - 6, cy, cx + 6, cy], fill=GOLD + (230,), width=2)
        draw.line([cx, cy - 6, cx, cy + 6], fill=GOLD + (230,), width=2)

    # Camera views of a blue player standing at x 0: at its spawn row and at the centre.
    for row, colour in zip(camera_rows, ((255, 255, 255, 230), (255, 255, 255, 120))):
        corners = [at(*p) for p in camera_ground(row, 0)]
        dashed_polygon(draw, corners, colour, width=2, dash=10)
        label_x, label_y = corners[0]
        draw.text((label_x + 8, label_y + 4), f"seen by a blue player at (0, {row:g})", font=SMALL, fill=colour)

    # Coordinates every five units.
    for x in range(-m["half_w"], m["half_w"] + 1):
        if x % 5 == 0:
            draw.text(at(x, -hh - 1.2), str(x), font=SMALL, fill=MUTED, anchor="mm")
    for y in range(-m["half_h"], m["half_h"] + 1):
        if y % 5 == 0:
            draw.text(at(-hw - 1.3, y), str(y), font=SMALL, fill=MUTED, anchor="mm")

    image.paste(base, (0, header))
    canvas = ImageDraw.Draw(image)
    canvas.text((16, 14), title, font=TITLE, fill=TEXT)

    # Panel: subtitle, numbers, notes, legend.
    x0, y = map_w + 12, header + 6
    width_px = PANEL - 28
    for line in wrap(subtitle, width_px, HEAD, canvas):
        canvas.text((x0, y), line, font=HEAD, fill=TEXT)
        y += 26
    y += 6
    rows = [
        ("Arena (cells of one unit)", numbers["size"]),
        ("Blocks / bushes", f"{numbers['blocks']} / {numbers['bushes']}"),
        ("Bushes in the centre (|y| <= 3)", str(numbers["bushes_centre"])),
        ("Blocks / bushes within 7 of own tower", f"{numbers['blocks_near_own_tower']} / {numbers['bushes_near_own_tower']}"),
        ("Poke cells on the red tower (gold), reached safely", f"{numbers['poke_cells']}, {numbers['poke_cells_safe']}"),
        ("Core chip cells (white), reached safely (filled)", f"{numbers['core_chip_cells']}, {numbers['core_chip_cells_safe']}"),
        ("Walk spawn to centre", f"{numbers['walk_centre']:.1f} u, {numbers['walk_centre'] / SPEED:.1f} s"),
        ("Walk spawn to the red tower", f"{numbers['walk_enemy_tower']:.1f} u, {numbers['walk_enemy_tower'] / SPEED:.1f} s"),
        ("Walk spawn to the red core", f"{numbers['walk_enemy_core']:.1f} u, {numbers['walk_enemy_core'] / SPEED:.1f} s"),
        ("Towers see each other", "yes" if numbers["towers_see_each_other"] else "no"),
        ("One-cell-wide passages (cells)", str(numbers["narrow_cells"])),
        ("A spawn inside a core of radius 1.5", "yes" if numbers["spawn_inside_core"] else "no"),
    ]
    for label, value in rows:
        canvas.text((x0, y), label, font=BODY, fill=MUTED)
        canvas.text((x0 + width_px, y), value, font=BODY, fill=TEXT, anchor="ra")
        y += 21
    y += 8
    for note in notes:
        lines = wrap(note, width_px - 14, BODY, canvas)
        canvas.text((x0, y), "-", font=BODY, fill=GOLD)
        for line in lines:
            canvas.text((x0 + 14, y), line, font=BODY, fill=TEXT)
            y += 20
        y += 4
    y += 6
    legend = [
        ("block (Static: stops heroes and straight shots)", BLOCK, "box"),
        ("bush (hides; stops nothing)", BUSH, "box"),
        ("tower: collider 0.6, model 0.9, range 5", BLUE, "dot"),
        ("core: proposed radius 1.5; dashed: today 0.75", RED, "dot"),
        ("spawn point / escape point / objective point", TEXT, "ring"),
        ("gold cell: hits the red tower from outside its range", GOLD, "small"),
        ("white cell: hits the red core from outside tower range;", (255, 255, 255), "hollow"),
        ("   filled when reached from the centre without entering it", (255, 255, 255), "none"),
        ("dashed frame: floor seen at 16:9 (x locked at 0)", (255, 255, 255), "line"),
    ]
    for label, colour, kind in legend:
        if kind == "box":
            canvas.rectangle([x0, y + 3, x0 + 14, y + 17], fill=colour, outline=BLOCK_EDGE if colour == BLOCK else BUSH_EDGE)
        elif kind == "dot":
            canvas.ellipse([x0, y + 3, x0 + 14, y + 17], fill=colour, outline=(255, 255, 255))
        elif kind == "ring":
            canvas.ellipse([x0, y + 3, x0 + 14, y + 17], outline=BLUE, width=3)
        elif kind == "small":
            canvas.rectangle([x0 + 3, y + 6, x0 + 11, y + 14], fill=colour)
        elif kind == "hollow":
            canvas.rectangle([x0 + 2, y + 5, x0 + 12, y + 15], outline=colour, width=2)
        elif kind == "line":
            canvas.line([x0, y + 10, x0 + 14, y + 10], fill=colour, width=2)
        canvas.text((x0 + 22, y + 1), label, font=SMALL, fill=TEXT)
        y += 20
    if y > image.height:
        grown = Image.new("RGB", (image.width, y + 12), INK)
        grown.paste(image, (0, 0))
        image = grown
    image.save(out, optimize=True)
    return image, header + map_h


def main():
    database, out_dir = Path(sys.argv[1]), Path(sys.argv[2])
    summary = {}
    maps = {"current": current_map(database)}
    texts = {"current": CURRENT_TEXT}
    for key, spec in LAYOUTS.items():
        maps[key] = build(spec)
        texts[key] = spec["text"]
    images = {}
    for key, m in maps.items():
        numbers = measure(m)
        title, subtitle, notes, file_name = texts[key]
        images[key] = render(m, numbers, title, subtitle, notes, out_dir / file_name)
        summary[key] = {k: round(v, 2) if isinstance(v, float) else v for k, v in numbers.items() if not isinstance(v, list)}
        print(key, json.dumps(summary[key]))
    # All four side by side, without their panels, at one scale of the game's units.
    thumbs = []
    for key in maps:
        image, map_height = images[key]
        crop = image.crop((0, 0, image.width - PANEL, map_height))
        thumbs.append(crop.resize((int(crop.width * 0.62), int(crop.height * 0.62)), Image.LANCZOS))
    sheet = Image.new("RGB", (sum(t.width for t in thumbs) + 12 * (len(thumbs) + 1), max(t.height for t in thumbs) + 24), INK)
    x = 12
    for thumb in thumbs:
        sheet.paste(thumb, (x, 12))
        x += thumb.width + 12
    sheet.save(out_dir / "siege-layouts-side-by-side-2026-10-17.png", optimize=True)
    (out_dir / "measures.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    text = []
    for key, m in maps.items():
        text.append(f"{key}: {2 * m['half_w'] + 1} x {2 * m['half_h'] + 1}, row y = {m['half_h']} first; "
                    f"'#' block, 'b' bush, '.' floor; 'C' core, 'T' tower, 'S' spawn mark the cell holding their centre "
                    f"(exact positions in the README)")
        for y in range(m["half_h"], -m["half_h"] - 1, -1):
            row = ""
            for x in range(-m["half_w"], m["half_w"] + 1):
                mark = "#" if (x, y) in m["blocks"] else "b" if (x, y) in m["bushes"] else "."
                for team in (0, 1):
                    if (round(m["cores"][team][0]), math.floor(m["cores"][team][1] + 0.5)) == (x, y):
                        mark = "C"
                    if (round(m["towers"][team][0] - 0.01), math.floor(m["towers"][team][1] + 0.5)) == (x, y):
                        mark = "T"
                    if any((round(p[0]), math.floor(p[1] + 0.5)) == (x, y) for p in m["spawns"][team]):
                        mark = "S"
                row += mark
            text.append(f"{y:4d} {row}")
        text.append("")
    (out_dir / "layouts.txt").write_text("\n".join(text), encoding="utf-8")


CURRENT_TEXT = (
    "Today: Map1, the sample's Coin Grab arena",
    "One scene for both modes; structures placed by code on free ground.",
    [
        "Spawns are not symmetric: team 0 at y -14, team 1 at y +16.",
        "Long belts of bushes at y -6/-7 and +6/+7 cross the width; the tower stands right behind one.",
        "The blocks were laid for coins at the centre, not for a push on a base: no lane leads to a tower.",
        "With a core of radius 1.5, the middle spawn of team 0 (0, -14) would stand inside it.",
    ],
    "siege-map-current-map1-2026-10-17.png",
)


# A: three lanes around the structures of today (same positions, same arena, same camera).
layout(
    "A",
    rows=grid(10, 16, mirrored([
        (-6, -5, -16, -10, "#"),     # the base's side walls, two thick, back wall to y -10
        (-4, -4, -10, -10, "#"),     # their returns: the base's mouth is x -3..3, all within the tower's range
        (-10, -9, -14, -12, "b"),    # a bush in each back corner, outside the base
        (-3, -3, -8, -7, "#"),       # cover beside the tower, inside its range
        (-1, 0, -4, -4, "#"),        # the block 4.5 rows in front of the tower (x -1..1)
        (-6, -5, -6, -4, "#"),       # the dividers between the side lanes and the middle
        (-10, -9, -8, -5, "b"),      # the side lanes' bushes, along the side walls
        (-8, -7, -10, -9, "b"),      # ... curving in towards the base
        (-4, -3, -1, 0, "b"),        # bushes of the centre
        (-8, -7, 0, 0, "#"),         # walls between the side lanes and the centre
        (0, 0, 0, 0, "#"),           # the block of the very centre
    ])),
    core=(0.0, -12.5), tower=(0.5, -8.5),
    spawns=[(-2.0, -15.5), (0.0, -15.5), (2.0, -15.5)],
    objectives=[(-3.5, -2.0), (3.5, -2.0), (0.0, -2.0)],
    escapes=[(-3.5, -15.0), (0.0, -16.0), (3.5, -15.0)],
    lanes=[[(-2, -14.5), (-3.5, -10.5), (-7.5, -7), (-7.5, 5), (-4, 8.5), (-1.6, 8.5)],
           [(0, -10.5), (-2.5, -6), (-2.5, -2), (-1.5, 2.5), (-0.5, 6.9)],
           [(2, -14.5), (3.5, -10.5), (7.5, -7), (7.5, 5), (4, 9.5), (0.7, 9.3)]],
    text=(
        "A. Three lanes",
        "Same arena, same camera, same structure positions; a walled base, a lane for each side and the middle.",
        [
            "The core sits where a Brawl Stars Heist safe or Siege turret sits (rows 3 to 4 from the back, centred), "
            "spawns behind it; walls two thick on both sides of the base (Junk Park, Factory Rush).",
            "A block 4.5 rows in front of the tower stops the straight shot down the middle (most Spirit Wars maps block "
            "the turret's front within 4 or 5 rows).",
            "Bushes on the flanks and at the centre, none within 7 of a tower: the Spirit Wars maps have a median of 0.5 "
            "bush cells within 7 tiles of the turret.",
            "Three chokepoints, one per lane, before the tower; none covers the other two.",
        ],
        "siege-map-candidate-a-three-lanes-2026-10-17.png",
    ),
)

# B: a walled keep with two gates; the middle is closed in front of the tower.
layout(
    "B",
    rows=grid(10, 16, mirrored([
        (-6, -5, -16, -10, "#"),     # the keep's side walls, two thick
        (-2, 0, -10, -10, "#"),      # the keep's front wall, behind the tower (x -2..2)
        (-10, -9, -14, -12, "b"),    # a bush in each back corner, outside the keep
        (-3, 0, -5, -5, "#"),        # the wall closing the middle (x -3..3) in front of the tower
        (-8, -7, -8, -7, "#"),       # cover in front of each gate's approach
        (-10, -9, -4, -1, "b"),      # the side lanes' bushes
        (-5, -4, -2, -1, "b"),       # bushes of the centre field
        (-7, -6, 0, 0, "#"),         # walls of the centre field
        (0, 0, 0, 0, "#"),
    ])),
    core=(0.0, -12.5), tower=(0.5, -8.5),
    spawns=[(-2.0, -15.5), (0.0, -15.5), (2.0, -15.5)],
    objectives=[(-3.5, -2.0), (3.5, -2.0), (0.0, -2.0)],
    escapes=[(-3.5, -15.0), (0.0, -16.0), (3.5, -15.0)],
    lanes=[[(-3.5, -11.5), (-3.5, -9.2), (-5.5, -7), (-6.5, 4), (-4.8, 6.6), (-1.6, 8.3)],
           [(3.5, -11.5), (3.5, -9.2), (5.5, -7), (6.5, 4), (4.8, 6.6), (0.7, 9.4)]],
    text=(
        "B. The keep and its two gates",
        "Same arena and positions; the core inside a closed keep, two gates beside the tower, no middle lane.",
        [
            "The core is reached only through a gate, and both gates are within the tower's range: "
            "the strongest defence of the three.",
            "A wall 7 wide closes the middle 3.5 rows in front of the tower (Nuts & Bolts' lake, Bot Drop's "
            "barriers): two lanes and a centre field instead of three lanes; the tower's square opens on the sides.",
            "Spawns inside the keep: defenders come out by the same gates, so a team pushed back is bottled up.",
        ],
        "siege-map-candidate-b-keep-gates-2026-10-17.png",
    ),
)

# C: bigger, 25 by 37 (between the 21 x 33 of Brawl Stars' 3v3 maps and the 27 x 39 of its Siege maps).
layout(
    "C",
    rows=grid(12, 18, mirrored([
        (-7, -6, -18, -12, "#"),     # the base's side walls, two thick
        (-5, -5, -12, -12, "#"),     # the returns of the base's mouth
        (-12, -11, -17, -12, "b"),   # back corners' bushes
        (-3, -3, -9, -8, "#"),       # cover beside the tower
        (-1, 0, -6, -6, "#"),        # the block 4.5 rows in front of the tower
        (-7, -6, -8, -5, "#"),       # dividers
        (-12, -11, -10, -6, "b"),    # side lanes' bushes
        (-10, -9, -12, -11, "b"),
        (-5, -4, -2, 0, "b"),        # centre bushes
        (-9, -8, 0, 0, "#"),
        (-2, -2, -1, 0, "#"),        # the centre's pillars (x -2 and 2)
    ])),
    core=(0.0, -14.5), tower=(0.0, -10.5),
    spawns=[(-2.0, -17.5), (0.0, -17.5), (2.0, -17.5)],
    objectives=[(-4.5, -2.5), (4.5, -2.5), (0.0, -3.0)],
    escapes=[(-3.5, -17.0), (0.0, -18.0), (3.5, -17.0)],
    lanes=[[(-2, -16.5), (-4, -12.5), (-9, -9), (-9, 6), (-5, 10.5), (-1.2, 10.5)],
           [(0, -12.5), (-2.8, -8), (-3, -1), (-1.5, 3), (-0.5, 8.8)],
           [(2, -16.5), (4, -12.5), (9, -9), (9, 6), (5, 10.5), (1.2, 10.5)]],
    text=(
        "C. Bigger: 25 x 37",
        "Answer to 'should siege be bigger': A's plan on a larger arena; structures moved back by 2.",
        [
            "Brawl Stars made its Siege maps 27 x 39 (19 maps in the game data) but its later turret mode, "
            "Spirit Wars, went back to the standard 21 x 33 (8 maps).",
            "25 wide still fits a 16:9 screen with the camera's x locked (26.8 units at the bottom edge, walls included "
            "26.5); 27 would not.",
            "Costs: new walls, floor, camera bounds (MinZ/MaxZ by 2), structure positions in code, and about 1.5 s more "
            "on every walk to the enemy base.",
        ],
        "siege-map-candidate-c-bigger-2026-10-17.png",
    ),
)


if __name__ == "__main__":
    main()
