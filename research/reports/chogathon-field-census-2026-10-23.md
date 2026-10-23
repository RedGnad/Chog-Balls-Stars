# Chogathon field census, 23 October 2026 (about 00:30 UTC, 14.5 hours before the close)

Read from the hackathon's own gallery (`python3 -I research/scripts/census_chogathon_gallery.py`, fixed that night to select the
columns the page selects: `select=*` takes the base64 banners and times out with HTTP 500). Raw rows:
[chogathon-gallery-2026-10-23.json](../evidence/chogathon-gallery-2026-10-23.json). Facts are what the projects' own pages and
descriptions say, plus whether their app answers; claims were not verified on chain or in code unless said.

## The field

- **110 rows, about 70 distinct projects** once the same title submitted several times is counted once.
- **Our project appears four times** (rows of 23:36, 23:50, 23:54 and 23:56 UTC on 22 October): three are to delete from My
  Projects (one project per team).
- Public hearts: 88 in all, at most 5 for a project. Not a signal: judges score after the close (Novelty 30, Fun 20, Retention 20,
  Clarity 15, Execution 15; the NFT gate must pass).

## Closest to us (games where the Chog is the fighter, multiplayer, wagers); every app answered HTTP 200

| Project | What it says it is | Overlap with us |
| --- | --- | --- |
| CHOG : ROOM ZERO (`chogroomzero.xyz`) | tactical top-down multiplayer shooter, solo, 1v1, 3v3, 5v5; traits (Laser Eyes, Visor, Bubblegum, Drooling, Aura) map to abilities, rarity to range, duration, cooldown; 5 locations, 7 operators; a polished landing | traits as powers, rarity as strength, team multiplayer |
| Chog Wave (`chogwave.online`) | real-time multiplayer RPG (Babylon.js), card-battler duels with traits as stats, dungeons, parties, **"wager real $CHOG in PvP brawls"** | the bet on PvP (not verified) |
| Grand Theft Chog (`grandtheftchog.duckdns.org`) | a live city where every holder walks as their Chog; traits pick one of 20 gangs; three-stage heists, police, jail; replays from server-checked moves; records on the token; $CHOG burned for bail and wanted posters | live multiplayer, server-checked replays, records per token |
| CHOG ARENA (`chog-arena.online`) | 3D (React Three Fiber) tap-and-battle, 3-lane card battles; traits set archetype, element, stats, 3D look and cards; ranked wins replayed on the server and signed (EIP-712), seasons on Monad | 3D Chogs from traits, server replay, on-chain records |
| Chog Arena (`chog-arena-nine.vercel.app`) | 3D auto-battler, 3 Chogs a squad, "Verify" re-runs a fight in the browser, Elo ladder, per-Chog XP | traits as fighting style, ladder |
| ChogTrack (`chogpool.vercel.app`) | skill racer as your Chog; stake USDC, weekly top three split the pot; every run re-simulated from its seed and input tape on a Proof page (Monad testnet) | stakes paid on a replay anyone can recompute |
| CHOG KINGDOM (`chogkingdom.xyz`) | three mobile-first multiplayer board games in rooms of 2 to 6, a shared ladder, Chog abilities for holders (.NET 8, SignalR) | mobile, rooms, ladder |
| Dybbo (`dybbo.vercel.app`) | "possess" any Chog for 1 to 10 minutes and pay its holder per minute; the NFT never moves | renting a Chog |

Others in games: Chog warr (turn-based duels), Chog brain, Chogbound, CHOG RAID, Chog Chaos League, Chog Escape Protocol, Chog Run,
Chog Dash (3D runner), Jetpack Chogride, The Chog World (pixel MMORPG), CHOG SURVIVOR, superchog, ChogSwipe, Playhouse, Chog Mischief.

## Where we stand (inference, to challenge)

- No other entry says it is a real-time 3v3 team brawler with towers and cores, on deterministic netcode, with all 1,969 Genesis
  modelled in 3D from their traits (CHOG ARENA draws 3D Chogs from traits; the others use the 2D art or pixel art).
- The bet: Chog Wave claims $CHOG wagers in PvP and ChogTrack stakes USDC on testnet with a public re-simulation; ours stakes
  $CHOG on Monad mainnet, judged by a server replay of Photon's record and paid by the contract.
- Renting: Dybbo has the stronger "the NFT never moves" story; ours also never moves the NFT, rents by the day, lends free by link.
- Judges read the description, the video and the links first: the video and a first-minute path for a judge decide Clarity and Fun.

