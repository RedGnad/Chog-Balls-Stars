#!/usr/bin/env python3
"""Cut each Chog Genesis out of its background, from the finished images.

The collection's layer files are not available here, only the finished images. But the
collection is made of layers: the Genesis that look the same way (Side), are of the same kind
(Base: Origin, Skull, Radioactive, 1:1) and have the same Background value share one background
picture, and those with the same Aura value share one pattern drawn over it. So both are rebuilt
from the collection itself (the colour most pictures of a group agree on, point by point), and
a Genesis is then cut out by difference: what matches its rebuilt background and is connected
to the edges of the picture is background, the rest is the Chog. A Genesis whose group is too
small to rebuild anything (a 1:1, most Skulls) is cut by the colours of its own edges instead.

Usage, with the Python of the private environment (numpy and Pillow):
  .artifacts/tools-venv/bin/python tools/genesis-art/cut_out.py learn
      rebuild the background of every Background value and the pattern of every Aura value
  ... cut_out.py layers <out.png>
      lay the rebuilt layers out to be looked at
  ... cut_out.py sheet <out.png> <id> [<id> ...]
      cut these Genesis and lay them out to be looked at
  ... cut_out.py all
      write, for every Genesis whose cut can be trusted, what to keep of its portrait
      (.artifacts/genesis-art/cut/<id>.png, white is kept) and the list of those (index.json)

Ids are the numbers shown in the names ("CHOG #n"), which are also the names of the image
files. Everything reads the 256-pixel portraits written by fetch_portraits.py.
The environment is made once with:
  python3 -m venv .artifacts/tools-venv && .artifacts/tools-venv/bin/python -m pip install numpy pillow
"""
import json
import re
import sys
from collections import deque
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
METADATA = ROOT / "research" / "evidence" / "genesis-metadata.jsonl"
PORTRAITS = ROOT / ".artifacts" / "genesis-art" / "256"
LAYERS = ROOT / ".artifacts" / "genesis-art" / "layers"
CUT = ROOT / ".artifacts" / "genesis-art" / "cut"
SIZE = 256

# Two colours are the same when no channel differs by more than this: the pictures are JPEG
# copies of grainy drawings.
SAME = 34
# A point of a background is known when this share of the pictures of that value agree on it.
BACKGROUND_KNOWN = 0.30
# A point belongs to an Aura's pattern when this share of the pictures with that Aura agree on
# its colour, and at most this share of the pictures without any Aura show that colour there.
AURA_AGREES = 0.55
AURA_ELSEWHERE = 0.30
# A layer is rebuilt from a group of at least this many pictures.
ENOUGH_PICTURES = 6
# A point whose rebuilt colour is the same for this share of the other backgrounds of the group
# is not background at all: it is the character itself (its face, drawn at the same place in
# every picture), and must never be cut away.
SHARED_BY_OTHERS = 0.5
# With the colours of its own edges: a colour counts as background from this share of the edge.
OWN_EDGE_SHARE = 0.12
# A Chog fills about five sixths of its picture. A cut that keeps more than this share has most
# likely missed the background; one that keeps less than the least has eaten into the Chog.
TOO_MUCH_KEPT = 0.93
LEAST_KEPT = 0.6
# A loose piece smaller than this share of the picture is a leftover, not part of the Chog.
SMALLEST_PIECE = 0.012
# The groups whose cuts were looked at on sheets drawn at random and found good enough.
TRUSTED_GROUPS = {"right-origin"}


def traits_by_id():
    traits = {}
    for line in METADATA.read_text(encoding="utf-8").splitlines():
        if line.strip():
            record = json.loads(line)
            attributes = json.loads(record["raw_json"]).get("attributes", [])
            traits[int(record["metadata_id"])] = {item["trait_type"]: item["value"] for item in attributes}
    return traits


def portrait(metadata_id):
    return np.asarray(Image.open(PORTRAITS / f"{metadata_id}.jpg").convert("RGB"), dtype=np.uint8)


def slug(name):
    return re.sub(r"[^a-z0-9]+", "-", name.lower()).strip("-")


def group(own):
    """Which way a Genesis looks and what kind it is: pictures of one group are laid out alike."""
    return slug(own.get("Side", "none") + " " + own.get("Base", "none"))


def difference(pictures, layer):
    """Largest difference of any channel between pictures and a layer, point by point."""
    return np.abs(pictures.astype(np.int16) - layer.astype(np.int16)).max(axis=-1)


