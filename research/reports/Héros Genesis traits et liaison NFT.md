# Price the traits, key the career

The differentiator holds, and the evidence says to deepen it by pricing and publishing what the catalog already contains, not by adding roles or 3D characters. The catalog's small vocabulary (3 classes, 5 shapes, 10 effects, 5 deliveries) gives an ability range in the order of a 20 to 40 hero launch roster, but three of its assumptions are contested: three class labels are fewer than the 5 to 7 that most shipped team action games use, "rarity changes delivery, not power" is false until each delivery is priced on a cost curve, and stun and invisibility need structural caps. On visuals, no precedent was found of playable avatars derived automatically per token from 2D art, the Genesis images are busts, and four recorded 1:1 tokens carry no traits, so the one approach that shows every holder's exact NFT inside two weeks is the real image on a shared body. The architecture fits on one static host, one serverless backend with a small SQL database, one small shop contract and direct chain reads, with a server-verified sign-in that the earlier game never had. Binding one NFT to another exists and works on the unmodifiable Genesis contract in two families: an ERC-6551 account controlled by the Chog (contracts verified on Monad mainnet) and state keyed by chain, collection and token id. Only the second cannot be emptied by a seller before a sale, and neither appears on OpenSea, so the game must publish its own page per token. Monad adds no NFT standard of its own; its real constraints are operational (gas charged on the limit, capped log queries, a 10 MON reserve on delegated accounts). Three actions are urgent: a written question to the Chog team about art rights (none are published), a build test of the wallet SDK on the project's Unity version (no source covers it), and the completion of the metadata, since only 552 of the 1,969 Genesis can enter a match today.

How to read this report. **VERIFIED** means a researcher read the cited page or file, or measured the value, on 9 October 2026. **INFERRED** means reasoning or arithmetic from verified facts, or a claim seen only in a search summary. **UNKNOWN** means searched and not established. **PROPOSAL** marks every recommendation: design decisions belong to the project owner. Effort figures are the notes' estimates, never measurements. Local evidence paths are relative to `/Users/red.g/CascadeProjects/chogathon/`. This report adds no research of its own; it synthesises the five notes in `research/research_notes/Héros Genesis traits et liaison NFT/`.

## Four questions, four direct answers

| Part of the question | Direct answer | Status of the evidence |
|---|---|---|
| Differentiator: roles | Keep the three Body classes as stat envelopes. Add a second role label derived from the kit and publish who counters whom. Do not add classes as new characters. | Class counts of shipped games VERIFIED. The fit to this roster is INFERRED. |
| Differentiator: attacks | Keep one basic attack plus one signature. Build 5 shape meshes and 10 effect colours, not 50 assets. Cap stun and invisibility. Make support effects timed and visible. | Patch values and rules VERIFIED. The caps are INFERRED starting values. |
| Differentiator: balance | Give every kit the same budget. Price each shape, effect and delivery in one currency. Tune the axis levels, never one NFT. Check with bot matrices and fixed win-rate bands. | Method VERIFIED in the literature. Nothing measured for this game. |
| Differentiator: visuals | Show the holder's real image (portrait disc first, face plate next) on a shared body, with Skin and Background tint and an aura effect. | Art facts VERIFIED. Effort and performance are estimates. |
| Architecture | Unity WebGL with Reown AppKit, EIP-4361 sign-in verified by a backend, session passed to Photon custom authentication, `ownerOf` read at admission, career in SQL keyed by token, one small shop contract, no mandatory indexer. | Building blocks VERIFIED one by one. The assembled flow is a PROPOSAL that no source documents end to end. |
| NFT bound to an NFT | Yes, it exists. ERC-6551 and keyed state both work on the immutable Genesis contract. Nesting standards that the parent must implement do not. | Standards and on-chain presence VERIFIED. Marketplace display: none found. |
| What Monad allows | The standard EVM NFT stack, unchanged, with cheap gas and 600 ms finality. No Monad-specific NFT standard and no NFT or gaming grant was found. | Docs and live RPC calls VERIFIED. |

## The catalog already holds a launch roster of abilities; pricing and legibility are what is missing

**Direct answer.** The request allows for "many roles and different attacks" and new Blender characters. The evidence points elsewhere. The vocabulary is large enough. What it lacks is a price for every building block, a published rule for how blocks interact, and one unmistakable look per block.

### Three class labels are too few to read, and a second label costs no new character

