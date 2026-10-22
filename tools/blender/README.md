# The Chog body, made by script

The 3D body a Genesis hero wears in the game: a round low-poly creature with a belly, big mitten
hands, big feet and a crest of spikes down its back. It is built from numbers by a Blender script,
with no file drawn by hand. The contract it follows is in `game/README.md`, section "The body of a
hero, and where the Chog body goes".

Made and measured on 11 October 2026 with Blender 5.2.0 LTS on this Mac. **In the game since
15:28 that day**: the exported file was copied to `game/Assets/Chog/Resources/ChogBody.fbx`, and
the build that followed shows it in the body viewer (`?bodies=1`, facing the right way, the five
poses) and on a Genesis hero in a headless match
(`research/evidence/chog-body-in-match-headless-2026-10-11.png`). After a change to the model,
copy the new `ChogBody.fbx` over that file and build again. The "Not done" list below was written
before that import.

## What a Chog looks like (official art, found 12 October 2026)

The first body (below) was drawn from the Genesis busts alone and got the character wrong: it
has a belly and a fan of spikes on its back. The user asked on 12 October that the model look
like the real Chog. The official pictures show:

- the spikes belong to the **head**: a big crown of flat spikes around a cream face. In the
  game the head is the picture of the Genesis, which already shows them, so the body carries
  none;
- the **body** is slim and plain, like a one-piece suit in the skin colour (purple by
  default): a narrow torso that runs into two legs, long arms;
- the **hands are big pink mittens** and the feet are pink: purple body and pink hands are
  what makes a Chog readable.

Where the pictures are (all public, fetched with a browser's User-Agent; copies under
`.artifacts/chog-reference/`, which is not part of the repository):

| Picture | Address |
| --- | --- |
| The whole character from behind (the main reference for the body) | `https://www.chog.xyz/home/chog.png` |
| The whole character from the front, in a costume | `https://www.chog.xyz/tools/jump.png` |
| Other poses | `https://www.chog.xyz/chog/curious.png`, `/chog/read.png`, `/team/bigchog.png` |
| The face and the gloved fists | `https://chogathon.chog.xyz/chog1.jpg` (also `chog5.jpg`, `chog6.jpg`, `chognice.jpg`, `chogwow.jpg`) |
| The layers of the site's picture maker (head with spikes, face, clothes, 7 folders) | `https://www.chog.xyz/chogify/body.webp`, `/chogify/face.webp`, `/chogify/<category>/<category>-<n>.webp` (see `research/research_notes/Héros Genesis traits et liaison NFT/trait_to_visual_pipeline.md`) |
| More scenes | `https://build.chog.xyz/images/` (`chog.png`, `cutechog.jpeg`, `chogcoolimage.jpeg`, `chogandmonadgolem.jpeg`), `https://portal.chog.xyz/chog_dance.gif` |

Three bodies that follow these were drawn on 12 October by `make_chog_body_variants.py`
(`-- --variant A|B|C`) into `.artifacts/chog-body/variants/`, with their pictures
(`render_chog_body_variants.py`) and one sheet that sets them beside the first body
(`make_chog_body_comparison.py`, kept as
`research/evidence/chog-body-variants-blender-2026-10-12.png`):

| Variant | What it is | Triangles | At the size of a phone |
| --- | --- | --- | --- |
| A | faithful to the picture from behind: slim torso, two legs, long arms, pink mittens and feet | 3,508 | the body is a small lump, the hands are specks |
| B | A pushed to be read small: shorter legs, wider stance, larger mittens and feet, arms held out | 3,508 | reads best: four pink shapes around the body |
| C | B with a ring at the neck and the torso in a cloth colour | 3,868 | ties the head to the body when the clothing is light |

Half of each count is the dark outline, a second shell inside the mesh of the part it
outlines. Materials: `ChogSkin`, `ChogGlove` (hands and feet), `ChogOutline`, and `ChogCloth`
in C. The shoulders are 0.25 from the middle (0.41 on the first body), which the game's
animator does not mind: it only turns the parts. **Variant B was in the game for an hour on
12 October and was taken out**: shown the models, the user found them worse than the first
body, because a body alone has no head, no spikes and no face, and nothing of a Chog is
left in 3D. The first body is back in `game/Assets/Chog/Resources/ChogBody.fbx`.
`check_chog_body.py` still measures the first body's rules (3,000 triangles, one material,
long toes) and fails the variants on those three.

## The whole character, with a head (12 October 2026, pictures only)

`make_chog_full.py` (`-- --style H1|H2|H3`) builds a whole Chog into
`.artifacts/chog-body/full/<style>/`: the body of variant B and a real 3D head, big, with a
crown of spikes, a cream face, eyes and blush. `render_chog_full.py` renders it and
`make_chog_full_comparison.py` lays the three heads beside the first body and variant B on
one sheet, kept as `research/evidence/chog-full-3d-heads-blender-2026-10-12.png`.

| Head | What it is | Seen from the game's camera |
| --- | --- | --- |
| H1 | seven flat blades in one plane around the head, as in the drawing | the crown is seen almost edge on, the face is lost |
| H2 | round spikes on the top and the back | reads as a hedgehog |
| H3 | the blades of H1 made thick, a row behind, the head turned up 20 degrees | reads best: face, crown and pink hands |

4,937 to 5,921 triangles; six materials (`ChogSkin`, `ChogFace`, `ChogGlove`, `ChogEye`,
`ChogBlush`, `ChogOutline`); the head's origin is at the neck (0.597) and the shoulders are
0.145 from the middle. The phone-size pictures assume the game draws the model 1.5 times
larger than today.

**The user chose H3 as the direction on 12 October** and refused the Genesis picture laid
over the 3D face (last row of the sheet): the traits of a Genesis are to be modelled in 3D,
a few at a time. **Nothing of this is in the game**: no import in Unity, and the game as it
is would hide the `Head` part and tint every material one colour. Still crude: the eyes are
round and generic where a Chog looks sideways, and the blades are faceted where the drawing
has soft leaves.

## Trait parts on the polished head (12 October 2026, pictures only)

`make_chog_traits.py` builds head H3 polished, then the 3D parts of each trait of a Genesis,
read from `research/evidence/genesis-metadata.jsonl`. `render_chog_traits.py` renders it,
`make_chog_traits_sheet.py` lays one sheet per Genesis, `make_chog_traits_overview.py` one
sheet for all, and `make_chog_traits_all.py` runs the lot. **Nothing of this is in the game.**

- **The base** keeps the head, the leaves and so the outline of H3. Its blades are leaves
  (round section, shaded smooth, a blunt point that curls back); its cream face is wider
  (0.25 from its middle, 0.225 on H3) and has no eyes and no mouth: in the collection those
  are traits. It keeps the dark line of the face, the nose, the cheeks and the teeth of
  hair on the brow. 5,953 triangles (H3: 5,177).
- **A trait part** is one object, child of `Head` (clothing: of `Body`, `armLeft`,
  `armRight`) with no move of its own, whose materials carry the name of its trait; the
  outline shell is the shared `ChogOutline`. An entry of `HATS`, `EYES`, `MOUTHS` or
  `CLOTHES` maps a value of the collection to a shape, a colour set, a print or a mark,
  and the numbers it replaces (first of all the brim of a hat). A value with no entry is
  printed as NOT MODELLED. `-- --coverage` prints how many Genesis each entry serves.

| Part | What it is | Values of the collection it has an entry for |
| --- | --- | --- |
| `Hat_Cap` | crown, short raised brim, button, a word in Blender's built-in font | TISM Cap (49); MMGA Cap (34), a check only |
| `Hat_BucketCap` | round crown, brim all around, turned up at the front, a mark | Blue Bucket Cap (48), Balls Cap (62) |
| `Hat_Crown` | flared band, eight points, gems | Crown (54) |
| `Hat_Beanie` | dome, ribbed cuff, printed rings | Wizard Beanie (30) |
| `Eyes_AnimeGlass` | thick frame, white lenses | Anime Glass (64) |
| `Eyes_Laser` | drawn dark eyes and two short beams | Purple laser (96) |
| `Eyes_Visor` | one band wrapped around the face, in three colours | Sunset visor (53) |
| `Eyes_NormalEye`, `Eyes_Happy`, `Eyes_Round` | flat shapes on the face | Normal Eye (74), Happy (121), Round (77) |
| `Mouth_RainbowPuke`, `Mouth_Bubblegum` | a stream of six stripes, a ball: volume | Rainbow Puke (145), Bubblegum (168) |
| `Mouth_GoldTooth`, `Mouth_Smile`, `Mouth_Drool`, `Mouth_DroolOpen`, `Mouth_Wo` | flat shapes on the face | Gold Tooth (127), normal smile (195), Drooling (184, the open mouth for Side=Left), wo (120) |
| `Cloth_Tee` (+ `_armLeft`, `_armRight`) | a thin shell over the torso and the top of the arms, stripes if asked | Fire Shirt (31), Prisoner (33), Red 143 Jersey (9) |

Seven Genesis are dressed (triangles with the outline shells, limit 8,000): #1287 7,546,
#1241 7,357, #80 6,821, #1408 6,011, #1078 7,801, #83 6,181, #1948 7,393. With these
entries 24 of the 1,969 Genesis are dressed whole (`-- --coverage`).

```sh
# everything: models, pictures, one sheet per Genesis, the overview, the brim strip (about 10 minutes)
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_all.py
# one Genesis by hand
B=/Applications/Blender.app/Contents/MacOS/Blender
$B --background --factory-startup --python tools/blender/make_chog_traits.py -- --genesis 1287
$B --background --factory-startup --python tools/blender/render_chog_traits.py -- --name 1287
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_sheet.py --genesis 1287 --with-base
# try numbers of a hat without touching the model of the Genesis
$B --background --factory-startup --python tools/blender/make_chog_traits.py -- --genesis 1287 \
    --set BRIM_LENGTH=0.07 --set BRIM_RISE_DEG=20 --out .artifacts/chog-body/traits/experiments/brim_try
```

The sheets are `.artifacts/chog-body/traits/<number>/sheet_<number>.png`,
`traits/overview_six.png` and `traits/1287/brim_before_after.png`. They read the 1000-point
pictures (`tools/genesis-art/fetch_portraits.py --size 1000 --quality 95 --ids ...`) and the
red pictures of H3 in `.artifacts/chog-body/full/H3/`.

What was learnt, and what is not right yet:

- **The camera of the game sees the eyes, never the mouth.** It looks down from 73 degrees
  and the head looks up by 20: the lower face turns away from it (computed and seen on the
  hero without a hat). So a hat must leave the brow clear (short brim that rises, crown
  that leans back), and from above a mouth shows only if it has volume (bubble, rainbow).
  Showing a grin from above would take a head turned up further, which is a change to H3.
- **A name is not a shape.** "Balls Cap", "Cloud Cap", "Dollar Cap", "Thunder Cap", "Star
  Cap", "Red Peace Cap", "Peace hat" are drawn as bucket hats, and so is "Mon Cap" on the
  pictures of Side=Right: `tools/genesis-art/trait_coverage.py` counts them as caps (951),
  which is too many by at least 313. Look at a `trait_sheet.py` sheet before modelling.
- **Side=Left is another drawing.** Hats, eyes and clothes hardly cross the two sides (4
  of 66 head values, 6 of 58 eyes); no Genesis wears a beanie and a visor. Mouths cross,
  but "Drooling" is an open mouth on the left: `MOUTHS_OF_SIDE` holds such cases.
- The leaves of H3 are much larger than the spikes of the Genesis pictures, and the busts
  are framed wider than the pictures. Clothing is a first pass: one colour, stripes, no
  print (the 143 of the jersey, the collar of a shirt beyond a band). Lettering is one
  geometric font. Drawn parts and lenses are shown in their colour whatever the light and
  trait parts cast no shadow: choices of these renders. No import in Unity, no check of
  the FBX, no Accessory, no Aura.

## More trait parts, batch 2 (14 and 15 October 2026, pictures only)

The tables grew into two modules that `make_chog_traits.py` imports: `chog_trait_hats.py` (hat shapes,
logos, beanie patches, clothing) and `chog_trait_faces.py` (eyes and mouths). **Nothing of this is in
the game.** What they hold, with the Genesis each serves (`-- --coverage`):

- **Hats, 10 shapes** (assignment by picture in `tools/genesis-art/trait_coverage.py`, `HEAD_SHAPES`):
  Cap 592, BucketCap 490, Beanie 129, Ushanka 67, Headband 65, CurlyWig 63, Crown 54, Durag 50,
  WizardHat 47, SpartanHelmet 40. Every cap carries its word (font built into Blender) or its logo;
  bucket hats their logo; beanies their patch. Not built: Clown Nose, Plumber, Robber (78 Genesis on
  the base Origin) and the hats of the other bases.
- **Eyes, 30 kinds**: every Eyes value worn on the base Origin, both Sides.
- **Mouths, 15 kinds**: every Mouth value; "Drooling" (an open mouth) and "Bubblegum" (other cheek)
  are other drawings on Side=Left.
- **Clothing**: the 32 Body values worn by 20 Genesis or more, on three cuts (`Tee`, `Jacket`,
  `Track`); costumes with a hood or helmet and the duck float are left out (`LEFT_OUT`).

On the base Origin (1,869 Genesis): hat right 1,791, eyes 1,869, mouth 1,869, all three 1,791,
clothing too 1,110. `-- --budget` adds up the triangles of every dressed hero: the largest is 7,994
(limit 8,000), after lowering the sides of the shirt, the cap and the bubble and cutting the words
into wider pieces (each change is noted at its number).

```sh
# build and render several Genesis into one folder, one Blender at a time (nice -n 10, -t 4)
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py \
    --out .artifacts/chog-body/traits/batch-2 --genesis 97,103,8 [--only bust,studio,towards]
# review sheets: real picture beside the 3D hero, game camera, phone size
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_review.py rows \
    --batch .artifacts/chog-body/traits/batch-2 --trait Head --genesis 97,103 --out FILE.png
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_review.py rows \
    --batch .artifacts/chog-body/traits/batch-2 --random 12 --seed 20261015 --missing --out FILE.png
```

The sheets are in `.artifacts/chog-body/traits/batch-2/` (`hats_sheet_part*.png`, `clothes_sheet.png`,
`eyes_sheet_part*.png`, `mouths_sheet_part*.png`, `random_twelve.png`). From the game camera at
phone size the hats, the beams, visors, goggles, headsets, glasses, the bubble and the rainbow show;
eyes and mouths drawn on the face mostly do not (head H3 and the camera are unchanged).

## Batch 3: spikes under hats, Side=Left clothing, prints, costumes (15 October 2026, pictures only)

What the owner asked after batch 2, in their order. **Nothing of this is in the game.**

- **No leaf goes through a hat.** Each hat shape says, in its numbers, what becomes of each line of
  `SPIKES` (`LEAVES`, items made by `leaf(share, turn, back, bend)` in `chog_trait_hats.py`): removed,
  kept, or brought out under the rim (longer, turned down, bent back up). Read on the pictures: they draw
  spikes beside and behind a hat, under its rim. Cap, bucket hat, beanie, crown, wizard hat: the lowest
  pair of the star, the pair of the back row and the leaf straight back stay. Ushanka: the same, lower.
  Headband (lowered onto the brow): the top leaf and the upper pair stand above it too. Durag: two pairs
  beside it. Spartan helmet (its back raised): the back row only. Plumber: all but the top and the upper
  pair. Robber: the knit lets the far and back leaves through, as drawn. `leaf_crossings()` in
  `make_chog_traits.py` checks every build and fails a Genesis whose leaves cross its hat.
- **Hair replaces the crown.** Bob Hair and Rainbow Wig (`CurlyWig`), a hood up (Yellow Hoodie, the
  costumes): no leaf and no teeth of hair on the brow (`FRINGE`). Durag and Spartan Helmet are headwear
  with spikes behind them on the pictures. No hat (or a hat not modelled): the whole crown. A removed
  leaf takes its outline shell with it: `Head` is 2,633 triangles (1,298 of outline) with the whole crown,
  1,793 (878) under a cap, 1,088 (528) under hair.
- **Fixes**: ushanka flaps longer, wider and splayed (leather ones of Side=Left get a fur rim); wigs are a
  mass covered with round curls; the Spartan crest is a brush 0.16 wide and 0.30 tall; seven points of
  hair in the skin colour along the front of the durag; tuxedo lapels and shirt collar points.
- **New**: Clown Nose, Plumber (a plunger), Robber (a balaclava); the clothing of Side=Left (hoodies hood
  down and up, tops without sleeves, the three 143 jerseys, tees, shirts, worn shirts with patches,
  track jackets, vests); prints (INMATE, 67, CHUG LIFE, CWO, MILF and its heart, Hawaiian flowers, yukata
  swirls, the Squid Game triangle, the Jesus sash, the open Kings Robe with its fur, the Wizard cape
  triangles and bow, Cloud Cap swirls, the Dollar Cap label, the ETH mark, the 10K TPS box, the bars of
  the Buttcoin B; long cap words kept on the flat front, `PRINT_FIT` 0.34); costumes (Gangster, Shark,
  Banana, Astro, Dragon, Duck Float); Squid Game and Cyan tuxedo, missed before.

Numbers (`-- --coverage`, `-- --budget`, base Origin, 1,869 Genesis): hat 1,869, eyes 1,869, mouth
1,869, all three 1,869, clothing too 1,789 (batch 2: 1,791, 1,869, 1,869, 1,791, 1,110). Not modelled:
Accessory, Aura, and 21 Body values worn by 1 to 17 Genesis each (Golden Armor, the toad suits, Sci Fi,
White Yukata...). Largest dressed hero #470 (Bob Hair) 7,940 triangles, median 6,527, none over 8,000.
One dressed hero is 6 to 10 mesh objects (the four parts and up to 6 trait parts, plus the root, an
empty) and, among the heroes built here, 9 to 24 distinct materials: #470 10 and 22, a typical one
(#97, cap, jacket) 10 and 17, #1209 8 and 24, #75 (no hat, no clothing) 6 and 9 (`parts.json` keeps
`mesh_objects` and `materials` per Genesis). Not restructured: that is for the import decision.

