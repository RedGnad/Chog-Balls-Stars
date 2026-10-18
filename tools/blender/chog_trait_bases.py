"""The bases other than Origin (batch 7, 15 October 2026, pictures only): the Skull base first.

What the pictures of the 60 Skull Genesis show (sheets of .artifacts/chog-body/special/, made with
tools/genesis-art/trait_sheet.py Base Skull and Skin ...): a skull in the place of the head, bald and
round, no spike and no cream face, the whole head in one bone colour (white, beige, grey, purple, jade,
blue, metal, black, red or yellow, the value of "Skin"); the pink cheeks of a Chog; a small dark nose
hole; a row of teeth across the lower face in place of the mouth (no Skull Genesis has a "Mouth"); the
eyes of its "Eyes" value drawn as on any Chog; under the head a skeleton, its ribs and spine showing,
when no clothing covers it. Hats sit on the skull as on a head.

make_chog_traits.build() asks here: no leaf of the crown, no teeth of hair on the brow, and in place of
the cream face, add_skull_face(); the body gets add_ribs(). The colours are SKULL_SKINS, by "Skin".
"""

import math
from types import SimpleNamespace

from mathutils import Vector

import chog_trait_hats as hats_module
import make_chog_body as base

X, Y, Z = Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))

# Bone colours of the Skull Genesis, read on their pictures (the skull, out of its shade), by "Skin".
SKULL_SKINS = {
    "White": (0.95, 0.95, 0.93),
    "Beige": (0.93, 0.88, 0.76),
    "Grey": (0.62, 0.62, 0.66),
    "Purple": (0.60, 0.53, 0.82),
    "Jade": (0.55, 0.76, 0.64),
    "Blue": (0.46, 0.56, 0.78),
    "Metal": (0.36, 0.38, 0.45),
    "Black": (0.20, 0.20, 0.23),
    "Red": (0.86, 0.36, 0.36),
    "Yellow": (0.95, 0.84, 0.40),
}

# The face of a skull, read again on 18 October 2026 on the 1000-point pictures of #54, #846, #1079, #1313 (Side=Right)
# and #81, #540, #1207, #1104 (Side=Left), after the owner found the skulls "not cute and not faithful". Until then
# (16 October) a skull grinned with two rows of tall teeth on a large dark mouth from the nose to the chin, its
# jaw pinched to two thirds of its width, its cheeks at the corners of the mouth: from the front a monster's
# grin. The pictures draw instead:
# - Side=Right: under the nose, a compact grin of two rows of small rounded teeth in the bone colour, each tooth
#   outlined in dark, a dark line between the rows, the rows curving up a little at their ends; no dark mouth
#   behind them; the dark crack of the grin at its near end, going up the cheek. The jaw a little narrower than
#   the skull under the cheekbones, by the teeth.
# - Side=Left: the skull larger over a smaller jaw that steps in below the cheekbones; one row of small upper
#   teeth along the front of the jaw, and under it the dark of the open mouth.
# - Both: a dark nose like an upside-down heart, larger on Side=Right; pink cheeks under the outer corner of each
#   eye (they were at the corners of the mouth).
# Places in the coordinates of the face (to the side, above its middle), before the jaw is narrowed (narrow_jaw):
# each row is (height of its middle, half its width, half the height of a tooth, how many teeth, how far its ends
# rise); a tooth is outlined by LINE, GAP apart.
SKULL_FACE = {
    "Right": {
        # The two rows are little rounded beads, not a large, square monster grin.
        "ROWS": ((-0.096, 0.132, 0.0145, 7, 0.010), (-0.123, 0.112, 0.011, 6, 0.009)),
        "MOUTH": None,
        "CRACK": ((0.172, -0.128), (0.190, -0.104), (0.204, -0.078), (0.212, -0.056)),
        "CRACK_THICK": (0.014, 0.011, 0.007, 0.003),
        "NOSE": (-0.040, 0.030, 0.024),
        "CHEEK": (0.168, -0.040, 0.040, 0.027),
    },
    "Left": {
        "ROWS": ((-0.112, 0.118, 0.018, 6, 0.012),),
        # The open mouth under the row: middle height, half width, half height.
        "MOUTH": (-0.151, 0.070, 0.013),
        "CRACK": (),
        "CRACK_THICK": (),
        "NOSE": (-0.046, 0.022, 0.019),
        "CHEEK": (0.168, -0.040, 0.040, 0.027),
    },
}
TEETH = {
    "LINE": 0.0035,                          # the dark line round a skull tooth
    "GAP": 0.004,                            # between two teeth of a row, the line showing there
    "SQUARENESS": 2.15,
    "SIDES": 8,
    # A radioactive Chog (all of Side=Left) shows the skull through its skin, drawn in a deeper tone of its
    # colour (RADIOACTIVE_LINES), never in black: a row of small teeth along the bottom of the face, the line of
    # the jaw under it, and the cheekbones from under the eyes down to the ends of the row (#31, #343, #598,
    # #1134, #140). Row: middle height, half width, half height, count, how far its ends rise.
    # The Genesis art uses six small, almost round teeth, then a short shallow chin curve: it is the skull
    # seen through the glow, not a rectangular grill across the whole mouth.
    "GLOW_ROW": (-0.136, 0.112, 0.0145, 6, 0.012),
    "GLOW_LINE": 0.0038,
    "JAW_LINE": ((-0.158, -0.104), (-0.140, -0.138), (-0.086, -0.163), (0.0, -0.171), (0.086, -0.163),
                 (0.140, -0.138), (0.158, -0.104)),
    "JAW_THICK": 0.006,
    "CHEEKBONE": ((0.198, -0.028), (0.185, -0.070), (0.153, -0.106), (0.117, -0.122)),
    "CHEEKBONE_THICK": (0.004, 0.006, 0.006, 0.004),
}
NOSE = {"UP": -0.040, "HALF_WIDTH": 0.026, "HALF_HEIGHT": 0.021}

# Ribs: how many, from which height of the torso to which (shares of its height), how far around the
# front they go (degrees either side of straight ahead), their thickness.
RIBS = {"COUNT": 5, "FROM": 0.34, "TO": 0.80, "SPREAD_DEG": 64.0, "THICK": 0.014}


