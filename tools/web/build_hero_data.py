#!/usr/bin/env python3
"""Write web/hero/heroes.json, the data of the "find your hero" page.

It joins the roster (content/genesis-roster.json), the sentences of the hero card
(content/genesis-card-text.json, written by the Unity roster import) and the skin colours
(content/genesis-skin-colors.json). Nothing here decides a kit: it only repacks what the
catalog and the importers decided, so the page shows the same card as the game.

  python3 tools/web/build_hero_data.py           # writes the file
  python3 tools/web/build_hero_data.py --check   # fails if the file is stale
"""
import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ROSTER = ROOT / "content/genesis-roster.json"
CARD_TEXT = ROOT / "content/genesis-card-text.json"
SKIN_COLORS = ROOT / "content/genesis-skin-colors.json"
OUTPUT = ROOT / "web/hero/heroes.json"

# The dash, U+2014, between the number and the name of a 1:1 hero, with its two spaces.
LONG_DASH = " " + chr(0x2014) + " "

# One hero is a row of positions, in this order, to keep the file small.
HERO_FIELDS = ["tokenId", "metadataId", "name", "role", "shape", "effect", "expression", "tier", "body", "eye", "aura", "skin"]


def build():
    roster = json.loads(ROSTER.read_text(encoding="utf-8"))
    text = json.loads(CARD_TEXT.read_text(encoding="utf-8"))
    skin_hex = {entry["value"]: entry["hex"] for entry in json.loads(SKIN_COLORS.read_text(encoding="utf-8"))["colors"]}

    if text["rulesetVersion"] != roster["rulesetVersion"]:
        raise SystemExit(f"Card text is for ruleset {text['rulesetVersion']}, the roster for {roster['rulesetVersion']}: "
                         "import the roster in Unity again.")
    for table, key in (("roles", "roles"), ("shapes", "shapes"), ("effects", "effects"), ("deliveries", "expressions")):
        if [entry["id"] for entry in text[table]] != roster[key]:
            raise SystemExit(f"Card text and roster disagree on {table}: import the roster in Unity again.")

    heroes = []
    for hero in roster["heroes"]:
        # The names of the 1:1 heroes carry a long dash; the page writes them without it.
        name = hero["name"].replace(LONG_DASH, " ")
        heroes.append([name if field == "name" else hero[field] for field in HERO_FIELDS])

    return {
        "rulesetVersion": roster["rulesetVersion"],
        "chainId": roster["chainId"],
        "collection": roster["collection"],
        "recruitHealthPercent": text["recruitHealthPercent"],
        "roles": text["roles"],
        "shapes": text["shapes"],
        "effects": text["effects"],
        "deliveries": text["deliveries"],
        "tiers": roster["tiers"],
        "bodies": roster["bodies"],
        "eyes": roster["eyes"],
        "auras": roster["auras"],
        "skins": [{"value": value, "color": skin_hex.get(value, "")} for value in roster["skins"]],
        "heroFields": HERO_FIELDS,
        "heroes": heroes,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="fail if web/hero/heroes.json is stale")
    args = parser.parse_args()

    wanted = json.dumps(build(), ensure_ascii=False, separators=(",", ":")) + "\n"
    if args.check:
        if not OUTPUT.is_file() or OUTPUT.read_text(encoding="utf-8") != wanted:
            sys.exit(f"{OUTPUT} is stale: run python3 tools/web/build_hero_data.py")
        print(f"{OUTPUT} is up to date")
        return
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(wanted, encoding="utf-8")
    print(f"{OUTPUT}: {len(json.loads(wanted)['heroes'])} heroes, {len(wanted) // 1024} KB")


if __name__ == "__main__":
    main()
