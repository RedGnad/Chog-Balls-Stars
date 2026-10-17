"""Lays the Genesis portraits on pages of a grid, for the collection screen of the game.

    python3 tools/genesis-art/make_atlas.py [--size 96]

Reads .artifacts/genesis-art/256/{metadata id}.jpg (tools/genesis-art/fetch_portraits.py) and
writes .artifacts/genesis-art/pages/{size}/{page}.jpg: page p holds metadata ids 48 p + 1 to
48 p + 48, eight to a row from the top left. The game reads the same numbers (ChogCollection in
game/Assets/Chog/Screens) and loads one page at a time, so a phone keeps a 768 by 576 picture in
memory instead of 48 portraits, or one sheet of all 1,969 (about 25 MB once decoded).
tools/release/assemble_site.py copies the pages beside the portraits.
"""

import argparse
import math
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
PORTRAITS = ROOT / ".artifacts/genesis-art/256"
OUT = ROOT / ".artifacts/genesis-art/pages"
LAST_METADATA_ID = 1969
COLUMNS = 8
ROWS = 6
PER_PAGE = COLUMNS * ROWS


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=int, default=96)
    args = parser.parse_args()

    folder = OUT / str(args.size)
    folder.mkdir(parents=True, exist_ok=True)
    pages = math.ceil(LAST_METADATA_ID / PER_PAGE)
    missing = []
    total = 0
    for page in range(pages):
        sheet = Image.new("RGB", (COLUMNS * args.size, ROWS * args.size), (20, 15, 36))
        for cell in range(PER_PAGE):
            metadata_id = page * PER_PAGE + cell + 1
            if metadata_id > LAST_METADATA_ID:
                break
            path = PORTRAITS / f"{metadata_id}.jpg"
            if not path.exists():
                missing.append(metadata_id)
                continue
            picture = Image.open(path).convert("RGB").resize((args.size, args.size), Image.LANCZOS)
            sheet.paste(picture, ((cell % COLUMNS) * args.size, (cell // COLUMNS) * args.size))
        target = folder / f"{page}.jpg"
        sheet.save(target, quality=84, optimize=True)
        total += target.stat().st_size
    print(f"{folder.relative_to(ROOT)}: {pages} pages of {COLUMNS}x{ROWS} at {args.size} px, "
          f"{total // 1024} KB in all, {len(missing)} missing {missing[:10]}")


if __name__ == "__main__":
    main()
