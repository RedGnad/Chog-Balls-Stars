"""Read-only, resumable census of the published Genesis metadata directory.

No wallet, transactions or game code. IDs here are metadata IDs, NOT token IDs.
The CID comes from the recorded on-chain baseURI; mapping uses metadataId().
"""

import argparse
import concurrent.futures
import hashlib
import json
import threading
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

CID = "bafybeid4ybujnscdipkno7ps5utqelp5ry5ryreqhay3zhgk3a6x4jruqu"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--last-id", type=int, default=1969)
    parser.add_argument("--workers", type=int, default=4)
    parser.add_argument("--interval", type=float, default=1.25,
                        help="Minimum seconds between request starts; stop on HTTP 429.")
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    existing = {}
    if args.output.exists():
        for line in args.output.read_text().splitlines():
            record = json.loads(line)
            if record["cid"] != CID:
                raise ValueError("Mixed metadata roots")
            existing[record["metadata_id"]] = record
    lock = threading.Lock()
    stopped = threading.Event()
    next_request = [0.0]

    def fetch(metadata_id):
        url = f"https://gateway.pinata.cloud/ipfs/{CID}/{metadata_id}.json"
        error = None
        for attempt in range(3):
            if stopped.is_set():
                return {"metadata_id": metadata_id, "deferred": True}
            with lock:
                delay = max(0, next_request[0] - time.monotonic())
                next_request[0] = time.monotonic() + delay + args.interval
            if stopped.wait(delay):
                return {"metadata_id": metadata_id, "deferred": True}
            try:
                request = urllib.request.Request(url, headers={
                    "Accept": "application/json", "User-Agent": "ChogathonResearch/1.0"
                })
                with urllib.request.urlopen(request, timeout=20) as response:
                    raw = response.read(100_001)
                if len(raw) > 100_000:
                    raise ValueError("Unexpected metadata size")
                value = json.loads(raw)
                if not isinstance(value, dict) or not isinstance(value.get("attributes"), list):
                    raise ValueError("Unexpected metadata shape")
                return {
                    "cid": CID, "metadata_id": metadata_id,
                    "uri": f"ipfs://{CID}/{metadata_id}.json", "gateway_url": url,
                    "retrieved_at_utc": datetime.now(timezone.utc).isoformat(),
                    "raw_sha256": hashlib.sha256(raw).hexdigest(),
                    "raw_json": raw.decode("utf-8"),
                }
            except (urllib.error.URLError, TimeoutError, ValueError) as exc:
                error = str(exc)
                if isinstance(exc, urllib.error.HTTPError) and exc.code == 429:
                    stopped.set()
                    return {"metadata_id": metadata_id, "error": error, "url": url,
                            "retry_after": exc.headers.get("Retry-After"),
                            "collection_stopped_on_rate_limit": True}
                if attempt < 2:
                    if stopped.wait(2 ** (attempt + 1)):
                        return {"metadata_id": metadata_id, "deferred": True}
        return {"metadata_id": metadata_id, "error": error, "url": url}

    pending = [i for i in range(1, args.last_id + 1) if i not in existing]
    failures = []
    deferred = 0
    print(json.dumps({"existing": len(existing), "pending": len(pending)}), flush=True)
    with args.output.open("a", encoding="utf-8") as out:
        with concurrent.futures.ThreadPoolExecutor(max_workers=args.workers) as pool:
            futures = [pool.submit(fetch, i) for i in pending]
            for position, future in enumerate(concurrent.futures.as_completed(futures), 1):
                record = future.result()
                if record.get("deferred"):
                    deferred += 1
                elif "error" in record:
                    failures.append(record)
                else:
                    out.write(json.dumps(record, ensure_ascii=False) + "\n")
                    out.flush()
                if position % 100 == 0 or position == len(pending):
                    print(json.dumps({"processed": position, "remaining": len(pending) - position,
                                      "failures": len(failures), "deferred": deferred}), flush=True)
    failure_path = args.output.with_suffix(".failures.json")
    failure_path.write_text(json.dumps(failures, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({"new_successes": len(pending) - len(failures) - deferred,
                      "failures": len(failures), "deferred": deferred,
                      "failure_log": str(failure_path)}), flush=True)


if __name__ == "__main__":
    main()
