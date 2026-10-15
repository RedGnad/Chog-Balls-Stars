#!/usr/bin/env python3
"""Count how many Chog Genesis a library of 3D trait parts would dress, by number of parts.

Usage: trait_coverage.py

It answers "how many shapes must be modelled before most Genesis look like themselves". The
values of Head and Eyes are grouped into shapes: one cap mesh serves every cap, whatever its
colour and print.

The Head grouping is NOT read from the names: "Balls Cap", "Dollar Cap" or "Peace hat" are drawn
as bucket hats, "MCGA" and "NEET" as caps, and "Mon Cap" is a bucket hat on the pictures of
Side=Right and a cap on those of Side=Left. Every value was assigned on 14 October 2026 from the
pictures themselves: a sheet of real pictures per value and per Side (trait_sheet.py makes such
a sheet), and, for every value worn by Origin Genesis, the layer of that value rebuilt from all
its pictures (the pixels that agree across them and differ from the median of their Side), which
shows the whole silhouette of the hat without the face, the eyes or the mouth in front of it.
HEAD_SHAPES is that assignment; HEAD_SHAPES_OF_SIDE overrides it where the two Sides draw a value
differently. A value missing from both tables stops the script: look at its pictures first.

A Genesis counts as dressed when the shape of its Head and the kind of its Eyes are modelled,
or it has none; the 15 mouths are counted as all drawn.
It reads the metadata recorded on 7 October 2026 (research/evidence/genesis-metadata.jsonl).
"""
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "research" / "evidence" / "genesis-metadata.jsonl"

# What the pictures show, value by value. The shape names are those of make_chog_traits.py where
# a shape is built ("Cap", "BucketCap", ...), plain words otherwise.
HEAD_SHAPES = {
    # A baseball cap: a crown whose front panel carries a word or a logo, a brim at the front only,
    # whose far end curls up on the pictures of Side=Right.
    "TISM Cap": "Cap", "MMGA Cap": "Cap", "Horny Cap": "Cap", "Choggie Cap": "Cap", "Boob Cap": "Cap",
    "BOOB Cap": "Cap", "BTC Cap": "Cap", "Chog Cap": "Cap", "ETH Cap": "Cap", "MCGA": "Cap",
    "MEGA Chog": "Cap", "Buttcoin Cap": "Cap", "10k TPS Cap": "Cap", "JEET Cap": "Cap", "Jeet Cap": "Cap",
    "RETARD Cap": "Cap", "WAGMI Cap": "Cap", "NEET": "Cap", "MCGA Cap": "Cap",
    # A bucket hat: a round crown with stitched rings and a brim all around, which dips at the far
    # side. Most of these are named "Cap".
    "Balls Cap": "BucketCap", "Cloud Cap": "BucketCap", "Blue Bucket Cap": "BucketCap",
    "Orange Bucket Cap": "BucketCap", "Gray Bucket Cap": "BucketCap", "Dollar Cap": "BucketCap",
    "Thunder Cap": "BucketCap", "Star Cap": "BucketCap", "Peace hat": "BucketCap", "Red Peace Cap": "BucketCap",
    "Peace Cap": "BucketCap", "Mon Cap": "BucketCap",
    # A knitted beanie: a dome over a thick ribbed cuff.
    "Wizard Beanie": "Beanie", "Choggie Beanie": "Beanie", "BTC Beanie": "Beanie", "RH Beanie": "Beanie",
    "Mon Beanie": "Beanie", "8 Ball Beanie": "Beanie",
    "Crown": "Crown",
    # A fur hat with ear flaps, on both Sides (another colour set on each).
    "Russian Hat": "Ushanka",
    # A band around the brow, under the leaves of the head.
    "Red Headband": "Headband", "Japan Headband": "Headband",
    # Curly hair around the head: yellow curls under a red top, or curls in six colours.
    "Bob Hair": "CurlyWig", "Rainbow Wig": "CurlyWig",
    "Durag": "Durag",
    "Wizard Hat": "WizardHat",
    "Spartan Helmet": "SpartanHelmet",
    # A red ball on the nose: a Head value, but not a hat.
    "Clown Nose": "ClownNose",
    "Plumber": "Plunger",
    # A knitted mask over the whole head, with holes for the eyes.
    "Robber": "Balaclava",
    # On Skull and Radioactive Genesis only.
    "Red Horn": "RamHorns", "Purple Horn": "RamHorns",
    "Demon Horn": "DemonHorns", "Void Horn": "DemonHorns", "devil horn": "DemonHorns",
    "Gold Thorn": "ThornCrown", "Silver Thorn": "ThornCrown",
    "Silver wreath": "Wreath", "3rd Eye": "ThirdEye", "Holy Seal": "HolySeal", "Taped Banana": "TapedBanana",
    "Diamond": "ForeheadGem", "Red Bandana": "Bandana", "Blue Laser": "ForeheadLaser",
    "Poison Shroom": "Mushrooms", "Mafia Hat": "Fedora", "Head Wrap": "HeadWrap",
}
# Where the two Sides draw a value as two different shapes.
HEAD_SHAPES_OF_SIDE = {
    ("Mon Cap", "Left"): "Cap",          # white crown, purple brim at the front
}


