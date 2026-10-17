#!/usr/bin/env python3
"""Assemble site/, the files the host serves next to the services (server/src/host.js).

  site/game/             one finished WebGL build (index.html, Build/, TemplateData/, StreamingAssets/)
  site/genesis-art/256/  the portraits, one JPEG per Genesis
  site/genesis-art/cut/  what to keep of a portrait to cut the Genesis out of its background,
                         for those that have a cut, and their list (tools/genesis-art/cut_out.py)
  site/genesis-art/pages/96/  the portraits on pages of 48, for the collection screen
                         (tools/genesis-art/make_atlas.py)
  site/quantum-db.json   the simulation assets of that build, for the program that plays a match
                         again (the judge must read the assets of the build that is online);
                         beside game/, so it is never served
  site/judge/            that program (Chog.Quantum.Verifier.dll and what it loads, run with the
                         .NET 8 runtime) and site/judge/lut/, the tables the simulation reads.
                         It is built here, now, from the simulation sources: assemble right
                         after the build, so the judge and the game run the same rules
  site/RELEASE.json      which build this is, the stamp of its simulation, and when it was assembled

  python3 tools/release/assemble_site.py --build 20261009T115508836Z-aa75436b16954bc99c5a8b98c713b4b0
  python3 tools/release/assemble_site.py --latest

It copies files and decides nothing. The build must be one that was checked; the hero page data
must be up to date (tools/web/build_hero_data.py --check), since the page and the game are
released together. --dotnet names the .NET SDK that builds the judge (default: the one Unity
6000.6.4f1 ships); without any, or for a build that came without simulation assets, the site
is assembled without a judge and its matches get no verdict.
"""
import argparse
import json
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILDS = ROOT / ".artifacts/unity"
PORTRAITS = ROOT / ".artifacts/genesis-art/256"
CUTS = ROOT / ".artifacts/genesis-art/cut"
# The portraits laid on pages of 48 for the collection screen (tools/genesis-art/make_atlas.py).
PAGES = ROOT / ".artifacts/genesis-art/pages/96"
SITE = ROOT / "site"
COLLECTION_SIZE = 1969
JUDGE_BUILD = ROOT / ".artifacts/dotnet-build/bin/Chog.Quantum.Verifier/release"
LOOKUP_TABLES = ROOT / "game/Assets/Photon/Quantum/Resources/LUT"
UNITY_DOTNET = Path("/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet")


def assemble_judge(dotnet):
    """Builds the program that plays a match again and copies it, with its tables, to site/judge/."""
    judge = SITE / "judge"
    if judge.exists():
        shutil.rmtree(judge)
    if dotnet is None:
        return False
    built = subprocess.run([sys.executable, str(ROOT / "tools/quantum/headless.py"), "verify", "--dotnet", str(dotnet)],
                           capture_output=True, text=True)
    if built.returncode != 0 or not (JUDGE_BUILD / "Chog.Quantum.Verifier.dll").is_file():
        sys.exit("The judge could not be built:\n" + (built.stderr or built.stdout).strip()[-2000:])
    (judge / "lut").mkdir(parents=True)
    # What the .NET runtime needs to start it: the libraries and the two files that describe
    # them. Debug symbols, documentation and the launcher made for this machine stay behind.
    for file in sorted(JUDGE_BUILD.iterdir()):
        if file.suffix == ".dll" or file.name.endswith((".deps.json", ".runtimeconfig.json")):
            shutil.copy2(file, judge / file.name)
    for table in sorted(LOOKUP_TABLES.glob("*.bytes")):
        shutil.copy2(table, judge / "lut" / table.name)
    return True