# The Radioactive base (30 Genesis, all of Side=Left): the spiky Chog, its colour running from a light
# top to a deeper bottom as if lit from inside (the "Skin" value: Green, Blue, Multi Colored, Red), no
# cream face, the teeth and nose of a skull showing on the lower face, the ribs through the body, and
# eyes that glow in a pale tint of the skin. (bottom, top) colours, read on their pictures.
# (Pushed further apart on 16 October 2026, the owner finding the run of colour faint on some: the bottoms
# deeper, the tops lighter, as the pictures glow.)
RADIOACTIVE_SKINS = {
    "Green": ((0.20, 0.70, 0.14), (0.96, 1.00, 0.34)),
    "Blue": ((0.10, 0.40, 0.88), (0.62, 0.97, 1.00)),
    "Multi Colored": ((0.30, 0.82, 0.30), (1.00, 0.62, 0.90)),
    "Red": ((0.80, 0.12, 0.36), (1.00, 0.72, 0.84)),
}
# The tone the skull shows in through the skin (its nose, teeth, jaw and cheekbones), read on the pictures (#31,
# #343, #598, #1134, #140; 18 October 2026): a deeper, saturated tone of the skin, never black (they were black
# until then). It is the "face" colour of the skin record: the lines are drawn with the face's material.
RADIOACTIVE_LINES = {
    "Green": (0.04, 0.52, 0.05),
    "Blue": (0.02, 0.33, 0.74),
    "Multi Colored": (0.12, 0.44, 0.10),
    "Red": (0.76, 0.04, 0.22),
}
# The radioactive crown keeps the same seven-point silhouette as the Genesis Chog. A short dark line at its
# roots separates the crown from the forehead, as in the portraits, without changing the points themselves.
RADIOACTIVE_HAIRLINE = ((-0.235, 0.205), (-0.155, 0.180), (-0.075, 0.215), (0.0, 0.185),
                        (0.075, 0.215), (0.155, 0.180), (0.235, 0.205))
RADIOACTIVE_HAIRLINE_THICK = 0.016
# Roots of volumetric Radioactive head items are clamped below the hairline. Their tips may rise through the
# crown as in the portraits, but a horn, mushroom or seal must visibly grow from the forehead, not the top.
RADIOACTIVE_FOREHEAD_TOP = 0.145
# The tint the eyes of each colour glow in (the white of an eye of the collection, in that colour).
RADIOACTIVE_EYE_TINTS = {
    "Green": (0.95, 1.0, 0.50),
    "Blue": (0.82, 0.98, 1.0),
    "Red": (1.0, 0.86, 0.90),
    "Mixed": (1.0, 0.96, 0.86),
}
# The eyes of the radioactive pictures are those of Side=Left in a colour: value word -> eye value drawn.
RADIOACTIVE_EYE_KINDS = {"Round Eye": "Round Eye", "Retard": "Retard", "Side Eye": "Side Eye", "Happy": "Happy",
                         "Smirk": "Smirk", "Flat": "Flat"}


def radioactive_eyes(table):
    """Entries for "<colour> <kind>": the entry of the plain kind, its whites in the glowing tint."""
    made = {}
    for colour, tint in RADIOACTIVE_EYE_TINTS.items():
        for word, plain in RADIOACTIVE_EYE_KINDS.items():
            entry = table.get(plain)
            if not isinstance(entry, dict):
                continue
            colours = {key: (tint if key == "White" else value) for key, value in entry["colours"].items()}
            made[f"{colour} {word}"] = {**entry, "colours": colours}
    return made


def is_radioactive(traits):
    return bool(traits) and traits.get("Base") == "Radioactive"


def is_skull(traits):
    return bool(traits) and traits.get("Base") == "Skull"


def skin_of(traits):
    """The colours of a Genesis on a base other than Origin: the skin and the face both; a radioactive one
    also the colour its skin runs to at the top ("top")."""
    if is_radioactive(traits):
        colours = RADIOACTIVE_SKINS.get(traits.get("Skin"))
        if colours is None:
            raise SystemExit(f"CHOG_TRAITS no colours recorded for a Radioactive of Skin={traits.get('Skin')}")
        return {"skin": colours[0], "face": RADIOACTIVE_LINES[traits.get("Skin")], "top": colours[1]}
    colour = SKULL_SKINS.get(traits.get("Skin"))
    if colour is None:
        raise SystemExit(f"CHOG_TRAITS no bone colour recorded for a Skull of Skin={traits.get('Skin')}")
    return {"skin": colour, "face": colour}


def rounded(middle, half_width, half_height, points=8, squareness=3.0):
    """The edge of a rounded box drawn on the face (a superellipse), as places (to the side, above)."""
    edge = []
    for index in range(points):
        angle = 2.0 * math.pi * index / points + math.pi / points
        c, s = math.cos(angle), math.sin(angle)
        edge.append((middle[0] + half_width * math.copysign(abs(c) ** (2.0 / squareness), c),
                     middle[1] + half_height * math.copysign(abs(s) ** (2.0 / squareness), s)))
    return edge


def tooth_row(part, surface, draw_shape, row, line, fill, outline, lifts):
    """A row of small rounded teeth (a row of SKULL_FACE or GLOW_ROW), each with its own thin outline.

    A single dark backing made the teeth look like a large grill. The portraits instead leave the head visible
    between the teeth, including between the two Skull rows.
    """
    up, half_row, half_tall, count, rise = row
    gap, square = TEETH["GAP"], TEETH["SQUARENESS"]
    wide = (2.0 * half_row - gap * (count - 1)) / count

    def height(across):
        return up + rise * (across / half_row) ** 2

    for index in range(count):
        centre = -half_row + wide * 0.5 + index * (wide + gap)
        # The teeth at the ends are shorter: the row follows the jaw.
        tall = half_tall * (0.80 if index in (0, count - 1) else 1.0)
        middle = (centre, height(centre))
        draw_shape(part, surface, outline, rounded(middle, wide * 0.5 + line, tall + line, TEETH["SIDES"], square), lifts[0],
                   middle=middle)
        draw_shape(part, surface, fill, rounded(middle, wide * 0.5, tall, TEETH["SIDES"], square), lifts[1], middle=middle)


def upside_down_heart(middle, half_width, half_height):
    """The nose of a skull: two lobes on top, a point below, as places (to the side, above)."""
    x, y = middle
    edge = []
    for index in range(7):
        angle = math.pi * index / 6
        edge.append((x + half_width * 0.5 + half_width * 0.5 * math.cos(angle), y + half_height * 0.25
                     + half_height * 0.75 * math.sin(angle)))
    for index in range(1, 7):
        angle = math.pi * index / 6
        edge.append((x - half_width * 0.5 + half_width * 0.5 * math.cos(angle), y + half_height * 0.25
                     + half_height * 0.75 * math.sin(angle)))
    edge.append((x - half_width * 0.85, y - half_height * 0.20))
    edge.append((x, y - half_height * 1.15))
    edge.append((x + half_width * 0.85, y - half_height * 0.20))
    return list(reversed(edge))


