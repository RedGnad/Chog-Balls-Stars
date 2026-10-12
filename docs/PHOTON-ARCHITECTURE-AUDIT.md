# Photon architecture audit for the Genesis MOBA (Quantum 3 vs Fusion 2)

Date: 2026-10-09. Independent technical audit requested by the project owner.

Scope: choose the Photon architecture for the 3v3 single-lane MOBA (Unity, WebGL and
mobile first, 1,969 Chog Genesis heroes, $CHOG specialisations, persistent career per
canonical tokenId, no wagering and no token rewards per match, 100 CCU for the MVP, the
PUN2 app untouched). The two earlier decisions, [Fusion Server, superseded](DECISION-RESEAU.md)
and [Quantum 3, active](DECISION-RESEAU-REVUE.md), were inputs to audit, not authorities.

Method: six research notes were produced on 2026-10-08 from primary sources (Photon manuals,
API reference, terms and pricing pages, Unity 6 manual, Chromium GPU lists, vendor spec
pages), with every claim tagged verified, inferred or unknown. The local SDKs, the sample
source, the session config and the WebGL player settings were inspected on disk. The notes
are in `research/research_notes/Audit architecture Photon MOBA Genesis/` (see appendix).

Status words used below: **Verified** means read in a primary source or on disk.
**Inferred** means deduced from verified facts. **Unknown** means not found and to be tested.

Time box: the hackathon end is derived from the site's client code as 2026-10-22 17:00
Paris, 13 days from today, and is not confirmed by an organiser announcement
([site facts](../research/evidence/chogathon-site-facts.json)).

**Addendum, 2026-10-09 02:45 Paris.** Two facts arrived after this audit.
(1) The site now sets sign-up to 72 h, so the derived end is **2026-10-23 17:00 Paris**; shift
every block date below by one day ([site re-read](../research/evidence/chogathon-site-facts-2026-10-09.json)).
The submission asks for a shared repository and the pre-existing work used, which answers
part of row 38 and makes row 34 (sample source redistribution) a real question.
(2) The user played an online party match between the Mac Editor and the phone, two humans
and four bots, and reports it fluid and playable
([evidence](../research/evidence/quantum-online-two-devices-2026-10-09.json)). This covers the
six-hero part of the gate and the two-client part of B1, qualitatively. It does not cover
minions and towers, reconnection, or a measured p95. Gate B0 is therefore relaxed: content work
starts, and an on-device performance overlay (`?perf=1`) measures each new system on the phone as
it lands. The flip conditions of section 9 are unchanged.

## 1. Recommendation

**Keep Photon Quantum 3 on the Photon Public Cloud with a webhook backend, and spend the
next two days on a measured gate (block B0 in section 8) before any content work.**

Confidence: moderate, about 70 percent that Quantum is the right engine for the product as
specified, conditional on B0 passing. Almost all of the remaining 30 percent is one unknown:
the per-client simulation cost of an online 3v3 with bots and minions on the real phone.
Nothing measured so far covers it, because the phone test ran in local mode, which performs
no rollback at all (section 7).

Why Quantum rather than the alternatives, for this product:

