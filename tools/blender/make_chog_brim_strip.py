"""Lays several builds of the same hero one under the other, to compare a setting of its hat (first
of all its brim): for each, the bust, the angle of the game coming towards the camera, and that angle
at the size of a phone. From the pictures of render_chog_traits.py.

It is plain Python with Pillow, not Blender. Run it with the private environment of the tools:

    .artifacts/tools-venv/bin/python tools/blender/make_chog_brim_strip.py --genesis 1287
        --row "before: 0.130 long, dips 9 degrees=FOLDER" --row "after: ...=FOLDER" --out PICTURE

Each "--row" is the words written beside the row, "=", then the folder that holds the pictures
(chog_<number>_bust.png, chog_<number>_game_front.png, chog_<number>_game_front_phone.png). The words
are drawn with the font that comes inside Pillow. Mock-ups made in Blender: nothing here is in the game.
"""

import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageFont

TILE = 420
GAP = 10
CAPTION = 30
HEADER = 50
PHONE_ZOOM = 3
BACKGROUND = (24, 22, 26)
INK = (236, 232, 226)
QUIET = (156, 152, 158)


def main():
    parser = argparse.ArgumentParser(description="Several builds of one hero, one under the other.")
    parser.add_argument("--genesis", required=True)
    parser.add_argument("--row", action="append", required=True, metavar="WORDS=FOLDER")
    parser.add_argument("--title", default=None)
    parser.add_argument("--out", required=True)
    arguments = parser.parse_args()
    number = arguments.genesis
    rows = [item.rsplit("=", 1) for item in arguments.row]

    def opened(folder, end):
        path = os.path.join(folder, f"chog_{number}_{end}.png")
        if not os.path.isfile(path):
            raise SystemExit(f"CHOG_STRIP no picture at {path}")
        return Image.open(path).convert("RGB")

    phone = opened(rows[0][1], "game_front_phone").width
    zoomed = phone * PHONE_ZOOM
    width = GAP + 2 * (TILE + GAP) + phone + GAP + zoomed + GAP
    height = HEADER + len(rows) * (CAPTION + TILE) + GAP
    sheet = Image.new("RGB", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    title, normal, small = ImageFont.load_default(26), ImageFont.load_default(17), ImageFont.load_default(14)
    draw.text((GAP + 4, 8), arguments.title or f"CHOG #{number}: the brim of the hat", font=title, fill=INK)
    draw.text((width - 420, 20), "bust  |  game angle, towards the camera  |  at phone size, then 3 times",
              font=small, fill=QUIET)

    top = HEADER
    for words, folder in rows:
        draw.text((GAP + 4, top + 5), words, font=normal, fill=INK)
        top += CAPTION
        left = GAP
        for end in ("bust", "game_front"):
            sheet.paste(opened(folder, end).resize((TILE, TILE), Image.LANCZOS), (left, top))
            left += TILE + GAP
        small_picture = opened(folder, "game_front_phone")
        sheet.paste(small_picture, (left, top + (TILE - phone) // 2))
        left += phone + GAP
        sheet.paste(small_picture.resize((zoomed, zoomed), Image.NEAREST), (left, top + (TILE - zoomed) // 2))
        top += TILE

    target = os.path.abspath(arguments.out)
    sheet.save(target, optimize=True)
    print(f"CHOG_STRIP wrote {target} ({width} x {height})")


if __name__ == "__main__":
    sys.exit(main())
