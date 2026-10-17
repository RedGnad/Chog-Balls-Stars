#!/usr/bin/env python3
"""Counts what the Brawl Stars base maps are made of, from the game's own maps.csv, for the
README beside this file (section 3).

Usage: brawl_map_stats.py <maps.csv>

maps.csv of version 59.212.1:
https://raw.githubusercontent.com/tailsjs/brawl-stars-patches/HEAD/59.212.1/csv_logic/maps.csv
Tile codes from tiles.csv of version 65.219.1 of the same repository: walls (stop movement and
shots) M X Y C I T B N; bushes F R; water W and rope fence a (stop movement, not shots);
"8" the objective (IKE turret or safe); "1" and "2" the spawns.

For each family (Spirit Wars "SiegeSmall_", Siege "Siege_", Heist "Bankheist_"): the grid sizes,
the median share of walls, bushes and water, the median number of wall and bush cells within 7
tiles of the bottom objective, how many rows in front of it the straight line meets a wall or
water, how many rows it stands from the back edge, and how many maps are the same turned by 180
degrees (spawns 1 and 2 swapped).
"""
import collections
import csv
import statistics
import sys

WALL, BUSH, WATER = set("MXYCITBN"), set("RF"), set("Wa")


def read(path):
    rows = list(csv.reader(open(path, encoding="utf-8")))[2:]
    maps, current = {}, None
    for row in rows:
        if row[0].strip():
            current = row[0]
            maps[current] = []
        maps[current].append(row[1])
    return maps


def measure(grid):
    height, width = len(grid), len(grid[0])
    cells = height * width
    count = lambda kinds: sum(ch in kinds for line in grid for ch in line)
    objective = [(r, c) for r in range(height) for c in range(width) if grid[r][c] == "8"]
    r0, c0 = max(objective)
    near = lambda kinds: sum(1 for r in range(height) for c in range(width)
                             if grid[r][c] in kinds and ((r - r0) ** 2 + (c - c0) ** 2) ** 0.5 <= 7)
    front = next((k for k in range(1, 15) if r0 - k >= 0 and (grid[r0 - k][c0] in WALL or grid[r0 - k][c0] in WATER)), None)
    swap = str.maketrans("12", "21")
    turned = all(a == b.translate(swap) for a, b in zip(grid, [line[::-1] for line in grid[::-1]]))
    return {
        "size": f"{width}x{height}", "walls": 100 * count(WALL) / cells, "bushes": 100 * count(BUSH) / cells,
        "water": 100 * count(WATER) / cells, "walls_near": near(WALL), "bushes_near": near(BUSH),
        "front": front, "from_back": height - 1 - r0, "turned": turned,
    }


def main():
    maps = read(sys.argv[1])
    sizes = collections.Counter(f"{len(g[0])}x{len(g)}" for g in maps.values())
    print(f"{len(maps)} maps; most common sizes {sizes.most_common(4)}")
    for prefix in ("SiegeSmall_", "Siege_", "Bankheist_"):
        found = [measure(g) for name, g in maps.items() if name.startswith(prefix) and any("8" in line for line in g)]
        median = lambda key: round(statistics.median(m[key] for m in found), 1)
        print(f"{prefix}: {len(found)} maps, sizes {collections.Counter(m['size'] for m in found).most_common()}")
        print(f"   walls {median('walls')} %, bushes {median('bushes')} %, water {median('water')} % (medians)")
        print(f"   within 7 of the objective: walls {median('walls_near')}, bushes {median('bushes_near')} cells (medians)")
        print(f"   rows from the back edge: {collections.Counter(m['from_back'] for m in found).most_common()}")
        print(f"   straight line in front closed after (rows): {[m['front'] for m in found]}")
        print(f"   the same turned by 180 degrees: {sum(m['turned'] for m in found)} of {len(found)}")


if __name__ == "__main__":
    main()
