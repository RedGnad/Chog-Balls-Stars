# Handoff for another agent (18 October 2026)

Chog Balls Stars (Chog Arena until 22 October 2026) is the owner's entry to the Chogathon: a 3v3 top-down arena (Unity 6000.6 WebGL, Photon Quantum 3)
where each Chog Genesis NFT (1,969 tokens, Monad mainnet) plays as its own 3D hero, its traits deciding its look
and its kit. Submission deadline: 23 October 2026, 15:00 UTC. Public game: `https://chogtanks.vercel.app/chog-balls-stars/` (since 22 October; `https://chogarena.vercel.app` sends players there)
(pages on Vercel, services on Render behind `/api/`). The owner talks in French; code, comments, commits and
technical docs are in English.

## Read first

- `AGENTS.md` (continuity rules), then the latest dated entries of `docs/STATUS.md` (search "18 October 2026").
- `docs/SUBMISSION.md`: the hackathon framing, what judges see and the judge instructions.
- `docs/DESIGN-BRIEF.md` and `research/chog-visual-identity-2026-10-15.md`: the UI direction
  (inspired by Supercell, Clash of Clans and Brawl Stars; mockups validated by the owner on 17 October).
  The theme lives in `game/Assets/Chog/Screens/ChogTheme.cs`; screens in `game/Assets/Chog/Screens/`.
- `docs/PLAN.md`, `docs/ARCHITECTURE-QUANTUM.md` (simulation, view and services boundaries).
- `game/README.md` (what the game shows and how), `tools/blender/README.md` (the 3D heroes, every batch).
- `docs/ATTACKS-FROM-THE-ART.md` (every attack and effect must visibly represent a Chog trait).
- `docs/PERF-AUDIT.md` (performance plan), `docs/DEPLOY.md` (hosting, Photon's version limit).
- `research/evidence/siege-map-layouts-2026-10-17/README.md` (the siege map study).

## Open work, by value

1. Heroes in 3D, the owner's feedback of 17 October (tools/blender and `game/Assets/Chog/Resources/ChogParts.bytes`
   only, no simulation impact): Skull and Radioactive heads not faithful to the art nor cute; the astronaut suit
   must be a full costume like the banana and the dragon; spikes longer under caps than bare; some eyes and mouths
   wrong or misplaced. Point 1 (faces floating) is done and online. Work from the real art
   (`.artifacts/genesis-art/256/`), judge every head turned around, at game size, with before/after sheets.
2. The siege map A and its simulation batch: on the branch `wip/siege-map-and-heroes` (42dea34), not finished
   (see its commit message). It changes the simulation, so Photon's version: keep it for a careful session.
3. The phone measurement (`?perf=1` from a phone on the Mac's Wi-Fi) and the perf items it points to.
4. UI polish within the validated direction.

## House rules (the owner's, not negotiable)

- Commits belong to the owner: never a "Co-Authored-By" line or any mention of an AI in commits.
- Never the characters U+2014 or U+2013 anywhere (code, docs, commits, images): use commas, colons, parentheses.
- Never mark a feature live, active or ready without an end-to-end implementation; never fake live data in the UI.
- Verify against the code and real sources; never invent. A failed lookup is not a conclusion.
- Never print secrets (Photon, Reown, session, referee, Vercel keys); no financial transactions.

## The machine (an 8 GB Mac, the owner's Unity Editor stays open)

- Heavy-job lock: `.artifacts/heavy-job.lock` holds `free` or `busy <who>`. Before any Blender run, Unity build or
  headless browser run, wait for `free`, write `busy <you>`, write `free` as soon as it ends.
- One Blender process at a time, `-t 4` and `nice -n 10`, never two 3D jobs at once (the Mac nearly crashed).
- Unity builds go through the open Editor: write `build` into `.artifacts/unity/webgl-build-request`, wait until
  `.artifacts/unity/webgl-build-status.json` says `succeeded` or `failed` with an `updatedAtUtc` later than the
  request (an old `succeeded` is not yours). Builds land in `.artifacts/unity/<build id>/`.
- Type-check without Unity: `python3 tools/dev/typecheck.py Chog.Match --unsafe --use Quantum.Simulation
  Chog.Diagnostics --sources Assets/Chog/Match`; for the game assembly the strict form
  `python3 tools/dev/typecheck.py Assembly-CSharp --use Quantum.Simulation Chog.Match --sources Assets/Chog/Screens`;
  view tests `sh tools/dev/view_tests.sh`; services `cd server && npm test`.
- Local servers: `python3 tools/unity/serve_webgl.py --port <yours> --directory .artifacts/unity/<id>/webgl
  --services http://127.0.0.1:8097` (local services run on 8097 with `--photon-auth`). Stop only your own server,
  by its port (`lsof -ti tcp:<port> | xargs kill`), never by name. Test runs are muted.
- Captures: `node tools/unity/qualify_webgl.mjs` (options at its top; `CHOG_BROWSER_POINTER` moves the mouse).
  Never press PLAY on the public site: a scripted client joins real players' rooms.
- `caffeinate -dimsu` keeps the Mac awake; it stops with a restart.

## Photon and deploying

- Photon's application version is the simulation stamp (`sim-<16 hex>`): any change under
  `game/Assets/QuantumUser/Simulation`, the Quantum assemblies or the exported assets changes it, and Photon
  refuses new versions past a hidden limit. Batch simulation changes, check the stamp of a build
  (`simulation.txt` beside it) and that a local build connects before deploying one with a new stamp.
- Deploy: `python3 tools/release/assemble_site.py --build <build id>`, `sh tools/release/check_judge.sh
  <Unity's dotnet>`, commit `site` and `game/ProjectSettings/ProjectSettings.asset`, push, `sh
  tools/release/vercel_site.sh deploy`, then poll `https://chog-genesis-game.onrender.com/api/photon` until it
  names the build. Only deploy a build seen in a match without exception.

## Working with the owner

- Answer in French, in plain words, about what the player sees. When the owner asks for an opinion or the next
  steps, answer and wait: build only on an explicit go. Put the answers at the end of a turn, after the work.
- Judges hold Chogs: never propose loans or trial modes for them.

## Several agents at once

See `docs/TEAM-WORKFLOW.md`: one territory and one branch per stream, one heavy job at a time, the lead integrates.
