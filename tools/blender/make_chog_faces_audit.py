"""The audit sheets of the eyes and the mouths of the 3D heroes (21 October 2026): one row per value of
"Eyes" or "Mouth" and Side, sorted by how many Genesis wear it, with the real picture of three Genesis
beside the 3D bust framed like it, then the first of them from the game camera, at phone size, and
turned around. A verdict per row (faithful, approximate, misplaced, wrong) and what is wrong come from
verdicts.json beside the batch; the same file gives the table in Markdown. Pictures only.

Plain Python with Pillow. The batch is one of make_chog_traits_batch.py whose rows.json lists the rows
(trait, value, side, counts, the Genesis built); the first Genesis of a row has the turnaround pictures.

    .artifacts/tools-venv/bin/python tools/blender/make_chog_faces_audit.py sheet \
        --batch .artifacts/chog-body/traits/faces-audit-2026-10-21 --trait Eyes \
        --out research/evidence/heroes-faces-audit-eyes-2026-10-21.jpg
    .artifacts/tools-venv/bin/python tools/blender/make_chog_faces_audit.py table \
        --batch .artifacts/chog-body/traits/faces-audit-2026-10-21 --out research/evidence/heroes-faces-audit-2026-10-21.md
"""

import argparse
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
ART_FOLDER = os.path.join(REPOSITORY, ".artifacts", "genesis-art")

GAP = 10
BACKGROUND = (24, 22, 26)
INK = (236, 232, 226)
QUIET = (156, 152, 158)
VERDICT_COLOURS = {"faithful": (120, 210, 130), "approximate": (236, 200, 90), "misplaced": (240, 150, 90),
                   "wrong": (240, 100, 100), "": QUIET}
# The turnaround pictures shown for the first Genesis of a row: the front, the two three quarters, the
# side the picture of the Genesis shows, from above, and the game camera in front.
TURNS = (("front", "turn_000"), ("45 (its left)", "turn_045"), ("left side", "turn_090"), ("315 (its right)", "turn_315"),
         ("from above", "turn_top"), ("game, in front", "turn_game_front"))


def font(size):
    return ImageFont.load_default(size)


def opened(path):
    if not os.path.isfile(path):
        raise SystemExit(f"CHOG_AUDIT no picture at {path}")
    try:
        return Image.open(path).convert("RGB")
    except (OSError, SyntaxError) as broken:
        # A render written badly (seen twice in 2,000 on 21 October 2026): a grey square that says so, and the
        # picture is rendered again by hand.
        print(f"CHOG_AUDIT broken picture {path}: {broken}")
        missing = Image.new("RGB", (200, 200), (90, 60, 60))
        ImageDraw.Draw(missing).text((10, 90), "broken render", font=font(16), fill=INK)
        return missing


def portrait(number):
    big = os.path.join(ART_FOLDER, "1000", f"{number}.jpg")
    return big if os.path.isfile(big) else os.path.join(ART_FOLDER, "256", f"{number}.jpg")


def wrapped(text, width, draw, face):
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


def read_rows(batch, trait):
    with open(os.path.join(batch, "rows.json"), encoding="utf-8") as source:
        rows = json.load(source)
    return [row for row in rows if trait is None or row["trait"] == trait]


def read_verdicts(batch):
    path = os.path.join(batch, "verdicts.json")
    if not os.path.isfile(path):
        return {}
    with open(path, encoding="utf-8") as source:
        return json.load(source)


def key_of(row):
    return f"{row['trait']}|{row['value']}|{row['side']}"


def part_of(batch, row):
    """The part built for the trait on the first Genesis of the row: (name, triangles), or None."""
    path = os.path.join(batch, str(row["genesis"][0]), "parts.json")
    if not os.path.isfile(path):
        return None
    with open(path, encoding="utf-8") as source:
        built = json.load(source)
    for name, part in built["parts"].items():
        if part["trait"].split("=", 1)[0] == row["trait"] and not part["on"].startswith("arm"):
            return name, part["triangles"]
    return None


