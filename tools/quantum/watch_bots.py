#!/usr/bin/env python3
"""Play one recorded match again with other seeds and print what the bots did in each.

Usage: watch_bots.py <replay.json> [number of seeds, default 8] [--all-bots] [--dotnet /path/to/dotnet]

With --all-bots the heroes of the recorded players are handed to bots when the match begins:
six bots, three against three, where a record of an idle player gives two bots and a hero that
stands still against three.

The judge (tools/quantum/Verifier.cs, built by "headless.py verify") replays a record as fast
as the machine goes. With another seed the bots play another match against the same recorded
commands of the players, so a record made with an idle player gives as many matches of bots
as wanted, in seconds each: enough to see whether a change to the bots or to the numbers of
the structures does what it was meant to. These are tests of the rules, never verdicts.
One line per seed: who won, the scores, how long the match lasted, how often each tower hit,
the health each structure had left, and what fell when.
"""
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
VERIFIER = ROOT / ".artifacts/dotnet-build/bin/Chog.Quantum.Verifier/release/Chog.Quantum.Verifier.dll"
DATABASE = ROOT / "site/quantum-db.json"
LUT = ROOT / "game/Assets/Photon/Quantum/Resources/LUT"


def main():
    arguments = sys.argv[1:]
    dotnet = shutil.which("dotnet")
    if "--dotnet" in arguments:
        at = arguments.index("--dotnet")
        dotnet = arguments[at + 1]
        del arguments[at:at + 2]
    all_bots = "--all-bots" in arguments
    if all_bots:
        arguments.remove("--all-bots")
    if not arguments or not dotnet:
        sys.exit(__doc__)
    replay = Path(arguments[0]).resolve()
    seeds = int(arguments[1]) if len(arguments) > 1 else 8
    if not VERIFIER.is_file():
        sys.exit(f"{VERIFIER} is missing: run tools/quantum/headless.py verify first")

    core_falls = 0
    with tempfile.TemporaryDirectory() as folder:
        for seed in range(1, seeds + 1):
            report_path = Path(folder) / f"seed-{seed}.json"
            run = subprocess.run([dotnet, str(VERIFIER), str(replay), str(DATABASE), str(LUT), str(report_path),
                                  "--test-seed", str(seed)] + (["--test-all-bots", "1"] if all_bots else []),
                                 stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
            if run.returncode or not report_path.is_file():
                print(f"seed {seed:2d}: the judge failed: {run.stderr.strip()[-200:]}")
                continue
            report = json.loads(report_path.read_text(encoding="utf-8"))
            rate = report["updateRate"]
            start = report.get("selectionEndedAtTick") or report["firstTick"]
            lasted = (report["endedAtTick"] - start) / rate if report["endedAtTick"] else None
            health = " ".join(f"{s['kind'][0]}{s['team']}={s['health']}" for s in sorted(
                report["structures"], key=lambda s: (s["team"], s["kind"])))
            fallen = ", ".join(f"{f['kind'].lower()} of team {f['team']} at {(f['tick'] - start) / rate:.0f} s"
                               for f in report["fallen"]) or "nothing fell"
            core_falls += report["coreLostBy"] is not None
            ending = "by a core" if report["coreLostBy"] is not None else "by the score"
            print(f"seed {seed:2d}: winner {report['winnerTeam']} {ending}, scores {report['scores']}, "
                  f"{'%.0f s' % lasted if lasted else 'not ended'}, tower hits {report['towerShots']}, {health}; {fallen}")
    print(f"{core_falls} of {seeds} matches ended by a core (C core, T tower, then the team; health left)")


if __name__ == "__main__":
    main()
