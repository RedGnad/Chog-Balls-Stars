#!/usr/bin/env python3
"""Download the Genesis images named by the recorded metadata and write small JPEG portraits.

Read-only towards the network. Output goes to .artifacts/genesis-art/, which is NOT under
version control: the rights to redistribute the collection's art are not established, so these
copies are for local tests until the Chog team answers. Files are named by METADATA id (the
number in "CHOG #n"), like the images themselves; the game maps a token to it.

Unity cannot decode WebP, which is the format of the originals, hence the JPEG copies.
"""
import argparse
import hashlib
import io
import json
import time
import urllib.error
import urllib.request
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
GATEWAYS = [
    # The marketplace's IPFS mirror first: same bytes as the public gateways, far less throttled.
    "https://ipfs2.seadn.io/ipfs/{path}",
    "https://ipfs.filebase.io/ipfs/{path}",
    "https://gateway.pinata.cloud/ipfs/{path}",
    "https://dweb.link/ipfs/{path}",
]
HEADERS = {"Accept": "image/webp,image/*", "User-Agent": "Mozilla/5.0 ChogathonResearch/1.0"}


def image_paths(metadata_file):
    """Metadata id -> IPFS path of its image, from the recorded metadata lines."""
    paths = {}
    for line in metadata_file.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        record = json.loads(line)
        image = json.loads(record["raw_json"]).get("image", "")
        if image.startswith("ipfs://"):
            paths[record["metadata_id"]] = image[len("ipfs://"):]
    return paths


def download(path, ready_at, interval):
    """Bytes of one image from the first gateway that is not cooling down, or None."""
    for index, template in enumerate(GATEWAYS):
        if ready_at[index] > time.monotonic():
            continue
        url = template.format(path=path)
        ready_at[index] = time.monotonic() + interval
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS), timeout=40) as response:
                return response.read(8_000_001), url
        except urllib.error.HTTPError as error:
            wait = float(error.headers.get("Retry-After") or 300) if error.code == 429 else 120
            ready_at[index] = time.monotonic() + wait
        except (urllib.error.URLError, TimeoutError):
            ready_at[index] = time.monotonic() + 60
    return None, None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--metadata", type=Path, default=ROOT / "research/evidence/genesis-metadata.jsonl")
    parser.add_argument("--output", type=Path, default=ROOT / ".artifacts/genesis-art")
    parser.add_argument("--size", type=int, default=256)
    parser.add_argument("--quality", type=int, default=85)
    parser.add_argument("--interval", type=float, default=0.5, help="seconds between requests to one gateway")
    parser.add_argument("--ids", type=int, nargs="*", help="metadata ids to fetch first (default: every recorded one)")
    parser.add_argument("--limit", type=int, default=0, help="stop after this many new portraits")
    parser.add_argument("--reverse", action="store_true",
                        help="start from the highest id, to share the work with a run going upwards")
    parser.add_argument("--max-minutes", type=float, default=120)
    args = parser.parse_args()

    folder = args.output / str(args.size)
    folder.mkdir(parents=True, exist_ok=True)
    manifest = args.output / "manifest.jsonl"
    paths = image_paths(args.metadata)
    wanted = [i for i in (args.ids or sorted(paths, reverse=args.reverse))
              if i in paths and not (folder / f"{i}.jpg").is_file()]
    print(json.dumps({"recorded": len(paths), "to_fetch": len(wanted)}), flush=True)

    ready_at = [0.0] * len(GATEWAYS)
    deadline = time.monotonic() + args.max_minutes * 60
    done = 0
    with manifest.open("a", encoding="utf-8") as log:
        while wanted and time.monotonic() < deadline and not (args.limit and done >= args.limit):
            metadata_id = wanted[0]
            if (folder / f"{metadata_id}.jpg").is_file():
                # Written meanwhile by another run of this script (see --reverse).
                wanted.pop(0)
                continue
            raw, url = download(paths[metadata_id], ready_at, args.interval)
            if raw is None:
                time.sleep(max(0.2, min(ready_at) - time.monotonic()))
                continue
            try:
                if len(raw) > 8_000_000:
                    raise ValueError("unexpected image size")
                picture = Image.open(io.BytesIO(raw)).convert("RGB")
            except (OSError, ValueError):
                # A placeholder page or a truncated file: try this id again later, through another gateway.
                wanted.append(wanted.pop(0))
                continue
            side = min(picture.size)
            left, top = (picture.width - side) // 2, (picture.height - side) // 2
            portrait = picture.crop((left, top, left + side, top + side)).resize((args.size, args.size), Image.LANCZOS)
            target = folder / f"{metadata_id}.jpg"
            portrait.save(target, "JPEG", quality=args.quality, optimize=True)
            log.write(json.dumps({"metadata_id": metadata_id, "source": url, "source_sha256": hashlib.sha256(raw).hexdigest(),
                                  "source_bytes": len(raw), "source_size": list(picture.size),
                                  "file": str(target.relative_to(args.output)), "bytes": target.stat().st_size}) + "\n")
            log.flush()
            wanted.pop(0)
            done += 1
            if done % 50 == 0:
                print(json.dumps({"new": done, "pending": len(wanted)}), flush=True)

    print(json.dumps({"new": done, "pending": len(wanted), "complete": not wanted}), flush=True)


if __name__ == "__main__":
    main()