## By judging criteria (two read-only audits, 23 October, about 00:30 to 01:20 UTC)

Scores are one auditor's estimates on 1 to 5 against the published weights (Novelty 30, Fun 20, Retention 20, Clarity 15,
Execution 15); they are bands, not a ranking. Facts marked verified were read from the project's own app, API, bundle,
repository or chain; nothing was signed, connected or submitted.

| Project | N | F | R | C | E | NFT gate | Verified facts that matter |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Grand Theft Chog | 4 | 4 | 5 | 4 | 5 | strong | playable without a wallet (2D city, NFT pictures as pieces); `/api/war`: 20 gangs, 20 members over 12; the last 30 robberies all by #326; repo `ANUBIS-cp/grand-theft-chog`, 119 commits, tests and CI; claimed usage (7 holders, 460+ guests) not verified, "0 playing now" when read |
| Chog Wave | 4 | 3 | 3 | 2 | 4 | likely passes | three.js (not Babylon.js as stated); `/models/chog-base.glb` plus `trait-parts.json`, 236 part models (Head 67, Body 96, Eyes 58, Mouth 15), 307 colour sets, flat materials: per-Genesis 3D from traits; the wager "vault" `0x98E162D2beb358fdD74F833d2618fc905Bb524d3` has no code on Monad (a plain wallet, 7 transactions, 0.205 $CHOG); `/api/leaderboard` empty; page says "Monad testnet" while `/health` says chain 143; Privy login wall; no public repo |
| CHOG : ROOM ZERO | 3 | 4 | 3 | 4 | 4 | weakest | guest dropped into a match with bots at once (PixiJS 2D, not Next.js); real mechanics (heard noise, closing zone, grenades); `wss://room-zero-game.onrender.com/ws` answers; solo board 7 players, online board 1 (the author); the Chog's look never used (generic operator sprites), the NFT adds one power and 5v5; repo `levithefirst/chog-room-zero` holds docs only |
| ChogTales | 4 | 2 | 4 | 5 | 3 | passes | three demo Chogs without a wallet; Groq writes, FLUX.2 draws from the NFT image; `ownerOf` checks; `/api/library`: 8 chapters, all by the project or demo users, 0 reads; repo `Milliebanned/chogtales`, 18 commits |
| Dybbo | 5 | 2 | 2 | 4 | 5 | passes | verified contract with no transfer or approve functions (the NFT never moves); best judge pages |
| ChogChoir | 5 | 3 | 3 | 4 | 4 | passes | no mainnet use found |
| Chog League | 3 | 3 | 4 | 5 | 4 | passes | no league players; treasury 284.6 $CHOG |
| CHOG ARENA (chog-arena.online) | 3 | 3 | 4 | 3 | 2 | passes | contracts not deployed, attestations off, placeholders in the description; one procedural plush model, not per-token 3D |
| CHOG KINGDOM | 2 | 3 | 3 | 3 | 4 | borderline | no bots, so a judge alone cannot start a match; the NFT is optional |
| Chog ToolHub | 1 | 1 | 3 | 2 | 3 | at risk | tools, not a game |

Read only on the surface, not scored: Chogbound (contract `0x16D687505037a0A3E2B882F2bBa45E772807BEde` on mainnet holds
0 $CHOG), Chogsticks (contract `0x430d...3d04` holds about 51.58 $CHOG), The Chog World, ChogTown, CHOG-TWAIN, CHOG STUDIO.