def add_skull_face(part, numbers, surface, full, draw_shape, oval, SKIN, EYE, BLUSH, FACE, cheeks=True, near=1.0,
                   side="Right"):
    """The cheeks, the nose and the teeth on the skull (SKULL_FACE, by Side), or on a radioactive head (TEETH:
    "cheeks" False), laid on the head as the face of an Origin Chog is (18 October 2026): the cheeks just over the
    head, the dark shapes over them, the teeth over those. The skull has no cream face: the bone of the head is
    its face. "near": the side of the character the picture shows nearer (1 its left), where a skull's grin
    cracks."""
    import make_chog_traits as traits_module
    lift = numbers.FACE_LIFT
    cheek_lift = lift + numbers.FACE_OVER[0]
    dark_lift, bone_lift = traits_module.DRAWN[0], traits_module.DRAWN[1]
    if cheeks:
        face = SKULL_FACE[side]
        cheek_side, cheek_up, cheek_width, cheek_height = face["CHEEK"]
        for sign in (1.0, -1.0):
            full.add_skin(part, surface, full.patch_loops(surface, (sign * cheek_side, cheek_up), cheek_width, cheek_height,
                                                          2.0, 0.0, cheek_lift, 1, 12), BLUSH)
        up, half_w, half_h = face["NOSE"]
        draw_shape(part, surface, EYE, upside_down_heart((0.0, up), half_w, half_h), dark_lift, middle=(0.0, up))
        if face["MOUTH"]:
            middle, half_width, half_height = face["MOUTH"]
            draw_shape(part, surface, EYE, rounded((0.0, middle), half_width, half_height, 16, 2.4), dark_lift,
                       middle=(0.0, middle))
        for row in face["ROWS"]:
            tooth_row(part, surface, draw_shape, row, TEETH["LINE"], SKIN, EYE, (dark_lift, bone_lift))
        if face["CRACK"]:
            crack = [(near * x, y) for x, y in face["CRACK"]]
            traits_module.draw_line(part, surface, EYE, crack, list(face["CRACK_THICK"]), dark_lift)
    else:
        # A radioactive Chog: the skull seen through the skin, in the deeper tone of the face's material.
        up, half_w, half_h = NOSE["UP"], NOSE["HALF_WIDTH"], NOSE["HALF_HEIGHT"]
        draw_shape(part, surface, FACE, upside_down_heart((0.0, up), half_w, half_h), dark_lift, middle=(0.0, up))
        tooth_row(part, surface, draw_shape, TEETH["GLOW_ROW"], TEETH["GLOW_LINE"], SKIN, FACE, (dark_lift, bone_lift))
        jaw = list(TEETH["JAW_LINE"])
        traits_module.draw_line(part, surface, FACE, jaw, [TEETH["JAW_THICK"]] * len(jaw), dark_lift)
        for sign in (1.0, -1.0):
            bone = [(sign * x, y) for x, y in TEETH["CHEEKBONE"]]
            traits_module.draw_line(part, surface, FACE, bone, list(TEETH["CHEEKBONE_THICK"]), dark_lift)


def add_radioactive_hairline(part, numbers, surface, FACE):
    """Draw the small crown-root line across a radioactive Genesis forehead."""
    import make_chog_traits as traits_module
    traits_module.draw_line(part, surface, FACE, RADIOACTIVE_HAIRLINE,
                            [RADIOACTIVE_HAIRLINE_THICK] * len(RADIOACTIVE_HAIRLINE), traits_module.DRAWN[0])


# The skull's jaw, read on the Skull pictures (CHOG #81, #846, #1000, #1104, #1207, #1642): a wide round
# cranium over a jaw narrower than it, the outline stepping in at the cheekbones, by the nose; the row of teeth
# is about as wide as the jaw. Heights in halves of the head's height from its middle (-1 its bottom): the head
# narrows from FROM down to TO, by NARROW of its width. (First: the round ball of the Chog with teeth drawn on
# it, a face more than a skull. Then, until 18 October 2026, 38 % from -0.30 to -0.45 on both Sides: the teeth
# squeezed into tall thin ones and the chin pinched. The pictures of Side=Right keep the grin half as wide as the
# skull; those of Side=Left step the jaw in further.)
JAW = {"Right": {"FROM": -0.40, "TO": -0.72, "NARROW": 0.20},
       "Left": {"FROM": -0.30, "TO": -0.62, "NARROW": 0.32}}


def narrow_jaw(part, numbers, side="Right"):
    """Narrows the lower head of a skull into its jaw (JAW, by Side): every point of the part (the head and what
    is drawn on it), before its outline is made, so the teeth and the nose stay on it. The eyes and anything
    worn higher are not touched."""
    jaw = JAW[side]
    for point in part.mesh.verts:
        height = (point.co.z - numbers.HEAD_MIDDLE) / numbers.HEAD_HALF_HEIGHT
        share = min(1.0, max(0.0, (jaw["FROM"] - height) / (jaw["FROM"] - jaw["TO"])))
        share = share * share * (3.0 - 2.0 * share)
        point.co.x *= 1.0 - jaw["NARROW"] * share


def bone_hands(parts, materials, skin, glove):
    """A skeleton's hands and feet are bone, not the pink mittens of a Chog: the material of the gloves
    becomes the skin (the bone colour) on the body and the arms."""
    for part in parts:
        for index, slot in enumerate(part.data.materials):
            if slot is not None and slot.name == glove:
                part.data.materials[index] = materials[skin]


def torso_section(numbers, height):
    """Half width and half depth of the torso at a height (TORSO_RINGS: height, width, depth)."""
    rings = numbers.TORSO_RINGS
    if height <= rings[0][0]:
        return rings[0][1] * 0.5, rings[0][2] * 0.5
    for lower, upper in zip(rings, rings[1:]):
        if height <= upper[0]:
            share = (height - lower[0]) / (upper[0] - lower[0])
            return (lower[1] + (upper[1] - lower[1]) * share) * 0.5, (lower[2] + (upper[2] - lower[2]) * share) * 0.5
    return rings[-1][1] * 0.5, rings[-1][2] * 0.5


def add_ribs(part, numbers, material):
    """The ribs and the spine of a skeleton, as dark lines around the front of the torso (and the back for
    the spine), a little off it, so the torso in its bone colour reads as a rib cage."""
    rings = numbers.TORSO_RINGS
    bottom, top = rings[0][0], rings[-1][0]
    thick = RIBS["THICK"]
    for index in range(RIBS["COUNT"]):
        share = RIBS["FROM"] + (RIBS["TO"] - RIBS["FROM"]) * index / max(1, RIBS["COUNT"] - 1)
        height = bottom + (top - bottom) * share
        half_w, half_d = torso_section(numbers, height)
        points = []
        spread = math.radians(RIBS["SPREAD_DEG"] + 6.0 * index)
        for step in range(8):
            # Around the front (-Y), from one side to the other, dipping a little at the middle.
            angle = -spread + 2.0 * spread * step / 7
            dip = 0.018 * math.cos(angle * math.pi / (2.0 * spread))
            points.append(Vector((math.sin(angle) * half_w * 1.03, -math.cos(angle) * half_d * 1.03, height - dip)))
        rings_of = []
        for step, point in enumerate(points):
            ahead = points[min(step + 1, 7)] - points[max(step - 1, 0)]
            first, second = base.frame_across(ahead, Z)
            rings_of.append((point, first * thick, second * thick))
        part.shell(rings_of, 4, material, 0.0)
    # The breastbone, down the middle of the front.
    low, high = bottom + (top - bottom) * (RIBS["FROM"] - 0.05), bottom + (top - bottom) * (RIBS["TO"] + 0.04)
    stations = []
    for step in range(5):
        height = low + (high - low) * step / 4
        _, half_d = torso_section(numbers, height)
        stations.append(Vector((0.0, -half_d * 1.035, height)))
    shell = []
    for step, point in enumerate(stations):
        shell.append((point, X * thick * 1.2, Y * thick * 0.8))
    part.shell(shell, 6, material, 0.0)


