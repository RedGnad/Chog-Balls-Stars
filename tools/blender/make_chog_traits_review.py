"""Review sheets of a batch of dressed Genesis (make_chog_traits_batch.py): the real picture of each
Genesis beside its 3D hero, so the owner can judge what a trait part looks like. Pictures only:
nothing here goes into the game.

Plain Python with Pillow. Run it with the private environment of the tools:

    # one row per Genesis: picture, bust framed like it, game camera, phone size (1x and 2x)
    .artifacts/tools-venv/bin/python tools/blender/make_chog_traits_review.py rows \
        --batch .artifacts/chog-body/traits/batch-2 --trait Head --genesis 97,103 \
        --title "Hats" --out .artifacts/chog-body/traits/batch-2/hats_sheet.png
    # a grid of cells, one per Genesis: picture, bust, game camera (for clothing)
    ... grid --batch ... --trait Body --genesis 10,62 --title "Clothing" --out .../clothes_sheet.png
    # the Genesis drawn at random among those on the base Origin, then their sheet
    ... pick --count 12 --seed 20261015
    ... rows --batch ... --random 12 --seed 20261015 --missing --out .../random_twelve.png
    # before and after: the bust and the game camera of an earlier batch beside those of this one
    ... compare --batch .../batch-3 --before .../batch-2 --genesis 97,19 --out .../spikes_before_after.png
    # several sheets laid one under the other
    ... stack --sheets a.png,b.png --title "Prints" --out .../prints_sheet.png
    # one row per value of "Head" and Side (batch 4): what its pictures show, picture and bust of each
    # Genesis given, the game camera of the first; 12 rows per sheet: <out>_1.png, <out>_2.png...
    ... values --batch .../batch-4 --genesis 3,991,1946,5,678,1936 --size 200 --label 320 \
        --rows-per-sheet 12 --out .../per_value.png [--art-only "BOOB Cap|Right|846,1079|skull heads"]
    # the head turned all around (render_chog_traits.py --only bust,turnaround): picture, bust, eight
    # turns, from above, the game behind and in front
    ... turnaround --batch .../batch-5 --genesis 75,97,4 --size 200 --label 220 --out .../turnaround.png

A sheet taller than --max-height (4,000 points) is cut into parts: <name>_part1.png, <name>_part2.png.
The captions are read from parts.json, written by make_chog_traits.py: the part built for the trait,
its triangles, the triangles of the whole hero, and what was left out. The words are drawn with the
font that comes inside Pillow.
"""

import argparse
import json
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
ART_FOLDER = os.path.join(REPOSITORY, ".artifacts", "genesis-art")
METADATA = os.path.join(REPOSITORY, "research", "evidence", "genesis-metadata.jsonl")

GAP = 10
BACKGROUND = (24, 22, 26)
INK = (236, 232, 226)
QUIET = (156, 152, 158)
WARN = (240, 150, 120)
# Traits of a Genesis that no part of make_chog_traits.py draws at all.
NEVER_MODELLED = ("Accessory", "Aura")


def font(size):
    return ImageFont.load_default(size)


def opened(path):
    if not os.path.isfile(path):
        raise SystemExit(f"CHOG_REVIEW no picture at {path}")
    return Image.open(path).convert("RGB")


def portrait(number):
    big = os.path.join(ART_FOLDER, "1000", f"{number}.jpg")
    return big if os.path.isfile(big) else os.path.join(ART_FOLDER, "256", f"{number}.jpg")


def read_built(batch, number):
    with open(os.path.join(batch, number, "parts.json"), encoding="utf-8") as source:
        return json.load(source)


def origin_numbers():
    """The numbers of every Genesis on the base Origin, in order."""
    numbers = []
    with open(METADATA, encoding="utf-8") as lines:
        for line in lines:
            if line.strip():
                record = json.loads(line)
                attributes = json.loads(record["raw_json"]).get("attributes", [])
                if any(item["trait_type"] == "Base" and item["value"] == "Origin" for item in attributes):
                    numbers.append(int(record["metadata_id"]))
    return sorted(numbers)