def learn():
    traits = traits_by_id()
    ids = sorted(metadata_id for metadata_id in traits if (PORTRAITS / f"{metadata_id}.jpg").is_file())
    pictures = np.stack([portrait(metadata_id) for metadata_id in ids])
    row = {metadata_id: index for index, metadata_id in enumerate(ids)}
    LAYERS.mkdir(parents=True, exist_ok=True)

    for old in LAYERS.glob("*.png"):
        old.unlink()

    def rows(of_group, trait, value, without_aura=False):
        return [row[metadata_id] for metadata_id in ids
                if group(traits[metadata_id]) == of_group and traits[metadata_id].get(trait) == value
                and not (without_aura and "Aura" in traits[metadata_id])]

    groups = sorted({group(own) for own in traits.values()})
    summary = {"backgrounds": {}, "auras": {}, "too small": {"backgrounds": 0, "auras": 0}}
    for of_group in groups:
        rebuilt = {}
        for value in sorted({own.get("Background", "") for own in traits.values() if group(own) == of_group} - {""}):
            chosen = rows(of_group, "Background", value, without_aura=True)
            if len(chosen) < ENOUGH_PICTURES:
                chosen = rows(of_group, "Background", value)
            if len(chosen) < ENOUGH_PICTURES:
                summary["too small"]["backgrounds"] += len(chosen)
                continue
            stack = pictures[chosen]
            layer = np.median(stack, axis=0).astype(np.uint8)
            rebuilt[value] = (layer, (difference(stack, layer) <= SAME).mean(axis=0), len(chosen))
        for value, (layer, known, count) in rebuilt.items():
            others = [other for name, (other, _, _) in rebuilt.items() if name != value]
            if len(others) >= 2:
                shared = np.mean([difference(layer, other) <= SAME for other in others], axis=0)
                known = np.where(shared >= SHARED_BY_OTHERS, 0.0, known)
            name = f"background-{of_group}-{slug(value)}"
            Image.fromarray(layer).save(LAYERS / f"{name}.png")
            Image.fromarray((known * 255).astype(np.uint8)).save(LAYERS / f"{name}-known.png")
            summary["backgrounds"][f"{of_group} / {value}"] = {"pictures": count, "known": round(float((known >= BACKGROUND_KNOWN).mean()), 2)}

        plain = [row[metadata_id] for metadata_id in ids if group(traits[metadata_id]) == of_group and "Aura" not in traits[metadata_id]]
        for value in sorted({own.get("Aura", "") for own in traits.values() if group(own) == of_group} - {""}):
            chosen = rows(of_group, "Aura", value)
            if len(chosen) < ENOUGH_PICTURES or len(plain) < ENOUGH_PICTURES:
                summary["too small"]["auras"] += len(chosen)
                continue
            stack = pictures[chosen]
            layer = np.median(stack, axis=0).astype(np.uint8)
            agrees = (difference(stack, layer) <= SAME).mean(axis=0)
            # The same colour at the same point in pictures without an Aura is the character or
            # a background, not this pattern.
            elsewhere = (difference(pictures[plain], layer) <= SAME).mean(axis=0)
            pattern = (agrees >= AURA_AGREES) & (elsewhere <= AURA_ELSEWHERE)
            name = f"aura-{of_group}-{slug(value)}"
            Image.fromarray(layer).save(LAYERS / f"{name}.png")
            Image.fromarray((pattern * 255).astype(np.uint8)).save(LAYERS / f"{name}-pattern.png")
            summary["auras"][f"{of_group} / {value}"] = {"pictures": len(chosen), "pattern": round(float(pattern.mean()), 2)}

    (LAYERS / "summary.json").write_text(json.dumps(summary, indent=1), encoding="utf-8")
    print(f"{LAYERS.relative_to(ROOT)}: {len(summary['backgrounds'])} backgrounds and {len(summary['auras'])} aura patterns rebuilt; "
          f"in groups too small: {summary['too small']['backgrounds']} Genesis for the background, {summary['too small']['auras']} for the aura")


