# Chog Arena visual production map, 19 October 2026

This is a production translation of the owner's approved direction. It is not a new concept board. The references are actual WebGL captures and the right halves of the 18 October comparison sheets. The right halves are 640 x 360, so positions below are described after scaling them by two. The corrected candidate is intended for public owner review, not asserted as an accepted visual match.

The 19 October correction is captured in the [desktop match](chog-visual-runtime-match-desktop-2026-10-19.png), [touch-emulated match](chog-visual-runtime-match-touch-2026-10-19.png) and [touch-emulated home](chog-visual-runtime-home-touch-2026-10-19.png). Build `20261019T170012093Z-1832effe278c49ac8ea39f8d65e834bb` retains the previous simulation stamp. The owner-approved Chog-head floor motif is still absent; no substitute was invented. The original stage tile was preserved, the duplicate collection CTA removed, the sample brick floor renderers hidden behind one slab surface, pink tops textured, and the near wall rebuilt visually.

## Evidence and current state

| View | Approved direction | Actual WebGL evidence | Current implementation |
| --- | --- | --- | --- |
| Home | [menu direction](chog-menu-direction-2026-10-18.jpg), right half | [final local desktop capture](chog-visual-local-home-desktop-final-2026-10-19.png), build `20261019T114810487Z` | A painted background, a real Genesis example card and a three-quarter hero on an irregular painted stage are in the game. The card and stage remain much cleaner and more geometric than the board. |
| Match | [arena direction](chog-arena-direction-2026-10-18-v2.jpg), right half | [final local touch-emulated match](chog-visual-local-match-touch-final-2026-10-19.png), same build | The projected floor albedo and rounded pink visual cover skin are visible in a running match. Bushes and walls still use the sample geometry; the capture contains the tutorial and performance overlays. |

The final build is `20261019T114810487Z-bf9042d09d1743cd84cd33828261f0b2`, with the same simulation stamp as the public build. It was captured locally after the floor override fix, stage treatment and cover treatment. No physical phone or real wallet was used. The public site remains on the earlier build.

## Home: element mapping

| Target element | Existing game element and source | Gap visible in the local capture | Production treatment |
| --- | --- | --- | --- |
| Dark violet wall with one oversized Chog head drawing | `ChogHome.BuildBackdrop`, `MenuMural.png` | The generated mural now reads behind the hero, but its head drawing has not been compared against the exact owner-provided contour. | Keep it as a candidate, verify the silhouette against the supplied head reference, and replace it if the contour is inaccurate. The torso stays excluded. |
| One large 3D hero near the centre, at a three-quarter angle | `ChogHeroStage`, placed by `ChogHome.BuildStage` | The final capture holds a left three-quarter showcase view, which is more readable than the earlier frontal framing. Its pose, scale and position still need comparison against the board rather than code-only judgement. | Keep the real 3D hero and its drag interaction. Tune framing only from equivalent captures. Do not replace the hero with painted mockup art. |
| Gold platform with a rough silhouette and a pink contact area | `ChogHeroStage` generates the pedestal and the two paint meshes in 3D | The existing tile supports the hero. The owner does not want further stage or platform work in the current pass. | Keep the current stage unchanged. Do not add a stamp, a new plinth, or another contact effect. |
| Genesis information panel with an illustration on the right and functional content on the left | `WalletPanel`, `GenesisArt`, Genesis roster, wallet state | The unapproved ornamental frame is gone. The visitor card uses real collection portrait #1078 and labels it as an example, but its clean rectangular styling is still much flatter than the approved sheet. The connected owner state has not been visually rechecked. | Refine the card around its real data states; verify both visitor and connected states before release. Never imply that an example portrait is owned. |
| Compact navigation rail, mode card and large Play action | `ChogHome` and the sample's menu controls | These exist and mostly occupy the right zones. Buttons are currently flat as requested. | Refine spacing and scale only after the stage and card are in place. No duplicated outlines, splash or glossy layers on buttons. |

## Arena: element mapping