def drawn_at_random(count, seed):
    return [str(number) for number in random.Random(seed).sample(origin_numbers(), count)]


def part_for(built, trait):
    """The part built for a trait of this Genesis, as (name, triangles, what its print lacks), or None."""
    for name, part in built["parts"].items():
        if part["trait"].split("=", 1)[0] == trait and not part["on"].startswith("arm"):
            triangles = sum(other["triangles"] for other_name, other in built["parts"].items()
                            if other["trait"] == part["trait"])
            return name, triangles, part.get("missing", "")
    return None


def missing_lines(built):
    """What of this Genesis is not in 3D: values with no part, prints not drawn, traits never modelled,
    and traits its hat covers (not built, as the pictures do not show them)."""
    lines = [f"no part: {value}" for value in built["left_out"]]
    lines += [f"under the hat, not built: {value}" for value in built.get("hidden", [])]
    for name, part in built["parts"].items():
        if part.get("missing") and not part["on"].startswith("arm"):
            lines.append(f"not drawn on {name}: {part['missing']}")
    for trait in NEVER_MODELLED:
        if trait in built["traits"]:
            lines.append(f"no part: {trait}={built['traits'][trait]} (never modelled)")
    return lines


def wrapped(text, width, draw, face):
    """Cuts a text into lines no wider than "width" points."""
    lines, line = [], ""
    for word in text.split(" "):
        tried = f"{line} {word}".strip()
        if draw.textlength(tried, font=face) <= width or not line:
            line = tried
        else:
            lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines


def draw_notes(draw, notes, width):
    """The lines under the title, each cut to the width of the sheet. Returns where they end."""
    place = 48
    for text in notes:
        for piece in wrapped(text, width - 2 * GAP - 8, draw, font(14)):
            draw.text((GAP + 4, place), piece, font=font(14), fill=QUIET)
            place += 19
    return place


def save_parts(blocks, width, header, out, max_height):
    """Lays blocks (pictures of the same width, laid one under the other) on one sheet or on several
    parts under max_height points. "header(draw, part, parts)" draws the top and returns its height."""
    parts, current, height = [], [], 0
    probe = Image.new("RGB", (width, 10))
    top = header(ImageDraw.Draw(probe), 1, 1)
    for block in blocks:
        if current and top + height + block.height + GAP > max_height:
            parts.append(current)
            current, height = [], 0
        current.append(block)
        height += block.height + GAP
    if current:
        parts.append(current)
    written = []
    stem, extension = os.path.splitext(out)
    for index, chosen in enumerate(parts):
        top_height = header(ImageDraw.Draw(probe), index + 1, len(parts))
        sheet = Image.new("RGB", (width, top_height + sum(block.height + GAP for block in chosen)), BACKGROUND)
        header(ImageDraw.Draw(sheet), index + 1, len(parts))
        place = top_height
        for block in chosen:
            sheet.paste(block, (0, place))
            place += block.height + GAP
        target = out if len(parts) == 1 else f"{stem}_part{index + 1}{extension}"
        sheet.save(target, optimize=True)
        written.append(target)
        print(f"CHOG_REVIEW wrote {target} ({sheet.width} x {sheet.height}, {os.path.getsize(target) // 1024} KB)")
    return written


