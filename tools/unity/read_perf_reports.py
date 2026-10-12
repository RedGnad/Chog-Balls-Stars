#!/usr/bin/env python3
"""Print the performance reports the game posted to the local test server.

One line per report: who sent it, how far into the match, and the numbers that matter.
Reports are written by tools/unity/serve_webgl.py when a page is opened with "?perf=1".
"""
import argparse
import json
from pathlib import Path
from urllib.parse import urlsplit

REPORTS = Path(__file__).resolve().parents[2] / ".artifacts/perf-reports.jsonl"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--client", help="only this address, for example the phone's")
    parser.add_argument("--last", type=int, default=30, help="how many reports to print")
    args = parser.parse_args()

    if not REPORTS.is_file():
        raise SystemExit("No report yet: " + str(REPORTS))
    rows = []
    for line in REPORTS.read_text(encoding="utf-8").splitlines():
        entry = json.loads(line)
        if args.client and entry["client"] != args.client:
            continue
        try:
            report = json.loads(entry["report"])
        except ValueError:
            continue
        rows.append((entry, report))

    print("received  client          options                    sec  fps  frame p50/p95   busy p50  sim p50  over100  quality  effects")
    for entry, report in rows[-args.last:]:
        options = urlsplit(report.get("page", "")).query.replace("token=", "t=")
        p50 = report.get("frameP50", 0) or 0.001
        status = report.get("status", "")
        effects = status.split("|")[0].strip() if status else ""
        print(f"{entry['received'][11:]}  {entry['client']:<15} {options[:26]:<26} {report.get('seconds', 0):>4} "
              f"{1000 / p50:>4.0f}  {report.get('frameP50', 0):>6.1f}/{report.get('frameP95', 0):<6.1f}  "
              f"{report.get('busyP50', 0):>7.1f}  {report.get('simulationP50', 0):>7.1f}  {report.get('over100Frames', 0):>7}  "
              f"{report.get('quality', ''):<8} {effects}")


if __name__ == "__main__":
    main()
