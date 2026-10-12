#!/usr/bin/env python3
"""Rename identifiers across the C# sources of a folder, keeping each file's line endings.

Usage: rename_in_sources.py <folder> <pairs.json> [--apply]

pairs.json is [["Old.Name(", "New.Name("], ...]: plain text, replaced everywhere it occurs in
the .cs files under the folder. Without --apply it only lists what would change.
"""
import json
import sys
from pathlib import Path

folder, pairs = Path(sys.argv[1]), json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))
apply = "--apply" in sys.argv
total = 0
for source in sorted(folder.rglob("*.cs")):
    raw = source.read_bytes()
    text = raw.decode("utf-8")
    changed = text
    count = 0
    for old, new in pairs:
        count += changed.count(old)
        changed = changed.replace(old, new)
    if count:
        total += count
        print(f"{source.relative_to(folder)}: {count}")
        if apply:
            source.write_bytes(changed.encode("utf-8"))
print(f"{total} replacement(s){'' if apply else ' (dry run, add --apply)'}")
