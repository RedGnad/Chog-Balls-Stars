#!/usr/bin/env python3
"""Count how many Chog Genesis a library of 3D trait parts would dress, by number of parts.

Usage: trait_coverage.py

It answers "how many shapes must be modelled before most Genesis look like themselves". The
values of Head and Eyes are grouped into shapes: one cap mesh serves every cap, whatever its
colour and print. The grouping is read from the names, corrected by what the pictures show
(see CAPS_SEEN_ON_PICTURES); it was checked on a sheet of real pictures for the caps, bucket
caps, beanies and headbands only (trait_sheet.py makes such a sheet).
A Genesis counts as dressed when the shape of its Head and the kind of its Eyes are modelled,
or it has none; the 15 mouths are counted as all drawn.
It reads the metadata recorded on 7 October 2026 (research/evidence/genesis-metadata.jsonl).
"""
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "research" / "evidence" / "genesis-metadata.jsonl"

# Head values whose name does not end in "Cap" but whose pictures show the same cap, with
# another colour and another print (looked at on 12 October 2026).
CAPS_SEEN_ON_PICTURES = {"Peace hat", "MCGA", "MEGA Chog", "NEET"}


def head_shape(value):
    low = value.lower()
    if "bucket" in low:
        return "bucket cap"
    if "beanie" in low:
        return "beanie"
    if "headband" in low or "bandana" in low:
        return "headband"
    if low.endswith(" cap") or value in CAPS_SEEN_ON_PICTURES:
        return "cap"
    if "horn" in low or "thorn" in low:
        return "horns"
    return value


def eye_kind(value):
    low = value.lower()
    if "laser" in low:
        return "laser eyes (an object)"
    if "visor" in low:
        return "visor (an object)"
    if "glass" in low:
        return "glasses (an object)"
    if "vr" in low:
        return "VR headset (an object)"
    return "drawn: " + value


def read_tokens():
    tokens = []
    for line in SOURCE.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        metadata = json.loads(json.loads(line)["raw_json"])
        tokens.append({str(item["trait_type"]): str(item["value"]) for item in metadata.get("attributes", [])})
    return tokens


def by_count(tokens, trait, group):
    counts, values = Counter(), {}
    for traits in tokens:
        if trait in traits:
            counts[group(traits[trait])] += 1
            values.setdefault(group(traits[trait]), set()).add(traits[trait])
    return counts, values


tokens = read_tokens()
other_base = sum(1 for traits in tokens if traits.get("Base") != "Origin")
print(f"source: {SOURCE.relative_to(ROOT)} ({len(tokens)} Genesis, {other_base} of them on a base other than Origin)")
print("without the trait: " + ", ".join(
    f"{trait} {sum(1 for traits in tokens if trait not in traits)}" for trait in ("Head", "Eyes", "Mouth", "Body")))

heads, head_values = by_count(tokens, "Head", head_shape)
eyes, eye_values = by_count(tokens, "Eyes", eye_kind)
for title, counts, values in (("Head shapes", heads, head_values), ("Eyes kinds", eyes, eye_values)):
    print(f"\n{title}: {len(counts)}")
    for name, count in counts.most_common():
        print(f"  {count:5d}  {name}  ({len(values[name])} values)")

head_order = [name for name, _ in heads.most_common()]
eye_order = [name for name, _ in eyes.most_common()]

print("\nGenesis whose hat is right (those without a hat included), most worn shapes first:")
for built in (1, 2, 4, 6, 10, 15, len(head_order)):
    done = set(head_order[:built])
    covered = sum(1 for traits in tokens if "Head" not in traits or head_shape(traits["Head"]) in done)
    print(f"  {built:3d} shapes: {covered:5d} of {len(tokens)}")

print("\nGenesis whose hat and eyes are both right:")
for hats_built, eyes_built in ((1, 4), (4, 10), (6, 14), (10, 20), (15, 30), (len(head_order), len(eye_order))):
    hats_done, eyes_done = set(head_order[:hats_built]), set(eye_order[:eyes_built])
    covered = sum(1 for traits in tokens
                  if ("Head" not in traits or head_shape(traits["Head"]) in hats_done)
                  and ("Eyes" not in traits or eye_kind(traits["Eyes"]) in eyes_done))
    print(f"  {hats_built:3d} hat shapes and {eyes_built:3d} eye kinds: {covered:5d} of {len(tokens)}")

body = Counter(traits["Body"] for traits in tokens if "Body" in traits)
print(f"\nBody (clothing): {len(body)} values, {sum(1 for count in body.values() if count >= 20)} of them worn by "
      f"20 Genesis or more, {sum(1 for count in body.values() if count < 5)} by fewer than 5")
