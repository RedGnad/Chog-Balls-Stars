"""One before/after sheet of heroes as the game's library holds them, seen on the home stage (22 October 2026). Plain Python,
no Blender: it reads pictures that render_recruit.py made (hero N of a library, folders <before>/hN and <after>/hN).

    .artifacts/tools-venv/bin/python tools/blender/make_chog_stage_sheet.py \
        --before .artifacts/chog-body/face-fill/before --after .artifacts/chog-body/face-fill/after \
        --heroes 0,75,76,15,23,85,143 --cheek h75big --out research/evidence/heroes-face-fill-stage-2026-10-22.jpg

One row per hero: its art (none for the recruit, number 0), then before | after for the whole hero on the stage (a camera of
24 degrees, 10 degrees above the horizon, the hero turned 42 degrees), the same camera close on the head, the bust framed like the art,
the side and the game camera at the size of a phone. With --cheek NAME, a strip at the bottom sets a close crop of a cheek
(from the large stage picture NAME in both folders) before | after, enlarged, to show any speck.
"""

import argparse
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
ART = os.path.join(REPOSITORY, ".artifacts", "genesis-art", "256")

VIEWS = (("stage", "stage"), ("stage close", "stage_close"), ("bust", "bust"), ("side", "turn_090"),
         ("game camera", "game_front_phone"))


def font(size):
    for path in ("/System/Library/Fonts/Helvetica.ttc", "/System/Library/Fonts/Supplemental/Arial.ttf"):
        if os.path.isfile(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def main():
    parser = argparse.ArgumentParser(description="One before/after sheet of heroes on the home stage.")
    parser.add_argument("--before", required=True)
    parser.add_argument("--after", required=True)
    parser.add_argument("--heroes", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--size", type=int, default=150)
    parser.add_argument("--cheek", default=None, help="name of a large stage picture (chog_<name>_stage_close.png)")
    parser.add_argument("--cheek-box", default="0.22,0.45,0.62,0.78", help="the crop of that picture, as shares of its side")
    parser.add_argument("--title", default="Face on the home stage: the art, then before | after")
    arguments = parser.parse_args()
    size, label = arguments.size, 110
    heroes = [word.strip() for word in arguments.heroes.split(",") if word.strip()]
    columns = 1 + 2 * len(VIEWS)
    width, head = label + columns * size, 40
    strip = 0
    if arguments.cheek:
        strip = int(size * 3.2)
    sheet = Image.new("RGB", (width, head + len(heroes) * size + strip), (245, 243, 238))
    draw = ImageDraw.Draw(sheet)
    draw.text((6, 4), arguments.title, font=font(15), fill=(20, 20, 20))
    draw.text((label, 22), "art", font=font(12), fill=(20, 20, 20))
    for index, (caption, _) in enumerate(VIEWS):
        left = label + size * (1 + 2 * index)
        draw.text((left + 2, 22), f"{caption}: before", font=font(12), fill=(20, 20, 20))
        draw.text((left + size + 2, 22), "after", font=font(12), fill=(20, 20, 20))
    for row, hero in enumerate(heroes):
        top = head + row * size
        draw.text((6, top + 6), "recruit" if hero == "0" else f"CHOG #{hero}", font=font(14), fill=(20, 20, 20))
        if hero != "0":
            sheet.paste(Image.open(os.path.join(ART, f"{hero}.jpg")).convert("RGB").resize((size, size), Image.LANCZOS),
                        (label, top))
        for index, (_, name) in enumerate(VIEWS):
            for shift, folder in enumerate((arguments.before, arguments.after)):
                picture = Image.open(os.path.join(folder, f"h{hero}", f"chog_h{hero}_{name}.png")).convert("RGB")
                sheet.paste(picture.resize((size, size), Image.LANCZOS), (label + size * (1 + 2 * index + shift), top))
    if arguments.cheek:
        top = head + len(heroes) * size
        draw.text((6, top + 4), "A cheek, close on the stage picture, enlarged: before | after", font=font(13), fill=(20, 20, 20))
        box = [float(word) for word in arguments.cheek_box.split(",")]
        for shift, folder in enumerate((arguments.before, arguments.after)):
            picture = Image.open(os.path.join(folder, arguments.cheek, f"chog_{arguments.cheek}_stage_close.png")).convert("RGB")
            w = picture.size[0]
            crop = picture.crop(tuple(int(value * w) for value in box))
            height = strip - 24
            crop = crop.resize((int(crop.size[0] * height / crop.size[1]), height), Image.LANCZOS)
            sheet.paste(crop, (label + shift * (crop.size[0] + 10), top + 22))
    os.makedirs(os.path.dirname(os.path.abspath(arguments.out)), exist_ok=True)
    sheet.save(arguments.out, quality=82, optimize=True)
    print(f"{arguments.out}: {sheet.size[0]}x{sheet.size[1]}, {os.path.getsize(arguments.out) / 1e6:.2f} MB")


if __name__ == "__main__":
    main()
