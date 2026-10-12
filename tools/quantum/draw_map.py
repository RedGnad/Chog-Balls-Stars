#!/usr/bin/env python3
"""Draws a map of the simulation from above, as the exported assets describe it: the static
colliders (walls, crates), where each team spawns and the objective points, on a grid of one
unit. Used to find free ground before something is placed, and to check a placement.

Usage: draw_map.py <quantum-db.json> <out.png> [--mark x,y,label ...] [--scale pixels per unit]

Needs Pillow. Positions are in units of the simulation; +y is up on the picture.
"""
import argparse
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw

ONE = 65536.0


def fp(value):
    return value.get("RawValue", 0) / ONE if isinstance(value, dict) else 0.0


def vector(value):
    return (fp(value.get("X", {})), fp(value.get("Y", {}))) if isinstance(value, dict) else (0.0, 0.0)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("database", type=Path)
    parser.add_argument("out", type=Path)
    parser.add_argument("--mark", action="append", default=[], help="x,y,label: a spot to show")
    parser.add_argument("--scale", type=int, default=22)
    args = parser.parse_args()

    assets = json.loads(args.database.read_text(encoding="utf-8-sig"))
    game_map = next(a for a in assets if a.get("$type", "").startswith("Quantum.Map,"))
    colliders = game_map.get("StaticColliders2D", [])

    spawns, objectives, position = [], [], None
    for reference in game_map.get("references", {}).get("RefIds", []):
        name = reference.get("type", {}).get("class", "")
        data = reference.get("data", {})
        if name == "Transform2DPrototype":
            position = vector(data.get("Position", {}))
        elif name == "SpawnPointPrototype":
            spawns.append((position, data.get("Team")))
        elif name == "ObjectivePointPrototype":
            objectives.append(position)

    points = [vector(c.get("Position", {})) for c in colliders] + [p for p, _ in spawns] + objectives
    left, right = math.floor(min(p[0] for p in points)) - 2, math.ceil(max(p[0] for p in points)) + 2
    bottom, top = math.floor(min(p[1] for p in points)) - 2, math.ceil(max(p[1] for p in points)) + 2
    scale = args.scale
    image = Image.new("RGB", ((right - left) * scale, (top - bottom) * scale), (24, 26, 34))
    draw = ImageDraw.Draw(image)

    def at(x, y):
        return ((x - left) * scale, (top - y) * scale)

    for x in range(left, right + 1):
        draw.line([at(x, bottom), at(x, top)], fill=(60, 64, 78) if x % 5 else (96, 100, 118))
        if x % 5 == 0:
            draw.text(at(x + 0.1, top - 0.1), str(x), fill=(170, 174, 190))
    for y in range(bottom, top + 1):
        draw.line([at(left, y), at(right, y)], fill=(60, 64, 78) if y % 5 else (96, 100, 118))
        if y % 5 == 0:
            draw.text(at(left + 0.1, y + 0.9), str(y), fill=(170, 174, 190))

    kinds = {}
    for collider in colliders:
        x, y = vector(collider.get("Position", {}))
        kind = collider.get("ShapeType")
        kinds[kind] = kinds.get(kind, 0) + 1
        radius = fp(collider.get("CircleRadius", {}))
        ex, ey = vector(collider.get("BoxExtents", {}))
        rotation = fp(collider.get("Rotation", {}))
        if ex > 0 or ey > 0:
            # A box, drawn with its turn (radians) around its centre.
            corners = []
            for cx, cy in ((-ex, -ey), (ex, -ey), (ex, ey), (-ex, ey)):
                corners.append(at(x + cx * math.cos(rotation) - cy * math.sin(rotation),
                                  y + cx * math.sin(rotation) + cy * math.cos(rotation)))
            draw.polygon(corners, fill=(150, 110, 70), outline=(210, 170, 120))
        elif radius > 0:
            x0, y0 = at(x - radius, y + radius)
            x1, y1 = at(x + radius, y - radius)
            draw.ellipse([x0, y0, x1, y1], fill=(70, 140, 90), outline=(120, 200, 140))
        else:
            px, py = at(x, y)
            draw.rectangle([px - 2, py - 2, px + 2, py + 2], fill=(200, 60, 60))

    for (x, y), team in spawns:
        px, py = at(x, y)
        colour = (80, 150, 255) if team == 0 else (255, 90, 90)
        draw.ellipse([px - 6, py - 6, px + 6, py + 6], outline=colour, width=3)
    for x, y in objectives:
        px, py = at(x, y)
        draw.line([px - 6, py, px + 6, py], fill=(250, 220, 60), width=3)
        draw.line([px, py - 6, px, py + 6], fill=(250, 220, 60), width=3)
    for mark in args.mark:
        sx, sy, label = mark.split(",", 2)
        px, py = at(float(sx), float(sy))
        draw.ellipse([px - 9, py - 9, px + 9, py + 9], outline=(255, 255, 255), width=2)
        draw.text((px + 12, py - 6), label, fill=(255, 255, 255))

    image.save(args.out)
    print(f"{args.out}: x {left}..{right}, y {bottom}..{top}, {len(colliders)} static colliders {kinds}, "
          f"{len(spawns)} spawn points, {len(objectives)} objective points")


if __name__ == "__main__":
    main()
