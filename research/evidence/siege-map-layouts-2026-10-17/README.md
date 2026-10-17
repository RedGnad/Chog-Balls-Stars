# Siege map layouts, 17 October 2026

Phase 1 of the siege map rework: how the arena is built today, what Brawl Stars and level design
writing say about base maps, three candidate layouts drawn at the scale of the game, a
recommendation and an implementation plan. Nothing in `game/`, `server/` or `site/` was changed and
nothing was built: every number below comes from the project's files, the exported simulation
assets of the build served today (`site/quantum-db.json`, stamp `f4a81f1ddbd919b6`) or a cited
source.

**Recommendation: candidate A, "Three lanes"**, as a second map used by the siege only (coins keep
the map of today). Same arena (21 x 33 cells, the size of 534 of the 723 Brawl Stars maps), same
camera, same structure positions, so no code moves a structure; a walled base, three lanes with one
chokepoint each, cover beside the tower, bushes on the flanks and in the centre, none near a tower.
In the same simulation batch: the core's collision radius from 0.75 to 1.5 (its model is now drawn
1.6 wide at the ground), and towers that need a line of sight to shoot, so that the cover near a
tower means something.

| Picture | What it shows |
| --- | --- |
| [siege-layouts-side-by-side-2026-10-17.png](siege-layouts-side-by-side-2026-10-17.png) | The four maps at one scale |
| [siege-map-current-map1-2026-10-17.png](siege-map-current-map1-2026-10-17.png) | Today: Map1, read from the exported assets |
| [siege-map-candidate-a-three-lanes-2026-10-17.png](siege-map-candidate-a-three-lanes-2026-10-17.png) | A. Three lanes (recommended) |
| [siege-map-candidate-b-keep-gates-2026-10-17.png](siege-map-candidate-b-keep-gates-2026-10-17.png) | B. The keep and its two gates |
| [siege-map-candidate-c-bigger-2026-10-17.png](siege-map-candidate-c-bigger-2026-10-17.png) | C. Bigger: 25 x 37 |
| [layouts.txt](layouts.txt) | Every map as text, one character per cell (the input a builder in the Editor can read) |
| [measures.json](measures.json) | The measures of section 5 |
| [draw_layouts.py](draw_layouts.py) | Draws and measures everything here (see the end) |
| [brawl_map_stats.py](brawl_map_stats.py), [brawl-map-stats.txt](brawl-map-stats.txt) | The counts of section 3 on Brawl Stars' own `maps.csv`, and their output |

On every picture: one cell is one unit of the simulation; blue (team 0) at the bottom, red at the
top; blocks in purple, bushes in green; each tower with its range of 5; each core at the proposed
radius 1.5 with today's 0.75 dashed; spawn points (rings), escape points (diamonds), objective
points (crosses); gold cells are free ground outside the red tower's range with a straight shot at
it within 8; white cells hit the red core from outside the red tower's range within 9 (the longest
hero attack), filled when they can be reached from the centre without entering that range; the
dashed frames are the floor a blue player's camera shows at 16:9, standing at its spawn and at the
centre.

## 1. How the arena is built today

**One Unity scene, one Quantum map, every mode.** The menu's scene info names the scene
`Assets/QuantumUser/Scenes/Gameplay.unity` and, in its `RuntimeConfig`, the map `Map1` (GUID
1710471675203725834) ([TSS-QuantumMenuSceneInfo.asset:15-23](../../../game/Assets/QuantumUser/Resources/TSS-QuantumMenuSceneInfo.asset)).
`MatchModeChoice.Apply` writes only the mode into that config
([MatchModeChoice.cs:86-94](../../../game/Assets/Chog/Screens/MatchModeChoice.cs)), so siege and
coins play on the same map. `Map1.asset` records its scene `Gameplay`
([Map1.asset:22-23](../../../game/Assets/QuantumUser/Resources/Map1.asset)).

**What the simulation sees** (Map1 in `site/quantum-db.json`, read by
`tools/quantum/list_map_points.py` and the script here): 424 static colliders and 156 map entities.

| Colliders | Count | Shape | Layer | From |
| --- | --- | --- | --- | --- |
| `Collider` (the crates, our "blocks") | 86 | box 1 x 1 | 6 Static | `Crate.prefab` instances inside `Tilemap_Level1.prefab` |
| `InvisibleProp` (bushes) | 138 | box 0.5 x 0.5, trigger | 10 InvisibilityPoint | `InvisibleProp.prefab` instances inside `Tilemap_Level1.prefab` |
| `CoverPoint(Clone)` | 170 | circle 0.25 | 9 CoverPoint | scene objects under `CoverPointsManager` |
| `QuadStaticCollider` (arena walls) | 30 | box 4 x 0.75 | 6 Static | `wallSingle.fbx` instances under `Level_1/Walls` |

- The playable floor is a grid of 21 x 33 cells of one unit, centred on whole coordinates (x -10..10,
  y -16..16): the inner faces of the walls are at x = +-10.525 and y = +-16.525. Blocks and bushes are
  exactly symmetric by a turn of 180 degrees around the centre (86 of 86 and 138 of 138), not by a
  mirror.
