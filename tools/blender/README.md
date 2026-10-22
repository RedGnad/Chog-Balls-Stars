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
