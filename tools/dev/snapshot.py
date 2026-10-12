#!/usr/bin/env python3
"""Save a compressed copy of the project's own sources under .artifacts/snapshots/.

Usage: snapshot.py <label>
There is no git repository yet, so this is the only way back to an earlier state. It keeps
the game scripts and data, the docs, the content, the tools and the small evidence files; it
leaves out build output, caches, the Unity Library and files larger than 2 MB.
"""
import sys
import tarfile
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
INCLUDE = [
    "AGENTS.md", "README.md", "render.yaml", "docs", "content", "tools", "tests", "shared", "server", "web",
    "contracts/README.md", "contracts/foundry.toml", "contracts/src", "contracts/test",
    "game/README.md", "game/Packages/manifest.json", "game/ProjectSettings",
    "game/Assets/Chog", "game/Assets/QuantumUser", "game/Assets/TwinStickShooter/Scripts",
    "game/Assets/Plugins", "research/evidence",
]
SKIP_DIRS = {"bin", "obj", "node_modules", "__pycache__", ".git"}
MAX_BYTES = 2 * 1024 * 1024

label = sys.argv[1]
target = ROOT / ".artifacts/snapshots" / f"{time.strftime('%Y%m%dT%H%M')}-{label}.tgz"
count = 0
with tarfile.open(target, "w:gz") as archive:
    for entry in INCLUDE:
        path = ROOT / entry
        if not path.exists():
            continue
        files = [path] if path.is_file() else sorted(p for p in path.rglob("*") if p.is_file())
        for file in files:
            if SKIP_DIRS.intersection(file.relative_to(ROOT).parts) or file.stat().st_size > MAX_BYTES:
                continue
            archive.add(file, arcname=str(file.relative_to(ROOT)))
            count += 1
print(f"{target} : {count} files, {target.stat().st_size / 1e6:.1f} MB")
