"""Lays the pictures of render_recruit.py on one sheet: the head turned around (front, three quarter, both
sides, back, and the game camera, 73 degrees above the ground, in front and behind), one row per folder
given, then the whole character of the last folder (front, three quarter, back, the game camera on the
floor, and that one at the size a phone shows it). Made on 17 October 2026 for the recruit's eyes.

    .artifacts/tools-venv/bin/python tools/blender/make_recruit_sheet.py --out FILE.png \
        --row "before=.artifacts/chog-body/recruit-eyes/recruit_before" \
        --row "after=.artifacts/chog-body/recruit-eyes/recruit"

Each folder holds chog_<folder name>_<view>.png. A picture that is missing is left grey and named.
"""

import argparse
import os

from PIL import Image, ImageDraw

CELL = 300
LABEL = 150
TITLE = 34
HEAD_VIEWS = (
    ("turn_000", "front"),
    ("turn_045", "three quarter"),
    ("turn_090", "its left side"),
    ("turn_270", "its right side"),
    ("turn_180", "back"),
    ("turn_game_front", "game camera, 73 deg, in front"),
    ("turn_game", "game camera, 73 deg, behind"),
)
WHOLE_VIEWS = (
    ("front", "front"),
    ("three_quarter", "three quarter"),
    ("back", "back"),
    ("game_front", "game camera, in front"),
    ("game", "game camera, behind"),
    ("game_front_phone", "game camera, phone size"),
)
BACKDROP = (30, 30, 34)
INK = (235, 235, 240)


def picture(folder, view):
    """The picture of one view, or None."""
    name = os.path.basename(os.path.normpath(folder))
    path = os.path.join(folder, f"chog_{name}_{view}.png")
    return Image.open(path).convert("RGB") if os.path.isfile(path) else None


def lay_row(sheet, draw, top, label, folder, views):
    draw.text((10, top + CELL // 2 - 10), label, fill=INK)
    for column, (view, caption) in enumerate(views):
        left = LABEL + column * CELL
        image = picture(folder, view)
        if image is None:
            draw.rectangle((left, top, left + CELL - 2, top + CELL - 2), fill=(70, 70, 74))
            draw.text((left + 8, top + CELL // 2), f"missing: {view}", fill=INK)
        elif view.endswith("_phone"):
            # The size a phone shows it: kept as it is, in the middle of the cell.
            sheet.paste(image, (left + (CELL - image.width) // 2, top + (CELL - image.height) // 2))
        else:
            sheet.paste(image.resize((CELL - 2, CELL - 2), Image.LANCZOS), (left, top))
        draw.text((left + 6, top + CELL - 18), caption, fill=INK)


def main():
    parser = argparse.ArgumentParser(description="One sheet of the recruit turned around.")
    parser.add_argument("--out", required=True, help="the sheet to write")
    parser.add_argument("--row", action="append", required=True, metavar="LABEL=FOLDER",
                        help="a row of the head turned around (may be given several times)")
    arguments = parser.parse_args()
    rows = [item.split("=", 1) for item in arguments.row]
    columns = max(len(HEAD_VIEWS), len(WHOLE_VIEWS))
    sheet = Image.new("RGB", (LABEL + columns * CELL, TITLE + (len(rows) + 1) * CELL), BACKDROP)
    draw = ImageDraw.Draw(sheet)
    draw.text((10, 10), "The recruit (number 0 of ChogParts.bytes), rendered from the library as the game "
                        "assembles it", fill=INK)
    for index, (label, folder) in enumerate(rows):
        lay_row(sheet, draw, TITLE + index * CELL, f"{label}\nhead", folder, HEAD_VIEWS)
    label, folder = rows[-1]
    lay_row(sheet, draw, TITLE + len(rows) * CELL, f"{label}\nwhole", folder, WHOLE_VIEWS)
    os.makedirs(os.path.dirname(os.path.abspath(arguments.out)), exist_ok=True)
    sheet.save(arguments.out)
    print(arguments.out, sheet.size)


if __name__ == "__main__":
    main()