- The product needs trusted admission (does this wallet own this Genesis, did it buy this
  specialisation), a loadout the client cannot forge, and a result the career can trust,
  with no money at stake per match. On the Public Cloud, Quantum gives the first two through
  the AddPlayer, GameConfigs and CreateGame webhooks (reject with HTTP 400, or replace the
  client's RuntimePlayer and configs entirely). It gives the raw material for the third: a
  server-streamed input history that Photon calls "a trusted source", plus a documented
  headless re-simulation path. Fusion Shared gives none of this on the Public Cloud. Fusion
  Server gives it only after we host, orchestrate and keep alive one headless Unity process
  per match.
- Photon's own guidance for WebGL puts Fusion Client/Server modes behind a toggle that is
  "not recommended and by default disabled", warns of "degraded performance" and disconnects,
  and ships no sample of a browser client on a dedicated server. The mobile offload argument
  for Fusion Server is real in principle but unmeasured, and every browser client would reach
  our server through the Photon relay over TCP WebSockets.
- Quantum is officially listed for WebGL (with caveats), the sample already runs in the
  browser and on the phone, and its combat, team, bot-fill, skill and KCC code is reusable.
  The MOBA layer (minions, tower, core, lane AI) has to be written in either engine.
- Fusion Shared is Photon's recommendation for WebGL and mobile on latency and CPU, and it is
  the right answer for a game without a persistent career. Here it would make the career
  trivially forgeable (any client edits its own HP, cooldowns and results), would run all
  bots and minions on one player's phone with authority migration on drop, and has no
  deterministic replay for disputes. It is the "fun demo only" option, not the product.
- Fusion Host runs the match on a player's phone whose browser tab is suspended in the
  background; Photon says this "will affect all players". Discarded.

What the recommendation is not: it is not "Quantum is faster on the phone" (unmeasured), and
it is not "the GameResult webhook is trustworthy" (Photon says the opposite). The result path
is: client results compared by the backend, replay stream archived from day one, headless
re-simulation as the dispute path, career credited only after that logic says yes.

## 2. Evidence matrix

| # | Claim | Status | Source |
| --- | --- | --- | --- |
| 1 | Quantum 3 lists WebGL as a supported platform with "special considerations": lower performance, Release Quantum DLLs plus IL2CPP required, simulation confined to the main thread (ThreadCount ignored) unless Unity 6 experimental multithreading is enabled, WebSockets over TCP, and "it is recommended to also offer the game as a download". | Verified | [WebGL manual](https://doc.photonengine.com/quantum/current/manual/webgl), [platforms](https://doc.photonengine.com/quantum/current/getting-started/initial-setup) |
| 2 | Quantum rolls back to the last verified frame and re-simulates to the predicted head on every update, not only on mispredictions; cost scales with verified ticks plus predicted depth times per-tick cost; levers are a lower simulation rate, prediction culling, physics tuning and ChecksumInterval 0. | Verified | [frames](https://doc.photonengine.com/quantum/current/manual/frames), [prediction culling](https://doc.photonengine.com/quantum/v3/video-tutorials/prediction-culling), [optimizations](https://doc.photonengine.com/quantum/v3/concepts-and-patterns/optimizations) |
| 3 | In offline (local) mode there are no duplicate system updates, so a local bot match exercises zero rollback cost. | Verified | [FAQ](https://doc.photonengine.com/quantum/v3/getting-started/faq) |
| 4 | The sample's session config is untuned for mobile: UpdateFPS 60, RollbackWindow 60, ChecksumInterval 60 (Photon: "set to zero for release"), InputDelayMin 0. | Verified (disk) | `game/Assets/QuantumUser/Resources/SessionConfig.asset`, [config files](https://doc.photonengine.com/quantum/current/manual/config-files) |
| 5 | The WebGL build uses the Release Quantum DLLs (installed `Quantum.Engine.dll` hash equals the one in `Quantum.Release.zip`) but compiles `QUANTUM_LOGLEVEL_DEBUG`, keeps explicitly thrown exceptions, starts the heap at 32 MB with geometric growth, and does not force `devicePixelRatio`. | Verified (disk) | `game/ProjectSettings/ProjectSettings.asset`, build `index.html` |
| 6 | Eleven Quantum webhooks exist on the Public Cloud; CreateGame, JoinGame, GameConfigs and AddPlayer can reject (400); AddPlayer's response "overwrite[s] the RuntimePlayer that the client sent"; responses "need to be complete because the configs send by clients are completely replaced"; without webhooks "RuntimePlayer is completely unprotected". | Verified | [webhooks](https://doc.photonengine.com/quantum/current/manual/webhooks), [cheat protection](https://doc.photonengine.com/quantum/current/manual/cheat-protection) |
| 7 | GameResult is produced by clients ("once per game by each client"), aggregated by identical payload and sorted by vote count; Photon: "should be handled as untrusted client input"; `IsServerResult` is true only with Enterprise server simulation. The SDK adds: "one game result during one session per client", to be sent "based on timing and simulation data of a verified frame". | Verified | [webhooks](https://doc.photonengine.com/quantum/current/manual/webhooks), `Quantum.Deterministic.xml` on disk |
| 8 | ReplayStart and ReplayChunk webhooks stream the server-side input history ("a trusted source for capturing the game replay directly from the server"); re-simulation "using the captured input stream and a non-Unity session runner application" is the documented verification path; the SDK generates `Quantum.Runner.Dotnet` with `--replay-path --lut-path [--db-path] [--checksum-path]`. Assembling the replay, extracting the result and adjudicating is developer code. | Verified | [webhooks](https://doc.photonengine.com/quantum/current/manual/webhooks), [replay](https://doc.photonengine.com/quantum/current/manual/replay), [cheat protection](https://doc.photonengine.com/quantum/current/manual/cheat-protection) |
| 9 | Custom authentication: Photon calls our HTTPS service at connect time; the backend can set an immutable `UserId` and a secret `AuthCookie` that the client never sees and that every player webhook carries; anonymous clients can be rejected app-wide. | Verified | [custom authentication](https://doc.photonengine.com/realtime/current/reference/custom-authentication) |
| 10 | Server-side simulation, server snapshots and per-tick input or command validation require the Plugin SDK (Gaming Circle, from $125 per month) deployed on a Photon Enterprise Cloud ("involves additional costs", minimum 2,000 CCU, price on request). | Verified | [plugin SDK](https://doc.photonengine.com/quantum/current/addons/plugin-sdk/overview), [pricing doc](https://doc.photonengine.com/photon/current/pricing), [Gaming Circle](https://www.photonengine.com/gaming/pricing) |
| 11 | "Perfect Information Problem": every Quantum client holds the full state, so hidden information and fog of war secrecy are "easily hackable"; a modified client cannot affect other players' state. | Verified | [cheat protection](https://doc.photonengine.com/quantum/current/manual/cheat-protection) |
| 12 | Quantum reconnection on the Public Cloud uses buddy snapshots uploaded by another client (15 s wait, 10 s upload, 20 s download timeouts), needs PlayerTTL above 0 (20 s suggested) and a recreated runner; a WebGL tab in the background disconnects after about 10 s. | Verified | [reconnecting](https://doc.photonengine.com/quantum/v3/manual/game-session/reconnecting), [Realtime known issues](https://doc.photonengine.com/realtime/current/troubleshooting/known-issues) |
| 13 | Fusion: "We strongly recommend Shared Mode on WebGL ... Server and Host mode are not recommended and by default disabled"; the toggle "Allow Client Server Modes in WebGL" warns of "degraded performance" and that Client Host "often lead[s] to disconnects"; "direct connections are not supported for WebGL builds". Shared is "the recommended mode for mobile"; Dedicated Server is "only recommended for high ARPU mobile titles". | Verified | [fusion-choose](https://doc.photonengine.com/fusion/v2/fusion-choose), [network project config](https://doc.photonengine.com/fusion/v2/manual/network-project-config) |
| 14 | Fusion dedicated server: "Photon does not provide servers for hosting a dedicated Fusion server application" and "Photon does NOT offer dedicated game session server orchestration"; a headless 64-bit Unity build runs on a provider machine (Edgegap, Gameye, PlayFab, GameLift, Multiplay named); the Public Cloud subscription suffices. | Verified | [dedicated server overview](https://doc.photonengine.com/fusion/v2/concepts-and-patterns/dedicated-server-overview), [topologies](https://doc.photonengine.com/fusion/v2/manual/network-topologies) |
| 15 | Fusion Shared: "cheating can occur without extra precautions being taken"; "each client has authority and decides what they hit. This is precise, but vulnerable to cheating"; server-side logic in Shared Mode is "available for Enterprise Cloud Only". | Verified | [fusion-choose](https://doc.photonengine.com/fusion/v2/fusion-choose), [lag compensation](https://doc.photonengine.com/fusion/v2/manual/advanced/lag-compensation), [what's new 2.1](https://doc.photonengine.com/fusion/v2/getting-started/release-notes/whats-new-2-1) |
| 16 | Fusion Server mode: lag compensation and input delay exist only in Server and Host modes; "NetworkObject spawn is not predicted in Fusion 2", so projectiles need the data-buffer pattern; proxies are rendered from an adaptive, jitter-derived interpolation delay. | Verified | [lag compensation](https://doc.photonengine.com/fusion/v2/manual/advanced/lag-compensation), [projectiles essentials](https://doc.photonengine.com/fusion/v2/technical-samples/projectiles-essentials), [time synchronization](https://doc.photonengine.com/fusion/v2/manual/advanced/time-synchronization) |
| 17 | Pricing is identical for Quantum and Fusion: free 20 CCU (non-commercial), free 100 CCU for launch ("One application per studio/account (a Fusion or a Quantum app, not both)", hard cap), $95 one-time 100 CCU for 12 months (stackable to 200), $125, $250, $500 per month for 500, 1,000, 2,000 CCU; 3 GB traffic per peak CCU; webhooks carry no fee; PUN2 has its own price list and no free 100 tier. | Verified | [Quantum pricing](https://www.photonengine.com/quantum/pricing), [Fusion pricing](https://www.photonengine.com/fusion/pricing), [pricing doc](https://doc.photonengine.com/photon/current/pricing), [PUN pricing](https://www.photonengine.com/pun/pricing) |
| 18 | Versions on 2026-10-08: Quantum 3.0.14 stable (Build 2241, Oct 05, 2026), 3.1.0 preview "not intended to be used for live or released games"; Fusion 2.1.3 Build 2390 is the latest stable and the project's version. Neither SDK lists Unity 6000.6 (Quantum's table stops at 6000.5; Fusion lists 6.0.x and 6.3.x). | Verified | [Quantum setup](https://doc.photonengine.com/quantum/current/getting-started/initial-setup), [3.1 preview](https://doc.photonengine.com/quantum/current/getting-started/preview-3-1/sdk-download), [Fusion SDK](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download) |
| 19 | Photon License Terms 7.3: the sample code may be modified and implemented "in the Application but in any case always without any Assets"; "Asset" covers sounds, animations, graphics, textures, 3D models and fonts not in the source code. The eleven `.anim` clips, Animator controllers, UI PNGs and VFX prefabs authored by Photon fall under that definition. | Verified text, inferred application | [license terms](https://www.photonengine.com/terms/licenseterms), `game/License.txt` |
| 20 | KayKit Dungeon Pack 1.0 and Character Animations are CC0; the 100 CC0 SFX pack is CC0; the sample tree also ships full JMO Cartoon FX Remaster 2 and 3 folders (1,026 prefabs, one used) that are per-licensee Asset Store content. | Verified | [KayKit dungeon](https://kaylousberg.itch.io/kaykit-dungeon), [KayKit animations](https://kaylousberg.itch.io/kaykit-animations), [100 CC0 SFX](https://opengameart.org/content/100-cc0-sfx), [Asset Store EULA](https://unity.com/legal/as-terms) |
| 21 | Bot SDK is a free download (3.10.2 for Quantum 3.0, Jun 02, 2026; the sample ships 3.5.0 A1); HFSM is the cheapest model; bot AI runs on every client, "there is no concept of a master client who simulates Bots input". | Verified | [Bot SDK](https://doc.photonengine.com/quantum/v3/addons/bot-sdk/overview), [player replacement](https://doc.photonengine.com/quantum/v3/manual/player/player-replacement) |
| 22 | No Quantum 3 sample has lanes, minions or towers; the only lane MOBA sample (Rogue MOBA, 3v3 single lane, minions, tower, base) is Quantum 2.1.8, Gaming Circle only, paid art, and its usage rights end with the membership. | Verified | [Rogue MOBA](https://doc.photonengine.com/quantum/v2/game-samples/rogue-moba), [Gaming Circle terms](https://www.photonengine.com/terms/gamingcirclemembership) |
| 23 | Unity 6 Web: managed C# runs on a single thread; profiling needs a Development Build ("Enable Web Profiling" and the template's Profile button for a phone); FrameTimingManager works but reports no GPU time; the Diagnostics Overlay works in Release builds and shows FPS plus JS and WASM heaps; mobile performance testing should use a non-Development "Disk Size with LTO" build. | Verified | [profile a Web build](https://docs.unity3d.com/6000.6/Documentation/Manual/web-profile.html), [technical limitations](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-technical-overview.html), [FrameTimingManager](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/FrameTimingManager.html), [mobile optimisation](https://docs.unity3d.com/6000.6/Documentation/Manual/web-optimization-mobile.html) |
| 24 | "Redmi 11C" is not a released Xiaomi model (absent from gsmarena and from Wikipedia's Redmi list; the only dedicated article is a December 2022 "upcoming" rumour). Likely devices: Redmi 12C (Helio G85, Mali-G52 MC2, 2 to 6 GB, 720x1650) or Redmi 10C (Snapdragon 680, Adreno 610). Chromium's blocklist has no WebGL 2 restriction for any Android GPU. | Verified absence, inferred candidates | [Redmi 12C](https://www.gsmarena.com/xiaomi_redmi_12c-12051.php), [Redmi 10C](https://www.gsmarena.com/xiaomi_redmi_10c-11418.php), [Wikipedia Redmi](https://en.wikipedia.org/wiki/Redmi), [Chromium blocklist](https://chromium.googlesource.com/chromium/src/+/main/gpu/config/software_rendering_list.json) |
| 25 | Online simulation CPU on the phone will exceed the local test by an unmeasured factor (verified ticks plus predicted depth re-simulated each update, over TCP WebSockets where loss turns into head-of-line delay and deeper prediction). | Inferred | rows 1 to 3 |
| 26 | Halving the simulation rate to 30 Hz, setting ChecksumInterval 0, shrinking RollbackWindow to 30 or 40 and enabling prediction culling should roughly halve simulation CPU; the exact gain is not published anywhere. | Inferred | rows 2 and 4 |
| 27 | A Fusion Server peer most likely occupies one CCU for as long as it is connected (the docs exempt only sessions that never connect to the cloud; the forum replies stating it explicitly now return 404), so a 3v3 match costs 7 CCU and the free tier holds about 14 concurrent matches. | Inferred | [pricing doc](https://doc.photonengine.com/photon/current/pricing), [encryption](https://doc.photonengine.com/fusion/v2/manual/advanced/encryption) |
| 28 | With no money per match, "majority of client results plus archived server input stream" is an acceptable MVP level; the headless re-simulation worker can follow because the inputs are already stored. | Inferred | rows 7 and 8 |
| 29 | If the browser is kept, a Fusion Server deployment forces every browser client through the Photon relay over TCP, losing the direct UDP path that is the dedicated server's main advantage. | Inferred | row 13, [connection process](https://doc.photonengine.com/fusion/v2/manual/connection-and-matchmaking/connection-process) |
| 30 | A native Android build of the Quantum project (multithreaded simulation, UDP) is a cheaper fallback than an SDK switch if WebGL CPU fails on the phone; Photon itself advises offering a download. | Inferred | row 1, [multithreading](https://doc.photonengine.com/quantum/current/manual/multithreading) |
| 31 | The PUN2 app does not consume the free 100 CCU slot (PUN has separate plans, the slot is per Fusion or Quantum AppId). | Inferred | row 17 |
| 32 | p95 frame time and simulation time of an online, representative 3v3 (6 heroes, 9 to 12 minions, 2 towers, projectiles) on the real phone. **This is the decisive unknown.** | Unknown | block B0 |
| 33 | Whether the free 100 CCU plan was already activated on the existing Fusion AppId. If so the Quantum app cannot take the slot unless the Fusion app is upgraded to a paid plan (downgrading to free is not allowed). | Unknown | [pricing doc](https://doc.photonengine.com/photon/current/pricing), Photon dashboard |
| 34 | Whether Photon permits publishing sample-derived source in a public repository (7.3 grants use "in the Application"; nothing grants redistribution of the Sample source), which matters only if the hackathon requires open source. | Unknown | [license terms](https://www.photonengine.com/terms/licenseterms) |
| 35 | The exact phone model, GPU and Chrome version. | Unknown | `adb shell getprop ro.product.model`, `UNMASKED_RENDERER_WEBGL` |
| 36 | Whether Unity 6000.6.4f1 produces a working Quantum 3.0.14 WebGL release build with LTO (the known LTO crash needs the sharp-zip-lib package). | Unknown | [WebGL manual](https://doc.photonengine.com/quantum/current/manual/webgl) |
| 37 | Which dashboard key spellings the live Public Cloud plugin reads (`WebHookEnableAddPlayer` vs `WebHookEnableRuntimePlayer`, `WebHookEnableOnCreate` vs `WebHookEnableCreateGame`) and the real defaults of `BlockNonProtocolMessages` and `BlockPlayerProperties`; Photon's pages contradict each other. Set both spellings and set the Block keys explicitly. | Unknown | [webhooks](https://doc.photonengine.com/quantum/current/manual/webhooks), [dashboard configuration](https://doc.photonengine.com/quantum/v3/manual/game-session/dashboard-configuration), [API reference](https://doc-api.photonengine.com/en/quantum/current/class_photon_1_1_deterministic_1_1_deterministic_plugin.html) |
| 38 | The hackathon freeze date and whether an open-source repository is required. | Unknown | organiser announcement |

## 3. Security actually obtained with Quantum on the Public Cloud

The distinction the project asked for: on the Public Cloud the Photon server is authoritative
about which inputs exist at which tick (it expires late inputs, replaces missing ones, delta
compresses and streams the final per-tick input set). It never runs the game simulation, so
nothing it sends is authoritative about state or outcome. "Confirmed inputs" is the strongest
guarantee Photon gives; "server-computed result" exists only on Enterprise.

| Goal | Public Cloud mechanism | Level at MVP | Residual limit | Work to build |
| --- | --- | --- | --- | --- |
| Only the owner of a Genesis enters with it | Custom auth returns `UserId` = canonical wallet and `AuthCookie` = signed session claims; JoinGame or CreateGame returns 400 for unknown or banned users; AddPlayer checks ownership for the `AuthCookie` wallet and returns 400 (`WebhookError`) or 200 with a server-built `RuntimePlayer`. | Strong: the client cannot bypass any of the three gates. | AddPlayer webhooks are serialised per room and time out (10 or 30 s, docs disagree); on-chain reads must be cached at auth time, not performed inside the hook. | Auth service (nonce, signature check), `/game/create`, `/game/join`, `/player/add` endpoints, ownership cache. |
| Loadout (class, signature, purchased specialisation) cannot be forged | AddPlayer response replaces the client's `RuntimePlayer` entirely; GameConfigs and CreateGame responses pin `RuntimeConfig` and `SessionConfig`; dashboard `MaxPlayerSlots` = 1, `BlockPlayerProperties`, `BlockRoomProperties`, `BlockNonProtocolMessages`, global `SessionConfig` JSON. | Strong, provided the backend owns a JSON schema of `RuntimePlayer` identical to the client build (`$type` tag, complete object). | Schema drift between backend and build breaks deterministic deserialisation; the sample's `CommandsSystem` (prototype selection, teleport, spawn) must be removed from public builds. | `/game/configs` endpoint, `RuntimePlayer` builder on top of `shared/Chog.Heroes`, removal of debug commands. |
| Match result credited to the career | Simulation raises `GameResult` once at game over (the event is `Synced`, so it fires on a verified frame); each client uploads it; GameResult webhook delivers the groups with vote counts; backend compares. ReplayStart and ReplayChunk stream the authoritative inputs. | Medium: Photon labels the result "untrusted client input"; majority vote fails on a 3 vs 3 split; absent results (crash, Alt+F4) leave only the input stream. | No server signature on results; the Public Cloud `GameResult` payload may or may not include `AuthCookie` (fixed in plugin 3.1); `CallbackGameResultResponse` does not exist in SDK 3.0.9, so the client must delay leaving after raising the event. | `/game/result`, `/replay/start`, `/replay/chunk`, `/game/close` endpoints; credit policy (unanimous human results credit; split or missing results go to re-simulation); headless worker: `Quantum.Runner.Dotnet` extended to subscribe `EventGameResult`, version-pinned asset DB and LUT per build. |
| Nobody plays with a tampered state against others | Determinism: a modified client only affects its own view; other clients re-simulate from inputs. | Strong by construction. | Aimbots and input bots are not preventable anywhere. | None. |
| Hidden information | None on the Public Cloud. | Not applicable to V1 (no fog of war, no hidden picks). | A future secret fog of war would be readable by a modified client. | Decide before adding such a feature. |
| Late join and reconnection | Buddy snapshot from a peer; `SnapshotsBlocked` in Join or Create responses excludes untrusted providers. | Adequate for 3v3 with bots. | The server does not validate snapshots; a tampered peer could seed a joiner's state; the input stream from tick 0 remains the state-independent artifact. | PlayerTTL 20 s, EmptyRoomTTL, `ClientId` = `UserId`, runner recreation flow. |

## 4. Fusion Server: what it would buy and what it would cost

Gains, all verified in the Fusion manual: full state authority on our process (speed hacks and
memory edits "impossible"); hidden information possible; bots, minions and towers simulated
off the phone; a result produced by our own process and pushed to the backend; reconnection
with a persistent hero object through `PlayerUniqueId` or connection tokens; lag compensation
and input delay available.

Costs and limits, all verified unless marked:

- Browser clients: Client/Server modes in WebGL are "not recommended and by default disabled";
  the project already set `AllowClientServerModesInWebGL` to true in
  `NetworkProjectConfig.fusion`. No Photon sample shows a WebGL client on a dedicated server;
  both official WebGL samples use Shared. A browser cannot open the direct UDP connection, so
  all traffic goes through the Photon relay over TCP (inferred from "direct connections are not
  supported for WebGL builds").
- Hosting: one headless Unity instance per match ("spinning up complete Unity instances for
  every game session"), a Linux Dedicated Server build target with the `FUSION_WEAVER` define,
  STUN or `CustomPublicAddress`, a region pinned per server, an orchestrator we operate or rent
  (Edgegap at $0.00115 per vCPU minute, Gameye at $0.07 per vCPU hour, or a Hetzner CPX22 at
  EUR 19.49 per month plus IPv4 and VAT; Hathora no longer exists, it redirects to GameFabric,
  which publishes no prices). Nobody publishes matches per machine; a load test would have to
  produce that number.
- Capacity: one CCU per running server (inferred, row 27), so about 14 concurrent 3v3 matches
  on the free 100 CCU tier instead of 16.
- Work with 13 days left: server-side bots, minions and towers from scratch (the Quantum sample
  does not port); projectile data buffers because spawns are not predicted; lag-compensated
  hitboxes; reconnection mapping; orchestration, health checks and logs; the same auth and
  backend as the Quantum option, minus the replay worker.
- Operational risk relevant to a judged demo: a match server that is not running during the
  review window is a failed demo, whereas the Quantum option has no per-match process to keep
  alive, only the Photon Cloud and a stateless backend.

Fusion Shared and Host are covered in section 1; neither is a candidate for the product as
specified.

## 5. The Twin Stick Shooter sample: durable base or demo to replace?

Durable, reusable as is (verified by reading the sources; about 6,500 lines of hand-written
simulation code plus 3,500 lines of view code):

- Player and bot input unification (`InputContainer`, `Bot.Input`, `InputSystem`,
  `AISetupHelper.Botify`), room filling and player replacement (`PlayerJoiningSystem`,
  `RuntimeConfig.FillWithBots`, `Player Replacement HFSM`).
- Teams (`TeamInfo`, `TeamData`, `MAX_TEAM_SIZE 3`), per-team spawn points and `RespawnSystem`.
- Attributes with modifiers, `Health`, `Immunity`, `Respawn`, damage and heal events.
- `Skill` and `Attack` entities with data assets (`AreaOfEffectAttackData`,
  `BallisticAttackData`, `RangedAttackData`), `SkillSystem`, `AttackSystem`.
- Top-down 2D `KCC`, `MovementSystem`, `RotationSystem`, auto-aim direction.
- `LineOfSightHelper`, bush `VisibilitySystem`, `AIMemory`, sensors, tactical sensors,
  `AIDirector` plumbing, context steering, navmesh agents.
- Match skeleton (`GameState`, HFSM game manager, `GameplaySystemsGroup` toggling, synced
  `GameOver { WinnerTeam }`), camera, HUD, floating text, on-screen sticks and buttons, audio.
- The project's own additions: `GenesisCombatLoadout`, `GenesisLoadoutSelection`,
  `RuntimePlayer.Genesis.cs`, `Chog.Controls`, 41 passing headless tests.

Rules that must change: the Coin Grab HFSM and its UI; director strategies keyed on
`AvailableCoins` and `Score`; the `Collect`, `Hide`, `TakeCover`, `GuardObjective` tactics and
their HFSM leaves; the two fixed skill slots (`BasicSkillData`, `SpecialSkillData`) versus the
planned attack plus class ability plus signature; `MAX_AIM_DISTANCE`, cover and hiding markers
and the navmesh, all map specific; the `CommandsSystem` debug commands.

Absent entirely: minions, towers, core, wave spawner, aggro rules, lane AI. `ObjectivePoint`
is an empty marker. These are additions, not reconfigurations, in either engine.

Licensing for a public build (verified terms, inferred application):

- Code, HFSM documents and prefab logic: usable in our Photon Application under terms 7.3.
  Publishing the modified sample source in a public repository is not expressly granted
  (row 34): ask Photon in writing if the hackathon requires open source.
- Shippable as is: KayKit Dungeon Pack 1.0, KayKit Character Animations (CC0), the 100 CC0
  SFX pack, "The Enemy" (CC0 per the credits file), TextMesh Pro's LiberationSans (OFL).
- Must be replaced or rebuilt before distribution: the eleven `.anim` clips and two Animator
  controllers (rebuild from the CC0 FBX), Photon's UI PNGs, materials and seven VFX prefabs,
  `Background_Audience.ogg` (uncredited), and the JMO Cartoon FX folders (acquire "Cartoon FX
  Remaster Free" under our own Asset Store account or replace the single `CFXR2 Hit` prefab and
  delete both folders).

Verdict: the sample is a durable combat and AI framework and a Coin Grab demo on top of it.
The framework carries a 3v3 lane MOBA; the demo layer is a few assets and one HFSM to replace.

## 6. Pricing and terms (consulted 2026-10-08)

- Quantum and Fusion are priced identically; any cost difference comes from hosting Fusion
  game servers.
- Free 100 CCU: one app per studio, Fusion or Quantum, not both; licensed for commercial
  launch; hard cap (clients get `MaxCcuReached`); stackable only with the $95 one-time 100 CCU
  for 12 months (200 CCU total); the slot moves to another app only when the current holder is
  upgraded to a paid plan. The project must check in the dashboard whether the Fusion AppId
  already holds it (row 33).
- 100 CCU means about 16 concurrent 3v3 matches on Quantum (6 CCU per match).
- Traffic: 0.3 TB per month on the 100 CCU plan; Quantum traffic is input only and delta
  compressed; no per-client figure is published, measure with the Graph Profiler bandwidth graph.
- Webhooks and custom authentication carry no fee. Enterprise (server simulation) starts at a
  2,000 CCU minimum, price on request, plus Gaming Circle membership for the Plugin SDK.
- PUN2 keeps its own plan; the 20 CCU plan is licensed for non-commercial use only, which is a
  separate compliance point for the PUN2 app.
- Hetzner's cost-optimised CX line (the CX33 cited in the previous decision) is marked "not
  available" on 2026-10-08.

## 7. The phone stutter: what can be attributed today, and the protocol

With the information available the cause cannot be attributed. What is known about the run:

- Local mode: no network, no rollback (row 3). The run says nothing about online cost.
- Simulation at 60 Hz with six HFSM bots on a navmesh, RollbackWindow 60, ChecksumInterval 60.
- Release Quantum DLLs, but `QUANTUM_LOGLEVEL_DEBUG` compiled into the WebGL build, exceptions
  "explicitly thrown", stripping level Low, heap starting at 32 MB with geometric growth,
  `devicePixelRatio` not forced, no post-processing components in the gameplay scene.
- Device unknown ("Redmi 11C" does not exist as a model; row 24), Chrome version unknown, and
  whether both sticks were used unknown.

Protocol (two builds, same commit, repeated after ten minutes of play for thermal effects):

1. Identify the device: `adb shell getprop ro.product.model` or the `UNMASKED_RENDERER_WEBGL`
   string in the DevTools console attached through `chrome://inspect`.
2. Build R (measure): Development Build off, "Disk Size with LTO" (second data point: "Runtime
   Speed with LTO"), exceptions None, debug symbols off, stack traces for Log and Warning off,
   `QUANTUM_LOGLEVEL_DEBUG` removed, ASTC textures, quality Low, Frame Timing Stats on,
   Diagnostics Overlay on, Run In Background on, the Quantum Graph Profiler prefab in the
   scene, `com.unity.sharp-zip-lib` present. Build D (attribute): the same plus Development
   Build, connected to the Editor through "Enable Web Profiling" and the template's Profile
   button.
3. Scripted load, identical in every variant: six heroes (bots on fixed paths casting on a
   timer), nine to twelve minions per wave, two towers firing, a fixed camera spline, five
   minutes, no human input except in a "touch drag" sub-variant.
4. Metrics per minute: p50, p95 and p99 of `Time.unscaledDeltaTime`, frames above 33 ms and
   above 50 ms, `cpuMainThreadFrameTime` and `cpuMainThreadPresentWaitTime` from
   `FrameTimingManager`, Quantum `Session.Stats` (simulation time split verified and predicted,
   simulated frames, prediction depth), WASM heap from the overlay, thermal status from
   `adb shell dumpsys thermalservice`.
5. Variants and what each isolates: BASE online; HALF-RES (`devicePixelRatio: 1`) moves the
   p95 the most means GPU bound; NO-RENDER (camera and UI off) stays high means CPU main thread;
   NO-BOTS isolates AI and pathing; SIM-ONLY (no render, no network, bots on) against NO-RENDER
   isolates the network stack and rollback; LOCAL versus WI-FI versus 4G isolates transport;
   HEAT isolates throttling; MEMORY (heap sawtooth aligned with spikes) isolates GC and growth.
6. Fairness between engines or architectures: same phone, same Chrome, same entity counts,
   same animator and VFX budgets per entity, same resolution policy, same build flavour, phone
   cooled between runs, charger unplugged, three repetitions, report distributions and not an
   average FPS. Comparing the full Quantum sample with the Archer Fusion slice is not a
   comparison.

Acceptance bar (the project's own, from [PLAN](PLAN.md), no vendor publishes one): p95 frame
time at or under 33.3 ms over five minutes on the target device, with the simulation leaving
room for rendering (a working budget of at most 12 ms p95 for `Session.Stats` simulation time
at 30 Hz is proposed here as a starting point, to be revised with the first measurement).

## 8. Remaining work for the recommended option, in testable blocks

Dates are a proposal against the derived freeze of 2026-10-22; a block ends on its exit test,
not on its date.

**B0, gate, 2026-10-09 to 10-10.** Identify the phone (row 35). Check the dashboard for the
free 100 CCU slot (row 33) and create the Quantum AppId. Upgrade Quantum 3.0.9 to 3.0.14
(no breaking change listed in between; brings the late-joiner physics desync fix, the 32-bit
memory integrity fix and macOS runner support) and add `com.unity.sharp-zip-lib`. Tune
`SessionConfig`: 30 Hz, RollbackWindow 30 to 40, ChecksumInterval 0, InputDelayMin 0 (try 2
later). Apply the build settings of section 7. Build the scripted load scene with minion and
tower placeholders. Run BASE online between the Mac and the phone and the ablations.
Exit test: the acceptance bar of section 7 is met online at representative load, or the
decision flips per section 9. Nothing else starts before this verdict.

**B1, online match, 2026-10-10 to 10-11.** Sample menu with the Quantum AppId; two clients
(desktop and phone) in one room; PlayerTTL 20 s and EmptyRoomTTL set; reconnection of the
phone with the same `ClientId` and a buddy snapshot; bot replacement of a disconnected player
and hand-back on return; dashboard hardening (`MaxPlayerSlots` 1, the three Block keys set
explicitly, global `SessionConfig`, `CloseRoomAfterStartSec`). Exit test: a phone that loses
Wi-Fi for 8 s resumes its hero; a second phone cannot add a second player slot.

**B2, MOBA rules, 2026-10-11 to 10-15.** Lane map and navmesh bake; wave spawner (first wave at
5 s, then every 20 s, three minions); tower targeting (minions first, aggro on a hero that
attacks a protected hero); core; victory HFSM replacing Coin Grab; respawn 6 s; bot tactics
re-based on lane state (push, defend tower, retreat); third skill slot (attack, class
ability, signature) with its input, HUD and bot support; removal of debug commands from
public builds. Exit test: two humans and four bots play two consecutive matches to core
destruction with a correct `GameOver { WinnerTeam }` on both clients, then rematch without a
scene reload.

**B3, Genesis admission, 2026-10-13 to 10-16.** Auth service (wallet signs a nonce;
`UserId` = canonical address; `AuthCookie` = session claims); dashboard rejects anonymous
clients; `/game/create`, `/game/join`, `/game/configs`, `/player/add`, `/player/added`,
`/player/removed` endpoints; `RuntimePlayer` built from `shared/Chog.Heroes` and the admitted
tokenId; ownership cached at auth time. Exit test: two real Genesis with visibly different
signatures in one match; a client that sends a forged `RuntimePlayer` receives the backend's
version; a wallet that does not own the tokenId gets error 33 and stays in the lobby.

**B4, verified result, 2026-10-15 to 10-18.** `GameResult` raised once from the simulation at
game over with a grace delay before leaving (no `CallbackGameResultResponse` in 3.0.x);
`/game/result`, `/replay/start`, `/replay/chunk` and `/game/close` endpoints storing result
groups and the input stream keyed by `GameId`; credit policy (unanimous human results credit
once, idempotent by `GameId`; split or missing results are queued for re-simulation and not
credited). Stretch, or after the freeze: export the asset DB and LUT per build, extend
`Quantum.Runner.Dotnet` to subscribe `EventGameResult`, re-simulate a stored stream and compare.
Exit test: a match credits each Genesis once; replaying the webhook does not credit twice; a
client that submits a false result produces a split that is not credited; the stored stream
of that match re-simulates to the true winner (stretch).

**B5, $CHOG specialisation, 2026-10-16 to 10-19.** Purchase transaction to the configured
treasury; indexer confirming the event with block reorg tolerance; backend marks the
specialisation owned by chain, collection and tokenId; `/player/add` includes it in the
`RuntimePlayer`; lobby preview and equip. Exit test: purchase on a fork or testnet then on
mainnet; a reverted transaction unlocks nothing; a duplicate event credits once; an indexer
outage resumes from the last block; the specialisation changes the signature visibly in the
next match and not in a running one.

**B6, ship, 2026-10-19 to 10-22.** Replace Photon-authored animations, UI, VFX and the
uncredited audio; remove the JMO folders or license the free pack; HTTPS deployment with
Brotli and a PWA shell; holder playtest; three-minute video; licences declared; [STATUS](STATUS.md)
and decision log updated. Exit test: a fresh phone installs the PWA, plays a full match with
its Genesis, and the career page shows the credited result.

## 9. Conditions that flip the recommendation, and the fastest test for each

| Condition | New recommendation | Fastest test |
| --- | --- | --- |
| B0 fails: after 30 Hz, culling, kinematic minions and no more than twelve minions, p95 frame time stays above 33 ms online on the target phone, with `Session.Stats` showing the simulation as the dominant cost. | First try a native Android build of the same Quantum project (multithreaded simulation, UDP, no WebGL dispatch cost). If the browser is mandatory, switch to Fusion Server and accept the relay path and the hosting work; take Fusion Shared only for a demo without a career. | The B0 run itself, two days. The native Android build is one extra build of the existing project. |
| The product adds information that must stay secret (fog of war with integrity, hidden picks) or money per match. | Fusion Server (Quantum Enterprise is out of reach at a 2,000 CCU minimum). | A product decision, no test needed; decide before writing the feature. |
| The free 100 CCU slot is held by the Fusion AppId and cannot be freed. | No engine change; budget $95 for the one-time 100 CCU on the Quantum app, or 20 CCU during the hackathon. | Dashboard, ten minutes. |
| The hackathon requires a public repository and Photon does not allow redistribution of sample-derived source. | No engine change; isolate our code from the sample's in the published repository, or publish without the sample folders. | Read the hackathon rules; one email to Photon licensing. |
| Unity 6000.6.4f1 cannot produce a release WebGL build of Quantum 3.0.14 with LTO. | No engine change; move the project to Unity 6000.3 LTS, which both SDKs list. | The B0 build. |
| Photon confirms that the Public Cloud plugin ignores the AddPlayer override or the replay webhooks for this AppId. | Re-evaluate; the security argument for Quantum would weaken to the Shared level. | The B3 forged `RuntimePlayer` test and the B4 chunk count. |

## 10. Errors in the two previous reasonings

The Fusion Server decision ([DECISION-RESEAU.md](DECISION-RESEAU.md)):

- It treated server authority as required for a career with no money at stake, without
  evaluating the Quantum Public Cloud security ladder that Photon documents (webhooks,
  replay stream, re-simulation). The threats that matter here (loadout forgery, result
  forgery, admission) are covered on Quantum without a server.
- It asserted a mobile CPU benefit with no measurement, while accepting Photon's "not
  recommended and by default disabled" status for WebGL clients in Server mode as a
  "contrepartie" without pricing the relay over TCP or noting that no Photon sample exists
  for that pairing.
- It underestimated the operating scope in a 13-day window: one headless Unity process per
  match, orchestration, one CCU per server, health and logs, and the demo-availability risk.
- It left the server-to-backend result path (authentication of our own server, idempotent
  credit) undesigned while criticising Shared for the same gap.
- It was right on three points worth keeping: Shared is not a first architecture to migrate
  from later; projectiles and lag compensation need design in Fusion; the cost tables were
  accurate for their date (the CX33 is now unavailable).

The Quantum decision ([DECISION-RESEAU-REVUE.md](DECISION-RESEAU-REVUE.md)):

- It used a local-mode phone run as evidence of mobile viability. Local mode performs no
  rollback, and the sample runs an untuned 60 Hz, 60-frame window, checksums-on config with a
  debug log level compiled in. The run measured neither the online cost nor a tuned build.
- "Chaque client simule les frames vérifiées" understates the cost model: each client
  re-simulates verified plus predicted frames on every update.
- It assumed the free 100 CCU plan would be available to the Quantum app without checking
  the one-app-per-studio rule against the Fusion AppId that already exists.
- It recorded that Photon documents web export without recording Photon's caveats (lower
  performance, offer a download) and without noting that neither SDK lists Unity 6000.6.
- It listed the third-party assets but not that Photon-authored animations, UI and VFX are
  "Assets" under the licence too, nor the JMO paid packs in the tree, nor the open-source
  question.
- It was right on the architecture of admission and results, on the "inputs confirmed is not a
  server result" distinction, and on bounding the scope.

Common to both: the engine was chosen on a narrative (authority, then code in hand) before a
measurable gate was defined; the phone was never identified; and the reasoning reused the
rule from the playbook that a lesson in a document does not fire on its own. The gate in B0
is the correction.

## Appendix: research notes (primary sources, tagged claims)

- `research/research_notes/Audit architecture Photon MOBA Genesis/quantum3_security_webhooks_replays.md`
- `research/research_notes/Audit architecture Photon MOBA Genesis/quantum3_netcode_mobile_webgl.md`
- `research/research_notes/Audit architecture Photon MOBA Genesis/fusion2_topologies_webgl_server.md`
- `research/research_notes/Audit architecture Photon MOBA Genesis/photon_pricing_terms_hosting.md`
- `research/research_notes/Audit architecture Photon MOBA Genesis/twin_stick_sample_reuse_licences.md`
- `research/research_notes/Audit architecture Photon MOBA Genesis/mobile_webgl_profiling_protocol.md`

Local facts cited: `game/Assets/QuantumUser/Resources/SessionConfig.asset`,
`game/ProjectSettings/ProjectSettings.asset` (WebGL defines and player settings),
`game/Assets/Photon/Quantum/Assemblies/` (Release DLL hash), `game/Assets/Photon/Quantum/CHANGELOG.md`
(SDK 3.0.9 Build 1932, 2025-12-10; the website lists 3.0.9 as Build 1927, a discrepancy
without known consequence), `game-fusion/Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion`
(`AllowClientServerModesInWebGL` true, PlayerCount 6, lag compensation disabled).
