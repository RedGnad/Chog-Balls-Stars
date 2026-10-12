#!/usr/bin/env python3
"""Ask the open Unity Editor for a WebGL build, wait for it and print the outcome.

  python3 tools/dev/build_webgl.py [build | refresh] [seconds to wait, default 540]

"build" compiles, imports the Genesis roster and builds (about four minutes); "refresh" only
compiles and imports (about thirty seconds; use it after adding a .qtn file). The Editor must
be open on game/: its watcher (Assets/Chog/Editor/BuildRequestWatcher.cs) picks the request up
from .artifacts/unity/webgl-build-request and writes webgl-build-status.json next to it. It
waits while Play Mode is active. Exit code 0 when the final state is "succeeded" or
"refreshed".
"""
import json
import sys
import time
from pathlib import Path

ARTIFACTS = Path(__file__).resolve().parents[2] / ".artifacts/unity"
REQUEST = ARTIFACTS / "webgl-build-request"
STATUS = ARTIFACTS / "webgl-build-status.json"
FINAL = {"succeeded", "failed", "refreshed"}

words = [a for a in sys.argv[1:] if not a.isdigit()]
numbers = [a for a in sys.argv[1:] if a.isdigit()]
kind = words[0] if words else "build"
if kind not in ("build", "refresh"):
    sys.exit(__doc__)

ARTIFACTS.mkdir(parents=True, exist_ok=True)
REQUEST.write_text(kind + "\n")
deadline = time.monotonic() + float(numbers[0] if numbers else 540)
state = {}
while time.monotonic() < deadline:
    time.sleep(2)
    try:
        state = json.loads(STATUS.read_text())
    except (OSError, ValueError):
        state = {}
    if not REQUEST.exists() and state.get("state") in FINAL:
        break
else:
    print("no final state in time: is the Editor open on game/, and out of Play Mode?")
print(json.dumps(state, indent=1))
sys.exit(0 if state.get("state") in ("succeeded", "refreshed") else 1)