```sh
# build and render, waits for no Blender and for .artifacts/heavy-job.lock not saying "busy"
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py \
    --out .artifacts/chog-body/traits/batch-3 --genesis 97,19 --only bust,studio,towards
# coverage and budget, with the same waits
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py --run make_chog_traits.py -- --coverage
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py --run make_chog_traits.py -- --budget
# before and after, and sheets laid one under the other
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_review.py compare \
    --batch .artifacts/chog-body/traits/batch-3 --before .artifacts/chog-body/traits/batch-2 \
    --genesis 97,19,4,39,12,43,1241,51,10,21,75 --out .artifacts/chog-body/traits/batch-3/spikes_before_after.png
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_review.py stack --sheets a.png,b.png --out FILE.png
```

Sheets in `.artifacts/chog-body/traits/batch-3/`: `spikes_before_after.png`, `hair_sheet.png`,
`clothes_left_sheet.png`, `fixes_sheet.png`, `new_hats_sheet.png`, `prints_sheet.png`,
`costumes_sheet.png`, `random_twelve.png` (seed 20261016). Still wrong on them: the curls are faceted
(7 sides, for the budget); the leaves brought out under a brim splay like wings seen from the front and
the back; inside the open crown the bare top of the head shows from above; the lapels and most shirt
prints are lost at the game camera and on the busts of Side=Left, framed on the face; the Buttcoin
bars are barely visible; the 10K of the box reads like "1OK"; the tees whose word changes per Genesis
carry none.

## Batch 4: case by case per Head value, shorter spikes, Bob Hair, Spartan helmet (15 October 2026, pictures only)

What the owner said after batch 3: "The 3D is better but you did not do it case by case. Some beanies do not
let the spikes through at the back. The spikes themselves should be shortened a little. #43 (Bob Hair) is hair
with a headband, not hair with a red top. #21: the helmet is badly made." **Nothing of this is in the game.**

- **Case by case, per value of Head.** Every value was looked at on five Genesis per Side (all of them when
  five or fewer), then on their 1000-point pictures; what each shows and what is built is in
  `head-values-from-the-art.md`, the contact sheets in `.artifacts/chog-body/traits/batch-4/art-looked-at/`.
  The decisions are in code: `HEAD_VALUES` in `chog_trait_hats.py`, one line per value and Side (53 lines), whose
  numbers (first of all `LEAVES`) replace those of the hat shape (`from_the_art()`); `-- --coverage` now says how
  many values take their leaves from their pictures (all of them) and how many from their shape only (none).
  What the pictures show: on Side=Left (every beanie, the Left caps, the Left Russian Hat) no spike at all, beside
  or behind the hat, so those keep no leaf; on Side=Right two to four short points stacked down the far side from
  under the rim to the cheek, about as wide as long. Those are now the middle pair just under the rim, the lowest
  pair at the cheek and the back pair behind, narrow (`width`, a fifth item of `leaf()`) and never bent (batch 3
  turned them down and bent them up: wings). #39 (Russian Hat, Side=Right) is not spikeless: its 1000-point
  picture shows two small yellow spikes beside the flap; it keeps two small leaves under the fur at the back.
  `leaf_crossings()` still fails any build whose leaf goes through its hat (Robber keeps its recorded
  exception, `THROUGH`, as its pictures draw the spikes through the knit).
- **Spikes shorter.** `LEAF_LENGTH` (in `POLISH`) makes every leaf 0.70 times as long as on H3: tried at 1.0,
  0.85, 0.75, 0.70 and 0.65 on the busts of #75, #112, #76 beside their pictures
  (`.artifacts/chog-body/traits/batch-4/leaf_length.png`). The pictures draw shorter spikes still; at 0.70 the far
  side becomes a row of small points as drawn and the crown still reads from the game camera.
- **Bob Hair** (all 45 Genesis looked at: the same yellow hair and red band on each, whatever the skin): yellow
  curls only, and a red band tied around the hair over the brow, its two edges waving, a darker lower edge
  (`BAND` of `CURLY_WIG`, `add_hair_band()`); curls kept off the band. Not built: the tip of one spike that
  #43, #470 and #1137 show above the band (it would have to come through the hair).
- **Spartan Helmet** (all 40 looked at), rebuilt: the edge of the shell is given per angle around the head
  (`EDGE`): just above the eyes, a nose guard down between them, cheek guards from the outer corner of the eyes
  to the jaw, the back open; a light yellow trim along every edge; a taller, wider red plume; the leaves come out
  of the open back, longer, and stand beside the helmet.
- **Also done** (item E): a gold floor inside the open crown, above the head (from the game camera the bare top
  of the head showed inside); a short upright bar inside the 0 of "10K", as the pictures draw it (it read "1OK");
  the durag lower at the back so its back pair of leaves comes out under it; the "Chog Cap" of Side=Right (one
  Skull Genesis) is the Choggie Cap drawing.
- **One Blender for many Genesis**: `make_chog_traits_many.py` builds and renders a whole list in one run;
  `make_chog_traits_batch.py` now starts it once per 40 Genesis (until now it started Blender twice per
  Genesis). `--no-render` builds only (to check the leaves).

- **Found on the way**: the "1" of "10K" was in two pieces ("!0K") because a straight stroke has points only
  at its ends, and laid across the bend of the crown at a ring height its middle sank under the box.
  `lay_word()` now also cuts a word across at the ring heights of the hat.

Numbers (`-- --coverage`, `-- --budget`). Coverage did not change (no value was added): base Origin 1,869 of
1,869 for hat, eyes, mouth and all three, 1,789 with the clothing too; all 1,969 Genesis: hat 1,930, eyes
1,933, mouth 1,969, the three 1,921, the clothing too 1,821. New line: the leaves of 53 values (on their Side),
1,677 Genesis, come from their pictures, none from the hat shape alone. Budget (base Origin): largest #470
7,924 triangles (batch 3: 7,940), then #1302 7,878 and #236 7,866; median 6,569 (6,527); none over 8,000. The
head is 4,408 to 5,953 triangles depending on the leaves kept (13 sets); Bob Hair 2,224, the Spartan helmet
1,196. 142 Genesis built and rendered for the sheets in 4 runs of Blender (about 10 minutes), none failed.

```sh
# build and render (one Blender per 40 Genesis; waits for no other Blender and for the heavy-job lock)
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py \
    --out .artifacts/chog-body/traits/batch-4 --genesis 3,991,1946,43,470 [--no-render] [-- --head-set LEAF_LENGTH=0.85]
# coverage and budget
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py --run make_chog_traits.py -- --coverage
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py --run make_chog_traits.py -- --budget
# one row per value of Head and Side, 12 rows per sheet (per_value_1.png, per_value_2.png...)
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_review.py values \
    --batch .artifacts/chog-body/traits/batch-4 --genesis 3,991,1946,5,678,1936 --size 200 --label 320 \
    --rows-per-sheet 12 --out .artifacts/chog-body/traits/batch-4/per_value.png
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_review.py compare \
    --batch .artifacts/chog-body/traits/batch-4 --before .artifacts/chog-body/traits/batch-3 \
    --genesis 43,470,21,4,39,75,97,19,3,12,51,10,1241 --out .artifacts/chog-body/traits/batch-4/before_after.png
```

Sheets in `.artifacts/chog-body/traits/batch-4/`: `per_value_1.png` to `per_value_4.png` (one row per value of
Head and Side, 47 rows, the bare heads of Side=Right included: what the art shows, three Genesis as picture and 3D bust, the game camera of the first),
`per_value_5.png` (the 7 values worn only on the Skull base: pictures, not built), `before_after.png` (batch 3
beside batch 4 for #43, #470, #21, #4, #39, #75, #97, #19, #3, #12, #51, #10, #1241), `leaf_length.png` (the five
lengths tried), and the contact sheets of the art in `art-looked-at/`.

Still wrong, or not done:

- The spikes of a bare head are still longer than drawn (0.70 is a compromise with the game camera), and on
  Side=Left the pictures draw them in cream, part of the face, where the builder keeps the skin colour.
- Under a cap the points are smaller than the spikes the pictures draw beside a cap, and the bust shows one or
  two where the pictures show two to four: the rest hide behind the cheek or the brim from that angle.
- Bob Hair: the waves of the band are a gentle up and down of its edges, not the frill the pictures draw; the
  curls are faceted (7 sides) and the hair is less tall than drawn; the tip of a spike above the band is not built.
- Spartan helmet: the cheek guards stop at the ears so the leaves show (the pictures draw them wider, the spikes
  coming out from behind the helmet); the eye openings are the space between the brow, the nose guard and the
  guards, not cut holes; the plume is an 8-sided brush.
- The floor inside the crown is a dark gold lid seen from above; the pictures never show the inside.
- The glass of the astronaut helmet (item E) is not done.
- Durag: the pictures draw no point of hair on the brow; the seven points the owner asked for in batch 3 are kept.
- Not modelled: the 17 values of Head worn only on the Skull and Radioactive bases (37 Genesis), listed with
  what their art shows at the end of `head-values-from-the-art.md`; those bases themselves are not built.

## Batch 5: the head judged turned around, a row under the real rim of each hat (15 October 2026, pictures only)

What the owner said after batch 4: still not coherent (long spikes on some, only two on each side, spikes
that do not show behind), some spikes and hair do not look like the pictures at all, the crown is worse,
and the work was judged from one angle. **Nothing of this is in the game.**

- **The turnaround.** `render_chog_traits.py --only turnaround` renders the head alone, without
  perspective, from eight turns around it (front, 45, left side, 135, back, 225, right side, 315), from
  straight above, and from the game's angle behind and in front; `make_chog_traits_review.py turnaround`
  lays them beside the picture and the bust, one row per Genesis. Seen that way, batch 4 had under every
  hat of Side=Right a pair of spikes on each side, none at the back, and a collar of spikes at the neck;
  the heads of Side=Left were in the skin colour where the pictures draw them cream; the crown had a gold
  floor that read as a pie from above.
- **A row under the real rim** (`UNDER_RIM`, `add_rim_leaves` in `make_chog_traits.py`). A hat is a volume
  on a spiky head: under every hat of Side=Right that lets the spikes out, the seven lines of the crown are
  removed and one row of leaves is planted just under the rim of the hat as built. At each angle the rim
  is found by casting rays out of the head and seeing where the hat stops covering it; each leaf is
  checked with `leaf_crossings()` and lowered until it does not cross the hat. Nine leaves (degrees from
  the front 78, 104, 130, 156 on both sides, 180), the same for every hat, leaning back and bent up a
  little at the point: from the pictures' angle two to four stack down the far side under the rim, as
  drawn; from behind and from above they fan out all around. The line of `HEAD_VALUES` says which row:
  `RIM_ROW` (caps), `BUCKET_RIM_ROW` (longer, so their points pass the brim that goes all around),
  `BIG_RIM_ROW` (wizard hat, durag), `USHANKA_RIM_ROW` (a few short ones at the back), none (Side=Left,
  hair, hoods). The pairs of batch 3 and 4 (`SIDE_POINTS` and the like) are gone.
- **Side=Left heads are cream** (`cream_head`): skull and leaves in the cream of the face, no teeth of
  hair on the brow; points of hair under a hood keep the skin colour.
- **The crown is an open band** (`FLOOR`: None, `BAND_INSIDE`): up the outside, down the inside, nothing
  over the top; inside it the head and its top leaf, short (`CROWN_TOP`), and the row under its band.
  (The first try closed the band with a flat face, which add_loops puts on an end row: a gold lid.)

Numbers (`-- --coverage`, `-- --budget`, base Origin): hat, eyes, mouth 1,869 of 1,869, clothing too
1,789 (unchanged); largest dressed hero #470 7,924 triangles, median 6,180, none over 8,000. One Genesis
per value and Side (48) built with the turnaround, none failed.

```sh
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py \
    --out .artifacts/chog-body/traits/batch-5 --genesis 97,19,1241 --only bust,turnaround
.artifacts/tools-venv/bin/python tools/blender/make_chog_traits_review.py turnaround \
    --batch .artifacts/chog-body/traits/batch-5 --genesis 97,19,1241 --size 150 --label 180 --out FILE.png
```

Sheets in `.artifacts/chog-body/traits/batch-5/`: `before_after_turned.png` (seven heroes, batch 4 above
batch 5), `turn_caps_right_1.png`, `turn_caps_right_2.png`, `turn_caps_left_1.png`, `turn_caps_left_2.png`,
`turn_beanies_left.png`, `turn_other_1.png`, `turn_other_2.png`. Still wrong: Bob Hair and Rainbow Wig are
masses of round puffs where the pictures draw one mass with a wavy edge, in large blocks of colour for the
wig; from the game camera the row under a bucket hat shows only its points; the bare head of Side=Left
keeps the whole star where the pictures draw a row of points over the top; the durag's row stands out
like ears from the front; the Spartan helmet's cheek guards and eye holes (batch 4).

## Into the game: the library of parts (15 October 2026)

After batch 6 the owner said yes to the 3D in the game. `export_chog_parts.py` writes
`game/Assets/Chog/Resources/ChogParts.bytes` (2.1 MB), which the game assembles every hero from
(`game/README.md`, "The 3D heroes"):

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
    --python tools/blender/export_chog_parts.py -- [--check 5000]
```

Each part depends on one trait (and the Side): the head with its leaves on the hat (or a hood), the hat
on `Head`, the eyes on `Eyes`, the mouth on `Mouth`, the clothing on `Body`. The script names the parts
each Genesis wears by those keys, builds the 79 Genesis that wear all 324 parts between them (with the
checks of `make_chog_traits.py`), and with `--check N` builds N more and fails if any of their objects
differs from the one kept under its key. Run with `--check 5000` on 15 October: the 1,790 other Genesis of
the base Origin were all built and all matched, so a hero assembled in the game is the hero this folder
builds. The 100 Genesis on another base are not exported (no head of their own yet). It also writes the
bare Chog in grey, under the number 0, for the recruits. Summary in `.artifacts/chog-body/parts-export.json`.
After any change to the 3D, run it again and build the game.

**The recruit's eyes (17 October 2026, branch `recruit-eyes`).** The bare Chog had no eyes (eyes are a trait),
so its face read as blank. The export now gives it the eyes of "Side Eye" (`RECRUIT_EYES` in
`export_chog_parts.py`), the very part the Genesis of that value wear, no new mesh: a dark eye half closed under
a straight lid, glancing aside, the Genesis drawing of the official mascot's half-lidded look
(`chogathon.chog.xyz/chog1.jpg`, the first eyes of the site's picture maker), and the most worn eye part that is
neither closed nor an object. CHOG #112 wears it on the bare head. Exported at 16:24 with `--check 5000`: 1,970
heroes, 495 pieces, 749 materials, 123 Genesis built and 1,846 more checked, none failed, none lacking a part (as
before); the file differs from `main`'s only in the recruit's record, which gains that one piece (6,033 triangles).
`render_recruit.py` renders any hero of the library as the game assembles it, turned around, and
`make_recruit_sheet.py` lays the pictures on one sheet
([before and after](../../research/evidence/chog-recruit-eyes-turnaround-2026-10-17.jpg)). Seen on it: from the
front, three quarter and both sides the eyes lie on the face, follow its curve and are symmetric. **From the game
camera (73 degrees) they barely show**: a few dark points under the fringe, unreadable at phone size, exactly as
on CHOG #112 and every Genesis whose eyes are drawn (batch 2 found it); showing drawn eyes from above would take a
change of the head or of where every drawn eye sits, for all heroes. The commands:

```sh
B=/Applications/Blender.app/Contents/MacOS/Blender
$B --background --factory-startup -t 4 --python tools/blender/export_chog_parts.py -- --check 5000
sh tools/dev/view_tests.sh
git show main:game/Assets/Chog/Resources/ChogParts.bytes > .artifacts/chog-body/recruit-eyes/ChogParts-main.bytes
$B --background --factory-startup -t 4 --python tools/blender/render_recruit.py -- \
    --library .artifacts/chog-body/recruit-eyes/ChogParts-main.bytes --name recruit_before
$B --background --factory-startup -t 4 --python tools/blender/render_recruit.py
.artifacts/tools-venv/bin/python tools/blender/make_recruit_sheet.py \
    --out .artifacts/chog-body/recruit-eyes/recruit_turnaround.png \
    --row "before=.artifacts/chog-body/recruit-eyes/recruit_before" --row "after=.artifacts/chog-body/recruit-eyes/recruit"
```

## The tower and the core (16 October 2026, a proposal behind `?structures3d=1`)

`make_chog_structures.py` builds what each team defends in the style of the heroes and writes
`game/Assets/Chog/Resources/ChogStructures.bytes` in the same format (four records: each structure in
the colour of each side, the side's colour being the "skin" of that library):

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
    --python tools/blender/make_chog_structures.py -- --export --render
```

- **The tower**: a column of pale lavender stone with two bands in the side's colour on a dark plinth,
  under the H3 head of the heroes (built by `make_chog_traits.build`, scaled to 2.05 units across) with
  the fire laser eyes of the collection cut short ("Tower laser": two glowing eyes; whole beams stuck out
  of the face like a beak from the side) and the mouth "Grit". About 3.4 units high; a hero is 2.5.
- **The core**: a pink heart, leaning back 35 degrees so the camera of the game (73 degrees above the
  ground) sees its shape and not a bean, in a crown of nine Chog leaves opening outwards, on a plinth with
  a ring in the side's colour.
- Seen as pictures turned all around and from the game's angle (`--render`, into
  `.artifacts/chog-body/structures/`; sheet in `research/evidence/chog-structures-3d-turn-2026-10-16.jpg`).
  Moving parts are their own: `Head`, `Eyes` (empty: where the beam starts) and `Heart`.

`export_chog_parts.py` gained two optional arguments for it (`piece_of(..., record=)`,
`write_library(..., skins=)`); the heroes' export is unchanged.

## Batch 6: one head for all, no spike dropped, spikes checked all around, hair as big clumps (15 October 2026, pictures only)

What the owner said after batch 5: some heroes are right (the caps), others show no spikes on the same
head (the bucket hats, the durag behind, the Side=Left caps), the crown ends in one lone spike, and the
process is wrong, not one hero. **Nothing of this is in the game.** The owner found batch 6 good enough for
now, to be improved later (15 October, evening).

