# Trait to visual pipeline: turning 1,969 Chog Genesis NFTs into recognisable playable heroes (Unity 6 WebGL, built-in pipeline, top-down 3D, low-end mobile)

Research date: 2026-10-09 (05:41 UTC at the last probe). Scope: one Blender artist, about two weeks, Helio G85 / Mali-G52 reference phone.

Tag legend used on every claim:
- **VERIFIED**: the primary page or file was opened in this session, or the value was measured directly in this session (method stated).
- **INFERRED**: reasoning from verified facts, an estimate by the researcher, or a claim seen only in a search-result summary or secondary press that could not be opened directly (marked "snippet" or "secondary").
- **UNKNOWN**: looked for and not found, or not measurable here. "Not found" is written explicitly.

Local evidence is cited by absolute path. Measurements made in this session used throwaway scripts in the session scratchpad (`/private/tmp/claude-501/-Users-red-g-CascadeProjects-chogathon/899443d7-e622-496c-bd95-9d79f7445321/scratchpad/`), which is not part of the repository; every number needed to reproduce them is written below.

Three findings contest premises of the brief and are developed in the sections below:
1. The six heroes of the current prototype contain **zero skinned meshes**. They are rigid part hierarchies with 18 to 21 material slots each. The "skinned meshes are expensive" premise does not describe what is on screen today.
2. No precedent was found of a PFP collection whose playable avatars were derived **automatically per token** from the 2D image. The documented cases are hand-built 3D per trait, hand-built per collection, or 2D heads on shared bodies.
3. The Genesis images are **head-and-shoulders busts**, not full bodies, and four of the 552 recorded tokens (the 1:1 Legendaries) carry **no trait data at all**. Any purely trait-driven 3D approach cannot represent them; only an image-based approach can.

## Key question 1: Precedents. Which PFP collections shipped playable avatars derived from 2D traits, and how

### Takeaway
Every documented precedent used manual 3D authoring (per trait or per collection) or 2D heads on shared animated bodies; none was found that used AI image-to-3D, and none was found in the Monad ecosystem. The closest structural match to Chog (bust-only PFP) is Worldwide Webb, which fitted PFP heads onto shared animated bodies.

### Cited Findings

