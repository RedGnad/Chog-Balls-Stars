#!/usr/bin/env python3
"""Lists what the exported simulation assets say of the maps: where each team spawns, the
objective points, and the static colliders around a point.

Usage: list_map_points.py <quantum-db.json> [x,y radius]

Positions are in the units of the simulation (fixed point, 65536 to a unit). With a point and a
radius it also prints the static colliders whose centre is within that distance, to see whether
a spot is free before something is placed there.
"""
import json
import sys
from pathlib import Path

ONE = 65536.0


def fp(value):
    return round(value.get("RawValue", 0) / ONE, 2) if isinstance(value, dict) else 0.0


def vector(value):
    return (fp(value.get("X", {})), fp(value.get("Y", {}))) if isinstance(value, dict) else (0.0, 0.0)


def short(type_name):
    return type_name.split(",")[0].replace("Quantum.Prototypes.", "").replace("Prototype", "")


def main():
    assets = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
    near = None
    if len(sys.argv) > 3:
        x, y = (float(part) for part in sys.argv[2].split(","))
        near = (x, y, float(sys.argv[3]))
    for asset in assets:
        kind = asset.get("$type", "")
        if not kind.startswith("Quantum.Map,"):
            continue
        print("map", asset.get("Identifier", {}).get("Path"), "static colliders", len(asset.get("StaticColliders2D", [])),
              "map entities", len(asset.get("MapEntities", [])))
        # The components of the map's entities are kept as a flat list of references, each
        # entity's one after the other: a spawn or objective point follows its own transform.
        references = asset.get("references", {}).get("RefIds", [])
        position = None
        for reference in references:
            name = reference.get("type", {}).get("class", "")
            data = reference.get("data", {})
            if name == "Transform2DPrototype":
                position = vector(data.get("Position", {}))
            elif name == "SpawnPointPrototype":
                print("   spawn point of team", data.get("Team"), "at", position)
            elif name == "ObjectivePointPrototype":
                print("   objective point at", position)
        if near:
            for collider in asset.get("StaticColliders2D", []):
                position = vector(collider.get("Position", {}))
                if (position[0] - near[0]) ** 2 + (position[1] - near[1]) ** 2 <= near[2] ** 2:
                    print("   static collider at", position, "shape", collider.get("ShapeType"),
                          "extents", vector(collider.get("BoxExtents", {})), "radius", fp(collider.get("CircleRadius", {})))


if __name__ == "__main__":
    main()
