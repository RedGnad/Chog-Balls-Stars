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
animator does not mind: it only turns the parts. **Variant B is the body in the game since
12 October** (`game/Assets/Chog/Resources/ChogBody.fbx` is a copy of
`variants/B/ChogBody.fbx`); C waits for the game to know the colour of each Genesis's
clothing. `check_chog_body.py` still measures the first body's rules (3,000 triangles, one
material, long toes) and fails the variants on those three.

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
