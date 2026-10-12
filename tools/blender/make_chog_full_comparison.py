"""Lays the three complete Chog characters side by side on one sheet, with the body of the game and
candidate B under their flat picture for comparison, from the pictures of render_chog_full.py and
render_chog_body_variants.py.

It is plain Python with Pillow, not Blender. Run it with the private environment of the tools:

    .artifacts/tools-venv/bin/python tools/blender/make_chog_full_comparison.py
        [--out .artifacts/chog-body/full/comparison.png]

One column per character: "H1", "H2", "H3" (make_chog_full.py), "now" (the body in the game today)
and "B" (candidate B of make_chog_body_variants.py), the last two with the flat picture of a Genesis
as their head, as the game shows a hero today. Rows, in purple unless said:

    1. from the front            4. the angle of the game, coming towards the camera
    2. three quarter             5. the angle of the game, seen slightly from behind
    3. from behind
    6. the game pictures at the size of a phone, point for point: purple towards the camera, purple
       from behind, red towards the camera, red from behind
    7. the same four, each point drawn twice as large and not smoothed
    8. H1, H2, H3 only: the face of a Genesis laid on the head (CHOG #1795 and CHOG #408), a mock-up

The pictures are mock-ups made in Blender. Nothing here was imported in the game.
"""

import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
BODY_FOLDER = os.path.normpath(os.path.join(HERE, "..", "..", ".artifacts", "chog-body"))

# column: (folder, start of the names, words under the title)
COLUMNS = {
    "H1": ("full/H1", "chog_H1", "flat blades in one plane"),
    "H2": ("full/H2", "chog_H2", "round spikes, top and back"),
    "H3": ("full/H3", "chog_H3", "for the game camera"),
    "now": ("variants/now", "chog_now", "in the game today, flat picture"),
    "B": ("variants/B", "chog_B", "slim body, flat picture"),
}
FULL = ("H1", "H2", "H3")
# row caption, then the end of the name of the picture for a complete character and for a body
# under a flat picture
LARGE_ROWS = (
    ("From the front", "front", "front_head"),
    ("Three quarter", "three_quarter", "three_quarter_head"),
    ("From behind (the flat picture of now and B always turns to the camera)", "back", "back_head"),
    ("Angle of the game, 73 degrees above the horizon, coming towards the camera", "game_front", "game_front_head"),
    ("Angle of the game, seen slightly from behind", "game", "game_head"),
)
PHONE_PICTURES = (("purple", "game_front", "game_front_head"), ("purple", "game", "game_head"),
                  ("red", "game_front", "game_front_head"), ("red", "game", "game_head"))
FACES = ("1795", "408")
LARGE_COLOUR = "purple"
PHONE_ZOOM = 2
# Points of a phone screen for one unit of the model: what render_chog_full.py frames its phone
# pictures at (126 points for 1.80 units), against what the game draws the present body at
# (46, measured on a phone screenshot, see render_chog_body_variants.py).
SCALE_NEEDED = (126.0 / 1.80) / 46.0

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


def picture(column, colour, end):
    folder, start, _ = COLUMNS[column]
    found = os.path.join(BODY_FOLDER, folder, f"{start}_{colour}_{end}.png")
    if not os.path.isfile(found):
        raise SystemExit(f"CHOG_SHEET no picture at {found}")
    return Image.open(found).convert("RGB")


def main():
    parser = argparse.ArgumentParser(description="One sheet comparing the complete Chog characters.")
    parser.add_argument("--out", default=os.path.join(BODY_FOLDER, "full", "comparison.png"))
    arguments = parser.parse_args()
    target = os.path.abspath(arguments.out)

    phone = picture("H1", PHONE_PICTURES[0][0], PHONE_PICTURES[0][1] + "_phone").width
    column_width = 4 * phone
    zoomed = phone * PHONE_ZOOM
    half = column_width // 2

    names = list(COLUMNS)
    width = GAP + len(names) * (column_width + GAP)
    height = (HEADER + len(LARGE_ROWS) * (CAPTION + column_width) + CAPTION + phone + CAPTION + 2 * zoomed
              + CAPTION + half + GAP)
    sheet = Image.new("RGB", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    title, small = font(34), font(16)

    def left_of(index):
        return GAP + index * (column_width + GAP)

    for index, column in enumerate(names):
        draw.text((left_of(index) + 4, 8), column, font=title, fill=INK)
        draw.text((left_of(index) + 4 + draw.textlength(column, font=title) + 12, 25), COLUMNS[column][2],
                  font=small, fill=QUIET)

    top = HEADER
    for caption, full_end, flat_end in LARGE_ROWS:
        draw.text((GAP + 4, top + 6), caption, font=small, fill=INK)
        top += CAPTION
        for index, column in enumerate(names):
            tile = picture(column, LARGE_COLOUR, full_end if column in FULL else flat_end)
            sheet.paste(tile.resize((column_width, column_width), Image.LANCZOS), (left_of(index), top))
        top += column_width

    draw.text((GAP + 4, top + 6), "At the size of a phone screen, point for point: purple towards the camera, purple "
              "from behind, red towards the camera, red from behind. now and B are at the scale the game draws a body "
              f"today; H1 to H3 are drawn {SCALE_NEEDED:.1f} times larger than that, which makes them 48 to 76 points "
              "tall", font=small, fill=INK)
    top += CAPTION

    def phone_tile(column, colour, full_end, flat_end, zoom):
        """A phone picture at "zoom" times its own size, and how far in it sits in a cell of the
        largest one: the pictures of now and B are smaller than those of H1 to H3."""
        tile = picture(column, colour, (full_end if column in FULL else flat_end) + "_phone")
        inset = (phone - tile.width) // 2 * zoom
        if zoom > 1:
            tile = tile.resize((tile.width * zoom, tile.height * zoom), Image.NEAREST)
        return tile, inset

    for index, column in enumerate(names):
        for place, (colour, full_end, flat_end) in enumerate(PHONE_PICTURES):
            tile, inset = phone_tile(column, colour, full_end, flat_end, 1)
            sheet.paste(tile, (left_of(index) + place * phone + inset, top + inset))
    top += phone

    draw.text((GAP + 4, top + 6), f"The same four, each point drawn {PHONE_ZOOM} times as large and not smoothed",
              font=small, fill=INK)
    top += CAPTION
    for index, column in enumerate(names):
        for place, (colour, full_end, flat_end) in enumerate(PHONE_PICTURES):
            tile, inset = phone_tile(column, colour, full_end, flat_end, PHONE_ZOOM)
            sheet.paste(tile, (left_of(index) + (place % 2) * zoomed + inset, top + (place // 2) * zoomed + inset))
    top += 2 * zoomed

    draw.text((GAP + 4, top + 6), "Mock-up: the face of a Genesis laid on the cream face of the head, in place of the "
              "drawn eyes and cheeks (CHOG #1795, then CHOG #408)", font=small, fill=INK)
    top += CAPTION
    for index, column in enumerate(names):
        if column not in FULL:
            draw.text((left_of(index) + 8, top + half // 2 - 10), "the flat picture is the head", font=small,
                      fill=QUIET)
            continue
        for place, token in enumerate(FACES):
            tile = picture(column, f"face{token}", "front").resize((half, half), Image.LANCZOS)
            sheet.paste(tile, (left_of(index) + place * half, top))

    sheet.save(target, optimize=True)
    print(f"CHOG_SHEET wrote {target} ({width} x {height}, {os.path.getsize(target) // 1024} KB)")


if __name__ == "__main__":
    sys.exit(main())
