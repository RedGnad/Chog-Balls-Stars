#!/usr/bin/env python3
"""Downloads the record Photon's server sent of one match to the public services (GET /api/match/record) and lays it out
as the judge reads a match folder (replay-start.json, replay-chunk-NNNNNN.json), so that the verdict can be replayed on
this machine and compared with the record a test page kept of the same match (?record=1, .artifacts/replays/).

    python3 tools/quantum/fetch_live_record.py "<gameId>" [folder] [--services https://chog-genesis-game.onrender.com]
    dotnet site/judge/Chog.Quantum.Verifier.dll <folder> site/quantum-db.json site/judge/lut <report.json>

A deploy of the services replaces their disk: fetch a record before the next one.
"""

import argparse
import json
import os
import sys
import urllib.parse
import urllib.request

DEFAULT_SERVICES = "https://chog-genesis-game.onrender.com"


def main():
    parser = argparse.ArgumentParser(description="Fetches a match's record from the services as a folder the judge reads.")
    parser.add_argument("game_id")
    parser.add_argument("folder", nargs="?", default=None)
    parser.add_argument("--services", default=DEFAULT_SERVICES)
    arguments = parser.parse_args()
    folder = arguments.folder or os.path.join(".artifacts", "live-records", arguments.game_id.replace(":", "_").replace("~", "_"))
    url = arguments.services.rstrip("/") + "/api/match/record?gameId=" + urllib.parse.quote(arguments.game_id, safe="")
    request = urllib.request.Request(url, headers={"User-Agent": "chog-live-record"})
    with urllib.request.urlopen(request, timeout=60) as answer:
        record = json.load(answer)
    if record.get("start") is None:
        sys.exit("the services hold no start of that match")
    os.makedirs(folder, exist_ok=True)
    with open(os.path.join(folder, "replay-start.json"), "w") as file:
        json.dump(record["start"], file)
    for chunk in record["chunks"]:
        with open(os.path.join(folder, "replay-chunk-%06d.json" % int(chunk["ChunkNumber"])), "w") as file:
            json.dump(chunk, file)
    last = record["chunks"][-1] if record["chunks"] else {}
    print(f"{folder}: {len(record['chunks'])} slices, ended {record['ended']}, missing {record['missing']}, "
          f"last tick {last.get('LastTick')}, ticks {last.get('TickCountTotal')}")


if __name__ == "__main__":
    main()