The current map is `Gameplay.unity` with the sample's `Tilemap_Level1.prefab`. Its Quantum map has 86 static block colliders, 138 active bush triggers, 170 cover points and 30 wall colliders. The scene is the same for siege and coins. Detailed source and bake constraints are recorded in [the existing map study](siege-map-layouts-2026-10-17/README.md).

| Target element | Existing sample element | Gap visible in the local capture | Safe first production treatment |
| --- | --- | --- | --- |
| Berry violet floor with irregular slab pattern and restrained head motif | Repeated floor meshes, painted through the `WoodDark` material and `ChogArenaSkin` | A world-space projected slab albedo is visible in the match, but its pattern is smaller and more regular than the approved board. The head motif is absent. | Keep floor projection as a technical proof, tune scale and slab distribution against the board, then add a motif only from the exact approved head contour. The mesh and colliders stay unchanged. |
| Pink cover blocks with readable top and sides | 86 `Crate.prefab` instances inside `Tilemap_Level1.prefab` | A runtime skin now replaces only their visible cage meshes with batched dark outlines, berry sides and pink inset rounded tops. It remains a regular grid of blocks and needs softer texture and variation to reach the board. | Keep the current instances as collision. Continue only with visual-only batching or a shared mesh skin, never a new collider or obstacle. Validate hero visibility behind cover. |
| Turquoise bushes | `InvisibleProp.prefab` instances, each also a Quantum visibility trigger | The colour is close; the leaf geometry is still the sample's. | Prototype a visual-only leaf material or mesh replacement. Keep trigger count, position and grouping unchanged. |
| Dark framed arena walls with warm accents | Sample wall meshes and shared stone materials | The scene reads as recoloured dungeon walls. | Prototype material/texture and small lighting accents that do not affect collision. Avoid a new playable layout in this visual pass. |
| Match HUD controls | Existing touch HUD | The board shows flat circles with no duplicate decorative strokes. | Inspect in a clean mobile capture; this visual pass should not move gameplay hit targets until their actual touch areas are measured. |

## First implementation slice and acceptance gate

1. Done locally: remove the unapproved ornamental panel; use one integrated visitor card with a genuine, clearly labelled collection example; hold the real 3D hero at three-quarter view over a 3D irregular gold stage with a pink contact mark. The connected Genesis state remains to be captured.
2. Done locally: replace the plain menu backdrop with a candidate mural; project a painted floor albedo onto the sample floor; and replace the visible cage mesh of all 86 sample crates at runtime with three batched cosmetic surfaces. No source prefab, collider, trigger, cover point, spawn or Quantum asset changed.
3. Done locally: capture final home at 1280 x 720 and a running match at 960 x 540 touch emulation. The tutorial and performance overlays remain in the match capture. The mural, card, stage, floor and cover treatment are still candidates, not a claim of a board match.
4. Next gate: review these actual in-game captures with the owner, then focus on the floor, boundary walls, covers, bushes and card as complete systems. Do not spend another pass on the existing stage tile. Check the connected card state, a clean match view and a physical phone. A layout change is a separate map-bake and gameplay task, because it changes Photon simulation assets and bot navigation.

## Generated bitmap briefs and file paths

The built-in image generator produced two candidate bitmaps from the right halves of the approved comparison sheets. The production briefs were:

- `game/Assets/Chog/Resources/MenuMural.png`: a full-bleed, dark violet painted wall with one large, low-contrast Chog head silhouette behind the left-center 3D hero; keep the right side quiet for the card; no rendered hero, platform, button, lettering or UI in the bitmap.
- `game/Assets/Chog/Resources/ArenaFloorSlabs.png`: a seamless top-down berry-violet hand-painted stone slab albedo, irregular and restrained at game camera scale; no regular grid, planks, logo, character or UI in the bitmap.

Both assets are candidates. Their visual accuracy is judged by the WebGL captures above, not by the generation prompt. Neither substitutes for the exact official head contour requested for the floor motif.

The earlier Skull and Radioactive head work was accepted. The remaining Genesis eyes and mouths audit, complete astronaut suit and cap spike check are still open tasks; visual production does not close them.