def layers_of(own):
    """The rebuilt background of a Genesis with its Aura's pattern over it, and where it is known."""
    name = f"background-{group(own)}-{slug(own.get('Background', ''))}"
    if not (LAYERS / f"{name}.png").is_file():
        return None, None
    expected = np.asarray(Image.open(LAYERS / f"{name}.png").convert("RGB"), dtype=np.uint8).copy()
    known = np.asarray(Image.open(LAYERS / f"{name}-known.png"), dtype=np.uint8) >= BACKGROUND_KNOWN * 255
    aura = f"aura-{group(own)}-{slug(own.get('Aura', ''))}"
    if "Aura" in own and (LAYERS / f"{aura}.png").is_file():
        pattern = np.asarray(Image.open(LAYERS / f"{aura}-pattern.png"), dtype=np.uint8) > 127
        expected[pattern] = np.asarray(Image.open(LAYERS / f"{aura}.png").convert("RGB"), dtype=np.uint8)[pattern]
        known = known | pattern
    return expected, known


def own_edge_matches(picture):
    """Where a picture shows one of the colours that fill its own sides: its likely background."""
    height, width, _ = picture.shape
    upper = slice(2, int(height * 0.7))
    edge = np.concatenate([picture[upper, 1], picture[upper, 4], picture[upper, width - 2], picture[upper, width - 5]])
    edge = edge[edge.max(axis=1) >= 70]
    if len(edge) == 0:
        return np.zeros((height, width), bool)
    buckets, counts = np.unique(edge // 8, axis=0, return_counts=True)
    matches = np.zeros((height, width), bool)
    for index in np.argsort(-counts)[:3]:
        if counts[index] / len(edge) >= OWN_EDGE_SHARE:
            matches |= difference(picture, buckets[index].astype(np.int16) * 8 + 4) <= SAME + 10
    return matches


def pieces(mask):
    """The connected pieces of a mask, as lists of points."""
    seen = np.zeros_like(mask, dtype=bool)
    found = []
    height, width = mask.shape
    for start_y, start_x in zip(*np.nonzero(mask)):
        if seen[start_y, start_x]:
            continue
        piece, queue = [], deque([(start_y, start_x)])
        seen[start_y, start_x] = True
        while queue:
            y, x = queue.popleft()
            piece.append((y, x))
            for near_y, near_x in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= near_y < height and 0 <= near_x < width and mask[near_y, near_x] and not seen[near_y, near_x]:
                    seen[near_y, near_x] = True
                    queue.append((near_y, near_x))
        found.append(piece)
    return found


def cut(picture, own):
    """
    The picture with its background made see-through, the share of it that was kept, and how
    it was cut: "layers" (by its rebuilt background) or "edges" (by the colours of its own edges).
    """
    expected, known = layers_of(own)
    kept, how = None, "edges"
    if expected is not None:
        matches = (difference(picture, expected) <= SAME) & known
        kept, how = keep(matches), "layers"
        # What was found is mostly one flat colour: the same colour elsewhere along the top and
        # the sides is background too, where the rebuilt layer knew nothing (behind the hats
        # of most pictures of the group).
        found = ~kept & matches
        if found.mean() >= 0.04:
            flat = np.median(picture[found], axis=0)
            wider = keep(matches | (difference(picture, flat) <= SAME))
            if wider.mean() >= LEAST_KEPT:
                kept = wider
    if kept is None or kept.mean() > TOO_MUCH_KEPT:
        other = keep(own_edge_matches(picture))
        if kept is None or LEAST_KEPT <= other.mean() < kept.mean():
            kept, how = other, "edges"
    mask = Image.fromarray((kept * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6))
    result = Image.fromarray(picture).convert("RGBA")
    result.putalpha(mask)
    return result, float(kept.mean()), how


def keep(matches):
    """What is left of a picture once the background that reaches its edges is taken away."""
    # The background reaches the sides and the top of the picture. The bottom edge is never a
    # way in: clothes of the background's own colour sit there.
    reached = np.zeros_like(matches)
    queue = deque()
    height, width = matches.shape

    def offer(y, x):
        if matches[y, x] and not reached[y, x]:
            reached[y, x] = True
            queue.append((y, x))

    for y in range(int(height * 0.85)):
        offer(y, 0)
        offer(y, width - 1)
    for x in range(width):
        offer(0, x)
    while queue:
        y, x = queue.popleft()
        if y > 0:
            offer(y - 1, x)
        if y < height - 1:
            offer(y + 1, x)
        if x > 0:
            offer(y, x - 1)
        if x < width - 1:
            offer(y, x + 1)

    kept = ~reached
    # Loose leftovers of a pattern go; the Chog and what it carries stay.
    smallest = SMALLEST_PIECE * height * width
    for piece in pieces(kept):
        if len(piece) < smallest:
            for y, x in piece:
                kept[y, x] = False
    return kept


def sheet(output, ids):
    traits = traits_by_id()
    cell = SIZE
    columns = min(8, len(ids))
    count_rows = (len(ids) + columns - 1) // columns
    page = Image.new("RGB", (columns * cell, count_rows * cell), (40, 44, 60))
    draw = ImageDraw.Draw(page)
    for index, metadata_id in enumerate(ids):
        result, kept, how = cut(portrait(metadata_id), traits[metadata_id])
        spot = ((index % columns) * cell, (index // columns) * cell)
        # Squares of two loud colours behind each cut-out: whatever was cut away shows as
        # squares, also a pale face that a plain light ground would hide.
        for square_y in range(0, cell, 16):
            for square_x in range(0, cell, 16):
                if (square_x + square_y) // 16 % 2:
                    page.paste((255, 0, 255), (spot[0] + square_x, spot[1] + square_y, spot[0] + square_x + 16, spot[1] + square_y + 16))
        page.paste(result, spot, result)
        draw.text((spot[0] + 4, spot[1] + 4), f"#{metadata_id} {kept:.0%} {how}", fill=(255, 255, 0))
    page.save(output)
    print(output)


def layers_sheet(output):
    files = sorted(LAYERS.glob("background-*.png")) + sorted(LAYERS.glob("aura-*.png"))
    files = [file for file in files if not file.stem.endswith(("-known", "-pattern"))]
    cell, columns = 128, 11
    count_rows = (len(files) + columns - 1) // columns
    page = Image.new("RGB", (columns * cell, count_rows * cell), (40, 44, 60))
    draw = ImageDraw.Draw(page)
    for index, file in enumerate(files):
        layer = Image.open(file).convert("RGB")
        extra = LAYERS / (file.stem + ("-known.png" if file.stem.startswith("background-") else "-pattern.png"))
        # What is not known of a background, or not part of an Aura's pattern, is darkened.
        shown = Image.composite(layer, layer.point(lambda value: value // 4), Image.open(extra).point(lambda value: 255 if value >= BACKGROUND_KNOWN * 255 else 0))
        spot = ((index % columns) * cell, (index // columns) * cell)
        page.paste(shown.resize((cell, cell)), spot)
        draw.text((spot[0] + 3, spot[1] + 3), file.stem.replace("background-", "bg ").replace("aura-", "aura ")[:20], fill=(255, 255, 0))
    page.save(output)
    print(output)


def all_of_them():
    traits = traits_by_id()
    CUT.mkdir(parents=True, exist_ok=True)
    for old in CUT.glob("*.png"):
        old.unlink()
    # Only the groups whose cuts can be trusted are cut at all; the other Genesis keep their
    # round picture until the method covers them.
    ids = [metadata_id for metadata_id in sorted(traits)
           if group(traits[metadata_id]) in TRUSTED_GROUPS and (PORTRAITS / f"{metadata_id}.jpg").is_file()]
    with ProcessPoolExecutor() as workers:
        outcomes = list(workers.map(cut_for_the_game, ids, [traits[metadata_id] for metadata_id in ids], chunksize=16))
    trusted = [metadata_id for metadata_id, kept in zip(ids, outcomes) if kept]
    (CUT / "index.json").write_text(json.dumps({"ids": trusted}), encoding="utf-8")
    print(f"{CUT.relative_to(ROOT)}: {len(trusted)} Genesis cut and given to the game, "
          f"{len(traits) - len(trusted)} left with their round picture ({len(ids) - len(trusted)} of them in a trusted group)")


def cut_for_the_game(metadata_id, own):
    """Writes what to keep of one portrait when its cut can be trusted without being looked at."""
    result, kept, how = cut(portrait(metadata_id), own)
    # Cut by its rebuilt background, and filling a usual share of its picture.
    if how != "layers" or not LEAST_KEPT <= kept <= TOO_MUCH_KEPT:
        return False
    # The game already has the colours (the portrait): it only needs what to keep.
    result.getchannel("A").save(CUT / f"{metadata_id}.png", optimize=True)
    return True


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    if sys.argv[1] == "learn":
        learn()
    elif sys.argv[1] == "layers":
        layers_sheet(sys.argv[2])
    elif sys.argv[1] == "sheet":
        sheet(sys.argv[2], [int(value) for value in sys.argv[3:]])
    elif sys.argv[1] == "all":
        all_of_them()
    else:
        sys.exit(__doc__)


if __name__ == "__main__":
    main()
