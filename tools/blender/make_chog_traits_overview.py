"""Lays several dressed Chogs on one sheet for the owner, one row each: the picture of the Genesis,
the bust of the 3D hero beside it, the angle of the game coming towards the camera, and that angle at
the size of a phone. From the pictures of render_chog_traits.py.

It is plain Python with Pillow, not Blender. Run it with the private environment of the tools:

    .artifacts/tools-venv/bin/python tools/blender/make_chog_traits_overview.py
        [--genesis 1287,1241,80,1408,1078,83] [--extra 1948] [--out .artifacts/chog-body/traits/overview_six.png]

Every row has the same height. At its left: the number of the Genesis, its traits as the metadata
give them, the parts built for it and its triangles (parts.json, written by make_chog_traits.py).
"--extra" adds rows marked as outside the set that was asked for. The words are drawn with the font
that comes inside Pillow. The pictures are mock-ups made in Blender: nothing here is in the game.
"""

import argparse
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
TRAITS_FOLDER = os.path.join(REPOSITORY, ".artifacts", "chog-body", "traits")
ART_FOLDER = os.path.join(REPOSITORY, ".artifacts", "genesis-art")

ROW = 360
LABEL = 250
GAP = 10
HEADER = 96
PHONE_ZOOM = 2
BACKGROUND = (24, 22, 26)
INK = (236, 232, 226)
QUIET = (156, 152, 158)
SHOWN = ("Skin", "Head", "Eyes", "Mouth", "Body")


def font(size):
    return ImageFont.load_default(size)


def opened(path):
    if not os.path.isfile(path):
        raise SystemExit(f"CHOG_OVERVIEW no picture at {path}")
    return Image.open(path).convert("RGB")


def main():
    parser = argparse.ArgumentParser(description="One sheet for several dressed Chogs.")
    parser.add_argument("--genesis", default="1287,1241,80,1408,1078,83")
    parser.add_argument("--extra", default="", help="more Genesis, shown under the others and marked as extra")
    parser.add_argument("--out", default=os.path.join(TRAITS_FOLDER, "overview_six.png"))
    arguments = parser.parse_args()
    asked = [number for number in arguments.genesis.split(",") if number]
    extra = [number for number in arguments.extra.split(",") if number]
    rows = asked + extra

    phone = opened(os.path.join(TRAITS_FOLDER, rows[0], f"chog_{rows[0]}_game_front_phone.png")).width
    zoomed = phone * PHONE_ZOOM
    columns = (("The picture of the Genesis", ROW), ("The 3D hero, framed like it", ROW),
               ("Game angle, towards the camera", ROW), ("At the size of a phone", phone),
               (f"The same, each point {PHONE_ZOOM} times", zoomed))
    width = GAP + LABEL + sum(size + GAP for _, size in columns)
    height = HEADER + len(rows) * (ROW + GAP)
    sheet = Image.new("RGB", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    title, big, normal, small = font(32), font(30), font(15), font(14)

    more = f", and {len(extra)} more (last row{'s' if len(extra) > 1 else ''})" if extra else ""
    draw.text((GAP + 4, 8), f"{len(asked)} Genesis in 3D{more}: each trait a part on the same base", font=title,
              fill=INK)
    draw.text((GAP + 4, 48), "Mock-ups made in Blender: nothing here is in the game. Hats, glasses, visor, bubble, "
              "stream and beams have volume; eyes and mouths are flat shapes laid on the face; no image is used.",
              font=small, fill=QUIET)
    left = GAP + LABEL
    for caption, size in columns:
        draw.text((left + 2, HEADER - 22), caption, font=small, fill=INK)
        left += size + GAP

    top = HEADER
    for number in rows:
        folder = os.path.join(TRAITS_FOLDER, number)
        with open(os.path.join(folder, "parts.json"), encoding="utf-8") as source:
            built = json.load(source)
        draw.text((GAP + 4, top + 2), f"CHOG #{number}", font=big, fill=INK)
        line = top + 44
        if number in extra:
            draw.text((GAP + 4, line), "extra, see the report", font=small, fill=QUIET)
            line += 20
        for trait in SHOWN:
            draw.text((GAP + 4, line), f"{trait}: {built['traits'].get(trait, 'none')}", font=normal, fill=INK)
            line += 20
        line += 8
        for name, part in built["parts"].items():
            if part["on"].startswith("arm"):
                continue
            draw.text((GAP + 4, line), name, font=small, fill=QUIET)
            line += 18
        for value in built["left_out"]:
            draw.text((GAP + 4, line), f"NOT MODELLED: {value}", font=small, fill=(240, 150, 120))
            line += 18
        draw.text((GAP + 4, top + ROW - 22), f"{built['triangles_total']} triangles", font=small, fill=QUIET)

        portrait = os.path.join(ART_FOLDER, "1000", f"{number}.jpg")
        if not os.path.isfile(portrait):
            portrait = os.path.join(ART_FOLDER, "256", f"{number}.jpg")
        left = GAP + LABEL
        for path in (portrait, os.path.join(folder, f"chog_{number}_bust.png"),
                     os.path.join(folder, f"chog_{number}_game_front.png")):
            sheet.paste(opened(path).resize((ROW, ROW), Image.LANCZOS), (left, top))
            left += ROW + GAP
        small_picture = opened(os.path.join(folder, f"chog_{number}_game_front_phone.png"))
        sheet.paste(small_picture, (left, top + (ROW - phone) // 2))
        left += phone + GAP
        sheet.paste(small_picture.resize((zoomed, zoomed), Image.NEAREST), (left, top + (ROW - zoomed) // 2))
        top += ROW + GAP

    target = os.path.abspath(arguments.out)
    sheet.save(target, optimize=True)
    print(f"CHOG_OVERVIEW wrote {target} ({width} x {height}, {os.path.getsize(target) // 1024} KB)")


if __name__ == "__main__":
    sys.exit(main())
