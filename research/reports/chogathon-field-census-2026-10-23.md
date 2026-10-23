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
