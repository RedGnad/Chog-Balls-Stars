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
