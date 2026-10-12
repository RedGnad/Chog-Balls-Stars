#!/usr/bin/env python3
"""Compare imported vendor files to the archive baseline; never rewrites them."""
import argparse
import hashlib
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[2] / "game")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    baseline = json.loads((root / "research/evidence/quantum-sample-baseline-sha256.json").read_text())
    missing, modified = [], []
    for name, expected in baseline.items():
        path = args.project / name
        if not path.is_file():
            missing.append(name)
        elif hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            modified.append(name)
    authored_roots = ("Assets", "Packages", "ProjectSettings")
    added = sorted(str(path.relative_to(args.project))
                   for folder in authored_roots for path in (args.project / folder).rglob("*")
                   if path.is_file() and str(path.relative_to(args.project)) not in baseline)
    print(json.dumps({"baseline_files": len(baseline), "missing": missing,
                      "modified": modified, "added_under_project_roots": added}, indent=2))
    return 1 if missing or modified else 0


if __name__ == "__main__":
    raise SystemExit(main())