# ---------------------------------------------------------------------------------------------
# What the Radioactive Genesis wear on the head (their values of "Head"), read on their pictures
# (.artifacts/chog-body/special/skins.png): ram horns curling back (Red Horn, Purple Horn), ridged horns
# standing up (Demon Horn, Void Horn, devil horn), a third eye on the brow, a blue gem on the brow
# (Diamond), a gold seal above the brow (Holy Seal), a beam going up from the brow (Blue Laser), mushrooms
# (Poison Shroom). The whole crown stays: they stand among the spikes. (Until 16 October 2026 the horns
# grew out of the top of the skull, where the spikes of the crown were, and those spikes were taken out:
# the owner saw horns on the crown where the pictures put them on the brow, and a head without its
# spikes. They now grow from the front of the head, before the crown, which keeps every spike.)
# ---------------------------------------------------------------------------------------------

FULL = ((1.0, None, None),) * 7


def _out(surface, side, up):
    return (surface.front(side, up, 0.01) - surface.front(side, up, 0.0)).normalized()


def radioactive_forehead_up(up):
    """Keep a Radioactive head item's root on the forehead, below the crown-root line."""
    return min(up, RADIOACTIVE_FOREHEAD_TOP)


RAM_HORN = {
    # Read on CHOG #275, #473, #672, #1067, #1200 (Red Horn) and #31, #1745 (Purple Horn): each horn is
    # about as thick at its root as a sixth of the head, rises well above the crown, then curls out to the
    # side and down, a little to the back; its rings show as ridges. (First: a curl of 0.08 and a thickness
    # of 0.046, small horns beside the pictures; then curled back, hidden from the front.)
    # Where each horn grows: at the upper corner of the brow, in the coordinates of the face (to the side,
    # above its middle), sunk this far into the head. The pictures draw the root there, in front of the
    # spikes, which show all around (16 October 2026; the root was on the top of the head until then).
    "ROOT": (0.18, 0.19),
    "SUNK": 0.02,
    # From the brow it first rises straight up this far, so that the curl stands above the head, as drawn.
    "RISE": 0.13,
    "RISE_RIDGES": 4,
    "CURL": 0.135,            # radius of the curl where it starts; it tightens to TIGHTEN of it at the tip
    "TIGHTEN": 0.62,
    "SWEEP_DEG": 205.0,       # how far it turns, from straight up
    "THICK": 0.078,           # half its thickness at the root
    "TIP": 0.34,              # share of THICK left at the tip
    "OUT": 0.10,              # how far the tip drifts out, sideways
    "BACK": 0.35,             # the curl goes out to the side, turned this much towards the back
    # Rings along the curl and points around each (22 and 10 until 16 October 2026: with the whole crown
    # kept, #31 went over 8,000 triangles).
    "RIDGES": 14,
    "SIDES": 8,
}


def build_ram_horns(maker):
    """Two big ridged horns from the upper corners of the brow, rising up, then curling out to the side and
    down, as the pictures draw them (RAM_HORN)."""
    numbers = SimpleNamespace(**RAM_HORN)
    for side in (1.0, -1.0):
        part = maker.part()
        across, up = numbers.ROOT
        up = radioactive_forehead_up(up)
        root = maker.surface.front(across * side, up, -numbers.SUNK)
        back = (X * side + Y * numbers.BACK).normalized()
        across = back.cross(Z).normalized()     # square to the plane of the curl
        rings = []
        # The straight rise, its ridges as thick as those at the start of the curl.
        for step in range(numbers.RISE_RIDGES):
            first, second = base.frame_across(Z, across)
            thick = numbers.THICK * (1.12 if step % 2 == 0 else 0.88)
            rings.append((root + Z * numbers.RISE * step / numbers.RISE_RIDGES, first * thick, second * thick))
        start = root + Z * numbers.RISE
        centre = start + back * numbers.CURL
        sweep = math.radians(numbers.SWEEP_DEG)
        for step in range(numbers.RIDGES + 1):
            t = step / numbers.RIDGES
            angle = t * sweep
            r = numbers.CURL * (1.0 - (1.0 - numbers.TIGHTEN) * t)
            point = centre - back * r * math.cos(angle) + Z * r * math.sin(angle) + X * side * numbers.OUT * t * t
            ahead = (back * math.sin(angle) + Z * math.cos(angle) + X * side * numbers.OUT * 2.0 * t).normalized()
            first, second = base.frame_across(ahead, across)
            thick = numbers.THICK * (1.0 - (1.0 - numbers.TIP) * t) * (1.12 if (step + numbers.RISE_RIDGES) % 2 == 0
                                                                       else 0.88)
            rings.append((point, first * thick, second * thick))
        rings.append((rings[-1][0] + (rings[-1][0] - rings[-2][0]) * 0.7, Vector(), Vector()))
        part.shell(rings, numbers.SIDES, part.named("Horn"), 0.012)


DEMON_HORNS = {
    # Read on CHOG #343, #625, #1460 (Demon Horn), #140, #1222 (Void Horn) and #1225 (devil horn): five
    # ridged horns among the spikes, three on the top of the brow (one in the middle, one each side) and
    # one low on each side of the head, curving up. Each: where its root is on the face (to the side, above
    # its middle; mirrored when the side is not 0), how far it leans out against straight up (0 up, 1 as
    # the head faces there), its length, its half thickness at the root, how far its tip bends up.
    "HORNS": (
        ((0.00, 0.27), 0.45, 0.17, 0.050, 0.00),
        ((0.17, 0.24), 0.55, 0.17, 0.050, 0.02),
        ((0.27, 0.06), 0.90, 0.16, 0.050, 0.08),
    ),
    "SUNK": 0.025,
    "RINGS": 8,
    "SIDES": 6,                      # (7 on the first try: #1222, in a hoodie, went 31 triangles over 8,000)
}


def build_demon_horns(maker):
    """Five ridged horns standing among the spikes (DEMON_HORNS); the crown keeps every spike. A value may
    give its own horns ("horns" of its entry, as HORNS: the 1:1 devil has one pair)."""
    numbers = SimpleNamespace(**DEMON_HORNS)
    surface = maker.surface
    for (across, up), out, length, thick, bend in maker.entry.get("horns", numbers.HORNS):
        up = radioactive_forehead_up(up)
        for side in ((1.0,) if across == 0.0 else (1.0, -1.0)):
            part = maker.part()
            root = surface.front(across * side, up, -numbers.SUNK)
            facing = _out(surface, across * side, up)
            away = (Z * (1.0 - out) + facing * out).normalized()
            rings = []
            for step in range(numbers.RINGS + 1):
                t = step / numbers.RINGS
                point = root + away * length * t + Z * length * bend * t * t
                ahead = (away + Z * 2.0 * bend * t).normalized()
                first, second = base.frame_across(ahead, X)
                size = thick * (1.0 - 0.85 * t) * (1.12 if step % 2 == 1 else 0.96)
                rings.append((point, first * size, second * size))
            rings.append((root + away * length * 1.1 + Z * length * bend * 1.2, Vector(), Vector()))
            part.shell(rings, numbers.SIDES, part.named("Horn"), 0.012)


