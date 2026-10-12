#!/usr/bin/env python3
"""Lists what a scripted browser run recorded beyond the game's own lines: the requests that
failed, the exceptions, and every console line that matches a word.

Usage: list_requests.py <touch-browser-smoke-events.json> [word ...]

Without words it prints failed requests and exceptions only. Meant for a run that stopped
without saying why (for instance a screenshot that never came back).
"""
import json
import sys
from pathlib import Path


def text_of(event):
    params = event.get("params", {})
    if event.get("method") == "Runtime.consoleAPICalled":
        return " ".join(str(arg.get("value", arg.get("description", ""))) for arg in params.get("args", []))
    if event.get("method") == "Runtime.exceptionThrown":
        details = params.get("exceptionDetails", {})
        return "EXCEPTION " + str(details.get("exception", {}).get("description", details.get("text", "")))
    if event.get("method") == "Log.entryAdded":
        entry = params.get("entry", {})
        return f"LOG {entry.get('level', '')} {entry.get('text', '')} {entry.get('url', '')}"
    if event.get("method") == "Network.loadingFailed":
        return f"FAILED {params.get('errorText', '')} {params.get('url', '')}"
    return ""


def main():
    events = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    words = [word.lower() for word in sys.argv[2:]]
    if isinstance(events, dict):
        events = events.get("events", [])
    print(len(events), "events recorded")
    for event in events:
        line = text_of(event)
        if not line:
            continue
        lowered = line.lower()
        wanted = line.startswith(("EXCEPTION", "FAILED")) or any(word in lowered for word in words)
        if wanted:
            print(line[:400].replace("\n", " | "))


if __name__ == "__main__":
    main()
