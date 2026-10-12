#!/usr/bin/env python3
"""Find, from the Genesis portraits, the colour that belongs to each value of one trait.

For each value of the trait, the script counts the colours of the portraits that carry it and
keeps the colour that is most over-represented compared with the rest of the collection. A
colour every Chog shares (the cream face, the black outline) scores low; the colour that only
Chogs with "Skin: Red" have scores high. The result is a starting point to look at, not a
measurement of the artist's palette.

Usage: derive_trait_colors.py Skin [--output content/genesis-skin-colors.json]
"""
import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / ".artifacts/genesis-art/256"
STEP = 24          # colours are compared in buckets of this size per channel
SAMPLE = 64        # portraits are read at this size: enough for flat cartoon colours


def traits_by_metadata_id():
    result = {}
    for line in (ROOT / "research/evidence/genesis-metadata.jsonl").read_text(encoding="utf-8").splitlines():
        if line.strip():
            record = json.loads(line)
            attributes = json.loads(record["raw_json"]).get("attributes", [])
            result[record["metadata_id"]] = {a["trait_type"]: a["value"] for a in attributes}
    return result


def colours_of(metadata_id):
    """Counter of colour buckets of one portrait, with the sum of real colours per bucket."""
    picture = Image.open(ART / f"{metadata_id}.jpg").convert("RGB").resize((SAMPLE, SAMPLE))
    counts, sums = Counter(), defaultdict(lambda: [0, 0, 0])
    raw = picture.tobytes()
    for offset in range(0, len(raw), 3):
        red, green, blue = raw[offset], raw[offset + 1], raw[offset + 2]
        bucket = (red // STEP, green // STEP, blue // STEP)
        counts[bucket] += 1
        total = sums[bucket]
        total[0] += red
        total[1] += green
        total[2] += blue
    return counts, sums


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("trait")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--per-value", type=int, default=40, help="portraits read for each value")
    parser.add_argument("--override", action="append", default=[], metavar="VALUE=#RRGGBB",
                        help="a colour chosen by eye where the derived one is wrong; recorded as such")
    args = parser.parse_args()
    overrides = dict(item.split("=", 1) for item in args.override)

    by_value = defaultdict(list)
    for metadata_id, traits in traits_by_metadata_id().items():
        if args.trait in traits and (ART / f"{metadata_id}.jpg").is_file():
            by_value[traits[args.trait]].append(metadata_id)

    share, real = {}, {}
    for value, ids in by_value.items():
        counts, sums = Counter(), defaultdict(lambda: [0, 0, 0])
        for metadata_id in sorted(ids)[:args.per_value]:
            one_counts, one_sums = colours_of(metadata_id)
            counts.update(one_counts)
            for bucket, total in one_sums.items():
                for channel in range(3):
                    sums[bucket][channel] += total[channel]
        pixels = sum(counts.values())
        share[value] = {bucket: count / pixels for bucket, count in counts.items()}
        real[value] = {bucket: tuple(round(sums[bucket][c] / counts[bucket]) for c in range(3)) for bucket in counts}

    colours = {}
    for value in sorted(by_value, key=lambda v: -len(by_value[v])):
        others = [share[other] for other in share if other != value]
        best, best_score = None, 0.0
        for bucket, part in share[value].items():
            elsewhere = sum(other.get(bucket, 0.0) for other in others) / max(1, len(others))
            # Present on a real part of the portrait here, and far more than on the others.
            score = part - 2.0 * elsewhere
            if part > 0.03 and score > best_score:
                best, best_score = bucket, score
        if best is None:
            best = max(share[value], key=share[value].get)
        red, green, blue = real[value][best]
        colours[value] = {"hex": f"#{red:02X}{green:02X}{blue:02X}", "portraits": len(by_value[value]),
                          "share": round(share[value][best], 3)}
        if value in overrides:
            colours[value] = {"hex": overrides[value].upper(), "portraits": len(by_value[value]),
                              "chosen_by_eye_instead_of": colours[value]["hex"]}
        print(f"{value:<22} {len(by_value[value]):>5}  {colours[value]['hex']}"
              + ("  chosen by eye" if value in overrides else f"  share {colours[value]['share']}"))

    if args.output:
        target = ROOT / args.output if not args.output.is_absolute() else args.output
        # A list, not a map: the Unity importer reads this file with JsonUtility.
        listed = [dict(value=value, **entry) for value, entry in colours.items()]
        target.write_text(json.dumps({"trait": args.trait, "method": "most over-represented colour per value, see tools/genesis-art/derive_trait_colors.py",
                                      "colors": listed}, indent=1) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