Why, found before changing anything: under each hat the row of batch 5 dropped every leaf that crossed
the hat, without failing the build (`#62` and `#80` kept 2 leaves of 9, `#51` 4: the brim that goes all
around and the tails take them); batch 4 had read the pictures of Side=Left as showing no spike, when
their spiky head, in the skin colour, fills the frame around the face and was taken for the backdrop
(looked at again on twelve bare heads and four caps of Side=Left: `#410`, `#491`, `#760`, `#1083`,
`#1120`, `#1193`, `#1194`, `#1271`, `#361`, `#864`, `#1065`, `#162`).

- **One head for all.** Side=Left keeps the head of Side=Right, in its skin colour (the cream head of
  batch 5 is gone), and every hat of Side=Left takes the row under the rim (`LEFT_HEAD` says what their
  pictures show).
- **No leaf dropped.** `add_rim_leaves` tries, in the order of `RIM_TRIES`, each leaf at its angle, rooted
  lower, leaning further down, turned a little aside, then shorter; one that finds no place stops the
  build. The rim is found by rays reaching 0.30 off the head (0.10 missed the roomy crown of a bucket hat).
  The leaves of the row have a lighter section (`RIM_PROFILE`), for the triangle budget.
- **Checked all around.** `spike_sectors_seen` casts a ray from the tip of each leaf towards the camera,
  from above and from the game's angle behind: a tip hidden by the hat or the head does not count. Every
  hero with leaves must show a tip in each sector of `SPIKE_SECTORS` (left and right sides, the two three
  quarters behind, the back), else its build fails; the ushanka and the Spartan helmet, whose flaps and
  cheek guards cover the sides down to the jaw, want them behind and three quarters behind only. The
  sectors seen are printed per hero ("spike tips seen").
- **Headbands, robber.** A headband keeps the leaf on top and the upper pair above it and the row under
  it; the robber's leaves through the knit point out to the sides, longer.
- **The crown** sits on the spiky head: the row under its band, and a tuft inside the open band
  (`CROWN_ROW`, "INSIDE": rooted on the top of the head, leaning back) whose points rise behind its points.
- **Hair as big clumps** (`LOBES`, `add_hair_lobe`): a few balls with a waving outline in place of small
  curls. Bob Hair: seven clumps above and below a wider band whose upper edge is a frill; Rainbow Wig:
  eleven, one colour each, red, orange, yellow, green, blue and purple around the face as drawn.

Numbers (`-- --coverage`, `-- --budget`): hat, eyes, mouth 1,869 of 1,869 on the base Origin, clothing too
1,789; largest dressed hero #470 7,984 triangles, median 6,671, none over 8,000. 49 Genesis built (every
value of Head on each Side of the base Origin, and the bare heads), all through both checks.

Sheets in `.artifacts/chog-body/traits/batch-6/`: `before_after_turned.png` (the heroes the owner pointed
at, batch 5 above batch 6), `turn_caps_right_1.png`, `turn_caps_right_2.png`, `turn_caps_left_1.png`,
`turn_caps_left_2.png`, `turn_beanies_left.png`, `turn_other_1.png`, `turn_other_2.png`, `turn_hair.png`.
Three are kept as evidence:
[before and after, turned around](../../research/evidence/chog-traits-batch6-before-after-turned-2026-10-15.jpg),
[caps of Side=Left](../../research/evidence/chog-traits-batch6-turn-caps-left-2026-10-15.jpg),
[hair](../../research/evidence/chog-traits-batch6-turn-hair-2026-10-15.jpg).
Still wrong: the durag's side leaves stand out like ears seen from the front; the tuft in the crown reads
as a flower from straight above; the Spartan helmet (cheek guards, eye holes); the spike tip the pictures
of Bob Hair draw above the band; the curly edge of the face under the Rainbow Wig.

The sections below describe the first body, which `make_chog_body.py` still builds.

## Commands

From the root of the repository. Blender never opens its window.

```sh
B=/Applications/Blender.app/Contents/MacOS/Blender

# 1. Build the model: writes ChogBody.fbx and ChogBody.blend into .artifacts/chog-body/
$B --background --factory-startup --python tools/blender/make_chog_body.py

# 2. Measure the exported FBX against the contract (ends with an error if a point fails)
$B --background --factory-startup --python tools/blender/check_chog_body.py

# 3. Render it: eight pictures and a sheet, plus the five poses with --poses
$B --background --factory-startup --python tools/blender/render_chog_body.py -- --poses

# The same pictures in another colour, kept apart
$B --background --factory-startup --python tools/blender/render_chog_body.py -- \
    --colour purple --name chog_body_purple --out .artifacts/chog-body/purple
```

## Files

| File | What it is |
| --- | --- |
| `make_chog_body.py` | Builds the four parts and exports them. Every proportion is a named number at the top of the file: change one, run the three commands again |
| `check_chog_body.py` | Opens the exported FBX in Blender and prints the parts, their origins, the height and the triangles; then reads the raw numbers of the file and says what Unity is expected to show |
| `render_chog_body.py` | Renders the model with Eevee, one flat colour on a mid-grey ground. `--colour` takes `green`, `purple`, `cream`, `red` or `RRGGBB` |
| `.artifacts/chog-body/ChogBody.fbx` | The model for the game (64 KB) |
| `.artifacts/chog-body/ChogBody.blend` | The same model to open in Blender; the renders are made from it |
| `.artifacts/chog-body/chog_body_front.png`, `_side`, `_three_quarter`, `_back`, `_game` | The body alone, neck stub visible. `_game` is the angle of the game: 73 degrees above the horizon, slightly behind |
| `.artifacts/chog-body/chog_body_game_head.png`, `_game_front_head`, `_three_quarter_head` | The same with a real Genesis picture where the game puts it. Mock-ups made here, not captures of the game |
| `.artifacts/chog-body/chog_body_sheet.png` | The eight pictures on one sheet |
| `.artifacts/chog-body/chog_body_poses.png` | Stands, runs, attacks, wins, loses: the turns of `ChogBodyAnimator.Pose` applied to the parts |
| `.artifacts/chog-body/purple/` | The eight pictures and the sheet in purple |

## Numbers of the last run (11 October 2026)

| What | Value |
| --- | --- |
| Height | 1.200 units (neck at 1.030, then a stub of 0.170) |
| Width, with the hands | 1.449 units |
| Depth, toes to the tip of the spikes | 1.124 units |
| Lowest point | 0.000: the soles are on the ground |
| Triangles | 1,900 in all (limit 3,000): `Body` 1,050, `Head` 110, `armLeft` 370, `armRight` 370 |
| Origins, as Unity is expected to read them (x y z) | `Body` 0 0 0, `Head` 0 1.030 0, `armLeft` -0.410 0.850 0, `armRight` +0.410 0.850 0 |
| Turn and scale written in the file | 0 0 0 and 1 1 1 for every part |
| Axes declared by the file | +Y up, +Z front, +X side, one unit is 100 cm |
| Material | one, `ChogSkin` (purple in the file; the game replaces the colour) |
| Skeleton, texture | none |

In a match the game draws a body 0.85 x 1.55 = 1.32 times larger: about 1.58 units tall and 1.91
wide, under a picture 1.2 units wide.

## How it is built

- **Four rigid parts under one empty root** (`ChogBodyRoot`): `Body` (torso, legs, feet, spikes),
  `Head` (the neck stub, hidden by the game), `armLeft`, `armRight`.
- **Shoulders.** Each arm starts with a ball of radius 0.125 centred on its origin. A ball looks the
  same however it is turned, and it reaches 0.065 into the torso, so an arm leaves no hole when it
  swings. The make script refuses to export if that overlap falls under 0.03.
- **Seen from above.** The game covers the top of a hero with its picture, so the feet reach 0.45
  ahead, the hands 0.72 to each side and the spikes 0.67 behind: they stay outside the picture.
- Shapes are closed shells that cross each other (no union): the torso, each leg, each foot, each
  spike, each arm and each hand. The round shapes are shaded smooth, the spikes flat.

## Not done, or not certain

- **No import in Unity.** Nothing under `game/` was touched. What Unity will show (looking along +Z,
  left arm at -X, no turn, scale 1) is deduced from the numbers in the file and from two rules:
  Unity reads a file with the axes Maya writes (+Y up, +Z front) without turning it, and it mirrors
  X. The first is in a source below. No page read states the second word for word: it is the usual
  account, and it agrees with the third source. Only an import can confirm it. Next step: copy `ChogBody.fbx` to a
  `Resources` folder (for instance `game/Assets/Chog/Resources/ChogBody.fbx`) and open `?bodies=1`.
  If the body comes in facing backwards or lying down, the export options are in `export()` of
  `make_chog_body.py`.
- **The pictures with a head are mock-ups.** They use the numbers of `GenesisLook.Default` (picture
  1.2 wide, raised 0.55, body 1.55) and draw the game angle without perspective. The real camera,
  light, shader and ground of the game are not reproduced.
- **No UV map, no texture, no vertex colours.** One flat colour only. A textured material would need
  a UV map first.
- **Never seen at phone size in the game.** A 128 px render of the mock-up still reads as a head on a
  body with two fists and a fan of spikes; that is not a phone test.
- **Size against walls not checked.** With the hands the body is 1.91 units wide in a match. The
  game README already notes that a body larger than its collision circle overlaps walls;
  `?look=` changes the scale without a build.
- **Two things the pose sheet shows, which belong to the animator, not to the model.** From the
  game angle the raised arms of the win pose are almost hidden behind the picture. The feet are
  part of `Body`, so when the body leans (12 degrees running, 28 losing) the toes go under the
  ground: about 0.09 units at most while running, by the numbers of the animator.
- **Renders run with Eevee only**, on this Mac. Workbench was tried and dropped: no picture on the
  head and a ground that is not plain grey.

## Sources read for the axes

- Blender manual and API, `bpy.ops.export_scene.fbx`: `bake_space_transform` is marked
  experimental and "known to be broken with armatures/animations" (this model has neither).
  <https://docs.blender.org/api/5.1/bpy.ops.export_scene.html>
- "Maya uses the same coordinate space as Unity", and Unity turns a file to convert its axes:
  <https://uninomicon.com/fbx_importing>
- A character that looks along +Z in Unity looks along -Y in Blender:
  <https://www.katsbits.com/codex/?p=10417>
- A report that "Apply Transform" loses the place of child objects:
  <https://devtalk.blender.org/t/fbx-exporter-apply-transform/26640>. Checked here: the file keeps
  each part at its place under the root (see the "file:" lines of the check).

## Batch 7: the robber, the Skull and Radioactive bases (15 October 2026, night, pictures only, branch `3d-batch-7`)

Nothing of this is in the game: the game's library of parts was not written again (the export of this
branch writes to a scratch file). The owner says yes or no first.

- **The robber.** The four leaves placed by hand under the knitted mask (batch 6) read from behind as a
  collar at mid height, unlike the bare head and the plumber the owner compared them with. A knitted mask
  is pulled over the spiky head, so the whole crown of a bare head now comes through it, each leaf where it
  grows (`ROBBER_POINTS = FULL_CROWN`). The mask is lighter (14 sides, 10 ribs) so a robber in a heavy robe
  stays under 8,000 triangles. Sheet: `research/evidence/chog-traits-batch7-robber-before-after-2026-10-15.png`.
- **The Skull base** (60 Genesis, `chog_trait_bases.py`): a bald skull in the bone colour of its Skin (ten
  colours read on the pictures), no leaf, no cream face, the pink cheeks, a nose hole, a row of teeth in
  place of the mouth (no Skull Genesis has a Mouth), ribs and breastbone on the body. Hats and eyes are
  those of the other Genesis. It is in the export of this branch: all 1,929 Genesis of Origin and Skull
  built, all matching their parts (`--check 5000`), none over 8,000 triangles; a bare skull throws bones.
  Sheet: `research/evidence/chog-traits-batch7-skull-2026-10-15.png`.
- **The Radioactive base** (30 Genesis): the spiky Chog in a gradient from a deep colour at the feet to a
  light one at the top (four Skins), no cream face, the skull's teeth and nose showing, no cheek, the ribs,
  and its own eyes: the eyes of Side=Left in a glowing tint of the skin (17 values). Its values of Head are
  modelled: ram horns curling at the sides (Red Horn, Purple Horn), ridged horns (Demon, Void, devil), a
  third eye, a gem (Diamond), a gold seal (Holy Seal), a beam (Blue Laser), mushrooms (Poison Shroom). Not
  yet in the export: the game draws one colour per material; the gradient needs a colour role that the game
  computes from the height of each point (a change of `ChogPartsLibrary` and `ChogHeroModel`).
  Sheets: `research/evidence/chog-traits-batch7-radioactive-2026-10-15.png`.
- **Still wrong:** the ten 1:1 Genesis keep the earlier look; the skull's profile stays round (only its
  width narrows into the jaw); Red Bandana's band is the narrow headband where the pictures draw a wider
  scarf.
- **Red Bandana (16 October, CHOG #874 and #1192):** the band of a headband and a knot with two flaps on the
  upper front of the head (`build_bandana`); the library has it (388 pieces, 1,959 Genesis checked).
  `research/evidence/chog-traits-batch7-red-bandana-2026-10-16.jpg`.
- **The skull's jaw and hands (16 October):** the skull was the round ball of the Chog with teeth drawn on it.
  Now (`JAW`, `narrow_jaw` of `chog_trait_bases.py`) the lower head narrows by 38 percent from the
  cheekbones down, the teeth and the nose with it, as on CHOG #81, #846, #1000, #1104, #1207, #1642; the
  hands and feet are bone, not the pink mittens of a Chog (`bone_hands`), so a skeleton's arms are their own
  parts in the export (`("rest", "armLeft", "Skull")`). Front, bust, side and game views:
  `research/evidence/chog-traits-batch7-skull-jaw-2026-10-16.jpg`.
- **The branch's library, 16 October:** exported again with the horns, the jaw and the bone hands, 1,959
  Genesis built or checked (`--check 5000`), all matching their parts; the view tests of the branch pass on
  it (84). Seen in the game in a preview build made on the local network only (the game's own library
  was put back right after): skulls with bone hands and jaws, radioactive Chogs with their curled horns,
  close and from the game's camera (`research/evidence/chog-traits-batch7-in-game-preview-headless-2026-10-16.png`).
  To put batch 7 in the game after a yes: merge this branch, build (the library is already exported here).
- **The ram horns, made again (16 October):** they were small nubs beside the pictures. Now (`RAM_HORN` of
  `chog_trait_bases.py`) each is as thick at its root as a sixth of the head, rises over the crown and
  curls out to the side and down, a little to the back, its rings drawn as ridges, as on CHOG #275, #473,
  #672, #1067, #1200 (Red Horn) and #31, #1745 (Purple Horn). A first try curled them back, hidden from the
  front. Seen from the front, the side, behind and the game's angle:
  `research/evidence/chog-traits-batch7-ram-horns-2026-10-16.jpg`. #31 dressed: 7,519 triangles (limit
  8,000). The library of the branch is not exported again yet: it will be at the merge.

Batch 7 in the game, as a preview only (15 October, 23:59): the export of this branch with the Radioactive
base (format 5: its skins run from their colour at the feet to a lighter one at the top) covers 1,959
Genesis (Origin, Skull, Radioactive), all built and matching their parts, none over 8,000 triangles; only the
ten 1:1 Genesis keep the earlier look. A build made with that library on the local network only (the
library of the game was put back right after; the public address was not touched) draws them as the
Blender pictures do, the gradients included:
`research/evidence/chog-traits-batch7-in-game-preview-headless-2026-10-15.png`. To put batch 7 in the game
once the owner says yes: merge this branch, run `export_chog_parts.py`, build.

## The ten 1:1 Genesis (16 October 2026, on the branch `3d-one-of-one`, pictures and a local preview)