def sheet(arguments):
    batch = os.path.abspath(arguments.batch)
    rows = read_rows(batch, arguments.trait)
    verdicts = read_verdicts(batch)
    size, small_size, label = arguments.size, arguments.turn_size, arguments.label
    first = rows[0]["genesis"][0]
    phone = opened(os.path.join(batch, str(first), f"chog_{first}_game_front_phone.png")).width
    per_row = max(len(row["genesis"]) for row in rows)
    pairs = per_row * (2 * size + 4 + GAP)
    width = GAP + label + pairs + size + GAP + phone * 2 + GAP + len(TURNS) * (small_size + 4) + GAP
    caption = 20
    height = caption + size
    big, normal, small = font(22), font(16), font(14)
    blocks = []
    for row in rows:
        block = Image.new("RGB", (width, height), BACKGROUND)
        draw = ImageDraw.Draw(block)
        draw.text((GAP + 4, 2), f"{row['value']}", font=big, fill=INK)
        line = 30
        draw.text((GAP + 4, line), f"{row['trait']}, Side {row['side']}", font=normal, fill=INK)
        line += 20
        said = f"{row['origin']} Genesis on Origin" + (f" ({row['all']} on every base)" if row["all"] != row["origin"] else "")
        draw.text((GAP + 4, line), said, font=small, fill=QUIET)
        line += 18
        part = part_of(batch, row)
        draw.text((GAP + 4, line), f"part {part[0]}, {part[1]} triangles" if part else "NO PART BUILT", font=small,
                  fill=QUIET if part else VERDICT_COLOURS["wrong"])
        line += 22
        verdict = verdicts.get(key_of(row), {})
        word = verdict.get("verdict", "")
        draw.text((GAP + 4, line), word.upper() if word else "(not judged yet)", font=normal,
                  fill=VERDICT_COLOURS.get(word, QUIET))
        line += 20
        for piece in wrapped(verdict.get("note", ""), label - 12, draw, small):
            if line < height - 14:
                draw.text((GAP + 4, line), piece, font=small, fill=INK)
                line += 17
        left = GAP + label
        for number, head in zip(row["genesis"], row.get("head", [None] * per_row)):
            hat = f", {head}" if head else ", bare head"
            draw.text((left + 2, 2), f"CHOG #{number}{hat}: picture, 3D bust", font=small, fill=INK)
            block.paste(opened(portrait(number)).resize((size, size), Image.LANCZOS), (left, caption))
            bust = os.path.join(batch, str(number), f"chog_{number}_bust.png")
            block.paste(opened(bust).resize((size, size), Image.LANCZOS), (left + size + 4, caption))
            left += 2 * size + 4 + GAP
        number = row["genesis"][0]
        left = GAP + label + pairs
        draw.text((left + 2, 2), f"#{number}, game camera", font=small, fill=INK)
        game = os.path.join(batch, str(number), f"chog_{number}_game_front.png")
        block.paste(opened(game).resize((size, size), Image.LANCZOS), (left, caption))
        left += size + GAP
        draw.text((left + 2, 2), "phone, 2x", font=small, fill=INK)
        small_picture = opened(os.path.join(batch, str(number), f"chog_{number}_game_front_phone.png"))
        block.paste(small_picture.resize((phone * 2, phone * 2), Image.NEAREST), (left, caption + (size - phone * 2) // 2))
        left += phone * 2 + GAP
        for title, view in TURNS:
            draw.text((left + 2, 2), title, font=small, fill=INK)
            path = os.path.join(batch, str(number), f"chog_{number}_{view}.png")
            block.paste(opened(path).resize((small_size, small_size), Image.LANCZOS), (left, caption + (size - small_size) // 2))
            left += small_size + 4
        blocks.append(block)

    notes = [
        "One row per value and Side, the most worn first. Three Genesis per row: the real picture, then the 3D bust framed"
        " like it. Then the first Genesis from the game camera (73 degrees above), at phone size (each point 2x), and its"
        " head turned around: front, its left three quarter, its left side, its right three quarter, from above, the game camera.",
        "Verdicts: FAITHFUL (shape and place as drawn), APPROXIMATE (right place, shape or colour off), MISPLACED (right"
        " shape, wrong place or size), WRONG (another drawing). What is wrong is said in a few words under the verdict.",
    ]
    top = 48 + 19 * len(notes) + 10
    total = Image.new("RGB", (width, top + sum(block.height + GAP for block in blocks)), BACKGROUND)
    draw = ImageDraw.Draw(total)
    draw.text((GAP + 4, 8), arguments.title, font=font(30), fill=INK)
    place = 48
    for text in notes:
        for piece in wrapped(text, width - 2 * GAP - 8, draw, font(14)):
            draw.text((GAP + 4, place), piece, font=font(14), fill=QUIET)
            place += 19
    place = top
    for block in blocks:
        total.paste(block, (0, place))
        place += block.height + GAP
    out = os.path.abspath(arguments.out)
    if arguments.scale != 1.0:
        total = total.resize((int(total.width * arguments.scale), int(total.height * arguments.scale)), Image.LANCZOS)
    if out.lower().endswith(".jpg"):
        total.save(out, quality=arguments.quality, optimize=True, subsampling=1)
    else:
        total.save(out, optimize=True)
    print(f"CHOG_AUDIT wrote {out} ({total.width} x {total.height}, {os.path.getsize(out) // 1024} KB, {len(rows)} rows)")


def table(arguments):
    """The Markdown table of every row and its verdict, written into --out between its markers (or appended)."""
    batch = os.path.abspath(arguments.batch)
    verdicts = read_verdicts(batch)
    lines = []
    for trait in ("Eyes", "Mouth"):
        rows = read_rows(batch, trait)
        lines.append(f"### {trait} ({len(rows)} rows, sorted by Genesis count)")
        lines.append("")
        lines.append("| # | Value | Side | Genesis (Origin / all) | Part | Verdict | What is wrong |")
        lines.append("| --- | --- | --- | --- | --- | --- | --- |")
        for index, row in enumerate(rows, 1):
            part = part_of(batch, row)
            verdict = verdicts.get(key_of(row), {})
            shown = ", ".join(f"#{number}" for number in row["genesis"])
            lines.append(f"| {index} | {row['value']} | {row['side']} | {row['origin']} / {row['all']} ({shown}) | "
                         f"{part[0] if part else 'none'} | {verdict.get('verdict', '')} | {verdict.get('note', '')} |")
        lines.append("")
        counts = {}
        for row in rows:
            word = verdicts.get(key_of(row), {}).get("verdict", "not judged")
            counts[word] = counts.get(word, 0) + 1
        worn = {}
        for row in rows:
            word = verdicts.get(key_of(row), {}).get("verdict", "not judged")
            worn[word] = worn.get(word, 0) + row["origin"]
        lines.append("Rows: " + ", ".join(f"{word} {count} ({worn[word]} Genesis)" for word, count in sorted(counts.items())) + ".")
        lines.append("")
    text = "\n".join(lines)
    out = os.path.abspath(arguments.out)
    start, stop = "<!-- audit table start -->", "<!-- audit table end -->"
    if os.path.isfile(out):
        with open(out, encoding="utf-8") as source:
            existing = source.read()
        if start in existing and stop in existing:
            before, rest = existing.split(start, 1)
            _, after = rest.split(stop, 1)
            existing = f"{before}{start}\n{text}\n{stop}{after}"
        else:
            existing = f"{existing.rstrip()}\n\n{start}\n{text}\n{stop}\n"
    else:
        existing = f"{start}\n{text}\n{stop}\n"
    with open(out, "w", encoding="utf-8") as target:
        target.write(existing)
    print(f"CHOG_AUDIT wrote the table into {out}")


def main():
    parser = argparse.ArgumentParser(description="Audit sheets of the eyes and mouths of the 3D heroes.")
    parser.add_argument("mode", choices=("sheet", "table"))
    parser.add_argument("--batch", required=True)
    parser.add_argument("--trait", default=None, help="sheet: Eyes or Mouth")
    parser.add_argument("--title", default="Faces audit")
    parser.add_argument("--size", type=int, default=180)
    parser.add_argument("--turn-size", type=int, default=150)
    parser.add_argument("--label", type=int, default=330)
    parser.add_argument("--scale", type=float, default=1.0, help="sheet: shrink the whole sheet by this factor")
    parser.add_argument("--quality", type=int, default=78, help="sheet: JPEG quality")
    parser.add_argument("--out", required=True)
    arguments = parser.parse_args()
    if arguments.mode == "sheet":
        sheet(arguments)
    else:
        table(arguments)


if __name__ == "__main__":
    sys.exit(main())