def head_shape(value, side=None):
    shape = HEAD_SHAPES_OF_SIDE.get((value, side)) or HEAD_SHAPES.get(value)
    if shape is None:
        raise SystemExit(f"trait_coverage: no shape recorded for Head={value!r}: look at its pictures first")
    return shape


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
    # Read on the pictures on 15 October 2026: these names say nothing of an object, yet they are.
    if value in ("MOG", "Chog Viper"):
        return "ski goggles (an object)"
    if value == "Thug":
        return "pixel sunglasses (an object)"
    return "drawn: " + value


def read_tokens():
    tokens = []
    for line in SOURCE.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        metadata = json.loads(json.loads(line)["raw_json"])
        tokens.append({str(item["trait_type"]): str(item["value"]) for item in metadata.get("attributes", [])})
    return tokens


def hat_of(traits):
    return head_shape(traits["Head"], traits.get("Side")) if "Head" in traits else None


tokens = read_tokens()
other_base = sum(1 for traits in tokens if traits.get("Base") != "Origin")
print(f"source: {SOURCE.relative_to(ROOT)} ({len(tokens)} Genesis, {other_base} of them on a base other than Origin)")
print("without the trait: " + ", ".join(
    f"{trait} {sum(1 for traits in tokens if trait not in traits)}" for trait in ("Head", "Eyes", "Mouth", "Body")))

split_by_side = {value for value, _ in HEAD_SHAPES_OF_SIDE}
heads, head_values = Counter(), {}
for traits in tokens:
    if "Head" in traits:
        shape = hat_of(traits)
        heads[shape] += 1
        value = traits["Head"]
        label = f"{value} [Side={traits.get('Side')}]" if value in split_by_side else value
        head_values.setdefault(shape, Counter())[label] += 1
eyes, eye_values = Counter(), {}
for traits in tokens:
    if "Eyes" in traits:
        eyes[eye_kind(traits["Eyes"])] += 1
        eye_values.setdefault(eye_kind(traits["Eyes"]), Counter())[traits["Eyes"]] += 1

none = sum(1 for traits in tokens if "Head" not in traits)
print(f"\nHead shapes, as the pictures draw them: {len(heads)}, most worn first. 'cumulative' counts the Genesis "
      f"whose hat is right once this shape and those above it are built, the {none} without a hat included.")
covered = none
for name, count in heads.most_common():
    covered += count
    values = ", ".join(f"{value} {number}" for value, number in head_values[name].most_common())
    print(f"  {count:5d}  cumulative {covered:5d}  {name}: {values}")

head_order = [name for name, _ in heads.most_common()]
eye_order = [name for name, _ in eyes.most_common()]
print("\nEyes kinds:")
for name, count in eyes.most_common():
    print(f"  {count:5d}  {name}  ({len(eye_values[name])} values)")

print("\nGenesis whose hat and eyes are both right:")
for hats_built, eyes_built in ((1, 4), (4, 10), (6, 14), (10, 20), (15, 30), (len(head_order), len(eye_order))):
    hats_done, eyes_done = set(head_order[:hats_built]), set(eye_order[:eyes_built])
    both = sum(1 for traits in tokens
               if ("Head" not in traits or hat_of(traits) in hats_done)
               and ("Eyes" not in traits or eye_kind(traits["Eyes"]) in eyes_done))
    print(f"  {hats_built:3d} hat shapes and {eyes_built:3d} eye kinds: {both:5d} of {len(tokens)}")

body = Counter(traits["Body"] for traits in tokens if "Body" in traits)
print(f"\nBody (clothing): {len(body)} values, {sum(1 for count in body.values() if count >= 20)} of them worn by "
      f"20 Genesis or more, {sum(1 for count in body.values() if count < 5)} by fewer than 5")
