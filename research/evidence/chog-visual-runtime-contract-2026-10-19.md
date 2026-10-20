# Chog visual runtime contract, 19 October 2026

## Purpose

The approved direction boards are production targets, not decorative reference pictures. This document translates their visible composition into runtime responsibilities and records the local correction made after the owner's feedback. The correction does not change gameplay, colliders, triggers or map baking.

The source references are `chog-menu-direction-2026-10-18.jpg` and `chog-arena-direction-2026-10-18-v2.jpg`. The corrected local build is `20261019T170012093Z-1832effe278c49ac8ea39f8d65e834bb`. Its captures are [home at touch size](chog-visual-runtime-home-touch-2026-10-19.png), [desktop match](chog-visual-runtime-match-desktop-2026-10-19.png) and [touch-emulated match](chog-visual-runtime-match-touch-2026-10-19.png). These are running WebGL views, not mockups or a physical phone test.

## Corrected local result

The hero now occupies the left focal column, the existing stage tile remains, and `SEE EVERY HERO` is removed. The floor no longer multiplies the old brick albedo into the slab paint: the interior brick renderers are hidden and one world-projected painted floor is shown over the same playable area. The near wall has a separate two-course face and caps. The cover tops use pink painted albedo while all source blockers remain in place. The touch-emulated match reaches active play without a JavaScript exception. This is a candidate for live owner review; it is not an exact reproduction of either board.

The exact Chog-head floor marks are still missing because the approved contour has not been made into a local decal source. The far wall, bushes and connected Genesis card remain unverified against the board. No invented mark or additional stage ornament was added.

## Home composition contract

| Role | Direction board | Current local result | Runtime rule for the next pass |
| --- | --- | --- | --- |
| Primary focal point | The live Chog is in the left focal column, with substantial quiet space around it. | The stage centre is too far right and competes with the information panel. | Reserve the left half for the hero, with its visual centre left of the screen centre on desktop. The card owns the right column. |
| Character presentation | A real 3D Chog sits on the existing stage. | The owner considers the existing base tile sufficient and does not want further work spent on it. | Keep the existing rotating 3D Chog and stage unchanged. Do not add a stamp, a new plinth, or a contact effect in this visual pass. |
| Background | A dark, quiet mural supports the Chog without competing with the UI. | The mural has the intended role, but the foreground composition does not yet match it. | Background, stage stamp and live model are one hero presentation group. They are positioned together and assessed together at each responsive breakpoint. |
| Information card | One expressive, paper-like card occupies the right information column. | The card is a large clean rectangle and its extra action reduces its usable area. | Build one card whose border, interior rhythm and art placement follow the board. It is a stateful wallet or Genesis card, not a generic panel. |
| Navigation | One clear route reaches each destination. | `SEE EVERY HERO` duplicates the rail's `HEROES` destination. | The rail owns collection navigation. Remove the redundant explicit collection CTA. |
| Actions | The mode selector and Play are a lower-right action group. | This group is already structurally close to the board. | Preserve its function, then adjust only as part of the full responsive layout pass. |

## Arena visual contract

### Source geometry and present rendering facts

The Photon sample's scene has three independent visual groups:

| Source group | Concrete source | Gameplay responsibility | Current visual treatment | Required treatment |
| --- | --- | --- | --- | --- |
| Playable ground | `Level_1/Floor/InteriorFloor`, 22 by 11 instances of `floorDecoration_wood` | The visible ground under play. It does not alter the Quantum collision map. | The material named `WoodDark` keeps its wood albedo and the slab image is multiplied over it. | One complete ground material family. Replace, never multiply with, the sample wood albedo. |
| Boundary walls | `Level_1/Walls`, including the lower `CenterWall` at z -17.25 and the side walls | The fixed boundary and its 30 static Quantum colliders | Shared `Stone` and `StoneDark` colours plus global foot shading make the lower wall read as a flat dark band. | Treat wall top, wall face and playfield edge as separate visual roles. They must frame the arena without reading as a broken overlay or as ground. |
| Covers and bushes | 86 `Crate` blockers and 138 `InvisibleProp` bush triggers in `Tilemap_Level1` | Block shots and movement, or provide invisibility | Covers are runtime-replaced and bushes recoloured while retaining the sample's simulation. | Keep their positions and gameplay. Give each a consistent silhouette and palette after the ground and boundary system are approved. |

The arena board has a single ground language: painted berry slabs. It does not combine wood planks, brick texture and a second slab overlay. The next floor shader or material must therefore use the new floor albedo as its base on every playable ground renderer. It may add controlled value, edge or lighting variation, but it must not multiply the old `WoodDark` albedo back into the picture.

The missing Chog marks have a direct cause: `ArenaFloorSlabs.png` was created without a logo and there is no decal or floor-mark placement system. No substitute mark should be invented. The marks will be implemented only after extracting the exact supplied Chog head contour into a source asset. They will be placed at named, mirrored gameplay-safe ground coordinates, not scattered decoratively.

## 20 October priority update

The owner deferred further arena and ground work. In particular, the new monochrome Chog head mark is a menu and profile mark only, not an arena-floor decal. The current slice therefore makes no terrain, wall, platform or floor-material change.

The active home and preparation rules for the `20261020T052754932Z` candidate are visible in [the connected empty-wallet capture](chog-home-connected-empty-runtime-2026-10-20.png) and [the recruit preparation capture](chog-recruit-lobby-runtime-2026-10-20.png). A connected wallet with no Genesis is told to use the `RENT` rail. It does not receive a duplicate rent call to action or a generic staking task. The profile and HEROES rail use the supplied Chog head's outer silhouette as one monochrome mark. Buttons use one face, one contour and one darker lower lip. The preparation view hides the Photon sample classes and shows the local player as `RECRUIT`.

This is a running WebGL check using a throwaway empty address. It is not a real-wallet or physical-phone result. The match music playlist also now contains the Chog loop and retained Photon sample loop; one clip is selected for a whole match, but headless capture does not audition sound.

## Remaining implementation sequence after owner review

1. Build a renderer and material role map in the running `Gameplay` scene. It will list ground, wall top, wall face, edge, cover, bush, objectives and HUD separately. This verifies the source hierarchy against the board and prevents one shared material from painting unrelated surfaces.
2. Make one responsive home layout specification at desktop and mobile sizes. It includes stage bounds, card bounds, rail clearance, mode and Play group, and the single collection route.
3. Replace the floor pipeline as a complete ground system. It includes base material coverage, edge treatment and exact Chog-head decals, and leaves all Quantum geometry and simulation data untouched.
4. Rebuild the boundary wall presentation as a wall system rather than a palette entry. Verify it from the active player camera at both map ends.
5. Only then tune covers, bushes and accent lighting against the same capture framing as the direction board.

Each implementation slice must have a matching in-game capture beside the board before it is considered ready for review. A colour change, texture swap or isolated button adjustment cannot be accepted as completion of a composition rule.
