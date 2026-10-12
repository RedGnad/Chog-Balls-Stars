#!/usr/bin/env python3
"""Free disk space by removing old WebGL build folders under .artifacts/unity.

Kept: every build named in docs/ or research/evidence/ (they are the record of what was
measured or shown), the build copied into site/, and the newest five. Everything else is
generated output that a new build replaces.

  prune_builds.py           # prints what would be removed
  prune_builds.py --apply   # removes it
"""
import json
import re
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILDS = ROOT / ".artifacts/unity"
NAME = re.compile(r"2026\d{4}T\d{9}Z(?:-[0-9a-f]{32})?")

named = set()
for folder in (ROOT / "docs", ROOT / "research/evidence", ROOT / "game"):
    for file in list(folder.glob("*.md")) + list(folder.glob("*.json")):
        named.update(match.group(0)[:19] for match in NAME.finditer(file.read_text(encoding="utf-8", errors="replace")))
release = ROOT / "site/RELEASE.json"
if release.is_file():
    named.add(json.loads(release.read_text())["build"][:19])

builds = sorted(p for p in BUILDS.iterdir() if p.is_dir() and NAME.fullmatch(p.name))
newest = {p.name for p in builds[-5:]}
removed = 0
for build in builds:
    keep = build.name in newest or build.name[:19] in named
    size = sum(f.stat().st_size for f in build.rglob("*") if f.is_file())
    print(f"{'keep  ' if keep else 'remove'} {build.name[:19]}  {size / 1e6:5.1f} MB")
    if not keep:
        removed += size
        if "--apply" in sys.argv:
            shutil.rmtree(build)
print(f"{'removed' if '--apply' in sys.argv else 'would remove'} {removed / 1e6:.0f} MB")