Ours, read the same way: `/api/career/top` lists one Genesis career (#1795, 3 confirmed matches, the owner's). Like every
rival above, no outside usage. What only we show working end to end: a real-time team match, judged by a server replay of
Photon's record in about 14 s, and $CHOG stakes held by a contract (refundable after an hour) rather than a server wallet.
What a rival now shares: per-Genesis 3D from traits (Chog Wave), a "the server replays every run" fairness story (Grand
Theft Chog), instant play without a wallet (ROOM ZERO, Grand Theft Chog).

### Added after the owner's question (23 October, about 01:45 UTC): the two "World" entries

| Project | N | F | R | C | E | NFT gate | Verified facts that matter |
| --- | --- | --- | --- | --- | --- | --- | --- |
| The Chog World (`chog.world`, @sebgroegb) | 4 | 3 to 4 | 5 | 3 | 4 | strong | Phaser 4 with a live server (`api.chog.world`, Colyseus); `/players-count` answered `{"online":0,"total":5}`; `/access-rules`: one Genesis needed to enter (spectators otherwise); traits as perks in the code (Laser eyes zap with L, Spartan Helmet, immune to lasers, Left side deals the roulette); in-world look: one hand-drawn base Chog recoloured by body colour, plus sheets for Skull, Radioactive and ten 1:1s (`assets/data/assets.json`), no trait drawn on the sprite in the public bundle; the NFT image is used as a portrait; $CHOG not spent on chain (sign-in only, the $CHOG to $MX conversion is announced, not live); repo private |
| Chug world, "CHOG World" (`chog-world-mainnet.vercel.app`, @Sabercodes123) | 5 | 2 | 4 | 4 | 4 to 5 | very strong | an ERC-6551 wallet and an AI agent per Genesis, every move checked by a policy contract; on Monad: ChogOperator `0xaDaA7f21Fb843140E20115D84F51F6f85D5342D1`, Treasury `0x1814943bc12E643D1477b47F0B3cD495Fe07fce6` (holds exactly the 390 $CHOG seed, so no fee or payout yet), ChogState `0x33B273B62d3e364dc1Ec33aC2800AAeAb1067618`, all with code; their own text says no holder has played on mainnet yet (testnet: 7 Chogs, 14 battles); 94 Foundry tests claimed, not read |

A third, "Chog World: Your Chog, Your Identity" (`chognftgame.netlify.app`), is a small social island with a two-line
description and no video: not audited. Lesson: the gallery spells one of them "Chug world"; search the titles by
keyword variants, not by the names heard.

Our own proof, read at 01:28 UTC (`python3 -I research/scripts/count_wager_events.py`): ChogWager v3 has seen one
transaction since its deployment (block 115733839 to 115766306, its setup) and no stake; `/api/season` answers
`{"enabled":false}`. Our main difference, a bet paid by a contract, has nothing on chain yet for a judge to see.

## Second census, 09:19 UTC (5 h 40 before the close)

`research/evidence/chogathon-gallery-2026-10-23-0919.json`: 134 rows, 84 distinct titles (110 rows at 00:30). New since
then, read from their descriptions only (apps answered HTTP 200, ChogSekai 307): ChogPit (real-time extraction shooter,
each token seeds its own raidable pit; its Raider Pass `0x6655F23193142CC1E323522C363A52CDea8E3CE2` has code on Monad,
5 minted of 40), Animr (each Chog an AI trader with an ERC-6551 wallet, 0G Compute, a judge tour), ChogSekai (Godot 3D
continent, 1,969 Chogs as AI agents, 24-hour seasons), monorb (Three.js 3D adventure and PvP; "everyone plays as a
guest, so any of the 1,969 Chogs can appear": weak on the NFT gate), Soryn (tactical creature battler, 500 $CHOG or a
Chog unlocks multiplayer), Chog Swarm (autonomous racing agents compiled from traits), Choguild, Chogslist, chogOS,
DeClub, ChogQuest, Chog Hideout, Chog Beauty Arena, Etalabs, Jetpack Chogride, CHOG Genesis Card Checker.

A judge's ranking, weighted by the published criteria (one auditor's estimates; ours self-assessed):
Grand Theft Chog about 4.35; then a close group between 3.8 and 4.0: Chug world, The Chog World, ChogChoir, ours (about
3.9), with Animr likely there (not audited); then ChogTales, Dybbo, Chog League about 3.6 to 3.65, ChogSekai and ChogPit
likely near (not audited); ROOM ZERO about 3.5 with its NFT gate at risk; Chog Wave 3.3; CHOG ARENA 3.05; CHOG KINGDOM
2.85; Chog ToolHub 1.85.

### Grand Theft Chog played through, 09:35 UTC (guest, practice vault, read-only)

The owner doubted the earlier scores ("just mini-games?"). Played end to end with a headless browser: the city is 2D
with each Chog shown as its flat NFT picture on a stand; a heist is two timing mini-games ("Stop the reels", "Pick
the lock": tap when the pin is in the gold band) then a 75-second top-down maze in the vault (grab coins in runs,
guards drawn as Chog pictures, Q hides, E fires laser eyes, crack the safe, get out before the doors lock), close to
Pac-Man. Polished screens and a coherent loop, no real-time fight between players' heroes. The earlier Fun 4 and
Retention 5 leaned on what its pages claim (seasons, live defence, events); scored on what was seen: Novelty 3.5,
Fun 3, Retention 4, Clarity 4, Execution 4.5, about 3.7, inside the same band as the next four, ours included.
The 1-to-5 scores of this report are one auditor's judgments without an anchored grid: read them as bands.

