#!/usr/bin/env python3
"""Patient, resumable completion of the Genesis metadata directory across several gateways.

Read-only. Same append-only record format as fetch_genesis_metadata.py. When a gateway
answers HTTP 429 it is left alone for its Retry-After delay and the next gateway is tried;
when every gateway is cooling down the script sleeps until the first one is due.
IDs are metadata IDs, NOT token IDs.
"""
import argparse
import hashlib
import json
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

CID = "bafybeid4ybujnscdipkno7ps5utqelp5ry5ryreqhay3zhgk3a6x4jruqu"
GATEWAYS = [
    # Added 9 October: both returned bytes identical to Pinata's for metadata 1287. The first
    # is the IPFS mirror of the marketplace where the collection trades; it kept answering
    # when the public gateways throttled.
    "https://ipfs2.seadn.io/ipfs/{cid}/{id}.json",
    "https://ipfs.filebase.io/ipfs/{cid}/{id}.json",
    "https://gateway.pinata.cloud/ipfs/{cid}/{id}.json",
    "https://{cid}.ipfs.dweb.link/{id}.json",
    "https://{cid}.ipfs.w3s.link/{id}.json",
    "https://{cid}.ipfs.nftstorage.link/{id}.json",
]
HEADERS = {"Accept": "application/json", "User-Agent": "Mozilla/5.0 ChogathonResearch/1.0"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--last-id", type=int, default=1969)
    parser.add_argument("--interval", type=float, default=2.0, help="Seconds between requests to one gateway.")
    parser.add_argument("--max-hours", type=float, default=10.0)
    args = parser.parse_args()

    existing = set()
    for line in args.output.read_text().splitlines():
        record = json.loads(line)
        if record["cid"] != CID:
            raise ValueError("Mixed metadata roots")
        existing.add(record["metadata_id"])
    pending = [i for i in range(1, args.last_id + 1) if i not in existing]
    print(json.dumps({"existing": len(existing), "pending": len(pending)}), flush=True)

    ready_at = [0.0] * len(GATEWAYS)       # earliest next use of each gateway
    failures_in_a_row = [0] * len(GATEWAYS)
    deadline = time.monotonic() + args.max_hours * 3600
    done = 0
    with args.output.open("a", encoding="utf-8") as out:
        while pending and time.monotonic() < deadline:
            now = time.monotonic()
            gateway = min(range(len(GATEWAYS)), key=lambda g: ready_at[g])
            if ready_at[gateway] > now:
                time.sleep(min(ready_at[gateway] - now, 60))
                continue

            metadata_id = pending[0]
            url = GATEWAYS[gateway].format(cid=CID, id=metadata_id)
            ready_at[gateway] = time.monotonic() + args.interval
            try:
                with urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS), timeout=25) as response:
                    raw = response.read(100_001)
                if len(raw) > 100_000:
                    raise ValueError("Unexpected metadata size")
                value = json.loads(raw)
                if not isinstance(value, dict) or not isinstance(value.get("attributes"), list):
                    raise ValueError("Unexpected metadata shape")
            except urllib.error.HTTPError as error:
                if error.code == 429:
                    wait = float(error.headers.get("Retry-After") or 900)
                    ready_at[gateway] = time.monotonic() + wait + 20
                    print(json.dumps({"gateway": gateway, "rate_limited_seconds": wait, "pending": len(pending)}), flush=True)
                else:
                    failures_in_a_row[gateway] += 1
                    ready_at[gateway] = time.monotonic() + min(60 * failures_in_a_row[gateway], 900)
                continue
            except (urllib.error.URLError, TimeoutError, ValueError, json.JSONDecodeError):
                # A gateway that times out or returns a placeholder page is backed off, not trusted.
                failures_in_a_row[gateway] += 1
                ready_at[gateway] = time.monotonic() + min(60 * failures_in_a_row[gateway], 900)
                continue

            failures_in_a_row[gateway] = 0
            out.write(json.dumps({
                "cid": CID, "metadata_id": metadata_id,
                "uri": f"ipfs://{CID}/{metadata_id}.json", "gateway_url": url,
                "retrieved_at_utc": datetime.now(timezone.utc).isoformat(),
                "raw_sha256": hashlib.sha256(raw).hexdigest(),
                "raw_json": raw.decode("utf-8"),
            }, ensure_ascii=False) + "\n")
            out.flush()
            pending.pop(0)
            done += 1
            if done % 50 == 0:
                print(json.dumps({"new": done, "pending": len(pending)}), flush=True)

    print(json.dumps({"new": done, "pending": len(pending), "complete": not pending}), flush=True)


if __name__ == "__main__":
    main()
