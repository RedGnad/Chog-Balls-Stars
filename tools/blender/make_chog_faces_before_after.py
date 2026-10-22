"""One before/after sheet of the faces of dressed Genesis, the real art beside each hero (22 October 2026). Plain
Python, no Blender: it only reads pictures that make_chog_traits_batch.py rendered (families bust, turnaround and
towards) into two folders, one built with the code before a change and one after it.

    .artifacts/tools-venv/bin/python tools/blender/make_chog_faces_before_after.py \
        --before .artifacts/chog-body/traits/heroes-face-before --after .artifacts/chog-body/traits/heroes-face-after-3 \
        --genesis 23,85,16 --out research/evidence/heroes-face-fill-2026-10-22.jpg

One row per Genesis: its art, then for the bust framed like the art, the front, the three quarter turn, the side
and the game camera at the size of a phone, the picture before and the picture after, side by side. The last
column pair is the hero seen from above, as the game camera sees it. The sheet is a JPEG made small on purpose.
"""

import argparse
import json
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
METADATA = os.path.join(REPOSITORY, "research", "evidence", "genesis-metadata.jsonl")
ART = os.path.join(REPOSITORY, ".artifacts", "genesis-art", "256")

# (caption, picture name, drawn small or at the size of a phone)
VIEWS = (("bust", "bust"), ("front", "turn_000"), ("three quarter", "turn_045"), ("side", "turn_090"),
         ("game camera", "game_front_phone"))


def traits_of(number):
    with open(METADATA, encoding="utf-8") as source:
        for line in source:
            row = json.loads(line)
            if row["metadata_id"] == int(number):
                return {item["trait_type"]: item["value"] for item in json.loads(row["raw_json"])["attributes"]}
    return {}


def font(size):
    for path in ("/System/Library/Fonts/Helvetica.ttc", "/System/Library/Fonts/Supplemental/Arial.ttf"):
        if os.path.isfile(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def main():
    parser = argparse.ArgumentParser(description="One before/after sheet of the faces of dressed Genesis.")
    parser.add_argument("--before", required=True)
    parser.add_argument("--after", required=True)
    parser.add_argument("--genesis", required=True, help="numbers shown in the names, separated by commas")
    parser.add_argument("--out", required=True)
    parser.add_argument("--size", type=int, default=164, help="side of one picture")
    parser.add_argument("--quality", type=int, default=78)
    arguments = parser.parse_args()
    size, label = arguments.size, 150
    numbers = [word.strip() for word in arguments.genesis.split(",") if word.strip()]
    columns = 1 + 2 * len(VIEWS)
    width, head = label + columns * size, 40
    sheet = Image.new("RGB", (width, head + len(numbers) * size), (245, 243, 238))
    draw = ImageDraw.Draw(sheet)
    draw.text((6, 4), "Face fill: the art, then before | after for each view", font=font(15), fill=(20, 20, 20))
    draw.text((label, 22), "art", font=font(12), fill=(20, 20, 20))
    for index, (caption, _) in enumerate(VIEWS):
        left = label + size * (1 + 2 * index)
        draw.text((left + 2, 22), f"{caption}: before", font=font(12), fill=(20, 20, 20))
        draw.text((left + size + 2, 22), "after", font=font(12), fill=(20, 20, 20))
    for row, number in enumerate(numbers):
        top = head + row * size
        traits = traits_of(number)
        for line, text in enumerate((f"CHOG #{number}", f"Side {traits.get('Side', '?')}", f"Body: {traits.get('Body')}",
                                     f"Head: {traits.get('Head')}")):
            draw.text((6, top + 6 + line * 17), text, font=font(13 if line else 15), fill=(20, 20, 20))
        sheet.paste(Image.open(os.path.join(ART, f"{number}.jpg")).convert("RGB").resize((size, size), Image.LANCZOS),
                    (label, top))
        for index, (_, name) in enumerate(VIEWS):
            for shift, folder in enumerate((arguments.before, arguments.after)):
                path = os.path.join(folder, number, f"chog_{number}_{name}.png")
                picture = Image.open(path).convert("RGB")
                sheet.paste(picture.resize((size, size), Image.LANCZOS), (label + size * (1 + 2 * index + shift), top))
    os.makedirs(os.path.dirname(os.path.abspath(arguments.out)), exist_ok=True)
    sheet.save(arguments.out, quality=arguments.quality, optimize=True)
    print(f"{arguments.out}: {sheet.size[0]}x{sheet.size[1]}, {os.path.getsize(arguments.out) / 1e6:.2f} MB")


if __name__ == "__main__":
    main()