Full 3D rebuilt per trait, by hand:
- VERIFIED. Forgotten Runes released 3D models for its 10k Wizards on August 2, 2023, then Souls (October 25, 2023) and Warriors (October 23, 2024). The models are "fully-rigged with bones for animation", "designed as low poly", distributed as Blender, GLB, FBX and VRM, with every prop and spell also modelled. They were first commissioned, then made in-house (concept art by mollyismutz, modelling by frogeater). [Wizzypedia, 3D Models](https://wizzypedia.forgottenrunes.com/3D_Models)
- INFERRED (snippet, the official post returned HTTP 404 on direct fetch). The team wrote that an in-house artist rebuilt each model within a polygon budget, that a separate rigger handled animation, that "every single head aligns with every single body", and that the rigs target Nifty Island's A-pose and oncyber's T-pose. [Forgotten Runes post](https://www.forgottenrunes.com/posts/the-forgotten-runes-wizards-cult-has-entered-the-3rd-dimension)
- VERIFIED. The Sandbox announced on August 24, 2022 that holders of thirteen collections (Bored Ape Yacht Club, Moonbirds and World of Women are the three named) could use avatars based on their NFTs in Alpha Season 3; Sebastien Borget: "you will be able to see them become a 3D character on the avatar creator and immediately interact with them". The article gives no technical method. [The Block](https://www.theblock.co/post/164995/bored-apes-and-moonbirds-to-get-playable-avatars-in-new-sandbox-season)
- VERIFIED. A contractor (Vizuu 3D) describes one such job for The Sandbox: the client supplied the 2D artwork, an older base model "needed to be redesigned", and "All assets were carefully created" to "match 5k unique variations". Tool listed: Blender. Duration and team size are not stated. [Contra portfolio page](https://contra.com/p/FPNNXNsq-playable-voxel-avatars-from-nft-collections)
- INFERRED (secondary press). RTFKT gave CloneX holders the 3D files of their avatar in 2022 (.max, .blend, .c4d, .gltf, .fbx) together with commercial rights. CloneX art was 3D from the start, so this is distribution of source files, not 2D to 3D conversion. [The Block](https://www.theblock.co/post/156015/nike-owned-rtfkt-grants-clonex-holders-commercial-rights-to-nfts), [Metaverse Post](https://mpost.io/rtfkt-introduces-new-utilities-for-clone-x-owners)
- INFERRED (snippet; the former Larva Labs page now redirects). Meebits were generated as 3D voxel characters and owners got an asset pack with a T-pose OBJ; a later community effort added VRM, FBX and GLB. Native 3D, not derived from 2D. The current site only says "Meebits is a collection of 20,000 digital sculptures designed by the world-renowned artists at Larva Labs." [meebits.com](https://meebits.com/) (VERIFIED for that sentence), [former page](https://meebits.larvalabs.com)
- INFERRED (secondary press). Otherside: in a July 2023 closed preview many participants played "3D versions of their BAYC PFPs"; Yuga's stated plan was a 3D version for BAYC, MAYC, CryptoPunks and Meebits. The direct fetch returned HTTP 403. No technical breakdown found. [nft now](https://nftnow.com/features/exclusive-yuga-labs-chief-creative-officer-talks-otherside-preview/)

Hand-built per collection (one avatar design per IP, not per token):
- VERIFIED (sponsored article, flagged "Brought to you by Nifty Island", March 6, 2024). "Nifty Island has also partnered with over 300 different NFT collections", "enabling players to use their PFPs in-game", and "allowing the game's creators to use the collections' IP to create inspired avatars and buildings". [Decrypt](https://decrypt.co/220430/one-of-the-coolest-things-out-there-right-now-the-web3-gaming-world-talks-nifty-island)
- INFERRED (snippet). A Nifty Island proposal to another project asked for limited commercial use of its IP "to create a fully playable avatar within Nifty Island that is token-gated" for holders. [Bobu proposal](https://just-ixora-cef.notion.site/Bobu-Island-89fa6e225ccc422cbf9f2740dfeca82d)
- INFERRED (snippet, unofficial community note of about August 2024). Nifty Island avatar triangle budget "20k - 40k max". [HackMD note](https://hackmd.io/@XR/anata-v1)
- INFERRED (snippet). Nifty Island's February 2025 AI feature is conversational agents placed on islands, not image-to-avatar generation. [Decrypt](https://decrypt.co/307270/nifty-island-game-drag-drop-ai-agents)

2D sprites or 2D heads on shared bodies:
- INFERRED (snippet; the Notion pages render through JavaScript and returned only the word "Notion" to a direct fetch). Worldwide Webb: because most PFPs are head-and-shoulders images, the team built shared body animations so that a PFP from a supported collection is fitted onto a full animated character; non pixel-art collections had to supply sprites through an API; integrations were manual and transparent backgrounds were preferred. [Avatars](https://worldwidewebb.notion.site/Avatars-d0b260cf04c7474ca03f5442b691410d), [Integration FAQ](https://worldwidewebb.notion.site/Integration-FAQ-1c3cd1a1d58141c9b60bfa7d4d1c08c5)
- INFERRED (snippet). FoxFam's PixelFam used a sprite sheet generator: roughly 400 traits across all angles, more than 15,000 individual pixel images. [FoxFam update, March 2022](https://foxfam.notion.site/Project-Update-March-23rd-2022-73d0e41a46ae426a8d17333336fcceb8)
- INFERRED (snippet). CyberBrokers shipped PixelBrokers: hand-drawn 2D pixel versions of each CyberBroker with a 48x48 PFP, an animated GIF and a sprite sheet; Worldwide Webb was the first integration. [CyberBrokers](https://cyberbrokers.substack.com/p/pixelbrokers-have-arrived)
- INFERRED (snippet, undated). Cool Cats described a 2D avatar system (full-body and PFP modes, backgrounds, borders, outfits), not a 3D export. [NFT Calendar](https://nftcalendar.beehiiv.com/p/shape-digital-persona-cool-cats-avatar-system)

Game characters not derived from each holder's token:
- VERIFIED. Pudgy World was announced (December 10, 2023) as a "3D browser game experience" in which "Pudgy Penguin NFT holders as well as plush toy owners will be able to adopt a virtual likeness" of their collectibles, with traits that players can buy. No engine or pipeline is named. [Decrypt](https://decrypt.co/209174/pudgy-penguins-reveals-pudgy-world-nft-game-rollout-plans)
- INFERRED (snippet). Pudgy World launched as a free browser game on March 9, 2026 with 12 towns. The Defiant article body did not load on direct fetch. [The Defiant](https://thedefiant.io/news/nfts-and-web3/pudgy-penguins-launches-pudgy-world-browser-game), [NFT Calendar](https://nftcalendar.io/news/pudgy-penguins-launch-browser-based-game-pudgy-world/)
- INFERRED (snippet). Pudgy Party (Mythical Games, global launch August 29, 2025) offers unlockable costumes and accessories behind a custodial account; nothing found on NFT traits driving the character. [CryptoPotato](https://cryptopotato.com/pudgy-penguins-and-mythical-games-announce-global-launch-of-pudgy-party/)
- INFERRED (snippet). Hytopia sells its own collection of 15,000 Genesis Avatars, each giving "a unique fully playable character". This is a native avatar collection, not a conversion of third-party PFPs. [Harvest listing](https://harvest.art/barn/1/0x05745e72fb8b4a9b51118a168d956760e4a36444)
- INFERRED (secondary press). Pixelmon promised "fully modeled 3D character[s]", revealed models that were widely criticised, with reports of stock models converted to voxels, and later upgraded the art. A cautionary case for rushed mass 3D. [DappRadar](https://dappradar.com/blog/pixelmon-collectors-cheated-with-generic-graphics-after-reveal), [Decrypt](https://decrypt.co/110542/nft-game-pixelmon-attempts-comeback-after-70m-horrible-art-reveal)
- INFERRED (snippet). Azuki and Moonbirds IP were made available in September 2025 inside Verse8, an AI game builder; this is IP licensing to a tool, not holder avatars. [Odaily](https://www.odaily.news/en/post/5182085)

Algorithmic 3D (not from 2D PFPs):
- INFERRED (snippet). Daz 3D's Non-Fungible People: 8,888 avatars assembled algorithmically from a pool of pre-built 3D characteristics and usable in game engines. This is native modular 3D generation. [Daz 3D blog](https://blog.daz3d.com/daz-3d-breaks-the-mold-with-head-turning-nft-collection/)

Summary matrix (classification is the researcher's reading of the sources above):

| Project | Method | Per-token fidelity | Source quality |
|---|---|---|---|
| Forgotten Runes Wizards | Full 3D, hand-modelled trait parts, shared rig | Yes | Team wiki (VERIFIED) |
| The Sandbox (13 collections) | Hand-made voxel trait assets on a redesigned base | Yes for 5k variations (contractor) | Press plus contractor page |
| CloneX, Meebits | Native 3D, files handed to holders | Yes | Press, snippets |
| Otherside | 3D versions of BAYC PFPs | Stated yes, method unknown | Press snippet |
| Nifty Island | Studio-made avatar per partner collection | Not established | Sponsored press |
| Worldwide Webb, PixelBrokers | 2D heads on shared animated bodies, or hand-drawn sprites | Yes (2D) | Snippets |
| Pudgy World, Pudgy Party | Game characters with traits or costumes | "Virtual likeness" announced, method unknown | Press |
| AI-generated per token | None found | n/a | n/a |

### Inferences
- INFERRED. The precedents that preserved per-token identity in 3D all paid for it with manual trait modelling on a shared base and rig, done by studios over months, not by one artist in two weeks.
- INFERRED. The cheapest precedent that still keeps each holder's art recognisable is the Worldwide Webb pattern: keep the PFP head as an image and put it on a shared animated body. It maps directly onto the Chog art, which is a bust (see key question 3).
- INFERRED. "Nifty Island: playable avatars from 2D NFTs", as worded in the brief, is better described as "studio-authored avatars inspired by a collection's IP"; whether trait variation per token exists there is not established by any source opened.

### Gaps
- UNKNOWN. Official Nifty Island creator documentation: `docs.niftyisland.com` does not resolve (DNS failure); no official specification of rig, triangle or texture limits was found.
- UNKNOWN. Doodles, Azuki, Moonbirds, CyberKongz: no team source describing a 2D-trait to playable-3D pipeline was found. CyberKongz VX (voxel versions) is known to the researcher from memory only and was not verified in this session.
- UNKNOWN. Pudgy World's engine and how a holder's penguin traits are rendered: not found.
- UNKNOWN. Any Monad ecosystem game shipping playable avatars derived from a PFP collection: not found. The only Monad game listings seen (Lumiterra, Omnia, Fantasy Top) are unrelated to PFP avatars. [Bankless](https://www.bankless.com/read/monads-culture-apps)
- UNKNOWN. Any PFP collection that used an image-to-3D service to mass-produce playable avatars: not found.

## Key question 2: Option A, modular 3D character (shared base, swappable parts, palette swaps)

### Takeaway
A faithful modular 3D set needs roughly 50 meshes and 150 texture variants for the 552 tokens whose metadata is recorded (28% of the collection), which is beyond one artist in two weeks; a reduced version (one Chog base, palette swaps, a handful of headwear families) is feasible and is best built as rigid parts with a single atlas material, because the current rig is a 6-transform rigid rig and WebGL skinning runs on the CPU.

### Cited Findings

What the trait data demands (local census, 552 of 1,969 metadata entries recorded):
- VERIFIED. Recorded value counts: Body 86, Head 52, Eyes 44, Background 26, Aura 20, Mouth 15, Skin 14, Accessory 9, Tier 5, Base 4, Form 2, Side 2, Naked 2. Head is absent on 73 tokens, Body on 58, Aura on 285, Accessory on 502. `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-trait-census.json`
- VERIFIED. The four Legendary tokens in the sample (for example CHOG #53 "Blaze") carry only `Base: 1:1` and `Tier: Legendary`, with no Head, Body, Eyes or Skin value. Same file, key `example_by_tier`.
- VERIFIED (keyword grouping run in this session on the same census; the grouping rules are the researcher's choice). Head families: baseball and bucket caps 27 values on 300 tokens (54% of recorded); tall or rigid hats (Crown, Russian Hat, Silver wreath, Spartan Helmet, Wizard Hat) 5 values on 59 tokens; beanies, durag, robber 8 values on 57 tokens; hair or wig 3 values on 30; headbands 2 on 14; horns 4 on 5; one-offs (Clown Nose, Holy Seal, Taped Banana) 3 on 14.
- VERIFIED (same method). Body families: shirts, tees, polos 26 values on 163 tokens (30%); full costumes with their own silhouette (Shark Suit, Dragon suit, Banana suit, Duck Float, Golden Armor, Astro Suit and others) 21 values on 113 tokens (20%); tuxedos and suit jackets 9 values on 83; jerseys, sleeveless, tanks 9 on 39; hoodies 7 on 27; vests 5 on 24; jackets and tracksets 5 on 23; yukata and Chinese jackets 4 on 22.
- VERIFIED (same method). Eyes: 29 drawn-eye values on 350 tokens (63%), 11 visor, glasses or VR values on 116 tokens (21%), 4 laser values on 82 tokens (15%).
- VERIFIED. Skin: 5 values (Pink 163, Origin 152, Red 77, Yellow 70, Black 67) cover 529 of the 552 recorded tokens; the remaining 9 values cover 19 tokens. Same census file.

Unity constraints that shape the structure:
- VERIFIED. "For animated character models that use a Skinned Mesh Renderer, you should use only one of these components per character." and "Using two skinned meshes in place of a single one could roughly double the rendering time for a model." Also "Keep the number of materials on each model as low as possible." [Unity 6 manual, Creating models for optimal performance](https://docs.unity3d.com/6000.0/Documentation/Manual/ModelingOptimizedCharacters.html)
- VERIFIED. "15 additional bones on a rig that already has 30 bones takes Unity 50% more operations to solve in Generic mode." and "use linear blend skinning with a maximum of four influences per vertex". Same page.
- VERIFIED. "A Humanoid Rig consumes 30 to 50% more CPU time than the equivalent generic rig." (numeric range rewritten without a dash). Also "Rendering [skinned meshes] is expensive.", "Make sure that every object using a SkinnedMeshRenderer requires it.", and "Avoid overusing Animators." [Unity, mobile optimisation tips part 2](https://unity.com/how-to/mobile-game-optimization-tips-part-2)
- VERIFIED. "Batching doesn't support Skinned Mesh Renderers." Dynamic batching requires meshes that "must not exceed 900 vertex attributes or 300 vertices". [Unity 6 manual, draw call batching](https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching.html)
- VERIFIED. `SkinnedMeshRenderer.bones` is "The bones used to skin the mesh."; ordering details are deferred to the `Mesh.bindposes` page. That this array can be reassigned at runtime so that a separately exported part follows the base skeleton is the researcher's inference from the property, not a statement on the page. [Scripting API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SkinnedMeshRenderer-bones.html)
- VERIFIED. `Mesh.CombineMeshes`: "Combines several Meshes into this Mesh."; "If all Meshes share the same Material, this property should be set to true." (for `mergeSubMeshes`); "Combining Meshes is useful for performance optimization." The page's example uses MeshFilter and MeshRenderer only and does not address bone weights. [Scripting API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh.CombineMeshes.html)
- VERIFIED. Unity reads `.fbx`, `.dae`, `.dxf` and `.obj` natively; `.blend` files "fail to import unless you have the corresponding 3D modeling software installed"; "Don't use proprietary file formats in production and export to the `.fbx`format wherever possible." glTF is not listed. [Unity 6 manual, model file formats](https://docs.unity3d.com/6000.0/Documentation/Manual/3D-formats.html)
- VERIFIED. glTF needs the glTFast package (`com.unity.cloud.gltfast`, version 6.14.1 on the page): "Use glTFast to import and export glTF 3D files efficiently at runtime or in the Editor."; "All platforms as well as Universal, High Definition and Built-In Render Pipelines are supported." [glTFast manual](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.14/manual/index.html)
- VERIFIED. Animation import compression: "Keyframe Reduction" ("Reduce redundant keyframes on import"), "Optimal" ("Let Unity decide how to compress, either by keyframe reduction or by using dense format", Generic and Humanoid only); "Off" gives "slower performance and bigger file and runtime memory size". [Unity 6 manual, Animation tab](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AnimationClip.html)

Blender side:
- VERIFIED. Blender 5.0 glTF exporter options: "Export Deformation Bones only" ("Animation for deformation bones are baked"), "Use Rest Position Armature", "Bone influences" ("Models may appear incorrectly in many viewers with value different to 4 or 8"), "Use Vertex Color" (Material or Active), animation export by Actions or "NLA Tracks" ("each NLA Track will be export as an independent glTF animation"), GLB single file or separate files, "+Y up". [Blender 5.0 manual, glTF 2.0](https://docs.blender.org/manual/en/5.0/addons/import_export/scene_gltf2.html)
- VERIFIED. Blender 5.2.0 LTS is installed on the working machine (`/Applications/Blender.app`, build of 2026-07-14) and was used headless in this session to inspect FBX files.

What the prototype uses today (measured in this session with headless Blender and by reading the prefabs):
- VERIFIED. The four KayKit Dungeon Pack 1.0 characters in the project have **no armature and no skin**: each is 5 or 6 separate static meshes (Body, Head, ArmLeft, ArmRight, plus Hair, Hat or Helmet parented to the head), 4,224 to 5,511 triangles in total (rogue 4,224, knight 4,634, barbarian 5,427, mage 5,511), and 6 to 9 flat-colour materials per character. Files: `/Users/red.g/CascadeProjects/chogathon/game/Assets/TwinStickShooter/Third Party Assets/KayKit Dungeon Pack 1.0/Characters/character_{mage,rogue,knight,barbarian}.fbx`
- VERIFIED. The hero prefabs contain 5 or 6 `MeshRenderer` components and 0 `SkinnedMeshRenderer`; the sum of material slots is 19 (Knight), 18 (Spellcaster) and 21 (Archer); every renderer has shadow casting on. `/Users/red.g/CascadeProjects/chogathon/game/Assets/QuantumUser/Resources/Character_Playable_{Knight,Spellcaster,Archer}.prefab`
- VERIFIED. The 18 animation files of the local "KayKit Character Animations 1.1" folder drive a 6-bone armature named `Body, Head, armLeft, handSlotLeft, armRight, handSlotRight` (no legs, no spine chain). `/Users/red.g/CascadeProjects/chogathon/game/Assets/TwinStickShooter/Third Party Assets/KayKit Character Animations 1.1/Single Animations/Idle.fbx`
- VERIFIED. The same pack ships 19 swappable head and hair meshes (three heads per class, hair sets, a skull) as plain static meshes of 1,004 to 2,242 triangles. `.../KayKit Dungeon Pack 1.0/Characters/extra heads/`

### Inferences
- INFERRED. Recommended structure for a Chog base, matching the rig already animated in the project: four rigid meshes (`Body`, `Head`, `armLeft`, `armRight`) with the same names and pivots as the KayKit parts, headwear and eyewear as rigid children of `Head`, held item as a child of a hand slot. No skin weights, no bind poses, no bone-sharing code. The existing clips should retarget because generic clips bind by transform path; this is a 30-minute test, not a verified fact.
- INFERRED. Use one shared atlas material for every part of every hero (a gradient or palette texture like KayKit Adventurers, see key question 8), and express Skin, outfit colour and cap colour as UV offsets into that palette or as vertex colours. With one material, a hero costs at most one draw call per rigid part instead of 18 to 21, and parts under 300 vertices become eligible for dynamic batching across heroes.
- INFERRED. If a skinned base is preferred (smoother deformation), export one joined mesh per hero and strip non-deform bones; Unity's "one Skinned Mesh Renderer per character" rule and the CPU skinning finding of key question 7 both argue against separate skinned parts at runtime.
- INFERRED (estimate). Part count for full fidelity on the recorded 28%: about 16 to 18 head meshes (8 family meshes, 5 unique tall hats, 3 one-offs), about 8 torso meshes, about 21 costume meshes, 3 to 4 eyewear meshes, and on the order of 150 texture or decal variants (27 cap prints, 26 shirt prints, 29 eye decals, 15 mouths, palettes). The 1,417 tokens whose metadata is not yet recorded may add values.
- INFERRED (estimate). A reduced set that one artist can ship in about a week: Chog base (4 parts), 1 cap, 1 bucket hat, 1 beanie, 1 tall-hat proxy, 1 horn pair, 1 visor, 1 glasses, plus palette. That covers the silhouette family of roughly 80% of recorded head values but none of the cap prints and none of the 21 costumes, so a holder would recognise "a red Chog with a cap", not "my Chog".
- INFERRED. Humanoid import is not worth its 30 to 50% CPU overhead here: nothing in the project needs retargeting from human motion libraries, and the Chog proportions are not humanoid.

### Gaps
- UNKNOWN. Full value list for the 1,417 unrecorded tokens (the public gateway rate-limited the crawl at entry 553, see key question 9). Part counts above are a floor.
- UNKNOWN. Whether the existing animation clips (copied from the Quantum sample, see STATUS) bind cleanly to a new rigid Chog hierarchy: not tested.
- UNKNOWN. The Blender FBX exporter manual page was reachable (HTTP 200, `https://docs.blender.org/manual/en/latest/files/import_export/fbx.html`) but its option text could not be extracted in this session; no FBX exporter setting is quoted here.
- UNKNOWN. Real frame cost of 18 to 21 material slots per hero versus one: not measured (see key question 7).

## Key question 3: Option B, 2D art in a 3D world (billboards, paper dolls, the actual NFT image)

### Takeaway
One quad per hero showing the real NFT image is the cheapest and the most faithful to each holder's art, and the art stays legible down to about 48 pixels; but the image is a square bust with a background, so it works as a standee, token or portrait disc rather than as an animated full-body sprite.

### Cited Findings
- VERIFIED (measured on 15 Genesis images downloaded in this session: ids 1, 2, 4, 14, 31, 40, 53, 54, 86, 100, 177, 197, 300, 465, 550). Every file is a 1000x1000 lossy WebP of 270,210 to 300,742 bytes. The framing is a tight head-and-shoulders bust: the head and headwear fill most of the frame, shoulders are cut by the lower edge, there are thick dark outlines, and the background is a flat colour with an aura shape behind the head when the token has an Aura. Source pattern: `https://ipfs.filebase.io/ipfs/bafybeihau2egcccbdxymeckmmbrqytbfor5gkt3en6d5fsbueymdbq7g6e/{metadataId}.webp`
- VERIFIED (visual check by the researcher on a downscaled sheet of 10 of those images at 32, 48, 64 and 96 pixels). At 48 to 64 pixels the headwear shape, eyewear and background colour still distinguish the ten tokens from one another; at 32 pixels only colour blocks and the largest shapes survive; mouths are not readable below about 64 pixels.
- VERIFIED. The 1:1 Legendary (#53), the Skull form (#54) and the Radioactive base (#31) are visually different characters (a flame creature, a skeleton, a translucent green head with horns). An image-based hero shows them correctly with no extra work.
- VERIFIED. Cult of the Lamb (Unity) chose "the 2.5D look" because it was the team's first 3D engine and "It felt like a waste if we just stuck to normal 2D"; the cost was that "we had to keep the camera relatively fixed, which restricted our options for combat" and "We had to arrange things so that the important stuff was all at the top/back of the screen." Lighting needed "custom solutions". [Unity blog, October 28, 2022](https://unity.com/en/blog/games/recipe-behind-smash-hit-cult-of-the-lamb)
- INFERRED (snippets). Don't Starve is described by Klei as "2D characters and odd creatures inhabiting a unique 3D world", and by players as flat 2D animations rendered in 3D with a fixed non-orthogonal camera. Octopath Traveler's HD-2D put 2D sprites in an Unreal Engine 3D scene and added point lights next to effects so sprites cast shadows on the environment. [Klei forum](https://kleiforums.com/forums/topic/56775-is-there-a-name-for-the-games-2d-as-3d-animation-style/), [Unreal Engine spotlight](https://www.unrealengine.com/en-US/spotlights/octopath-traveler-s-hd-2d-art-style-and-story-make-for-a-jrpg-dream-come-true), [Siliconera](https://www.siliconera.com/project-octopath-traveler-developers-answer-project-started-troubles-developing-hd-2d/)
- VERIFIED. "To reduce the number of draw calls Unity sends to the GPU, create a sprite atlas." and "Unity only needs to create one draw call for all the sprites in a sprite atlas." [Unity 6 manual, sprite atlas](https://docs.unity3d.com/6000.0/Documentation/Manual/sprite/atlas/atlas-landing.html)
- VERIFIED. Dynamic batching applies to meshes of at most 300 vertices and 900 vertex attributes that share a material; "Batching of transparent GameObjects might be limited because Unity sorts all meshes back-to-front before batching." [Unity 6 manual](https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching.html)
- VERIFIED. "Rendering an object with transparency always uses more GPU resources than rendering an opaque object" and "It's a good practice to use an opaque material whenever possible, especially for mobile platforms." [Unity mobile tips](https://unity.com/how-to/mobile-game-optimization-tips-part-2)
- INFERRED (snippet). NFT precedent for the bust problem: Worldwide Webb fitted head-and-shoulders PFPs onto shared animated bodies (key question 1).

### Inferences
- INFERRED. Techniques that make a single static image feel alive, none of which needs extra art: squash and stretch on move start and stop, a sine bob while idle, a lean in the movement direction, a blob shadow quad on the ground, a white flash by lerping a shader colour on hit, a one-pixel outline in team colour, and a short scale punch on ability cast. All are vertex or uniform animations on one quad, so they add no draw call. These are standard practice and are not sourced to a document opened in this session.
- INFERRED. Use an opaque quad with the full square image, or a disc cut by alpha test, rather than alpha blending: the Genesis image has no transparent background, an opaque quad avoids sorting and overdraw, and the background colour is itself a trait.
- INFERRED. Facing: a camera-facing billboard reads best under a fixed top-down camera, but the image cannot show which way the hero aims. A ground ring with an arrow, or the 3D body of Option C, has to carry direction. The Side trait (Left 184, Right 364 in the census) only says which way the bust leans in the art.
- INFERRED. Cost per hero: 1 quad (2 triangles), 1 texture. If the 6 to 10 images of a match are packed into one runtime atlas or texture array, all hero quads can share one material and batch into a single draw call; otherwise it is one draw call per hero, still far below the current 18 to 21.
- INFERRED. Risk: on the hackathon's Execution and Fun axes a field of flat squares can read as low effort unless it is clearly stylised (board-game standees, coins, cards). This is a judgment, not a measurement.

### Gaps
- UNKNOWN. A developer source describing billboard implementation details for Don't Starve, Paper Mario or HD-2D: not found. Paper Mario was not researched beyond the brief's mention.
- UNKNOWN. Legibility on the actual phone at the actual camera height: the 48 to 64 pixel finding comes from downscaled images viewed on a desktop, not from the game.
- UNKNOWN. Whether cutting the bust out of its background is wanted by holders (the background is a trait that some will want visible).

## Key question 4: Option C, hybrid (3D body, NFT art as face, portrait or banner, colour identity from traits, aura as particles)

### Takeaway
A shared low-poly Chog body carrying the holder's actual image (as a face plate, back banner or floating portrait disc), tinted from Skin and Background and wrapped in an aura effect, is the only option that keeps both exact art and a readable 3D character within one artist's two weeks; it also degrades gracefully to Option B if the body slips.

### Cited Findings
- VERIFIED. Trait data available for identity without any art: Skin on 548 of 552 recorded tokens (5 values cover 529), Background on 548 (26 values), Aura on 267 (20 values), Tier on all 552 (Common 356, Uncommon 170, Rare 16, Epic 6, Legendary 4). `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-trait-census.json`
- VERIFIED. Metadata carries names, not colours: attributes are `trait_type` and `value` strings such as "Rosy Pink" or "Fiery Aura", and one `image` URI. A colour table has to be authored or sampled from the image. `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-metadata.jsonl`
- VERIFIED (visual, 15 images). Backgrounds are flat colour fields, so a corner patch of the image gives the Background colour directly.
- VERIFIED. The project already resolves each recorded token to a role, a signature shape and an effect (552 resolved, 160 distinct kits), with the limit that "no combat asset is linked to them yet". `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-catalog-coverage-2026-10-09.json`
- VERIFIED. The Particle System renderer supports billboard and mesh particles ("Unity renders the particle from a 3D Mesh instead of a Billboard.") and optional mesh GPU instancing ("This requires the use of a compatible shader."). [Unity 6 manual](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysRendererModule.html)
- VERIFIED. "To minimize overdraw, reduce the number and/or size of particles in your game." [Unity mobile tips](https://unity.com/how-to/mobile-game-optimization-tips-part-2)
- INFERRED (snippet). Precedent: PFP heads on shared animated bodies (Worldwide Webb), and "every single head aligns with every single body" as the alignment rule of a modular set (Forgotten Runes). See key question 1.

### Inferences
- INFERRED. Where to put the image, ranked by legibility from a top-down camera: (1) a camera-facing portrait disc above the hero, readable from any angle and doubling as the nameplate; (2) the image mapped on a large flat "face plate" on the front of an oversized head, which reads when the hero faces the camera and matches the bust framing; (3) a banner or cape on the back, which reads when the hero walks up-screen. One and two together cost two quads.
- INFERRED. Colour identity: body tint from Skin (14 authored colours), ground ring and UI frame from Background (sampled per image), rim or outline thickness from Tier. Because 5 Skin values cover 96% of recorded tokens, Skin alone will not separate two heroes; Background (26 values) plus the portrait does.
- INFERRED. Aura: one looping effect per hero with a colour and a shape family (flame, mist, smoke, wind, plain glow) chosen from the 20 Aura values, capped to a few particles or implemented as a single scrolling mesh ring so that it adds one draw call and little overdraw. About half of the tokens have no Aura, which makes the effect a visible distinction rather than noise.
- INFERRED. Headwear can be added later as rigid children of the head for the largest families (one cap mesh covers 54% of recorded tokens), with the exact print left to the portrait. This is the bridge from Option C to a reduced Option A.
- INFERRED (estimate). Work for the artist: Chog base in 4 rigid parts with one atlas material (2 to 3 days), fitting to existing clips and a few new ones (1 to 2 days), face plate and portrait disc setup with outline and hit flash shader (1 day), aura effect family (1 to 2 days), then headwear families as stretch. Engineering needs the image pipeline of key question 9.

### Gaps
- UNKNOWN. Whether a flat face plate with a square bust looks acceptable on a round Chog head: needs a visual test in engine.
- UNKNOWN. Whether the hackathon judges read "the NFT image on a shared body" as making the NFT "essential". The site only states "The Chog NFT must be essential." and "take the NFT out, does it still work?" `/Users/red.g/CascadeProjects/chogathon/research/evidence/chogathon-site-facts-2026-10-09.json`

## Key question 5: Option D, generated 3D (image-to-3D services and models in 2026)

### Takeaway
Hosted image-to-3D is priced at roughly 0.3 to 0.7 USD per textured asset and can target low triangle counts, so about 2,000 assets would cost on the order of 500 to 1,400 USD; but nothing found shows these tools handling flat cartoon busts with no body, each result would be a unique mesh and texture to rig, check and host, and the licence of the input art is unknown.

### Cited Findings

Meshy:
- VERIFIED. API credit costs for Image to 3D: mesh only 20 credits (meshy-7.1, meshy-6) or 5 (meshy-6-lite, meshy-t2); mesh with 2K textures 30 or 15; Remesh 5; Retexture 10; Auto-Rigging 5 per request; Animation 3 per action. [Meshy API pricing](https://docs.meshy.ai/api/pricing)
- VERIFIED. API parameters: `target_polycount` 100 to 300,000 (default 30,000) for standard remesh, 100 to 15,000 (default 4,000) for Smart Topology (`meshy-t2`); `topology` quad or triangle; `pose_mode` `a-pose` or `t-pose`; inputs `.jpg`, `.jpeg`, `.png`; outputs glb, obj, fbx, stl, usdz, 3mf. "The actual count may deviate from the target depending on the geometry." [Meshy API, Image to 3D](https://docs.meshy.ai/api/image-to-3d)
- VERIFIED (structured data embedded in the pricing page, read with curl). Plans: Free 0 USD with 100 credits per month and CC BY 4.0 licence; Pro 20 USD per month with 1,000 credits and private licence; Premium 40 USD with 3,000 credits; Studio 70 USD with 5,500 shared credits; Ultra 100 USD with 8,000 credits. Pro includes "API access, private asset ownership, and 10 concurrent tasks". [Meshy pricing](https://www.meshy.ai/pricing)
- VERIFIED. "If you are on a premium plan, you own all assets you create with Meshy. If you are on a free plan, we grant you a CC BY 4.0 license instead." Ownership is conditional on having "used materials that do not violate the copyrights of others". Same page.
- VERIFIED. In the web app, Image to 3D costs 25 credits on Meshy 7 and 20 on Meshy 6, and Remesh, Rigging and Animate are listed at 0 credits. [Meshy web app pricing](https://docs.meshy.ai/en/webapp/pricing)

Tripo:
- VERIFIED. The P1 model is "Optimized for best-in-class low-poly generation", with a face limit of "48 to 20,000 (recommended: ≥150 simple, ≥250 complex)" and costs of 40, 50 or 60 (no texture, standard, detailed) for image to 3D; it does not support quad, smart low poly or part generation. No USD price per credit on the page; no rigging endpoint on the page. [Tripo developer docs, P1](https://developers.tripo3d.com/en/models/p1)
- INFERRED (snippet). H3.1 costs 20, 30 or 40 credits for image to 3D, with add-ons for quad mesh (+5) and smart low poly (+10). [Tripo developer docs, H3.1](https://developers.tripo3d.com/en/models/v3-1)
- INFERRED (snippet of the vendor's AWS Marketplace listing). Usage beyond prepaid credits is billed at 0.01 USD per credit. [AWS Marketplace](https://aws.amazon.com/marketplace/pp/prodview-hwlbvx7vubh7c)
- UNKNOWN. Tripo's consumer pricing page returned HTTP 403 to both the fetch tool and curl with a browser User-Agent (`https://www.tripo3d.ai/pricing`); third-party figures conflict with one another and are not reported here.

Rodin (Hyper3D):
- VERIFIED. Creator 30 USD per month ("About 60 models"), Business 120 USD per month ("About 416 models", "Full API access"), "Direct credits cost $1.5 per credit" on the free tier; paid plans state "Unlimited export and any use". Credits per generation and rigging are not stated. [Hyper3D pricing](https://hyper3d.ai/pricing)

Open models and their licences (licence files read directly):
- VERIFIED. TripoSR: MIT, "Copyright (c) 2024 Tripo AI & Stability AI". [LICENSE](https://raw.githubusercontent.com/VAST-AI-Research/TripoSR/main/LICENSE)
- VERIFIED. TRELLIS and TRELLIS.2: MIT, Microsoft Corporation. [TRELLIS](https://raw.githubusercontent.com/microsoft/TRELLIS/main/LICENSE), [TRELLIS.2](https://raw.githubusercontent.com/microsoft/TRELLIS.2/main/LICENSE)
- VERIFIED. Stable Fast 3D: Stability AI Community License (last updated July 5, 2024). Commercial use requires registration; above 1,000,000 USD of annual revenue an enterprise licence is required; "You own any outputs generated from the Models or Derivative Works to the extent permitted by applicable law." [LICENSE](https://raw.githubusercontent.com/Stability-AI/stable-fast-3d/main/LICENSE.md)
- VERIFIED. Hunyuan3D 2.1 community licence (release date June 13, 2025): "THIS LICENSE AGREEMENT DOES NOT APPLY IN THE EUROPEAN UNION, UNITED KINGDOM AND SOUTH KOREA"; Territory is "the worldwide territory, excluding the territory of the European Union, United Kingdom and South Korea"; "You must not use, reproduce, modify, distribute, or display the Tencent Hunyuan 3D 2.1 Works, Output or results of the Tencent Hunyuan 3D 2.1 Works outside the Territory."; a separate licence is required above 1 million monthly active users; "Tencent claims no rights in Outputs You generate." [LICENSE](https://raw.githubusercontent.com/Tencent-Hunyuan/Hunyuan3D-2.1/main/LICENSE)

Runtime side:
- VERIFIED. Unity does not list glTF among natively imported formats; runtime loading of generated GLB files needs glTFast. [Unity formats](https://docs.unity3d.com/6000.0/Documentation/Manual/3D-formats.html), [glTFast](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.14/manual/index.html)

### Inferences
- INFERRED (arithmetic on the verified prices; API credits may be priced differently from plan credits). Meshy for 1,969 tokens: textured generation on meshy-6 or 7.1 plus auto-rigging is 35 credits each, 68,915 credits in total, which is about 860 USD at the Ultra plan rate (0.0125 USD per credit) or about 1,380 USD at the Pro rate (0.02 USD per credit). With the lite or t2 models (15 credits textured, plus 5 for rigging) it is 39,380 credits, about 490 to 790 USD. Monthly plan caps (8,000 credits on Ultra) mean a single month covers at most about 230 textured and rigged assets unless extra credits are bought at a rate not found.
- INFERRED. Tripo P1 at 50 credits and 0.01 USD per credit is about 0.50 USD per asset, about 985 USD for 1,969. Rodin Business at 120 USD for about 416 models is about 0.29 USD per model, about 570 USD spread over five monthly cycles.
- INFERRED. The triangle counts are controllable (100 to 15,000 on Meshy Smart Topology, 48 to 20,000 on Tripo P1), so a 2,000 to 4,000 triangle target comparable to the current KayKit heroes is requestable. Whether the result keeps the Chog features at that density is untested.
- INFERRED. The input is a bust: the body below the shoulders and the entire back of the head do not exist in the image, so a generator has to invent them, differently for each token. Consistent proportions across 1,969 heroes, needed for shared animations and hitboxes, are not guaranteed.
- INFERRED. Runtime cost profile: one unique mesh and one unique texture per hero, no batching between heroes, CPU skinning per hero (key question 7), and a per-match download of 6 to 10 GLB files. Hosting 1,969 files is required because generation cannot happen at match time.
- INFERRED. Hunyuan3D 2.1 is the one open model whose licence text excludes the EU. The project works on Paris time (STATUS), so if the team or its players are in the EU, that licence does not grant them use of the model or its outputs. This is a reading of the licence text, not legal advice.
- INFERRED. Feeding each holder's NFT image to a third-party service raises the same rights question as mirroring the image (key question 11), plus Meshy's ownership clause is conditional on the input not infringing.

### Gaps
- UNKNOWN. Output quality of any of these tools on Chog Genesis art: no test was run (no API key in this session) and no source evaluates flat-outline cartoon busts. This is the decisive unknown for Option D; the fastest test is 10 tokens through one free tier.
- UNKNOWN. Generation time per asset and wall-clock time for about 2,000 assets: not found on the pages opened. Only the concurrency of Meshy Pro (10 tasks) is verified.
- UNKNOWN. Price of Meshy API credits bought outside a plan: the API pricing page says credits are purchased in the account settings and gives no USD figure.
- UNKNOWN. Auto-rigging success rate on non-humanoid characters (a Chog is a ball with stubby limbs): not found.
- UNKNOWN. Any PFP collection that shipped AI-generated playable avatars: not found (key question 1).

## Key question 6: Access to trait layers (isolating traits without the original layer files)

### Takeaway
The Genesis layer files were not found anywhere public; the layers that chog.xyz serves for its PFP maker are a different, simpler art set. Differencing between tokens that share all but one or two traits does isolate individual trait regions cleanly, so reconstruction is technically possible, but it is laborious, approximate under occlusion, and legally unresolved. Asking the Chog team remains the right first move and its outcome is unknown.

### Cited Findings
- VERIFIED. Generative collections of this kind are composed from per-trait image folders stacked in a fixed order: "Create your different layers as folders in the 'layers' directory", then set `layersOrder` "in order of the back layer to the front layer". Whether Chog used this particular tool is not known. [HashLips art engine README](https://raw.githubusercontent.com/HashLips/hashlips_art_engine/main/README.md)
- VERIFIED (read from the site's JavaScript bundle and confirmed by HTTP 200 on sample files). The chog.xyz PFP maker composes a 1000x1000 canvas from public transparent layers at `https://www.chog.xyz/chogify/{category}/{category}-{n}.webp` plus `/chogify/body.webp` and `/chogify/face.webp`. Counts in the bundle: headwears 36, clothes 33, costume 11, eyes 20, mouth 17, accessories 17, face-accessories 18. Draw order in the bundle: body, clothes, face, eyes, mouth, face accessories, headwear, accessories, costume. `headwears-37.webp` returns 404, consistent with the count. Sample files are RGBA with 53% to 99% transparent pixels; responses carry `access-control-allow-origin: *`. [PFP maker](https://www.chog.xyz/maker?tab=pfp), example layer: [headwears-1.webp](https://www.chog.xyz/chogify/headwears/headwears-1.webp)
- VERIFIED (visual comparison of a composite of those layers with the Genesis images). The maker layers are the older, simpler Chog drawing (small spiky body, thin lines, full figure) and do not match the Genesis art style or its counts (Genesis records 52 Head, 86 Body, 44 Eyes values against 36, 33 plus 11, and 20 in the maker). One maker file (`headwears-36.webp`) is in fact a 3000x3000 PNG.
- VERIFIED (experiment run in this session). In the 552 recorded tokens there are 20 pairs of Origin-base tokens that differ by at most two traits. For tokens 40 and 197 (same Background, Aura, Body, Mouth, Head, Side; different Skin and Eyes), 92.3% of pixels are identical within 8 levels out of 255, and the differing pixels form two tight eye-shaped regions. For tokens 14 and 86 (different Background and Eyes), the thresholded difference isolates the two laser beams and eye rims. For 177 and 197 (Background Aqua Mint against Aqua, Skin Origin against Pink), fewer than 0.05% of pixels differ by more than 24 levels, because the two backgrounds and the two skins are close in colour. Method: download the two 1000x1000 WebP files, absolute per-pixel difference, thresholds 8, 24 and 48. Pair list derived from `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-metadata.jsonl`.
- VERIFIED. General-purpose segmentation tools with permissive licences exist: SAM 2 (Apache 2.0) and rembg (MIT). Neither was run in this session. [SAM 2 licence](https://raw.githubusercontent.com/facebookresearch/sam2/main/LICENSE), [rembg licence](https://raw.githubusercontent.com/danielgatis/rembg/main/LICENSE.txt)
- VERIFIED. The NFT contract source is MIT-licensed code and exposes `baseURI`, `metadataId`, `provenanceHash`; it contains no reference to layer files or art licence. `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-ChogMint.sol`, `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-mutability-facts.json`

### Inferences
- INFERRED. The Genesis images were composited deterministically from layers at fixed positions: shared traits reproduce pixel-for-pixel up to lossy compression noise. This is what makes differencing work.
- INFERRED. A reconstruction procedure, if ever needed: for each trait value pick tokens that share everything else, difference them to get the region mask, then take the pixels from one token. Limits: occluded parts are never recovered (a cap hides the top of the head, a mask hides the mouth), semi-transparent overlays such as auras and visors mix with what is behind them, values that appear on a single token have no partner, and the 1:1 tokens have no layers at all. With only 20 near-identical pairs among 552 tokens, most values would need chains of comparisons or manual masking.
- INFERRED. For the hero pipeline none of the recommended options needs true layers. A circular or rectangular crop of the full image is enough for Options B and C; layers would only add a cut-out head without background and cheaper texture variants for Option A.
- INFERRED. The maker layers are a ready-made, CORS-enabled, transparent paper-doll set, but they depict the mascot generically and do not correspond to any holder's Genesis token, so they do not serve "recognise my NFT".

### Gaps
- UNKNOWN. Whether the Chog team would share the Genesis layer files and layer order: not asked in this session. The brief says to treat this as unknown.
- UNKNOWN. A dedicated tool that reverse-engineers layers from a finished generative collection: not found; search results only returned forward generators.
- UNKNOWN. Licence terms of the maker layers: the site footer says "© 2026 Chog. All rights reserved." and no terms page exists (key question 11).

## Key question 7: Mobile WebGL budgets from primary sources

### Takeaway
The primary sources agree on direction more than on numbers: draw-call dispatch is the expensive part on WebGL and on OpenGL ES class drivers (Arm: aim under 500 per frame), skinned meshes cannot be batched or instanced and are skinned on the CPU in WebGL 2, and the SRP Batcher does not exist in the built-in pipeline. The prototype's heroes currently spend their budget on material slots, not on skinning.

### Cited Findings

Draw calls and batching:
- VERIFIED. "Web performance is close to native apps on the GPU", but "the CPU side dispatch of WebGL operations is slower than in native OpenGL", so the advice to "avoid large numbers of draw calls per frame" applies. Also "Unity can't profile draw calls for Web applications." [Unity 6 manual, Web performance](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-performance.html)
- VERIFIED. "Committing draw calls to the command stream is an expensive operation in terms of graphics driver overheads on the application processor. Draw call runtime costs are higher on OpenGL ES than on Vulkan." and "For OpenGL ES, aim for fewer than 500 draw calls per frame." with the caveat "these draw call count recommendations are approximate guidelines for current Arm GPU hardware". Also "Do not request many small draw calls for rendering, for example single points or quads, without batching." [Arm GPU Best Practices Developer Guide, issue 10, 2025-01-31, section 3.2](https://documentation-service.arm.com/static/67a62b17091bfc3e0a947695) (landing page: [developer.arm.com/documentation/101897](https://developer.arm.com/documentation/101897))
- VERIFIED. "The main API optimization is to minimize draw calls, combining what you can. To aid optimization, try combining textures in a texture atlas". Same guide, section 3.1.
- VERIFIED. The SRP Batcher is supported in URP, HDRP and custom SRPs and is not supported in the Built-in Render Pipeline; it "reduces the CPU time Unity requires to prepare and dispatch draw calls" without reducing their number. [Unity 6 manual, SRP Batcher](https://docs.unity3d.com/6000.0/Documentation/Manual/SRPBatcher.html)
- VERIFIED. Built-in pipeline tools that remain: static batching, dynamic batching ("Meshes must not exceed 900 vertex attributes or 300 vertices"; enabled by default in the Web player settings), and GPU instancing ("compatible with all Unity render pipelines"; in the built-in pipeline it "doesn't work with Shader Graph shaders"). [Batching](https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching.html), [Web player settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PlayerSettingsWebGL.html), [GPU instancing](https://docs.unity3d.com/6000.0/Documentation/Manual/GPUInstancing.html)
- VERIFIED. "GPU instancing is available on every platform except WebGL 1.0." [Unity 2022.3 manual](https://docs.unity3d.com/2022.3/Documentation/Manual/GPUInstancing.html)
- VERIFIED. "Disable shadows whenever possible to reduce draw calls." and "On lower-end mobile platforms, each camera can use up to 1 ms of CPU time." [Unity mobile tips](https://unity.com/how-to/mobile-game-optimization-tips-part-2)

Skinned meshes and bones:
- VERIFIED. "Batching doesn't support Skinned Mesh Renderers." and, for instancing, "Skinned Mesh Renderer components are not supported." [Batching](https://docs.unity3d.com/6000.0/Documentation/Manual/DrawCallBatching.html), [GPU instancing](https://docs.unity3d.com/6000.0/Documentation/Manual/GPUInstancing.html)
- VERIFIED. Unity staff (Jonas Echterhoff, January 20, 2017): "WebGL 2 supports transform feedback, so it is in theory capable of running our GPU Skinning implementation. However, in all our tests, it was consistently slower then CPU skinning in current WebGL 2 implementations, which is why we disabled it." [Unity Discussions](https://discussions.unity.com/threads/no-gpu-skinning-on-webgl-2.449776/)
- VERIFIED. The Unity 6 Web player settings page contains no "GPU Skinning" setting (the word "skinning" occurs zero times in the page text, checked with curl). [Web player settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PlayerSettingsWebGL.html)
- VERIFIED. Unity 2022.3: "Some engine parts are multi-threaded or SIMD-optimized and offer lower performance on WebGL.", with mesh skinning given as the example. Unity 6 changes the wording: "Unity provides multithreading for C/C++ code, but not yet for C# code", "SIMD on the web is supported as part of WebAssembly 2023 feature set", "Make sure to enable WebAssembly 2023 for best performance on newer browsers." [2022.3](https://docs.unity3d.com/2022.3/Documentation/Manual/webgl-performance.html), [Unity 6](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-performance.html)
- VERIFIED. "Native C/C++ Multithreading": "Enable this option to use the native Unity C/C++ engine code that targets WebAssembly/SharedArrayBuffer"; "This setting doesn't enable multithreading C# code". [Web player settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PlayerSettingsWebGL.html)
- VERIFIED. One Skinned Mesh Renderer per character; two "could roughly double the rendering time"; fewer bones is better; at most four influences per vertex. The Unity 6 page gives no numeric polygon or bone budget. [Unity 6 manual](https://docs.unity3d.com/6000.0/Documentation/Manual/ModelingOptimizedCharacters.html)
- INFERRED (snippet of legacy manuals up to 2018.3, no longer in the Unity 6 page). Older Unity guidance: 300 to 1,500 polygons per mesh and fewer than about 30 bones on mobile. [Unity 2018.3 manual, mirror listed in search results](https://docs.unity.cn/2018.3/Documentation/Manual/ModelingOptimizedCharacters.html)
- VERIFIED. Skinned Mesh Renderer settings that cost: more bone influences "increases the computational resources required"; "Update When Offscreen" is "disabled by default, for performance reasons". [Unity 6 manual](https://docs.unity3d.com/6000.0/Documentation/Manual/class-SkinnedMeshRenderer.html)
- INFERRED (third-party recap, not Unity's own text; Unity's WebGPU manual URL tried returned 404). Compute-based GPU skinning on the web requires the WebGPU backend, which is opt-in; WebGL 2 remains the default. [Cinevva recap of Unity 6.6](https://app.cinevva.com/news/2026-09-01-unity-6-6-webgpu-production)

Geometry and textures:
- VERIFIED. "Use models that create at least 10-20 fragments per primitive." and "Do not generate micro triangles, as they increase bandwidth and processing power costs for little visual benefit." Arm guide, section 5.8 (same PDF).
- VERIFIED. "Use texture compression, ideally ASTC". Arm guide, optimisation checklist (same PDF).
- VERIFIED. Unity Web texture formats: desktop browsers with DXT, mobile browsers with ASTC; "If a device doesn't natively support the texture compression format you select, Unity uses a software fallback to decompress the textures at runtime." and "it uses more memory and might run slower"; the manual gives a script that builds twice and an `index.html` check on `WEBGL_compressed_texture_astc`. [Unity 6 manual, texture compression in Web](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-texture-compression.html)
- VERIFIED. ASTC ranges "from eight bits/pixel (4x4 block size) down to 0.89 bits/pixel (12x12 block size)". [Unity 6 manual, texture formats by platform](https://docs.unity3d.com/6000.0/Documentation/Manual/texture-choose-format-by-platform.html)
- VERIFIED. WASM heap: initial 32 MB by default, maximum 2048 MB by default. [Web player settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PlayerSettingsWebGL.html)

Crowds (minions):
- INFERRED (snippet; the Unity blog URL now returns 404 after redirect). Unity's experimental "Animation Instancing" bakes skeletal animation to a texture and skins in the vertex shader so that instancing applies; it supported attachments and root motion but not transitions or layers. [Unity blog](https://blog.unity.com/engine-platform/animation-instancing-for-skinnedmeshrenderer)
- INFERRED (snippet). A community vertex-animation-texture library (MIT) bakes vertex positions per frame to a texture, with a limit of 2,048 vertices per mesh. [Mesh-Animation, index entry](https://awesome.ecosyste.ms/projects/github.com%2Fcodewriter-packages%2FMesh-Animation)

What the project measures today:
- VERIFIED. On the Mali-G52 MC2 phone at 30 Hz: about 32 fps, median frame 30 to 31 ms, six heroes, lowest quality level; "non-simulation work 17 to 18 ms is the limit", described as "draw submission, view scripts, UI" and "not yet broken down". `/Users/red.g/CascadeProjects/chogathon/docs/STATUS.md` (lines 142, 221, 379)
- VERIFIED. Those six heroes are rigid, with 18 to 21 material slots and 5 to 6 shadow-casting renderers each (key question 2).

### Inferences
- INFERRED. Six heroes at 18 to 21 slots are roughly 110 to 125 draw calls in the colour pass before any batching, and as many again in a shadow pass when shadows are on. That is a quarter of Arm's whole-frame guideline spent on six characters. A single-material hero would cut this to about 5 per hero (rigid parts) or 1 (joined mesh). Whether this is where the 17 to 18 ms goes is not established, since the breakdown has not been measured and Unity cannot profile draw calls in a Web player; the editor Frame Debugger or a native Android build is the way to count.
- INFERRED. For this target, rigid parts beat skinning: rigid parts cost transform updates and stay eligible for dynamic batching and instancing, whereas every skinned hero is skinned on the CPU in a single-threaded managed runtime, cannot batch and cannot instance.
- INFERRED. Minions should be rigid or vertex-animated, share one material, and use GPU instancing or dynamic batching; a skinned minion wave is the configuration the sources most clearly warn against. Vertex animation textures need vertex-stage texture sampling, which WebGL 2 provides, but that last point is from general knowledge and was not checked against a source.
- INFERRED. Enable WebAssembly 2023 and consider native multithreading only after checking hosting headers, since SharedArrayBuffer requires cross-origin isolation; the Unity page quoted does not spell out the header requirement.

### Gaps
- UNKNOWN. A numeric triangle, bone or draw-call budget for Unity 6 WebGL on a Mali-G52: not found in any primary source. The only number is Arm's general 500 draw calls guideline for OpenGL ES.
- UNKNOWN. Arm's "Real-Time 3D Art Best Practices: Geometry" guide: the page (`https://developer.arm.com/documentation/102448/latest/`, redirecting to `support.arm.com`) is a JavaScript shell and returned only its title to two fetch attempts; its content is not quoted here.
- UNKNOWN. Whether CPU skinning of six roughly 4,000-triangle heroes is measurable on the reference phone: not tested.
- UNKNOWN. Current state of GPU skinning on WebGL 2 in Unity 6: the only explicit Unity statement found is from 2017.

## Key question 8: KayKit asset family (Kay Lousberg, CC0)

### Takeaway
KayKit packs are CC0 and already modular in the way this project needs: the Dungeon Pack 1.0 characters in the prototype are rigid four-part puppets with swappable static heads, and the Adventurers 1.0 pack is a 41-joint skinned family with hats and held items as rigid children of bones and a single gradient atlas. Mixing custom Blender parts is straightforward if they follow the same part names, pivots and atlas.

### Cited Findings
- VERIFIED. Adventurers licence: "License: (Creative Commons Zero, CC0)", "This content is free to use in personal, educational and commercial projects.", crediting is "not mandatory". Version 1.0, created 13/03/2023. [LICENSE.txt](https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0/main/addons/kaykit_character_pack_adventures/LICENSE.txt)
- VERIFIED. Adventurers 1.0 README: "4 (+2 in the EXTRA tier) stylised low-poly character assets", "fully rigged and animated", "75 animations", "25+ accessories", "Textured using a single gradient atlas texture (1024x1024) that can be downsampled to 128x128", files in FBX and GLTF. [README](https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0/main/README.md)
- VERIFIED. Skeletons 1.0 README: 4 (+2 extra) skeleton characters, "90+ animations", "10+ accessories", same atlas approach, CC0. [README](https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Character-Pack-Skeletons-1.0/main/README.md)
- VERIFIED (measured in this session by parsing the GLB JSON chunk of files downloaded from the repository). `Knight.glb`: 6,952 triangles in 15 meshes, of which 6 are skinned (body 1,052, head 956, two arms 594 each, two legs 476 each, 4,148 skinned triangles) and 9 are rigid (helmet under `head`, cape under `chest`, weapons and shields under `handslot.l` and `handslot.r`); 1 material, 1 texture, one skin of 41 joints, 76 animations. `Mage.glb`: 5,683 triangles, 4,029 skinned, hat rigid under `head`. `Rogue_Hooded.glb`: 6,035 triangles, 3,921 skinned. Exported with "Khronos glTF Blender I/O v1.7.33". [Knight.glb](https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0/main/addons/kaykit_character_pack_adventures/Characters/gltf/Knight.glb)
- VERIFIED (same parse). The 41 joints are 23 body joints (root, hips, spine, chest, head, and per side upperarm, lowerarm, wrist, hand, handslot, upperleg, lowerleg, foot, toes) plus 18 IK and control joints (kneeIK, heelIK, IK-foot, IK-toe, foot-roll controls, elbowIK, handIK).
- VERIFIED. Dungeon Pack 1.0 as present in the project: rigid characters of 4,224 to 5,511 triangles, 5 or 6 parts, 6 to 9 materials, and 19 extra head and hair meshes; the local animation files use a 6-bone armature (`Body, Head, armLeft, handSlotLeft, armRight, handSlotRight`) with a 4,879-triangle "PrototypePete" mesh. Paths in key question 2.
- INFERRED (snippets; the itch.io pages returned HTTP 403 to the fetch tool and to curl with browser headers). The itch.io Adventurers page now lists a "Free 2.0" download with 5 characters (+3 extra); the KayKit Character Animations pack is "split up per rig type, currently Rig_Medium and Rig_Large", CC0, with version 1.1 adding 28 tool animations; the author says the characters import as Humanoid in Unity by switching the rig type, with a shared `Rig_Medium` avatar available in a free Unity sample. [Adventurers](https://kaylousberg.itch.io/kaykit-adventurers), [Character Animations](https://kaylousberg.itch.io/kaykit-character-animations), [devlog 1.1](https://kaylousberg.itch.io/kaykit-character-animations/devlog/1139588/character-animations-update-11)
- VERIFIED. Project note on rights: the KayKit meshes have an identified CC0 source, while the five `.anim` clips and the controller copied from the Quantum sample still need replacing or licensing. `/Users/red.g/CascadeProjects/chogathon/docs/STATUS.md` (line 380)

### Inferences
- INFERRED. Two KayKit character families coexist under similar names: the older segmented "PrototypePete" family (Dungeon Pack 1.0 and the animation files in the project, 6 transforms) and the skinned Adventurers and Skeletons family (41 joints, 76 or more clips). They are not interchangeable; animations of one do not drive the other.
- INFERRED. For low-end WebGL the older rigid family is the better template for a Chog base: no skinning, four animated transforms, heads and hats swap by re-parenting a static mesh. Its weakness is the material setup (one material per colour), which a custom Chog avoids by using one atlas.
- INFERRED. If the skinned Adventurers family is used instead, join the six skinned meshes into one before export, delete the 18 IK and control joints from the export (Blender's "Export Deformation Bones only"), keep hats as rigid children of `head`, and hide unused weapons: the shipped GLB contains every weapon attached at once.
- INFERRED. Custom Blender parts mix cleanly with either family provided they reuse the same atlas texture and UV convention (so they share the material), sit at the same pivots, and keep the triangle density of the pack (a KayKit hat is 370 to 1,060 triangles).

### Gaps
- UNKNOWN. Contents, rig and triangle counts of Adventurers 2.0 and of the current "Character Pack" listings: the itch.io pages could not be opened (HTTP 403 on `kaykit-adventurers`, `kaykit-skeletons`, `kaykit-dungeon-remastered`, `kaykit-character-animations`, `kaykit-dungeon`), and the GitHub repository still holds version 1.0. A manual browser download is needed.
- UNKNOWN. Licence file of the Dungeon Pack 1.0 copy inside the project: no licence text file was found next to the assets in `Third Party Assets`; the CC0 status rests on the project's own provenance note.
- UNKNOWN. Whether the GitHub organisation hosts the Dungeon Pack and animation packs: the organisation listing API returned 404 and the guessed repository names returned 404.

## Key question 9: Loading NFT images at runtime in WebGL (CORS, gateways, caching, sizes)

### Takeaway
Public IPFS gateways are documented as not for production and were rate-limiting or blocking this machine during the session; the originals are 1000x1000 WebP files that Unity's image loader does not list as supported. The robust path is a pre-resized copy of all 1,969 images on the team's own static host (about 18 MB in WebP at 256 pixels, about 31 MB in JPEG), loaded per match for only the heroes present.

### Cited Findings

Where the images are:
- VERIFIED. Token metadata is at `ipfs://bafybeid4ybujnscdipkno7ps5utqelp5ry5ryreqhay3zhgk3a6x4jruqu/{metadataId}.json` and each image at `ipfs://bafybeihau2egcccbdxymeckmmbrqytbfor5gkt3en6d5fsbueymdbq7g6e/{metadataId}.webp`; the metadata id differs from the ERC-721 token id and must be read through `metadataId()` or `tokenURI()`. `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-metadata.jsonl`, `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-trait-census.json`
- VERIFIED (10 files measured). Images are 1000x1000 lossy WebP (VP8), mean 286,653 bytes (270,286 to 300,742).

Gateway behaviour:
- VERIFIED. Public IPFS gateways are provided "on a best-effort basis. As such, it is not intended to be part of your critical path or production infrastructure."; "Websites should not rely on the ipfs.io gateway for hosting of any kind."; "The IPFS Foundation will throttle or ban users who overuse or misuse community resources."; ipfs.io, dweb.link and trustless-gateway.link "are served by the same pool of servers", so querying all three "burns your rate limit three times as fast". [IPFS docs, public utilities](https://docs.ipfs.tech/concepts/public-utilities/)
- INFERRED (snippets). Pinata: "Public Gateways are not meant for production apps, be sure to only use them for testing!"; dedicated gateways are stated to have no retrieval rate limit. [Pinata docs](https://docs.pinata.cloud/ipfs-101/what-are-ipfs-gateways), [Pinata limits](https://docs.pinata.cloud/account-management/limits)
- VERIFIED (project probe of 2026-10-07). ipfs.io, dweb.link and nftstorage.link returned HTTP 429 at once; gateway.pinata.cloud served 552 metadata files then returned 429 with `retry-after: 3600`. `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-metadata-gateway-probe.json`, `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-metadata.failures.json`
- VERIFIED (probe of 2026-10-09 in this session, one image, browser User-Agent, `Origin` header set). ipfs.io: 403. gateway.pinata.cloud: 429 with `retry-after: 1313`. dweb.link: 429 with `retry-after: 900` and `access-control-allow-origin: *`. w3s.link: redirect to dweb.link, 429. nftstorage.link: redirect to ipfs.io, 403. 4everland.io: redirect then timeout. trustless-gateway.link: timeout. cloudflare-ipfs.com: DNS failure. ipfs.fleek.co: 522. **ipfs.filebase.io: 200, `image/webp`, `access-control-allow-origin: *`, 0.5 to 1.7 s per image**, but 3 of 12 sequential requests timed out at 40 s in one batch (5 of 5 succeeded in a later batch). This machine's address had been crawling the same gateways two days earlier, so a fresh player address may fare better; that was not tested.

Unity side:
- VERIFIED. Cross-origin requests from a Unity Web build need CORS: "The server needs to add Access-Control headers to the http responses it sends out"; UnityWebRequest "uses the JavaScript Fetch API"; blocking waits are not allowed. [Unity 6 manual, Web networking](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-networking.html)
- VERIFIED. `ImageConversion.LoadImage` "Loads PNG, JPG or EXR image byte array into a texture." (EXR only on desktop). "JPG files are loaded into RGB24 format", "PNG files are loaded into ARGB32 format", and "Texture will be uploaded to the GPU automatically". WebP is not listed. [Scripting API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ImageConversion.LoadImage.html)
- VERIFIED. glTFast mentions "KTX/Basis Universal" texture compression support. Reading this as the route to GPU-compressed textures loaded at runtime is the researcher's inference; it was not tested. [glTFast manual](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.14/manual/index.html)

Sizes (measured in this session on the same 10 images resized with Lanczos, then multiplied by 1,969; GPU sizes are arithmetic from bits per pixel, without mipmaps):

| Tile size | WebP q75 each | WebP q75, all 1,969 | JPEG q80, all | PNG, all | ASTC 4x4, all | ASTC 6x6, all | ASTC 8x8, all | RGBA32 each |
|---|---|---|---|---|---|---|---|---|
| 128 px | 3.8 KB | 7.2 MB | 10.7 MB | 59.1 MB | 30.8 MB | 13.7 MB | 7.7 MB | 64 KB |
| 256 px | 9.1 KB | 17.6 MB | 31.1 MB | 216.6 MB | 123.1 MB | 54.7 MB | 30.8 MB | 256 KB |
| 512 px | 32.9 KB | 63.3 MB | 108.4 MB | 890.1 MB | 492.2 MB | 218.8 MB | 123.1 MB | 1,024 KB |

- VERIFIED (same measurement). WebP at quality 60 gives 6.3, 14.9 and 47.6 MB for the three sizes; at quality 85 it gives 9.7, 26.7 and 116.0 MB.
- VERIFIED (arithmetic). A 4096x4096 atlas holds 1,024 tiles of 128 px, 256 tiles of 256 px or 64 tiles of 512 px, so the whole collection needs 2, 8 or 31 atlases; one 4096 atlas is about 7.5 MB in ASTC 6x6, 16 MB in ASTC 4x4 and 64 MB uncompressed.

### Inferences
- INFERRED. The original files cannot be passed to `LoadImage` because they are WebP. Either serve converted JPEG or PNG copies, or decode in the browser through a small JavaScript plugin and upload the pixels to a Unity texture. The first is simpler and removes the gateway dependency at the same time. Not tested in a build.
- INFERRED. A match shows 6 to 10 heroes, so the game should fetch 6 to 10 files of 256 px (about 10 to 16 KB each) at match start, not ship an atlas of the whole collection. Uncompressed in memory that is under 3 MB in total. A whole-collection atlas only makes sense for a gallery screen, and then at 128 px (2 atlases, about 15 MB in ASTC 6x6).
- INFERRED. Decoding a 1000x1000 image at runtime would happen on the main thread of a build whose C# is single-threaded, and would expand to 3 to 4 MB per hero in memory; pre-resizing avoids both. No Unity figure for decode time was found.
- INFERRED. Hosting: a static folder `{metadataId}.jpg` on the same origin as the game (no CORS at all) or on a CDN with `Access-Control-Allow-Origin`. Build it once from a source the team controls. The one public gateway that answered today (filebase) has no stated guarantee and timed out on a quarter of one batch.
- INFERRED. Mirroring and resizing the images is a reproduction of the art, which ties this pipeline to the licence question of key question 11.

### Gaps
- UNKNOWN. Unity WebGL texture decode and upload cost in milliseconds on the reference phone: no primary figure found, not measured.
- UNKNOWN. Whether a marketplace CDN (OpenSea, Magic Eden) serves resized Chog images with permissive CORS: not tested.
- UNKNOWN. Maximum texture size reported by the Mali-G52 in mobile Chrome: not checked; the atlas arithmetic assumes 4096.
- UNKNOWN. Complete image crawl: only 15 images were downloaded in this session; the full 1,969 need a patient, rate-limited fetch or a dedicated gateway.

## Key question 10: Visual language for abilities derived from traits

### Takeaway
The best-documented rule set (Riot's VFX style guide, read here through a secondary summary because the official link is dead) says clarity comes from a primary shape that carries gameplay, mid-range values, restrained saturation, one dominant hue and short durations; mapped to this project that means shape per signature (already five in the catalog) and colour per effect (already ten), built from a few meshes and flipbooks. No numeric particle budget for mobile was found in a primary source.

### Cited Findings
- VERIFIED (secondary summary dated August 29, 2022, quoting the guide). "FX should clearly define areas of focus with primary and secondary elements."; "A visual effect should accurately represent the level of importance of an attack or spell."; "avoid using the extremes of 0 or 100 and lean towards using more of the mid-range" (value); "steer clear from using 100% or 0% saturation"; colour conventions such as "healing spells and effects will always be on the greener side"; "create effects that end up reducing visual noise."; "Timing can make or break an effect."; keep durations short so effects do not block later spells. [VFX Apprentice](https://www.vfxapprentice.com/blog/10-league-of-legends-vfx-design-tips)
- INFERRED (snippet of the official PDF and of Riot's clarity post). The guide's goals are gameplay clarity, less visual clutter, champion theme and delight, organised under gameplay, value, colour, shape and timing; "The amount of attention an ability grabs should match its level of importance"; projectiles "should convey the direction they're traveling, even without motion". [Official PDF, now redirecting to the League home page](https://nexus.leagueoflegends.com/wp-content/uploads/2017/10/VFX_Styleguide_final_public_hidpjqwx7lqyx0pjj3ss.pdf), [Riot, clarity in League](https://riotgames.com/en/news/dev-clarity-in-league)
- VERIFIED. The project's catalog already resolves each recorded token to one of 5 signature shapes (flash 101, wave 144, nova 157, puddle 68, beam 82) and one of 10 effects (raw 281, burn 68, chill 50, hex 47, haste 27, rally 25, mend 17, purify 17, veil 15, toxin 5), with the limit "no combat asset is linked to them yet". `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-catalog-coverage-2026-10-09.json`
- VERIFIED. Flipbooks in the built-in particle system: the Texture Sheet Animation module "lets you treat the Texture as a grid of separate sub-images that can be played back as frames of animation", with Grid or Sprites mode, whole sheet or single row, and lifetime, speed or FPS time modes. [Unity 6 manual](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysTexSheetAnimModule.html)
- VERIFIED. Mesh particles: "Unity renders the particle from a 3D Mesh instead of a Billboard."; mesh GPU instancing "requires the use of a compatible shader". [Unity 6 manual](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysRendererModule.html)
- VERIFIED. "To minimize overdraw, reduce the number and/or size of particles in your game."; transparency "always uses more GPU resources" than opaque; avoid too many dynamic lights. [Unity mobile tips](https://unity.com/how-to/mobile-game-optimization-tips-part-2)
- INFERRED (snippet of legacy Unity manuals, 2017 to 2019). "On mobiles you're essentially fillrate bound (fillrate = screen pixels * shader complexity * overdraw)" and "use the simplest possible shaders for particles". [Unity 2019.1 manual](https://docs.unity3d.com/2019.1/Documentation/Manual/MobileOptimisation.html)
- VERIFIED. The project already contains Cartoon FX Remaster effect libraries under `Third Party Assets/JMO Assets` and `JMO Assets New`, and the sample's damage-zone prefabs use 3 particle systems each. `/Users/red.g/CascadeProjects/chogathon/game/Assets/TwinStickShooter/Prefabs/VFX/DamageZoneVFX.prefab`

### Inferences
- INFERRED. Mapping: shape tells the player what will hit them, so the five signature shapes each get one fixed silhouette and telegraph (flash: short cone, wave: travelling arc, nova: expanding ring, puddle: ground disc, beam: line); colour tells them which effect it carries, so the ten effects each get one hue kept in the mid-value, sub-maximal saturation band, with "raw" neutral. Five meshes plus ten colours give the 50 combinations without 50 assets.
- INFERRED. Trait colour should not replace effect colour. If the Aura or Background colour tinted the ability, two heroes with the same effect would look different and two different effects could look the same, which breaks the rule that the primary element communicates gameplay. Trait colour belongs on secondary elements: the cast flourish, the trail, the hero's ring.
- INFERRED. Blender to Unity workflow for one artist: model each shape as a low-poly mesh with UVs laid out for a scrolling texture (arc, ring, cone, disc, cylinder), export FBX, and drive it with a simple unlit additive or alpha-tested shader whose UV offset, tint and dissolve threshold animate over the lifetime; add one or two 4x4 flipbooks (impact puff, spark) rendered from Blender or taken from the Cartoon FX library already in the project. Mesh effects have fixed, small overdraw compared with clouds of billboards.
- INFERRED. Working budget to validate on the phone rather than a sourced limit: one mesh and at most a handful of particles per active ability, no dynamic lights, no soft particles, effects shorter than one second except ground zones.

### Gaps
- UNKNOWN. A numeric particle or overdraw budget for mobile WebGL from Unity or Arm: not found.
- UNKNOWN. The official Riot style guide text: the Nexus article and its PDF both redirect to the League home page today, and web.archive.org could not be fetched from this environment. Quotes above come from a secondary summary and from search snippets.
- UNKNOWN. Licence terms of the Cartoon FX assets inside the Quantum sample for a public build: not checked in this session.

## Key question 11: IP and licence for using each holder's NFT art in the game

### Takeaway
No public statement of holder rights or art licence for Chog Genesis was found. The only rights language on chog.xyz is "All rights reserved", and the hackathon site sets no rule about art use beyond declaring pre-existing work. Written confirmation from the Chog team is needed before mirroring images, shipping them in a build, or sending them to a third-party generator.

### Cited Findings
- VERIFIED. chog.xyz has no terms, licence, legal, privacy, docs or FAQ page: `/terms`, `/tos`, `/license`, `/licence`, `/legal`, `/privacy`, `/docs`, `/faq`, `/robots.txt` and `/sitemap.xml` all return HTTP 404. The footer reads "© 2026 Chog. All rights reserved." The home page links to the PFP maker, meme maker, snapshot tool, messenger, swap, team page, `portal.chog.xyz`, Discord and X. [chog.xyz](https://www.chog.xyz/)
- VERIFIED. The home, team and portal pages contain no occurrence of "terms of", "privacy policy" or "license agreement" in their server-rendered HTML (checked with grep on the fetched pages; text injected later by JavaScript was not inspected). [portal.chog.xyz](https://portal.chog.xyz/)
- VERIFIED. The hackathon page text contains none of the words licence, intellectual, IP, artwork, assets, rights, brand or trademark. Its relevant requirements are: "Share your repository, tech stack, and any pre-existing work you used.", "The Chog NFT must be essential.", and the tagline "take the NFT out, does it still work?". [chogathon.chog.xyz](https://chogathon.chog.xyz/), `/Users/red.g/CascadeProjects/chogathon/research/evidence/chogathon-site-facts-2026-10-09.json`
- VERIFIED. Token metadata contains name, description, image and attributes only; no licence field. The contract source carries an MIT identifier for the code, which says nothing about the art. `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-metadata.jsonl`, `/Users/red.g/CascadeProjects/chogathon/research/evidence/genesis-ChogMint.sol`
- VERIFIED. The site itself offers tools that remix Chog art (a PFP maker and a meme maker) with publicly served, CORS-enabled layers. [PFP maker](https://www.chog.xyz/maker?tab=pfp)
- INFERRED (secondary legal commentary, general, not about Chog). Absent explicit terms, buying an NFT does not transfer rights in the associated artwork, and what a holder may do depends on the project's licence. [Fintech and Digital Assets blog](https://www.fintechanddigitalassets.com/2023/04/key-legal-considerations-in-commercial-rights-licenses-for-content-based-nfts/)
- INFERRED (snippet). Third-party descriptions present ChogNFT as a community project that "blends meme culture, IP development, and community activations"; this does not state holder rights. The sentence came from one of two versions of the same third-party listing; which one was not recorded. [Monad NFT tier list](https://teletype.in/@artofresearch/monad-tier-list-nft), [updated version](https://teletype.in/@artofresearch/monad-tier-list-nft-updated)

### Inferences
- INFERRED. A game made for the collection's own hackathon, which requires the NFT to be essential, will very plausibly be welcome to display the collection's art; but plausibility is not permission, and nothing public covers copying the images to another host, modifying them (cropping, resizing, cutting layers), or uploading them to an AI service.
- INFERRED. The lowest-risk technical posture until an answer arrives: display each image unmodified apart from scaling, only for tokens present in a match, with the collection credited; avoid redistribution of extracted layers; do not send images to third-party generators.
- INFERRED. One message to the organisers can settle four items at once: permission to display and mirror the Genesis images in the game, permission to crop or mask them, availability of the Genesis layer files, and whether the PFP maker layers may be reused.

### Gaps
- UNKNOWN. Chog's position on holder rights (personal, commercial, derivative): not found on chog.xyz, the portal, the hackathon site, token metadata or web search. Marketplace collection pages and the project's Discord and X were not read.
- UNKNOWN. Whether hackathon rules outside the public page (Discord announcements, sign-up form) say anything about art use.
- UNKNOWN. Who owns the copyright in the Genesis art (team entity, artist): not found.

## Key question 12: Ranked comparison of the options for this team

### Takeaway
Ranked for one Blender artist, two weeks and a Mali-G52 phone: (1) Option C hybrid, built so that its first milestone is Option B; (2) Option B alone; (3) a reduced Option A; (4) Option D. The ranking rests on three verified facts (bust-only art, 1:1 tokens without traits, and a prototype whose cost is material slots rather than skinning) and on estimates that four cheap tests can confirm or overturn.

### Cited Findings
- VERIFIED. Judging weights: Novelty 30%, Fun 20%, Retention 20%, Clarity 15%, Execution 15%, with the requirement "The Chog NFT must be essential." Derived end of the hackathon: 2026-10-23 15:00 UTC. `/Users/red.g/CascadeProjects/chogathon/research/evidence/chogathon-site-facts-2026-10-09.json`
- VERIFIED. Art facts: 1000x1000 bust images; Legendary 1:1 tokens have no trait values; 552 of 1,969 metadata entries recorded (key questions 2, 3).
- VERIFIED. Prototype facts: 0 skinned meshes, 18 to 21 material slots and 5 to 6 shadow-casting renderers per hero, 4,224 to 5,511 triangles per hero; 17 to 18 ms of non-simulation work on the phone, not broken down (key questions 2, 7).
- VERIFIED. Engine facts: no batching or instancing for skinned meshes, no SRP Batcher in the built-in pipeline, CPU-side WebGL dispatch slower than native, Arm guideline under 500 draw calls for OpenGL ES (key question 7).
- VERIFIED. Supply facts: public gateways unreliable and WebP not in Unity's loader list; resized copies of the whole collection weigh 18 to 31 MB at 256 px (key question 9).
- VERIFIED. Cost facts for generation: 15 to 35 Meshy credits per textured, rigged asset at 0.0125 to 0.02 USD per plan credit; Tripo P1 50 credits; Rodin about 0.29 USD per model on Business (key question 5).
- VERIFIED. Licence fact: no public statement of rights for the art (key question 11).

### Inferences

Comparison (all cells are the researcher's estimates unless a number repeats a verified finding):

| Rank | Option | Effort for one Blender artist in two weeks | How recognisable the holder's own NFT stays | Performance on low-end mobile WebGL | Risk |
|---|---|---|---|---|---|
| 1 | **C. Hybrid**: shared rigid Chog body with one atlas material, the real image as portrait disc and face plate, Skin and Background tint, aura effect, optional headwear families | About 6 to 9 artist days for body, fit to clips, shaders and aura; headwear families as stretch. Needs the image pipeline from engineering | High: the exact art is on screen for every token including 1:1s, and the body adds colour identity | Good: about 5 draw calls for the body (fewer if batched), plus 1 or 2 quads; no skinning | Medium-low: depends on image hosting and on the licence answer; visual fit of a square bust on a round head needs one test |
| 2 | **B. 2D standee**: one quad with the image, procedural motion, blob shadow, outline | 1 to 3 days, mostly engineering and shader work; almost no modelling | Highest for the art itself; no body language, no facing | Best: 1 quad per hero, batchable, no skinning | Medium: may read as low effort on Execution and Fun unless stylised; same hosting and licence dependency |
| 3 | **A. Modular 3D (reduced)**: Chog base plus a handful of part families and palettes, no image | About 8 to 12 days for base plus 6 to 8 families; full fidelity (about 50 meshes, 150 variants) does not fit | Medium to low: "a red Chog with a cap", prints and costumes missing; 1:1 tokens not representable | Good if one atlas material and rigid parts; worse if skinned multi-part | Medium-high: schedule risk, unknown values in the unrecorded 72% of metadata, holders may reject an approximation |
| 4 | **D. Generated 3D** | Little modelling, but generation, clean-up, rig checks and QA for 1,969 unique assets; about 500 to 1,400 USD | Unknown: no test on bust-only flat cartoon art | Fair to poor: unique mesh and texture per hero, CPU skinning, per-match GLB downloads, extra package | High: untested quality, cost, hosting, licence of inputs, one open model excluded in the EU |

Why this order:
- INFERRED. Recognition is what the event scores ("The Chog NFT must be essential"), and the only way to show the 1:1 tokens, the 21 costume silhouettes and the 27 cap prints inside two weeks is to show the image itself. That puts B and C ahead of A.
- INFERRED. C ranks above B because a 3D body gives facing, animation and hit feedback that a flat quad cannot, at a cost of a few rigid parts, and because C can ship as B on day two and improve from there, so it has no worse failure mode than B.
- INFERRED. A ranks above D because its output is controllable and cheap at runtime, whereas D has an untested core assumption and the highest count of independent risks.

Errors or weak premises found in the brief, for the report writer:
- INFERRED from verified facts. "Draw calls and skinned meshes are expensive" is half supported: the draw-call half fits the evidence (18 to 21 slots per hero), the skinned-mesh half does not apply to the current build (no skinned mesh exists in it).
- INFERRED. "The current prototype uses KayKit Dungeon Pack 1.0 characters" is correct, and those characters are a rigid family distinct from the skinned Adventurers family the brief lists alongside it.
- INFERRED. "Nifty Island (playable avatars from 2D NFTs)" overstates what the sources show (studio-made avatars per partner collection).

Gates before committing, each with its fastest test:
1. INFERRED. Does cutting material slots move the 17 to 18 ms? Test: one build with six heroes whose parts share a single material (even a flat grey one), same phone, same overlay. Half a day. If the non-simulation time does not drop, the bottleneck is scripts or UI and hero art choice matters less for performance than assumed.
2. INFERRED. Does the portrait read at game scale? Test: six camera-facing quads with real Genesis images at the intended world size, screenshot on the phone. Two hours.
3. INFERRED. Can images arrive reliably? Test: 20 resized JPEG files on the game's own host, load 10 at match start on the phone over mobile data, record time to texture. Half a day.
4. INFERRED. Is the use allowed, and are layers available? Test: one message to the Chog team today.

Flip conditions:
- INFERRED. If the Chog team supplies the Genesis layers and their order: Option C gains a background-free head cut-out and Option A's prints become texture swaps, so a C plus A blend (image-accurate head on a 3D body with matching 3D headwear) becomes realistic.
- INFERRED. If the Chog team refuses image use in the build: fall back to reduced Option A driven by traits only, and state plainly that 1:1 tokens get a bespoke stand-in.
- INFERRED. If gate 1 shows no gain from fewer draw calls: keep the existing KayKit bodies, add only the portrait disc and tint (the smallest possible C), and spend the artist's time on abilities and readability instead.
- INFERRED. If a ten-token trial of a hosted generator gives clean, consistent Chogs under 4,000 triangles: Option D could be reconsidered for a post-hackathon version, not for the two-week window.

### Gaps
- UNKNOWN. All effort figures are estimates by the researcher; no source measures one-artist throughput for this kind of work.
- UNKNOWN. Frame-time effect of each option on the reference phone: nothing was measured in this session; the only measured figures are the project's own (STATUS).
- UNKNOWN. How judges of this event weigh "exact art shown" against "bespoke 3D character": no past edition or judge statement was found.
- UNKNOWN. iOS Safari and mid-range Android behaviour: not measured by the project and not researched here.