def finished_builds():
    return sorted((p for p in BUILDS.glob("*/webgl") if (p / "index.html").is_file() and (p.parent / "build-report.json").is_file()),
                  key=lambda p: (p / "index.html").stat().st_mtime)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    choice = parser.add_mutually_exclusive_group(required=True)
    choice.add_argument("--build", help="name of a build folder under .artifacts/unity")
    choice.add_argument("--latest", action="store_true", help="use the newest finished build")
    parser.add_argument("--dotnet", type=Path, help="the .NET SDK that builds the judge")
    args = parser.parse_args()

    if args.latest:
        builds = finished_builds()
        if not builds:
            sys.exit("No finished build under .artifacts/unity.")
        build = builds[-1]
    else:
        build = BUILDS / args.build / "webgl"
        if not (build / "index.html").is_file():
            sys.exit(f"No WebGL build at {build}.")

    portraits = sorted(PORTRAITS.glob("*.jpg"))
    if len(portraits) != COLLECTION_SIZE:
        sys.exit(f"{len(portraits)} portraits in {PORTRAITS}, expected {COLLECTION_SIZE}: run tools/genesis-art/fetch_portraits.py.")

    stale = subprocess.run([sys.executable, str(ROOT / "tools/web/build_hero_data.py"), "--check"], capture_output=True, text=True)
    if stale.returncode != 0:
        sys.exit((stale.stderr or stale.stdout).strip())

    # Each part is replaced whole, so no file of an earlier release stays behind.
    game = SITE / "game"
    art = SITE / "genesis-art/256"
    cuts = SITE / "genesis-art/cut"
    pages = SITE / "genesis-art/pages/96"
    for folder in (game, art, cuts, pages):
        if folder.exists():
            shutil.rmtree(folder)
    shutil.copytree(build, game)
    art.mkdir(parents=True)
    for portrait in portraits:
        shutil.copy2(portrait, art / portrait.name)
    # The cuts are optional: without them every Genesis keeps its whole round picture.
    cut_ids = json.loads((CUTS / "index.json").read_text(encoding="utf-8"))["ids"] if (CUTS / "index.json").is_file() else []
    if cut_ids:
        cuts.mkdir(parents=True)
        shutil.copy2(CUTS / "index.json", cuts / "index.json")
        for metadata_id in cut_ids:
            shutil.copy2(CUTS / f"{metadata_id}.png", cuts / f"{metadata_id}.png")
    # The pages of the collection screen; without them it shows the portraits one by one.
    if PAGES.is_dir():
        shutil.copytree(PAGES, pages)

    # Builds made before 11 October 2026 have no simulation assets beside them: such a release
    # is served as before and its matches get no verdict.
    assets = build.parent / "quantum-db.json"
    if assets.is_file():
        shutil.copy2(assets, SITE / "quantum-db.json")
    elif (SITE / "quantum-db.json").exists():
        (SITE / "quantum-db.json").unlink()
    dotnet = args.dotnet or (UNITY_DOTNET if UNITY_DOTNET.is_file() else None) or shutil.which("dotnet")
    has_judge = assemble_judge(dotnet if assets.is_file() else None)

    roster = json.loads((ROOT / "content/genesis-roster.json").read_text(encoding="utf-8"))
    release = {
        "assembledAtUtc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "build": build.parent.name,
        "rulesetVersion": roster["rulesetVersion"],
        "catalogHash": roster["catalogHash"],
        "portraits": len(portraits),
        "cuts": len(cut_ids),
        "simulationAssets": assets.is_file(),
        "judge": has_judge,
    }
    # The stamp of the build's simulation (QualificationBuild): what its pages tell Photon, so what
    # the services compare rooms and matches with (servedPhotonVersion in server/src/node.js).
    simulation = build.parent / "simulation.txt"
    if simulation.is_file():
        release["simulation"] = simulation.read_text(encoding="utf-8").strip()
    (SITE / "RELEASE.json").write_text(json.dumps(release, indent=1) + "\n", encoding="utf-8")

    size = sum(f.stat().st_size for f in SITE.rglob("*") if f.is_file())
    print(f"{SITE}: build {release['build']}, {len(portraits)} portraits, "
          f"{'with' if has_judge else 'WITHOUT'} the judge, {size / 1e6:.0f} MB")


if __name__ == "__main__":
    main()