## Anchored grading, 23 October about 10:00 UTC (asked by the owner)

The grid, fixed before grading. A level counts only on what was seen: played, read in the app, its code or its API,
or checked on the chain. A project's own claims earn nothing. Where an NFT wall kept us out, the code is the evidence
and the grade is marked "not played".

| Level | Novelty (30) | Fun (20) | Retention (20) | Clarity (15) | Execution (15) |
| --- | --- | --- | --- | --- | --- |
| 1 | a reskin | no loop | nothing kept | not understood in a minute | broken |
| 2 | known genre, one NFT twist | passive (idle, read, listen) | one stat kept | understood with the docs | partial, placeholders |
| 3 | the NFT drives a core mechanic | short active loop, mini-games | progression that works | understood in the app with effort | works end to end, rough |
| 4 | a combination at most one other entry shares | real-time play with skill and choices | progression plus a social or economic loop that works | clear pitch and a guided first minute | polished, live services, its chain parts checked |
| 5 | a new category, nobody else, seen in use | 4 plus humans against humans seen | 4 plus players seen coming back | 4 plus proofs at hand and nothing confusing | 4 plus public tested code and its chain flows really used |

| Project | N | F | R | C | E | Weighted | Main evidence | Not seen |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Chog Balls Stars (ours, self-graded) | 4 | 4 | 4 | 4 | 4 | 4.00 | real-time 3v3 in our captures, 1,969 heroes in 3D, careers on chain, season 1 live, contracts with code, public tested repo | humans against humans today; a stake on wager v3 (none yet) |
| Animr | 4 | 3 | 3 | 5 | 4 | 3.75 | a 9-stop judge tour without a wallet; two contracts with code (`0x7720Ed65...513d`, `0x92D3d4Af...987b`) | the trading loop and the Daily Bell working |
| Grand Theft Chog | 3 | 3 | 4 | 4 | 4 | 3.50 | played through: 2 timing mini-games and a maze; `/api/war` live; public repo with tests and CI | live defence by gang members; burns on chain |
| The Chog World | 4 | 3 | 4 | 3 | 3 | 3.50 | bundle: perks from traits, casino, trading floor, houses; `/players-count` 0 online, 5 in all | not played (NFT wall) |
| CHOG : ROOM ZERO | 3 | 4 | 3 | 4 | 4 | 3.50 | played: a real-time shooter, fun at once; boards of 7 and 1 players | NFT gate at risk: generic sprites, the NFT adds one power |
| ChogChoir | 4 | 3 | 3 | 3 | 4 | 3.45 | each Chog sings from its traits (landing played); choir rounds on testnet and mainnet | mainnet use (none found) |
| Chug world (CHOG World) | 4 | 2 | 3 | 4 | 4 | 3.40 | three mainnet contracts with code; treasury still at its 390 $CHOG seed | its agents and progression working |
| Chog Wave | 3 | 3 | 2 | 2 | 3 | 2.65 | 236 trait parts in 3D; a custodial "vault"; empty leaderboard; login wall | its card duels |

One grade point on one criterion moves a project by 0.15 to 0.30: the grid separates our group from Chog Wave, not
the second place from the seventh. If our Fun and Execution were 3, we would be at 3.65, still in the group.

### CHOG STUDIO on the same grid (10:15 UTC)

Two entries carry the name. CHOG STUDIO (`chogstudio.xyz`, @Dayzer__): an AI film studio where the holder's Genesis is
the actor (a 3D actor made from its portrait, a persona from its traits, a voice), photos, 5 to 30 s clips (a "replace"
mode swaps the person of a viral video for the Chog), episodes up to 2 minutes, paid in $CHOG per take. Seen: Chog TV
(`/tv`, public) holds 9 clips and episodes starring 4 Genesis (#141, #1578, #1751, #1752) with votes (3, 2, 1) and a
"top this week"; the clip on screen shows a convincing clay-like 3D Chog; `/api/pricing` answers live prices with the
$CHOG rate and a 25% markup. Not seen: making a take (it needs a Genesis), the $CHOG deposits on chain, its repository.
Grades: Novelty 4 (ChogTales is the one other entry making AI content from the NFT), Fun 3 (a creative loop, not a
game), Retention 4 (filmography per token, votes, weekly theme, seen working), Clarity 4, Execution 4: **3.80**, second
behind ours. The other, "Chog studio" (`chogstudio-pi.vercel.app`, @oxkael_), is a comic editor without AI or chain
writes: 2, 2, 2, 4, 3, about 2.45.
