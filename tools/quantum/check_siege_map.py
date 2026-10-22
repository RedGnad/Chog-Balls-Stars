#!/usr/bin/env python3
"""Checks the siege's map as baked by the Editor (Chog.Editor.SiegeMapBuilder) against its layout, and
that the coins' map did not change.

  check_siege_map.py [--db .artifacts/unity/siege-map/quantum-db.json] [--reference site/quantum-db.json]
                     [--layout content/maps/siege-three-lanes.txt] [--record <replay.json>]
                     [--picture <out.png>] [--dotnet <path to dotnet>]

--db is the exported simulation assets with the siege's map in it (the builder writes it, a WebGL build
writes one beside the build); --reference an export made before (the assembled site's, by default):
Map1 and the asset InvisibilityGroups must be the same in both. With --record the judge's program
(tools/quantum/Verifier.cs, built by "headless.py verify") plays that record on the siege's map and says
whether rays cross the map where the layout says: a tower must not see through a block, and must see
through a bush. With --picture it draws the map's blocks, bushes, points and the navmesh of the bots
(.artifacts/unity/siege-map/navmesh.json, written by the builder) over the arena (needs Pillow).
Exit code 0 when every check passes.
"""
import argparse
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ONE = 65536.0
SIEGE = "QuantumUser/Resources/SiegeMap"
COINS = "QuantumUser/Resources/Map1"
GROUPS = "QuantumUser/Resources/InvisibilityGroups"
VERIFIER = ROOT / ".artifacts/dotnet-build/bin/Chog.Quantum.Verifier/release/Chog.Quantum.Verifier.dll"
LUT = ROOT / "game/Assets/Photon/Quantum/Resources/LUT"

# Rays the layout decides (from, to, clear): the towers at (0.5, -8.5) and (-0.5, 8.5) behind the cover
# beside them, and in the open; the block of the very centre; a bush, which hides but stops nothing.
SIGHTS = [
    ((0.5, -8.5), (-4.0, -7.5), False),
    ((0.5, -8.5), (4.0, -7.5), False),
    ((0.5, -8.5), (0.5, -5.0), True),
    ((0.5, -8.5), (2.0, -6.0), True),
    ((-0.5, 8.5), (4.0, 7.5), False),
    ((-0.5, 8.5), (-0.5, 5.0), True),
    ((-2.0, 0.0), (2.0, 0.0), False),
    ((-7.5, -2.0), (-9.5, -6.5), True),
]

failures = []


def check(condition, message):
    print(("ok    " if condition else "FAIL  ") + message)
    if not condition:
        failures.append(message)


def fp(value):
    return value.get("RawValue", 0) / ONE if isinstance(value, dict) else 0.0


def vec(value):
    return (fp(value.get("X", {})), fp(value.get("Y", {}))) if isinstance(value, dict) else (0.0, 0.0)


def by_path(assets):
    return {a.get("Identifier", {}).get("Path"): a for a in assets}


def read_layout(path):
    blocks, bushes, spawns, objectives, escapes = set(), set(), [], [], []
    for line in Path(path).read_text(encoding="utf-8").splitlines():
        parts = line.split()
        if not parts or parts[0].startswith("//"):
            continue
        if parts[0] == "row":
            y = int(parts[1])
            for i, cell in enumerate(parts[2]):
                (blocks if cell == "#" else bushes if cell == "b" else set()).add((i - 10, y))
        elif parts[0] == "spawn":
            spawns.append((int(parts[1]), float(parts[2]), float(parts[3])))
        elif parts[0] == "objective":
            objectives.append((float(parts[1]), float(parts[2])))
        elif parts[0] == "escape":
            escapes.append((int(parts[1]), float(parts[2]), float(parts[3])))
    return blocks, bushes, spawns, objectives, escapes


