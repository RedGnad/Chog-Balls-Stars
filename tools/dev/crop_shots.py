#!/usr/bin/env python3
"""Cut the same rectangle out of several screenshots and put the enlarged cuts side by side.

Usage: crop_shots.py <output.png> <left,top,right,bottom> <zoom> <image> [<image> ...]

For looking closely at one spot of the screen across runs, for instance the hero at its spawn
point under different layouts (needs Pillow).
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

output, box, zoom, files = Path(sys.argv[1]), tuple(int(v) for v in sys.argv[2].split(",")), float(sys.argv[3]), sys.argv[4:]
size = (int((box[2] - box[0]) * zoom), int((box[3] - box[1]) * zoom))
sheet = Image.new("RGB", (size[0] * len(files), size[1]), "black")
draw = ImageDraw.Draw(sheet)
for index, name in enumerate(files):
    cut = Image.open(name).convert("RGB").crop(box).resize(size, Image.LANCZOS)
    sheet.paste(cut, (index * size[0], 0))
    draw.text((index * size[0] + 6, 6), Path(name).parent.name[-28:], fill="white")
sheet.save(output)
print(output)
