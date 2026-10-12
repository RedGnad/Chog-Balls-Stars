# Hero catalog

`hero-catalog.prototype.json` is the single source of the rules that turn a Genesis into a
hero. The traits come from the public metadata; **which trait decides what is a design
decision of this project**. Ruleset `genesis-0.3.0`, schema 2.

## One visible trait decides one thing

| Trait | Decides | Results |
| --- | --- | --- |
| Body | the class | `brute`, `ranger`, `controller`; a Genesis without Body is a `brute` |
| Eyes | the shape of the signature | `beam` (lasers), `flash` (visors and glasses), `puddle` (tears), `wave` (hard stares), `nova` (soft eyes) |
| Aura | the effect of the signature | `burn`, `chill`, `hex`, `mend`, `purify`, `rally`, `haste`, `veil`, `toxin`; no Aura is `raw` |
| Base and Tier | how the signature is delivered | Common `direct`, Uncommon `echo`, Rare Skull `return`, Epic Radioactive `persistent-zone`, Legendary 1:1 `inversion` |

Head, Mouth, Skin, Background, Side and Accessory are cosmetic in this ruleset. Each 1:1 has its
own entry under `uniqueHeroes`, keyed by canonical token id and metadata id.

Rules are grouped by result: `{ "resultId": "burn", "values": ["Fiery Aura", "Burning Aura", "Fire"] }`.
A `null` entry stands for an absent trait; the literal string `None` would be a different value.
A value that is not listed is rejected with `UNMAPPED_TRAIT`: no Genesis ever receives a default
hero. A value listed twice fails at load.

## Coverage, 9 October 2026

The whole collection resolves: 1,969 of 1,969 Genesis, none rejected, into 237 distinct kits.

| Axis | Count per result |
| --- | --- |
| Class | ranger 725, brute 655, controller 589 |
| Shape | nova 596, wave 478, flash 370, beam 279, puddle 246 |
| Effect | raw 985, burn 205, chill 184, hex 175, haste 112, rally 106, mend 74, purify 60, veil 58, toxin 10 |
| Delivery | direct 1,279, echo 590, return 60, persistent-zone 30, inversion 10 |

Ruleset 0.3.0 added the 25 trait values that only appeared once the rest of the metadata was
recorded, each placed with its family (a "Black Shirt" is a ranger like the other shirts, a
"Red Round Eye" a nova like the other round eyes), and an authored kit for the six remaining
1:1 heroes, chosen from their picture. The exact additions are in
`tools/hero-catalog/extend_catalog_0_3.py`.
[Report](../research/evidence/genesis-catalog-coverage-2026-10-09.json).

| 1:1 | Class | Shape | Effect |
| --- | --- | --- | --- |
| Blaze | controller | nova | burn |
| Burning Skully | brute | wave | burn |
| Chog Scream | controller | wave | hex |
| Chogbomination | brute | nova | toxin |
| Frogger | brute | puddle | toxin |
| Grim | ranger | beam | veil |
| Halo | controller | nova | mend |
| Krown | brute | wave | rally |
| Midas | ranger | flash | haste |
| Redd | controller | beam | burn |

```sh
python3 tools/hero-catalog/run.py test       # 30 resolver tests on real fixtures
python3 tools/hero-catalog/run.py coverage   # resolve every scanned token with recorded metadata
```

Fixtures with their token bindings are in [the manifest](../tests/fixtures/genesis/manifest.json):
tokens 512, 561, 973 and 1795, the project owner's Genesis (CHOG #1287).

## Roster

`genesis-roster.json` is generated from this catalog, the recorded metadata and the on-chain
token bindings: one entry per Genesis the resolver accepts, sorted by canonical token id, with
its kit stored as positions in the catalog's id tables (so the order of `roles`, `shapes`,
`effects`, `expressions` and `tiers` is part of the ruleset). The game imports it into the
`GenesisRoster` simulation asset; see [the game notes](../game/README.md). A Genesis the
catalog rejects would be absent from the roster and could not enter a match; today all 1,969
are in it.

```sh
python3 tools/hero-catalog/run.py roster           # regenerate after a catalog or metadata change
python3 tools/hero-catalog/run.py roster --check   # fail if the file is stale
```

## Limits

The class of a Genesis is linked to a playable character through the roster, and its shape and
effect to one of fifty special attacks ([game notes](../game/README.md)). **Four of the five
deliveries exist in a match:** `direct`, `echo` (Uncommon: fires twice), `return` (Rare Skull:
goes out, then comes back) and `persistent-zone` (Epic Radioactive: leaves a zone).
`inversion` is still a content identifier: the ten Legendary 1:1 play like a direct hit, and
their hero card says so. What each effect does
in play is a first pass written in `game/Assets/Chog/Editor/GenesisSignatureImporter.cs`, not
a balanced design. Change `rulesetVersion` whenever a
rule changes: the catalog hash travels with every resolved hero and with the match configuration.
