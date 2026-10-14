"""Lays the pictures of one dressed Chog on one sheet for the owner: its NFT picture beside the bust
of the 3D hero, the whole character from five sides, the game angle at the size of a phone, and the
polished base beside head H3 as it was chosen. From the pictures of render_chog_traits.py and
render_chog_full.py.

It is plain Python with Pillow, not Blender. Run it with the private environment of the tools:

    .artifacts/tools-venv/bin/python tools/blender/make_chog_traits_sheet.py --genesis 1287 [--with-base]
        [--out .artifacts/chog-body/traits/1287/sheet_1287.png]

Rows:

    1. the picture of the Genesis and the bust of the 3D hero, the same size: what is judged
    2. the whole character: front, three quarter, back, the angle of the game coming towards the
       camera, the angle of the game seen slightly from behind
    3. the two game angles at the size of a phone, point for point, then each point three times as
       large and not smoothed; and, when it was rendered, a check that the hat shape takes another
       colour set and word (traits/<number>_<Hat>/)
    4. with --with-base: head H3 as chosen (.artifacts/chog-body/full/H3/, red), with its generic eyes
       and mouth
    5. with --with-base: the polished base alone (traits/base/), which has no eyes and no mouth: they
       are trait parts

The words on the sheet are drawn with the font that comes inside Pillow, no file of this Mac. The
pictures are mock-ups made in Blender. Nothing here was imported in the game.
"""

import argparse
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
BODY_FOLDER = os.path.join(REPOSITORY, ".artifacts", "chog-body")
ART_FOLDER = os.path.join(REPOSITORY, ".artifacts", "genesis-art")

VIEWS = (
    ("front", "From the front"),
    ("three_quarter", "Three quarter"),
    ("back", "From behind"),
    ("game_front", "Game angle (73 degrees above), coming towards the camera"),
    ("game", "Game angle, seen slightly from behind"),
)
PHONE_VIEWS = ("game_front", "game")
PHONE_ZOOM = 3
# Points of a phone screen for one unit of the model in the phone pictures (126 points for 1.80
# units, render_chog_full.py) against what the game draws a body at today (46).
SCALE_NEEDED = (126.0 / 1.80) / 46.0

TILE = 400
GAP = 10
CAPTION = 30
HEADER = 78
BACKGROUND = (24, 22, 26)
INK = (236, 232, 226)
QUIET = (156, 152, 158)


def font(size):
    return ImageFont.load_default(size)


def opened(path):
    if not os.path.isfile(path):
        raise SystemExit(f"CHOG_SHEET no picture at {path}")
    return Image.open(path).convert("RGB")