def rows_sheet(arguments, numbers):
    batch = os.path.abspath(arguments.batch)
    size, label = arguments.size, arguments.label
    phone = opened(os.path.join(batch, numbers[0], f"chog_{numbers[0]}_game_front_phone.png")).width
    columns = (("The picture of the Genesis", size), ("The 3D hero, framed like it", size),
               ("Game camera (73 degrees above)", size), ("Phone size", phone), ("Phone size, each point 2x", phone * 2))
    width = GAP + label + sum(column + GAP for _, column in columns)
    big, normal, small = font(26), font(16), font(14)
    blocks = []
    for number in numbers:
        built = read_built(batch, number)
        traits = built["traits"]
        block = Image.new("RGB", (width, size), BACKGROUND)
        draw = ImageDraw.Draw(block)
        draw.text((GAP + 4, 2), f"CHOG #{number}", font=big, fill=INK)
        line = 36
        said = f"Side {traits.get('Side', '?')}, skin {traits.get('Skin', '?')}"
        draw.text((GAP + 4, line), said, font=small, fill=QUIET)
        line += 20
        shown = arguments.trait.split(",") if arguments.trait else ("Head", "Eyes", "Mouth", "Body")
        for trait in shown:
            value = traits.get(trait)
            if value is None:
                draw.text((GAP + 4, line), f"{trait}: none", font=normal, fill=INK)
                line += 20
                continue
            found = part_for(built, trait)
            draw.text((GAP + 4, line), f"{trait}: {value}", font=normal, fill=INK)
            line += 19
            if found:
                name, triangles, _ = found
                draw.text((GAP + 16, line), f"{name}, {triangles} triangles", font=small, fill=QUIET)
            else:
                draw.text((GAP + 16, line), "NOT MODELLED", font=small, fill=WARN)
            line += 19
        if arguments.missing:
            line += 4
            for text in missing_lines(built):
                for piece in wrapped(text, label - 10, draw, small):
                    if line < size - 40:
                        draw.text((GAP + 4, line), piece, font=small, fill=WARN)
                        line += 17
        draw.text((GAP + 4, size - 20), f"whole hero {built['triangles_total']} triangles", font=small, fill=QUIET)
        left = GAP + label
        for path in (portrait(number), os.path.join(batch, number, f"chog_{number}_bust.png"),
                     os.path.join(batch, number, f"chog_{number}_game_front.png")):
            block.paste(opened(path).resize((size, size), Image.LANCZOS), (left, 0))
            left += size + GAP
        small_picture = opened(os.path.join(batch, number, f"chog_{number}_game_front_phone.png"))
        block.paste(small_picture, (left, (size - phone) // 2))
        left += phone + GAP
        block.paste(small_picture.resize((phone * 2, phone * 2), Image.NEAREST), (left, (size - phone * 2) // 2))
        blocks.append(block)

    def header(draw, part, parts):
        title = arguments.title + (f" (part {part} of {parts})" if parts > 1 else "")
        draw.text((GAP + 4, 8), title, font=font(30), fill=INK)
        place = draw_notes(draw, arguments.note, width)
        left = GAP + label
        for caption, column in columns:
            draw.text((left + 2, place + 6), caption, font=font(14), fill=INK)
            left += column + GAP
        return place + 30

    return save_parts(blocks, width, header, os.path.abspath(arguments.out), arguments.max_height)


def grid_sheet(arguments, numbers):
    batch = os.path.abspath(arguments.batch)
    size, per_row = arguments.size, arguments.per_row
    views = arguments.views.split(",")
    cell_width = (1 + len(views)) * size + len(views) * 4
    caption = 44
    width = GAP + per_row * (cell_width + GAP)
    normal, small = font(15), font(13)
    trait = arguments.trait or "Body"
    blocks = []
    for start in range(0, len(numbers), per_row):
        block = Image.new("RGB", (width, caption + size), BACKGROUND)
        draw = ImageDraw.Draw(block)
        for column, number in enumerate(numbers[start:start + per_row]):
            built = read_built(batch, number)
            left = GAP + column * (cell_width + GAP)
            value = built["traits"].get(trait, "none")
            found = part_for(built, trait)
            draw.text((left + 2, 2), f"#{number}  {value}", font=normal, fill=INK)
            if found:
                name, triangles, missing = found
                note = f"{name}, {triangles} triangles" + (f"; not drawn: {missing}" if missing else "")
                draw.text((left + 2, 22), wrapped(note, cell_width - 4, draw, small)[0], font=small,
                          fill=WARN if missing else QUIET)
            else:
                draw.text((left + 2, 22), "NOT MODELLED", font=small, fill=WARN)
            paths = [portrait(number)] + [os.path.join(batch, number, f"chog_{number}_{view}.png") for view in views]
            for index, path in enumerate(paths):
                block.paste(opened(path).resize((size, size), Image.LANCZOS), (left + index * (size + 4), caption))
        blocks.append(block)

    def header(draw, part, parts):
        title = arguments.title + (f" (part {part} of {parts})" if parts > 1 else "")
        draw.text((GAP + 4, 8), title, font=font(30), fill=INK)
        return draw_notes(draw, arguments.note, width) + 8

    return save_parts(blocks, width, header, os.path.abspath(arguments.out), arguments.max_height)


def compare_sheet(arguments, numbers):
    """One row per Genesis: its picture, then the bust and the game camera of an earlier batch
    (--before) and of this one, then the phone size of this one (added 15 October 2026)."""
    batch, before = os.path.abspath(arguments.batch), os.path.abspath(arguments.before)
    size, label = arguments.size, arguments.label
    phone = opened(os.path.join(batch, numbers[0], f"chog_{numbers[0]}_game_front_phone.png")).width
    columns = (("The picture of the Genesis", size), ("Before: bust", size), ("After: bust", size),
               ("Before: game camera", size), ("After: game camera", size), ("After, phone size 2x", phone * 2))
    width = GAP + label + sum(column + GAP for _, column in columns)
    big, normal, small = font(26), font(16), font(14)
    blocks = []
    for number in numbers:
        built, earlier = read_built(batch, number), read_built(before, number)
        traits = built["traits"]
        block = Image.new("RGB", (width, size), BACKGROUND)
        draw = ImageDraw.Draw(block)
        draw.text((GAP + 4, 2), f"CHOG #{number}", font=big, fill=INK)
        line = 36
        draw.text((GAP + 4, line), f"Side {traits.get('Side', '?')}, skin {traits.get('Skin', '?')}", font=small,
                  fill=QUIET)
        line += 22
        draw.text((GAP + 4, line), f"Head: {traits.get('Head', 'none')}", font=normal, fill=INK)
        line += 22
        kept = sum(1 for item in built.get("leaves", []) if item[0] > 0.0)
        for text in wrapped(f"leaves kept: {kept} lines of 7 (" + ", ".join(
                f"{index}" + ("" if item[1] is None and item[2] is None else " moved")
                for index, item in enumerate(built.get("leaves", [])) if item[0] > 0.0) + ")", label - 10, draw, small):
            draw.text((GAP + 4, line), text, font=small, fill=QUIET)
            line += 17
        draw.text((GAP + 4, size - 40), f"before {earlier['triangles_total']} triangles", font=small, fill=QUIET)
        draw.text((GAP + 4, size - 20), f"after {built['triangles_total']} triangles", font=small, fill=QUIET)
        left = GAP + label
        for path in (portrait(number), os.path.join(before, number, f"chog_{number}_bust.png"),
                     os.path.join(batch, number, f"chog_{number}_bust.png"),
                     os.path.join(before, number, f"chog_{number}_game_front.png"),
                     os.path.join(batch, number, f"chog_{number}_game_front.png")):
            block.paste(opened(path).resize((size, size), Image.LANCZOS), (left, 0))
            left += size + GAP
        small_picture = opened(os.path.join(batch, number, f"chog_{number}_game_front_phone.png"))
        block.paste(small_picture.resize((phone * 2, phone * 2), Image.NEAREST), (left, (size - phone * 2) // 2))
        blocks.append(block)

    def header(draw, part, parts):
        title = arguments.title + (f" (part {part} of {parts})" if parts > 1 else "")
        draw.text((GAP + 4, 8), title, font=font(30), fill=INK)
        place = draw_notes(draw, arguments.note, width)
        left = GAP + label
        for caption, column in columns:
            draw.text((left + 2, place + 6), caption, font=font(14), fill=INK)
            left += column + GAP
        return place + 30

    return save_parts(blocks, width, header, os.path.abspath(arguments.out), arguments.max_height)


TURN_COLUMNS = (("front", "turn_000"), ("45", "turn_045"), ("left side", "turn_090"), ("135", "turn_135"),
                ("back", "turn_180"), ("225", "turn_225"), ("right side", "turn_270"), ("315", "turn_315"),
                ("from above", "turn_top"), ("game, behind", "turn_game"), ("game, in front", "turn_game_front"))


def turnaround_sheet(arguments, numbers):
    """One row per Genesis: its picture, the bust framed like it, then the head turned all around it
    (render_chog_traits.py --only turnaround): the sides and the back the pictures never show, the top,
    and the two angles of the game (added 15 October 2026)."""
    batch = os.path.abspath(arguments.batch)
    size, label = arguments.size, arguments.label
    columns = [("The picture", "picture"), ("Bust", "bust")] + list(TURN_COLUMNS)
    width = GAP + label + len(columns) * (size + GAP)
    big, normal, small = font(24), font(16), font(14)
    blocks = []
    for number in numbers:
        built = read_built(batch, number)
        traits = built["traits"]
        block = Image.new("RGB", (width, size), BACKGROUND)
        draw = ImageDraw.Draw(block)
        draw.text((GAP + 4, 2), f"CHOG #{number}", font=big, fill=INK)
        line = 34
        for text in (f"Side {traits.get('Side', '?')}, skin {traits.get('Skin', '?')}",
                     f"Head: {traits.get('Head', 'none')}", f"Body: {traits.get('Body', 'none')}"):
            draw.text((GAP + 4, line), text, font=normal if text.startswith("Head") else small, fill=INK)
            line += 21
        draw.text((GAP + 4, size - 20), f"{built['triangles_total']} triangles", font=small, fill=QUIET)
        left = GAP + label
        for _, view in columns:
            if view == "picture":
                path = portrait(number)
            else:
                path = os.path.join(batch, number, f"chog_{number}_{view}.png")
            block.paste(opened(path).resize((size, size), Image.LANCZOS), (left, 0))
            left += size + GAP
        blocks.append(block)

    def header(draw, part, parts):
        title = arguments.title + (f" (part {part} of {parts})" if parts > 1 else "")
        draw.text((GAP + 4, 8), title, font=font(30), fill=INK)
        place = draw_notes(draw, arguments.note, width)
        left = GAP + label
        for caption, _ in columns:
            draw.text((left + 2, place + 6), caption, font=font(14), fill=INK)
            left += size + GAP
        return place + 30

    return save_parts(blocks, width, header, os.path.abspath(arguments.out), arguments.max_height)


def values_sheet(arguments, numbers):
    """One row per value of "Head" and Side (batch 4): what its pictures show (parts.json,
    "head_from_the_art", from HEAD_VALUES of chog_trait_hats.py), then for each of its Genesis given the
    real picture beside the 3D bust, then the game camera of the first. The Genesis are grouped by the
    value and Side read in their parts.json, in the order given. --art-only adds rows of values that are
    not built (VALUE|SIDE|n,n,n|what the pictures show): their pictures only."""
    batch = os.path.abspath(arguments.batch)
    size, label = arguments.size, arguments.label
    groups = []
    for number in numbers:
        built = read_built(batch, number)
        key = (built["traits"].get("Head", "no Head"), built["traits"].get("Side", "?"))
        for group in groups:
            if group[0] == key:
                group[1].append((number, built))
                break
        else:
            groups.append((key, [(number, built)]))
    for item in arguments.art_only:
        value, side, chosen, note = item.split("|", 3)
        groups.append(((value, side), [(number, None) for number in chosen.split(",")], note))
    # What the pictures show for a group with no line in HEAD_VALUES (the bare heads): --value-note.
    notes = {}
    for item in arguments.value_note:
        value, side, text = item.split("|", 2)
        notes[(value, side)] = text
    per_row = max(len(group[1]) for group in groups)
    caption = 22
    width = GAP + label + per_row * (2 * size + 4 + GAP) + size + GAP
    big, normal, small = font(22), font(15), font(14)
    blocks = []
    for group in groups:
        (value, side), members = group[0], group[1]
        block = Image.new("RGB", (width, caption + size), BACKGROUND)
        draw = ImageDraw.Draw(block)
        draw.text((GAP + 4, 2), value, font=big, fill=INK)
        line = 30
        draw.text((GAP + 4, line), f"Side {side}", font=small, fill=QUIET)
        line += 18
        first = members[0][1]
        seen = (first or {}).get("head_from_the_art", {}) if first else {}
        note = group[2] if len(group) > 2 else seen.get("art", "") or notes.get((value, side), "")
        looked = seen.get("looked_at") or [int(number) for number, _ in members]
        texts = [f"art looked at: " + ", ".join(f"#{number}" for number in looked)]
        texts.append(f"art: {note}" if note else "art: (no line in HEAD_VALUES)")
        if first:
            kept = sum(1 for leaf in first.get("leaves", []) if leaf[0] > 0.0)
            texts.append(f"built: {kept} of 7 lines of leaves kept; hero #{members[0][0]} "
                         f"{first['triangles_total']} triangles")
        else:
            texts.append("not built: the builder makes the Origin head only")
        for text in texts:
            for piece in wrapped(text, label - 14, draw, small):
                if line < caption + size - 16:
                    draw.text((GAP + 4, line), piece, font=small, fill=QUIET if not text.startswith("art:") else INK)
                    line += 17
            line += 3
        left = GAP + label
        for number, built in members:
            draw.text((left + 2, 2), f"CHOG #{number}: the picture, then the 3D bust" if built else
                      f"CHOG #{number}", font=small, fill=INK)
            block.paste(opened(portrait(number)).resize((size, size), Image.LANCZOS), (left, caption))
            if built:
                bust = os.path.join(batch, number, f"chog_{number}_bust.png")
                block.paste(opened(bust).resize((size, size), Image.LANCZOS), (left + size + 4, caption))
            left += 2 * size + 4 + GAP
        if first:
            number = members[0][0]
            left = GAP + label + per_row * (2 * size + 4 + GAP)
            draw.text((left + 2, 2), f"#{number}, game camera", font=small, fill=INK)
            game = os.path.join(batch, number, f"chog_{number}_game_front.png")
            block.paste(opened(game).resize((size, size), Image.LANCZOS), (left, caption))
        blocks.append(block)

    def header(draw, part, parts):
        title = arguments.title + (f" (part {part} of {parts})" if parts > 1 else "")
        draw.text((GAP + 4, 8), title, font=font(30), fill=INK)
        return draw_notes(draw, arguments.note, width) + 8

    if arguments.rows_per_sheet:
        # Sheets of a fixed number of rows, named <out>_1.png, <out>_2.png... (per_value_N.png).
        stem, extension = os.path.splitext(os.path.abspath(arguments.out))
        written = []
        for index in range(0, len(blocks), arguments.rows_per_sheet):
            target = f"{stem}_{index // arguments.rows_per_sheet + 1}{extension}"
            written += save_parts(blocks[index:index + arguments.rows_per_sheet], width, header, target, 100000)
        return written
    return save_parts(blocks, width, header, os.path.abspath(arguments.out), arguments.max_height)


def stack_sheet(arguments):
    """Sheets laid one under the other (--sheets a.png,b.png), cut into parts under --max-height."""
    pictures = [opened(path) for path in arguments.sheets.split(",")]
    width = max(picture.width for picture in pictures)
    blocks = []
    for picture in pictures:
        block = Image.new("RGB", (width, picture.height), BACKGROUND)
        block.paste(picture, (0, 0))
        blocks.append(block)

    def header(draw, part, parts):
        title = arguments.title + (f" (part {part} of {parts})" if parts > 1 else "")
        draw.text((GAP + 4, 8), title, font=font(30), fill=INK)
        return draw_notes(draw, arguments.note, width) + 8

    return save_parts(blocks, width, header, os.path.abspath(arguments.out), arguments.max_height)


def main():
    parser = argparse.ArgumentParser(description="Review sheets of a batch of dressed Genesis.")
    parser.add_argument("mode", choices=("rows", "grid", "pick", "compare", "stack", "values", "turnaround"))
    parser.add_argument("--art-only", action="append", default=[], help="values: a row of pictures only, "
                        "VALUE|SIDE|n,n,n|what the pictures show (may be repeated)")
    parser.add_argument("--rows-per-sheet", type=int, default=0, help="values: this many rows per sheet, the "
                        "sheets named <out>_1, <out>_2...")
    parser.add_argument("--value-note", action="append", default=[], help="values: what the pictures show for a "
                        "value with no line in HEAD_VALUES (the bare heads), VALUE|SIDE|text (may be repeated)")
    parser.add_argument("--before", default=None, help="compare: the folder of the earlier batch")
    parser.add_argument("--sheets", default="", help="stack: the sheets to lay one under the other, separated by commas")
    parser.add_argument("--batch", default=None, help="the folder of make_chog_traits_batch.py")
    parser.add_argument("--genesis", default="", help="numbers shown in the names, separated by commas")
    parser.add_argument("--random", type=int, default=0, help="this many Genesis drawn at random on the base Origin")
    parser.add_argument("--count", type=int, default=12, help="pick: how many to draw")
    parser.add_argument("--seed", type=int, default=20261015)
    parser.add_argument("--trait", default=None, help="the traits captioned, separated by commas")
    parser.add_argument("--missing", action="store_true", help="list under each hero what of it is not in 3D")
    parser.add_argument("--title", default="Dressed Genesis")
    parser.add_argument("--note", action="append", default=[], help="a line under the title (may be repeated)")
    parser.add_argument("--size", type=int, default=300, help="width and height of a picture")
    parser.add_argument("--label", type=int, default=300, help="width of the caption column (rows)")
    parser.add_argument("--per-row", type=int, default=3, help="cells per row (grid)")
    parser.add_argument("--views", default="bust,game_front", help="grid: the pictures of render_chog_traits.py "
                        "laid after the real picture in each cell, for instance bust,front,game_front")
    parser.add_argument("--max-height", type=int, default=4000)
    parser.add_argument("--out", default=None)
    arguments = parser.parse_args()

    if arguments.mode == "pick":
        print(",".join(drawn_at_random(arguments.count, arguments.seed)))
        return
    if arguments.mode == "stack":
        if not arguments.sheets or not arguments.out:
            raise SystemExit("CHOG_REVIEW stack: give --sheets and --out")
        stack_sheet(arguments)
        return
    numbers = [number for number in arguments.genesis.split(",") if number]
    if arguments.random:
        numbers = drawn_at_random(arguments.random, arguments.seed)
    only_pictures = arguments.mode == "values" and arguments.art_only
    if (not numbers and not only_pictures) or not arguments.batch or not arguments.out:
        raise SystemExit("CHOG_REVIEW give --batch, --out and --genesis (or --random)")
    if arguments.mode == "rows":
        rows_sheet(arguments, numbers)
    elif arguments.mode == "compare":
        if not arguments.before:
            raise SystemExit("CHOG_REVIEW compare: give --before")
        compare_sheet(arguments, numbers)
    elif arguments.mode == "values":
        values_sheet(arguments, numbers)
    elif arguments.mode == "turnaround":
        turnaround_sheet(arguments, numbers)
    else:
        grid_sheet(arguments, numbers)


if __name__ == "__main__":
    sys.exit(main())
