# Eyes and mouths of the 3D heroes against their art (22 October 2026)

Lot 2 of the heroes work. For every value of `Eyes` and `Mouth` worn on the base Origin, per Side (64 rows, from
`.artifacts/chog-body/traits/faces-audit-2026-10-21/`, made by `tools/blender/make_chog_faces_audit.py`), then the
Radioactive and Skull bases, the 3D bust and the head turned around were set beside the real picture of three Genesis
of the value. Verdicts: **faithful** (shape and place as drawn), **approximate** (right place, shape or colour a little
off), **wrong** (another drawing, or a colour no picture shows). Judged by eye on the sheets; the face change of
22 October (lot 1) does not touch the shapes of the eyes and mouths, so the Origin rows were not re-rendered. The
fixes are in `chog_trait_faces.py`, `chog_trait_bases.py` and `make_chog_traits.py`; the before and after, seen
turned and from the game camera, is `heroes-faces-fix-2026-10-22.jpg`.

## Result

| Group | Rows | Faithful | Approximate | Wrong |
| --- | --- | --- | --- | --- |
| Eyes, Origin | 43 | 28 | 15 (4 fixed) | 0 |
| Mouths, Origin | 21 | 19 | 2 | 0 |
| Eyes, Radioactive (17 values, 30 Genesis) | 17 | 0 before, 17 after | 0 | 17 before, 0 after |
| Eyes, Skull (25 of 25 value and Side pairs looked at) | 25 | 18 (Happy Left fixed) | 7 (Dead, five lasers, Side Eye Left) | 0 |
| Third eye (Head `3rd Eye`, 4 Genesis) | 1 | 0 before, 1 after | 0 | 1 before |

The Origin eyes and mouths were not the problem: no value is wrong, the four most worn approximate ones were fixed.
The Radioactive eyes were all wrong, in one way.

## The Radioactive coloured eyes (30 Genesis): wrong, fixed

Values: Red Round Eye (4 Genesis), Blue Retard (3), Green Side Eye (3), Mixed Retard (2), Blue Round Eye (2), Mixed Happy
(2), Mixed Side Eye (2), Red Flat (2), Green Happy (2), and one each of Green Round Eye, Green Smirk, Green Retard, Mixed
Round Eye, Blue Happy, Blue Smirk, Blue Flat, Red Retard. Looked at on #703, #834, #598, #772, #140, #343, #415, #625,
#672, #1009, #31, #473, #1100, #1192, #1200, #1281, #1657, #1849, #275 and the 1000-point pictures of #703, #343, #31,
#1192, #598, #140.

- **What the art shows**: the eye of the plain value (the Side=Left drawing of `Round Eye`, `Retard`, `Side Eye`, `Happy`,
  `Smirk`, `Flat`) with every black line, ring and pupil in the colour of the variant: dark red, blue, green or purple
  (Mixed). The white is white, or yellow for the green ones. Nothing is black. `Round Eye` has no pupil at all: a white
  disc in a coloured ring.
- **What the builder made**: the plain eye with only its white tinted; black rings, black pupils, a black pupil in the
  Round Eye. Verdict before: wrong (17 of 17).
- **Fix** (`RADIOACTIVE_EYE_INKS`, `RADIOACTIVE_EYE_NUMBERS`, `radioactive_eyes()` in `chog_trait_bases.py`): the `Dark`
  material of the plain entry becomes the ink of the colour (red 0.64/0.07/0.20, blue 0.06/0.33/0.68, green 0.24/0.58/0.08,
  purple 0.46/0.15/0.62; read on the pictures), the white keeps the glow tint (a yellow for green); the Round Eye drops its
  pupil (`PUPIL: None` in `build_round`, `LINE`/`WHITE` 0.058/0.050 so the ring is thin). Verdict after: faithful in
  shape and colour on the 17 values. Not exact: the ink is one colour per variant where the pictures shade it with the
  skin gradient.