def build_third_eye(maker):
    import make_chog_traits as traits_module
    part, surface = maker.part(), maker.surface
    # Three layers on the brow, laid on the head (settled(): 0.026, 0.034 and 0.042 off it until 18 October 2026).
    lifts = [traits_module.settled(lift) for lift in (0.026, 0.026 * 1.3, 0.026 * 1.6)]
    traits_module.draw_shape(part, surface, part.named("Dark"), traits_module.oval((0.0, 0.135), 0.040, 0.026, 16),
                             lifts[0])
    traits_module.draw_shape(part, surface, part.named("White"), traits_module.oval((0.0, 0.135), 0.032, 0.019, 16),
                             lifts[1])
    traits_module.draw_shape(part, surface, part.named("Dark"), traits_module.oval((0.0, 0.134), 0.011, 0.016, 10),
                             lifts[2])


def build_gem(maker):
    part, surface = maker.part(), maker.surface
    out = _out(surface, 0.0, 0.13)
    centre = surface.front(0.0, 0.13, 0.012)
    first, second = base.frame_across(out, Z)
    rings = [(centre - out * 0.004, first * 0.030, second * 0.036), (centre + out * 0.014, first * 0.034, second * 0.040),
             (centre + out * 0.026, first * 0.020, second * 0.024), (centre + out * 0.030, Vector(), Vector())]
    part.shell(rings, 6, part.named("Gem"), 0.010, smooth=False)


HOLY_SEAL = {
    # Read on CHOG #415, #743 and #1813 (Holy Seal): a large gold wheel standing upright above the brow,
    # in front of the spikes, its lower edge over the top of the face; a lattice inside it and short rays
    # around it, each crossed near its tip. (Until 16 October 2026: a small ring lying on the top of the
    # skull, the upper spikes taken out.) Where it stands: above the middle of the face, then raised and
    # brought ahead of that point; how far it leans back (from the camera of the game it is seen better).
    "UP": 0.27,
    "RAISE": 0.085,
    "AHEAD": 0.075,
    "LEAN_BACK_DEG": 15.0,
    "RADIUS": 0.115,
    "TUBE": 0.014,
    "SEGMENTS": 16,
    "BARS": (-0.05, 0.0, 0.05),      # the lattice: bars across and up, at these distances from the middle
    "BAR": 0.007,
    "RAYS": 8,
    "RAY": (0.055, 0.014),           # length and half width at the root
    "CROSS": (0.70, 0.022, 0.006),   # the bar across a ray: where along it, half length, radius
}


def build_seal(maker):
    """Holy Seal (HOLY_SEAL): a gold wheel standing on the brow, before the crown, which keeps every spike."""
    numbers = SimpleNamespace(**HOLY_SEAL)
    part, surface = maker.part(), maker.surface
    lean = math.radians(numbers.LEAN_BACK_DEG)
    facing = (-Y * math.cos(lean) + Z * math.sin(lean)).normalized()
    upward = (Z * math.cos(lean) + Y * math.sin(lean)).normalized()
    up = radioactive_forehead_up(numbers.UP)
    centre = surface.front(0.0, up, 0.0) + Z * numbers.RAISE - Y * numbers.AHEAD
    gold = part.named("Gold")
    rings = []
    for step in range(numbers.SEGMENTS + 1):
        angle = 2.0 * math.pi * step / numbers.SEGMENTS
        outward = X * math.cos(angle) + upward * math.sin(angle)
        rings.append((centre + outward * numbers.RADIUS, outward * numbers.TUBE, facing * numbers.TUBE))
    part.shell(rings, 5, gold, 0.010)
    for offset in numbers.BARS:
        reach = math.sqrt(numbers.RADIUS ** 2 - offset ** 2)
        for along, across in ((X, upward), (upward, X)):
            start = centre + across * offset - along * reach
            part.shell(base.tube_rings(start, along, facing, [(0.0, numbers.BAR), (2.0 * reach, numbers.BAR)]), 4,
                       gold, 0.0)
    length, wide = numbers.RAY
    where, half, radius = numbers.CROSS
    for step in range(numbers.RAYS):
        angle = 2.0 * math.pi * step / numbers.RAYS + math.pi * 0.5
        outward = X * math.cos(angle) + upward * math.sin(angle)
        root = centre + outward * (numbers.RADIUS + numbers.TUBE * 0.5)
        part.shell(base.tube_rings(root, outward, facing, [(0.0, wide), (length * 0.55, wide * 0.8), (length, 0.0)]),
                   5, gold, 0.010)
        sideways = outward.cross(facing).normalized()
        middle = root + outward * length * where
        part.shell(base.tube_rings(middle - sideways * half, sideways, facing, [(0.0, radius), (2.0 * half, radius)]),
                   4, gold, 0.0)


HEAD_LASER = {
    # Read on CHOG #772 (Blue Laser, the only one): a wide pale blue beam leaving the middle of the brow from
    # a white glow, straight out of the head. (A thin beam straight up until 16 October 2026, which the bust
    # showed leaning the other way; then one at 45 degrees to the side, which the owner saw slanting along
    # the head where the picture has it come straight out of it.)
    "AT": 0.13,                       # above the middle of the face
    "UP": 0.12,                       # how far it is turned up from straight out of the head
    "BEAM": (0.026, 0.47),            # radius, length
    "GLOW": (0.040, 0.026),           # the white glow on the brow where it leaves: half width, half height
}


def build_head_laser(maker):
    """Blue Laser (HEAD_LASER): the beam of the brow and its glow."""
    numbers = SimpleNamespace(**HEAD_LASER)
    part, surface = maker.part(), maker.surface
    start = surface.front(0.0, numbers.AT, 0.0)
    way = (_out(surface, 0.0, numbers.AT) + Z * numbers.UP).normalized()
    radius, length = numbers.BEAM
    part.shell(base.tube_rings(start, way, Z, [(0.0, radius * 0.8), (0.04, radius), (length - 0.02, radius),
                                                (length, 0.0)]), 8, part.named("Beam"), 0.0)
    import make_chog_traits as traits_module
    wide, high = numbers.GLOW
    traits_module.draw_shape(part, surface, part.named("Glow"), traits_module.oval((0.0, numbers.AT), wide, high, 14),
                             traits_module.settled(0.026))