- Layers ([SimulationConfig.asset:90-101](../../../game/Assets/QuantumUser/Resources/SimulationConfig.asset)):
  6 Static, 7 Character, 8 Collectible, 9 CoverPoint, 10 InvisibilityPoint. Projectiles test
  "Static" and "Character" ([PhysicsHelper.cs:14](../../../game/Assets/QuantumUser/Simulation/System/PhysicsHelper.cs)),
  skip triggers and stop on a static when their data says so
  ([AttackData.cs:110-124](../../../game/Assets/QuantumUser/Simulation/AssetDefinition/AttackData.cs)):
  blocks stop straight shots, bushes stop nothing. There is no tile that stops heroes but lets shots
  through (Brawl Stars' water and rope fences): it would need a new layer.
- The scene: root `Level_1` ([Gameplay.unity:1975](../../../game/Assets/QuantumUser/Scenes/Gameplay.unity))
  holds `Floor` (22 rows of 11 `floorDecoration_wood` tiles, 2 units each, x -11..11, z -22..22),
  `Walls` (`SideWall` at x +-11.25, `CenterWall` at z +-17.25), `Spawners/Team0` and `Team1` (three
  `SpawnPoint.prefab` each), `ObjectivePoints` (six), `EscapeWaypoints` (three per team), a
  decorative chest at the centre (no collider) and the `Tilemap_Level1.prefab` instance with the
  86 crates and 138 bushes. `Tiles_HidingTest`, with 132 more bushes, is inactive
  ([Gameplay.unity:202-207](../../../game/Assets/QuantumUser/Scenes/Gameplay.unity)) and is not baked
  (the baker only collects active objects, `FindLocalObjects`, QuantumUnityRuntime.cs:20304-20316).
- **Bushes** are both a static trigger and a map entity (`QuantumEntityPrototype` +
  `QPrototypeInvisibilitySpot`). `VisibilitySystem` maps each spot by its static collider index at
  init and makes a hero invisible while it touches one, unless it attacked or was hurt in the last
  2 s ([VisibilitySystem.cs:27-42, 65-99](../../../game/Assets/QuantumUser/Simulation/System/VisibilitySystem.cs)).
  The spot's group, collider index and four neighbours (used by the bots' `RunInBush`) are written
  by the custom baker `HidingPointsMapBaker` in `OnBake`
  ([HidingPointsMapBaker.cs:13-47, 76-105](../../../game/Assets/TwinStickShooter/Scripts/Dev/HidingPointsMapBaker.cs)),
  which Quantum calls after the prototypes are baked
  ([QuantumUnityRuntime.cs:19431-19446](../../../game/Assets/Photon/Quantum/Runtime/QuantumUnityRuntime.cs)):
  the values reach the map only on a second bake. The groups go into one global asset,
  `QuantumUser/Resources/InvisibilityGroups`, that no simulation code reads (only its own class and
  two editor tools refer to it).
- **Cover points** (bots' `FindCoverSpot` looks for them within 8 and reads the normal of their
  `CoverPoint` asset, [FindCoverSpot.cs:30-49](../../../game/Assets/QuantumUser/Simulation/AI/FindCoverSpot.cs))
  were made by `CoverPointsManager.GenerateCoverPoints` from `CoverPointsGenerator` components
  ([CoverPointsGenerator.cs:11-14, 45](../../../game/Assets/TwinStickShooter/Scripts/Dev/CoverPointsGenerator.cs)).
  **Trap found:** the 86 generators in the scene have their four `CoverPoint` assets empty (86 of 86
  `_coverPointNorth: {fileID: 0}` in Gameplay.unity): generating again would make cover points with no
  asset, and `FindCoverSpot` would then read `Normal` from a missing asset.
- **Spawn points** (`component SpawnPoint { Int32 Team; }`,
  [Respawn.qtn:6-9](../../../game/Assets/QuantumUser/Simulation/DSL/Respawn.qtn)): team 0 at y -14,
  team 1 at y +16, x -3, 0, 3. Not symmetric: team 1 starts 2 units further away. A first spawn takes
  the player's own point, a respawn a random one of its team
  ([RespawnSystem.cs:42-75](../../../game/Assets/QuantumUser/Simulation/System/RespawnSystem.cs)),
  after 3 s ([HealthSystem.cs:13](../../../game/Assets/QuantumUser/Simulation/System/HealthSystem.cs)).
- **Structures are placed by code, not by the map**
  ([Structures.cs:14-30, 54-95](../../../game/Assets/QuantumUser/Simulation/System/Structures.cs)):
  cores at (0, -12.5) and (0, 12.5), radius 0.75, 12,000 health; towers at (0.5, -8.5) and
  (-0.5, 8.5), radius 0.6, 8,000 health, range 5, 500 damage every second. They are dynamic circle
  colliders on the Character layer, so the navmesh does not know them. A tower hurts the nearest
  living enemy hero whose centre is within 5 of its own, **with no line of sight test**
  ([Structures.cs:101-141](../../../game/Assets/QuantumUser/Simulation/System/Structures.cs)): a block
  between them changes nothing, and the beam is drawn through the block. The core can be hurt while
  its tower stands (`Attackable` only guides the bots, [Structures.cs:207-227](../../../game/Assets/QuantumUser/Simulation/System/Structures.cs)).
- **Modes** ([MatchModes.cs:21-27](../../../game/Assets/QuantumUser/Simulation/System/MatchModes.cs),
  [MatchMode.qtn](../../../game/Assets/QuantumUser/Simulation/DSL/MatchMode.qtn)): siege 180 s, score
  = damage to the other team's structures in hundreds; coins has no structure; a config without a
  mode (records before 16 October) is TowersAndCoins. The coins need only the centre: the game
  manager spawns one coin every 8 s within 1 of `Origin` (0, 0)
  ([SpawnWithRandomRadius.cs:15-26](../../../game/Assets/QuantumUser/Simulation/AI/SpawnWithRandomRadius.cs),
  machine read with `tools/quantum/list_hfsm.py`) and the bots guard the six objective points around it.
- **Heroes:** KCC radius 0.5 ([BaseKCC.asset:19-20](../../../game/Assets/QuantumUser/Resources/BaseKCC.asset)),
  speed 2.75 (Knight, Archer) or 2.0 (Spellcaster) units a second (attribute 3 of the class prefabs,
  read by [MovementSystem.cs:31-32](../../../game/Assets/QuantumUser/Simulation/System/MovementSystem.cs)),
  longest attack 9 ([Structures.cs:189-193](../../../game/Assets/QuantumUser/Simulation/System/Structures.cs)).
