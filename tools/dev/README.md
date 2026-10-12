# Developer tools: working on the game while the Unity Editor stays open

The Editor is open on `game/`, so Unity cannot be started a second time in batch mode on the
same project. These scripts are the loop that works next to it. They were written during the
session of 9 October 2026 and moved here so a later session finds them.

| Script | What it does |
| --- | --- |
| `typecheck.py <Assembly>` | compiles one Unity assembly outside the Editor with the Editor's own compiler arguments; errors in a few seconds instead of a build |
| `view_tests.sh` | compiles the view code and runs its NUnit tests (`game/Assets/Chog/Match/Tests`) with a small reflection runner (`TestRunner.cs`) |
| `build_webgl.py [build\|refresh]` | asks the open Editor for a WebGL build, or only a compile and import, and waits for the outcome |
| `snapshot.py <label>` | saves a compressed copy of the sources under `.artifacts/snapshots/`: the only way back while there is no git repository |
| `prune_builds.py [--apply]` | removes old build folders that no document names, when the disk fills up |
| `edit_keep_eol.py <file> <pairs.json>` | exact text replacements in a file while keeping its line endings: the sample's scripts use CRLF |
| `tile_shots.py <out> <columns> <images...>` | tiles screenshots into one image to scan a run at a glance (needs Pillow) |
| `crop_shots.py <out> <left,top,right,bottom> <zoom> <images...>` | cuts the same rectangle out of several screenshots and puts the enlarged cuts side by side: one spot of the screen across runs (needs Pillow) |
| `rename_in_sources.py <folder> <pairs.json> [--apply]` | renames identifiers across the C# sources of a folder; lists what would change unless `--apply` is given |

A usual round:

```sh
sh tools/dev/view_tests.sh                    # the view code compiles and its tests pass
python3 tools/dev/typecheck.py Assembly-CSharp --use Quantum.Simulation Chog.Match Chog.Diagnostics \
    --sources Assets/Chog/Screens              # the screens compile against it
sh tools/release/check_all.sh                 # catalog, simulation, services, contract
python3 tools/dev/build_webgl.py build        # about four minutes
python3 tools/unity/serve_webgl.py --latest --host 0.0.0.0 --port 8084
sh tools/unity/checks/browser_check.sh ...    # scripted runs in a muted headless browser
```

Two test options added on 10 October for the game's page: `serve_webgl.py --drop-first
framework.js` leaves the first request for that file unanswered, as a broken connection does,
to see the page start again by itself; `tools/web/screenshot.mjs` reads `CHOG_PAGE_AS=android`
or `iphone` (which phone the page should take the browser for), `CHOG_PAGE_SLOW_KBPS` (a slow
connection, to see the loading screen) and `CHOG_PAGE_WAIT_MS`.

Limits. `typecheck.py --use <Assembly>` hands that assembly to the compile whether or not the
Editor's own assembly references it: on 11 October a screen that used `Chog.Diagnostics`
passed this check and failed in the Editor, because the game's own assembly does not see it.
Only name with `--use` what the assembly really references. `typecheck.py` reuses the arguments under `game/Library/Bee`: after a change of
packages or assembly definitions, let the Editor compile once first. Its `--sources` option
adds files the Editor has not seen yet; a deleted file still listed by the Editor makes the
compile fail until the Editor has compiled again. `view_tests.sh` cannot run a test that needs
a running engine. Paths to Unity 6000.6.4f1 are written in the scripts.