Each 1:1 Genesis is a character of its own and its metadata names no trait but its base and its tier.
`chog_one_of_one.py` gives each one a look read on its picture, as traits the builder knows (skin, eyes,
mouth, hat, clothing) and parts no other Genesis wears: the cape of the fire Chog (#53), the flames of
the skull (#392), the brain of the zombie (#494), the frog suit with its eyes (#973), the halo and the
wings of the angel (#1159), the gold helmet with its red gem (#1290), the horns and the trident of the
devil (#1376); The Scream (#434), the manga page in greys (#1006) and the Chog of gold (#1373) are their
colours, eyes and mouths. `make_chog_traits.traits_of` gives a 1:1 its look; `export_chog_parts.py`
exports them like any Genesis, with their body, arms and head under their own number (their hands or
cheeks are their own colours). The devil throws its trident (a new attack object); the others throw
their hat (the halo, the brain, the flames, the helmet) or a spike of their crown in their colours, and
each says it under its hero (`LINES`).

The library of this branch covers all 1,969 Genesis (1,970 with the recruit; 433 pieces, 3.2 MB), every
one built or checked (`--check 5000`); the view tests pass on it (84). Seen beside their pictures
(`research/evidence/chog-one-of-one-pictures-and-busts-2026-10-16.jpg`) and in the game in a preview build
made on the local network only, the game's own library put back right after
(`research/evidence/chog-one-of-one-in-game-preview-headless-2026-10-16.png`).

Not drawn: the painted style of #434 and its hands on its cheeks, the cracks of the skull of #392, the
stripes on the face of #1290, the screentone of #1006, the green drool of #494 (gritted teeth instead).
Tried and changed on the way: #494's brain crossed the crown (the upper and middle pairs of leaves are
left out under it, and the brain made larger); #973 had round frog eyes (the tired lids read as Pepe);
the angel's wings were too small to see and its first build went over 8,000 triangles.

This branch starts from `3d-batch-7`: merging it brings batch 7 too. To put them in the game after a yes:
merge, build (the library is exported here).

## What stands on the head no longer takes the place of the spikes (16 October 2026, on this branch)

The owner saw horns and the plunger fixed on the top of the skull where the pictures put them on the brow,
and spikes missing from the head because of it. Each case was looked at against its pictures, then turned
all around:

| Value | Before | Now |
| --- | --- | --- |
| Red Horn, Purple Horn (`RAM_HORN`) | two horns from the top of the head; the four front lines of leaves taken out | from the upper corners of the brow, rising straight (`RISE`) then curling out above the head; every leaf kept; 14 rings of 8 points along each horn (22 of 10 before: #31 went over 8,000 triangles) |
| Demon Horn, Void Horn, devil horn (`DEMON_HORNS`, new shape `DemonHorns`) | two horns on the top; leaves 0 to 2 taken out | five horns among the spikes, three on the top of the brow and one low on each side curving up, as drawn; every leaf kept |
| The 1:1 devil (#1376) | the same two horns on the top | one pair of short horns at the corners of the brow (`horns` of its entry), darker than its skin so they read |
| Plumber (`PLUNGER`) | the cup on the crown (polar 22 degrees), leaves 0 and 1 taken out | at the front of the top (polar 38), standing half upright (`UPRIGHT`), on the left of the character so the bust shows it right of the face as the pictures do, the stick out to the upper right; every leaf kept |
| Holy Seal (`HOLY_SEAL`) | a small ring lying on the top, leaves 0 and 1 taken out, 1,264 triangles | a large gold wheel standing over the brow, a lattice inside, eight rays each crossed near its tip, about 600 triangles; every leaf kept |
| Poison Shroom (`POISON_SHROOM`) | two purple caps on the top, leaves 0 and 1 taken out | three spotted mushrooms in the colours of #1100 (pale green caps spotted orange, a small orange one spotted green), before the crown |
| Blue Laser (`HEAD_LASER`) | a thin beam straight up, leaning the wrong way on the bust | a wide beam from a white glow on the brow, out to the left of the character at about 45 degrees |

The shape `RidgedHorns` and the helper that took lines of leaves out are gone: nothing on these bases takes
the place of a spike any more. Sheets: `research/evidence/chog-head-objects-horns-before-after-2026-10-16.jpg`
and `chog-head-objects-plunger-seal-shroom-laser-before-after-2026-10-16.jpg` (the picture, the bust before,
then the bust and the turns now); every value of every base in `.artifacts/chog-body/traits/batch-8/`
(`per_value_origin_1.png` to `_4`, `per_value_radioactive_1.png`, `per_value_skull_1.png` to `_3`,
`one_of_one.png`). The library exported again: 1,970 heroes, 438 pieces, 3.1 MB, 110 built and 1,859 more
checked, none failed, none lacking a part; the view tests pass on it (84); the ten seen in a local preview
build (`research/evidence/chog-head-objects-in-game-preview-headless-2026-10-16.png`), the game's own library
put back right after.

## The night of 18 October 2026: what the owner saw in the game

What the owner wrote after looking at every hero in the app on 17 October, in their order, and what was done
for each. Every change is a rule for all the Genesis it touches, measured or judged from every side, never
a patch on one picture.

### 1. The face, its eyes and its mouth lie on the head

"The faces are slightly detached from the head, a strange result, like a sheet hanging in the air." Measured
first with `measure_face_gaps.py` (a ray from fifteen points of every triangle drawn on the head back to the
faceted head, and out again to see what shows through it; six Genesis of both Sides built before the change):
the cream face stood 0.015 off the head on average (0.0185 at most), the eyes 0.031 (0.037), the mouths 0.030
(0.043), up to 0.021 and 0.030 off the face. Seen turned (the hero pages), the face was a plate with a dark
band under its edge, the teeth of hair on the brow stood in the air, and a wide grin stood out of the profile.
Two causes: every layer was laid a fixed lift off the egg of the head (face 0.012, line 0.006, cheeks 0.0216,
eyes and mouths 0.0232 to 0.0397), and the faceted head lies up to 0.007 inside that egg between its points.

- **The face** (`add_settled_face` in `make_chog_traits.py`, in place of `add_face` of `make_chog_full.py`):
  0.0038 off the egg, in triangles small enough that none sags into the head: its 24 points evenly spaced
  along its edge (`face_edge_angles`; spaced by angle, the rounded square left chords 0.15 long over the brow
  that sank 0.006 into the head) and its five rings evenly spaced over the head. Its dark line runs from under
  the face out to its width, then a skirt goes down into the head (`FACE_EDGE`), so no gap shows at its edge
  from any side; at the chin the line follows the egg (`along_egg`) where `HeadSurface.front` put it in the air.
  The teeth of hair on the brow end in the same skirt, in the skin colour, and have a row halfway so the face
  never shows through them. The cheeks and the nose lie 0.0016 over the face (`FACE_OVER`).
- **Eyes and mouths** (every Eyes and Mouth part): the layers they are drawn in follow one rule, `settled()`:
  the lowest just over the cheeks (0.0066 off the egg), each next one 0.0011 to 0.0020 higher, in the order
  they had (`DRAWN` of `make_chog_traits.py`, `LIFT` of `chog_trait_faces.py`, the third eye and the glow of
  the head laser of `chog_trait_bases.py`). 0.0011 is about ten times the step of the depth buffer of the
  game's camera at its distance. The bandana of the gangster suit and the pixel shades came down with the face.
- **Measured after** (twelve Genesis: #75, #15, #1287, #23, #103, #249, #38, #1149, #26, #45, #591, #70): the face
  0.0069 off the head on average (0.0100 at most), the eyes 0.0125 (0.018), the mouths 0.011 (0.020), at most
  0.010 and 0.015 off the face; no point of the head shows through the face, the cheeks, the teeth of hair, an
  eye or a mouth, and no point of the face through an eye or a mouth (the dark line's skirt goes into the head
  by design). Sheet: [before and after, turned](../../research/evidence/hero-3d-faces-on-the-head-2026-10-18.jpg)
  (heroes read from the game's library, the one of `main` before and the new one).
- **Checks**: library exported with `--check 5000` (1,970 heroes, 507 pieces, 3,570,687 bytes, 123 built and
  1,846 checked, 0 failed, 0 lacking a part; every hero it builds under 8,000 triangles, the largest 7,983; a first
  try with 28 points round the face put four heroes over, by 9 to 48); `sh tools/dev/view_tests.sh`: 155 passed.

```sh
B=/Applications/Blender.app/Contents/MacOS/Blender
$B --background --factory-startup -t 4 --python tools/blender/measure_face_gaps.py -- --genesis 75,1287,23
```


## 22 October 2026: the face fills the front of the head

The owner: "the cream face is drawn as a delimited oval on the front of the head and looks smaller than in the
art". On the Genesis art (looked at on 18 Genesis: the hoodies #23, #78, #85, #124, #57, #182; the costumes #16,
#143, #116, #37, #249, #32, #40; the bare heads #75 and #15; the caps #3, #4, #947) the cream face runs from the
edge of the hood, the cap or the costume opening to the other, and at the sides up to the skin of the crown; the
only skin above it is a row of teeth. In 3D it was a rounded square 0.250 from its middle (0.266 at its widest,
0.178 high) inside a head 0.300 wide and 0.250 high: a ring of bare skin 0.03 to 0.04 wide each side of the cheeks,
and 0.12 of skin above the brow. Sheet, art beside each hero, before and after, from the front, three quarters,
the side and the game camera at phone size:
[heroes-face-fill-2026-10-22.jpg](../../research/evidence/heroes-face-fill-2026-10-22.jpg)
(`make_chog_faces_before_after.py`).

- **The edge of the face follows the egg of the head** (`face_place` in `make_chog_traits.py`). The fixed oval
  (`FACE_HALF_WIDTH` 0.250, `FACE_TAPER` 0.22, both removed from `POLISH`) is now `FACE_FIT` 0.985 of the width
  the head has at each height, a rounded square of `FACE_SQUARENESS` 4.0 (3.0 before: a flatter brow), the brow
  `FACE_TOP` 0.235 above the middle of the face (0.178 before; the chin stays at `FACE_HALF_HEIGHT` 0.178 below
  it, at the bottom of the head). Seen from the front its area goes from 0.157 to 0.199 (+26 %), from 67 % to
  84 % of the front of the head; its widest half width from 0.266 to 0.295. The middle of the face
  (`FACE_MIDDLE`) did not move, and neither did the eyes, mouths, cheeks and nose, which are placed from it:
  they stay where the art puts them. The teeth of hair follow the brow (`FACE_TOP`).
- **The openings follow the face, by construction.** Every opening had numbers typed in for the old face. Now
  `opening_rim()` (`make_chog_traits.py`) finds, for each angle round the head, where the shell of the hood or the
  helmet meets the edge of the face seen from the front, pushed out by `GAP` (the dark line and the roll): the
  `RIM` of the hood (`HOOD`, `GAP` 0.022, `RIM_TAIL` for the jaw and the neck behind) and the ring of the
  astronaut helmet (`ASTRO`, `RING_GAP` 0.040). The yellow hoodie, the gangster, shark and toad suits and the
  astronaut ring get it; an entry may still give its own `RIM`. The banana and the dragon cut their volume along
  `face_place` (`MASCOT["OPENING"]`), so they widened with the face, and their collar with them. The typed tables
  are kept in comments.
- **FACE_LIFT 0.0038 to 0.0046, FACE_OVER[0] 0.0016 to 0.0008, FACE_OVER[1] 0.0046 to 0.0038.** A bigger face
  has longer quads, which sag; on the first try 4 of 3,240 measured points of the face (`measure_face_gaps.py`)
  let a point of the head show through as a speck of skin. At 0.0046 none does (#23, #37, #75, #15 measured for the face: 0.0072 off the head on average, 0.0114 at
  most; #23 and #75 for everything drawn on it: no head poke through the face, the cheeks, any eye or mouth; the
  two skin-coloured teeth that the head touches are the same colour). The cheeks stay 0.0054 off the egg, the eyes
  and mouths 0.0066 and up, as before.
- **Checked**: `-- --budget` (1,869 heroes of the base Origin built): largest #710 7,971 triangles, median 7,157,
  none over 8,000 (the face has the same rings and points, so the counts did not move), no failure and no leaf
  through a hat. Built and turned all round (front, three quarters, side, back, top, the two angles of the game)
  for 15 Genesis (hoods, the six costumes, caps, a beanie, bare heads of both Sides) and 16 more for the leaves
  and hats (#43, #21, #51, #10, #44, #12, #14, #39, #29, #17, #18, #80, #70, #275, #207, #1287): the face
  does not show above a cap or under a hat, the hoods and costumes close on it with no gap and no overlap from
  the front and the three quarters.

Still wrong, or not done: the face reaches the silhouette of the head from the front and from three quarters,
but not round the side (a pure side view shows the cream stopping at the cheek, as before): going further
round would take more rings, and the heaviest heroes are 29 triangles under the limit. At the size of a phone
from the game camera the face is mostly under the hood or the hat, so the change reads little there. On the art
of Side=Right the cream points of the brow stand up into the crown; ours are skin-coloured teeth hanging down
(unchanged). The radioactive hairline (`RADIOACTIVE_HAIRLINE`) is at a fixed height, now inside the taller
face: it is for the eyes and mouths audit. `chog_one_of_one.py` and `export_chog_parts.py` were not touched.

## 22 October 2026: the eyes and mouths against their art (lot 2)

Every value of `Eyes` and `Mouth` of the base Origin (64 rows, `make_chog_faces_audit.py`), then the Radioactive and Skull
bases, set beside the real pictures: [verdicts and what is wrong](../../research/evidence/heroes-faces-audit-2026-10-22.md),
[before and after, turned and from the game camera](../../research/evidence/heroes-faces-fix-2026-10-22.jpg)
(`make_chog_faces_before_after.py --views ...`).

- **Origin**: 48 of 64 rows faithful, 16 approximate, none wrong. Fixed: `Round`, `Round Eye` (pupil 0.033/0.039 to
  0.036/0.042 and 0.030/0.035 to 0.034/0.039) and `Dead` (stroke 0.022 to 0.030). Not fixed: the lasers (short beams by
  design, thick and slanting down on Side=Left; the beam is the attack's), `MOG`, `Chog Viper`, `BTC Glass`, `Frog`,
  `Sunset visor`, `Worried` and `Lip Bite` mouths (small differences of size and shade).
- **Radioactive (30 Genesis, 17 eye values): all wrong, fixed.** The pictures draw the eye of the plain value with every black
  line, ring and pupil in the colour of the variant (`RADIOACTIVE_EYE_INKS`: dark red, blue, green, purple), and a `Round Eye`
  with no pupil (`PUPIL: None`, `RADIOACTIVE_EYE_NUMBERS`); the builder had only tinted the white and kept everything black.
  The third eye (`3rd Eye`) is a tilted white almond in a thin dark-red outline (`almond()`), not a black oval with a pupil.
  The hairline follows the taller face (`RADIOACTIVE_HAIRLINE`).
- **Checked**: `-- --budget`: largest #710 7,971 triangles, none over 8,000; the 90 Skull and Radioactive Genesis built:
  largest 7,980, none failed.

## 22 October 2026: the dragon, astronaut and shark suits from the art (lot 3)

The owner said the three costumes were not accurate. Looked at: all 23 dragon, 24 astronaut and 25 shark Genesis (every
one is Side=Right on the base Origin; contact sheets of the 256-point pictures), and the 1000-point pictures of #16, #42
(dragon), #572, #947, #1529 (astronaut), #116 (shark). Sheet, art beside each hero, before and after, front, three
quarters, side, back and the game camera: [heroes-costumes-2026-10-22.jpg](../../research/evidence/heroes-costumes-2026-10-22.jpg).

**What the art shows, and what the 3D got wrong**

- **Dragon suit** (red-orange hood, `#f04a2a`): a hood that frames a large cream face (the face fills the opening up to
  its edge); two small dark ovals (nostrils) and dark "w" marks scattered over the top of the hood, the shoulders and
  the chest; four small blunt tan horns, wide at their foot (one at each upper side of the opening, one at each lower
  side, at the jaw); a large pale peach belly under the chin; one small bat wing with red ribs behind each shoulder; no
  tail in the frame. The 3D had six long sharp horns, wings as long as the body, four spikes on a tail, a belly cut by the
  bottom of the picture, the dark "w" marks few and thin, and a strip of the skin of the head (yellow on #16) showing in
  the slit between the face and the hood.
- **Shark Suit** (blue hood `#3ba5e3`): a large face in an opening whose whole edge is a row of white triangular teeth
  in a thick dark outline, about 20 of them: longer at the top of the opening, short at the sides and under the chin; a
  white belly; dark blue pectoral fins low at the sides of the body with fine lines. The 3D had eleven tiny teeth along the
  top of the rim only (nearly invisible), no fins, a thin thorn for a dorsal fin, and the rim a hand-typed shape.
- **Astro Suit** (24): a white suit, a round helmet closed all round the head, a thick light grey ring round the face and
  the glass inside; the crown of the head is under the glass (no spike comes out of it); a ribbed grey unit at the side
  of the helmet (the left of the picture), a ribbed hose from it over the chest, the flag patch (stars and stripes) on the
  right of the chest, a panel with a lined plate on the chest, a pack at the back. The 3D had a ring that followed the
  edge of the face and the whole crown of leaves coming out of it like a sunflower, and nothing else of the suit.

**What changed** (`chog_trait_hats.py`, `MASCOT`, `COSTUME`, `ASTRO`; the face edge is `face_place`, lot 1)

- **Dragon**: `OPENING` 0.6 to -0.3 (the rim inside the edge of the face, no more skin in the slit), four horns
  (`HORN_PLACES`) 0.085 long and 0.052 at the foot where six sharp ones were 0.115 by 0.040, wings 0.27 by 0.19 (0.46 by
  0.32), fourteen "w" marks instead of eight, drawn as flat strips (8 triangles each, not a 40 triangle tube), nostrils
  with a dark outline, the belly 0.22 by 0.19 half sizes, the tail spikes taken out (no picture shows a tail). Largest
  hero 7,784 triangles (was 7,992).
- **Shark**: `add_shark_teeth()`: 20 teeth all round the opening (the length 0.066 at the sides, 0.082 on top, 0.056
  under the chin; white inside a dark triangle), cut in three bands because a flat triangle of 0.09 sags 0.004 under
  the curve of the head and hid under the face; `add_pectoral_fins()`; the dorsal fin shorter and thicker. Heroes 6,260 to
  6,572 triangles.
- **Astronaut**: `build_astro()` rebuilt: a round ring across the way the face looks (28 stations, radius 0.046) and a
  closed shell over the back of the head behind it (`DOME`), the crown taken out (`"head": {"LEAVES": NO_CROWN}`), the ribbed
  unit (`VENT`), the flag (`FLAG`, 5 stripes and a blue field), the chest panel (`CHEST_BOX`), the pack (`PACK`), the hose
  (`HOSE`). Heroes 6,921 to 7,363 triangles (was 6,900 to 7,300).
- **Checked**: all 72 heroes of the three suits build; `-- --budget` over the 1,869 Origin heroes: largest #710 7,971, median
  7,152, none over 8,000. Each suit seen from the front, three quarters, side, back, above and from the game camera.

Still wrong: the astronaut's glass is an opaque shell (the pictures show the head through it), so from the game camera the
helmet hides the top of the head; the hose and the flag are small and barely show at the size of the game; the shark's
teeth read as a zigzag outline at the size of a phone; the dragon's wings still show as dark triangles from behind; no
picture shows the back of a suit, so the back of each (dorsal fin, pack, wings) is a choice.

## 22 October 2026: the glass of the astronaut helmet (lot 4a)

On the art (the 24 Astro Suit Genesis; twelve looked at, #249, #257, #572, #675, #708, #839, #903, #947, #1062, #1529,
#1539, #1944, at 400 points) the helmet is a clear bubble: the crown of the Chog (purple, yellow, red, pink or black hair, the skin
colour) shows through it as a flat zigzag edge, and the face is open inside the grey ring with no glass drawn over it.
Until this lot the shell behind the ring was opaque white with no crown: from the game camera, a white egg
([before](../../research/evidence/heroes-costumes-2026-10-22.jpg)). Sheet, the art beside before | after for six
astronauts (#249, #257, #572, #1529, #675, #708), from the three quarters, the side, the back, above behind and the game
camera at phone size: [heroes-astro-glass-2026-10-22.jpg](../../research/evidence/heroes-astro-glass-2026-10-22.jpg).

- **The glass is a material** (`Cloth_Astro_Glass`, `"glass": ("Glass",)` in the `Astro Suit` entry, `trait_materials(...,
  glass=)` in `make_chog_traits.py`): the property `glass` of the Blender material is exported with the light 3
  (`export_chog_parts.py`, commit b62507b), and the hero shader draws it on a share of the pixels only (0.3 where it faces
  the camera, 0.9 at its edges, lightened toward the edges). The colour is a pale blue-white, srgb 0.80/0.90/0.98. **The glass
  has no outline shell** (`add_loops(..., 0.0)`): the dark inside of an outline shell would show through the holes. The
  ring, the unit, the hose and the suit keep their outline and stay opaque. Not closed over the face: no picture draws a
  glass there, and the face stays fully readable.
- **The dome** (`ASTRO["DOME"]` 24 by 6, `DOME_SIZE` 1.32, `DOME_FULL` 55): it starts tucked in the tube of the ring and
  widens smoothly to 1.32 times the head (1.10 before), so there is room for the crown under it. A bubble this much larger
  than the head is a choice: the pictures draw a helmet only a little larger than the head, but with the room of a snug one
  a leaf that stands out of the head at all goes through the glass.
- **The crown under the glass** (`ASTRO_CROWN`, `LEAF_SIDES` 4, `SECTORS` (180,)): the three lines of leaves lying over the
  top and the back (lines 4, 5 and 6 of `SPIKES`) are kept, shortened to a share of 0.22, 0.22 and 0.26 of their length
  (standing out of the head 0.05 to 0.06, with their outline under the glass: `leaf_crossings` passes for all 24 astronauts)
  and made of four sides. The front star (lines 0 to 3) is taken out: its leaves go through the ring over the brow (tried at 0.22
  of their length: the build fails). Because the leaves are that short, most of what shows through the
  glass is the head itself in its skin colour (the hair colour of these pictures); the leaves give the zigzag edge from
  above. The check of spikes seen all around the head asks only the back sector (180) for this suit, as the glass shows no
  more.
- **Triangles**: the 24 astronauts 7,051 to 7,493 (7,141 to 7,363 for the six before: the shell lost its outline, 220
  triangles, and the leaves took them back). Library: 1,970 Genesis, 0 failures, 0 lacking a part, 3,649,354 bytes;
  `--budget` on the 1,869 Origin heroes: largest #710 7,971 (unchanged), median 7,157, none over 8,000.
- **How the sheet shows the glass**: in the renders of `render_chog_traits.py` a material with `glass` is a mix of a
  transparent shader and its colour (`glass_material`): the same two curves as the game (coverage `0.9 - 0.6 toward^2`,
  lightening `0.6 (1 - toward)^2` toward white), hashed transparency, so the sheet shows how much of the head shows through.
  It is the share the game keeps, not the game's screen pattern: the game keeps whole pixels (a third at the middle) with
  no blending. Not verified in Unity: no build was made for this lot.

Still wrong: the hose and the flag are small at the size of the game (lot 4c).

## 22 October 2026: the 8 Skull eye values never looked at (lot 4b)

The audit had looked at 17 of the 25 (eye value, Side) pairs the 90 Skull Genesis wear. The 8 left: Purple laser Left (#418,
#1647), Angry Left (#993, #1221), Happy Left (#1207, #1405), Classic VR Left (#259), Round Right (#1045), Side Eye Left (#1107),
Retard Left (#1267), cyan laser Left (#1426). Each was set beside its picture and two more pictures of the value (other
Skull Genesis, or the same value on the Origin base): [sheet](../../research/evidence/heroes-skull-eyes-2026-10-22.jpg)
(`make_chog_faces_before_after.py`, new `--more` option for the extra pictures), verdicts in
[the audit](../../research/evidence/heroes-faces-audit-2026-10-22.md).

- **Faithful**: Angry Left, Classic VR, Retard Left, Round. **Approximate**: Side Eye Left (the far eye of the picture has a
  pink iris, ours is dark), the two lasers (beams, lot 4d).
- **Wrong, fixed: Happy on Side=Left** (33 Origin Genesis and 4 Skull). The pictures draw the near eye (left of the picture) as
  a long flat stroke whose right end is higher, and the far eye as a short arch; the builder drew the two equal tall arches of
  Side=Right. `EYES_OF_SIDE["Left"]["Happy"]` (`chog_trait_faces.py`) with the new `HAPPY["EYE_ARCS"]` (an arch of its own per
  eye: half width, height, thickness, rise; `build_happy`, `make_chog_traits.py`): near eye 0.066, 0.026, 0.019, rise 0.034;
  far eye 0.032, 0.032, 0.013, no rise. Seen on #925, #1430, #1207 and #1405 only; the other Happy Left Genesis follow by the
  value. Side=Right Happy is unchanged.

## 22 October 2026: the shark's teeth, the astronaut's flag and hose at phone size (lot 4c)

Sheet at the size of the game (126 points across, the hero roughly 60 to 70 points tall by eye), then the same views enlarged, before | after, for
four sharks (#116, #139, #169, #289) and four astronauts (#249, #1529, #675, #708):
[heroes-phone-size-2026-10-22.jpg](../../research/evidence/heroes-phone-size-2026-10-22.jpg). What the camera of the game sees of
a hero is the top of the hood or the helmet, the face under it and the mittens: the head is wider than the chest, so the chest
and the arms are under it.

- **Shark teeth** (`COSTUME["TEETH"]`): 20 teeth 0.066 to 0.082 long with a thin dark edge were a fine zigzag; now 14, 0.072 to
  0.104 long, the dark triangle 1.20 times the tooth (1.10) and the white 0.76 (0.74). Read from the front and from the game
  angle as a row of teeth; at the true phone size they are still small. The pictures
  draw about 20: 14 is a departure from them, for the size of the game. Face still readable from the front (the teeth do not
  reach the eyes).
- **Astronaut hose** (`ASTRO["HOSE_OUT"]`, `HOSE_OUT_RADIUS`, `HOSE`): 0.030 thick (0.020), darker (srgb 0.34/0.38/0.50 against
  0.60/0.61/0.66), and a new first part: it leaves the tip of the ribbed unit and hangs down outside the helmet, where the camera
  sees it (on the head part, 112 triangles). The part over the chest is still under the helmet from the game camera.
- **Astronaut flag** (`ASTRO["FLAG"]`, `FLAG_LIFT`): moved from the chest, where the helmet hides it, to the top of the right
  sleeve (0.085 long, as wide as the arm, 5 stripes and the blue field at the shoulder end), where the pictures draw it at the
  shoulder. Seen between the helmet and the mitten from the front; from the game camera from behind it is hidden.
- **Triangles**: sharks 6,304 to 6,488 for the four seen, the astronauts 112 triangles more each (the hose; #249 7,271 to 7,383); `--budget`: largest
  #710 7,971, median 7,164, none over 8,000. Library: 1,970 Genesis, 0 failures.

Not done: no change makes the flag or the hose clearly readable from the game camera at the phone size, because they are the
size of a few pixels and partly hidden by design; the shark's teeth are a clear improvement from the front and at 2x, a small
one at 1x.

## 22 October 2026: the face lasers on Side Left (lot 4d)

The lasers of Side=Left (`Purple laser`, `fire laser`, `cyan laser`: 68 Origin Genesis and 3 Skull) were thick and slanted down
where the pictures draw two thin beams, level or sinking a little: [sheet](../../research/evidence/heroes-lasers-left-2026-10-22.jpg)
(12 Genesis, each beside two more pictures of its value, bust, three quarters, side, game camera, before | after; verdicts in
[the audit](../../research/evidence/heroes-faces-audit-2026-10-22.md)). The model's beam is level with the ground and the head is turned
up 20 degrees for the game camera, so from the front it slanted about 30 degrees. `LASER["BEAM_PITCH_DEG"]` (new, 0 by default) turns
the beam up from level in `build_laser` of `chog_trait_faces.py`; the three Left entries set it to 8 and `BEAM_RADIUS` to 0.019 (0.021;
the radius of Side Right). The light core (5 of 8 sides) stays: the pictures draw most of the beam in the light colour. Side Right is not
changed. Chosen by comparing the bust at 0, 15 and 8 degrees with the pictures (0: 30 down, 15: level, art: about 12 down). The beam
stays short (the game draws the long one when the hero attacks), and the slant in the game itself is that of the attack's beam, not of
this stub.

## 22 October 2026: the costumes reasoned as objects (stream B), lot 1: the dragon

The owner, seeing the suits in the game: "the work on the costumes is lacking". The method of this series: say what each costume IS
as a toy, check it on the pictures, then build that object, and judge it in the views of the game: the hero stage (the whole hero,
three quarters from a little above, close: `render_chog_traits.py --only pedestal` gives `pedestal`, `pedestal_side`,
`pedestal_back34`, `pedestal_back`), the bust, and the game camera from behind and from the front. Sheet:
[costumes-dragon-2026-10-22.jpg](../../research/evidence/costumes-dragon-2026-10-22.jpg) (6 dragons, the art, then before | after).

- **What the dragon suit is**: the hood IS a dragon's head and the face looks out of its open mouth. #16, #42, #226: the two dark
  ovals on top of the hood are the NOSTRILS at the tip of a snout, so the upper jaw comes forward over the face; the four tan pointed
  things at the corners of the opening are its FANGS (two hang from the upper rim, two point up from the lower rim), not horns (the four
  blunt horns pointing away from the face, until today, were these misread); a pale belly, dark "w" marks, a small red bat wing behind
  each shoulder.
- **Changed** (`MASCOT`, `add_dragon_head`, `build_mascot`): a rounded snout (`SNOUT`, a loft of 7 rings) over the face, its underside the
  upper lip, higher than the top of the face so it does not hide the eyes from the game camera; the two nostrils on its top (dark
  outline, hood-coloured fill); four fangs on the rim of the opening, pointing into it (`FANG_PLACES`, `FANG`); the horns are gone. The
  belly is a plate cut from the points of the volume (`BELLY`: a plate sampled between them sagged under its facets and showed as two
  patches); bat wings with three scalloped fingers and dark ribs (`WING_EDGE`), tilted back so the game camera sees them from above
  (upright they were a line); a row of six small plates down the back from the head to the tail (`RIDGE`, a choice: no picture shows
  the back, it makes the dragon read from behind, which is the match camera). The material `Horn` is now `Fang`.
- **Checked**: 6 dragons rendered before | after (bust, pedestal, side, back, game camera behind and in front); `--budget`: largest
  hero #710 7,971, none over 8,000, the dragons 7,536 to 7,756 (the heaviest part, the dragon's body, 2,896 with its head and arms); library
  1,970 Genesis, 0 failures, 0 lacking a part, 3,650,451 bytes.
- **Not done / not verified**: nothing is seen in Unity; the hero shader's light on the new wing and ridge (flat colours) is not checked.
  New in `render_chog_traits.py` (shared): the `pedestal` family of views (`PEDESTAL_VIEWS`).
## 22 October 2026: the face on the home stage (lot 5a)

The owner, looking at the recruit on the pedestal (three quarters, close): the bottom of the face does not reach the neck, the forehead is too big,
"the face is too high", and little white specks on the face features. Sheet, seven heroes (the recruit, #75, #76, #15, the hoodie #23, the cap #85,
the dragon #143) on the stage of the home screen, bust, side and game camera, before | after, and a close crop of a cheek:
[heroes-face-fill-stage-2026-10-22.jpg](../../research/evidence/heroes-face-fill-stage-2026-10-22.jpg). New: the stage view of `render_chog_traits.py`
(families `stage`: the camera of `ChogHeroStage.cs`, 24 degrees of field, 10 above the horizon, the hero turned 42 degrees, full body and close, drawn
with the look of the game: `game_look_material` emulates the matcap and the rim of `ChogHero.shader` with the numbers of `MatchLookTuning.cs`, and
no shadow), `render_recruit.py --size`, `make_chog_stage_sheet.py` and `check_face_overlaps.py`.

- **Forehead and chin, from the art.** Measured on the bare Genesis (#76, #75, #112, #237, #432 at 1000 points): from the eyes up to the middle of the hair
  line is about 0.9 of the distance from the eyes to the mouth, and from the eyes down to the chin about twice that. `FACE_TOP` 0.235 to 0.125 above the
  middle of the face, the teeth of hair 0.040 long (0.050), `FACE_HALF_HEIGHT` 0.178 to 0.196, which puts the chin 0.246 under the middle of the head, 0.004
  from the lowest point of the egg: the cream now wraps under the chin down to the neck (the face reaches the bottom of the head, as the art draws it).
  Hoods and costumes follow the face by construction (`opening_rim`).
- **The specks inside the cheeks: a layer too close, not a clip of the face with the head.** The cheek is a fan of 12 triangles lying 0.0008 over the face
  (`FACE_OVER`); its flat triangles sag under the egg between their corners, and those of the face do too, a little differently. Measured on 10,400 points
  of the two cheeks of #75 (`check_face_overlaps.py --decals`), the cheek stood 0.00014 over the face at its lowest; a pixel here and there of the stage
  picture showed the cream (found by probing that pixel, `CHOG_PROBE`: the face 0.00017 in front of the cheek). Not a z-fight at 24 bits (the step at the stage
  distance is 0.000003), a real sag. Now `FACE_OVER` (0.0020, 0.0052) and every layer drawn above 0.0014 higher (`settled()` in `make_chog_traits.py`,
  `LIFT` in `chog_trait_faces.py`), which keeps the order of the layers: lowest point over the face, cheeks 0.00136, eyes 0.0027, mouth line 0.0034
  (before 0.00014, 0.0013, 0.0020). Floating: the largest drawn height is 0.0093 over the face for a glint; nothing is seen to float from the side.
  The head skin never comes through the face (`measure_face_gaps.py`: head pokes 0).
- **The white along the edge of the face is the rim of the game's shader**, not geometry: it adds a warm white (`FFF6E0` times 0.35, edge cubed) at the
  edges that face the key, and on the already light cream of the face it saturates to white along the silhouette. Emulated in the stage pictures; tried a
  rim added as a screen blend (`rim * (1 - colour)`, `CHOG_RIM_MODE=screen`), which removes it. A shader change, not mine: proposed in the report.
  Meanwhile `FACE_FIT` 0.985 to 0.96 leaves the most grazing part of the face to the skin.
- **Checked**: 35 Genesis (hoods, costumes, caps, bare heads of both Sides) build with no failure; `--budget` largest #710 7,971, median 7,165, none over
  8,000; library 1,970 Genesis, 0 failures. Not verified in Unity.


## 22 October 2026: the costumes reasoned as objects, lot 2: the shark

Sheet: [costumes-shark-2026-10-22.jpg](../../research/evidence/costumes-shark-2026-10-22.jpg) (6 sharks: #116, #139, #289, #169, #1966, #215;
the art, then before | after in the same views as lot 1).

- **What the Shark Suit is**: the hood IS a shark's head and the face looks out of its open mouth: a row of white triangular teeth with
  black outlines on the rim of the opening (#116: about 8 on top pointing down, 6 at the bottom pointing up), none across the face. The
  owner: "understand that the costume IS a shark; we do not care whether teeth show on a phone". The body is the same animal: a white
  belly under the chin (#116) and a dark blue pectoral fin low at the side of the body, nothing on the arms.
- **Changed** (`COSTUME`, `add_shark_teeth`, `add_snout`, `add_dorsal_fin`, `add_tail_fin`, `add_back_fin`, `add_pectoral_fins`,
  `add_shark_head`): the teeth are 14 small white blades with an outline shell, each made on the rim and pointing into the opening (the
  old ones were flat triangles with a larger dark triangle behind each: a ring of fine dark wedges that read as the rays of a sun; the
  owner saw it too); a rounded snout over the face (the hood's top coming forward, as the dragon's); a tall dorsal fin on the top
  and back of the hood, thicker (the main read from above), a smaller fin on the back of the body, a tail fin behind the lower back
  (crescent, standing), small shark eyes on the sides of the hood and three gill lines behind each (a choice: no picture shows the hood
  from the side; they help it read as a shark); the pectoral fins moved from the height of the arms (0.46, where they read as wings) to
  the hip (0.30), behind the arm; a broad white belly (0.28 wide, from 0.13 to 0.57; it was a stripe 0.17 wide) cut a little off the
  suit (0.010) so it does not sag into the shirt.
- **Checked**: 6 sharks before | after; `--budget`: largest hero #710 7,971 (not a shark), none over 8,000, the sharks 6,912 to 7,120;
  library 1,970 Genesis, 0 failures, 0 lacking a part, 3,660,417 bytes.
- **Not done / not verified**: not seen in Unity. The dorsal fin is seen edge on from the game camera from behind (a line, with its
  outline); the teeth are small at the true phone size and the snout hides the upper ones a little from the game camera in front.

## 22 October 2026: the costumes reasoned as objects, lot 3: the astronaut

Sheet: [costumes-astro-2026-10-22.jpg](../../research/evidence/costumes-astro-2026-10-22.jpg) (6 astronauts: #249, #572, #1529, #947, #675,
#708; the art, then before | after).

- **What the Astro Suit is**: a space suit with an opaque white helmet over the whole head. #572 and #1416: a thick grey ring frames a
  round visor opening; through it show the face and the fringe of the Chog's hair (its colour, a zigzag edge) above the face, inside the
  ring; the hair stays in the helmet. Around it: a ribbed unit at the side of the helmet, a ribbed hose, the collar, the chest box, the
  pack, the flag at the shoulder. The owner, on the glass of lot 4a: "I don't understand why you put transparency and grain, it's useless,
  just make an astronaut costume."
- **Changed** (`ASTRO`, `build_astro`, entry `Astro Suit`): the glass is gone; the helmet shell is opaque white (`Helmet`) with an outline,
  1.20 times the head (1.32 for the glass); no leaf of the crown (`NO_CROWN`), the fringe of the base stays (`FRINGE`); the legs are white
  trousers over the skin and grey boots (`Boot`) over the feet (until now purple legs and pink feet came out of a white suit; `add_trousers`
  and `add_boots` take a number of sides). The ring has 18 stations (28) and the helmet 20 by 5, the trousers and boots 10 sides, for the
  budget. The glass mode of the hero shader and of the renders (`glass_material`) is kept, unused.
- **Checked**: 6 astronauts before | after; `--budget`: largest hero #710 7,971, the astronauts up to 7,907 (#1855), none over 8,000 (the first
  try, 28 ring stations, had ten over).
- **Not done / not verified**: not seen in Unity. The gloves are still the pink mittens of the base (no budget left for a white glove);
  the flag and the hose are small at the size of the game, as before.
## 22 October 2026: the laser eyes on Side Right, frowning almonds (lot 5b)

The pictures of Side=Right lasers (#52, #102, #107, #41, #72, #16, #42, the Skull #121, #373) draw each eye as a dark almond a little wider than high whose top is cut
by a flat lid sloping down towards the nose, the beam leaving its middle; the builder drew a whole tall oval (0.090 by 0.128). `draw_laser_eyes`
(`make_chog_traits.py`) now cuts the oval by a lid: `LASER["EYE_SIZE"]` (0.070, 0.064), new `EYE_LID` (0.28, 0.40: where the lid cuts, against the half height, and how much
it rises per unit outwards), 20 segments (14), eye middle 0.006 above the middle of the face (0.010). 450 Origin Genesis and 12 Skull Genesis wear them. Sheet, 8 Genesis,
stage, stage close, bust, side and game camera, before | after beside the art: [heroes-lasers-right-2026-10-22.jpg](../../research/evidence/heroes-lasers-right-2026-10-22.jpg).
Checked: library 1,970 Genesis, 0 failures; `--budget` largest #630 7,981 (was 7,969), none over 8,000. Not exact: the near eye of the pictures is larger than the far one
(perspective); ours are equal. The beam still hides most of the lower part of the eye from the front, as it does on the pictures.

## 22 October 2026: the tears mirrored (lot 5c)

The owner on the banana #321 (Round Crying): "why are the eyes and tears in the same direction?". The tears were placed with a signed offset toward -X for both eyes (as the
three quarter pictures show them, on the left of each eye), which puts one on the outer side of its eye and the other on the inner side: seen from the front it reads as a
mistake. `tear()` of `chog_trait_faces.py` now takes the side of its eye: the sideways numbers `FROM[0]` and `DRIFT` of `WIDE_TEARS`, `THIN_TEAR` and `CLOSED_TEARS` are
magnitudes laid on the outer side of each eye, mirrored (the light streak of a wide tear lies on the inner side of each). Wide tears start 0.024 off the middle of the eye and drift
0.008 outwards (0.008 and 0.004 before, towards -X); closed-eye tears 0.020 and 0.008 (0.004 and 0.004); thin tears 0.040 and 0.008 as before but mirrored. Values: Round Crying
(69 Genesis), Round Tear (65), Closed Eye Crying (57), all Origin on Side Right (there are no tears on Side Left or on a Skull); `Teary` (54) has no tear drawn. Sheet,
8 Genesis (#321, #6, #39, #92, #27, #21, #18, #76) on the stage, close, bust, side and game camera, before | after beside the art:
[heroes-tears-2026-10-22.jpg](../../research/evidence/heroes-tears-2026-10-22.jpg). Checked: library 1,970 Genesis, 0 failures; `--budget` largest #630 7,981, none over 8,000 (no
triangle changed). The pictures themselves draw the tears on the left of the picture for both eyes: ours follow the owner's rule, not the three quarter drawing.

## 22 October 2026: lot 5, the owner's review of the deployed costumes

Owner on the hero stage: dragon "almost perfect", astronaut "very good", shark "much better but still messy". One sheet, the art then
before | after (front close, three quarter, game camera behind and in front):
[costumes-lot5-2026-10-22.jpg](../../research/evidence/costumes-lot5-2026-10-22.jpg) (#116, #139, #16, #226, #25, #32, #242, #249, #572).

- **Shark, tidied** (`hood_swell`, `add_shark_teeth`; entry `hood_numbers`): the head is one smooth volume, the snout a swell of the hood itself
  (`SWELL`: 0.085 forward, 0.030 up, a Gaussian over the front of the top; the hood on 24 sides), no separate disc (the beret).
  The teeth: 8 on top and 6 below, all of one size and spacing, feet in the roll of the hood rim (upper row) or on the upper edge of the band
  that closes the hood under the chin (lower row, so no more necklace on the chest), tips on the face, pointing in; none past the silhouette
  of the hood (the first ones were placed on the edge of the face, which is not the edge of the hood).
- **Dragon, its eyes** (`MASCOT["EYES"]`, `add_dragon_head`): a half sunk ball with an outline on the hood each side of the snout, a dark slit
  on its front facing ahead and up; read from the front, three quarters and the game camera (from above they make the face of the dragon).
  A plush dragon onesie has them there; no picture shows the top of the hood, so this is a choice.
- **Toads** (`toad()`, `build_frog_suit` of `chog_one_of_one.py`, shared with #973 which keeps its own `FROG_EYES`): the two eyes at the two
  upper corners of the hood, 43 degrees each side of straight up (17 before), as #25, #32 and #242 draw them.
- **Astronaut, the hose** (`build_astro`, `HOSE_WAY`, `HOSE_CHEST`): one ribbed tube, a smooth curve, from the tip of the side unit down beside the
  helmet, over the shoulder and across the chest into the top of the chest box: both ends connected (the hanging piece is gone). Built on the head
  from points of the rest pose. To stay under 8,000: ring 16 stations (18), unit on 6 sides (8), hose on 5 sides, dragon eye on 3 stacks.
- **Checked**: 9 Genesis before | after; `--budget` largest #630 7,981, none over 8,000 (the dragons with the eyes were four over by up to 47).
  Not verified in Unity. The astronaut's hose is thick and ribbed at the size of the game and the pupil of the toad is still round (the art draws a
  rounded bar).
## 22 October 2026: the face takes the room it needs, to the sides and to the chin (lot 6)

The owner on #76 seen from the front on the stage: the forehead is good, but at the sides and at the bottom the face does not take all the room. The pictures
(#76 at 1000 points, #75, #112, #237, #432) fill almost the whole front of the head with the cream: the skin shows only as the zigzag on top and a thin band at the
sides, the cheeks sit near the outline and the chin is the bottom of the head. The cause was the shape of the edge: a rounded square cut its width with the same squareness
as its height (`FACE_SQUARENESS` 4), so below the cheeks the edge left the outline of the egg and left a triangle of skin in each lower corner (from the front, a band of
skin about a fifth of the head wide beside the cheeks and under the face). `face_place` (`make_chog_traits.py`) now gives the width its own squareness,
`FACE_SQUARENESS_X` 20, which makes the edge follow the outline of the egg all down the sides to the chin; `FACE_FIT` 0.96 to 0.99, `FACE_HALF_HEIGHT` 0.196 to 0.198 (the chin 0.248
under the middle of the head, 0.002 from its lowest point), seven teeth of hair 0.080 wide (five of 0.088) so that the fringe still covers the brow, which is wider. The forehead
(`FACE_TOP` 0.125) and the eyes and mouths, placed from the middle of the face, are untouched. New: the `stage_front` picture (the stage camera from straight in front) in
`render_chog_traits.py`, `--views` in `make_chog_stage_sheet.py`, `--head-set` in `measure_face_gaps.py`. Sheet, 9 heroes (the bare #76, #75, #112, #237, #432, the hoodie #23, the shark #116, the dragon
#16, the astronaut #249), the front view, three quarters, the stage, the bust and the game camera, before | after beside the art:
[heroes-face-wide-2026-10-22.jpg](../../research/evidence/heroes-face-wide-2026-10-22.jpg). The hoods, the shark's teeth, the dragon's opening and the astronaut ring follow the edge of the
face by construction (`opening_rim`) and still close around it on the sheet.

- **Checked**: 37 Genesis (hoods, costumes, caps, bare heads) build with no failure; library 1,970 Genesis, 0 failures; `--budget` largest #630 7,991 (7,981 before: the two more teeth of
  hair), median 7,175, none over 8,000, **9 triangles under the limit**. `check_face_overlaps.py --specks` on #76: the face is nowhere in front of a drawn thing. `measure_face_gaps.py` on #76:
  the head still pokes through the face at 41 of 3,240 points along its top edge (0 before this lot) whatever the lift (0.0046, 0.0065) or the number of points (24, 32): under the teeth of hair, no
  skin is seen in the cream in the pictures at the stage or in the bust; not understood, left open.

## 22 October 2026: the top of the face meets the fringe (lot 7)

The owner on the recruit (front, close, stage): above the zigzag fringe a band of the skin of the head with a jagged lower edge shows over the cream, across the forehead, "the head goes through the
face". It is the open item of lot 6 (`measure_face_gaps.py`: the head through the face at 41 of 3,240 points on #76), and it is visible in the game. **Cause**: with the width squareness of lot 6
(`FACE_SQUARENESS_X` 20) the edge of the face went straight up the sides and then crossed the whole brow in the last degree of its angle, so no point of the edge lay along the brow: a gap of 0.3 between
two edge points (`face_edge_angles` printed x 0.167 then -0.132 at the top, not even symmetrical), and the flat triangles of the face's top ring that span it sag under the dome of the head by more than
`FACE_LIFT` (0.0046). Neither the lift (0.0065) nor more points (32) fixed it, because the points still did not fall along the brow. **Fix** (`make_chog_traits.py`): `FACE_SQUARENESS_X_TOP` 8, the width
over the brow is rounded off more than at the cheeks (20 stays below the cheeks, so the sides and the chin of lot 6 are kept); the top edge now has points every 0.09 or less
(x 0.185, 0.128, 0.090, -0.013, -0.090, -0.128, -0.185); and `FACE_TOP_LIFT` 0.002, a little more lift along the brow (growing with the square of the ring, the fringe lifted with it), as a margin.
`measure_face_gaps.py`: 0 head pokes on the recruit (`--genesis 0` builds the base) and on #76, #75, #112, #237, #432, #23, #85, #116, #16, #249, #15, #1287, #3, #4, #947, #43, #21, #51, #10 (the face is the
same on every one of them), and 0 of 49,896 face points on #76 and #23 with the new `--dense 20`. Sheet, the recruit, #76, #75, #112, #23, #116, #16, #249, front close, three quarter close, stage and bust, before | after,
and an enlarged crop of the forehead of the recruit and of #76: [heroes-face-top-2026-10-22.jpg](../../research/evidence/heroes-face-top-2026-10-22.jpg). Library 1,970 Genesis, 0 failures; `--budget` largest #630 7,991
(unchanged), none over 8,000.

## 22 October 2026: lot 6, the clown nose

Owner on the hero stage (a pink Genesis with heart eyes, #210): "the red nose looks weird, like a button". Sheet, 7 Genesis with different eyes and
mouths, the art then before | after (front close, three quarter close, bust, hero stage, game camera behind and in front):
[costumes-clown-nose-2026-10-22.jpg](../../research/evidence/costumes-clown-nose-2026-10-22.jpg).

- **The art** (#210, #248, #246, #195, #640, #741, #920, #1431, #1677, #1797, #1806, #1807): a round red ball with a dark outline and a white glint,
  ON the nose of the face, nothing flat around it. With the mouth "wo" the small dark mouth sits right under the ball, touching it.
- **The cause**: the ball stood 0.022 off the face with an outline shell; from the side the shell showed as a dark crescent and a thin ring on the face,
  a button's rim, and the shell of a ball that is not on the face cannot be a uniform outline.
- **Changed** (`CLOWN_NOSE`, `build_clown_nose`): the ball is half sunk (its middle 0.004 off the face, radius 0.052, 12 by 6), no outline shell; the
  outline is a dark patch of the shape of the face laid under it, 0.0075 wider than the ball (`Edge`, the layer of the drawn dark lines), so the ball
  meets the face with one clean dark line all round; the glint is kept. The mouth is NOT hidden or moved: the art draws it there; at the stage it is
  the same small dark oval under the ball as on #195 and #210.
- **Checked**: 7 Genesis before | after; `--budget`: largest #630 7,991 (it wears the nose; +10 triangles), none over 8,000; library 1,970 Genesis, 0 failures.
  Not verified in Unity.

## 22 October 2026: visible eyes, lasers, glasses and mouths (lot 8, batch 1)

The owner's review of the heroes in the game, and then: "when I talk about eyes I mean the visible ones: many are not exact" (the toad #242, Smirk on Side Left). The audit of
lot 2 graded 48 of 64 rows faithful; it compared each value against two or three pictures at a glance. It is redone strictly: each eye of a pair is compared by itself (the near and the far
eye of the pictures are often two different drawings), at the stage view the owner looks at (front close and three quarter close). Batch 1: Smirk Left and the five most worn values (Happy,
Round, Dead, Normal Eye, Heart, all on Side Right). Sheet, 13 Genesis, the art beside the front close and the three quarter close, before | after:
[heroes-eyes-batch1-2026-10-22.jpg](../../research/evidence/heroes-eyes-batch1-2026-10-22.jpg).

- **Smirk, Side Left (29 Origin Genesis): wrong, redrawn.** On #242, #111, #441, #2, #499 the two eyes are two drawings: the near one (left of the picture) a wide almond tilted so that its
  outer end is low, a thick black lid arch and a large black pupil on the left with white on the right, one glint at the top left; the far one a small tall narrow oval, nearly all pupil, two glints.
  We drew two equal almonds. `WHITE_EYE` takes a new `EYES` table (numbers of one eye by its side, laid over those of both) used by `SMIRK`: near eye 0.082 by 0.043, tilt -18, pupil 0.048 by 0.044;
  far eye 0.034 by 0.052, tilt 10, pupil 0.030 by 0.044. Graded approximate: the near eye's lower lid curve and the far eye's lean are not exact.
- **Happy, Round, Dead, Normal Eye, Heart, Side Right: approximate, made closer** (not faithful, said so): the closed arches of Happy are 1.6 times thicker and the eye at +X wider than the other (`HAPPY`
  `EYE_ARCS` by side); the Round eyes taller ovals (0.058 by 0.070 dark disc, was 0.060 by 0.066: the pictures' are 0.75 as wide as high); the Dead crosses bolder (stroke 0.036, arm 0.050); the
  Normal Eye larger (0.050 by 0.063, was 0.043 by 0.060) with larger glints; the Heart 1.17 times larger (half width 0.070). What still differs: the near eye of the pictures is smaller than the far one
  (perspective) and ours are equal; the Normal Eye lid cut is not the slope of the pictures; the dark outline weights are close but not measured.
- **Retro Glass (and the round red glasses, 56 Genesis)**: the eyes under the glasses were round open irises where the art draws bored, frowning ones: the iris is now larger (0.068 by 0.066), pale blue, cut by a
  heavy lid that slopes down towards the nose (`RETRO_EYES["LID"]`, `under_lid()`). Not yet audited: Sunset, Plasma and Gradient visors, BTC Glass, 3D Glass, MOG, Chog Viper, VR, Thug (next batches).
- **Lasers start from the middle of the eye** (`draw_laser_eyes`, `lidded_eyes`): the beam and its hot spot left the middle of the whole oval, but the lid takes the top off, so the visible eye is lower and the beam
  seemed to leave its inner corner; it now leaves the middle of the area of what is seen (`centroid()`), on Side Right and, with the same rule, on Side Left and the Skull (the Left beam no longer starts 0.25 of the
  half height lower than the middle).
- **Mouths centred on the face** (`centre_on_face`, `measure_mouth_centres.py`): measured, the smile, the flat mouth and the others stood 0.040 to 0.052 off the middle line of the face (Side Left `normal smile` +0.048, `Sad` +0.052,
  Right `Flat` +0.050, `Grit` +0.041, `Smile` +0.040, Bubblegum -0.022 / +0.022), because they were placed where the three quarter pictures draw them against the nose. Each mouth part is now moved so that the middle of its extent
  lies on the middle line, back on the surface of the head (all 36 Side/value pairs now within 0.004 of the middle). Not done: comparing each mouth's shape with the art beyond the coin.
- **Coin mouth ("Mon coin", "BTC Coin", Russian Hat #34, #790, #1185, #25)**: the coin held at the left end of the teeth lower than the pictures: `COIN_AT` (-0.006, -0.092), `COIN_OFF` down 0.008 (0.020 before). Still a little low and the grin
  narrower than the pictures'. Sheet of the lasers, the glasses and the coin: [heroes-lot8-lasers-glasses-coin-2026-10-22.jpg](../../research/evidence/heroes-lot8-lasers-glasses-coin-2026-10-22.jpg).
- **Checked**: library 1,970 Genesis, 0 failures; `--budget` largest #630 7,991 (unchanged, 9 under the limit), none over 8,000. Not in a Unity build.
## 22 October 2026: lot 7, the owner's review of the heroes (turning them on the hero stage)

### 7.1 The Russian Hat (ushanka)

Sheet: [costumes-ushanka-2026-10-22.jpg](../../research/evidence/costumes-ushanka-2026-10-22.jpg) (9 Genesis of both Sides, the art then before | after: front
close, three quarter, side, hero stage).

- **What the art draws** (67 Genesis, #39, #58, #90, #148, #166 on Side=Right, #34, #91, #137, #144 on Side=Left): a tall crown of fur, a cylinder
  standing high above the head, its lower edge a thick wavy roll across the brow (the eyes just under it), with two big ear flaps hanging
  beside the face to the jaw: fur on Side=Right, dark leather with two snaps on Side=Left, edged with fur. No teeth of hair under the fur.
  The old hat was a low cap with a thin roll and flaps laid on the head.
- **Changed** (`USHANKA`, `build_ushanka`): the crown is an oval cylinder 0.35 by 0.275 half wide, its lower edge following the new face (the
  face top is 0.29 above the neck now: 0.292 at the front, higher at the sides, 0.17 behind), flaring from the head over 0.2 of its height as a
  roll of fur (the lower three bands in `Fur`, 8 bumps), a barrel of 6 per cent, its top at 0.52 with a rounded dome 0.07 higher
  (the old crown was 0.10 above the head); the flaps hang nearly upright beside the face (0.33 to 0.34 out) from 0.33 down to -0.075, with
  their fur rim and snaps on Side=Left; the teeth of hair on the brow are off (`FRINGE`). The leaves under the rim (`USHANKA_RIM_ROW`) follow.
- **Roll of fur kept visible** (the coordinator, after the first try: on Side=Right the roll was swallowed from the front, a plain tan cylinder): the roll
  is taller (the lower 0.36 of the wall, 4 bands), swells out of the wall by 11 per cent (`ROLL`), its fur bumps are deeper and the lower edge rises
  and falls with them (`WAVE_Z`), and on Side=Right the fur colour is lighter (0.94 0.83 0.70) against the crown (0.78 0.56 0.41). The flaps have 4
  stations (5): #608 was 8,028 triangles.
- **Checked**: 9 Genesis before | after; `--budget`: largest #630 7,991, the hat heroes up to #608 7,932, none over 8,000; library 1,970 Genesis, 0 failures.
  Not verified in Unity. The top of the hat is hidden by the frame of every picture of the art, so its height is estimated from the brow line to the top
  of the frame (at least 0.7 of the face).

### 7.2 The clown nose, a tiny bit more out

[costumes-clown-nose-out-2026-10-22.jpg](../../research/evidence/costumes-clown-nose-out-2026-10-22.jpg) (#210, #248, #195, before | after): the owner liked the sunk ball of
lot 6 and asked for it to stick out a tiny bit more: its middle is 0.014 off the face (0.004), the radius, the glint and the outline patch unchanged.

### 7.3 and 7.8 The headbands: on the art's brow line, and nothing through them

Sheet (both items, one lot): [costumes-headbands-2026-10-22.jpg](../../research/evidence/costumes-headbands-2026-10-22.jpg) (Red Headband #12, #984, #416, #1170; Japan
Headband #65, #378, #789, #87; Durag #51, #121; Red Bandana #874, #1192; the art then before | after: front close, three quarter, side, hero stage).

- **The art** (34 Red Headband, 31 Japan Headband Genesis, #12, #416, #984, #1170, #65, #87, #378, #789): the band crosses the brow with its lower edge on the top of the eyes, the
  crown of spikes standing above it. The face was lowered on 22 October (its top 0.29 above the neck, 0.40 before) and the bands, tuned on the old face,
  stayed: a strip of face between the band and the eyes, the band riding up into the hair.
- **Lowered** (`HEADBAND["LOWER"]`, `["UPPER"]`): lower edge 87 degrees at the front (82), 85 at the sides, 81 behind; upper edge 58 (50) at the front. The Red Bandana
  (#874, #1192, the band of `build_headband` and its knot) follows. The Durag (50 Genesis, and the bandana-like cloth): its front edge was on the old brow (66 degrees),
  leaving a strip of forehead and the teeth of hair above the eyes; the art draws it just above the eyes: 82 degrees at the front and the middle, and the points
  of hair that came out under it are off (`DURAG["RIM"]`, `HAIR_POINTS`).
- **Nothing through the band**: until now the whole crown stood and its crossings with the band were only noted (`THROUGH`): the upper pair, the
  middle pair, the lowest pair and the back row all went through it (#12: leaves 1 to 6). Each line was tried alone against the new band (`leaf_crossings`): the top
  leaf clears it; the upper pair clears it at 24 degrees from straight up (38); the middle pair cannot (gone); the lowest pair clears it at 130 degrees, brought out
  below; the top of the back row at a lean of 36; the pair of the back row at 125 degrees and 40 back; the leaf straight back at 104 (`BAND_CROWN`). `THROUGH` is off for both
  values: the 65 Genesis build with no leaf crossing.
- **Checked**: 12 Genesis before | after; 65 headband and 98 durag, bandana, head wrap and wig Genesis build; `--budget`: largest #630 7,991, none over 8,000; library 1,970 Genesis, 0
  failures. Not verified in Unity. The crown above a band is narrower than the art's (the middle pair is gone, the upper pair is turned in).

### 7.4 The Spartan Helmet: the spikes out of its rear edge

Sheet: [costumes-spartan-2026-10-22.jpg](../../research/evidence/costumes-spartan-2026-10-22.jpg) (8 of the 40 Genesis, the art then before | after: front close, three quarter, side, back
three quarter, game camera behind, hero stage).

- **What the art draws** (#21, #331, #1081, #1536, #1903, #49, #430, #990): a gold helmet with a nose guard and cheek pieces, a red plume; the Chog's spikes come out of the
  rear edge of the helmet on the far side, three or four stacked, pointing out and back, so the Chog is recognisable.
- **What was wrong**: the shape's own leaves were replaced by a row planted under the real rim of the shell (`USHANKA_RIM_ROW`), all round the back and pointing down: a
  collar of spikes hanging at the nape like the legs of a bug (the owner: two spikes pointing down at the back).
- **Changed** (`SPARTAN["LEAVES"]`, `HEAD_VALUES`): the value takes the leaves of the shape again: the lowest pair (92 degrees, 55 back) and the pair of the back row
  (80 degrees, 70 back), each 1.45 times a leaf of the crown, their points bending up; the leaf straight back is gone (it hung down the nape whatever its lean, the head being turned
  up by 20 degrees: no leaf pointing back can clear the rear edge of the shell and point up). Each was tried alone against the shell (`leaf_crossings`): out of the
  rear edge, none crosses it. `SECTORS` (135, -135) asks a tip to show on each side from above.
- **Checked**: 8 Genesis before | after; the 40 Spartan Genesis build; `--budget`: largest #630 7,991, none over 8,000; library 1,970 Genesis, 0 failures. Not verified in Unity. From
  straight behind the leaves still overlap into a fan at the nape (they point back, the head being turned up); the art has no view from behind.

## 22 October 2026: strict eye audit, batch 2, and a second pass on Smirk (lot 8)

Same strict rule as batch 1 (each eye by itself, at the stage view, at least three Genesis per value). Sheet, 18 Genesis, the art beside the front close and the three quarter close, before | after (the
before pictures were rendered from a temporary copy of origin/main and not kept): [heroes-eyes-batch2-2026-10-22.jpg](../../research/evidence/heroes-eyes-batch2-2026-10-22.jpg).

- **Smirk Left, second pass.** On #242, #111, #441 the near eye is a long flat almond, about 2.4 times as wide as high, nearly all black (the pupil fills it) with a crescent of white at the top right under a heavy lid: it
  is now `SIZE` 0.088 by 0.037, tilt 12, pupil 0.078 by 0.036, heavy lid, so it reads as a different shape from the small tall far eye at the stage close view. Approximate: the lower line of the lid is simpler than the drawing's.
- **Round Crying, Round Tear, Teary (188 Genesis), `GLOSSY`: wrong shape, redrawn.** The shiny eyes of the pictures (#6, #39, #125, #92, #213, #260) are tall ovals about 0.7 as wide as high with a thin white ring round a black pupil that
  fills nearly all of it, one large oval glint up from the middle and a small one low; ours were round discs 0.94 as wide as high. Now ring 0.060 by 0.082, white 0.050 by 0.073, pupil 0.045 by 0.067. Approximate: the near eye is smaller than the far one in the pictures.
- **Side Eye, Right (66): approximate, made closer** (#97, #179, #258, #47, #86): larger (0.060 by 0.066), the lid cutting at 0.30, one large upright glint instead of two small ones. Entry-level `numbers`, so Tired, the clown and Side Left are unchanged.
- **Anime Glass, Right (64): approximate, made closer** (#3, #98, #157, #236, #199): lenses 0.092 by 0.090 half sizes (0.104 by 0.074), squareness 2.6 (3.2): about as high as wide and rounded, not rectangles; entry-level numbers, so Retro Glass, BTC and 3D Glass are unchanged.
- **Fire, Green and Purple laser eyes, Right (194): approximate** (#38, #135, #212, #72, #104, #107, #52): the frowning almond of lot 5b is right in shape and place; nothing changed in this batch. From the front the beam is seen end on as a vertical bar, as in the pictures.
- **Checked**: library 1,970 Genesis, 0 failures; `--budget` largest #630 7,991 (unchanged, 9 under 8,000), none over. By eye on the sheet only: no measurement of eye sizes against the art beyond the proportions read above; not in a Unity build.
- **Left to do**: the rest of the strict audit, in the order of `heroes-faces-audit-2026-10-22.md` (Gradient visor, Round Eye, Closed Eye Crying, Tired, Sunset and Plasma visors, then Side Left).

### 7.5 The dragon's belly, from the side

Sheet: [costumes-dragon-belly-2026-10-22.jpg](../../research/evidence/costumes-dragon-belly-2026-10-22.jpg) (6 dragons, the art then before | after: front close, three quarter, side close,
hero stage side, hero stage, game camera in front).

- **What the owner saw**: from the side the belly was a separate flat slab standing off the body (the thin peach sliver of the "before" columns).
- **The cause**: the cut of the volume round the face is not the size of the face. The rays from the middle of the head through the face and the chin reach the volume as
  low as 0.2 (rows 1 to 5 of the volume have 6 to 8 faces of 20 cut out, `left_open` 38). What closes that opening is the collar, a slope from the chin down to the volume. The
  belly of lot 1 was a plate cut from the points of the rings and laid 0.026 off the volume (the two patches, then the plate of the "before" pictures): it floated in front of the slope.
- **Changed** (`MASCOT["BELLY"]`, `["BELLY_CHIN"]`, `build_mascot`): no plate any more. The faces of the collar under the chin (rim points 14 to 22) and the faces of the bottom row of
  the volume (columns -3 to 2) are painted in the colour of the belly: the belly IS the body, flush, and reads from the side as the front of the dragon. Under the chin the collar is lower
  (0.011 off the volume, 0.008 at least) so that painted it does not stand off as a slab; flush it fought the volume.
- **Checked**: 6 dragons before | after; `--budget`: largest #630 7,991, none over 8,000; library 1,970 Genesis, 0 failures. Not verified in Unity. The edge of the belly follows the faces
  of the collar and of the volume (steps, not a smooth outline); the art draws a rounded oval.

### 7.6 The balaclava (Head Robber, 24 Genesis)

Sheet: [costumes-balaclava-2026-10-22.jpg](../../research/evidence/costumes-balaclava-2026-10-22.jpg) (8 Genesis, the art then before | after: front close, three quarter, side, back
three quarter, game camera behind, hero stage).

- **What the art draws** (#14, #86, #319, #460, #630, #652, #781, #1960): a dark grey knitted mask wrapping the whole head as one smooth volume, fine ribs running up it, down to the
  neck under the chin, no mouth hole; two large almond holes, each with a thin rolled edge, through which the face and the eye show. The owner: "bugged: a flat grey block with seams, the eye holes
  thick rings".
- **Rebuilt** (`BALACLAVA`, `build_balaclava`): one shell over the whole head, rows of points from the top to the lower edge under the chin (158 degrees at the front), at 20 angles
  closer together at the front; the faces whose centre falls in a hole (`HOLE`, cut at 0.86 of the almond) are left out, and a thin rolled edge (12 stations, 4 sides) follows the almond
  and covers the jagged cut; ribs (9 angles, a piece is left out where it would lie across a hole) darker than the knit. The flat panel 0.030 off the face, the flat bridge, the
  thick rings and the seams are gone.
- **Crown**: the top leaf and the top of the back row are gone (`ROBBER_POINTS`: no spike above a mask that wraps the head smooth; the art shows them only on the far side); the pairs stay and come
  through the knit (`THROUGH`).
- **Triangles**: the mask 1,312 (1,040), the base 280 less (two leaves fewer): #630 7,983, the heaviest Robber; `--budget` largest #630 7,983, none over 8,000.
- **Not verified in Unity.** The pairs of leaves at both sides are larger than the few spikes the art shows on its far side; the lower edge has no cuff, the art draws a thick rolled edge.

### 7.7 The ten 1:1 Genesis, brought to their 1000 px art

Sheet: [heroes-one-of-one-2026-10-22.jpg](../../research/evidence/heroes-one-of-one-2026-10-22.jpg), each Genesis beside its picture at 1000 points (fetched from the addresses of
`.artifacts/genesis-art/manifest.jsonl`; `make_chog_one_of_one_sheet.py --art-folder`), then before | after: bust, hero stage, side of the stage, back three quarter, game camera. The owner:
"the ten 1:1 are not worked enough". Each was read on its 1000 px picture for what makes it recognisable and the pieces missing were built (`chog_one_of_one.py`), within the 8,000 triangles:

| # | What the picture shows that the 3D lacked | Added |
| --- | --- | --- |
| 53 Flame | a thick brown collar with a round gold clasp, a red cape wide over the shoulders | collar 0.075 tall (0.040), clasp 0.040 (0.026), wider round the neck, cape from 150 degrees (120) |
| 392 Skull | a great sheet of flames streaming from the back of the head, tall as the skull | nine tongues 0.30 to 0.52 long (0.20 to 0.30), 0.085 wide, over 240 degrees |
| 434 The Scream | the hands pressed to the cheeks, big cream eyes with a small pupil, a tall open mouth | `ScreamHands` (two pink hands on the head), `1:1 Scream Eye`, `1:1 Scream Mouth` |
| 494 Zombie | the brain large, spilling over the brow, blood running from it, green slime hanging from the mouth, blood speckles | brain 1.45 times larger, three blood drips, two strands of slime, five speckles |
| 973 Pepe | the huge pink-red frown, the ruff of yellow-green points round the neck of the suit | lips on the face (`PEPE`), 13 points round the neck |
| 1006 Manga | the angry thick brows, the grey shading under the eyes, big eyes in a thick line | `MangaBrows`, `1:1 Manga Eye` |
| 1159 Angel | a large thick halo, large layered wings, a thick white brow | halo 0.27 by 0.036 (0.20 by 0.024), five feathers 0.42 to 0.76 long, 0.14 wide (four, 0.36 to 0.56, 0.075), white brows |
| 1290 Warrior | a gold Y helmet (a band and a stem down the face), a red gem in a gold setting, red stripes of war paint, large shiny eyes, a collar of fur | stem, setting and kite gem, two stripes, glints, `WarriorTee` (15 points of fur) |
| 1373 Gold | the light running along the polished gold, tall almond eyes with a glint | `GoldShine` (two streaks, glints), taller eyes |
| 1376 Devil | glossy red drop horns, large glossy eyes, dark red bat wings and a tail with an arrow tip | `DevilHorns` (0.235 tall), wings and tail on the suit, larger eyes with a glint |

- **What they throw is unchanged**: the strokes on the face (brows, blood, slime, war paint, stem) are on a part of their own, so the halo, the brain and the helmet that #1159, #494 and #1290 throw stay
  the object; the Scream, the manga page and the gold Chog now wear a value of "Head" (hands, brows, shine) and still throw a spike of their crown (`ATTACK_OF_HEAD`, `LINES`). Read back
  from the exported library: #434, #1006, #1373, #53 throw `Attack_Leaf`, #494 `Hat_Brain` (864 triangles), #1159 `Hat_Halo` (320), #1290 `Hat_GoldHelmet` (280), #1376 `Attack_Trident`.
- **Checked**: the ten build, 6,287 to 7,789 triangles; `--budget`: largest hero #630 7,983, none over 8,000; library 1,970 Genesis, 0 failures.
- **Not done / not verified in Unity**: the painted brushwork of #434, the screentone and the streaming hair of #1006, the glow and the white-yellow outline of #1373 and #53 (the outline material is shared by every hero), the
  cracks of the skull of #392, the ragged edge of the face of #494, the pose of the arms (the game moves them). The skull and the zombie faces are the faces agent's.

### 7.7b Second pass on #53, #1373 and #434

Sheet: [heroes-one-of-one-pass2-2026-10-22.jpg](../../research/evidence/heroes-one-of-one-pass2-2026-10-22.jpg) (the art at 1000 points, then before | after, `make_chog_one_of_one_sheet.py --numbers`); "before" is the first pass above.
The title of the sheet of the ten said 23 October: it says 22 October now (the file name is unchanged).

- **#53 Flame**: the head IS a flame. The crown of leaves on a round skin-coloured head is gone: `1:1 Flame Hair` (`FlameHair`, `build_flame_hair`), 9 tongues round the face and 7 behind and above it
  (orange with a yellow heart, a lighter outline, waving), no leaf, no teeth of hair on the brow; the cheeks are a deeper orange, not pink (`BLUSH`); the eyes are larger glowing discs (`LINE` 0.086 by 0.094); the
  brown collar is 0.100 tall (0.075) with a clasp of 0.056 (0.040), the cape wider (`FLARE` 0.12), so the collar, the clasp and the red cape show from the front on the hero stage.
- **#1373 Gold**: it had two streaks on the face only. Now `1:1 Gold Plating` (`GoldPlating`): three streaks of light and two bands of shade down the torso, a darker belly, a streak along the
  top of each arm with a band of shade under it, a streak down the front of each leg; the skin runs from 0.94 0.67 0.07 to 1.00 0.92 0.46 (a try at 0.86 0.50 0.04 read as terracotta); the cheeks are orange.
  Not done: the glowing outline (the outline material is shared by every hero), the dark-gold shading on the head.
- **#434 The Scream**: the hands are 0.082 by 0.120 (0.052 by 0.088), flat on the cheeks (0.205 out, 0.058 off the face), with the thumb and four fingers standing up along the face, so they show from the front.
- **Checked**: #53 7,976 triangles (7,311: the tongues of the flame head cost 665), #434 7,427, #1373 6,373; `--budget`: largest hero #630 7,983, none over 8,000; library 1,970 Genesis, 0 failures; what each throws is unchanged (#53, #434, #1373 throw the leaf: `1:1 Flame Hair` is in `ATTACK_OF_HEAD`, or the 2,240 triangles of the flames would be thrown).

## 22 October 2026: item 7, the audit of the clothes against the art

Method: for each value of "Body" (91 of them, from the metadata), three Genesis of different skins, their 256 point pictures beside the 3D hero (`render_chog_traits.py`, new views `pedestal_torso`
and `pedestal_torso_back`: the chest and the arms close, from the front and from behind, which the head hides on the bust), read for what the drawing shows and the 3D lost; the values are audited
in batches by number of Genesis. The prints and logos of the first passes (`CLOTHES`, `printed()`) are judged on these views too.

### Batch 1: the 14 most worn values of Side=Right (Jesus, Kings Robe, Black Trackset, Farmer Shirt, Tuxedo, Squid Game Guard, Black Vest, Inmate, Red Tuxedo, White tuxedo, Six Seven, Prisoner, Stripes, White Pure Shirt)

Sheet: [clothes-batch1-2026-10-22.jpg](../../research/evidence/clothes-batch1-2026-10-22.jpg) (15 rows: #47, #24, #8, #66, #10, #84, #95, #19, #5, #87, #12, #11, #126, #1, #1214; the art, then before | after:
torso, torso from behind, hero stage).

- **White Pure Shirt** (28 Genesis, #1, #30, #145, #233, #254, #307, #1960): the art draws a plain white short-sleeved shirt with a thin rolled neckline and no sash; ours is that (white, a grey collar): unchanged. The red sash
  of the brief is **Jesus** (24 Genesis) and the robe of the two skeletons (**Jesus Robe**, #1214, #1311).
- **Jesus** (the red sash): the art is a thick red scarf wrapped round the neck with one end hanging down the chest over the shoulder, folds running along it; ours was one flat red stripe. Now `SCARF`, `lay_scarf`:
  a roll round the neck (radius 0.030) and a hanging end 0.104 wide standing 0.014 off the shirt, five stations from the shoulder to the hip, two darker fold lines (`SashFold`). **Jesus Robe** has it too.
- **Squid Game Guard**: the triangle of the chest was 0.030 across and lost beside the art's: 0.048, and a little lower.
- **Inmate**: the word INMATE was a speck (0.026 high on a label 0.124 wide); now 0.036 high on a label 0.176 wide, lower on the hem, as the art draws it at the bottom of the shirt.
- **Tuxedos** (the nine values, ~270 Genesis): the V of the shirt in the jacket was 0.12 wide at the top, the art's collar is wide and open: 0.156.
- **Kings Robe**: the rolls of white fur along the open front are thicker (0.034 in place of 0.022); the art's fur is fluffy and broad, ours stays a smooth roll.
- **Judged faithful, unchanged**: Black Trackset (zip, white stripes along the sleeves), Farmer Shirt, Black Vest (padded bands), Six Seven (67), Prisoner and Stripes (bands), White tuxedo.
- **Checked**: `--budget` after the scarf: largest #710 7,981, #911 (a Jesus) 7,931 (8,043 with the first scarf of 12 stations and 6 sides, so the scarf was cut to 9 stations and 5 sides), none over 8,000;
  library 1,970 Genesis, 0 failures. Not verified in Unity.
## 22 October 2026: the Radioactive and Skull bases (lot 9)

Three items of the owner's review, one sheet each, the art beside the hero stage view (front and three quarter close), before | after:
[skins](../../research/evidence/heroes-radioactive-skins-2026-10-22.jpg), [jaws](../../research/evidence/heroes-jaws-2026-10-22.jpg),
[bones](../../research/evidence/heroes-bones-2026-10-22.jpg). New in the previews: the stage look draws the skin gradient of a Radioactive as the game does (`game_look_material`, stops).

1. **Multicoloured skins** (30 Radioactive Genesis: Green 8, Blue 8, Multi Colored 7, Red 7; compared on #31, #473, #772, #1531 green; #343, #598, #1281, #1200 blue; #672, #703, #834, #1134 red; #140, #275,
   #415, #625, #1187, #1192, #1225 multi coloured). The art runs the skin through several bands from the crown to the chest: Multi Colored is a white-pink crown, magenta over the forehead and cheeks, orange then yellow
   at the jaw, lime then green on the chest; Green yellow over the eyes then lime then green; Blue pale aqua, sky blue, blue; Red pale pink, hot pink, red-pink. The game keeps two colours (the bottom, and the top reached at
   1.12: `ChogPartsLibrary.ColourOf`, the library format), so **the two colours are re-read for the head and the jaw** (`RADIOACTIVE_SKINS`, Multi Colored (0.50, 0.90, 0.24) to (1.00, 0.56, 0.86), Green, Blue and Red
   deeper at the chest, lighter at the crown) and the **bands are given as stops** (`RADIOACTIVE_BANDS`, 4 to 6 per colour, the previews draw them with `CHOG_BANDS=1`): the sheet shows both, the two-colour result and the bands
   beside the art. The bands are not in the game: a third and fourth colour need a library with N stops (`ChogPartsLibrary.SkinColours`, its reader and the exporter's table), which is the lead's file; the exporter side is ready
   to write them (`skin["bands"]`). A two-colour skin cannot run pink to green without a grey middle, which the Multi Colored head shows.
2. **The jaw.** The art draws a real jaw, wide and flat, under a dark open mouth: on the Skulls (#54, #1079, #1313, #1642, #1776, #81) a lower jaw of its own standing out under the cranium with its outline and a row of big
   square teeth on it, under a row on the cranium; on the Radioactives (#140, #275, #1187, #1281, #1531) a dark band (the mouth) over a row of big teeth, the jaw line flat across the chin and turning up at the sides.
   Before, the Skull had two rows of small beads on the ball of the head and the Radioactive a U line. Now: **Skull**, one row of seven big teeth on the cranium (`SKULL_FACE`, half height 0.024) and a lower jaw that is a
   volume, a rounded wedge (`JAW_VOLUME`, `add_mandible`, `JawSurface`) with its outline and its own row of six teeth; **Radioactive**, the mouth band (`TEETH["MOUTH"]`), bigger teeth (`GLOW_ROW` 0.020) and a flat jaw
   line (`JAW_LINE`, 0.205 wide). Hats and hoods close on it (checked on #552, #816, #259, #1647, #1405, #1045, #388, #1000). Not exact: the Skull jaw is a smooth egg, the art's has a corner at the chin; the Left jaw opens
   less than the art's.
3. **The bones.** On the art the ribs show only on bare skin: on the 14 Skull and Radioactive Genesis that wear nothing, and under an open Wizard Cape (#388, #459, #846, #1000), a Kings Robe (#1045, #1667) and
   Mummy wraps; under a shirt, a hoodie, a vest, a jacket or an armour (#1647, #1405, #1953, #415, #1192) and under the white Jesus Robe (#1214, #1311) none show. `add_ribs` is now called only for those bodies
   (`BONES_SHOW` in `chog_trait_bases.py`, read in `make_chog_traits.build`), and a Radioactive's ribs are in the deeper tone of its skin, not black. Not done: the bones through the translucent vest or tank top of a few
   Radioactives (#275, #1192) and the arm bones on the art.
- **Checked**: the 114 Skull and Radioactive heroes (`export_chog_parts.py` builds all of them, limit 8,000 triangles). Not in a Unity build.


### Batch 2: the next 24 values of Side=Right by number of Genesis

Sheet: [clothes-batch2-2026-10-22.jpg](../../research/evidence/clothes-batch2-2026-10-22.jpg) (23 rows, the art then before | after: torso, torso from behind, hero stage). Values seen (two Genesis each): Squid Game,
Cream, Pink, Aqua, Blue and Cyan Tuxedo, Fire Shirt, Chug Life, Hawaiian Polo, Blue and Red Chinese, Tank Top, CWO, Yellow Yukata, Yellow Shirt, Wizard, Red shirt, MILF Lover, Red Trackset, Black Shirt, Wizard Cape,
Stripe Shirt, CEO Vest, Mummy.

- **Hawaiian Polo** (30): the art covers the shirt with large white hibiscus flowers; ours had seven small dots. Nine flowers 0.028 to 0.036 (0.020 to 0.026), of 20 points (15), bolder petals.
- **Blue and Red Chinese** (29 and 25): the art has no zip: a short column of gold toggles (cords with a knot at each end) across the top of the chest. Ours was a gold line down the whole front. Now `toggles`
  (three cords 0.052 long, 0.034 apart, from 0.545 down) in the colour `Toggle`, no zip.
- **CWO** (27) and **MILF Lover** (22): the words sit low on the shirt and are larger in the art: CWO 0.092 high at 0.345 (0.075 at 0.410), MILF 0.066 at 0.350 with a larger heart (0.032, at 0.118 across).
- **Yellow Yukata** (27): a kimono: its fronts cross in a V with a red edge and the cloth is covered with swirls. Now the V (`crossed`, two bands 0.040 wide meeting at the breastbone) and nine swirls (five). **White Yukata** (Side=Left)
  has the same cut: it takes it in the Side=Left batch.
- **Wizard** (24): the drawstring of the hood hangs from the bow in two ties (`ties`).
- **Judged faithful, unchanged**: Squid Game (069 and the zip), Cream, Pink, Aqua, Blue and Cyan Tuxedo (the V widened in batch 1), Fire Shirt, Chug Life, Tank Top, Yellow Shirt, Red shirt, Red Trackset, Black Shirt, CEO Vest.
  **Wizard Cape** (5 Genesis on skeletons) lacks its yellow pattern and its ties (a skeleton, the faces agent's bones); **Mummy** (3) has the crossed bandages of the art; **Stripe Shirt** (4): the ribs drawn over it are the faces agent's.
- **Checked**: `--budget`: largest #710 7,981, none over 8,000; library 1,970 Genesis, 0 failures. Not verified in Unity.
## 22 October 2026: strict eye audit, batch 3 (lot 8)

Gradient, Sunset and Plasma visors (157 Genesis), Round Eye (61), Closed Eye Crying (57), Tired (56), Right, strict rule, 3 to 6 Genesis per value (#1, #30, #100, #105; #11, #83, #147; #60, #62, #82; #37, #79, #68, #237; #21, #18, #24, #261;
#66, #106, #123, #246, #267). Sheet, 19 Genesis, the art beside the front close and the three quarter close, before | after (before rendered from a temporary copy of origin/main, not kept):
[heroes-eyes-batch3-2026-10-22.jpg](../../research/evidence/heroes-eyes-batch3-2026-10-22.jpg).

- **Visors, Right: approximate, made closer.** The visor of the pictures is a lens (an almond with round ends, 0.076 half height in the middle and 0.036 at its ends where ours was a band 0.056 and 0.042), its colours running
  smoothly where ours stepped in ten flat stripes. `VISOR["HALF_HEIGHT"]`, `ROUND` 0.55 for the ends, and `smooth_visors()` (`make_chog_traits.py`) cuts each stripe in two with the colour running between the stripes (20 to 24 pieces).
  Still differs: the art's lens is larger and slanted in the three quarter pictures and its edge is a thick black line round it; the colour still shows a step every 0.03 across.
- **Round Eye, Right: wrong shape, redrawn, now approximate.** Tall ovals 0.72 as wide as high, a thin white ring, the pupil filling the white (0.056 by 0.074 dark disc, 0.046 by 0.064 white, pupil 0.038 by 0.056; round and 0.060 by 0.066 before).
  Approximate: the near eye is larger than the far one in the pictures.
- **Closed Eye Crying, Right: wrong, redrawn, now approximate.** The pictures draw a SAD closed eye: a thick arch, its outer end much lower than its inner one; ours were upright happy arches. `CLOSED_ARCH` tilt -0.10 to -0.40, 0.054 by 0.022, thickness 0.026 (0.015).
  The tear is as in lot 5c.
- **Tired, Right: approximate, made closer.** Larger eyes (0.058 by 0.062), one upright glint at the top left of the pupil (it was at the right); the grey-blue bag is a crescent in the pictures and a ring in ours (not changed).
- **Checked**: library export builds all 1,970 Genesis under 8,000 triangles; by eye on the sheet only; not in a Unity build.
- **Left to do**: Side Left (Side Eye, Base, Happy, MOG, Vertical, Flat, Frog, Clown Eye, Chog Viper, Clown Eye Mix, Classic VR, Modern VR, Retard, XD, 3D Glass, BTC Glass, Worried, Thug), then Right: Teary, Retro Glass and Anime Glass remainders.

## 22 October 2026: strict eye audit, batch 4: Side Left (lot 8)

Side Eye L (34 Origin Genesis), Base L (33), Happy L (33), MOG L (32), Vertical L (31), Flat L (30), strict rule, 5 Genesis each (#17, #34, #64, #73, #151; #23, #198, #220, #240, #381; #26, #223, #71, #221, #925; #181, #162,
#176, #293, #380; #63, #78, #175, #45, #296; #218, #196, #301, #450, #531). Sheet, 14 Genesis, the art beside the front close and the three quarter close, before | after:
[heroes-eyes-batch4-2026-10-22.jpg](../../research/evidence/heroes-eyes-batch4-2026-10-22.jpg).

- **The common finding: on Side=Left the two eyes of a pair are two different drawings** (as on Smirk). The near eye, on the left of the picture, is large: wide and flat, 1.6 times as wide as high, a straight lid that runs on to the right
  as a point, a pupil that fills it, a tall glint at its left. The far eye is a small narrow oval, a third as wide as high. We drew two equal eyes. `EYES` (numbers of one eye by its side) is now in the lidded eyes (`SIDE_EYE`, `lidded_eyes`),
  in `FLAT_EYE` and in `VERTICAL`, and `SIDE_LEFT_EYES` holds the two eyes of Side Eye and Base.
- **Side Eye L, Base L: wrong, redrawn, now approximate.** A Left entry for Side Eye (`EYES_OF_SIDE["Left"]`, the Right entry keeps its own numbers): near eye 0.082 by 0.046 with a lid flick 0.036 inwards, far eye 0.030 by 0.056. Approximate: the near
  eye's pupil does not touch the lid in the same way, the far eye is a little too tall.
- **Happy L: approximate, made closer.** The near eye a thicker hooked blade (0.026 thick, rising 0.040), the far one a narrower hook. The art's blade ends in a downward hook and carries a white streak: not drawn.
- **Vertical L: wrong, redrawn, now approximate.** A large white almond with a narrow upright pupil for the near eye (0.078 by 0.046), a small tall one for the far eye (0.030 by 0.046).
- **Flat L: approximate, made closer.** The near eye a long thick dash (0.060 half, 0.024 thick), the far one short (0.028) and slanting; the art's near dash is three strokes, ours one.
- **MOG L (and Chog Viper): approximate, made closer.** The goggles of the pictures are a tall lens (0.074 half height, 0.054 at the ends where ours was 0.060 and 0.050) with colours running smoothly: `smooth_visors()` now cuts the stripes of goggles too
  (14 pieces). The art's lens is a smooth rainbow with a thick dark frame and a notch at the nose; ours still shows a step every 0.04.
- **Checked**: library export builds all 1,970 Genesis under 8,000 triangles; by eye on the sheet only; not in a Unity build.
- **Left to do**: Left: Frog, Clown Eye, Clown Eye Mix, Chog Viper (shape), Classic VR, Modern VR, Retard, XD, 3D Glass, BTC Glass, Worried, Thug, Angry, Smirk done, the lasers; Right: Retro Glass remainder.

## 22 October 2026: strict eye audit, batch 5: Side Left, the rest of the white and clown eyes (lot 8), and the ribs

Frog L (27 Origin Genesis), Clown Eye L (27), Clown Eye Mix L (25), Retard L (23), XD L (23), Worried L (18), Angry L (27), 3 to 5 Genesis each (#7, #15, #74, #131, #144; #178, #206, #224, #245, #142; #57, #70, #790, #942; #32, #182, #216, #217; #85, #180, #273,
#342; #192, #247, #349; #29, #91, #103, #276, #410). Sheet, 13 Genesis, the art beside the front close and the three quarter close, before | after:
[heroes-eyes-batch5-2026-10-22.jpg](../../research/evidence/heroes-eyes-batch5-2026-10-22.jpg). As in batch 4 the two eyes of a pair are two drawings; `EYES` (numbers of one eye by its side) is now also in
`FROG`, `RETARD`, `WORRIED_EYE`, `ANGRY`, `CLOWN` and `XD`.

- **Frog L: wrong, redrawn, now approximate.** Near eye a tall oval 0.050 by 0.072 under a thick outline with a wide black pupil in its lower half and a large glint; far eye 0.030 by 0.056 with the pupil in its middle.
- **Retard L: wrong, redrawn, now approximate.** Near eye a big white oval (0.062 by 0.064) with a pupil low at its left; far eye a narrow oval 0.032 by 0.064 with the pupil in its middle.
- **Clown Eye and Clown Eye Mix L: wrong, redrawn, now approximate.** Near eye a large flat lidded eye under a tall kite-shaped mark (0.130 above, 0.060 below), far eye a narrow oval under a narrower mark (0.100 above).
- **XD L: approximate, made closer.** A large near chevron (0.062 by 0.058, stroke 0.022) and a narrow far one (0.022 by 0.046).
- **Worried L: approximate, made closer.** Near eye larger (0.064 by 0.062), far eye a narrow oval under the same lid.
- **Angry L: wrong, redrawn, now approximate.** Near eye a wide half moon under a flat lid, far eye a narrow tall oval under no lid (the far eye of the art has no angry brow).
- **Still differs on all of them**: the art's near eye has a thicker, wobbling outline; the far eye is drawn to a slightly different tilt on each Genesis.
- **The ribs of #121 and #1214** ([sheet](../../research/evidence/heroes-bones-121-1214-2026-10-22.jpg), the real library files of origin/main and of this branch, stage close and front): neither has rib bands, under the Hawaiian Polo or under the Jesus Robe,
  in either file. `check_bone_bodies.py` builds all 124 Skull and Radioactive Genesis and counts the rib faces of their Body part: none under any shirt, polo, stripe shirt, robe (Jesus), hoodie, vest, jacket or armour; present for no body, the Wizard Cape,
  the Kings Robe, Mummy wraps and the Duck Float; 0 wrong. The costumes agent's sheet (`clothes-batch2`, row #121) shows the bands: their tree must have been built before lot 9 (the code with `BONES_SHOW`, 4f28646) reached it.
- **Checked**: library export builds all 1,970 Genesis under 8,000 triangles; by eye on the sheet only; not in a Unity build.
- **Left to do**: Left: Chog Viper (shape), Classic VR, Modern VR, 3D Glass, BTC Glass, Thug and the lasers of Left; Right: the rest of Retro Glass.