- **The camera** (`GameEssentials.prefab` lines 708-715 and `LocalPlayerCameraFollow.cs:159-183`, angle in
  [ChogCameraRig.cs:108-113](../../../game/Assets/Chog/Match/ChogCameraRig.cs)): 25 above and 7.5
  behind the player, pitch 75, vertical field of view 35, **x locked at 0**, z clamped to -18..3
  for team 0 (team 1's camera is turned around, -5..19). Worked out in `camera_ground` of the
  script: at 16:9 it shows a floor 26.8 units wide at the bottom of the screen and 31.7 at the top,
  17.0 long (from 8.6 behind the player to 8.4 ahead); from a spawn, y -19.1 to -2.1. The whole width
  of the arena (22.6 with its walls) is always on screen at 16:9 and wider; at 4:3 the bottom edge
  shows 20.1 and cuts the sides. The towers stand 17 apart: one screen.
- **The bots** move on a Quantum navmesh named "NavMesh" (`frame.Map.GetNavMesh("NavMesh")` in
  [EngagementSteering.cs:31](../../../game/Assets/QuantumUser/Simulation/AI/EngagementSteering.cs) and
  six other AI actions), baked into `Map1_NavMesh` and `Map1_NavMesh_data` (agent radius 0.25,
  32 x 32 grid of 4 units). The scene has no Unity NavMesh data and its `QuantumMapNavMeshUnity` lists
  no surface ([Gameplay.unity:43304](../../../game/Assets/QuantumUser/Scenes/Gameplay.unity)): the
  navmesh shipped with the sample cannot be baked again as the scene stands. The package
  `com.unity.ai.navigation` 2.0.14 is installed ([manifest.json:6](../../../game/Packages/manifest.json)).
  On top of the navmesh: `Structures.KeepAway` (stay out of enemy tower range unless pushing),
  `Structures.BotObjective` (defend, push), cover points, bushes, objective and escape points.
- **What paints the arena** reads materials by name and only in a scene named `Gameplay`:
  `ChogArenaSkin.ArenaScene` ([ChogArenaSkin.cs:23, 38](../../../game/Assets/Chog/Screens/ChogArenaSkin.cs)),
  `ChogSounds.MatchScene` ([ChogSounds.cs:114](../../../game/Assets/Chog/Match/ChogSounds.cs)),
  `GenesisEffects.MatchScene` ([GenesisEffects.cs:26](../../../game/Assets/Chog/Match/GenesisEffects.cs)).

## 2. What changes a map, and what must be baked again

**Edits that change one mode's map.** Today there is one map, so any change to `Level_1` (blocks,
bushes, walls, spawn, objective or escape points) changes siege and coins together. To change the
siege alone:

1. a second scene, e.g. `Assets/QuantumUser/Scenes/GameplaySiege.unity` (a copy of Gameplay.unity),
   whose `QuantumMapData` points to a **new** Map asset (`QuantumUser/Resources/SiegeMap.asset`);
2. `MatchModeChoice.Apply` sets `RuntimeConfig.Map` with the mode (SiegeMap for SIEGE, Map1 for
   COINS);
3. the scene added, enabled, to the build settings (the network build takes the enabled scenes,
   [QualificationBuild.cs:58-62](../../../game/Assets/Chog/Editor/QualificationBuild.cs);
   [EditorBuildSettings.asset](../../../game/ProjectSettings/EditorBuildSettings.asset) has the menu
   and Gameplay only);
4. the three view checks of the scene name accept the new scene (simplest: names starting with
   `Gameplay`).

**Can a second map be used for the siege only? Yes, the engine supports it as configured.** The
simulation config loads the scene of whatever map the running game has:
`AutoLoadSceneFromMap: 2` (UnloadPreviousSceneThenLoad,
[SimulationConfig.asset:141](../../../game/Assets/QuantumUser/Resources/SimulationConfig.asset)), and
the runner loads `game.Frames.Verified.Map.Scene` when it changes
([QuantumUnityRuntime.cs:1308-1360](../../../game/Assets/Photon/Quantum/Runtime/QuantumUnityRuntime.cs));
the menu only preloads a scene when that option is off
([QuantumMenuConnectionBehaviourSDK.cs:227-252](../../../game/Assets/Photon/QuantumMenu/Runtime/QuantumMenuConnectionBehaviourSDK.cs)).
Rooms are already split by mode (`-siege`, `-coins` after the stamp, [modes.js](../../../server/src/modes.js)),
so every player of a siege room sends the same map. Photon's configs webhook keeps the config as the
first client sent it ([photon.js:210-215](../../../server/src/photon.js)), and the judge replays with
the record's config, whose map it finds in `quantum-db.json` (the export writes every asset of the
Quantum database, maps and navmeshes included,
[QualificationBuild.cs:196-217](../../../game/Assets/Chog/Editor/QualificationBuild.cs)). Not
checked anywhere today: that a siege config names the siege map (a forged config could play a siege
on Map1); the judge can refuse a record whose map is not its mode's.

