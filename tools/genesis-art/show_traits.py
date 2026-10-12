#!/usr/bin/env python3
"""Print the traits of some Chog Genesis, one line each, from the recorded metadata.

Usage: show_traits.py <id> [<id> ...]     ids are the numbers shown in the names ("CHOG #n")
"""
import json
import sys
from pathlib import Path

SOURCE = Path(__file__).resolve().parents[2] / "research" / "evidence" / "genesis-metadata.jsonl"
wanted = {int(value) for value in sys.argv[1:]}
for line in SOURCE.read_text(encoding="utf-8").splitlines():
    if not line.strip():
        continue
    record = json.loads(line)
    if int(record["metadata_id"]) in wanted:
        traits = json.loads(record["raw_json"]).get("attributes", [])
        print(f"#{record['metadata_id']}: " + ", ".join(f"{item['trait_type']}={item['value']}" for item in traits))
