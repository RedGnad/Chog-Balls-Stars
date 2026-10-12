#!/usr/bin/env python3
"""One sheet of real Genesis pictures per trait value, to see which values share a shape.

Usage: trait_sheet.py <out.png> <trait> <value> [<value> ...]
       (run with .artifacts/tools-venv/bin/python: it needs Pillow)

One row per value, five pictures each, spread over the Genesis that carry it, with the value
and its count at the left and the number shown in the name ("CHOG #n") on each picture.
It reads the recorded metadata (research/evidence/genesis-metadata.jsonl) and the 256 px
pictures fetched by fetch_portraits.py (.artifacts/genesis-art/256/). Look at such a sheet
before modelling a trait: the name of a value says little about what is drawn.
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "research" / "evidence" / "genesis-metadata.jsonl"
PICTURES = ROOT / ".artifacts" / "genesis-art" / "256"
PER_ROW, SIZE, LABEL = 5, 200, 170

out, trait, values = Path(sys.argv[1]), sys.argv[2], sys.argv[3:]

by_value = {}
for line in SOURCE.read_text(encoding="utf-8").splitlines():
    if not line.strip():
        continue
    record = json.loads(line)
    for item in json.loads(record["raw_json"]).get("attributes", []):
        if str(item["trait_type"]) == trait:
            by_value.setdefault(str(item["value"]), []).append(int(record["metadata_id"]))

sheet = Image.new("RGB", (LABEL + PER_ROW * SIZE, len(values) * SIZE), (30, 30, 34))
draw = ImageDraw.Draw(sheet)
for row, value in enumerate(values):
    numbers = sorted(by_value.get(value, []))
    step = max(1, len(numbers) // PER_ROW)
    draw.text((8, row * SIZE + 80), f"{value}\n({len(numbers)})", fill=(255, 255, 255))
    for column, number in enumerate(numbers[::step][:PER_ROW]):
        picture = Image.open(PICTURES / f"{number}.jpg").convert("RGB").resize((SIZE, SIZE))
        sheet.paste(picture, (LABEL + column * SIZE, row * SIZE))
        draw.text((LABEL + column * SIZE + 4, row * SIZE + 4), f"#{number}", fill=(255, 255, 255))
out.parent.mkdir(parents=True, exist_ok=True)
sheet.save(out)
print(f"{out} ({sheet.width}x{sheet.height})")
