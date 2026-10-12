#!/usr/bin/env python3
"""Apply exact, unique text replacements to a file while keeping its BOM and line endings.

Usage: edit_keep_eol.py <file> <pairs.json>   (pairs.json: [["old", "new"], ...], LF newlines)
"""
import json
import sys
from pathlib import Path


def main():
    path, pairs = Path(sys.argv[1]), json.loads(Path(sys.argv[2]).read_text(encoding="utf-8"))
    raw = path.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    crlf = "\r\n" in text
    text = text.replace("\r\n", "\n")
    for old, new in pairs:
        if text.count(old) != 1:
            sys.exit(f"Expected exactly one match in {path.name} for: {old[:70]!r} (found {text.count(old)})")
        text = text.replace(old, new)
    if crlf:
        text = text.replace("\n", "\r\n")
    path.write_bytes((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))
    print(f"{path.name}: {len(pairs)} replacement(s), {'CRLF' if crlf else 'LF'}{', BOM' if bom else ''}")


if __name__ == "__main__":
    main()