- **The third eye** (Head `3rd Eye`, #598, #703, #1657, #1849, in `chog_trait_bases.py`): wrong before (a black oval with a
  white eye and a black pupil in it, which no picture draws); the pictures draw a white almond, tilted, in a thin dark-red
  outline, no pupil. Now an almond (`almond()`), outline 0.50/0.10/0.15. Faithful.
- **The hairline** (`RADIOACTIVE_HAIRLINE`): set for a face whose brow stood 0.178 over its middle; the face now rises to
  0.235 and narrows towards its top, so the zigzag now rises to 0.214 in the middle and falls to 0.140 at the sides: it
  stays on the face from one cheek to the other, as the top edge of the head the pictures draw.

## Origin and Skull: what was fixed

- `Round` and `Round Eye` (Right, 76 + 61 Genesis): the pupil filled 55 % of the dark disc, the pictures draw it close to
  full: `PUPIL` 0.033/0.039 to 0.036/0.042 (`Round`), 0.030/0.035 to 0.034/0.039 (`Round Eye`).
- `Dead` (Right, 74 Genesis; also the Skull #54, #389, #1716, #1776): strokes a seventh of the cross, the pictures a fifth:
  `THICKNESS` 0.022 to 0.030, `HALF` 0.044 to 0.046.

Left approximate, not fixed (said so, with their cause):

- The lasers (`fire laser`, `Green laser`, `Purple laser` on Right, and on Left `Purple laser`, `fire laser`,
  `cyan laser`, 262 Genesis on Origin): the beam stays short on purpose (the game draws the long beam when the hero attacks);
  on Side=Left it is thick and slants down where the pictures draw it level. Changing it touches the beam of the
  attack (`chog_attack_objects.py`, not mine).
- `MOG`, `Chog Viper`, `BTC Glass`, `Frog`, `Sunset visor`, the Skull `Dead` cross (larger on the skulls than on Origin):
  small differences of size and shade, listed below.

## Skull base (spot check, 17 of the 25 eye values; the other 8 are in the section after this one)

#121 Purple laser, #459 Angry, #54 Dead, #207 Worried, #729 Green laser, #122 Vertical, #373 fire laser, #540 Smirk, #81
Frog, #155 Happy, #483 Modern VR, #846 Flat, #1079 Normal, #1088 Thug, #1313 Up, #1331 XD, #1819 Flat: faithful, except
Dead (cross smaller than the sockets of the picture) and the three lasers (beam length, as above), approximate. The skulls'
jaw and teeth belong to the base, not to this audit.

## Skull base, the 8 values not looked at (lot 4b, 22 October 2026)

The 17 values of the spot check above were 17 of the 25 pairs of (eye value, Side) that the 90 Skull Genesis wear. The 8 left
(found by listing the Skull Genesis of `genesis-metadata.jsonl` and taking away the 17 seen) are: **Purple laser, Left**
(#418, #1647), **Angry, Left** (#993, #1221), **Happy, Left** (#1207, #1405), **Classic VR, Left** (#259), **Round, Right**
(#1045), **Side Eye, Left** (#1107), **Retard, Left** (#1267) and **cyan laser, Left** (#1426). Each was set beside its
picture and two more of the value (the other Skull Genesis, or the same value on the Origin base for the five worn once):
[heroes-skull-eyes-2026-10-22.jpg](heroes-skull-eyes-2026-10-22.jpg), bust, front, side, game camera, before | after. The
Left values of the Skull are drawn with the same eye builders as on Origin (only the Right `Flat`, `Normal`, `Up` and `Angry`
have dark sockets, already audited).

| Value | Genesis | Verdict | What is wrong |
| --- | --- | --- | --- |
| Angry, Left | #993, #1221 | faithful | white eye under a lid sloping to the nose, pupil to the right of the picture, glint: as the pictures (and #410, #425 of Origin) |
| Classic VR, Left | #259 | faithful | black visor with its slanting white stripes, a light frame; stripes a little whiter than the pictures' grey |
| Retard, Left | #1267 | faithful | large round whites, the near pupil low on the left, the far one to the right (as #32 and #1249) |
| Round, Right | #1045 | faithful | dark disc in a white ring, no pupil, as the pictures; a little smaller than the near eye of the picture |
| Side Eye, Left | #1107 | approximate | half lid and glint right; the far eye of the picture has a pink iris, ours is dark (small) |
| Happy, Left | #1207, #1405 (and Origin #925, #1430, 33 Genesis) | wrong shape, fixed | two equal tall arches; the pictures draw a long flat stroke, its right end higher, on the near eye and a short arch on the far one: `EYES_OF_SIDE["Left"]["Happy"]`, `HAPPY["EYE_ARCS"]` (near eye half width 0.066, arch 0.026, rise 0.034; far 0.032, 0.032, 0.013) |
| Purple laser, Left; cyan laser, Left | #418, #1647; #1426 | approximate, fixed in lot 4d | beams thick, short, slanting down where the pictures draw them level: now thinner and turned up 8 degrees (below) |

None was wrong enough to change beyond Happy (Left). Still approximate, listed: Side Eye (far eye iris).

## The lasers on Side Left, Origin and Skull (lot 4d, 22 October 2026)

Looked at on #127, #137, #214 (Purple), #252, #36, #308 (fire), #25, #28, #565 (cyan) and the Skull #418, #1647, #1426, each
beside two other pictures of its value: [heroes-lasers-left-2026-10-22.jpg](heroes-lasers-left-2026-10-22.jpg). The pictures draw
two thin beams that leave the eyes level or sinking a little towards the edge of the picture (about 10 to 15 degrees). The model
leaves the eyes level with the ground, but its head is turned up by 20 degrees for the game camera, so from the front the beams
slanted down by about 30 degrees, and they were thicker (0.021) with a wide light core. Fix (`BEAM_PITCH_DEG` 8 and `BEAM_RADIUS`
0.019, the radius of Side Right, in the three `EYES_OF_SIDE["Left"]` laser entries, `build_laser` of `chog_trait_faces.py`): the
beam is turned up 8 degrees from level, which brings the slant to the pictures'. The core (5 of the 8 sides) is kept: the
pictures draw most of the beam in the light colour with an edge of the darker one. Still short by design (the game draws the long
beam when the hero attacks), and Side Right is untouched. The Skull Left lasers (3 Genesis) use the same entries.

## The 64 Origin rows

<!-- audit table start -->
### Eyes (43 rows, sorted by Genesis count)

| # | Value | Side | Genesis (Origin / all) | Part | Verdict | What is wrong |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Happy | Right | 84 / 86 (#1396, #1408, #19) | Eyes_Happy | faithful | closed arches, thick in the middle |
| 2 | Round | Right | 76 / 77 (#826, #1090, #55) | Eyes_Round | approximate | pupil too small: the picture's pupil nearly fills the white (fixed 22 Oct: pupil 0.033/0.039 to 0.036/0.042) |
| 3 | Dead | Right | 74 / 78 (#333, #815, #1500) | Eyes_Dead | approximate | cross strokes a seventh of its width where the picture draws a fifth (fixed 22 Oct: stroke 0.022 to 0.030, arm 0.044 to 0.046) |
| 4 | Normal Eye | Right | 74 / 74 (#75, #5, #10) | Eyes_NormalEye | faithful |  |
| 5 | Heart | Right | 72 / 72 (#1043, #8, #13) | Eyes_Heart | faithful |  |
| 6 | Round Crying | Right | 69 / 69 (#6, #39, #87) | Eyes_RoundCrying | faithful |  |
| 7 | fire laser | Right | 67 / 70 (#38, #41, #65) | Eyes_Laser | approximate | short beam by design (the game draws the long one); dark oval and glow as drawn |
| 8 | Side Eye | Right | 66 / 66 (#1197, #47, #48) | Eyes_SideEye | faithful |  |
| 9 | Round Tear | Right | 65 / 65 (#1434, #92, #150) | none | faithful |  |
| 10 | Anime Glass | Right | 64 / 64 (#1387, #3, #44) | Eyes_AnimeGlass | faithful |  |
| 11 | Green laser | Right | 64 / 68 (#72, #77, #104) | none | approximate | short beam by design; eyes as drawn |
| 12 | Purple laser | Right | 63 / 68 (#52, #102, #107) | none | approximate | short beam by design; eyes as drawn |
| 13 | Gradient visor | Right | 61 / 61 (#30, #100, #305) | Eyes_Visor | faithful |  |
| 14 | Round Eye | Right | 61 / 61 (#237, #885, #953) | Eyes_Round | approximate | pupil too small, as Round (fixed 22 Oct: pupil 0.030/0.035 to 0.034/0.039) |
| 15 | Closed Eye Crying | Right | 57 / 57 (#24, #263, #277) | Eyes_ClosedEyeCrying | faithful |  |
| 16 | Retro Glass | Right | 56 / 56 (#819, #165, #312) | Eyes_RetroGlass | faithful |  |
| 17 | Tired | Right | 56 / 56 (#306, #66, #106) | Eyes_Tired | faithful |  |
| 18 | Teary | Right | 54 / 54 (#76, #432, #1736) | Eyes_Teary | faithful |  |
| 19 | Sunset visor | Right | 53 / 53 (#83, #1266, #1458) | Eyes_Visor | approximate | darker and browner than the orange-red band with its light middle |
| 20 | Plasma visor | Right | 43 / 43 (#82, #60, #93) | Eyes_Visor | faithful |  |
| 21 | Side Eye | Left | 34 / 35 (#983, #1442, #1764) | Eyes_SideEye | faithful |  |
| 22 | Base | Left | 33 / 33 (#1120, #198, #220) | Eyes_BaseEye | faithful |  |
| 23 | Happy | Left | 33 / 35 (#925, #1430, #1920) | Eyes_Happy | approximate | two equal tall arches where the pictures draw a long flat stroke, its right end higher, for the near eye and a short arch for the far one (fixed 22 Oct, lot 4b, seen on the Skull #1207 and #1405: `EYES_OF_SIDE` Left `Happy`) |
| 24 | MOG | Left | 32 / 32 (#162, #176, #181) | Eyes_Goggles | approximate | goggles narrower than the big wrapped goggles of the picture |
| 25 | Vertical | Left | 31 / 34 (#895, #1897, #1923) | Eyes_Vertical | faithful |  |
| 26 | Flat | Left | 30 / 32 (#671, #1731, #1824) | Eyes_FlatEye | faithful |  |
| 27 | Smirk | Left | 29 / 32 (#242, #1193, #1509) | Eyes_Smirk | faithful |  |
| 28 | Angry | Left | 27 / 29 (#410, #425, #783) | Eyes_Angry | faithful |  |
| 29 | Clown Eye | Left | 27 / 27 (#178, #760, #142) | Eyes_Clown | faithful |  |
| 30 | Frog | Left | 27 / 29 (#15, #1055, #1271) | Eyes_Frog | approximate | round where the picture draws a wide oval under a lid |
| 31 | Chog Viper | Left | 26 / 26 (#4, #50, #109) | Eyes_Goggles | approximate | light frame and strap narrower than the picture's, its lettering not drawn |
| 32 | Purple laser | Left | 26 / 28 (#1237, #127, #137) | Eyes_Laser | approximate | beams short, thick and slanting down where the picture's run straight; eyes as drawn (fixed 22 Oct, lot 4d: thinner, turned up 8 degrees; still short by design) |
| 33 | Clown Eye Mix | Left | 25 / 25 (#770, #1623, #1666) | Eyes_Clown | faithful |  |
| 34 | fire laser | Left | 25 / 25 (#522, #36, #279) | Eyes_Laser | approximate | same beams as Purple laser Left (fixed 22 Oct, lot 4d); colours read on the beams of #893, #252, #565, #1658 |
| 35 | Classic VR | Left | 24 / 25 (#1083, #46, #124) | Eyes_Headset | faithful |  |
| 36 | Modern VR | Left | 24 / 26 (#491, #219, #227) | Eyes_Headset | faithful | glowing rim a little thinner than drawn |
| 37 | Retard | Left | 23 / 24 (#32, #1249, #182) | Eyes_Retard | faithful |  |
| 38 | XD | Left | 23 / 25 (#768, #1194, #1911) | Eyes_XD | faithful |  |
| 39 | 3D Glass | Left | 22 / 22 (#361, #419, #424) | Eyes_Glasses3D | faithful |  |
| 40 | BTC Glass | Left | 19 / 19 (#189, #288, #510) | Eyes_CoinGlass | approximate | coins a little smaller than the lenses of the picture |
| 41 | Worried | Left | 18 / 22 (#192, #247, #349) | Eyes_WorriedEye | faithful |  |
| 42 | cyan laser | Left | 17 / 18 (#25, #1142, #1151) | Eyes_Laser | approximate | same beams as Purple laser Left (fixed 22 Oct, lot 4d) |
| 43 | Thug | Left | 15 / 17 (#141, #203, #250) | Eyes_PixelShades | faithful |  |

Rows: approximate 15 (663 Genesis), faithful 28 (1206 Genesis).

### Mouth (21 rows, sorted by Genesis count)

| # | Value | Side | Genesis (Origin / all) | Part | Verdict | What is wrong |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Sad | Right | 141 / 141 (#76, #953, #1043) | Mouth_Sad | faithful |  |
| 2 | normal smile | Right | 130 / 130 (#237, #9, #24) | Mouth_Smile | faithful |  |
| 3 | Gold Tooth | Right | 127 / 127 (#82, #815, #826) | Mouth_GoldTooth | faithful |  |
| 4 | Worried | Right | 127 / 127 (#333, #1197, #1500) | Mouth_Worried | approximate | wave smaller than drawn |
| 5 | Smile | Right | 122 / 122 (#306, #5, #6) | Mouth_SmileCurl | faithful |  |
| 6 | wo | Right | 120 / 120 (#83, #432, #1387) | Mouth_Wo | faithful |  |
| 7 | Drooling | Right | 116 / 116 (#819, #885, #1090) | Mouth_Drool | faithful |  |
| 8 | Bubblegum | Right | 112 / 112 (#163, #1, #12) | Mouth_Bubblegum | faithful |  |
| 9 | Rainbow Puke | Right | 111 / 111 (#112, #202, #1918) | Mouth_RainbowPuke | faithful |  |
| 10 | Lip Bite | Right | 108 / 108 (#75, #1736, #10) | Mouth_LipBite | approximate | line and teeth right, the stitched look of the teeth simplified |
| 11 | Drooling | Left | 68 / 68 (#15, #491, #1623) | Mouth_DroolOpen | faithful |  |
| 12 | normal smile | Left | 65 / 65 (#760, #770, #783) | Mouth_SmileLeft | faithful |  |
| 13 | BTC Coin | Left | 61 / 61 (#25, #1083, #1582) | Mouth_Coin | faithful |  |
| 14 | Clown Mouth | Left | 57 / 57 (#522, #925, #1120) | Mouth_ClownMouth | faithful |  |
| 15 | Smile | Left | 57 / 57 (#32, #178, #242) | Mouth_SmileCurl | faithful |  |
| 16 | Bubblegum | Left | 56 / 56 (#64, #985, #57) | Mouth_Bubblegum | faithful |  |
| 17 | Flat | Left | 54 / 54 (#425, #1666, #45) | Mouth_FlatMouth | faithful |  |
| 18 | Mon coin | Left | 48 / 48 (#895, #1194, #1249) | Mouth_Coin | faithful |  |
| 19 | Grit | Left | 47 / 47 (#671, #983, #1151) | Mouth_Grit | faithful |  |
| 20 | Sad | Left | 43 / 43 (#768, #46, #71) | Mouth_SadLeft | faithful |  |
| 21 | Rainbow Puke | Left | 34 / 34 (#814, #922, #17) | Mouth_RainbowPuke | faithful |  |

Rows: approximate 2 (235 Genesis), faithful 19 (1569 Genesis).

<!-- audit table end -->