POISON_SHROOM = {
    # Read on CHOG #1100 (Poison Shroom, the only one): five mushrooms grow out of the forehead among the
    # spikes. There are two tall pale green caps, then three small foreground and side caps, each spotted in
    # orange or green. (Until 16 October 2026: two purple caps on the top of the skull, the upper spikes taken
    # out.) Each:
    # where its root is on the face (to the side, above its middle), how far it leans out (0 up, 1 as the
    # head faces there), the height of its stem, its radius, the radius of its cap, whether the cap is the
    # orange one.
    "SHROOMS": (
        ((-0.10, 0.145), 0.22, 0.165, 0.020, 0.088, False),
        ((0.13, 0.135), 0.42, 0.142, 0.018, 0.075, False),
        ((-0.255, 0.075), 0.78, 0.092, 0.014, 0.052, True),
        ((0.265, 0.075), 0.75, 0.082, 0.013, 0.046, False),
        ((0.035, 0.055), 0.93, 0.060, 0.012, 0.038, True),
    ),
    "SUNK": 0.02,
    # Five foreground mushrooms must still leave the dressed hero below 8,000 triangles.
    "STEM_SIDES": 6,
    "CAP_SIDES": 8,
    "CAP_STACKS": 5,
    "SPOTS": 3,                      # around each cap, halfway down it
    "SPOT": (0.016, 0.006),          # half width and half thickness of a spot, against a cap of 0.1
}


def build_mushrooms(maker):
    """Poison Shroom (POISON_SHROOM): spotted mushrooms before the crown, which keeps every spike."""
    numbers = SimpleNamespace(**POISON_SHROOM)
    surface = maker.surface
    for (across, up), out, height, stem_radius, cap, orange in numbers.SHROOMS:
        part = maker.part()
        up = radioactive_forehead_up(up)
        root = surface.front(across, up, -numbers.SUNK)
        stem = (Z * (1.0 - out) + _out(surface, across, up) * out).normalized()
        part.shell(base.tube_rings(root, stem, X, [(0.0, stem_radius * 1.15), (height, stem_radius)]),
                   numbers.STEM_SIDES,
                   part.named("Stem"), 0.010)
        top = root + stem * height
        cap_key, spot_key = ("Spot", "Cap") if orange else ("Cap", "Spot")
        part.shell(base.ball_rings(top + stem * cap * 0.2, stem, X, cap * 0.55, cap, cap, numbers.CAP_STACKS),
                   numbers.CAP_SIDES,
                   part.named(cap_key), 0.012)
        first, second = base.frame_across(stem, X)
        wide, thick = numbers.SPOT
        for step in range(numbers.SPOTS):
            angle = 2.0 * math.pi * step / numbers.SPOTS + 0.4
            polar = math.radians(55.0 if step % 2 == 0 else 35.0)
            radial = first * math.cos(angle) + second * math.sin(angle)
            normal = (stem * math.cos(polar) * 1.8 + radial * math.sin(polar)).normalized()
            point = (top + stem * cap * 0.2 + stem * cap * 0.55 * math.cos(polar) + radial * cap * math.sin(polar)
                     + normal * thick * 0.5 * cap / 0.1)
            size = cap / 0.1
            part.shell(base.ball_rings(point, normal, radial, thick * size, wide * size, wide * size, 3), 6,
                       part.named(spot_key), 0.0)


BANDANA_KNOT = {
    # Read on CHOG #874 and #1192 (Red Bandana): the band of a headband over the brow, tied in a knot on the
    # upper front of the head, two short flaps standing out of the knot.
    "WHERE": (0.42, -0.30, 0.86),     # direction from the middle of the head (side, front, up), side to the left
    "KNOT": 0.034,                    # radius of the knot
    "FLAP": (0.080, 0.034, 0.012),    # half length, half width, half thickness of each flap
    "FLAP_SPREAD_DEG": 38.0,          # each flap from straight out of the head
}


def build_bandana(maker):
    """Red Bandana: the band of a headband (chog_trait_hats.build_headband) and its knot with two flaps."""
    hats_module.build_headband(maker)
    numbers = SimpleNamespace(**BANDANA_KNOT)
    part = maker.part()
    out = Vector(numbers.WHERE).normalized()
    knot = maker.surface.towards(out) + out * numbers.KNOT * 0.6
    part.shell(base.ball_rings(knot, out, X, numbers.KNOT, numbers.KNOT * 1.15, numbers.KNOT, 6), 10,
               part.named("Band"), 0.012)
    sideways = out.cross(Z).normalized()
    for side in (1.0, -1.0):
        turn = math.radians(numbers.FLAP_SPREAD_DEG) * side
        along = (out * math.cos(turn) + sideways * math.sin(turn)).normalized()
        middle = knot + along * (numbers.KNOT + numbers.FLAP[0] * 0.8)
        length, width, thick = numbers.FLAP
        part.shell(base.ball_rings(middle, along, Z, length, width, thick, 6), 10, part.named("Band"), 0.012)


# ---------------------------------------------------------------------------------------------
# What only Skull Genesis wear on the head (16 October 2026: the owner saw bare skulls where the pictures
# draw them): a crown of thorns in gold (#1723, #1819, #1838) or in silver (#1107, #1647), a laurel wreath
# (#207, #418, #939, #1331), a banana taped on the brow (#373, #978), a fedora (#1311) and a turban (#1861).
# ---------------------------------------------------------------------------------------------

def _ring_path(surface, front_polar, back_polar, size, segments, wave=0.0, phase=0.0, waves=3):
    """Points around the head, at front_polar degrees from the top at the front and back_polar at the back:
    (point, way out of the head, angle seen from above, polar radians). "wave" makes the path wind in and
    out and up and down, "waves" times around."""
    points = []
    for step in range(segments):
        angle = 2.0 * math.pi * step / segments
        front = 0.5 - 0.5 * math.sin(angle)
        polar = math.radians(back_polar + (front_polar - back_polar) * front)
        point = hats_module.on_head(surface, angle, polar, size)
        out = hats_module.head_normal(surface, angle, polar)
        if wave:
            turn = waves * angle + phase
            point = point + (out * math.sin(turn) + Z * math.cos(turn) * 0.6) * wave
        points.append((point, out, angle, polar))
    return points


def _tube_loop(part, points, radius, material, sides, outline):
    """A tube around a closed path of points."""
    count, rings = len(points), []
    for index in range(count + 1):
        ahead = points[(index + 1) % count][0] - points[(index - 1) % count][0]
        first, second = base.frame_across(ahead, Z)
        rings.append((points[index % count][0], first * radius, second * radius))
    part.shell(rings, sides, material, outline)


THORN_CROWN = {
    # Two strands wound around each other around the top of the skull, lower at the front, thorns out.
    # (First try: a tube of 0.011 at 50 and 34 degrees, 20 thorns of 0.032: a thin ring on the top, where the
    # pictures draw a bushy crown down on the brow.)
    "FRONT": 60.0, "BACK": 44.0, "SIZE": 1.04, "SEGMENTS": 28, "TUBE": 0.017, "WAVE": 0.020,
    "THORNS": 28, "THORN": (0.052, 0.010),   # count, then the length and the radius of each thorn
}