Shipped team action games converge on 5 to 7 mechanically defined classes (VERIFIED). Two studios split coarser schemes in public: Blizzard replaced four Heroes of the Storm roles with six because "the existing roles have grown limited in accurately describing each individual hero" ([Blizzard](https://news.blizzard.com/en-us/article/22921910/novye-roli-geroev)), and Riot moved to 7 classes and 11 subclasses ([League of Legends Wiki](https://wiki.leagueoflegends.com/en-us/Champion_classes)). Supercell defines 7 Brawl Stars classes and publishes their counter relations, for example Tanks counter Assassins and are susceptible to Damage Dealers ([Supercell, April 2023 patch notes](https://supercell.com/en/games/brawlstars/blog/release-notes/mysteryatthehub-patch-notes/)). Dota 2 shows another route: a 9-slot role vector graded 0 to 3 plus an official complexity rating of 1 to 3 ([Dota 2 data feed](https://www.dota2.com/datafeed/herolist?language=english)).

| Game | Class scheme (VERIFIED) | Roster at launch (VERIFIED unless stated) | Source |
|---|---|---|---|
| Brawl Stars | 7 classes with published counters | not found | [Supercell](https://supercell.com/en/games/brawlstars/blog/release-notes/mysteryatthehub-patch-notes/) |
| League of Legends | 7 classes, 11 subclasses | 17 at closed beta; a target of 20 was doubled for release | [Wikipedia](https://en.wikipedia.org/wiki/League_of_Legends) |
| Heroes of the Storm | 6 roles (4 before) | more than 30 | [Blizzard press release](https://blizzard.gamespress.com/BLIZZARDS-WORLDS-COLLIDE-WHEN-HEROES-OF-THE-STORM-LAUNCHES-JUNE-2) |
| Pokemon UNITE | 5 battle types, each a stat profile | about 20 | [Pokemon UNITE](https://unite.pokemon.com/en-us/pokemon/), [InvenGlobal](https://labs.invenglobal.com/articles/14546/july-21-confirmed-as-pokemon-unite-launch-date-on-nintendo-switch) |
| Battlerite | 3 categories, as a top layer only | 22 | [Stunlock Studios](https://press.stunlock.com/freetoplay/) |
| Omega Strikers | 2 positions | 15 (read from a search excerpt) | [Wikipedia](https://en.wikipedia.org/wiki/Omega_Strikers), [BusinessWire](https://www.businesswire.com/news/home/20230208005989/en) |
| Squad Busters | not found | more than 25 | [TouchArcade](https://toucharcade.com/2024/05/29/squad-busters-download-iphone-android-release-out-now-global/) |

The current build has three classes (brute, ranger, controller), decided by Body (`content/README.md`). With 1,969 units, each label covers hundreds of different kits and does not tell an opponent what to expect (INFERRED). Readability in the games above comes from three devices, not from the class count alone: each class is a stat envelope plus a job, the counter relations are published, and outliers get an explicit "does not fit" bucket such as Riot's Specialist (INFERRED). **PROPOSAL:** keep the three Body classes as stat envelopes, derive a second visible label from the kit (range band from the shape, support flag from the effect), keep a "hybrid" label for combinations that cross roles, and print the counter relations on the hero card. This needs no new 3D character. The census records 86 distinct Body values among 552 tokens (VERIFIED, `research/evidence/genesis-trait-census.json`), so a move to five or more Body classes would mean remapping every value (INFERRED by the report writer).

### One attack and one signature fit the button budget of the mobile leader

The shape by effect by delivery grid has the same structure as Hades (one boon per slot, each god owns one signature effect) and Magicka (elements times cast modes) ([Hades Wiki](https://hades.fandom.com/wiki/Boons), [Wikipedia, Magicka](https://en.wikipedia.org/wiki/Magicka)). Systems of this kind stay predictable through a few hard rules (INFERRED from those VERIFIED sources): one modifier per slot with replacement, a published rule when parts conflict, one name and one visual per effect, and no stacking of the same effect. Brawl Stars treats stacking slows as a bug: "Squeak's Residue slow could stack and immobilize players" ([Supercell, February 2026](https://supercell.com/en/games/brawlstars/blog/release-notes/release-notes-february-2026/)). The catalog already satisfies the first rule. It has no stated answer for the second and the fourth: what happens when a burn and a slow from two players land on one target, or when a heal meets a poison (INFERRED).

Control effects need limits written into the system. In the 2026 Brawl Stars patches, hard stuns sit between 0.5 and 2 seconds and move down (Bull 2 s to 1 s, Kit 2 s to 1.5 s), a slow was cut from 30% to 25% for being "too oppressive", and invisibility is a 6 second Super on one assassin ([Supercell, February 2026](https://supercell.com/en/games/brawlstars/blog/release-notes/release-notes-february-2026/), [Supercell, April 2026](https://supercell.com/en/games/brawlstars/blog/release-notes/release-notes-april-2026/), [Brawl Stars Wiki, Leon](https://brawlstars.fandom.com/wiki/Leon)). These are one studio's values, not a rule (VERIFIED as values, INFERRED as guidance). Riot's designers named the failure modes: an effect whose "anti-fun" for the victim exceeds the user's fun, and "Power Without Gameplay", with team auras as the example ([Zileas' list, repost](https://lawofgamedesign.com/2014/02/20/zileas-list-of-game-design-anti-fun-patterns/)). **PROPOSAL (starting values to test, not laws):** hard stun at most 1.0 to 1.5 s, slows around 25 to 30%, a per-target diminishing rule, invisibility of a few seconds with reveal on dealing or taking damage and on proximity, and heal, cleanse and buffs delivered as timed zones or pulses with a visible radius.

On controls, the strongest evidence is Supercell's own history. Brawl Stars survived soft launch only after moving to landscape with two sticks: "Our decision was landscape or kill [the game]" ([PocketGamer.biz](https://www.pocketgamer.biz/brawl-stars-we-at-supercell-have-never-prepared-more-for-a-game-launch/)). Target assistance reduced the score gap between players of different skill, and weaker players reported more fun ([Bateman et al., CHI 2011](https://doi.org/10.1145/1978942.1979287)). Tutorials raised engagement only in the most complex of three games tested on over 45,000 players ([Andersen et al., CHI 2012](https://doi.org/10.1145/2207676.2207687)). **PROPOSAL:** two action buttons at first contact, aim assist by default with manual aim available, and explicit teaching only for the delivery modes.

Blender time pays most on the shapes, not on more attacks. One fixed silhouette per shape (flash as a short cone, wave as a travelling arc, nova as an expanding ring, puddle as a ground disc, beam as a line) and one hue per effect give the 50 combinations from five meshes and ten colours (INFERRED, trait note). Riot's rule is that "VFX should communicate a spell's gameplay space, power, and function, without creating a distraction" ([Riot VFX style guide, archived](https://web.archive.org/web/20191022110249/https://nexus.leagueoflegends.com/en-us/2017/10/dev-leagues-vfx-style-guide/)). It follows that a trait colour (Aura value, Background) must not tint the ability itself, or two heroes with the same effect would look different. Trait colour belongs on secondary elements: the cast flourish, the trail, the hero's ring (INFERRED).

### "Rarity is not power" becomes true only once every delivery has a price

Where rarity carried stats, entry cost and pay-to-win pressure followed. DeFi Kingdoms states that "each level of rarity corresponds with bonuses to the base stat levels of your Hero" ([DFK docs](https://docs.defikingdoms.com/gameplay/heroes/rarity)). Galaxy Fight Club ties weapon strength to rarity and hides battle stats because "we do not want this to turn into a pay-to-win game" ([GFC whitepaper, Weapons](https://whitepaper.galaxyfightclub.com/economy/weapons), [Fighters](https://whitepaper.galaxyfightclub.com/economy/fighters)). Axie later added three free starter Axies ([Axie Infinity](https://blog.axieinfinity.com/p/origin)). The cleanest positive pattern is Aavegotchi's two-sided trait map, where each end of a trait gives one benefit and one cost ([Aavegotchi Wiki](https://wiki.aavegotchi.com/en/trait-mappings)). Only that third pattern, rarity as extremity on a trade-off, is compatible with "rarity is not power" (INFERRED).

The catalog maps Base and Tier to a delivery: Common `direct`, Uncommon `echo`, Rare Skull `return`, Epic Radioactive `persistent-zone`, Legendary 1:1 `inversion` (`content/README.md`). Echo, return and a persistent zone each add expected damage, area or uptime, so the mapping is not power-neutral by construction (INFERRED). The standard tool is a cost curve: "We want the costs and benefits to be equal, numerically", every effect converted into one resource, and "It's better to make an object too weak than to make it too strong" ([Schreiber, Game Balance Concepts, Level 3](https://gamebalanceconcepts.wordpress.com/2010/07/21/level-3-transitive-mechanics-and-cost-curves/)). Sidegrade then becomes a checkable property: equal budget, different spend (INFERRED). Rarity can still ration complexity, as Magic does: "If a common card is complex, it has to have a reason" ([Wizards of the Coast](https://magic.wizards.com/en/news/making-magic/new-world-order-2011-12-05)).

The distribution of the recorded tokens shows where the budget rule matters most.

| Axis | Levels and counts among the 552 recorded Genesis (VERIFIED, local files) |
|---|---|
| Class (Body) | brute 34%, ranger 38%, controller 28% |
| Shape (Eyes) | nova 157, wave 144, flash 101, beam 82, puddle 68 |
| Effect (Aura) | raw 281, burn 68, chill 50, hex 47, haste 27, rally 25, mend 17, purify 17, veil 15, toxin 5 |
| Tier | Common 356, Uncommon 170, Rare 16, Epic 6, Legendary 4 |

Sources: `content/README.md`, `research/evidence/genesis-catalog-coverage-2026-10-09.json`, `research/evidence/genesis-trait-census.json`. Two consequences follow by arithmetic. **Raw (no Aura) is 281 of 552, about half of the recorded collection**, so "no effect" must be a full kit with its whole budget spent on raw numbers, presented to holders as a specialist and not as a missing feature. And if delivery follows Tier as the catalog states, direct and echo cover 526 of 552 recorded tokens, while return, persistent-zone and inversion together concern 26.

For measurement, Riot is the only studio found that publishes thresholds: in average play a champion is overpowered above 54.5% win rate, sliding to 52.5% at high ban rates, and underpowered below 49%; a 2020 update tightened the upper bands by 0.5% ([Riot, 2019](https://www.leagueoflegends.com/en-us/news/dev/dev-champion-balance-framework/), [Riot, 2020](https://www.leagueoflegends.com/en-us/news/dev/dev-balance-framework-update/)). The hero note computes that 600 theoretical kits give 179,700 one-versus-one pairings and 35,820,200 three-hero teams per side, so exhaustive testing is not practical, while testing the axis levels is (INFERRED arithmetic). The literature supports a concrete loop for a deterministic simulation with bots: measure each level by restricting it ([Jaffe et al., 2012](https://doi.org/10.1609/aiide.v8i1.12513)), rate kits from a win matrix with a strength rating plus a counter table ([Lin et al., 2024](https://arxiv.org/abs/2408.17180)), correct for near-duplicate kits with Nash averaging ([Balduzzi et al., 2018](https://arxiv.org/abs/1806.02643)), and search numbers toward a target counter graph drawn by the designer ([Hernandez et al., 2020](https://arxiv.org/abs/2006.04419)). Two cautions recur: bots are not players ([Pfau et al., 2020](https://doi.org/10.1109/CoG47356.2020.9231958)), and a balanced number is not the same as a kit players perceive as good ([Rupp et al., 2024](https://arxiv.org/abs/2407.11396)). **PROPOSAL:** tune only the axis levels, never one NFT, so a holder's kit identity stays fixed while numbers change in dated patch notes; publish the full trait-to-kit table and all numbers.

### The real image on a shared body is the only route that shows every Genesis in two weeks

No precedent was found of a PFP collection whose playable avatars were derived automatically per token from the 2D image (UNKNOWN after search). The documented cases are 3D modelled by hand per trait, such as Forgotten Runes, whose low-poly rigged models were first commissioned and then made in-house ([Wizzypedia](https://wizzypedia.forgottenrunes.com/3D_Models)), avatars made by a studio per partner collection, or 2D heads on shared animated bodies (Worldwide Webb, INFERRED from a search summary). The precedents that kept per-token identity in 3D were the work of studios over months, not of one artist in two weeks (INFERRED). No Monad game doing this was found either.

Three facts about the art decide the ranking (VERIFIED on 15 downloaded images and on the census). The Genesis images are 1000 by 1000 WebP head-and-shoulders busts with a flat background, not full bodies. The four Legendary 1:1 tokens in the sample carry only `Base: 1:1` and `Tier: Legendary`, with no Head, Body, Eyes or Skin value. A faithful modular 3D set would need about 50 meshes and 150 texture variants for the 552 recorded tokens alone (INFERRED estimate). The Genesis layer files were not found anywhere public; the layers served by the chog.xyz PFP maker are a different, simpler art set ([PFP maker](https://www.chog.xyz/maker?tab=pfp)).

| Rank | Option | Effort (trait note's estimate) | How recognisable the holder's NFT stays | Cost on the low-end phone | Main risk |
|---|---|---|---|---|---|
| 1 | C. Hybrid: shared rigid Chog body with one atlas material, real image as portrait disc and face plate, Skin and Background tint, aura effect | About 6 to 9 artist days, plus the image pipeline | High: exact art for every token, 1:1s included | About 5 draw calls for the body plus 1 or 2 quads, no skinning | Image hosting and the licence answer; a square bust on a round head needs one visual test |
| 2 | B. Standee: one quad with the image, procedural motion, blob shadow, outline | 1 to 3 days, mostly engineering and shader work | Highest for the art; no body language, no facing | 1 quad per hero | Can read as low effort unless clearly stylised |
| 3 | A. Modular 3D, reduced: Chog base plus a few part families and palettes, no image | About 8 to 12 days; full fidelity does not fit | Medium to low: "a red Chog with a cap"; 1:1 tokens not representable | Good with one atlas material and rigid parts | Schedule; unknown values in the unrecorded 72% of metadata |
| 4 | D. Generated 3D per token | Little modelling, but checks for 1,969 unique assets; about 500 to 1,400 USD | Unknown: never tested on bust-only flat cartoon art | Unique mesh and texture per hero, CPU skinning, per-match downloads | Untested quality, cost, hosting, licence of inputs |

Every cell of this table is the trait note's estimate unless it repeats a verified fact. Option C ranks first because it can ship as Option B on its first milestone and improve from there, so it has no worse failure mode than B (INFERRED). The legibility check supports it: on a desktop sheet, headwear, eyewear and background colour still distinguish ten tokens at 48 to 64 pixels; the same check on the phone at game scale has not been run (UNKNOWN).

The performance premise needs correcting. The six heroes of the current prototype contain zero skinned meshes; they are rigid part hierarchies with **18 to 21 material slots and 5 to 6 shadow-casting renderers each** (VERIFIED by reading the prefabs under `game/Assets/QuantumUser/Resources/`). That is roughly 110 to 125 draw calls for six heroes before batching (INFERRED), against Arm's guideline "For OpenGL ES, aim for fewer than 500 draw calls per frame" ([Arm GPU Best Practices](https://documentation-service.arm.com/static/67a62b17091bfc3e0a947695)). Unity states that on the Web "the CPU side dispatch of WebGL operations is slower than in native OpenGL" ([Unity manual](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-performance.html)) and that "Batching doesn't support Skinned Mesh Renderers" ([Unity manual](https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching.html)). Whether the material slots explain the 17 to 18 ms of non-simulation work measured on the phone is not established: that time has not been broken down (UNKNOWN, `docs/STATUS.md`). For this target, rigid parts with one shared atlas material beat a skinned character (INFERRED).

Images cannot come from public gateways at match time. The IPFS documentation says a public gateway "is not intended to be part of your critical path or production infrastructure" ([IPFS docs](https://docs.ipfs.tech/concepts/public-utilities/)), and two probes from the project machine met HTTP 429 and 403 on most gateways (VERIFIED). Unity's loader "Loads PNG, JPG or EXR image byte array into a texture"; WebP is not listed ([Unity scripting API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ImageConversion.LoadImage.html)). The robust path is a pre-resized copy on the game's own host, loaded only for the 6 to 10 heroes of a match (INFERRED, not tested in a build).

| Tile size | WebP q75, each | WebP q75, all 1,969 | JPEG q80, all 1,969 | Uncompressed in memory, each |
|---|---|---|---|---|
| 128 px | 3.8 KB | 7.2 MB | 10.7 MB | 64 KB |
| 256 px | 9.1 KB | 17.6 MB | 31.1 MB | 256 KB |
| 512 px | 32.9 KB | 63.3 MB | 108.4 MB | 1,024 KB |

These sizes were measured on 10 images and multiplied by 1,969 (VERIFIED as a measurement, trait note). Files are named by metadata id, which differs from the token id: token 1 is "CHOG #1462" (VERIFIED on chain, Monad note).

One point blocks all of this: rights. No public statement of holder rights or art licence for Chog Genesis was found. The only rights language on the site is "© 2026 Chog. All rights reserved." ([chog.xyz](https://www.chog.xyz/)), and the hackathon page sets no rule about art use beyond "The Chog NFT must be essential." and declaring pre-existing work ([chogathon.chog.xyz](https://chogathon.chog.xyz/)). A game made for the collection's own hackathon will plausibly be welcome to display the art, but plausibility is not permission (INFERRED). Until an answer arrives, the lowest-risk posture is to display each image unmodified apart from scaling, only for tokens present in a match, and to send no image to a third-party generator (INFERRED).

## One static host, one serverless backend and one small contract carry everything but gameplay

**Direct answer.** Gameplay networking stays on Photon Quantum as decided. Everything else fits on a static host, a serverless backend with a small SQL database, one shop contract and direct RPC reads. No indexer is mandatory at the scale of 1,969 tokens. The assembled flow is a PROPOSAL: no primary reference implementation of a wallet sign-in feeding Photon custom authentication was found.

### The earlier game's wallet code is reusable, its trust model is not

The earlier game (chogtanks) connects with Reown AppKit Unity 1.4.1 on Monad testnet, signs the constant string "Hello Choggie! (Request #1)", and sends that signature nowhere; NFT gating only toggles buttons on the client (VERIFIED in `Assets/Scripts/Connect.cs` and `NFTVerification.cs` of that project). A modified client can therefore claim any address (INFERRED).

| Reusable after changing constants | To rewrite |
|---|---|
| `AppKitConfig` plus a custom `Chain` object (chain reference "143", an RPC URL with no secret in it) | Message format: EIP-4361 with nonce, domain, chain id and expiry |
| `OpenModal` then `SignMessageAsync` sequence | Server-side verification of the signature |
| `ReadContractAsync` with a human-readable ABI | Identity: the wallet comes from the verified session, not from a request body |
| Header-secret check on Photon webhooks, per-route rate limiting | NFT listing: Genesis is not enumerable, so `tokenOfOwnerByIndex` does not exist |
| Compression header pattern for the hosted build | Webhook routes and payloads (Quantum 3), storage (no JSON file, no in-memory maps), wallet list |

The SDK moved on. The latest Unity AppKit is 1.7.1 (3 September 2026) with a minimum Unity version of 2023.1; no release note, doc page or issue mentions Unity 6000.6, and the game is on 6000.6.4f1 (VERIFIED, [reown-dotnet releases](https://api.github.com/repos/reown-com/reown-dotnet/releases?per_page=12)). Whether it compiles and runs there is UNKNOWN and needs a build test. On WebGL the package is a thin bridge that loads the web AppKit from a CDN at runtime ([AppKit.jslib](https://raw.githubusercontent.com/reown-com/reown-dotnet/main/src/Reown.AppKit.Unity/Plugins/AppKit.jslib)). Three documented limits shape the design. First, "One-Click Auth and Sign-In With Ethereum are currently not supported on WebGL" ([Reown docs](https://docs.reown.com/appkit/unity/core/siwe.md)), so sign-in must be built with `SignMessageAsync` plus a backend. Second, Monad is not a predefined chain in the Unity SDK and Reown's RPC proxy lists only Monad testnet ([Reown docs](https://docs.reown.com/cloud/blockchain-api.md)), so a custom chain with its own RPC URL is required, and email and social login should be switched off because Reown's embedded wallets are "limited to the chains the secure site and Blockchain API support" ([Reown docs](https://docs.reown.com/appkit/networks/overview.md)) (the conclusion is INFERRED). Third, Phantom's docs now say "Monad support has been deprecated." ([Phantom docs](https://docs.phantom.com/ethereum-monad-testnet-base-and-polygon/getting-started.md)), so the earlier game's wallet list cannot be copied.

### Sign in once, prove ownership at admission, write the career exactly once

The proposed flow has six steps (INFERRED design, each block VERIFIED separately). The backend issues a single-use nonce. Unity builds the EIP-4361 text with the page domain, chain id 143, nonce and expiry, and signs it ([EIP-4361](https://eips.ethereum.org/EIPS/eip-4361)). The backend verifies with viem's `verifySiweMessage` on a Monad client, which covers plain accounts and smart-contract wallets through ERC-1271 and ERC-6492 ([viem](https://raw.githubusercontent.com/wevm/viem/main/site/pages/docs/siwe/actions/verifySiweMessage.md)); the ERC-6492 validator has code on Monad (VERIFIED on chain). The backend then issues a short session token. Unity passes it to Photon custom authentication, whose endpoint answers with `UserId` set to the lowercase wallet address and a secret `AuthCookie` that every Quantum webhook carries (VERIFIED in the local note `research/research_notes/Audit architecture Photon MOBA Genesis/quantum3_security_webhooks_replays.md`). Each webhook reads the wallet from `AuthCookie`, never from a client field.

Ownership needs no indexer. The Genesis contract is not `ERC721Enumerable`, but it ships a view `tokensOfOwner(address)` that scans ids 1 to 1,969 (VERIFIED, `research/evidence/genesis-ChogMint.sol`). Its measured cost ranges from 38,820 gas for an address holding nothing to 16,546,500 gas for the holder of token 1969 (VERIFIED by `eth_estimateGas`, two notes agree). **PROPOSAL:** call `tokensOfOwner` once per login for the hero list, then check `ownerOf(tokenId)` at the `finalized` block tag inside the `AddPlayer` webhook. A whole owner table is one Multicall3 call: 1,969 `ownerOf` calls returned in 0.64 s and showed 1,011 distinct owners (VERIFIED, Monad note). Because the career is keyed by token id and not by wallet, a sale during a match cannot move value between players (INFERRED). Three practical rules follow: verify at admission and let the match finish, hold a lease so one token plays one match at a time, and re-read `ownerOf` before granting anything that goes to a wallet.

The career belongs in an ordinary SQL database keyed by `(chain_id, collection, token_id)` with the canonical token id, written exactly once per match using the Photon game id as idempotency key (INFERRED design). The closest precedent keeps the same split: Axie's "AXP ... is an off-chain token" while "Levels are on-chain numbers that are mapped to each Axie", synced by a player-paid transaction every 10 levels ([Axie help center](https://support.axieinfinity.com/api/v2/help_center/en-us/articles/16669621368347.json)). The one fully on-chain game found shut that version down: "The demand for the fully on-chain version of Pirate Nation simply isn't there to sustain operation indefinitely." ([Decrypt](https://decrypt.co/335879/pirate-nation-ethereum-rpg-shutting-down-crypto-gaming-graveyard-grows)). One limit must stay visible: Photon documents that the result JSON "should be handled as untrusted client input" (local quantum note), so career credits stay provisional until a re-simulation worker exists.

### The component list, with what each one still has to prove

| # | Component | Simplest option (PROPOSAL from the web3 note) | Free-tier figure or measure (VERIFIED on the pricing pages) | Open point |
|---|---|---|---|---|
| 1 | Client | Unity 6000.6 WebGL, AppKit Unity 1.7.1, custom chain `eip155:143`, email and social login off | Free SDK | Compatibility with 6000.6 and `.wasm` growth: UNKNOWN |
| 2 | Static hosting | Cloudflare Pages, Unity-compressed files, `_headers` file, custom service worker | 25 MiB per file, 20,000 files | Whether Pages keeps a `Content-Encoding` header on pre-compressed files: UNKNOWN |
| 3 | Auth service | One Worker: nonce, verify, Photon custom authentication endpoint | 100,000 requests per day, 10 ms CPU per call | CPU time of signature verification: not measured |
| 4 | Nonce and lease store | D1 table or SQLite Durable Object | D1: 100,000 rows written per day | KV is unsuitable: 1,000 writes per day |
| 5 | Ownership | `ownerOf` at `finalized` in `/player/add`; `tokensOfOwner` once per login | Public RPC: 25 requests per second on `rpc.monad.xyz` | Worst-case `tokensOfOwner` uses the limited high-gas pool |
| 6 | Career database | D1, or Neon Postgres if a non-Cloudflare worker must share it | D1: 5 GB; Neon free: compute suspends after 5 minutes | D1 batch atomicity: not read |
| 7 | Shop contract | One non-upgradeable contract: SKU prices, `buy`, `buyWithPermit`, `owned[tokenId][skuId]`, `Purchased` event | Monad's guide puts an ERC-721 deployment at roughly 0.25 MON | No transaction was sent in any note |
| 8 | Purchase indexer | Optional: entitlements are read from contract state at admission | n/a | n/a |
| 9 | Photon webhooks | Same Worker, each route checking the secret header and reading `AuthCookie` | About 90 requests per match in the note's estimate (10 players and 15 minutes assumed, not measured) | Roughly 1,100 matches per day inside the free quota (arithmetic) |
| 10 | Re-simulation worker | A .NET process on a container or small VM | Not researched | Career stays provisional until it exists |

Sources for the figures: [Cloudflare Pages limits](https://developers.cloudflare.com/pages/platform/limits/), [Cloudflare Workers pricing](https://developers.cloudflare.com/workers/platform/pricing/), [Neon pricing](https://neon.com/pricing), [Monad network information](https://docs.monad.xyz/developer-essentials/network-information/index.md), [Monad, Build with NFTs](https://docs.monad.xyz/guides/build-with-nfts.md).

### On mobile, the dependable wallet path is the wallet's own browser

Unity's stock PWA template ships a cache-first service worker that stores every response and never cleans up (VERIFIED by reading the template files). Used as shipped, it would serve stale sign-in and career responses (INFERRED), so it must be replaced by a worker that caches only same-origin hashed build files. On iOS, an installed web app cannot be the return target of a wallet link: "Only a push message can open an installed PWA." ([firt.dev, table of 2023](https://firt.dev/notes/pwa-ios/)). The dependable mobile path is therefore a button that opens the game inside the wallet's in-app browser, where a provider is injected (INFERRED; MetaMask's dapp deeplink is documented, read through a search index only). One known trap deserves a test: a reporter linked a broken MetaMask connection on mobile browsers to the viewport setting `user-scalable=no` ([reown-dotnet issue 178](https://github.com/reown-com/reown-dotnet/issues/178)), and Unity's own templates set exactly that value (VERIFIED in the local template files).

## Binding exists in two working families, and only keyed state survives a sale intact

**Direct answer.** Yes, an NFT can be bound to another NFT so that it follows it on every transfer, and this works on Chog Genesis without touching the collection. What does not exist is any marketplace that shows the bound thing to a buyer on Monad, and with one of the two families the seller can remove it before the sale.

### The standards that need the parent's cooperation are excluded by measurement

Chog Genesis answers `false` to the interface ids of ERC-4907 (rental role), ERC-5192 (soulbound), ERC-7401 and ERC-6059 (nesting) and ERC-6220 (equippable parts); it answers `true` to ERC-721 and ERC-2981 and to none of the binding-related interfaces tested (VERIFIED by `supportsInterface` calls on [rpc.monad.xyz](https://rpc.monad.xyz)). ERC-7401 puts `addChild` on the parent contract, and RMRK's implementation reverts when the destination does not implement it ([erc-7401.md](https://github.com/ethereum/ERCs/blob/master/ERCS/erc-7401.md), [RMRKNestable.sol](https://github.com/rmrk-team/evm/blob/master/contracts/RMRK/nestable/RMRKNestable.sol)). The only way to use those standards would be to wrap each Chog into a new token held in escrow, which removes the Chog from the holder's wallet and breaks every integration that reads Chog ownership (INFERRED). Attestations are no shortcut either: EAS does not list Monad among its deployments, and the two addresses probed on chain 143 have no code (VERIFIED, [eas-contracts README](https://github.com/ethereum-attestation-service/eas-contracts/blob/master/README.md)).

### ERC-6551 works today on Monad, with a known hole at the moment of sale

The ERC-6551 registry and the Tokenbound V3 account contracts have code on Monad mainnet with the same size and hash as on Ethereum (VERIFIED, binding note), and Monad documents the deployment in its own guide ([Monad, ERC-6551 guide](https://docs.monad.xyz/guides/erc-6551)). The standard needs nothing from the parent: "it does not extend the ERC-721 standard" ([erc-6551.md](https://github.com/ethereum/ERCs/blob/master/ERCS/erc-6551.md)). Each Genesis already has a computable account address. Creating the account is estimated at 103,380 gas, or 171,403 with initialisation, about 0.01 to 0.02 MON at the measured gas price (gas VERIFIED by simulation, cost by arithmetic; nothing was sent). Exactly one such account exists for a Genesis token: token 1659, created by its holder on 6 October 2026. It is one of only 3 accounts the registry has created in the whole history of Monad mainnet up to block 111,812,000 (VERIFIED by a full event scan in the Monad note, through a single data provider, SQD Portal, not cross-checked against a second source). Whether that holder is connected to this project is UNKNOWN.

Three weaknesses come with it. The standard is still in Review, its text unchanged since May 2024, and the reference contracts have not moved since early 2024 (VERIFIED). Whoever `ownerOf` returns controls the account, and an OpenSea listing leaves the Chog in the seller's wallet until the order fills, so the seller can empty the account just before the sale (INFERRED from the standard's own fraud scenario). The ERC assigns the mitigations to marketplaces and concludes: "Preventing fraud is outside the scope of this proposal." ([erc-6551.md](https://github.com/ethereum/ERCs/blob/master/ERCS/erc-6551.md)). And no documentation of native display of account contents was found on OpenSea, the one marketplace verified for the collection ([docs.opensea.io](https://docs.opensea.io/llms.txt)).

### Keyed state is the oldest pattern and the only one a seller cannot strip

A store that keeps `state[tokenId]` and checks `ownerOf` at use time binds anything to a collection one does not control, on chain or off chain (INFERRED; the project's own evidence file already records that gameplay state "can be kept externally under chain ID, collection address and canonical token ID"). The state follows the token because it was never attached to a wallet. Its two limits are that nothing shows it to a buyer unless the team builds that surface, and that any value claimable once per token is exposed to borrowed ownership: the ApeCoin airdrop was drained through flash-loaned Bored Apes because it "only considered the spot state" of holdings ([BlockSec](https://blocksec.com/blog/the-short-analysis-of-the-flashloan-attack-to-the-ape-air-drop), INFERRED from a search summary). Cumulative progression is safe against that; one-shot claims are not (INFERRED).

| Mechanism | Works on immutable Genesis | On-chain footprint | Can the seller strip it before a sale | Shown on a marketplace | Complexity in two weeks (binding note's assessment) |
|---|---|---|---|---|---|
| Off-chain state keyed by (143, collection, tokenId) | Yes | None (one RPC read) | No | No | Lowest |
| Companion contract keyed by tokenId | Yes | One contract; about 50,000 to 80,000 gas per write (estimated, not measured) | No | No | Low |
| Hybrid: off-chain accrual, on-chain checkpoint at milestones (Axie model) | Yes | Same contract, fewer writes | No | No | Low to medium |
| ERC-6551 account holding transferable items | Yes | Item contracts; account deployment optional | Yes, until the sale settles | No evidence of display | Medium |
| ERC-6551 address holding non-transferable items | Yes | Item contract; no account deployment needed | No | No | Medium |
| Child-side binding in the game's item contract (ERC-998 bottom-up, ERC-5114, ERC-5700, ERC-7929 style) | Yes | One item contract with a parent pointer | Not for a badge or mirror; ERC-5700 lets the owner unbind | No; a mirror emits no `Transfer` when the Chog is sold | Medium; no adoption or tooling found |
| ERC-7656 service, EAS attestation | In principle | The registry or EAS must first be self-deployed on Monad | n/a | No | Medium to high |
| ERC-7401, ERC-6059, ERC-6220, ERC-998 top-down, ERC-4907 | No | n/a | n/a | n/a | Excluded |
| Wrapper NFT (escrow the Chog) | Technically yes | New collection plus escrow | n/a | The Chog shows the escrow as owner | High |

For the specific request, "an NFT that follows another one everywhere", three readings give three answers.

| What "follows" should mean | Mechanism | Follows on sale | Seller can remove it first | Visible on OpenSea |
|---|---|---|---|---|
| A companion that can also be traded alone | Transferable NFT held by the Chog's ERC-6551 account | Yes | Yes | No evidence of display |
| A companion that never leaves its hero | Non-transferable item minted to the Chog's ERC-6551 address, or a parent pointer in the companion contract | Yes | No | No |
| A companion in the holder's wallet | Ordinary second collection | No | n/a | Yes |

The mechanisms combine without migration. Keyed state can be the source of truth today, and items can later be minted to each Chog's ERC-6551 address, because that address is fixed by registry, implementation, salt, chain id, collection and token id (INFERRED).

### What kept binding under control elsewhere: freeze, show, or make it non-extractable

The projects that made binding work in production control their own parent contract. Aavegotchi locks a listed token on chain ("NFTs that are listed are immediately locked") so it cannot be re-equipped during the sale ([ERC721MarketplaceFacet.sol](https://github.com/aavegotchi/aavegotchi-contracts/blob/master/contracts/Aavegotchi/facets/ERC721MarketplaceFacet.sol)). Mocaverse shows the bound value on the listing: "You can see XP points for each listed Moca." ([Rarible blog](https://app.rarible.com/blog/mocaverse/)). The game cannot lock a Genesis, so its options are to show the state and to make bound value non-extractable (INFERRED). No case was found of a game binding progression to a PFP collection it does not control with published results (UNKNOWN).

Binding also turns play into resale value, and the genre's history warns about that. Blizzard closed the Diablo III auction house because it "ultimately undermines Diablo's core game play" ([Blizzard](https://news.blizzard.com/en-us/article/10974978/iii)). Riot forbids account transfers in its MOBA (INFERRED from a search summary of [Riot support](https://support-wildrift.riotgames.com/hc/en-us/articles/360053284953-Account-Sharing)). The binding note draws the line this way (INFERRED): bind identity progression (history, titles, chosen specialisation) to the token, keep matchmaking rating with the player account, and never sell bound power.

## Monad adds no NFT standard of its own; its limits are operational

**Direct answer.** Monad's documentation states: "Monad is fully EVM-compatible, so the NFT stack you already know works unchanged." ([Monad, Build with NFTs](https://docs.monad.xyz/guides/build-with-nfts)). ERC-721, ERC-1155, ERC-721A, EIP-2981 and ERC-6551 are listed as working with no changes. There is no Monad-specific NFT standard, and no gaming or NFT grant program was found (VERIFIED on the pages read; absence is not proof).

| Topic | Fact (VERIFIED) | Consequence for the game (INFERRED) |
|---|---|---|
| EVM level | All opcodes and precompiles as of Fusaka ([Monad summary](https://docs.monad.xyz/developer-essentials/summary)); contract size limit 128 KB ([Monad differences](https://docs.monad.xyz/developer-essentials/differences)) | Standard contracts and libraries apply |
| Gas | "The **gas limit** is what is charged."; a 200,000-gas transaction costs about $0.0005 at the minimum base fee ([Monad gas pricing](https://docs.monad.xyz/developer-essentials/gas-pricing)) | Pass a tight explicit gas limit on every transaction |
| Speed | Blocks every 300 ms; "Blocks are finalized after two blocks (600 ms)" ([Monad summary](https://docs.monad.xyz/developer-essentials/summary)) | Credit a purchase at `finalized`; the wait is under a second |
| Time | `TIMESTAMP` has one-second granularity, so 3 to 4 blocks share a timestamp (same page) | Use block numbers for any on-chain ordering below one second |
| Logs | `eth_getLogs` is capped per endpoint (table below) | No naive log replay through the default RPC |
| Old state | Not every public endpoint serves historical state (measured on [rpc.monad.xyz](https://rpc.monad.xyz) and four other endpoints) | Do not depend on historical `eth_call` |
| EIP-7702 | Supported; a delegated account cannot be debited below 10 MON ([Monad, EIP-7702](https://docs.monad.xyz/developer-essentials/eip-7702)) | Price items in $CHOG, not in MON |
| Standard mint | About 85,000 gas; contract deployment roughly 0.25 MON ([Monad, Build with NFTs](https://docs.monad.xyz/guides/build-with-nfts)) | Cost is not a constraint |

The log limits differ from what the docs say on two endpoints.

| Endpoint | Docs say ([JSON-RPC overview](https://docs.monad.xyz/reference/json-rpc/overview)) | Measured on 9 October 2026 (Monad note) |
|---|---|---|
| `rpc.monad.xyz` (QuickNode) | 100 blocks | 100 accepted, 101 rejected |
| `rpc1.monad.xyz` (Alchemy) | 1,000 blocks and 10,000 logs | 100 accepted, 101 rejected |
| `rpc2.monad.xyz` (Goldsky Edge) | not stated in the notes | 10,001 accepted, 100,001 rejected |
| `rpc3.monad.xyz` (Ankr) | 1,000 blocks | 101 accepted, 1,000 rejected |
| `rpc-mainnet.monadinfra.com` | 100 blocks | 100 accepted, 101 rejected |

The contracts a game would lean on are mostly present (all VERIFIED by `eth_getCode` on [rpc.monad.xyz](https://rpc.monad.xyz)).

| Has code on chain 143 | No code at the usual address |
|---|---|
| ERC-6551 registry, Tokenbound V3 proxy and implementation | delegate.xyz registry v2 |
| ERC-4337 EntryPoint v0.6, v0.7, v0.8 and v0.9 | EAS (two addresses probed) |
| Multicall3, Permit2, CreateX | ERC-7656 registry |
| Seaport 1.5 and 1.6, OpenSea's conduit | ERC-1820 registry |
| ERC-6492 UniversalSigValidator, Safe 1.3.0 | OpenSea OperatorFilterRegistry |

**The Genesis contract is final in everything that matters to a game.** It is a plain, non-upgradeable OpenZeppelin v5 ERC-721 with ERC-2981 and AccessControl, source-verified with an exact match ([Sourcify on MonadVision](https://sourcify-api-monad.blockvision.org/v2/contract/143/0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763?fields=deployment,compilation,creationMatch,runtimeMatch)). Supply is 1,969 with `maxSupply` 1,969, metadata is frozen after reveal, and there is no burn, pause, lock, rental role, operator filter or enumeration (VERIFIED). What can still change is the royalty (5% to the treasury, optional on OpenSea, not enforced on chain) and the role assignments, both in the hands of the deployer account (VERIFIED). Secondary sales are therefore not a revenue source to count on (INFERRED). A listed token stays in the seller's wallet, so "listed" and "playable" are not mutually exclusive (INFERRED).

**$CHOG is a fixed-supply token with permit and no admin.** It is a nad.fun launchpad token: an immutable minimal proxy, 18 decimals, 1,000,000,000 supply, with EIP-2612 `permit`, `burn` and `burnFrom`, and no owner, mint, pause or fee-setter selector (VERIFIED by selector extraction). Its implementation source is verified on MonadVision's Sourcify at a non-exact match level and contains no transfer fee; one sampled wallet-to-wallet transfer moved exactly the stated amount ([Sourcify on MonadVision](https://sourcify-api-monad.blockvision.org/v2/contract/143/0x7f64ccfeb3e3afd7691ea0ef404e947786cefae8?fields=sources,compilation,creationMatch,runtimeMatch)). The on-chain domain separator equals the one recomputed for name "Chog", version "1", chain id 143 (VERIFIED). The `permit` function accepts only an ECDSA signature from the owner, so a contract account must use `approve` or Permit2 instead (INFERRED from the verified source).

**For data, the chain itself is enough, and one hosted API is trustworthy today.** OpenSea's API returned correct owners, revealed names, traits and CDN image URLs for Genesis, with a free key at 600 reads per hour ([OpenSea API keys](https://docs.opensea.io/reference/api-keys.md)). Alchemy's ownership data is correct but its Genesis metadata is stuck on the pre-reveal placeholder (VERIFIED). thirdweb reports Insight and account abstraction as disabled on chain 143, against Monad's own list ([thirdweb chain services](https://api.thirdweb.com/v1/chains/143/services)). SQD Portal served the full Genesis event history without any key (VERIFIED). OpenSea's IPFS mirror answered for a metadata file in the same minute in which public gateways refused (VERIFIED on one file; its terms of use were not checked).

**Gasless and one-click purchases are technically available, and none was exercised.** Pimlico's public bundler endpoint for chain 143 answered with the four EntryPoint addresses, and Alchemy, ZeroDev and Biconomy list Monad mainnet ([Pimlico supported chains](https://docs.pimlico.io/guides/supported-chains)). No UserOperation was sent, and paymaster pricing was not researched (UNKNOWN). At these fees, sponsorship is not needed for price reasons, only to serve holders with zero MON (INFERRED).

**Wallets to target:** MetaMask, Rabby, Backpack, OKX Wallet, Rainbow and Trust Wallet are on Monad's mainnet list with token and NFT auto-detection ([Monad, software wallets](https://docs.monad.xyz/tooling-and-infra/wallets/software-wallets)); their behaviour was not tested on a device. Monad Games ID, used by the earlier game on testnet, should be treated as discontinued: the contract named in its guide has no code on mainnet or on the current testnet, and its site returns 404 (VERIFIED facts, INFERRED conclusion).

## Decisions to take (every recommendation is a PROPOSAL)

Build costs are quoted only where a note gives one. "None in the notes" means no researcher estimated that work; it does not mean the work is small.

### 1. How a Genesis appears on its hero

| | |
|---|---|
| Options | A: modular 3D from traits, reduced. B: 2D standee with the real image. C: shared rigid body plus the real image, tint and aura. D: AI-generated 3D per token. |
| PROPOSAL | C, built so that its first milestone is B: portrait disc and tint on the existing bodies first, the Chog body afterwards if time allows. |
| Why | It shows the exact art of every token, 1:1s included. It costs one quad per hero. It has no worse failure mode than B. |
| Build cost (trait note's estimates) | B: 1 to 3 days. Full C: about 6 to 9 artist days. Reduced A: about 8 to 12 days. D: about 500 to 1,400 USD plus checks on 1,969 unique assets. |
| Flip condition | The Chog team refuses image use: fall back to reduced A, with a bespoke stand-in for each 1:1. The team supplies the layers: a C plus A blend becomes realistic. The single-material test shows no gain: keep the existing bodies and add only the portrait and tint. |
| Fastest test | Six camera-facing quads with real Genesis images at game size, screenshot on the phone (2 hours, estimate). |

### 2. Image supply and art permission

| | |
|---|---|
| Options | Public IPFS gateways at runtime. A marketplace CDN. Pre-resized copies on the game's own host. An atlas of the whole collection in the build. |
| PROPOSAL | Pre-resized 256 px JPEG or PNG copies on the game's own origin, 6 to 10 files fetched per match, after written permission. |
| Why | Gateways are documented as not for production and refused two probes. Unity's loader does not list WebP. The whole collection at 256 px weighs about 18 to 31 MB; a match needs under 3 MB in memory. |
| Build cost | Hosting test: half a day (estimate). Full crawl of 1,969 images: none in the notes. |
| Flip condition | Permission refused: no mirroring, see decision 1. |
| Fastest test | One message to the Chog team covering four items: display and mirror the images, crop or mask them, access to the Genesis layer files, reuse of the PFP maker layers. Then 20 resized files on the game's host, loaded on the phone over mobile data. |

### 3. Role scheme

| | |
|---|---|
| Options | Keep three classes only (current build). Move to 5 to 7 mechanically defined classes. Keep three Body classes and add a second role label derived from the kit. |
| PROPOSAL | The third option, with counter relations published on the hero card. |
| Why | Most shipped schemes use 5 to 7 classes, and two studios split coarser ones. A derived label needs no new character and no remap of the 86 Body values. |
| Build cost | None in the notes. |
| Flip condition | The bot Nash mix over roles puts more than half the weight on one Body class: three classes are degenerate and the second axis must carry real stat differences. |
| Fastest test | Round robin of 3 classes by 5 shapes with effect `raw`. |

### 4. Kit budget: rarity as delivery, and what `raw` is

| | |
|---|---|
| Options | Keep "Base and Tier decide delivery" with an equal budget and a price per delivery. Move delivery to a non-rarity trait and make rarity cosmetic. Let rarity add power. |
| PROPOSAL | The first option, with a measurable gate: win rate grouped by tier stays inside the chosen band. `raw` spends its whole budget on raw numbers and is presented as a specialist. |
| Why | Rarity as power is the documented route to pay-to-win pressure. Echo, return and persistent zone add damage, area or uptime, so they must be paid for in base damage, cooldown or range. `raw` is about half of the recorded tokens. |
| Build cost | None in the notes. |
| Flip condition | Equal-budget tuning cannot bring every delivery tier inside the band in the bot matrix: move delivery off rarity. |
| Fastest test | Restricted-play screening of the deliveries on 3 reference kits. |

### 5. Stun, invisibility and support effects

| | |
|---|---|
| Options | Ordinary effect values on a short cooldown. Capped effects with structural limits. Moved to a charged ultimate. |
| PROPOSAL | Capped: stun at most 1.0 to 1.5 s, slow about 25 to 30%, a per-target diminishing rule (second stun in a fixed window halved, third ignored), short invisibility with reveal rules, support effects as timed visible zones. Write the full interaction table for the effects. |
| Why | Brawl Stars stun values sit at 0.5 to 2 s and trend down; three players can chain control in a 3v3; passive auras are "power without gameplay". |
| Build cost | None in the notes. |
| Flip condition | Stun or invisibility kits show normal win rates but the highest quit or complaint rates: cap further or move them to a charged ultimate. |
| Fastest test | Tag matches by enemy effect and compare early-leave rates. |

### 6. Balance gates and the simulation loop

| | |
|---|---|
| Options | Tune by feel. Bands on human data only. A bot-matrix loop per axis level, fixed bands, then a human check. |
| PROPOSAL | The third option. Flag a level above 54% or below 49% win rate in human play; use a wider band and at least two bot personas for bot matrices; call a level overpowered if any persona crosses the top, underpowered only if all are below the floor. Price blocks bottom-up: shapes alone, then each effect on one reference shape, then each delivery on one reference kit. When unsure, ship weak. |
| Why | Exhaustive testing of kits and teams is not practical; the axis levels are few. Riot's published bands exist to limit subjective judgment. |
| Build cost | None in the notes. It uses the existing deterministic simulation and bots. |
| Flip condition | Playtesters cannot name an opponent's shape and effect after one exchange: the vocabulary is too large or the visuals too similar, so cut effects before adding content. |
| Fastest test | A 10-minute blind identification session with 5 outside players. |

### 7. Who can play

| | |
|---|---|
| Options | Holders only. Anyone plays a real Genesis, and only the verified owner earns career and buys for it. A generic non-NFT baseline hero for non-holders. |
| PROPOSAL (report writer's synthesis, not a note's recommendation) | The second option. The build already lets a player enter as a Genesis declared by token id in the URL. |
| Why | The hero note asks for a baseline unit with full budget, citing Axie's free starters and Galaxy Fight Club's default fighter. The hackathon states "The Chog NFT must be essential." A generic hero answers "take the NFT out, does it still work?" with yes. Holders-only shuts out anyone outside the 1,011 owner addresses. |
| Build cost | None in the notes. |
| Flip condition | The organisers state that judges hold a Genesis, or that only holders should play. |
| Fastest test | Ask the organisers how judges will access the game. |

### 8. Sign-in, ownership check and the mobile wallet path

| | |
|---|---|
| Options | Reuse the earlier game's flow (constant message, client-side gating). EIP-4361 sign-in verified by a backend, session token to Photon custom authentication, `ownerOf` in the `AddPlayer` webhook. A hosted indexer or NFT API as the authority. |
| PROPOSAL | The second option, with email and social login off, the lowercase address as Photon `UserId`, one active match per token id, and an "Open in wallet" link as the dependable mobile path. |
| Why | The earlier flow proves nothing to a server. The SDK does not support SIWE on WebGL. `ownerOf` is one fresh call. Third-party APIs suit a gallery, not admission. |
| Build cost | None in the notes. |
| Flip condition | AppKit Unity 1.7.1 does not build or run on Unity 6000.6.4f1 WebGL. The notes give no fallback. The WebGL side is a thin jslib over the web AppKit (VERIFIED), which points to a direct jslib bridge as the fallback (INFERRED by the report writer, untested). |
| Fastest test | Add the package to a copy of the project, build WebGL, connect MetaMask on the Android phone, once with and once without `user-scalable=no`. |

### 9. Where the career lives, and how a buyer sees it

| | |
|---|---|
| Options | Off-chain state keyed by token. A companion contract. Off-chain accrual with an on-chain checkpoint (Axie model). A periodic Merkle root. ERC-6551 inventories. |
| PROPOSAL | Off-chain SQL keyed by (143, collection, canonical token id), written once per Photon game id, plus a public page per token. An on-chain checkpoint or Merkle root can come later without migration. Matchmaking rating stays with the player account. |
| Why | A seller cannot strip it. Nothing is displayed on OpenSea whatever the mechanism, so the page is what makes the binding real for a buyer. |
| Build cost | Complexity "lowest" (binding note's assessment, not a duration). |
| Flip condition | The Novelty criterion turns out to need proof on chain (how judges weigh this is UNKNOWN): add the checkpoint or the Merkle root. |
| Fastest test | Send the same match result twice and confirm the career changes once (report writer's suggestion). |

### 10. A companion NFT bound to the Genesis

| | |
|---|---|
| Options | None in the window. A non-transferable companion minted to the Chog's ERC-6551 address. A companion contract with a parent pointer. A transferable companion in an ERC-6551 account. Wrapping or nesting (excluded). |
| PROPOSAL | Not before the match, the wallet step and the career page exist. If built, the non-transferable variant. |
| Why | With non-transferable contents the seller-empties problem disappears by construction and the account never needs deploying. Nothing is visible on a marketplace either way. |
| Build cost | Complexity "medium" (binding note's assessment). Gas: 103,380 for `createAccount` if ever needed; about 85,000 for a standard mint (Monad's guidance). |
| Flip condition | Companions must be tradable on their own: use transferable contents, and the game must then supply the buyer protection (a lock, or a wrapper order), since OpenSea offers none. |
| Fastest test | Compute the account address of the owner's own Genesis with the registry's `account` view and simulate a mint to it with `eth_estimateGas`. Then open the one existing Genesis account (token 1659) on OpenSea and tokenbound.org to see what a buyer sees. |

### 11. The $CHOG shop

| | |
|---|---|
| Options | What is sold: power, or equal-budget specialisations and cosmetics. Bound to: the token id, or the buyer's wallet. Payment: `approve` plus `buy`, EIP-2612 `permit` plus `buy`, Permit2, or a plain transfer matched off chain. |
| PROPOSAL | Sell equal-budget specialisations or cosmetics only. Record the entitlement against the token id in contract state. Use `buyWithPermit` for ordinary accounts with `approve` plus `buy` as fallback. Credit at `finalized`. Pass an explicit gas limit. |
| Why | Purchasable bound power is what the genre's incumbents ban. A token-bound purchase is consistent with the career. `permit` exists with a verified domain but accepts ECDSA only. Pricing in $CHOG avoids the 10 MON reserve rule. |
| Build cost | None in the notes. An ERC-20 transfer costs about 65,000 gas, about 0.0065 MON. |
| Flip condition | A dust-amount `permit` fails on mainnet: use `approve` plus `buy`, or Permit2. The owner prefers one sale per player over one sale per token: bind to the wallet. |
| Fastest test | One mainnet `permit` and `transferFrom` with a dust amount. |

### 12. Hosting and database

| | |
|---|---|
| Options | Cloudflare Pages, Workers and D1. Neon Postgres. Supabase. Vercel (the earlier game's pattern). GitHub Pages. itch.io. |
| PROPOSAL | Cloudflare Pages, one Worker and D1; Neon if the .NET re-simulation worker must share the database. |
| Why | Free quotas sit far above 1,969 heroes. Supabase's free tier pauses after a week of inactivity. GitHub Pages disallows commercial use. itch.io runs the game in an iframe on another origin, which conflicts with domain-bound sign-in (INFERRED). |
| Build cost | None in the notes. |
| Flip condition | Pages does not serve pre-compressed files with their header: use Workers static assets or R2 behind a Worker. Signature verification exceeds 10 ms of CPU: take the 5 USD plan. |
| Fastest test | Deploy the current build and run `curl -I` on one compressed file; time one sign-in verification. |

## Build order for 9 to 23 October: each step ends on something playable

Fourteen days remain. The notes estimate art work only. No note estimates engineering time for the wallet, the backend, the shop or the MOBA rules, and no note researched lane, tower and core design. The order below is therefore a PROPOSAL about sequence, not a validated schedule. It applies one rule from the project's own instructions: a complete match and observable NFT behaviours first, content and polish after. Every step before step 5 is playable through the existing token-id entry, so nothing waits on the wallet.

| Step | What a player sees or plays at the end | Why it sits here (evidence) | Estimate found in the notes |
|---|---|---|---|
| 0. Gates and metadata | Any of the 1,969 Genesis ids enters a match (552 today). Six real portraits on the phone, as a screenshot. | The art question has no public answer. Three cheap tests decide later steps. The roster rejects tokens without recorded metadata, and OpenSea's API and IPFS mirror answered where gateways did not. The SDK build test exposes the wallet step's largest unknown on day one. | Tests: 2 hours, half a day, half a day. The rest: none. |
| 1. My Chog on my hero | Each hero and each bot carries its real Genesis image as a portrait disc, with Skin and Background tint. | Option B is the cheapest and the most faithful, and it is Option C's first milestone. The 1:1 tokens appear correctly with no extra work. | 1 to 3 days. |
| 2. Five signatures, `raw` | The player fires the signature shape their Eyes give, with direct and echo delivery. | `raw` is 281 of 552 recorded tokens; direct and echo cover 526 of 552 by Tier. The cost curve prices single shapes with no effect first. | None. |
| 3. A match that can be won | Lane, tower and core: a 3v3 match with a winner. | Project priority. The phone holds about 30 fps with no minion or tower yet, so minions should be rigid or vertex-animated with one shared material (INFERRED). | None. |
| 4. Effects, largest share first | Burn, chill and hex, then haste and rally, then mend, purify, veil and toxin. One hue and one keyword per effect. A hero card shows class, second label, shape, effect, delivery and numbers. | With `raw`, the first three effects cover 446 of 552 recorded tokens (81%); the next two bring it to 498 (90%). "Ship fewer effects first if any block cannot be made visually unambiguous." | None. |
| 5. My wallet, my Genesis | The player connects a wallet, signs in, sees the Genesis they own and enters with one. The server gives career credit only to the verified owner. | Sign-in must be verified on a server; `ownerOf` at admission is one call. | None. |
| 6. A career that follows the token | After a match, the hero's public page shows its matches and progression. The link works for anyone, a buyer included. | Keyed state is the one binding a seller cannot strip. The per-token page is the buyer-facing surface no marketplace provides. Retention is 20% of the score. | None (complexity "lowest"). |
| 7. First balance pass | A published trait-to-kit table, a dated patch note, and the rare deliveries (return, persistent-zone, inversion) in play. | Bot matrix per axis level, then bands. The rare deliveries concern 26 of 552 recorded tokens, so they come last. | None. |
| 8. One $CHOG purchase | The player buys one specialisation for a Genesis with $CHOG and plays it in the next match. | The $CHOG bonus in the judging. Only if steps 1 to 7 hold, and after the dust test. | None. |
| 9. Stretch | A shared 3D Chog body replaces the stand-in bodies, or one non-transferable companion appears on the hero page. | The remainder of Option C; the binding variant with no strip risk. | Body 2 to 3 days, fit to clips 1 to 2 days, aura 1 to 2 days. |

Two cautions on this order. The trait note sized Option C for a dedicated Blender artist over about two weeks; here the same person builds the game, so the 6 to 9 artist days of full Option C compete with every engineering step. And career credits rest on results that Photon itself labels untrusted until a re-simulation worker exists, so the career page should not present them as secured.

## Where the notes disagree, and which claim holds

### Note against note

| Topic | Claim A | Claim B | Better supported |
|---|---|---|---|
| $CHOG source and fees | Web3 note: source unverified on Monadscan, a transfer fee "cannot be excluded", the EIP-712 domain must be recovered. | Monad note: implementation verified on MonadVision's Sourcify (non-exact match), no fee in the source, one sampled transfer exact, domain recomputed as "Chog", "1", 143. | Monad note. It did the checks the web3 note listed as missing. The dust test remains worth doing: no `permit` was executed. |
| `eth_getLogs` ranges | Web3 note, from the docs: 1,000 blocks on `rpc1` and `rpc3`; a backfill is "impractical" (about 1.1 million calls). | Monad note, measured: `rpc1` rejects 101 blocks, `rpc3` rejects 1,000, `rpc2` accepts 10,001. The collection was deployed at block 101,704,036, so a replay is about 1,010 calls on `rpc2`. SQD Portal gave the full history in 382 key-less requests. | Monad note (measurement beats documentation). The web3 note also counted from block 0 instead of the deployment block. |
| Size of a Multicall3 snapshot | Web3 note: about 8,400 gas per token, so chunks of about 950 tokens stay under the 8.1M low-gas threshold. | Monad note: 1,969 `ownerOf` calls in one call cost 22,559,719 gas (about 11,460 per token) and returned in 0.64 s. | Monad note. By its figure, about 700 tokens per chunk stay under 8.1M (arithmetic), or one call uses the high-gas pool. |
| Token-bound accounts for Genesis | Binding note: none of six sampled addresses has code; total count UNKNOWN. | Monad note: exactly one account exists for Genesis (token 1659), and the registry has created 3 accounts in all on Monad mainnet up to block 111,812,000. | Both are true. The Monad note's full scan closes the binding note's gap, with one limit: a single data provider, not cross-checked. In the other direction, the binding note compared bytecode hashes with Ethereum, which the Monad note lists as unchecked. |
| Embedded wallets | Web3 note: switch email and social login off; Reown's proxy lists only Monad testnet. | Monad note: target an embedded wallet "for players without one"; whether Reown's embedded wallets work on chain 143 is UNKNOWN. | Web3 note for the Reown path. The Monad note's embedded option is a different stack (Privy plus Pimlico in Monad's web template) that no note researched for Unity. |
| Monad Games ID | Web3 note: described from the guide as a cross-game identity, used by the earlier game on testnet. | Monad note: the guide's contract has no code on mainnet or on the current testnet; the site returns 404. | Monad note. Treat it as discontinued. The earlier game's server calls a different address that neither note probed. |
| Other marketplaces | Binding note: Poply reported unavailable; OpenSea is the only verifiable marketplace. | Monad note: Monad's docs name OpenSea and Scatter; the Poply report is unverified. | Agreement on OpenSea. Whether Scatter trades Genesis is UNKNOWN. |
| Riot VFX style guide | Trait note: official link dead, archive not reachable, quotes from a secondary summary. | Hero note: read through the Wayback Machine, quotes verified. | Hero note. |

### Notes against the current build or the brief

| Topic | What a note assumes | What the build or brief says | Which holds |
|---|---|---|---|
| Number of deliveries | Hero note: 4 deliveries, hence 200 ability variants, 600 kits, 22 axis levels. | `content/README.md`: 5 deliveries (direct, echo, return, persistent-zone, inversion), with each 1:1 as its own entry; 160 distinct kits among the 552 recorded tokens. | The catalog. The note's arithmetic and its reference to "the 4 deliveries" should be read with 5; the method does not change. |
| Effect names | Hero note: speaks of "stun" and "invisibility" among the ten values. | Catalog ids are `hex` and `veil`, among others; their exact behaviour is not defined in the notes. | Confirm in the catalog that `hex` is the stun and `veil` the invisibility before applying the caps. |
| "Skinned meshes are expensive" | The research brief's premise. | Trait note, reading the prefabs: zero skinned meshes; the cost is 18 to 21 material slots per hero. | The trait note. The draw-call half of the premise fits; the skinning half does not describe today's build. |
| Size of the web build | Web3 note: "current prototype build" of 12.7 MB gzip, largest file 9.4 MB. | That is the older Fusion prototype in `game-fusion/`. `docs/STATUS.md` records the active Quantum network build at 22,684,934 bytes. | STATUS. Every file is still under 25 MiB by arithmetic, but the margin is smaller, per-file sizes are not in the notes, and the wallet packages will add to the `.wasm`. |
| Artist capacity | Trait note: one Blender artist for about two weeks. | Brief: one developer plus an AI agent; the owner also does the Blender work. | The brief. Art days and engineering days come out of the same 14 days. |
| Wallet list | Earlier game: Phantom among the custom wallets. | Phantom's docs: Monad support deprecated. | Phantom's docs. |

### Tensions the owner must arbitrate

| Topic | One side | Other side |
|---|---|---|
| Free baseline unit | Hero note: guarantee a free or baseline unit with full budget, so that owning an NFT changes variety and cosmetics, not match odds. | Hackathon rule: "The Chog NFT must be essential." See decision 7. |
| What a purchase is bound to | Web3 note: bind the entitlement to the token id, so it follows the NFT, consistent with the career. | Binding note: token or wallet is an open product decision with a direct revenue effect (one sale per token against one sale per player), and the buyer of a Chog gets what a previous holder paid for. |
| How much to put on chain | Binding and web3 notes: off-chain keyed state is the lowest-cost binding and has a live precedent. | The scored axis is Novelty at 30%, "a new way to use or interact with NFTs". Whether an off-chain career reads as novel to judges is UNKNOWN. |

## Open unknowns that can move a decision

| Unknown | Decision it affects | Cheapest way to resolve it |
|---|---|---|
| Chog's position on displaying, mirroring, cropping the art; access to the Genesis layers | 1, 2 | One message to the Chog team today. |
| Whether AppKit Unity 1.7.1 builds and runs on Unity 6000.6.4f1 WebGL, and how much the `.wasm` grows | 8, 12 | One A/B build of a copy of the project; connect on the phone. |
| Traits of the 1,417 Genesis whose metadata is not recorded | 3, 4, step 0 | Fetch the remaining JSON files through OpenSea's API or its IPFS mirror, then run the catalog's `coverage` command. |
| Whether fewer material slots reduce the 17 to 18 ms of non-simulation time | 1 | One build with six heroes sharing a single material, same phone, same overlay (half a day, estimate). |
| Whether a portrait reads at game scale on the phone | 1 | Six quads with real images, screenshot (2 hours, estimate). |
| Whether images arrive reliably from the game's host | 2 | 20 resized files, 10 loaded at match start over mobile data (half a day, estimate). |
| How judges access the game and how they read "essential" | 7, 9 | Ask the organisers. No past edition or judge statement was found. |
| Whether $CHOG `permit` behaves as the verified source says in a real transaction | 11 | One mainnet `permit` and `transferFrom` with a dust amount. |
| Whether `hex` and `veil` are the stun and the invisibility | 5 | Read the effect definitions in `content/hero-catalog.prototype.json`. |
| Whether the bots give usable balance signals | 4, 6 | At least two bot personas in the matrix, then the 10-minute blind session with 5 outside players. |
| MetaMask connection from a mobile browser, with and without `user-scalable=no` | 8 | Two connection attempts on the Android phone. |
| What a buyer sees of a token-bound account on OpenSea or in a wallet | 10 | Look at the one existing Genesis account (token 1659) in a logged-in browser session. |
| Whether more Genesis accounts were created after block 111,812,000, and who created the one for token 1659 | 10 | Re-run the registry event scan from that block; the scan used one provider and was not cross-checked. |
| Whether Cloudflare Pages keeps `Content-Encoding` on pre-compressed files; Worker CPU time for verification | 12 | `curl -I` on a deployed build; time one verification. |
| Real gas of a companion-contract write | 9, 10 | Deploy a test contract and estimate one write. |
| Licences of the five animation clips and of the effect library copied from the Quantum sample | 1, submission | Read their licence terms; the hackathon asks to declare pre-existing work. |
| Behaviour on iOS Safari | 8 | Not measured by the project and not researched; one test on an iPhone. |
| Paths that need two wallets or two tokens (transfer during a session, lease, multi-token list) | 8, 9 | The owner holds one Genesis: simulate with `eth_call`, or move that one token between two of the owner's wallets. |

## Conclusion

The three parts of the differentiator converge on a single object the project does not have yet: a public card per Genesis. The balance evidence asks for a place where the role, the kit and every number are published. The visual evidence says the holder recognises the NFT by its real image, not by a 3D approximation. The binding evidence says no marketplace on Monad shows what follows a token, so the game must. One page per token answers all three, and it is also the plainest demonstration that the NFT is essential: take the Genesis away and the page, the kit and the career have nothing to attach to.

The second finding is about where ambition belongs. On each axis the cheapest option is also the best evidenced: the image over modelling, keyed state over an account, direct reads over an indexer, a signed message over an embedded wallet. None of these choices closes a door, because the richer options attach to deterministic keys that exist already: each Chog's ERC-6551 address is computable today, and a keyed career can gain an on-chain checkpoint without migration. Depth then lies in making the trait system priced, published and measurably fair. The one PFP brawler with trait-derived fighters that the notes could verify, Galaxy Fight Club, chose the opposite and hid its stats.
