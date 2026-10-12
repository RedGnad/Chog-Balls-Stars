#!/usr/bin/env python3
"""Tile screenshots into one image to scan a run at a glance.

Usage: tile_shots.py <output.png> <columns> <image> [<image> ...]
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

output, columns, files = Path(sys.argv[1]), int(sys.argv[2]), sys.argv[3:]
cell = (640, 360)
rows = (len(files) + columns - 1) // columns
sheet = Image.new("RGB", (cell[0] * columns, cell[1] * rows), "black")
draw = ImageDraw.Draw(sheet)
for index, name in enumerate(files):
    image = Image.open(name).convert("RGB").resize(cell)
    spot = ((index % columns) * cell[0], (index // columns) * cell[1])
    sheet.paste(image, spot)
    draw.text((spot[0] + 6, spot[1] + cell[1] - 16), Path(name).stem[-12:], fill="white")
sheet.save(output)
print(output)
