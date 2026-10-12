#!/usr/bin/env python3
"""One-off authoring step for ruleset genesis-0.3.0: map the trait values and the 1:1 heroes that
only appeared once the whole collection's metadata was recorded (9 October 2026).

Kept in the repository as the record of what changed between 0.2.0 and 0.3.0 and why. It
refuses to run on any other version, so it cannot be applied twice.
"""
import json
from pathlib import Path

CATALOG = Path(__file__).resolve().parents[2] / "content/hero-catalog.prototype.json"

# Same reading as the existing tables: shirts, tees and jackets are rangers; tank tops, armour
# and costumes are brutes; vests, robes and formal wear are controllers.
BODY = {
    "ranger": ["Black Shirt", "red tee", "Stripe shirts", "Boner", "Blue Holed Jacket", "Orange Holed Jacket"],
    "brute": ["Purple Tank", "Blue Mafia"],
    "controller": ["CEO Vest", "Jesus Robe"],
}
# Colour variants of eyes already mapped keep the shape of their family.
EYES = {
    "wave": ["Green Side Eye", "Mixed Side Eye", "Blue Retard", "Red Retard", "Blue Smirk", "Up", "Green Retard"],
    "nova": ["Red Round Eye", "Mixed Round Eye", "Red Flat", "Blue Flat", "Blue Happy", "Green Happy", "Normal"],
}
AURA = {
    "purify": ["White Aura"],
    "haste": ["Electric Shock"],
}
# Each 1:1 carries no trait values, so its kit is authored from its picture.
UNIQUES = [
    {"tokenId": "1481", "metadataId": 973, "id": "frogger", "roleId": "brute", "shapeId": "puddle", "effectId": "toxin"},
    {"tokenId": "1514", "metadataId": 1006, "id": "grim", "roleId": "ranger", "shapeId": "beam", "effectId": "veil"},
    {"tokenId": "1667", "metadataId": 1159, "id": "halo", "roleId": "controller", "shapeId": "nova", "effectId": "mend"},
    {"tokenId": "1798", "metadataId": 1290, "id": "krown", "roleId": "brute", "shapeId": "wave", "effectId": "rally"},
    {"tokenId": "1881", "metadataId": 1373, "id": "midas", "roleId": "ranger", "shapeId": "flash", "effectId": "haste"},
    {"tokenId": "1884", "metadataId": 1376, "id": "redd", "roleId": "controller", "shapeId": "beam", "effectId": "burn"},
]


def extend(groups, additions):
    by_result = {group["resultId"]: group["values"] for group in groups}
    listed = {value for values in by_result.values() for value in values}
    for result, values in additions.items():
        for value in values:
            if value in listed:
                raise SystemExit(f"Already mapped: {value}")
            by_result[result].append(value)


def main():
    text = CATALOG.read_text(encoding="utf-8")
    catalog = json.loads(text)
    if catalog["rulesetVersion"] != "genesis-0.2.0":
        raise SystemExit("This step only applies to genesis-0.2.0; the catalog is " + catalog["rulesetVersion"])
    if json.dumps(catalog, indent=2, ensure_ascii=False) + "\n" != text:
        raise SystemExit("The catalog file is not in its canonical two-space layout; refusing to rewrite it.")

    extend(catalog["bodyRules"], BODY)
    extend(catalog["eyeRules"], EYES)
    extend(catalog["auraRules"], AURA)
    for unique in UNIQUES:
        catalog["uniqueHeroes"].append({
            "tokenId": unique["tokenId"], "metadataId": unique["metadataId"], "id": unique["id"],
            "roleId": unique["roleId"], "shapeId": unique["shapeId"], "effectId": unique["effectId"],
            "expressionId": "inversion"})
    catalog["rulesetVersion"] = "genesis-0.3.0"
    CATALOG.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print("Catalog is now genesis-0.3.0:", sum(map(len, BODY.values())), "Body,", sum(map(len, EYES.values())),
          "Eyes,", sum(map(len, AURA.values())), "Aura values and", len(UNIQUES), "unique heroes added.")


if __name__ == "__main__":
    main()
