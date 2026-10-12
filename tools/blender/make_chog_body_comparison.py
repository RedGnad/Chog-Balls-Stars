"""Lays the present Chog body and the three candidates side by side on one sheet, large and at the
size of a phone screen, from the pictures of render_chog_body_variants.py.

It is plain Python with Pillow, not Blender. Run it with the private environment of the tools:

    .artifacts/tools-venv/bin/python tools/blender/make_chog_body_comparison.py
        [--folder .artifacts/chog-body/variants] [--out .artifacts/chog-body/variants/comparison.png]

The sheet has one column per body ("now", "A", "B", "C") and four rows:

    1. purple, seen from behind at the angle of the game, large
    2. red, coming towards the camera at the angle of the game, large
    3. the four game pictures at the size of a phone, point for point (a hero is about 70 points tall)
    4. the same four, each point drawn three times as large and not smoothed: what survives the size

The pictures are mock-ups made in Blender. None of the candidates was imported in the game.
"""

import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_FOLDER = os.path.normpath(os.path.join(HERE, "..", "..", ".artifacts", "chog-body", "variants"))

COLUMNS = ("now", "A", "B", "C")
ABOUT = {
    "now": "the body in the game today",
    "A": "faithful: slim suit, pink mittens and feet",
    "B": "A pushed for small: big hands and feet, arms out",
    "C": "B with a collar and a torso in the cloth colour",
}
# colour and view of each phone picture, in the order they are laid out
PHONE_PICTURES = (("purple", "game_head"), ("purple", "game_front_head"), ("red", "game_head"),
                  ("red", "game_front_head"))
PHONE_WORDS = "purple from behind, purple from the front, red from behind, red from the front"
LARGE_ROWS = (
    ("purple", "game_head", "Angle of the game, seen from behind (purple, CHOG #1568, on the bare floor)"),
    ("red", "game_front_head", "Angle of the game, coming towards the camera (red, CHOG #1287, on the green disc "
                               "of the player's own hero)"),
)
PHONE_ZOOM = 3

BACKGROUND = (24, 22, 26)
INK = (236, 232, 226)
QUIET = (150, 146, 152)
GAP = 10
CAPTION = 30
HEADER = 64

FONTS = (
    "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
    "/System/Library/Fonts/Helvetica.ttc",
    "/Library/Fonts/Arial.ttf",
)


def font(size):
    for candidate in FONTS:
        if os.path.isfile(candidate):
            try:
                return ImageFont.truetype(candidate, size)
            except OSError:
                continue
    return ImageFont.load_default(size)


def picture(folder, column, colour, view, phone=False):
    name = f"chog_{column}_{colour}_{view}{'_phone' if phone else ''}.png"
    found = os.path.join(folder, column, name)
    if not os.path.isfile(found):
        raise SystemExit(f"CHOG_SHEET no picture at {found}: run render_chog_body_variants.py -- --variant {column}")
    return Image.open(found).convert("RGB")


def main():
    parser = argparse.ArgumentParser(description="One sheet comparing the present Chog body and the candidates.")
    parser.add_argument("--folder", default=DEFAULT_FOLDER, help="where the folders now, A, B and C are")
    parser.add_argument("--out", default=None, help="the sheet (default: comparison.png in that folder)")
    arguments = parser.parse_args()
    folder = os.path.abspath(arguments.folder)
    target = os.path.abspath(arguments.out or os.path.join(folder, "comparison.png"))

    phone = picture(folder, COLUMNS[0], *PHONE_PICTURES[0], phone=True).width
    across = 4                                   # phone pictures in a row, point for point
    column_width = across * phone + (across - 1) * 2
    zoomed = phone * PHONE_ZOOM
    zoom_row_width = 2 * zoomed + 2              # the enlarged ones go two by two
    column_width = max(column_width, zoom_row_width)
    large = column_width

    width = GAP + len(COLUMNS) * (column_width + GAP)
    height = (HEADER + len(LARGE_ROWS) * (CAPTION + large) + CAPTION + phone + CAPTION + 2 * zoomed + 2 + GAP)
    sheet = Image.new("RGB", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    title, small = font(34), font(17)

    def left_of(index):
        return GAP + index * (column_width + GAP)

    for index, column in enumerate(COLUMNS):
        draw.text((left_of(index) + 4, 8), column, font=title, fill=INK)
        draw.text((left_of(index) + 4 + draw.textlength(column, font=title) + 14, 24), ABOUT[column],
                  font=small, fill=QUIET)

    top = HEADER
    for colour, view, caption in LARGE_ROWS:
        draw.text((GAP + 4, top + 5), caption, font=small, fill=INK)
        top += CAPTION
        for index, column in enumerate(COLUMNS):
            tile = picture(folder, column, colour, view).resize((large, large), Image.LANCZOS)
            sheet.paste(tile, (left_of(index), top))
        top += large

    draw.text((GAP + 4, top + 5), f"At the size of a phone screen, point for point ({phone} points across, the hero about "
              f"70 tall): {PHONE_WORDS}", font=small, fill=INK)
    top += CAPTION
    for index, column in enumerate(COLUMNS):
        for place, (colour, view) in enumerate(PHONE_PICTURES):
            sheet.paste(picture(folder, column, colour, view, phone=True), (left_of(index) + place * (phone + 2), top))
    top += phone

    draw.text((GAP + 4, top + 5), f"The same four, each point drawn {PHONE_ZOOM} times as large and not smoothed",
              font=small, fill=INK)
    top += CAPTION
    for index, column in enumerate(COLUMNS):
        for place, (colour, view) in enumerate(PHONE_PICTURES):
            tile = picture(folder, column, colour, view, phone=True).resize((zoomed, zoomed), Image.NEAREST)
            sheet.paste(tile, (left_of(index) + (place % 2) * (zoomed + 2), top + (place // 2) * (zoomed + 2)))

    sheet.save(target, optimize=True)
    print(f"CHOG_SHEET wrote {target} ({width} x {height}, {os.path.getsize(target) // 1024} KB)")


if __name__ == "__main__":
    sys.exit(main())
