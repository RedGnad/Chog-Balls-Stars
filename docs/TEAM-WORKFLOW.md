# Working as several agents at once (from 21 October 2026)

Several agents work on this repository at the same time, with few days left (submission 23 October 2026,
15:00 UTC). This page is the contract between them. `docs/HANDOFF.md` holds the context and the house rules;
this page only adds how to share the work. The lead (the owner's main session) integrates: it merges, builds
for production and deploys. Nobody else deploys.

## Territories

Each workstream owns a set of files. An agent edits only its own territory; when a change outside it is
needed, it writes the exact change it wants in its report and the lead does it.

| Stream | Owns | Heavy jobs |
| --- | --- | --- |
| A. Siege map and rules (lead) | `game/Assets/QuantumUser/**`, `game/Assets/QuantumUser/Scenes/GameplaySiege*`, `tools/quantum/**`, `tests/fixtures/replays/**`, `game/Assets/Chog/Match/StructureViews.cs`, `MatchModeChoice.cs`, `ArenaScenes.cs` | Unity builds |
| B. Heroes, hats and costumes | `tools/blender/chog_trait_hats.py`, `chog_trait_bases.py`, `chog_one_of_one.py`, `make_chog_body*.py` | Blender |
| C. Heroes, eyes and mouths | `tools/blender/chog_trait_faces.py` | Blender |
| B and C together | `tools/blender/make_chog_traits.py`, `export_chog_parts.py`, `game/Assets/Chog/Resources/ChogParts.bytes`, `tools/blender/README.md` (its own section each) | one export at a time |
| D. Attacks and effects from the art | `game/Assets/Chog/Match/ChogAttackLook.cs`, `GenesisAttackView.cs`, `GenesisSpecial*.cs`, `GenesisEffects.cs`, `ChogFx*.cs`, `ChogLight*.cs`, `ChogImpact*.cs`, `ChogHeroModel.BuildAttack`, `tools/blender/chog_attack_objects.py`, `docs/ATTACKS-FROM-THE-ART.md` | Blender (objects), Unity builds |
| E. Ladder, rating, seasons | `server/src/ranks.js`, `rank-store.js`, `matches.js` (rank parts), `server/test/ranks*.js`, `game/Assets/Chog/Match/Wallet/PlayerRanks.cs`, `docs/POST-MATCH-PROGRESSION-2026-10-20.md` | none |
| F. $CHOG economy: stakes, rental, shop | `server/src/wager.js`, `rental.js`, `shop.js`, `chain.js`, `contracts/**`, `game/Assets/Chog/Match/Wallet/**` (except PlayerRanks), `game/Assets/Chog/Screens/WagerPanel.cs`, `WalletPanel*.cs`, `ChogHeroSheet.cs`, `ChogCollection.cs` | none, final build by the lead |
| G. Result and home screens (the owner's other assistant) | `game/Assets/Chog/Screens/ChogResultScreen.cs`, `ChogHome.cs`, `MasteryRoad.cs`, `game/Assets/Chog/Match/ChogHeroStage.cs` | Unity builds |

Shared and owned by the lead: `ChogTheme.cs`, `ChogUi.cs`, `ChogIcons.cs`, `ChogSprites.cs`, `docs/STATUS.md`,
`docs/SUBMISSION.md`, `site/**`, `game/ProjectSettings/**`. An agent that needs a new theme colour, icon or
shared widget asks the lead for it in its report.

## Branches

- Each stream works on its own branch, named after it: `stream/siege-map`, `stream/heroes-hats`,
  `stream/heroes-faces`, `stream/attacks`, `stream/ladder`, `stream/economy`, `stream/result-home`.
- Commit small and often, with explicit paths (`git add <files>`, never `git add -A`); push the branch.
- Rebase on `main` before reporting a lot ready (`git fetch && git rebase origin/main`), fix conflicts in your
  territory only. Never push to `main` and never merge another branch into yours.
- A lot is ready when: tests pass (see HANDOFF), the evidence exists (sheets, captures, logs), and the report
  says what was verified and what was not. The lead merges, builds, checks in a match and deploys.

## Heavy jobs: one at a time on this Mac

- `.artifacts/heavy-job.lock`: wait for `free`, write `busy <stream>`, run the job, write `free` at once.
  Blender renders, Unity builds and headless browser captures all count. Never hold the lock while thinking.
- One Blender process at a time, `nice -n 10`, `-t 4`. One library export at a time (B and C coordinate
  through the lock, and each re-exports after rebasing on the other's commit).
- Unity builds: a build of a branch ships that branch's tree only; never request one while another stream's
  simulation change is uncommitted in your tree (it is not, if you are on your branch).
- Servers on your own port (B 8098, C 8094, D 8099, G 8093, E and F 8092), stopped by port.

## Method: meticulous, grounded, no breaking

- Change the base when you know what it brings, and say what it brings in the commit; never break what works:
  every existing test keeps passing, every screen you touch is captured after the change, every rule you
  change has a before/after.
- References first: for a design or a game rule, read what the references do (Brawl Stars, Clash of Clans,
  Supercell's own posts; for the economy, live products) and cite the source in the report. Never a value
  chosen at random: write why.
- 3D and visuals: start from the real art (`.artifacts/genesis-art/256/`), several Genesis per value, judged
  from every angle at game size, with a before/after sheet beside the art. The owner judges on the sheet.
- Evidence in `research/evidence/<stream>-<topic>-<date>.{jpg,png,md}`, small enough to commit.
- Simulation changes (anything under `game/Assets/QuantumUser/Simulation`) change the Photon version: only
  stream A makes them, batched. If your stream needs one, write it in the report.
- English in code, comments, commits and docs; no U+2014 or U+2013 characters anywhere; no AI mention in
  commits.

## Reports

Short, in English, to the lead, after each lot: what changed (with counts), the evidence paths, what was
verified and how, what was not, what you need from the lead (a merge, a shared widget, a simulation change).