def map_points(game_map):
    """The map's entities: each one's position and its prototypes (the components follow their transform)."""
    entities, position = [], None
    for reference in game_map.get("references", {}).get("RefIds", []):
        kind, data = reference["type"]["class"], reference["data"]
        if kind == "Transform2DPrototype":
            position = vec(data["Position"])
            entities.append({"at": position})
        elif entities:
            entities[-1][kind] = data
    return entities


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--db", type=Path, default=ROOT / ".artifacts/unity/siege-map/quantum-db.json")
    parser.add_argument("--reference", type=Path, default=ROOT / "site/quantum-db.json")
    parser.add_argument("--layout", type=Path, default=ROOT / "content/maps/siege-three-lanes.txt")
    parser.add_argument("--record", type=Path)
    parser.add_argument("--picture", type=Path)
    parser.add_argument("--dotnet", default=shutil.which("dotnet"))
    args = parser.parse_args()

    assets = by_path(json.loads(args.db.read_text(encoding="utf-8-sig")))
    reference = by_path(json.loads(args.reference.read_text(encoding="utf-8-sig")))

    # The coins' map and the global groups of bushes: byte for byte what the reference export had.
    for path in (COINS, GROUPS):
        check(json.dumps(assets.get(path), sort_keys=True) == json.dumps(reference.get(path), sort_keys=True),
              f"{path} is the same as in {args.reference.relative_to(ROOT) if args.reference.is_relative_to(ROOT) else args.reference}")
    coins = assets[COINS]
    check(len(coins["StaticColliders2D"]) == 424 and len(coins["MapEntities"]) == 156,
          f"Map1 has 424 static colliders and 156 map entities ({len(coins['StaticColliders2D'])}, {len(coins['MapEntities'])})")
    added = sorted(set(assets) - set(reference))
    changed = sorted(p for p in set(assets) & set(reference)
                     if json.dumps(assets[p], sort_keys=True) != json.dumps(reference[p], sort_keys=True))
    print("      assets added:", ", ".join(map(str, added)) or "none")
    print("      assets changed:", ", ".join(map(str, changed)) or "none")

    siege = assets.get(SIEGE)
    check(siege is not None, f"{SIEGE} is in the export")
    if siege is None:
        return 1
    check(siege.get("Scene") == "GameplaySiege", f"SiegeMap names the scene GameplaySiege ({siege.get('Scene')})")
    blocks, bushes, spawns, objectives, escapes = read_layout(args.layout)
    colliders = siege["StaticColliders2D"]
    baked_blocks = {(round(vec(c["Position"])[0]), round(vec(c["Position"])[1])) for c in colliders
                    if c["StaticData"]["Name"] == "Collider" and c["Layer"] == 6}
    baked_bushes = {(round(vec(c["Position"])[0]), round(vec(c["Position"])[1])) for c in colliders
                    if c["StaticData"]["Name"] == "InvisibleProp" and c["Layer"] == 10 and c["StaticData"]["IsTrigger"]}
    walls = [c for c in colliders if c["StaticData"]["Name"] == "QuadStaticCollider"]
    cover = [c for c in colliders if c["StaticData"]["Name"].startswith("CoverPoint")]
    check(baked_blocks == blocks, f"blocks on the Static layer at the layout's {len(blocks)} '#' cells ({len(baked_blocks)} baked)")
    check(baked_bushes == bushes, f"bushes (triggers, InvisibilityPoint layer) at the layout's {len(bushes)} 'b' cells ({len(baked_bushes)} baked)")
    check(len(walls) == 30, f"the arena's 30 walls ({len(walls)})")
    every_off_grid = [c for c in colliders if c["StaticData"]["Name"] in ("Collider", "InvisibleProp")
                      and any(abs(v - round(v)) > 1e-4 for v in vec(c["Position"]))]
    check(not every_off_grid, "every block and bush on the centre of its cell")
    cover_ok = all(c["Layer"] == 9 and c["StaticData"].get("Asset", {}).get("Id", {}).get("Value", 0) != 0 for c in cover)
    beside = all(any((round(vec(c["Position"])[0]) + dx, round(vec(c["Position"])[1]) + dy) in blocks
                     for dx, dy in ((0, 1), (0, -1), (1, 0), (-1, 0))) for c in cover)
    check(len(cover) > 0 and cover_ok and beside, f"{len(cover)} cover points, each beside a block and with its direction's asset")

    entities = map_points(siege)
    spawn_points = sorted((e["SpawnPointPrototype"]["Team"], *e["at"]) for e in entities if "SpawnPointPrototype" in e)
    objective_points = sorted(e["at"] for e in entities if "ObjectivePointPrototype" in e)
    escape_points = sorted((e["EscapeRoutePrototype"]["TeamId"], *e["at"]) for e in entities if "EscapeRoutePrototype" in e)
    check(spawn_points == sorted(spawns), f"spawn points where the layout puts them ({spawn_points})")
    check(objective_points == sorted(objectives), "objective points where the layout puts them")
    check(escape_points == sorted(escapes), "escape points where the layout puts them")
    spots = [e for e in entities if "InvisibilitySpotPrototype" in e]
    index_of = {c["StaticData"]["ColliderIndex"]: c for c in colliders}
    spots_ok = len(spots) == len(bushes) and all(
        index_of.get(s["InvisibilitySpotPrototype"]["StaticColliderId"], {}).get("StaticData", {}).get("Name") == "InvisibleProp"
        and vec(index_of[s["InvisibilitySpotPrototype"]["StaticColliderId"]]["Position"]) == s["at"] for s in spots)
    neighbours = sum(1 for s in spots for key in ("NorthNeighborId", "SouthNeighborId", "EastNeighborId", "WestNeighborId")
                     if s["InvisibilitySpotPrototype"][key] >= 0)
    expected_neighbours = sum(1 for (x, y) in bushes for dx, dy in ((0, 1), (0, -1), (1, 0), (-1, 0)) if (x + dx, y + dy) in bushes)
    check(spots_ok, f"each of the {len(spots)} bush spots names its own collider (the second bake reached the map)")
    check(neighbours == expected_neighbours, f"bush neighbours as the layout has them ({neighbours} of {expected_neighbours})")

    links = [link["Id"]["Value"] for link in siege.get("NavMeshLinks", [])]
    navmeshes = [a for a in assets.values() if a and a.get("$type", "").startswith("Quantum.NavMesh,")
                 and a.get("Identifier", {}).get("Guid", {}).get("Value") in links]
    check(len(navmeshes) == 1 and navmeshes[0].get("Name") == "NavMesh",
          f"SiegeMap links one navmesh named NavMesh ({[n.get('Identifier', {}).get('Path') for n in navmeshes]})")

    if args.record:
        if not VERIFIER.is_file() or not args.dotnet:
            check(False, f"{VERIFIER} and a dotnet are needed for --record: run tools/quantum/headless.py verify")
        else:
            with tempfile.TemporaryDirectory() as folder:
                report_path = Path(folder) / "report.json"
                sights = ";".join(f"{a[0]},{a[1]}>{b[0]},{b[1]}" for a, b, _ in SIGHTS)
                subprocess.run([args.dotnet, str(VERIFIER), str(args.record), str(args.db), str(LUT), str(report_path),
                                "--test-map", SIEGE, "--test-mode", "Siege", "--test-sight", sights],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                report = json.loads(report_path.read_text(encoding="utf-8")) if report_path.is_file() else {}
                seen = report.get("sight") or []
                check(report.get("map") == SIEGE and len(seen) == len(SIGHTS), f"the record replayed on {SIEGE} ({report.get('map')})")
                for (start, end, clear), result in zip(SIGHTS, seen):
                    check(result["clear"] == clear, f"a ray from {start} to {end} is {'clear' if clear else 'stopped by a block'}")

    if args.picture:
        draw(args.picture, colliders, entities, blocks, bushes)
    print(f"{len(failures)} check(s) failed" if failures else "every check passed")
    return 1 if failures else 0


def draw(out, colliders, entities, blocks, bushes):
    from PIL import Image, ImageDraw
    scale, margin = 24, 24
    width, height = int(21 * scale + 2 * margin), int(33 * scale + 2 * margin)
    image = Image.new("RGB", (width, height), (24, 20, 40))
    pen = ImageDraw.Draw(image)

    def at(x, y):
        return (margin + (x + 10.5) * scale, margin + (16.5 - y) * scale)

    pen.rectangle([at(-10.5, 16.5), at(10.5, -16.5)], fill=(40, 34, 70))
    navmesh_path = ROOT / ".artifacts/unity/siege-map/navmesh.json"
    if navmesh_path.is_file():
        navmesh = json.loads(navmesh_path.read_text(encoding="utf-8"))
        vertices = navmesh["vertices"]
        for a, b, c in navmesh["triangles"]:
            pen.polygon([at(*vertices[a]), at(*vertices[b]), at(*vertices[c])], fill=(58, 70, 96), outline=(90, 110, 150))
    for x, y in bushes:
        pen.rectangle([at(x - 0.45, y + 0.45), at(x + 0.45, y - 0.45)], fill=(40, 170, 110))
    for x, y in blocks:
        pen.rectangle([at(x - 0.5, y + 0.5), at(x + 0.5, y - 0.5)], fill=(120, 100, 220), outline=(170, 150, 255))
    for c in colliders:
        if c["StaticData"]["Name"].startswith("CoverPoint"):
            x, y = vec(c["Position"])
            pen.ellipse([at(x - 0.15, y + 0.15), at(x + 0.15, y - 0.15)], fill=(240, 200, 80))
    for (x, y), radius, colour in (((0, -12.5), 1.5, (80, 140, 255)), ((0, 12.5), 1.5, (255, 90, 90)),
                                   ((0.5, -8.5), 0.6, (80, 140, 255)), ((-0.5, 8.5), 0.6, (255, 90, 90))):
        pen.ellipse([at(x - radius, y + radius), at(x + radius, y - radius)], outline=colour, width=3)
        if radius < 1:
            pen.ellipse([at(x - 5, y + 5), at(x + 5, y - 5)], outline=colour, width=1)
    for e in entities:
        x, y = e["at"]
        if "SpawnPointPrototype" in e:
            pen.ellipse([at(x - 0.3, y + 0.3), at(x + 0.3, y - 0.3)], outline=(255, 255, 255), width=2)
        elif "ObjectivePointPrototype" in e or "EscapeRoutePrototype" in e:
            pen.line([at(x - 0.3, y), at(x + 0.3, y)], fill=(255, 255, 255), width=2)
            pen.line([at(x, y - 0.3), at(x, y + 0.3)], fill=(255, 255, 255), width=2)
    out.parent.mkdir(parents=True, exist_ok=True)
    image.save(out)
    print("      picture:", out)


if __name__ == "__main__":
    sys.exit(main())
