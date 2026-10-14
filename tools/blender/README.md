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