def main():
    parser = argparse.ArgumentParser(description="One sheet for one dressed Chog.")
    parser.add_argument("--genesis", required=True, help="the number shown in the name of the Genesis")
    parser.add_argument("--with-base", action="store_true", help="add the two rows that set the polished base "
                        "beside head H3 as it was chosen")
    parser.add_argument("--out", default=None)
    arguments = parser.parse_args()
    number = arguments.genesis
    folder = os.path.join(BODY_FOLDER, "traits", number)
    target = os.path.abspath(arguments.out or os.path.join(folder, f"sheet_{number}.png"))

    portrait = os.path.join(ART_FOLDER, "1000", f"{number}.jpg")
    if not os.path.isfile(portrait):
        portrait = os.path.join(ART_FOLDER, "256", f"{number}.jpg")

    width = GAP + len(VIEWS) * (TILE + GAP)
    pair = (width - 3 * GAP) // 2
    phone = opened(os.path.join(folder, f"chog_{number}_{PHONE_VIEWS[0]}_phone.png")).width
    zoomed = phone * PHONE_ZOOM
    height = HEADER + CAPTION + pair + CAPTION + TILE + CAPTION + zoomed + GAP
    if arguments.with_base:
        height += 2 * (CAPTION + TILE)
    sheet = Image.new("RGB", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    title, normal, small = font(34), font(17), font(15)

    draw.text((GAP + 4, 8), f"CHOG #{number} in 3D: trait parts on the polished H3 base", font=title, fill=INK)
    # What make_chog_traits.py says it built: the traits it read, the parts, the triangles.
    with open(os.path.join(folder, "parts.json"), encoding="utf-8") as source:
        built = json.load(source)
    worn = ", ".join(f"{name} ({part['trait'].replace('=', ' ')}, {part['triangles']})"
                     for name, part in built["parts"].items() if not part.get("on", "").startswith("arm"))
    under = (f"Skin {built['traits'].get('Skin', '?')}  |  {worn}  |  {built['triangles_total']} triangles with the "
             f"outline shells, of which {built['triangles_base']} for the base  |  mock-ups made in Blender, nothing "
             "here is in the game")
    if built["left_out"]:
        under += "  |  NOT MODELLED: " + ", ".join(built["left_out"])
    draw.text((GAP + 4, 50), under, font=small, fill=QUIET)

    def left_of(index):
        return GAP + index * (TILE + GAP)

    # 1. The pair that is judged.
    top = HEADER
    draw.text((GAP + 4, top + 6), f"The picture of CHOG #{number} ({os.path.basename(os.path.dirname(portrait))} points)",
              font=normal, fill=INK)
    draw.text((2 * GAP + pair + 4, top + 6), "The 3D hero: same side, same tilt, flat background, lit from the front "
              "too. Geometry and flat colours only, no image", font=normal, fill=INK)
    top += CAPTION
    sheet.paste(opened(portrait).resize((pair, pair), Image.LANCZOS), (GAP, top))
    sheet.paste(opened(os.path.join(folder, f"chog_{number}_bust.png")).resize((pair, pair), Image.LANCZOS),
                (2 * GAP + pair, top))
    top += pair

    # 2. The whole character.
    for index, (_, caption) in enumerate(VIEWS):
        draw.text((left_of(index) + 4, top + 7), caption, font=small, fill=INK)
    top += CAPTION
    for index, (view, _) in enumerate(VIEWS):
        tile = opened(os.path.join(folder, f"chog_{number}_{view}.png"))
        sheet.paste(tile.resize((TILE, TILE), Image.LANCZOS), (left_of(index), top))
    top += TILE

    # 3. The size of a phone, and the check of the hat shape.
    draw.text((GAP + 4, top + 7), f"The two game angles at the size of a phone screen, point for point, then each point "
              f"{PHONE_ZOOM} times as large. They assume the game draws the model {SCALE_NEEDED:.1f} times larger than "
              "it draws a body today", font=small, fill=INK)
    top += CAPTION
    place = GAP
    for view in PHONE_VIEWS:
        tile = opened(os.path.join(folder, f"chog_{number}_{view}_phone.png"))
        sheet.paste(tile, (place, top + (zoomed - phone) // 2))
        place += phone + GAP
    for view in PHONE_VIEWS:
        tile = opened(os.path.join(folder, f"chog_{number}_{view}_phone.png"))
        sheet.paste(tile.resize((zoomed, zoomed), Image.NEAREST), (place, top))
        place += zoomed + GAP
    checks = sorted(name for name in os.listdir(os.path.dirname(folder))
                    if name.startswith(f"{number}_") and os.path.isfile(
                        os.path.join(os.path.dirname(folder), name, f"chog_{name}_bust.png")))
    if checks:
        place += 3 * GAP
        words = ("Check of the system, not finished art:", "the same hat shape under another",
                 "colour set and word (" + ", ".join(name.split("_", 1)[1] for name in checks) + ")")
        for line, text in enumerate(words):
            draw.text((place + len(checks) * (zoomed + GAP) + 4, top + 8 + line * 22), text, font=small, fill=QUIET)
        for name in checks:
            tile = opened(os.path.join(os.path.dirname(folder), name, f"chog_{name}_bust.png"))
            sheet.paste(tile.resize((zoomed, zoomed), Image.LANCZOS), (place, top))
            place += zoomed + GAP
    top += zoomed

    # 4 and 5. The base before and after.
    rows = (
        ("Head H3 as it was chosen, with its generic eyes and mouth (faceted blades)",
         os.path.join(BODY_FOLDER, "full", "H3"), "chog_H3_red"),
        ("The polished base alone, no trait part: the blades are leaves, the cream face is wider; no eyes and no "
         "mouth of its own, a Genesis brings them as parts", os.path.join(BODY_FOLDER, "traits", "base"), "chog_base"),
    )
    for caption, where, start in rows if arguments.with_base else ():
        draw.text((GAP + 4, top + 7), caption, font=small, fill=INK)
        top += CAPTION
        for index, (view, _) in enumerate(VIEWS):
            tile = opened(os.path.join(where, f"{start}_{view}.png"))
            sheet.paste(tile.resize((TILE, TILE), Image.LANCZOS), (left_of(index), top))
        top += TILE

    sheet.save(target, optimize=True)
    print(f"CHOG_SHEET wrote {target} ({width} x {height}, {os.path.getsize(target) // 1024} KB)")


if __name__ == "__main__":
    sys.exit(main())
