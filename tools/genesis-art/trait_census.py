#!/usr/bin/env python3
"""Count the values of every trait of the Chog Genesis collection, from the recorded metadata.

Usage: trait_census.py [--values TRAIT | --tokens TRAIT VALUE]

Without an option it prints, for each trait, how many different values it has, how many
Genesis carry it, and its most frequent values. With --values it lists every value of one
trait with its count; with --tokens, the numbers shown in the names ("CHOG #n", also the name
of the image file) of the Genesis that have one value. It answers "how many parts would a
library of trait parts need" and "which images show this trait".
It reads the metadata recorded on 7 October 2026, one line per Genesis
(research/evidence/genesis-metadata.jsonl).
"""
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "research" / "evidence" / "genesis-metadata.jsonl"


def tokens_with_traits():
    """One {trait: value} dictionary per Genesis."""
    found = []
    for line in SOURCE.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        record = json.loads(line)
        metadata = json.loads(record["raw_json"])
        traits = {str(item["trait_type"]): str(item["value"]) for item in metadata.get("attributes", [])}
        traits["#"] = str(record["metadata_id"])
        found.append(traits)
    return found


tokens = tokens_with_traits()
if "--tokens" in sys.argv:
    trait, value = sys.argv[sys.argv.index("--tokens") + 1], sys.argv[sys.argv.index("--tokens") + 2]
    print(" ".join(sorted((traits["#"] for traits in tokens if traits.get(trait) == value), key=int)))
    sys.exit(0)

print(f"source: {SOURCE.relative_to(ROOT)} ({len(tokens)} Genesis)")
for traits in tokens:
    del traits["#"]

counts = defaultdict(Counter)
for traits in tokens:
    for name, value in traits.items():
        counts[name][value] += 1

if "--values" in sys.argv:
    wanted = sys.argv[sys.argv.index("--values") + 1]
    for value, count in counts[wanted].most_common():
        print(f"{count:5d}  {value}")
    sys.exit(0)

total = 0
for name, values in sorted(counts.items(), key=lambda item: -len(item[1])):
    total += len(values)
    top = ", ".join(f"{value} ({count})" for value, count in values.most_common(4))
    print(f"{name:14s} {len(values):4d} values on {sum(values.values()):5d} Genesis   most frequent: {top}")
print(f"all traits together: {total} values")