def build_thorn_crown(maker):
    numbers = SimpleNamespace(**THORN_CROWN)
    part, surface = maker.part(), maker.surface
    vine = part.named("Vine")
    for phase in (0.0, math.pi):
        _tube_loop(part, _ring_path(surface, numbers.FRONT, numbers.BACK, numbers.SIZE, numbers.SEGMENTS,
                                    numbers.WAVE, phase), numbers.TUBE, vine, 5, 0.008)
    length, radius = numbers.THORN
    places = _ring_path(surface, numbers.FRONT, numbers.BACK, numbers.SIZE, numbers.THORNS)
    for index, (point, out, angle, polar) in enumerate(places):
        along = (places[(index + 1) % len(places)][0] - places[index - 1][0]).normalized()
        way = (out + Z * (0.55 if index % 2 == 0 else -0.35) + along * (0.35 if index % 3 else -0.35)).normalized()
        part.shell(base.tube_rings(point, way, Z, [(0.0, radius), (length, 0.0)]), 4, vine, 0.006)


LAUREL = {
    # A thin stem around the head and leaves along it on both sides, pointing back, one above and one below
    # the stem in turn, lying flat on the head.
    # (First try: 16 leaves of 0.050 at 46 degrees: a ring of grains, where the pictures draw large leaves.)
    "FRONT": 54.0, "BACK": 36.0, "SIZE": 1.02, "STEM": 0.008, "LEAVES": 12,
    "LEAF": (0.075, 0.032, 0.008),            # half length, half width, half thickness
}


def build_laurel(maker):
    numbers = SimpleNamespace(**LAUREL)
    part, surface = maker.part(), maker.surface
    _tube_loop(part, _ring_path(surface, numbers.FRONT, numbers.BACK, numbers.SIZE, 32), numbers.STEM,
               part.named("Stem"), 4, 0.0)
    half_long, half_wide, half_thick = numbers.LEAF
    places = _ring_path(surface, numbers.FRONT, numbers.BACK, numbers.SIZE + 0.01, numbers.LEAVES)
    for index, (point, out, angle, polar) in enumerate(places):
        tangent = (places[(index + 1) % len(places)][0] - places[index - 1][0]).normalized()
        backwards = tangent if tangent.y > 0.0 else -tangent
        along = (backwards + Z * (0.45 if index % 2 == 0 else -0.45) + out * 0.25).normalized()
        sideways = along.cross(out).normalized()
        part.shell(base.ball_rings(point + along * half_long * 0.9, along, sideways, half_long, half_wide, half_thick, 4),
                   6, part.named("Leaf"), 0.006)


TAPED_BANANA = {
    # The banana taped to the wall (Maurizio Cattelan's "Comedian"), on the brow, read again on #373 and #978
    # on 16 October 2026 (the owner: a reference, to be modelled as such; until then a short banana under two
    # crossed strips): nearly upright, its stem at the top in brown and its tip at the bottom in black, curved
    # with its back to the right of the character, its other side in a deeper yellow, and one wide strip of
    # grey tape across its middle, rising to the left of the character.
    "AT": (0.030, 0.175),          # its middle, in the coordinates of the face
    "TILT_DEG": -16.0,             # its top leans to the right of the character
    "OFF": 0.028,                  # its axis this far off the head
    # (First: 0.240 by 0.031 under a strip 0.092 wide: the tape hid the banana.)
    "LENGTH": 0.290, "RADIUS": 0.037, "BEND": 0.045, "RINGS": 11, "SIDES": 6,
    "STEM": (0.040, 0.010),        # length and radius of the stem, going on from its top
    # Half length, half width, degrees from across the face (rising to its left). Its middle is that of the
    # banana, where its back bulges (16 October 2026, the owner: the tape was off to the side, at AT, BEND to
    # the left of the banana; 38 degrees until then, 24 on the pictures).
    "TAPE": (0.165, 0.036, 28.0),
}


