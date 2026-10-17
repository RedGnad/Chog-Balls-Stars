# Performance audit, phase 1 (static audit and research), 17 October 2026

Scope: why Chog Arena "lags a bit", with phones first (PWA in Android Chrome and iOS Safari),
desktop browsers second. Phase 1 is reading only: no build, no browser, no server was started.
Phase 2 (measurement under the build lock) is specified in section 7.

Evidence levels used throughout:

- **Confirmed**: read in the code, a setting, a build graph or a prefab, with file:line.
- **Measured**: a number that already exists in `.artifacts/perf-reports.jsonl`, in
  `docs/STATUS.md` or in a recorded console log. Nothing new was measured in this phase.
- **Inferred**: reasoning from confirmed facts and sources. Every gain estimate is inferred.

Code state read: `main` at 35ec248 (with 68c336a, the fight effects) plus the working tree of
17 October evening (another agent is still editing `StructureViews.cs`, `PhotonIdentity.cs`,
`TopDownInput.cs`, `AttackPreview.cs`; line numbers in those files may move).

## 1. Summary

1. **No phone has been measured since 10 October.** Since then the game gained the 3D heroes
   (6,200 to 8,000 triangles each), the matcap shaders, the 3D specials, 23 pooled particle
   effects with faked lights and lightning, rings and shadows under every hero, and desktop
   post-processing. The Mac (M1) shows no main-thread growth from those additions, but the Mac
   says nothing about a Mali GPU. Measuring the phone is step 0 of the plan.
2. **On the phone the limit was the main thread** (10 October: busy p50 19 to 28 ms, of which
   simulation 3 to 9 ms and view plus rendering 15 to 18 ms), with spikes (p99 45 to 120 ms,
   worst frames 158 to 1,552 ms). The GPU side is now unknown.
3. **Correction to the brief: the project uses the Built-in Render Pipeline, not URP**, and its
   post-processing is a custom `OnRenderImage` pass, not the Post Processing Stack v2 package
   (installed but unused). Built-in is deprecated since Unity 6.5 but supported through the 6.7
   LTS: not an issue for 23 October.
4. Top items, all view or settings only unless flagged:

