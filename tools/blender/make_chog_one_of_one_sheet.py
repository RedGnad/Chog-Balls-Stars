"""One before/after sheet of the ten 1:1 Genesis, each beside its picture at 1000 points (23 October 2026). Plain Python, no Blender: it reads the pictures that
make_chog_traits_many.py rendered into two folders (before and after a change), as make_chog_faces_before_after.py does, but the art comes from a folder of
your choice, here the 1000 point pictures (not kept in the repository: the addresses are in .artifacts/genesis-art/manifest.jsonl, "source", a .webp each).

    .artifacts/tools-venv/bin/python tools/blender/make_chog_one_of_one_sheet.py \
        --before .artifacts/chog-body/traits/costumes-before --after .artifacts/chog-body/traits/costumes-a \
        --art-folder /path/to/1000 --out research/evidence/heroes-one-of-one-2026-10-23.jpg

One row per Genesis: its art, then for the bust framed like the art, the hero stage (three quarter, close), the side of the stage, the back three
quarter and the game camera in front, the picture before and the picture after side by side.
"""

import argparse
import os

from PIL import Image, ImageDraw, ImageFont

NUMBERS = (53, 392, 434, 494, 973, 1006, 1159, 1290, 1373, 1376)
VIEWS = (("bust", "bust"), ("hero stage", "pedestal"), ("stage side", "pedestal_side"), ("back 3/4", "pedestal_back34"),
         ("game camera", "game_front"))
NAMES = {53: "Flame", 392: "Skull in flames", 434: "The Scream", 494: "Zombie", 973: "Pepe", 1006: "Manga page", 1159: "Angel",
         1290: "Warrior", 1373: "Gold", 1376: "Devil"}


def font(size):
    for path in ("/System/Library/Fonts/Helvetica.ttc", "/System/Library/Fonts/Supplemental/Arial.ttf"):
        if os.path.isfile(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def main():
    parser = argparse.ArgumentParser(description="One before/after sheet of the ten 1:1 Genesis.")
    parser.add_argument("--before", required=True)
    parser.add_argument("--after", required=True)
    parser.add_argument("--art-folder", required=True, help="the folder of the pictures, <number>.jpg each")
    parser.add_argument("--out", required=True)
    parser.add_argument("--size", type=int, default=190, help="side of one picture")
    parser.add_argument("--quality", type=int, default=78)
    arguments = parser.parse_args()
    size, label = arguments.size, 110
    art_size = size + 40
    width, head = label + art_size + len(VIEWS) * 2 * size, 40
    sheet = Image.new("RGB", (width, head + len(NUMBERS) * art_size), (245, 243, 238))
    draw = ImageDraw.Draw(sheet)
    draw.text((6, 4), "The ten 1:1, 23 October 2026: the art at 1000 points, then before | after for each view", font=font(15), fill=(20, 20, 20))
    draw.text((label, 22), "art (1000 px)", font=font(12), fill=(20, 20, 20))
    for index, (caption, _) in enumerate(VIEWS):
        left = label + art_size + 2 * size * index
        draw.text((left + 2, 22), f"{caption}: before", font=font(12), fill=(20, 20, 20))
        draw.text((left + size + 2, 22), "after", font=font(12), fill=(20, 20, 20))
    for row, number in enumerate(NUMBERS):
        top = head + row * art_size
        draw.text((6, top + 6), f"CHOG #{number}", font=font(14), fill=(20, 20, 20))
        draw.text((6, top + 26), NAMES[number], font=font(12), fill=(60, 60, 60))
        art = Image.open(os.path.join(arguments.art_folder, f"{number}.jpg")).convert("RGB").resize((art_size, art_size), Image.LANCZOS)
        sheet.paste(art, (label, top))
        for index, (_, name) in enumerate(VIEWS):
            for shift, folder in enumerate((arguments.before, arguments.after)):
                path = os.path.join(folder, str(number), f"chog_{number}_{name}.png")
                picture = Image.open(path).convert("RGB").resize((size, size), Image.LANCZOS)
                sheet.paste(picture, (label + art_size + 2 * size * index + size * shift, top + (art_size - size) // 2))
    os.makedirs(os.path.dirname(os.path.abspath(arguments.out)), exist_ok=True)
    sheet.save(arguments.out, quality=arguments.quality, optimize=True)
    print(f"{arguments.out}: {sheet.size[0]}x{sheet.size[1]}, {os.path.getsize(arguments.out) / 1e6:.2f} MB")


if __name__ == "__main__":
    main()
