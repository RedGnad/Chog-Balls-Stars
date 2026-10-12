"""Compile the shared C# library and its verification CLI using installed Mono.

This executes real C# without Unity/Photon. It does not qualify an IL2CPP/WebGL build.
"""
import argparse
from pathlib import Path
import shutil
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=["test", "inspect", "coverage", "roster"], default="test", nargs="?")
    parser.add_argument("--output", default="content/genesis-roster.json", help="roster file to write")
    parser.add_argument("--check", action="store_true", help="roster: fail if the file on disk is not current")
    parser.add_argument("--metadata", default="research/evidence/genesis-metadata.jsonl")
    parser.add_argument("--bindings", default="research/evidence/genesis-bindings-2026-10-09.json")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    compiler, runtime = shutil.which("mcs"), shutil.which("mono")
    if not compiler or not runtime:
        raise SystemExit("Mono mcs and mono are required for this local verification command.")
    output = root / ".artifacts" / "hero-catalog"
    output.mkdir(parents=True, exist_ok=True)
    library = output / "Chog.Heroes.dll"
    executable = output / "hero-catalog.exe"
    sources = sorted((root / "shared/Chog.Heroes").glob("*.cs"))
    subprocess.run([compiler, "-langversion:7.2", "-warnaserror", "-target:library",
                    "-r:System.Runtime.Serialization", "-r:System.Numerics",
                    "-out:" + str(library), *map(str, sources)], check=True)
    subprocess.run([compiler, "-langversion:7.2", "-warnaserror", "-r:System.Runtime.Serialization",
                    "-r:" + str(library), "-out:" + str(executable),
                    str(root / "tools/hero-catalog/Program.cs")], check=True)
    if args.mode == "roster":
        # The playable cast the game imports: every recorded Genesis the real resolver accepts.
        result = subprocess.run([runtime, str(executable), "roster",
                                 str(root / "content/hero-catalog.prototype.json"),
                                 str(root / args.metadata), str(root / args.bindings)],
                                check=False, stdout=subprocess.PIPE)
        if result.returncode:
            return result.returncode
        target = root / args.output
        if args.check:
            current = target.read_bytes() if target.is_file() else b""
            if current != result.stdout:
                print("Roster is stale: run python3 tools/hero-catalog/run.py roster", file=sys.stderr)
                return 1
            return 0
        target.write_bytes(result.stdout)
        return 0
    if args.mode == "coverage":
        # Resolve every scanned token whose metadata is recorded, with the real resolver.
        return subprocess.run([runtime, str(executable), "coverage",
                               str(root / "content/hero-catalog.prototype.json"),
                               str(root / args.metadata), str(root / args.bindings)], check=False).returncode
    result = subprocess.run([runtime, str(executable), args.mode,
                             str(root / "content/hero-catalog.prototype.json"),
                             str(root / "tests/fixtures/genesis/manifest.json")], check=False)
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