| # | Item | Expected gain (inferred) | Touches the simulation stamp |
| --- | --- | --- | --- |
| 0 | Instrument and measure on the phone (draw calls, GC count, separate the combat log) | none by itself; decides everything below | no |
| 1 | Cap the canvas pixel ratio in the page template | 44% fewer pixels on a DPR 2 phone at a 1.5 cap, 56% on a DPR 3 iPhone at a 2 cap; GPU only | no |
| 2 | Pool Quantum entity views; stop instancing materials per attack and per hero (they leak for the whole session) | removes several Instantiate/Destroy per second plus their garbage; stops a memory leak across matches | no (to verify on the build's stamp line) |
| 3 | Warm the effect and special shaders while a match loads | removes first-use stalls (tens to hundreds of ms each) | no |
| 4 | Remove per-frame garbage (HUD strings, scoreboard, `PhotonIdentity` concat every frame app-wide) | fewer full stop-the-world collections (Web has no incremental GC) | no |
| 5 | Cheaper effect shader (eye-picture math runs on every effect pixel, including additive flares) | GPU fragment cost of all fight effects | no |
| 6 | Turn off Unity's own physics stepping and stack traces for Log/Warning | small CPU per frame; honest perf runs | no |
| 7 | Split the moving HUD followers into their own canvas | the whole gameplay canvas is re-batched every frame today; size unknown until profiled | no |
| 8 | Simulation-side: input delay 1 to 2 ticks and a verified-ticks cap (client session, no stamp); AI and physics tuning only if sim-bound (stamp change, batch once) | roughly a quarter to a third of predicted re-simulation per tick of delay | first two no; the rest yes |

## 2. What is already measured

### 2.1 Phone (Mali-G52 MC2, Android 10, Chrome 126), builds of 9 and 10 October

Source: `.artifacts/perf-reports.jsonl`, 15 reports posted by the phone on the LAN with
`perf=1` (one per minute), quality "Fast", 30 Hz simulation, online against bots.
Same numbers in `docs/STATUS.md` (entries of 9 and 10 October) and
`research/evidence/genesis-effects-perf-phone-2026-10-09.json`,
`research/evidence/quantum-tuning-runs-phone-2026-10-09.json`.

| Metric (ms unless stated) | Range over the 15 reports |
| --- | --- |
| frame p50 | 23 to 34 |
| frame p95 | 39 to 56 |
| frame p99 | 45 to 120 (one report 1,000) |
| worst frame | 158 to 1,552 |
| frames over 40 ms | 8.2 to 20.2 % |
| main-thread busy p50 | 19.2 to 27.8 |
| busy minus simulation ("other") p50 | 15.1 to 18.3 |
| simulation (Quantum update) p50 / p95 | 3.0 to 9.2 / 11.3 to 19.0 |
| predicted ticks per update, average | 2.5 to 4.3 |
| ping | 47 to 161 |
| managed heap (`GC.GetTotalMemory`) | 25 to 39 MB (one report 71 MB) |
| canvas | 1508x560 to 1650x720 in landscape (about 0.84 to 1.19 Mpx), one run 720x1396 in portrait |

Reading (inferred): busy is close to the frame time, so the phone was main-thread bound and
quantised to about 30 fps on a 60 Hz panel. The simulation took a third of the busy time at
best; the view and the rendering submission took the rest. The tail (p99, worst) is spikes,
not a steady cost. Also from STATUS (10 October): about one own attack start in eleven was
drawn then undone by a correction, so part of a "lag" feeling can be prediction, not frames.

### 2.2 Mac (M1, Metal through headless Chrome), 10 and 17 October

| Date, configuration | busy p50 | other p50 | sim p50 | frame p50 / p95 | heap |
| --- | --- | --- | --- | --- | --- |
| 10 Oct, Good, 2842x1576, flat heroes | 3.9 to 4.3 | 3.5 to 3.8 | 0.4 to 0.6 | 17 / 18 | 28 to 32 MB |
| 17 Oct 20:53 to 21:13, Good, 1280x720, 3D heroes, effects Full | 3.7 to 4.7 | 3.3 to 4.1 | 0.4 to 0.7 | 17 / 17 to 18 | 36 to 48 MB |
| 17 Oct 21:08 to 21:34, same, effects Light | 3.8 to 4.5 | 3.3 to 3.9 | 0.5 to 0.8 | 17 / 17 to 18 | 36 to 46 MB |
| same, `fx=0` | 4.3 to 4.7 | 3.8 to 4.1 | 0.6 to 0.7 | 17 / 18 | 37 to 41 MB |

Reading: on the Mac, main-thread cost did not grow with the 3D heroes and the effects, and the
effects on, light or off are inside the noise (as 68c336a says). The managed heap grew by about
10 to 15 MB. The afternoon runs of 17 October with 1 s frames every second are not usable
(the Mac was swapping 11 to 13 GB, STATUS 17 Oct 17:25 and 18:45). Headless runs without
`CHOG_BROWSER_GPU=1` use SwiftShader and say nothing about rendering.

### 2.3 A measurement artefact found

`?perf=1` also starts `CombatLog` (`game/Assets/Chog/Diagnostics/PerformanceHud.cs:112`), which
writes a console line per cast, skill and damage (`CombatLog.cs:53,86,92,97`). In the recorded
scripted match `.artifacts/unity/check-juice-7/browser-smoke-events.json` that is **1,899
`CHOG_COMBAT` lines in about 194 s**, about 10 per second, each with a script stack trace
(`ProjectSettings.asset:57`, every log type set to ScriptOnly). Every perf run so far, phone
included, paid that cost; real players do not. Confirmed (count); its cost per line on the phone
is not measured.

## 3. Corrections and facts about the setup

- **Render pipeline: Built-in.** `ProjectSettings/GraphicsSettings.asset:43`
  `m_CustomRenderPipeline: {fileID: 0}`, no URP package in `Packages/manifest.json`, forward
  rendering (`ProjectSettings.asset:796`). Unity: "The Built-In Render Pipeline is deprecated and
  will be made obsolete in a future release ... It remains supported ... through the full Unity
  6.7 LTS lifecycle" ([6.5 upgrade guide](https://docs.unity3d.com/6000.6/Documentation/Manual/UpgradeGuideUnity65.html)).
- **Post-processing is ours**, `game/Assets/Chog/Match/ChogMatchPost.cs:82-108` with
  `Resources/ChogMatchPost.shader` (half-size prefilter, two downsamples, two upsamples, a
  full-size FXAA plus grading pass). Attached only at quality "Good" and above
  (`ChogMatchPost.cs:31-45`), so never on a phone. `com.unity.postprocessing` 3.5.4
  (`manifest.json:14`) has no component in any scene or prefab; only its define remains.
- **HDR**: the camera allows HDR (`TwinStickShooter/Prefabs/GameEssentials.prefab:670`), but in
  Built-in forward "a Camera only supports HDR if you have a post-processing effect on the
  Camera" ([hdr-birp](https://docs.unity3d.com/6000.6/Documentation/Manual/hdr-birp.html)): phones
  render LDR; desktops with the post pass render to an HDR intermediate target. No change needed.
- **Dynamic batching is gone in Unity 6.6** ("Dynamic batching is now obsolete",
  [6.6 upgrade guide](https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity66.md)).
  Every small code-built mesh (rings, flares, pieces of a special) is its own draw call unless
  instanced. The arena props are batching-static (`StaticEditorFlags` 87/95/119 on 170+ objects
  of `QuantumUser/Scenes/Gameplay.unity`) and the arena reskin edits shared materials
  (`Screens/ChogArenaSkin.cs:54-69`), so static batching survives.
- **Quantum 3.0.9** (`Assets/Photon/Quantum/build_info.txt:1`), current stable is 3.0.14 (5 Oct
  2026); 3.1 is still a preview "not intended to be used for live or released games". Photon's
  compatibility table lists Unity 6000.5 as the newest fully supported version; this project is
  on 6000.6.4f1 ([initial setup](https://doc.photonengine.com/quantum/v3/getting-started/initial-setup)).

## 4. Static audit, by area

### 4.1 Quality levels, lights, shadows

- Levels (`ProjectSettings/QualitySettings.asset`): no per-platform default for WebGL
  (`:235-237`), so the game picks at first run: "Fast" on a mobile browser, "Good" on a desktop
  (`Chog/Presentation/QualityDefaults.cs:12,16`, applied by `DeviceQuality.cs:20-25` unless the
  menu already stored a level under `Photon.Menu.QualityLevel`).
- "Fast" (`QualitySettings.asset:47-84`): no shadows, pixel lights 0, no MSAA, no anisotropic,
  no soft particles. Right for phones.
- "Good" (`:123-158`): soft shadows (lights are `m_Type: 2`, `GameEssentials.prefab:335,522`),
  2 cascades, medium resolution, distance 40 raised to `ChogCameraRig.ShadowReach` when lower
  (`ChogMatchLook.cs:149-150`), 2 pixel lights, anisotropic, realtime reflection probes on.
  Desktop only.
- Shadow casters on desktop: heroes cast with their outline shells (`ChogHero.shader:133-136`,
  `Cull Off`), and 13 of the 74 particle renderers of the effect prefabs cast shadows
  (`Chog/Effects/Resources/ChogFx/*.prefab`, `m_CastShadows: 1`). Cascades multiply shadow draw
  calls ([shadow cascades](https://docs.unity3d.com/6000.6/Documentation/Manual/shadow-cascades-performance.html)).
- No real-time light is spawned by effects: `GenesisEffects.cs:118-119` sets
  `CFXR_Effect.GlobalDisableLights` and `GlobalDisableCameraShake`, and `ChogFxPlayback.Prepare`
  disables every `Light` (`GenesisEffects.cs:329-330`). Lights are faked with additive meshes
  (`ChogLight.cs`, `ChogLightning.cs`). Confirmed.
- Risk: `Application.isMobilePlatform` decides the phone path for quality and effects
  (`GenesisEffects.cs:68`, `DeviceQuality.cs:23`). Unity warns it "might not always report
  accurate information" on the Web
  ([isMobilePlatform](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Application-isMobilePlatform.html));
  an iPad in desktop mode would likely get "Good", soft shadows, post and full effects
  (inferred). The Redmi reported `mobile: true` (measured).

### 4.2 Canvas size and the page

- `game/Assets/WebGLTemplates/Chog/index.html:195-235`: the Unity config sets no
  `devicePixelRatio`, so the canvas renders at the browser's full ratio. Measured on the Redmi:
  1650x720 (1.19 Mpx). An iPhone at DPR 3 in landscape renders about 2532x1170 (2.96 Mpx),
  2.5 times the Redmi's pixels (inferred from Apple's scale factor of 2 or 3,
  [UIScreen.scale](https://developer.apple.com/documentation/uikit/uiscreen/scale)). Unity:
  setting `devicePixelRatio` lower "can help performance on lower-end mobile devices"
  ([template configuration](https://docs.unity3d.com/6000.6/Documentation/Manual/web-templates-build-configuration.html));
  decoupling sizes helps "conserve the GPU fill rate"
  ([canvas size](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-canvas-size.html)).
  Dynamic Resolution is not available on the Web
  ([dynamic resolution](https://docs.unity3d.com/6000.6/Documentation/Manual/DynamicResolution-introduction.html)).
- Frame pacing: `Application.targetFrameRate` stays at the menu default -1
  (`Photon/QuantumMenu/Runtime/QuantumMenu.Common.cs:413,509-510`), so the browser paces frames;
  on 90 to 144 Hz screens the game draws that often with a 30 Hz simulation. Unity recommends -1
  unless throttling is wanted ([Web performance](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-performance.html)).
  iOS Low Power Mode caps requestAnimationFrame at 30 fps by design
  ([WebKit 215745](https://bugs.webkit.org/show_bug.cgi?id=215745)).

### 4.3 Player and build settings (Web)

| Setting | Value | Where | Note |
| --- | --- | --- | --- |
| Graphics API | WebGL 2 only, not automatic | `ProjectSettings.asset:445-447` | fine |
| Color space | Gamma | `:49` | fine; no change (art) |
| Code optimization | compile `-O3`, link `-O2`, no `-flto` | `Library/Bee/Playere48532bf.dag.json` (last build), `Library/BuildProfiles/PlatformProfile.84a3...asset` `m_CodeOptimization: 4` | runtime speed without LTO; LTO is for "final releases or performance testing" ([mobile](https://docs.unity3d.com/6000.6/Documentation/Manual/web-optimization-mobile.html)) but builds take far longer on this Mac |
| Development build | off (`BuildOptions.None`) | `Chog/Editor/QualificationBuild.cs:119-125` | numbers are release numbers |
| Exceptions | explicitly thrown only | `:712` | wasm exceptions (`-fwasm-exceptions` in the graph); keep |
| Stack traces | ScriptOnly for every log type | `:57` | Photon: set Log and Warning to None on WebGL ([Quantum WebGL](https://doc.photonengine.com/quantum/current/manual/webgl)) |
| Compression | Gzip | `:722` | load time only |
| Data caching | off | `:715` | every visit downloads again (12.4 MB data and 12.6 MB wasm, gzip, build 20261017T193628403Z); load time only |
| Memory | initial 32 MB, max 2048, geometric | `:727-729` | Unity: set the initial size "to the typical heap usage" on mobile ([memory](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-memory.html)) |
| Threads | off | `:725` | needs COOP/COEP headers; see 6.4 |
| Incremental GC | on in settings | `:791` | "The web platform doesn't support incremental garbage collection" ([incremental GC](https://docs.unity3d.com/6000.6/Documentation/Manual/performance-incremental-garbage-collection.html)) |
| Managed stripping | Low | `:780` | size; raising it risks reflection breaks |
| Frame Timing Stats | off | `:160` | not needed (no GPU time on WebGL anyway) |
| Diagnostics overlay | off | `:714` | useful in a measurement build (WASM heap on the phone) |
| Defines | `QUANTUM_LOGLEVEL_DEBUG` | `:756` | debug-level Quantum logs compiled in; only 8 Quantum lines in the recorded match |
| Texture compression | Web default (DXT) | `PlatformProfile` `m_WebGLTextureSubtarget: 0`, `m_BuildTargetDefaultTextureCompressionFormat: []` | see 4.9 |

### 4.4 Shaders and GPU cost

- **Heroes** (`Chog/Resources/ChogHero.shader`): one material for all heroes, 4 meshes per hero
  (`ChogHeroModel.cs:157-170`), 6,180 to 7,984 triangles per hero (STATUS 15 Oct). The opaque pass
  is `Cull Off` (`:38`) and discards the outline shell's outside faces in the fragment shader
  (`:101-103`). Every triangle is rasterised from both sides and the shader contains `discard`,
  which on tile-based mobile GPUs prevents early depth rejection for that draw (inferred from how
  Mali/Adreno work; Unity's mobile advice is "pick ones from the Mobile or Unlit categories",
  [graphics performance](https://docs.unity3d.com/6000.6/Documentation/Manual/OptimizingGraphicsPerformance.html)).
  The shadow caster is also `Cull Off` and includes the shells (`:133-136`).
- **Effects** (`Chog/Resources/ChogEffect.shader:131-137`): the eye picture lookup
  (`normalize`, `acos`, a division, `tex2D`) runs for every fragment of every effect, then is
  masked by `step(0.5, i.core.w)`. Only eyeballs wear a picture, but the same shader draws the
  additive flares, floor pools, rings, columns and bolts of `ChogLight`/`ChogLightning`
  (`ChogEffectMaterials.cs:141-146`, material "Light", `Blend SrcAlpha One`, `Cull Off`). Those
  are the largest transparent surfaces of a fight. Confirmed code; cost inferred.
- **Ground** (`ChogGround.shader`): transparent, one per hero ring and per structure
  shadow/ring (`ChogTeamRing.cs:106-170`), plus lob shadows and landing rings
  (`ChogAttackLook.cs:111-132`). Small screens areas; cheap.
- **Particles**: 23 Cartoon FX prefabs, 2 to 8 systems each, summed authored max particles 1 to
  1,212 per prefab (`RainbowStars` 1,212). Phones and quality below Good take the lighter path:
  at most 30 particles per system, bursts and streams halved, 14 effects at once
  (`GenesisEffects.cs:32-40,319-353`). 31 of the 36 materials use `_FADING_ON`, but soft
  particles need `SOFTPARTICLES_ON`, which the quality levels in use leave off
  (`QualitySettings.asset` `softParticles: 0` for Fast and Good;
  `CFXR Assets/Shaders/CFXR.cginc:89`), so no depth texture is forced. 23 particle renderers are
  mesh particles (`m_RenderMode: 4`).

### 4.5 Draw calls (the main CPU suspect on the phone)

Unity: "the CPU side dispatch of WebGL operations is slower than in native OpenGL ... avoid large
numbers of draw calls per frame" ([Web performance](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-performance.html)).
No Unity budget exists; PlayCanvas gives "100-200 draw calls is a rough target for low end mobile
devices" ([PlayCanvas](https://developer.playcanvas.com/user-manual/optimization/guidelines/)).
Nothing in the project counts draw calls today. Contributions found (confirmed sources, counts
inferred for a busy 3v3 moment on a phone):

- heroes 6 x 4; attack objects in flight (one mesh each, `ChogHeroModel.cs:251-264`) with lob
  shadow and landing ring; hero rings 6 and structure shadow plus ring 4 x 2;
- each `EffectPiece` is a renderer with its own `MaterialPropertyBlock`
  (`GenesisSpecial3D.cs:88-141`, `ChogEffectMaterials.cs:103-113`): a laser alone is 9 pieces;
  `ChogLight` up to 16 live on a phone (40 on desktop), 2 pieces each; `ChogLightning` up to 4
  (10); particle effects up to 14 (40) with 2 to 8 systems each;
- UI: four screen-space canvases in the scene (`Gameplay.unity`: TeamStrategyCanvas,
  GameModeCanvas, UIButtonsCanvas, GameplayCanvas) plus the Chog HUD (scoreboard, desktop panel,
  coach), name labels and bars per hero, damage numbers.

Estimate: on the order of 100 to 250 draw calls in a fight (inferred, to measure). The shaders
`Chog/Effect` and `Chog/Ground` have no instancing variant, so identical meshes cannot batch now
that dynamic batching is gone.

### 4.6 Per-frame CPU and garbage in view code

Web specifics: the GC "only runs at the end of each program frame", per-frame temporary
allocations create "quadratic memory growth pressure"
([memory](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-memory.html)), and there is
no incremental GC. Measured heap 25 to 48 MB. Allocations found in per-frame paths (confirmed):

| File:line | What runs every frame | Garbage or cost |
| --- | --- | --- |
| `Chog/Screens/PhotonIdentity.cs:115-120` | `name + "=" + value` (the session token) to compare with what was given | one long string per frame, in menus and matches, whenever the server asks for sign-in (production) |
| `TwinStickShooter/Scripts/UI/MatchDurationUI.cs:11-19` | `TimeSpan` twice, two `ToString("00")`, a concat | 3 strings per frame |
| `UI/MatchTimerUI.cs:10-16` | `ToString()` | 1 string |
| `UI/TeamCollectiblesUI.cs:36-37` | two `ToString()` | 2 strings |
| `UI/NumberedBarUI.cs:14` | `ToString()` per bar | 1 per bar |
| `UI/CollectiblesUI.cs:47` | `ToString()` per hero | 6 strings |
| `Chog/Screens/ChogScoreboard.cs:111` | `foreach (Transform child in transform)` | an enumerator object |
| `ChogScoreboard.cs:132-133,141-142` | two `ToString()`, `StructureViews.ClockText` (interpolated string, `StructureViews.cs:148-152`), `ChogTheme.Hex("FF7A7F")` (concat and parse, `ChogTheme.cs:312-316`) | 4 to 5 strings and a parse |
| `TwinStickShooter/Scripts/GameView/CharacterView.cs:70-103` with `CharacterMaterialController.cs:36-45` | `renderer.materials = array` on every hidden sample mesh of every hero (6 controllers per class prefab) | about 36 native array assignments per frame, no visible effect (the sample meshes are hidden under the 3D hero) |
| `UI/AttackPreviewDynamicMesh.cs:60-119` | new `List`, `Vector3[]`, `int[]`, `RecalculateNormals` and up to 12 + 10 Quantum raycasts per edge | while the cone preview shows |
| `UI/AttackPreview.cs:130-133,185` | `mesh.material.color` on every preview renderer, `GetLayerMask("Static")` by name | while aiming; material copies made once |
| `Chog/Match/ChogHeroStage.cs:193-197` | `new Vector3[4]` and `GetComponentInParent<Canvas>` per `LateUpdate` | menu only |

TextMeshPro returns early when the text is equal, so the cost is the string, not a mesh rebuild
(inferred from the TMP setter; Unity's doc only says `SetText(string)` "is the same as using the
text property", [TMP_Text](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/TMPro.TMP_Text.html)).
Rough total: 15 to 25 short strings per frame, 0.5 to 2 KB, 15 to 120 KB per second at 30 to
60 fps (inferred from the paths above). What each collection costs on the phone is not measured.

Fine as they are (confirmed): `ChogEffectPool`, `ChogLight`, `ChogLightning` (mesh rebuilt 20
times a second per bolt into a reused `MarkDynamic` mesh), `ChogAimGuide` (reused lists),
`ChogTeamRing`, `ChogHitFeedback` (reused property block), `PerformanceHud` (one string per
second), `CFXR_Effect.Update` (lights and shake globally off).

### 4.7 Instantiation, pooling and material leaks

- **Quantum entity views are not pooled**: no `QuantumEntityViewPool` in any scene or prefab
  (script GUID searched). Every attack, skill and projectile entity instantiates its prefab and
  destroys it. Photon: "Creating and destroying views can lead to CPU performance spikes and
  increased garbage collection. For best performance QuantumEntityViews should be pooled"
  ([optimizations](https://doc.photonengine.com/quantum/v3/concepts-and-patterns/optimizations)).
- Per attack view, on top of the instantiation (confirmed):
  - `AttackColorSetter.cs:19,24` uses `renderer.materials[...]`, which copies every material of the
    renderer. Unity: "It is your responsibility to destroy the materials"
    ([Renderer.material](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Renderer-material.html)).
    Nothing destroys them.
  - `ChogAttackLook.cs:75-83,166-191`: `GetComponent`, `AddComponent`, a new object for the 3D
    object, two `GetComponentsInChildren` arrays, then the sample's renderers and particle systems
    that were just instantiated are disabled.
  - `AttackView.cs:77` instantiates the destroy effect, unpooled.
  - `GenesisAttackView.cs:44-47` still uses `GenesisEffects.Spawn`, which instantiates
    (`GenesisEffects.cs:166-168`) and is destroyed after its fade (`GenesisAttackView.cs:105-110`).
- Per hero view: `CharacterMaterialController.cs:13-34` copies all materials of each of 6 hidden
  meshes and makes as many "invisible" copies, never destroyed.
- **They accumulate**: the match scene is loaded additively
  (`Photon/QuantumMenu/Runtime/QuantumMenuConnectionBehaviourSDK.cs:252`) and unloaded with
  `UnloadSceneAsync` (`:491`); no `Resources.UnloadUnusedAssets` anywhere in the project. So the
  leaked materials stay for the whole browser session (inferred; to verify with "Total Used
  Memory" over three matches). On iOS, "only ~300 MB of RAM is reliably available to WebAssembly
  pages" ([WebKit 269777](https://bugs.webkit.org/show_bug.cgi?id=269777), 2024, partly
  addressed in [269937](https://bugs.webkit.org/show_bug.cgi?id=269937)), and heavy WebGL pages
  are killed and reloaded without warning ([WebKit 300782](https://bugs.webkit.org/show_bug.cgi?id=300782)).
- The fight effects themselves are pooled and pre-made one per frame while the match loads
  (`GenesisEffects.cs:122-126,535-541`). Recolouring with gradients allocates a `Gradient` and
  key arrays per play (`GenesisEffects.cs:504-514`): small.

### 4.8 UI canvases

`CharacterUIFollow.cs:68-87` moves every follower (names, bars, coin counters) in
`GameplayCanvas` (`:27`) every `LateUpdate`. Unity: "When a single element changes on the UI
Canvas, it dirties the whole Canvas" and "Keep static UI Elements on a separate Canvas"; also
"Animators will dirty their UI Elements on every frame" and "Remove Graphic Raycasters from
non-interactive UI Canvases" ([UI tips](https://unity.com/how-to/unity-ui-optimization-tips)).
On WebGL that batching work runs on the main thread (inferred: no worker threads). Cost unknown
until profiled.

### 4.9 Textures and memory

- Textures are few and small in the arena (0.2 Mpx in `TwinStickShooter/Art/Textures`); heroes
  use vertex colours. The effect materials use 36 textures (survey of the materials' GUIDs):
  30 have a WebGL override to R8 (single channel, small max size), 6 do not (`cfxr debris unlit
  3x3.png` 1024x1024, `cfxr leave a.png` and its normal map 512x1024, `cfxr smoke cloud x4.png`,
  `cfxr uv distortion fire smudge.png`) and use the default DXT.
- Unity: without native support "Unity uses a software fallback to decompress the textures at
  runtime ... it uses more memory and might run slower"; one build carries one format; the
  documented answer is two builds (DXT for desktop, ASTC for mobile) with the `.data` file
  chosen by the page ([texture compression](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-texture-compression.html)).
  [web3dsurvey](https://web3dsurvey.com/webgl2/extensions/) reports S3TC/DXT on about 30% of
  Android WebGL2 devices and ASTC on 99.8% (survey page, no date shown). For this game it is mostly memory and load time, a few MB,
  not frame time (inferred).
- Runtime portraits and eye pictures are RGBA32 (`GenesisSpecialMeshes.cs:372-384`, 128 px per
  look, cached): small.

### 4.10 Unity's own physics

`ProjectSettings/DynamicsManager.asset:22-23` auto simulation on with auto sync transforms;
`Physics2DSettings.asset:41` simulation in FixedUpdate; `TimeManager.asset:6` fixed step 0.02 s.
`Gameplay.unity` holds 258 BoxColliders (used to bake Quantum colliders) and no Rigidbody. No
runtime code queries Unity physics (only `TwinStickShooter/Scripts/Dev/` editor tools); no
physics raycaster in scenes. So PhysX steps 1 to 2 times per frame at 30 fps for nothing
(confirmed configuration; cost inferred small, under 0.5 ms on the phone).

### 4.11 Shader first use

No warm-up exists (`ShaderVariantCollection`, `GraphicsStateCollection` or a pre-draw):
`ChogFxRunner.Warm` instantiates one inactive copy per effect, which does not compile shaders
(`GenesisEffects.cs:535-557`). Unity: "there might be a visible stall when the graphics driver
creates a GPU-specific shader variant for the first time"
([shader loading](https://docs.unity3d.com/6000.6/Documentation/Manual/shader-loading.html)); on
WebGL the GraphicsStateCollection API "is the recommended method" and falls back to shader warmup
([prewarm](https://docs.unity3d.com/6000.6/Documentation/Manual/shader-prewarm-other.html)).
The phone's worst frames (158 to 1,552 ms) predate the 23 effect prefabs (36 materials, several
keyword variants each) and the 3D specials, which now add first uses in the middle of a fight
(inferred).

### 4.12 Quantum (simulation and view)

Confirmed configuration (`QuantumUser/Resources/SessionConfig.asset`): `UpdateFPS: 30` (`:20`),
`ChecksumInterval: 0` (`:21`), `RollbackWindow: 60` (`:22`), `MinTimeCorrectionFrames: 1`
(`:28`), `InputDelayMin: 0` (`:33`). `SimulationConfig.asset`: `UseAngularVelocity: 1` (`:37`),
`AllowSleeping: 0` (`:56`), `ThreadCount: 2` (`:143`, ignored without WebGL threads).
`TSS-SystemsConfig.asset:21` includes `CullingSystem2D`, but nothing outside `Assets/Photon`
calls `SetPredictionArea`, so prediction culling is inactive. AI runs `HFSMManager.Update` for
every bot every tick (`Simulation/System/AISystem.cs:76`). `BotSDKDebuggerSystem` is in the
systems list, but it only invokes a callback on verified frames and `AddToDebugger` runs once per
bot at setup (`AISetupHelper.cs:119`): negligible, not worth a stamp change.

What Photon says (all from the v3 manual, read as Markdown, see section 8):

- "Quantum always rolls-back and re-simulates frames" ([frames](https://doc.photonengine.com/quantum/v3/manual/frames)):
  per frame cost is (verified ticks + predicted ticks) x tick cost. The phone ran 2.5 to 4.3
  predicted ticks per update (measured).
- Input delay: "Add a little bit of Input Delay as it reduces the amount of predicted frames",
  with a warning to avoid changing it without understanding the latency trade
  ([mispredictions](https://doc.photonengine.com/quantum/v3/concepts-and-patterns/mispredictions-and-entity-views)).
- Prediction culling needs the culling system (present) and `Game.SetPredictionArea(position,
  radius)` "on every Unity update"; filters respect it, component iterators do not; it "does not
  guarantee a performance improvement for every project"
  ([prediction culling](https://doc.photonengine.com/quantum/v3/manual/prediction-culling)).
- `DeterministicSession.MaxVerifiedTicksPerUpdate` exists in 3.0.9 ("Limit the maximum number of
  verified ticks computed per update. Default is int.MaxValue", local
  `Assemblies/Quantum.Deterministic.xml:8471`): a client-side cap on catch-up bursts.
- View: the entity view updater walks every entity with a View in the verified and predicted
  frames each rendered frame (`Photon/Quantum/Runtime/QuantumEntityViewUpdater.cs:276`, iterator
  at `:411`); in 3.0.x the Prediction
  interpolation mode costs "two extra full frame copies each update", removed only in 3.1
  ([3.1 what's new](https://doc.photonengine.com/quantum/v3/getting-started/preview-3-1/whats-new)).
- Physics: disabling angular velocity is "faster and more stable", enable resting bodies; query
  options other than `HitAll` "save computation"
  ([physics performance](https://doc.photonengine.com/quantum/v3/manual/physics/physics-performance)).
- WebGL: Release DLLs and IL2CPP, stack traces None for Log and Warning; "TCP's reliable and
  sequenced transfer protocol can negatively impact gameplay for players with poor network
  connections" ([Quantum WebGL](https://doc.photonengine.com/quantum/current/manual/webgl)).

**What changes the simulation stamp** (`QualificationBuild.cs:164-189`): the `.cs` and `.qtn` files
under `Assets/QuantumUser/Simulation`, Quantum's assemblies, and the exported Quantum asset
database (`quantum-db.json`, every `AssetObject` in `QuantumUnityDB`: systems config, simulation
config, attack and skill data, prototypes). `SessionConfig.asset` is a session config asset, not
an `AssetObject`, and project settings, scenes, view prefabs and view scripts are not hashed, so
they are expected to keep the stamp (inferred from the code; verify with the `CHOG simulation
stamp` line of the build log before deploying). The judge replays with the session config stored
in each record (`tools/quantum/Verifier.cs:128,194,292`), so a client session change does not
break verification.

## 5. Research notes (beyond what section 4 cites)

- **WebGPU** is out of experimental in Unity 6.6 ("WebGPU is now out of experimental release",
  [What's new 6.6](https://docs.unity3d.com/6000.6/Documentation/Manual/WhatsNewUnity66.html);
  [Unity staff, Aug 2026](https://discussions.unity.com/t/webgpu-out-of-experimental-in-unity-6-6/1734694)),
  not enabled by default, with limited device support, and the same Unity engineer notes "One
  place where WebGL might still have some CPU advantage is with large numbers of draw calls".
  Not a lever for this deadline.
- **Quality on the Web**: Very Low or Low "is recommended for a Web application"
  ([web quality](https://docs.unity3d.com/6000.6/Documentation/Manual/web-optimization-quality.html));
  "Soft shadows have a greater rendering overhead than hard shadows"
  ([shadow performance](https://docs.unity3d.com/6000.6/Documentation/Manual/ShadowPerformance.html)).
  Graphics tiers put "WebGL: all devices" in Tier 2, so tiers cannot separate a phone from a
  desktop ([tiers](https://docs.unity3d.com/6000.6/Documentation/Manual/graphics-tiers.html)).
- **Draw calls in Built-in**: SRP Batcher, GPU Resident Drawer and BRG are "Not supported"; GPU
  instancing "performance benefits are better on mobile platforms than on desktop platforms"
  ([choose a method](https://docs.unity3d.com/6000.6/Documentation/Manual/optimizing-draw-calls-choose-method.html),
  [GPU instancing](https://docs.unity3d.com/6000.6/Documentation/Manual/GPUInstancing.html));
  "Don't put non-instanced properties in the MaterialPropertyBlock, because this disables
  instancing" ([MaterialPropertyBlock](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/MaterialPropertyBlock.html)).
- **Meshes**: call `MarkDynamic` before repeated uploads; `MeshUpdateFlags.DontRecalculateBounds`
  avoids "potentially costly" bounds work
  ([MarkDynamic](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mesh.MarkDynamic.html),
  [SetVertices](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mesh.SetVertices.html)).
- **Post on mobile** (for reference, desktop-only here): "Fullscreen Post-processing effects, like
  glows, can dramatically slow down performance"
  ([Unity mobile tips](https://unity.com/blog/games/optimize-your-mobile-game-performance-expert-tips-on-graphics-and-assets));
  FXAA "is the most efficient anti-aliasing technique"
  ([PPv2 anti-aliasing](https://docs.unity3d.com/Packages/com.unity.postprocessing@3.5/manual/Anti-aliasing.html)).
- **Thermal**: "Devices can only maintain a high level of performance for a limited amount of
  time before being thermally throttled" ([Android ADPF](https://developer.android.com/games/optimize/adpf/thermal));
  Unity: lowering the frame rate helps prevent the device "rising to a point where the CPU
  frequency may be throttled" ([graphics performance](https://docs.unity3d.com/6000.6/Documentation/Manual/OptimizingGraphicsPerformance.html)).
  No browser-specific source found. A web page cannot read thermal state (Compute Pressure only
  exposes "cpu"; earlier note `research/research_notes/Audit architecture Photon MOBA Genesis/mobile_webgl_profiling_protocol.md`).
- **Profiling a Web build** (earlier verified note, same file): the Profiler needs a Development
  build ("Enable Web Profiling", then the build's Profile button, present only in the Default and
  PWA templates, or `--player-connection-ip` in `config.arguments`); the Frame Debugger does not
  work on the Web; FrameTimingManager reports no GPU time on WebGL.
- **Release-build counters**: Draw Calls, SetPass Calls, Triangles and Vertices counts, and GC
  Used / GC Reserved / Total Used / System Used memory are "Available" in release players through
  `ProfilerRecorder`; "GC Allocated In Frame" is "Unavailable"
  ([profiler counters](https://docs.unity3d.com/6000.6/Documentation/Manual/profiler-counters-reference.html)).
  Not yet checked that they read non-zero on WebGL: the instrumentation must test `Valid`.

## 6. Prioritised plan

Effort: S under 2 hours, M half a day to a day, L more than a day. "Stamp" says whether the item
changes the simulation stamp, hence Photon's AppVersion (which has a hidden limit; see
`docs/DEPLOY.md`, "Photon's limit on application versions").

### 6.0 Step 0 (precondition): measure the phone

Instrument (section 7.2), build once, run the phone matrix (section 7.3). Every gain below is a
guess until the phone says where the 30 ms go. Effort M. Stamp: no.

### 6.1 Quick wins (view, page or settings only)

| ID | What | Evidence | Expected gain and basis | Risk | Effort | Stamp |
| --- | --- | --- | --- | --- | --- | --- |
| Q1 | Cap the pixel ratio: `config.devicePixelRatio = Math.min(window.devicePixelRatio, cap)` with a lower cap on phones (for example 1.5; desktop 2), and a page option to change it | `WebGLTemplates/Chog/index.html:195-235` sets none; Redmi canvas 1650x720 measured | pixels -44% on a DPR 2 phone at 1.5, -56% on a DPR 3 iPhone at 2 (pixel arithmetic). Turns into frame time only where the GPU is the limit; nothing on the Redmi of 10 October (CPU bound) | softer picture and UI text | S (page only, about 35 s build) | no |
| Q2 | Remove per-frame strings and swaps: cache the credential string in `PhotonIdentity` and compare its parts; update HUD texts only when the integer changes; cache the scoreboard's colour and child list; set the hidden sample meshes' materials only on a visibility change | section 4.6 table | 15 to 25 strings per frame fewer (0.5 to 2 KB per frame) and about 36 native calls; fewer full GC passes, whose cost on the phone is not measured | low | S to M | no |
| Q3 | Warm the shaders at match load: draw each effect prefab, the special pieces and lights once off-screen during loading (or record a `GraphicsStateCollection` / `ShaderVariantCollection` and warm it) | 4.11 | removes first-use stalls; magnitude unknown (each first use of a variant compiles a GL program); the phone's worst frames were 158 to 1,552 ms | longer loading; the menu-to-match transition | M | no |
| Q4 | Cheaper effect shader: skip the eye-picture math unless the vertex wears a picture (branch on `core.w`, or a keyword and a separate material for eyeballs) | `ChogEffect.shader:131-137` | removes `normalize`, `acos`, a division and a texture fetch per pixel of every flare, pool, ring, column, bolt and glow; GPU only, fight moments only | low (visual check of eyeballs) | S | no |
| Q5 | Stop Unity physics: `Physics.simulationMode = Script`, `Physics2D.simulationMode = Script`, auto sync transforms off (settings or a start-up line) | 4.10 | 1 to 2 PhysX steps per frame removed; small (inferred under 0.5 ms on the phone) | none found (no runtime Unity physics use) | S | no |
| Q6 | Logging: put `CombatLog` behind its own page option; set Log and Warning stack traces to None (keep Error and Exception) | 2.3; `ProjectSettings.asset:57`; Photon WebGL advice | honest perf runs (about 10 stack-traced lines per second removed from every perf run so far); cheaper logs for players | fewer stack traces in consoles for logs and warnings | S | no |
| Q7 | Effect particles and hero shells stop casting shadows (`shadowCastingMode = Off` in `ChogFxPlayback.Prepare`; shells excluded from the hero shadow pass) | 4.1 | desktop shadow pass only | none | S | no |
| Q8 | Initial memory size set to the measured steady state (from the diagnostics overlay in step 0) | `ProjectSettings.asset:727-729` | removes heap growth hitches in the first match (inferred) | a too-large initial size can fail on iOS | S | no |
| Q9 | Test a 60 fps cap on high-refresh phones (`fps=60` already exists with `perf=1`) | 4.2 | thermal headroom over long sessions (inferred); Unity recommends -1 unless throttling | pacing jitter | S (test only) | no |

### 6.2 Deeper work (view only)

| ID | What | Evidence | Expected gain and basis | Risk | Effort | Stamp |
| --- | --- | --- | --- | --- | --- | --- |
| D1 | Pool Quantum entity views (`QuantumEntityViewPool` on the updater, precache the attack prefabs); remove the sample's hidden renderers and particle systems from the attack prefabs instead of instantiating then disabling them; colour through `MaterialPropertyBlock` instead of `renderer.materials`; pool the destroy effect and the `GenesisEffects.Spawn` path | 4.7 | removes several Instantiate/Destroy per second in a fight (195 hero hits and 42 structure hits in about 150 s on 17 Oct, more attacks than hits) and their garbage; stops the material leak across matches | pooled views must reset state and subscribe with `onlyIfActiveAndEnabled` (Photon); `ChogAttackLook` already expects reuse (`:168`) | M | no (view prefabs and scene are not hashed; verify the stamp line) |
| D2 | UI: move the followers (names, bars, coins) to their own canvas; static HUD apart; no raycaster on display-only canvases; damage numbers without Animator | 4.8 | stops re-batching the whole gameplay canvas every frame; size unknown until profiled (`Canvas.BuildBatch`, `SendWillRenderCanvases`) | layout regressions on small screens | M | no |
| D3 | Hero shader without `Cull Off` plus `discard`: split each part into an opaque body (Cull Back), the outline shells (Cull Front, no discard, inverted hull) and a small double-sided set (brims, lenses); shells out of the shadow pass | 4.4 | GPU cost of 6 heroes of 6,200 to 8,000 triangles; share of the frame unknown | look of the heroes (owner judges); +1 to 2 draw calls per part unless merged | M to L (runtime split in `ChogHeroModel.MeshOf`, or in `tools/blender/export_chog_parts.py`) | no |
| D4 | After counting draw calls: instancing on `Chog/Ground` and `Chog/Effect` (with `_Fade` as an instanced property), fewer pieces per special on phones, lower live caps on phones if the GPU is the limit | 4.5 | depends on the count; the goal is under about 150 in a fight on a phone (PlayCanvas guideline, not a Unity rule) | visuals | M | no |
| D5 | Prediction culling: call `SetPredictionArea(local hero, radius beyond the viewport)` every frame; check the view code for culled entities | 4.12 | only entities outside the area leave the predicted ticks; small arena, so 0 to 30% of simulation time (inferred) | offscreen entities update late; mispredictions of the culled ones | S to M | no (the culling system is already in the systems config) |
| D6 | Client session knobs: `InputDelayMin` 1 to 2 ticks; `MaxVerifiedTicksPerUpdate` cap after a stall | 4.12; measured 2.5 to 4.3 predicted ticks | each tick of delay removes about one re-simulated tick per update, so roughly a quarter to a third of predicted work per tick of delay; +33 ms of local input latency per tick at 30 Hz | game feel; Photon advises care; only the room creator's config applies | S | no (`SessionConfig.asset` is not an `AssetObject`; verify) |
| D7 | Texture formats: WebGL overrides for the 6 CFXR textures without one, or the documented dual build (DXT and ASTC) with a `.data` choice in the page | 4.9 | a few MB of memory and faster loading on phones; little frame time | build pipeline change for the dual build | S (overrides) / M (dual build) | no |
| D8 | Device class beyond `isMobilePlatform` (touch plus screen size, or the WebGL renderer string) for quality and effects | 4.1 | avoids an iPad or a large phone taking the desktop path | misclassifying a touch laptop | S | no |

### 6.3 Simulation changes (change the stamp, hence Photon's AppVersion)

Only if step 0 shows the simulation is the limit on the phone (it was 3 to 9 ms of 19 to 28 ms
busy on 10 October). Batch them into one stamp change, re-record the judge fixtures
(`tests/fixtures/replays/`), and deploy once.

| ID | What | Evidence | Expected gain | Risk | Effort |
| --- | --- | --- | --- | --- | --- |
| S1 | Spread AI updates over ticks (`HFSMManager.Update` every N ticks per bot, by entity index) | `AISystem.cs:76` | AI share of each tick (not measured) | bot behaviour changes; fixtures | M |
| S2 | Physics settings: sleeping on, angular velocity off | `SimulationConfig.asset:37,56`; Photon physics performance page | physics share of each tick (not measured) | gameplay feel; fixtures | S plus testing |
| S3 | Quantum 3.0.9 to 3.0.14 (GC reductions, broad-phase fixes) | `build_info.txt:1`; release notes | small, mostly correctness | SDK upgrade before a deadline | M |
| (not recommended) | Lower the simulation rate below 30 Hz; remove `BotSDKDebuggerSystem`; Quantum 3.1 | 4.12 | none or not allowed for production | | |

### 6.4 Not recommended before 23 October

- **WebGPU**: supported in 6.6 but not default, limited devices, and possibly worse with many draw
  calls (section 5).
- **Native C/C++ multithreading**: needs `Cross-Origin-Opener-Policy` and
  `Cross-Origin-Embedder-Policy` headers (earlier verified note); cross-origin isolation can block
  third-party iframes and popups that the wallet connection relies on (inferred, untested), and
  Photon calls Unity 6 WebGL threading experimental.
- **URP migration, LTO builds, higher stripping**: large, slow to build on this Mac, uncertain
  gains for frame time.

## 7. Measurement protocol (phase 2, under the build lock)

### 7.1 Principles

- One release build, the same settings as the shipped one, plus the view-only instrumentation of
  7.2; it keeps the simulation stamp, so it joins Photon rooms like production (check the stamp
  line). Pin it on port 8083 with `tools/unity/serve_webgl.py --directory <build>/webgl`, never
  `--latest` during a series.
- Before any run on the Mac: no other build, Blender or heavy job; check swap (`vm_stat`,
  `sysctl vm.swapusage`); the 17 October afternoon runs were ruined by swapping. All launches muted.
- Report percentiles (p50, p95, p99, worst, frames over 40 and 100 ms), never average fps.
  Discard the first minute report if loading or warm-up shows in it.
- Separate frame-time lag from network lag: read ping, predicted ticks, `STALLING`, and the
  "own attacks confirmed / undone" line together with frame times.

### 7.2 Instrumentation to add (small, view-only; not implemented here)

1. `PerformanceHud`: `ProfilerRecorder` for "Draw Calls Count", "SetPass Calls Count",
   "Triangles Count", "Vertices Count", "GC Used Memory", "GC Reserved Memory", "Total Used
   Memory" (p50 and max per minute for the render counters; check `Valid` on WebGL and print
   "unavailable" otherwise); `System.GC.CollectionCount(0)` delta per minute and the number of
   frames over 50 ms that start right after a collection; the count of live particles over the
   playing effects (once a second). Add all to the JSON report.
2. Put `CombatLog` behind its own option (for example `combatlog=1`) so `perf=1` measures what
   players run.
3. Page options for A/B on one build (private pages only, as the other test options):
   `dpr=<number>` read by the template; `physics=0`; `shadows=0`; `pool=0/1` once D1 exists;
   `inputdelay=<ticks>` next to the existing `hz`, `rollback`, `checksum` (room creator only).
   Existing ones to reuse: `fx=0`, `fxlevel=0/1/2`, `hero3d=0`, `special3d=0`, `matchlook=0` or
   `-post`, `quality=<index>`, `fps=<n>`.
4. A measurement-only build variant with the Unity diagnostics overlay on (`webGLShowDiagnostics`)
   to read the WASM heap on the phone; not for players.
5. Optional `perf=2`: a ring buffer of per-frame rows (frame, busy, sim, predicted, draw calls,
   GC count) posted once at the end of a match, to correlate spikes with collections, catch-up
   bursts and first-use stalls.

### 7.3 Runs

Phone (the Redmi on the LAN, Chrome; an iPhone in Safari if one is available), each run a
three-minute siege against a fixed bot list (as on 17 October:
`?noguide=1&charged=1&perf=1&bots=29,57,21`, recruit or a test token on the private address),
three repetitions, phone at rest 10 minutes between series, charger state and brightness noted,
reports read with `python3 tools/unity/read_perf_reports.py --client <phone>`:

| Run | Options | Isolates |
| --- | --- | --- |
| A | baseline | today's cost |
| B | `fx=0` | fight effects |
| C | `hero3d=0` | 3D heroes (vertex and fragment) |
| D | `special3d=0` | 3D specials |
| E | `dpr=1`, then `dpr=1.5` | GPU fill rate (if frame time drops, the GPU is the limit) |
| F | `quality=0` vs default "Fast"; `quality=3` once | quality level cost; desktop path on a phone |
| G | `matchlook=0` | matcap, rings, light rig |
| H | `physics=0` | Unity physics stepping |
| I | `inputdelay=1` and `2` (room creator) | predicted ticks vs simulation time |
| J | baseline after 10 minutes of continuous play | thermal throttling |
| K | three matches in a row without reload, memory counters after each | material leak and heap growth |

Desktop: `CHOG_BROWSER_GPU=1 node tools/unity/qualify_webgl.mjs` (Metal, muted) or
`tools/unity/checks/browser_check.sh match`, at 1280x720 and 2560x1440, quality "Good", with and
without `matchlook=-post` and `shadows=0`. A phone-sized viewport on the Mac
(`CHOG_BROWSER_WIDTH=844 CHOG_BROWSER_HEIGHT=390 CHOG_BROWSER_TOUCH=1`) checks the phone code path
(lighter effects, layout) and CPU trends only; it says nothing about a phone GPU.

Attribution, once: a Development build connected to the Editor's Profiler from the phone
("Enable Web Profiling"; the Chog template has no Profile button, so pass
`--player-connection-ip=<editor address>` in `config.arguments`), reading `Camera.Render`,
`Canvas.BuildBatch`, `ParticleSystem`, the Quantum update, `GC.Collect` and script updates. Its
absolute numbers do not count (development overhead). On Android, Chrome remote debugging
(`chrome://inspect`) gives the Performance panel with the GPU track.

### 7.4 Decision rules

- E cuts the frame time: GPU-bound, apply Q1, Q4, D3, D4 first.
- B, C or D cut it: content cost, tune caps and pieces on the phone path.
- Draw calls above about 150 with "other" above 12 ms: D4 and D2 first.
- Spikes line up with GC count changes: Q2 and D1 first.
- Spikes line up with first uses (first special of a kind, first effect): Q3.
- Simulation p50 above a third of busy, or catch-up bursts: D6, D5, then section 6.3.
- J worse than A at the same load: thermal; consider Q9 and lower caps.
- Acceptance: phone p95 at or under 33.3 ms over a match (the bar of the earlier audit, STATUS
  9 October); proposed here, to be agreed: at most one frame over 100 ms per minute on the phone,
  desktop p95 at or under 16.7 ms.

## 8. Sources and lookup routes

Routes that worked (recorded so the next search is not repeated):

- Photon docs block plain fetchers; append `.md` to a page URL (index at
  `https://doc.photonengine.com/llms-quantum.md`), or curl with a browser User-Agent and Referer.
  Real Quantum 3 paths are under `/quantum/v3/manual/`, `/quantum/v3/concepts-and-patterns/`,
  `/quantum/current/manual/`.
- Unity 6000.6 manual table of contents: `https://docs.unity3d.com/6000.6/Documentation/Manual/docdata/toc.json`
  (to find live slugs); `https://docs.unity.com/en-us/engine/6000.6/manual/...md` returns
  Markdown. `dynamic-batching.html` now redirects to `DrawCallBatching.html`.
- Copies of the Photon pages read on 17 October are in this session's scratchpad only; the
  earlier verified notes are in `research/research_notes/Audit architecture Photon MOBA Genesis/`.

Unity: [webgl-performance](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-performance.html),
[webgl-memory](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-memory.html),
[incremental GC](https://docs.unity3d.com/6000.6/Documentation/Manual/performance-incremental-garbage-collection.html),
[web-optimization-mobile](https://docs.unity3d.com/6000.6/Documentation/Manual/web-optimization-mobile.html),
[web-optimization-quality](https://docs.unity3d.com/6000.6/Documentation/Manual/web-optimization-quality.html),
[Web Player settings](https://docs.unity3d.com/6000.6/Documentation/Manual/class-PlayerSettingsWebGL.html),
[web-build-settings](https://docs.unity3d.com/6000.6/Documentation/Manual/web-build-settings.html),
[webgl-texture-compression](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-texture-compression.html),
[web-templates-build-configuration](https://docs.unity3d.com/6000.6/Documentation/Manual/web-templates-build-configuration.html),
[webgl-canvas-size](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-canvas-size.html),
[UpgradeGuideUnity65](https://docs.unity3d.com/6000.6/Documentation/Manual/UpgradeGuideUnity65.html),
[6.6 upgrade guide](https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity66.md),
[WhatsNewUnity66](https://docs.unity3d.com/6000.6/Documentation/Manual/WhatsNewUnity66.html),
[optimizing-draw-calls-choose-method](https://docs.unity3d.com/6000.6/Documentation/Manual/optimizing-draw-calls-choose-method.html),
[GPUInstancing](https://docs.unity3d.com/6000.6/Documentation/Manual/GPUInstancing.html),
[DrawCallBatching-Properties](https://docs.unity3d.com/6000.6/Documentation/Manual/DrawCallBatching-Properties.html),
[class-QualitySettings](https://docs.unity3d.com/6000.6/Documentation/Manual/class-QualitySettings.html),
[ShadowPerformance](https://docs.unity3d.com/6000.6/Documentation/Manual/ShadowPerformance.html),
[shadow-cascades-performance](https://docs.unity3d.com/6000.6/Documentation/Manual/shadow-cascades-performance.html),
[hdr-birp](https://docs.unity3d.com/6000.6/Documentation/Manual/hdr-birp.html),
[graphics-tiers](https://docs.unity3d.com/6000.6/Documentation/Manual/graphics-tiers.html),
[OptimizingGraphicsPerformance](https://docs.unity3d.com/6000.6/Documentation/Manual/OptimizingGraphicsPerformance.html),
[DynamicResolution-introduction](https://docs.unity3d.com/6000.6/Documentation/Manual/DynamicResolution-introduction.html),
[shader-loading](https://docs.unity3d.com/6000.6/Documentation/Manual/shader-loading.html),
[shader-prewarm-other](https://docs.unity3d.com/6000.6/Documentation/Manual/shader-prewarm-other.html),
[profiler-counters-reference](https://docs.unity3d.com/6000.6/Documentation/Manual/profiler-counters-reference.html),
[Application.isMobilePlatform](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Application-isMobilePlatform.html),
[Renderer.material](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Renderer-material.html),
[MaterialPropertyBlock](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/MaterialPropertyBlock.html),
[Mesh.MarkDynamic](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mesh.MarkDynamic.html),
[Mesh.SetVertices](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mesh.SetVertices.html),
[TMP_Text](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/TMPro.TMP_Text.html),
[PPv2 anti-aliasing](https://docs.unity3d.com/Packages/com.unity.postprocessing@3.5/manual/Anti-aliasing.html),
[UI optimization tips](https://unity.com/how-to/unity-ui-optimization-tips),
[mobile graphics tips](https://unity.com/blog/games/optimize-your-mobile-game-performance-expert-tips-on-graphics-and-assets),
[render pipelines strategy](https://unity.com/topics/render-pipelines-strategy-for-2026),
[WebGPU out of experimental](https://discussions.unity.com/t/webgpu-out-of-experimental-in-unity-6-6/1734694).

Photon Quantum 3: [optimizations](https://doc.photonengine.com/quantum/v3/concepts-and-patterns/optimizations),
[prediction culling](https://doc.photonengine.com/quantum/v3/manual/prediction-culling),
[mispredictions and entity views](https://doc.photonengine.com/quantum/v3/concepts-and-patterns/mispredictions-and-entity-views),
[entity view](https://doc.photonengine.com/quantum/v3/manual/entityview),
[frames](https://doc.photonengine.com/quantum/v3/manual/frames),
[config files](https://doc.photonengine.com/quantum/v3/manual/config-files),
[profiling](https://doc.photonengine.com/quantum/v3/manual/profiling),
[physics performance](https://doc.photonengine.com/quantum/v3/manual/physics/physics-performance),
[WebGL](https://doc.photonengine.com/quantum/current/manual/webgl),
[release notes](https://doc.photonengine.com/quantum/v3/getting-started/release-notes),
[initial setup](https://doc.photonengine.com/quantum/v3/getting-started/initial-setup),
[3.1 what's new](https://doc.photonengine.com/quantum/v3/getting-started/preview-3-1/whats-new),
[Bot SDK HFSM](https://doc.photonengine.com/quantum/v3/addons/bot-sdk/hfsm).

Browsers and devices: [WebKit 269777](https://bugs.webkit.org/show_bug.cgi?id=269777),
[WebKit 269937](https://bugs.webkit.org/show_bug.cgi?id=269937),
[WebKit 300782](https://bugs.webkit.org/show_bug.cgi?id=300782),
[WebKit 215745](https://bugs.webkit.org/show_bug.cgi?id=215745),
[UIScreen.scale](https://developer.apple.com/documentation/uikit/uiscreen/scale),
[Android thermal](https://developer.android.com/games/optimize/adpf/thermal),
[PlayCanvas guidelines](https://developer.playcanvas.com/user-manual/optimization/guidelines/),
[WebGL 2 specification](https://registry.khronos.org/webgl/specs/latest/2.0/).

## 9. Open questions

- Is the phone now GPU-bound (run E) or still main-thread bound? Everything in 6.1 and 6.2 is
  ordered on the 10 October answer (main thread) plus the new GPU load; step 0 may reorder it.
- How many draw calls a fight really issues, and whether the release-build counters read on
  WebGL.
- How long one garbage collection takes on the phone with a 30 to 50 MB heap.
- Whether the leaked materials (4.7) are a few MB per match or less.
- Whether `SessionConfig.asset` and view prefab edits really keep the stamp (check the build log).
- No iPhone has ever been measured; Low Power Mode caps it at 30 fps whatever we do.
