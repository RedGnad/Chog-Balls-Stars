#!/usr/bin/env python3
"""Says how much free ground there is around spots of a map: the distance from each spot to
the surface of the nearest static collider (a wall, a crate, a bush marker), and which one.

Usage: free_ground.py <quantum-db.json> x,y [x,y ...]
       free_ground.py <quantum-db.json> --scan x0,x1,y0,y1,step,radius

With --scan it lists the spots of that grid where a disc of that radius fits, largest
clearance first: the places where a round structure can stand without touching anything.
Units of the simulation. The first map of the assets that has colliders is read.
"""
import json
import math
import sys
from pathlib import Path

ONE = 65536.0


def fp(value):
    return value.get("RawValue", 0) / ONE if isinstance(value, dict) else 0.0


def vector(value):
    return (fp(value.get("X", {})), fp(value.get("Y", {}))) if isinstance(value, dict) else (0.0, 0.0)


def distance_to(collider, x, y):
    cx, cy = vector(collider.get("Position", {}))
    ex, ey = vector(collider.get("BoxExtents", {}))
    if ex > 0 or ey > 0:
        # Into the frame of the box, then the usual distance to an axis-aligned box.
        rotation = fp(collider.get("Rotation", {}))
        dx, dy = x - cx, y - cy
        lx = dx * math.cos(-rotation) - dy * math.sin(-rotation)
        ly = dx * math.sin(-rotation) + dy * math.cos(-rotation)
        return math.hypot(max(abs(lx) - ex, 0.0), max(abs(ly) - ey, 0.0))
    return math.hypot(x - cx, y - cy) - fp(collider.get("CircleRadius", {}))


def nearest(colliders, x, y):
    best = min(colliders, key=lambda c: distance_to(c, x, y))
    return distance_to(best, x, y), best


def describe(collider):
    ex, ey = vector(collider.get("BoxExtents", {}))
    cx, cy = vector(collider.get("Position", {}))
    shape = f"box {2 * ex:g} x {2 * ey:g}" if ex > 0 or ey > 0 else f"circle of radius {fp(collider.get('CircleRadius', {})):g}"
    return f"{shape} at ({cx:g}, {cy:g})"


def main():
    assets = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
    game_map = next(a for a in assets if a.get("$type", "").startswith("Quantum.Map,") and a.get("StaticColliders2D"))
    colliders = game_map["StaticColliders2D"]
    if sys.argv[2] == "--scan":
        x0, x1, y0, y1, step, radius = (float(part) for part in sys.argv[3].split(","))
        found = []
        y = y0
        while y <= y1 + 1e-9:
            x = x0
            while x <= x1 + 1e-9:
                clearance, _ = nearest(colliders, x, y)
                if clearance >= radius:
                    found.append((clearance, x, y))
                x += step
            y += step
        for clearance, x, y in sorted(found, reverse=True)[:40]:
            print(f"({x:g}, {y:g}) clearance {clearance:.2f}")
        print(len(found), "spots of the grid take a disc of radius", radius)
        return
    for spot in sys.argv[2:]:
        x, y = (float(part) for part in spot.split(","))
        clearance, collider = nearest(colliders, x, y)
        print(f"({x:g}, {y:g}): {clearance:.2f} to the nearest collider, a {describe(collider)}")


if __name__ == "__main__":
    main()
