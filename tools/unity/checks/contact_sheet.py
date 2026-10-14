#!/usr/bin/env python3
"""Lay the pictures taken during a scripted run on one sheet, small, in the order they were taken.

Usage: contact_sheet.py <folder of a check> <out.png> [columns, default 6] [width of one picture, default 320]
       (run with .artifacts/tools-venv/bin/python: it needs Pillow)

It takes the pictures named touch-browser-linger-<n>.png, the ones a run takes on its way
(CHOG_BROWSER_LINGER_SHOTS), and writes their number on each: a whole match can be read at a
glance, and the pictures worth opening at full size are found by their number.
"""
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw

folder, out = Path(sys.argv[1]), Path(sys.argv[2])
columns = int(sys.argv[3]) if len(sys.argv) > 3 else 6
width = int(sys.argv[4]) if len(sys.argv) > 4 else 320

pictures = sorted((int(match[1]), path) for path in folder.glob("touch-browser-linger-*.png")
                  if (match := re.search(r"linger-(\d+)\.png$", path.name)))
if not pictures:
    sys.exit(f"no touch-browser-linger-<n>.png under {folder}")

first = Image.open(pictures[0][1])
height = round(width * first.height / first.width)
rows = (len(pictures) + columns - 1) // columns
sheet = Image.new("RGB", (columns * width, rows * height), (20, 20, 24))
draw = ImageDraw.Draw(sheet)
for index, (number, path) in enumerate(pictures):
    x, y = (index % columns) * width, (index // columns) * height
    sheet.paste(Image.open(path).convert("RGB").resize((width, height)), (x, y))
    draw.rectangle((x, y, x + 26, y + 14), fill=(0, 0, 0))
    draw.text((x + 4, y + 1), str(number), fill=(255, 255, 255))
out.parent.mkdir(parents=True, exist_ok=True)
sheet.save(out)
print(f"{out} ({sheet.width}x{sheet.height}, {len(pictures)} pictures)")
