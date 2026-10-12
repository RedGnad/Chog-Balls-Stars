#!/usr/bin/env python3
"""Type-check one Unity assembly outside the Editor by reusing Unity's own compiler arguments.

  python3 tools/dev/typecheck.py <Assembly> [--unsafe] [--sources folder ...] [--add file.cs ...]
                                 [--ref Other.Assembly ...] [--use Checked.Assembly ...]

  --sources  folders (relative to game/) whose .cs files belong to the assembly: files the
             Editor has not compiled yet are added. Sub-folders named Tests or Editor are skipped
  --add      single source files the Editor has not compiled yet (relative to game/)
  --ref      assemblies to reference from the Editor's last compile
  --use      assemblies type-checked earlier by this script, which replace the Editor's copy
  --unsafe   the assembly definition allows unsafe code

The Editor can stay open: nothing in the project is touched, the output goes to
.artifacts/typecheck/. The arguments come from game/Library/Bee, so the Editor must have
compiled the project once since the last change of packages or assembly definitions.
"""
import argparse
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GAME = ROOT / "game"
DAG = "Library/Bee/artifacts/2000b0aE.dag"
OUT = ROOT / ".artifacts/typecheck"
SDK = Path("/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk")
OWN_ASSEMBLY_FOLDERS = {"Tests", "Editor"}


def sources_of(folder):
    """The .cs files of a folder, without those of sub-folders that are assemblies of their own."""
    base = GAME / folder
    for file in sorted(base.rglob("*.cs")):
        if not OWN_ASSEMBLY_FOLDERS.intersection(file.relative_to(base).parts[:-1]):
            yield str(file.relative_to(GAME))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("assembly")
    parser.add_argument("--sources", nargs="*", default=[])
    parser.add_argument("--add", nargs="*", default=[])
    parser.add_argument("--ref", nargs="*", default=[])
    parser.add_argument("--use", nargs="*", default=[])
    parser.add_argument("--unsafe", action="store_true")
    args = parser.parse_args()

    arguments = GAME / DAG / f"{args.assembly}.rsp"
    if not arguments.is_file():
        print(f"{args.assembly}: no compiler arguments at {arguments}; let the Editor compile the project once")
        return 2

    lines = []
    for line in arguments.read_text().splitlines():
        if re.match(r"^(-analyzer|-out:|-refout:|/additionalfile|-additionalfile|-ruleset)", line):
            continue
        if any(f"/{name}.ref.dll" in line for name in args.use):
            continue
        lines.append(line)
    existing = set(lines)
    added = list(args.add)
    for folder in args.sources:
        added += sources_of(folder)
    lines += [f'"{source}"' for source in dict.fromkeys(added) if f'"{source}"' not in existing]
    lines += [f'-r:"{DAG}/{name}.ref.dll"' for name in args.ref if f'-r:"{DAG}/{name}.ref.dll"' not in existing]
    lines += [f'-r:"{OUT}/{name}.dll"' for name in args.use]
    lines.append(f'-out:"{OUT}/{args.assembly}.dll"')
    if args.unsafe:
        lines.append("-unsafe+")
    OUT.mkdir(parents=True, exist_ok=True)
    rsp = OUT / f"{args.assembly}.check.rsp"
    rsp.write_text("\n".join(lines) + "\n")

    result = subprocess.run([str(SDK / "dotnet"), str(SDK / "sdk/8.0.318/Roslyn/bincore/csc.dll"), "-nologo", f"@{rsp}"],
                            cwd=GAME, capture_output=True, text=True)
    errors = [line for line in (result.stdout + result.stderr).splitlines() if ": error " in line]
    for line in errors[:25]:
        print(line)
    print(f"{args.assembly}: exit {result.returncode}, {len(errors)} error(s)")
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