def build_taped_banana(maker):
    numbers = SimpleNamespace(**TAPED_BANANA)
    part, surface = maker.part(), maker.surface
    out = _out(surface, *numbers.AT)
    middle = surface.front(numbers.AT[0], numbers.AT[1], numbers.OFF)
    tilt = math.radians(numbers.TILT_DEG)
    upright = X * math.sin(tilt) + Z * math.cos(tilt)
    along = (upright - out * upright.dot(out)).normalized()
    sideways = out.cross(along).normalized()
    if sideways.x < 0.0:
        sideways = -sideways             # towards the left of the character
    half, rings = numbers.LENGTH * 0.5, []
    for step in range(numbers.RINGS):
        share = -1.0 + 2.0 * step / (numbers.RINGS - 1)
        radius = numbers.RADIUS * max(0.12, 1.0 - 0.80 * share ** 4)
        # The back of the banana bulges to the right of the character, its ends towards the left.
        centre = middle + along * half * share - sideways * numbers.BEND * (1.0 - share * share)
        ahead = (along + sideways * numbers.BEND * 2.0 * share / half).normalized()
        first, second = base.frame_across(ahead, sideways)
        rings.append((centre, first * radius, second * radius))
    _, faces = part.shell(rings, numbers.SIDES, part.named("Peel"), 0.008, smooth=False)
    for face in faces:
        centre = face.calc_center_median()
        along_here = (centre - middle).dot(along) / half
        if along_here < -0.86:
            part.paint([face], part.named("Tip"))
        elif (centre - middle).dot(sideways) > numbers.RADIUS * 0.15:
            part.paint([face], part.named("Shade"))
    length, radius = numbers.STEM
    top = rings[-1][0]
    stem_way = (along - sideways * 0.35).normalized()
    part.shell(base.tube_rings(top - stem_way * 0.004, stem_way, sideways, [(0.0, radius * 1.3), (length, radius)]), 6,
               part.named("Stem"), 0.006)
    half_length, half_width, degrees = numbers.TAPE
    # The strip runs across the face (rising to its left), through the middle of the banana, where its back
    # bulges: on the head on each side, over the banana in the middle, wrapped round its outer half (the
    # side away from the head), and down to the head again beside it. (Until 16 October 2026 it was lifted
    # off the head along the brow, which put it above the banana and behind it from the front.)
    angle = math.radians(degrees)
    middle_of_banana = rings[numbers.RINGS // 2][0]
    flat = Vector((math.cos(angle), 0.0, math.sin(angle)))
    way = (flat - out * flat.dot(out)).normalized()
    across_banana = (way - along * way.dot(along)).normalized()
    slant = way.dot(across_banana)
    radius = numbers.RADIUS + 0.004
    path, wide, thick = [], [], []
    for step in range(13):
        t = -1.0 + 2.0 * step / 12
        here = middle_of_banana + way * half_length * t
        # Where the strip lies on the head under that place.
        side = here.x - surface.middle.x - surface.face_middle[0]
        up = here.z - surface.middle.z - surface.face_middle[1]
        on_head = surface.front(side, up, 0.006)
        # Where it lies on the banana: across it by "beside", on its outer half.
        beside = half_length * t * slant
        if abs(beside) < radius:
            on_banana = here + out * math.sqrt(radius * radius - beside * beside)
        else:
            on_banana = here
        share = min(1.0, max(0.0, (radius + 0.030 - abs(beside)) / 0.030))
        share = share * share * (3.0 - 2.0 * share)
        point = on_head + (on_banana - on_head) * share
        path.append(point)
        wide.append(out.cross(way).normalized() * half_width)
        thick.append(out * 0.004)
    hats_module.slab(part, path, wide, thick, part.named("Tape"), 0.008, sides=4)


def build_fedora(maker):
    """The fedora of #1311: the round crown and the brim of the bucket hat, taller and flatter (the
    numbers of its entry), and a band around the foot of the crown."""
    import make_chog_traits as traits_module
    traits_module.build_bucket(maker)
    numbers = maker.numbers(traits_module.BUCKET)
    part = maker.part()
    crown = traits_module.CapCrown(numbers, maker.surface.middle)
    low = numbers.BRIM_HEIGHT + numbers.BRIM_THICKNESS * 0.5
    angles = [2.0 * math.pi * index / numbers.SIDES for index in range(numbers.SIDES)]
    loops = [[crown.around(height, angle, 1.025) for angle in angles] for height in (low, low + FEDORA_BAND)]
    traits_module.full.add_skin(part, maker.surface, loops, part.named("Band"))


FEDORA_BAND = 0.046


HEAD_WRAP = {
    # A turban over the skull down to the brow, wider than the head and puffed in folds, two of its rows in
    # the shade of a fold. The rim: (degrees from the front, polar degrees).
    "RIM": ((0.0, 66.0), (60.0, 82.0), (120.0, 98.0), (180.0, 102.0)),
    "ROWS": 5, "SIZE": 1.16, "SIDES": 16, "SWELL": 0.030, "FOLDS": (1, 3),
}


def build_turban(maker):
    numbers = SimpleNamespace(**HEAD_WRAP)
    part, surface = maker.part(), maker.surface
    bands = hats_module.dome(part, surface, part.named("Cloth"), numbers.SIDES, numbers.RIM, numbers.ROWS, numbers.SIZE,
                             0.014, swell=lambda angle, polar, row: numbers.SWELL * math.sin(2.0 * angle + 1.3 * row))
    for row in numbers.FOLDS:
        if row < len(bands):
            part.paint(bands[row], part.named("Fold"))


HAT_SHAPES = {"RamHorns": build_ram_horns, "Bandana": build_bandana, "DemonHorns": build_demon_horns,
              "ThirdEye": build_third_eye, "ThornCrown": build_thorn_crown, "Laurel": build_laurel,
              "TapedBanana": build_taped_banana, "Fedora": build_fedora, "Turban": build_turban,
              "Gem": build_gem, "Seal": build_seal, "HeadLaser": build_head_laser, "Mushrooms": build_mushrooms}
# Every one of these stands before the crown, which keeps all its spikes (the build refuses a leaf through an
# object, so a part that grew into a spike would fail its Genesis).
HAT_NUMBERS = {name: {"LEAVES": FULL, "OUTLINE_THICKNESS": 0.012} for name in HAT_SHAPES}
# The bandana lies where a headband does (chog_trait_hats.HEADBAND), under the whole crown, as the headbands
# do since 16 October 2026: its crossings with the leaves are kept, not refused.
HAT_NUMBERS["Bandana"] = {**hats_module.HEADBAND, "LEAVES": FULL}
HATS = {
    "Red Bandana": {"shape": "Bandana", "colours": {"Band": (0.86, 0.20, 0.18), "Line": (0.62, 0.11, 0.12)}},
    "Red Horn": {"shape": "RamHorns", "colours": {"Horn": (0.62, 0.30, 0.24)}},
    "Purple Horn": {"shape": "RamHorns", "colours": {"Horn": (0.56, 0.42, 0.86)}},
    "Demon Horn": {"shape": "DemonHorns", "colours": {"Horn": (0.95, 0.52, 0.32)}},
    "Void Horn": {"shape": "DemonHorns", "colours": {"Horn": (0.52, 0.36, 0.86)}},
    "devil horn": {"shape": "DemonHorns", "colours": {"Horn": (0.90, 0.26, 0.26)}},
    "3rd Eye": {"shape": "ThirdEye", "colours": {"Dark": (0.07, 0.05, 0.10), "White": (0.98, 0.97, 0.94)},
                "unlit": ("Dark", "White")},
    "Diamond": {"shape": "Gem", "colours": {"Gem": (0.36, 0.76, 0.98)}, "unlit": ("Gem",)},
    "Holy Seal": {"shape": "Seal", "colours": {"Gold": (0.98, 0.80, 0.22)}},
    "Blue Laser": {"shape": "HeadLaser", "colours": {"Beam": (0.55, 0.92, 1.0), "Glow": (0.97, 0.99, 1.0)},
                   "unlit": ("Beam", "Glow")},
    "Gold Thorn": {"shape": "ThornCrown", "colours": {"Vine": (0.93, 0.62, 0.18)}},
    "Silver Thorn": {"shape": "ThornCrown", "colours": {"Vine": (0.64, 0.64, 0.66)}},
    "Silver wreath": {"shape": "Laurel", "colours": {"Leaf": (0.82, 0.76, 0.62), "Stem": (0.52, 0.44, 0.34)}},
    "Taped Banana": {"shape": "TapedBanana", "colours": {"Peel": (0.99, 0.82, 0.20), "Shade": (0.92, 0.62, 0.12),
                                                         "Tip": (0.10, 0.08, 0.06), "Stem": (0.45, 0.30, 0.13),
                                                         "Tape": (0.62, 0.62, 0.66)}},
    "Mafia Hat": {"shape": "Fedora",
                  "colours": {"Crown": (0.21, 0.21, 0.24), "Brim": (0.21, 0.21, 0.24), "Under": (0.12, 0.12, 0.14),
                              "Band": (0.88, 0.88, 0.90)},
                  "numbers": {"RINGS": ((0.090, 0.99, 0.000), (0.160, 1.00, 0.004), (0.262, 0.95, 0.016),
                                        (0.332, 0.80, 0.030), (0.372, 0.50, 0.040)),
                              "TOP": (0.386, 0.040), "BRIM_LENGTH": 0.130, "BRIM_DIP_DEG": 6.0,
                              "BRIM_FRONT_LENGTH": 0.120, "BRIM_FRONT_RISE_DEG": 10.0, "BRIM_FRONT_SPREAD": 1.0}},
    "Head Wrap": {"shape": "Turban", "colours": {"Cloth": (0.95, 0.95, 0.93), "Fold": (0.80, 0.80, 0.83)}},
    "Poison Shroom": {"shape": "Mushrooms", "colours": {"Stem": (0.80, 0.90, 0.78), "Cap": (0.62, 0.84, 0.70),
                                                        "Spot": (0.95, 0.58, 0.24)}},
}
