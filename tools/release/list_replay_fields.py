#!/usr/bin/env python3
"""Lists the fields of a recorded match (a Quantum replay file) without its bulk, to see what a
record carries before it is kept or shared: every key, with the length of long texts instead of
the texts themselves.

Usage: list_replay_fields.py <replay.json> [word ...]

With words, it also says which keys or short values contain one of them (case ignored), for
instance "session" or "secret".
"""
import json
import sys
from pathlib import Path


def walk(value, path, words, out):
    if isinstance(value, dict):
        for key, inner in value.items():
            walk(inner, f"{path}.{key}" if path else key, words, out)
    elif isinstance(value, list):
        out.append(f"{path}: list of {len(value)}")
        for inner in value[:1]:
            walk(inner, path + "[0]", words, out)
    else:
        text = str(value)
        shown = text if len(text) <= 60 else f"<{len(text)} characters>"
        marks = [word for word in words if word in path.lower() or (len(text) <= 200 and word in text.lower())]
        out.append(f"{path}: {shown}" + (f"   <-- contains {', '.join(marks)}" if marks else ""))


def main():
    record = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
    out = []
    walk(record, "", [word.lower() for word in sys.argv[2:]], out)
    print("\n".join(out))


if __name__ == "__main__":
    main()