**What must be baked again, and how.** Quantum's map documentation: a map is a Unity scene plus the
baked `QuantumMapData`; the navmesh is imported from a Unity NavMeshSurface listed in
`QuantumMapNavMeshUnity` and baked with "Bake All" ([Map Baking](https://doc.photonengine.com/quantum/v3/manual/maps/map-baking),
[Importing a Unity navmesh](https://doc.photonengine.com/quantum/v3/manual/navigation/workflow-navmesh)).
In this project:

| Step | Editor action | By script |
| --- | --- | --- |
| Static colliders and map entities (blocks, bushes, spawn, objective, escape points) | Saving the scene bakes them (`AutoBuildOnSceneSave: 96` = colliders and prototypes, [QuantumEditorSettings.asset:24-26](../../../game/Assets/QuantumUser/Editor/QuantumEditorSettings.asset)); or Tools > Quantum > Bake > MapData | `QuantumEditorAutoBaker.BakeMap(mapData, BakeMapData \| GenerateAssetDB)` ([QuantumUnityEditor.cs:18497-18516, 18663-18728](../../../game/Assets/Photon/Quantum/Editor/QuantumUnityEditor.cs)) |
| The same, a second time | Bake again | The bush neighbours written by `HidingPointsMapBaker.OnBake` reach the map on the second bake (section 1) |
| Cover points | `CoverPointsManager` > Generate Cover Points, after giving the generators their four assets | `CoverPointsManager.GenerateCoverPoints()` is public; or written by the builder from the grid |
| Navmesh | A NavMeshSurface under the map, listed in `QuantumMapNavMeshUnity.NavMeshSurfaces`, then Tools > Quantum > Bake > MapData with Unity NavMesh Import | Same `BakeMap` with `BakeUnityNavMesh \| ImportUnityNavMesh \| BakeNavMesh` |
| Register the new assets before a build | (automatic, delayed) | `QuantumUnityDBUtilities.RefreshGlobalDB()`, then check `QuantumUnityDB.TryGetGlobalAsset` (a build started too early ships without a new asset) |

Nothing bakes during a build (`AutoBuildOnBuild: 0`), and the build watcher only refreshes, imports
the roster and builds ([BuildRequestWatcher.cs:64-117](../../../game/Assets/Chog/Editor/BuildRequestWatcher.cs)).
The Editor stays open on the project, so `-batchmode` cannot run on it: the bake has to be an Editor
method started by the watcher (a new request word, e.g. `maps`) or from a menu.

**Every one of these changes the simulation stamp.** `QualificationBuild.SimulationStamp` hashes the
`.cs` and `.qtn` files of `Assets/QuantumUser/Simulation`, Quantum's assemblies and the exported
`quantum-db.json` ([QualificationBuild.cs:158-189](../../../game/Assets/Chog/Editor/QualificationBuild.cs)):
a new map, a navmesh, the rewritten `InvisibilityGroups` or a new core radius each make a new stamp,
four new Photon names (siege and coins, public and preview), and every player must be on the new
build. One batch, one stamp.

## 3. Research: how Brawl Stars builds its base maps

Sources: the game's own data files published in
[tailsjs/brawl-stars-patches](https://github.com/tailsjs/brawl-stars-patches) (`maps.csv` of version
59.212.1, `tiles.csv` 65.219.1, `characters.csv` 66.306.1, `game_mode_variations.csv` 63.351.1), the
tile legend of [bloodwiing/BrawlMapGen](https://github.com/bloodwiing/BrawlMapGen) (`presets/brawlstars.json`),
the Brawl Stars Fandom wiki (pages read as wikitext through its `api.php`), and Supercell's patch
notes. Where a number is worked out from the data rather than quoted, it says "derived". The sizes,
positions and shares below were computed again here from `maps.csv` and `tiles.csv`
([brawl_map_stats.py](brawl_map_stats.py), output in [brawl-map-stats.txt](brawl-map-stats.txt)),
not only taken from the research pass.

**The grid.**
- 534 of the 723 maps of `maps.csv` are 21 wide by 33 tall (the 3v3 modes); Siege maps are
  27 x 39 (19 maps); the 8 maps of Spirit Wars (Siege's successor, 2024 and 2025) are 21 x 33
  (`SiegeSmall_1` to `_8`). **Our arena is exactly that 21 x 33 grid.**
- Tiles (`tiles.csv`): walls (`M X Y C I T B N`) stop movement and projectiles; bushes (`F`, `R`)
  stop neither; water (`W`) and rope fence (`a`) stop movement but not projectiles.
- The Map Maker refuses "illegal placements such as leaving one-tile gaps between solid blocks or
  having enclosed spaces" and offers horizontal, vertical, diagonal mirrors (Fandom, Map Maker page).
  "Diagonally symmetrical" on the map pages means a turn of 180 degrees; 5 of the 8 Spirit Wars maps
  are exactly symmetric that way in the data.
- Brawler body: collision radius 120 units, about 0.8 tile (derived, 300 units a tile); normal
  speed 720, about 2.4 tiles a second (derived). Ours: 1 unit wide, 2.0 to 2.75 units a second.

**Siege** (Fandom "Siege"; removed from rotation 7 March 2022 and from the game 31 August 2022,
back as a community event 5 September to 24 October 2023, no return in the 2024 to 2026 version
histories): bolts spawn near the middle, the team with more bolts gets a Siege Bot at set times, the
IKE turret attacks "whichever Brawler is closest to it".
- In every Siege map of the data the IKE is **3 rows from the back edge, centred**, the three
  spawns on row 0 behind it, 2 columns apart. Same in the 8 Spirit Wars maps (turret row 3, spawns
  row 0 at columns -2, 0, 2).
- IKE range: 35 (old data) or 28 (current data) units of a third of a tile, 11.7 or 9.3 tiles
  (derived); 25,000 base health, 700 damage every 0.5 s (`characters.csv`, `RoboWarsBase`). Our
  tower's 5 is close to the Brawl Arena tower's 17 units, 5.7 tiles (derived).
- Layouts (Fandom map pages, checked on the grids): Nuts & Bolts, a 7 x 4 lake 5 rows in front of
  the IKE; Factory Rush, "open center ... side lanes include two tile wide bushes that curve inward
  towards Siege IKEs. Each IKE is protected by two tile thick walls"; Bot Drop, three wall blocks two
  rows in front of the IKE with two 5-tile gaps; Junk Park, "on both sides of the IKE turret, there
  are walls that protect ... from the side".
- **Measured on the 8 Spirit Wars maps (21 x 33):** walls 13.4 % of the cells (median), bushes
  6.2 %, water 1.5 %; within 7 tiles of the turret a median of 13.5 wall cells and **0.5 bush cell**;
  the straight line in front of the turret is closed by a wall or water within 4 or 5 rows in 5 of
  the 8 maps (within 10 rows in 7). The 19 Siege maps: walls 13.5 %, bushes 9.7 %, no bush within 7 of the IKE (median).
  Our Map1: blocks 12.4 %, bushes 19.9 %, 24 bush cells within 7 of a tower.

**Heist** (the closest live mode with a base to destroy; Fandom "Heist"): the safe has no attack; in
the data it sits 4 rows from the back, centred, in 40 of the 47 Heist maps, spawns behind it
("Players now spawn behind the safe", 5 December 2018). Design notes in the version history: Bandit Stash changed "so that throwers
cannot reach the Safe with their main attack"; Safe Zone "Only through the middle lane can Brawlers
access each teams' safes". Heist maps (47 in the data): walls 11.8 %, bushes 15.6 %, 13 bush cells
within 7 of the safe (median).

**Brawl Arena** (Fandom "Brawl_Arena", permanent since 3 September 2025): 3v3 with "two towers on
the side of the arena for both teams which must be destroyed before it is possible to target the
base"; the base attacks nearby enemies. The closest mode to ours (towers, then a base). Its emotes
are named left, center, right: three lanes.

**Camera.** Beta notes of March 2018 (Fandom version history): "Mask added to ensure the same field
of vision across all devices" and "Camera is a bit farther away giving more visibility"; TouchArcade
(13 March 2018, read through the r.jina.ai reader, the site refusing direct requests): "now the
entire width of the map is visible to players at once". Ours shows the whole width at 16:9 too.

**Level design writing.**
- The Level Design Book, "Balance" ([book.leveldesignbook.com/process/combat/balance](https://book.leveldesignbook.com/process/combat/balance)):
  "distribute 3-4 chokepoints across the map, one chokepoint per lane. It should be impossible to
  cover all chokepoints from a single point"; "For PvP gameplay, err on too little cover"; soft
  cover "blocking sight but not projectiles", such as foliage; symmetry by a mirror or a turn of 180.
- World of Level Design (AlexG, 2013): "Choke points must be placed before the attacking team reaches
  the map's objective"; both teams should reach a chokepoint at about the same time.
- Game Developer, GDC 2018 Level Design Workshop (Andrew Yoder): "When wide flank routes offer the
  best tactical options, gameplay can become diluted across the map".
- Game Developer, Riot on the 3v3 Twisted Treeline (2010): with "your enemy's tower sitting just over
  a screen away from your own" the lanes felt "extremely cramped".
- Riot, Wild Rift "Map and Modes": the red side is "rotated a full 180 degrees" so both teams get the
  same view (our maps do the same).

**Not found:** a Supercell text on its map design philosophy; official tile counts of the grid or
the IKE range (only the data files); the footprint of the IKE and the safe in tiles; the size of the
Brawl Arena maps (not in the newest `maps.csv` found). Reddit, archive.org (offline that night),
NamuWiki and itch.io refused the research pass.

## 4. Rules for our siege map

Drawn from sections 1 and 3, in our own units:

1. Keep the 21 x 33 arena: it is the Brawl Stars 3v3 grid, the whole width is on screen with the x of
   the camera locked, and the walks are already Brawl-like (spawn to centre 6 to 7 s).
2. Turn by 180 degrees around the centre, as today: each team sees the same map from its camera.
3. The core where Brawl Stars puts its objective (rows 3 to 4 from the back, centred, today's 12.5),
   spawns behind it, outside a core of radius 1.5.
4. Walls on both sides of the base, two thick, and a mouth the tower's range covers entirely: the
   core can be hit from outside the tower's range only from a few spots, by the longest attacks.
5. A block 4 to 5 rows in front of the tower: no straight shot down the middle at it.
6. Three lanes, one chokepoint each, before the tower; none of them covers the others.
7. Bushes on the flanks and in the centre, not within 7 of a tower; walls about 13 to 15 % of the
   cells, bushes 8 to 15 %.
8. No passage one cell wide (Brawl's rule, and the KCC's diameter is one unit).
9. An open centre that both teams reach at the same time: the place for a contested object later
   (the bolts of a siege bot).

## 5. The candidates and their measures

Measured by `draw_layouts.py` for team 0 attacking team 1 (the maps are the same turned); the core
at radius 1.5 for every map, today's included. "Reached safely": reached from the centre without
standing within 5 of the red tower. Walks from the middle spawn, 8 ways, at 2.75 units a second.

| | Today (Map1) | A. Three lanes | B. Keep and gates | C. Bigger |
| --- | --- | --- | --- | --- |
| Arena | 21 x 33 | 21 x 33 | 21 x 33 | 25 x 37 |
| Structure positions | code | same as today | same as today | moved back 2 |
| Blocks / bushes (cells) | 86 / 138 | 103 / 84 | 101 / 72 | 116 / 124 |
| Blocks / bushes (share) | 12.4 % / 19.9 % | 14.9 % / 12.1 % | 14.6 % / 10.4 % | 12.5 % / 13.4 % |
| Bush cells within 7 of own tower | 24 | 2 | 0 | 0 |
| Poke cells on the enemy tower (all, safe) | 47, 47 | 52, 24 | 30, 30 | 60, 26 |
| Core chip cells (all, safe) | 91, 91 | 43, 12 | 34, 3 | 64, 22 |
| Walk to centre / enemy tower / enemy core (s) | 6.6 / 9.5 / 11.3 | 6.1 / 9.0 / 10.1 | 6.7 / 9.8 / 11.1 | 6.8 / 10.8 / 11.9 |
| Towers see each other | yes | no | no | no |
| One-cell passages | 0 | 0 | 0 | 0 |
| A spawn inside a core of radius 1.5 | yes (0, -14) | no | no | no |

**A. Three lanes.** The base is closed on both sides by walls two thick from the back wall to
y -10, with a one-cell return at x +-4: its mouth (x -3..3) lies wholly within the tower's range.
Spawns at (-2, -15.5), (0, -15.5), (2, -15.5). Cover beside the tower at x +-3 (y -8..-7), a block
x -1..1 at y -4 (4.5 rows in front of the tower), dividers x +-5..6 (y -6..-4) between the middle and
the side lanes. Side lanes along the walls with bushes two wide that curve in towards the base
(Factory Rush); an open centre with two bushes of 2 x 3, walls at x +-7..8 and one block at the very
centre (where the decorative chest stands). Chokepoints: middle at y -4 (two gaps of 3 around the
block), each side between its divider and the bushes. The 12 safe chip cells all stand within 3 of
the edge of the tower's range (8 of them within 1) and 5.2 to 8.5 from the core's edge: a step in
puts the hero under the tower, and only mid and long attacks reach the core from there.

**B. The keep and its two gates.** The core inside a keep closed on three sides; its front wall
(x -2..2) stands behind the tower; two gates two wide at x +-3..4, both within the tower's range; a
wall 7 wide closes the middle 3.5 rows in front of the tower, so two lanes come round to the tower's
square from the sides. The strongest defence (3 safe chip cells) and the longest walk. Risks: gates of
two cells for six heroes and bots, spawns bottled in the keep, no middle lane, which reads less like
the "lanes towards each tower" asked for.

**C. Bigger, 25 x 37.** A's plan on a larger arena, structures 2 further back. Brawl Stars did make
Siege bigger (27 x 39) but went back to 21 x 33 for Spirit Wars. 25 is the widest that stays on a
16:9 screen with the camera's x locked (26.8 at the bottom edge, 26.5 needed with walls); 27 would
need the camera pulled back or its x unlocked. Costs: walls, floor and camera bounds (MinZ, MaxZ) of a
new size, structure positions in code for this map only, and 1 to 2 s more on each walk to the enemy
base, in a match of 180 s. Its larger mouth leaves 22 safe chip cells.

## 6. Recommendation

**A**, as the siege's own map. It is what was agreed with the owner (lanes towards each tower, cover
near the towers, bushes, a contested centre), it follows what Brawl Stars does on the same grid
(objective rows 3 to 4, spawns behind, walls on its sides, side bushes curving in, a block in front of
the turret, few bushes near it, three lanes as in Brawl Arena), and it costs least: no new size, no
camera change, no structure position in code. Against today it closes the long sightline between the
towers, cuts the cells that chip the core from safety from 91 to 12, takes the bushes away from the
towers (24 cells to 2), and has the spawns outside a core of radius 1.5. B is the fallback if play
shows cores falling too easily on A; C only if play shows the arena too small, which nothing measured
says.

## 7. Implementation plan

### 7.1 The simulation batch (one new stamp)

- **Core radius:** `Structures.CoreRadius` from `FP._0_75` to `FP._1 + FP._0_50` (1.5)
  ([Structures.cs:17](../../../game/Assets/QuantumUser/Simulation/System/Structures.cs)), and the
  comment of `StructureViews.CoreScale` ([StructureViews.cs:87-89](../../../game/Assets/Chog/Match/StructureViews.cs)).
  Measured on `Assets/Chog/Resources/ChogStructures.bytes` (the library the game draws, records 3
  and 4, read with a parser of the CHOGPARTS5 format of `tools/blender/export_chog_parts.py`): the
  core's plinth has a radius of 1.065 at the ground (below 0.35 high; `CORE_PLINTH` 1.02 plus its
  outline in `make_chog_structures.py:91`), its widest part 1.173 (leaves and heart), height 1.77;
  drawn 1.5 times larger ([StructureViews.cs:521](../../../game/Assets/Chog/Match/StructureViews.cs)):
  1.60 and 1.76. With 1.5 a hero (radius 0.5) stops with its centre 2.0 away, its body touching the
  plinth's outline. Today, with 0.75, it walks 0.85 into the plinth. The tower has the same kind of
  gap (plinth 0.905, widest 1.077, collider 0.6): optional in the same batch, at 0.85, but on A the
  passage right of the tower (x 1.1 to 2.5 today) would narrow to 1.15.
- **Towers need a line of sight** (recommended): in `Structures.Shoot` skip a hero when
  `LineOfSightHelper.HasLineOfSight(frame, tower, hero)` is false
  ([LineOfSightHelper.cs:10-22](../../../game/Assets/QuantumUser/Simulation/System/LineOfSightHelper.cs),
  Static layer only). Without it the cover beside the tower protects from nothing and the beam crosses
  blocks. `KeepAway` stays as it is (range only).
- **Optional, a Brawl Arena rule:** the core cannot be hurt while its tower stands. It would make the
  tower the gate whatever the map; it changes the siege's scoring and is the owner's call.
- Tests in `tools/quantum/Program.cs`: the core radius; a tower does not shoot through a block; and
  the structure placement test (lines 344-361) stays valid since A keeps the positions.
- Map1 for TowersAndCoins (records made before 16 October only): its spawn (0, -14) would stand
  inside a core of 1.5. No menu plays that mode; the two fixtures that do are re-recorded (7.7).

### 7.2 The siege map's assets

- `Assets/QuantumUser/Scenes/GameplaySiege.unity`, a copy of Gameplay.unity. **First thing after the
  copy, before any save:** its `QuantumMapData.AssetRef` to a new `QuantumUser/Resources/SiegeMap.asset`
  (Create > Quantum > Assets > Map). Saving the copy while it still points to Map1 bakes the siege
  layout into Map1 (the save bakes, section 2) and changes the coins map.
- In the copy: delete `Tiles_HidingTest` and the `Tilemap_Level1` instance; a root `Layout_Siege`
  with `Crate.prefab` and `InvisibleProp.prefab` instances at the cells of layout A
  ([layouts.txt](layouts.txt), x = column, y = row, on the floor at height 0); spawn points
  (-2, -15.5), (0, -15.5), (2, -15.5) and turned for team 1; objective points (-3.5, -2), (3.5, -2),
  (0, -2) and turned; escape points (-3.5, -15), (0, -16), (3.5, -15) and turned; the chest on the
  centre block. Walls, floor and `GameEssentials` (camera bounds) unchanged.
- Cover points generated again for the new blocks with the four `CoverPointAsset_*` assets set.
- A NavMeshSurface under `MapData/NavMesh`, listed in its `QuantumMapNavMeshUnity`, agent radius
  0.25 as Map1's; baked from the blocks and walls only (bush meshes must not carve it: a temporary
  BoxCollider per static box collider and a ground box, built and removed by the bake script, is the
  sure way), plus the two cores and towers as obstacles (their positions are fixed on this map; the
  navmesh does not know dynamic colliders, and the middle spawn is straight behind the core).
- The result: `SiegeMap.asset`, `SiegeMap_mesh`, `SiegeMap_NavMesh`, `SiegeMap_NavMesh_data`; the
  navmesh keeps the name "NavMesh" that the AI asks for.

### 7.3 Editor steps, by script

An Editor class, e.g. `Assets/Chog/Editor/SiegeMapBuilder.cs` (menu Chog > Maps > Build siege map,
and a request word `maps` in `BuildRequestWatcher`), that: opens the siege scene; checks that its map
is SiegeMap and not Map1 (refuses otherwise); rebuilds `Layout_Siege` from the layout text; places the
points; writes the cover points; bakes map data twice
(`QuantumEditorAutoBaker.BakeMap(data, BakeMapData | GenerateAssetDB)`); builds the temporary
navmesh geometry, bakes with `BakeUnityNavMesh | ImportUnityNavMesh | BakeNavMesh`, removes it; saves
the scene and the assets; `QuantumUnityDBUtilities.RefreshGlobalDB()` and checks that SiegeMap is
found. All of these calls are public (section 2). Then the usual build request.

### 7.4 View code (no stamp)

- `MatchModeChoice.Apply`: `scene.RuntimeConfig.Map` = SiegeMap for SIEGE, Map1 for COINS (GUIDs
  looked up by path in the Quantum database, tested in the view tests).
- `ChogArenaSkin.ArenaScene`, `ChogSounds.MatchScene`, `GenesisEffects.MatchScene`: accept every scene
  whose name starts with `Gameplay`.
- `ProjectSettings/EditorBuildSettings.asset`: the new scene, enabled.

### 7.5 Bots

Navmesh baked for the new layout (above); passages are all two cells or more (mouth 7, lanes 4,
middle gaps 3); `KeepAway`, `BotObjective` and the alarm read positions from the entities, unchanged;
cover points with assets; bushes baked twice for their neighbours; objective points around the
centre for "Guard Objective"; escape points in the base for "Run". To check without a browser: a
`--map <guid>` option in `tools/quantum/Verifier.cs` (beside `--mode`, lines 86-91) and in
`watch_bots.py`, then `watch_bots.py <record> 10 --all-bots --mode Siege --map <SiegeMap>`: every
seed must end without exception, bots must reach and hurt the enemy tower, and the counts of fallen
towers and cores compared with today's (ten sieges of six bots: five ended by a core, STATUS of
16 October).

### 7.6 Risk to the coins mode

Low if A is the siege's own map: Map1, its navmesh and its scene are not touched; the coins mode
gets the new stamp only. Three ways to break it, each with its check: baking the copied scene while it
still points to Map1 (check: `list_map_points.py` still finds Map1 with 424 colliders, 156 entities);
the global `InvisibilityGroups` asset rewritten by the siege bake (read by no simulation code; to be
made per map later); the tools that read "the first map" of the export (`draw_map.py`,
`free_ground.py`, `list_map_points.py` take the first map with colliders) silently reading the wrong
map: give them a `--map` option.

### 7.7 Judge fixtures

`tests/fixtures/replays/` holds two records of 12 October, TowersAndCoins on Map1, judged by
`tools/release/check_judge.sh`. The core radius changes that mode (structures present), so their
winners may change. Record again, with the new build: one siege on SiegeMap and one match of coins on
Map1 (`sh tools/unity/checks/browser_check.sh match <base url> <label> '1795&record=1' 330`, as the
header of check_judge.sh says), keep the old two only if they still agree, name each
`...-winnerN-tickT.json`.

### 7.8 Checks

1. `tools/quantum/headless.py test` (simulation tests, new ones included) and the view tests
   (`tools/dev/view_tests.sh`); `tools/release/check_all.sh`.
2. The export: `list_map_points.py site/quantum-db.json` lists Map1 unchanged and SiegeMap with its
   blocks, bushes, cover points and points; `draw_layouts.py` and `draw_map.py` on SiegeMap match
   this folder's picture A cell for cell.
3. `watch_bots.py` on SiegeMap (7.5).
4. One local build: its stamp connects to Photon (no `ReturnCode 32767` in the console), then muted
   headless matches through Photon's cloud: a siege on the new map (the scene loads, painted, sounds,
   effects warmed), a match of coins on Map1, `?perf=1` on both.
5. `check_judge.sh` with the new fixtures.
6. The owner on a computer and a phone: lanes readable, camera, bushes.

### 7.9 Order

Simulation changes and the builder first (no build), the bake in the open Editor, the checks of 7.8
1 to 3 headless, one build, the matches, the fixtures, then deploy once. Rough effort: the builder
and the bake half a day; code and tests two hours; checks and fixtures two hours.

## 8. Limits

- The measures are geometry on cells (straight shots from cell centres, walks on cells), not play:
  only matches tell whether a lane is fun or a base too strong.
- The longest attack (9) and the speeds are read from the code's comment and the class prefabs; the
  real reach of each Genesis attack was not listed.
- The camera frames assume 16:9; phones in landscape are usually wider (19.5:9 shows 32.6 to 38.6
  units), a 4:3 window narrower.
- Brawl Stars numbers in tiles are derived from data files with unit conversions that Supercell does
  not publish (a third of a tile per range unit, 300 units per tile).
- Map1's navmesh was baked by the sample's authors; how it was built is not recorded in the scene.

## Sources

Loaded during the research pass of 17 October 2026 (the route actually used is given; nothing was
reconstructed).

- Brawl Stars game data, [tailsjs/brawl-stars-patches](https://github.com/tailsjs/brawl-stars-patches),
  default branch: [maps.csv 59.212.1](https://raw.githubusercontent.com/tailsjs/brawl-stars-patches/HEAD/59.212.1/csv_logic/maps.csv),
  [tiles.csv 65.219.1](https://raw.githubusercontent.com/tailsjs/brawl-stars-patches/HEAD/65.219.1/csv_logic/tiles.csv),
  [locations.csv 64.239.1](https://raw.githubusercontent.com/tailsjs/brawl-stars-patches/HEAD/64.239.1/csv_logic/locations.csv),
  [characters.csv 66.306.1](https://raw.githubusercontent.com/tailsjs/brawl-stars-patches/HEAD/66.306.1/csv_logic/characters.csv),
  [game_mode_variations.csv 63.351.1](https://raw.githubusercontent.com/tailsjs/brawl-stars-patches/HEAD/63.351.1/csv_logic/game_mode_variations.csv),
  [texts.csv 62.281.1](https://raw.githubusercontent.com/tailsjs/brawl-stars-patches/HEAD/62.281.1/localization/texts.csv);
  older `characters.csv` for the IKE range of 35:
  [LekmaDev v37](https://raw.githubusercontent.com/LekmaDev/Brawl-Stars-Server-V37/HEAD/GameAssets/csv_logic/characters.csv),
  [LkPrtctrd v41](https://raw.githubusercontent.com/LkPrtctrd/BSL.v41/HEAD/BSL.v41/SaveBase/Assets/csv_logic/characters.csv),
  [ShadowwDev v36](https://raw.githubusercontent.com/ShadowwDev/BrawlStars-v36/HEAD/GameAssets/csv_logic/characters.csv).
- Tile legend: [bloodwiing/BrawlMapGen presets/brawlstars.json](https://raw.githubusercontent.com/bloodwiing/BrawlMapGen/HEAD/presets/brawlstars.json).
- Brawl Stars Fandom wiki, read through its API only:
  `https://brawlstars.fandom.com/api.php?action=parse&page=<PAGE>&format=json&prop=wikitext` with the
  pages Siege, Spirit_Wars, Brawl_Arena, Heist, Map_Maker, Version_History/Beta and /2019 to /2026,
  Nuts_%26_Bolts, Factory_Rush, Bot_Drop_(Siege), Junk_Park, Safe_Zone, Hot_Potato, Bridge_Too_Far,
  Pit_Stop; the reverted 2024 claim that Siege came back:
  [revision comparison 170494 to 170633](https://brawlstars.fandom.com/api.php?action=compare&fromrev=170494&torev=170633&format=json).
- Liquipedia, Siege page through its API: [liquipedia.net/brawlstars/api.php?action=parse&page=Siege](https://liquipedia.net/brawlstars/api.php?action=parse&page=Siege&format=json&prop=wikitext).
- Supercell: [patch notes "Angels vs Demons"](https://supercell.com/en/games/brawlstars/blog/game-updates/patch-notes-angels-vs-demons/)
  (Spirit Wars) and [release notes June 2025](https://supercell.com/en/games/brawlstars/blog/game-updates/release-notes-june-2025/).
- NamuWiki, Siege page (Korean), Wayback Machine capture of 17 February 2022 (Siege Bot timing and
  health, IKE range history), secondary:
  [web.archive.org capture](https://web.archive.org/web/20220217040236id_/https://namu.wiki/w/%EB%B8%8C%EB%A1%A4%EC%8A%A4%ED%83%80%EC%A6%88/%EA%B2%8C%EC%9E%84%20%EB%AA%A8%EB%93%9C/%EC%8B%9C%EC%A6%88%20%ED%8C%A9%ED%86%A0%EB%A6%AC?from=%EC%8B%9C%EC%A6%88%20%ED%8C%A9%ED%86%A0%EB%A6%AC).
- TouchArcade, 13 March 2018, ["Brawl Stars" Landscape Update Angers Community](https://toucharcade.com/2018/03/13/brawl-stars-landscape-update-angers-community/)
  (read through `https://r.jina.ai/` in front of that address).
- Samurai Gamers, [Brawler Stats Comparison](https://samurai-gamers.com/brawl-stars/brawler-stats-comparison/) (21 January 2019).
- The Level Design Book, [Balance](https://book.leveldesignbook.com/process/combat/balance).
- World of Level Design, AlexG, 17 December 2013,
  [6 Principles of Choke Point Level Design for Multiplayer Maps](https://www.worldofleveldesign.com/categories/csgo-tutorials/csgo-principles-choke-point-level-design.php).
- Game Developer, Andrew Yoder, 23 February 2018,
  [GDC 2018 Level Design Workshop: An expert roundtable Q&A](https://www.gamedeveloper.com/design/gdc-2018-level-design-workshop-an-expert-roundtable-q-a).
- Game Developer, David Dunne (Riot Games), 31 March 2010,
  [Twisted Timeline: Iterative Level Design in a Competitive Online Game](https://www.gamedeveloper.com/design/twisted-timeline-iterative-level-design-in-a-competitive-online-game).
- Riot Games support, 22 April 2022, [Wild Rift: Map and Modes](https://support.riotgames.com/en-us/wild-rift/gameplay/map-and-modes).
- Surrender at 20, June 2014, [Update to Summoner's Rift headed to PBE](https://www.surrenderat20.net/2014/06/update-to-summoners-rift-headed-to-pbe.html).
- Photon Quantum 3 manual: [Map Baking](https://doc.photonengine.com/quantum/v3/manual/maps/map-baking),
  [Importing a Unity navmesh](https://doc.photonengine.com/quantum/v3/manual/navigation/workflow-navmesh)
  (read as Markdown, `.md` after the address).

## Reproduce

From the repository root, with Pillow (the tools virtualenv has it):

```sh
.artifacts/tools-venv/bin/python research/evidence/siege-map-layouts-2026-10-17/draw_layouts.py \
    site/quantum-db.json research/evidence/siege-map-layouts-2026-10-17
```

It reads Map1 from the exported assets, builds the candidates from the rectangles written in the
script (team 0's half, turned for team 1), and writes the five pictures, `layouts.txt` and
`measures.json`. The Brawl Stars counts: download `maps.csv` (Sources) into a folder of its own, then
`.artifacts/tools-venv/bin/python -I research/evidence/siege-map-layouts-2026-10-17/brawl_map_stats.py <that maps.csv>`.
