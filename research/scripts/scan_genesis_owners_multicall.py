#!/usr/bin/env python3
"""Read the owner of every Chog Genesis token with ONE Multicall3 eth_call.

Read-only. The Genesis contract is not enumerable, so the slow path is one ownerOf per token
(research/scripts/scan_genesis_bindings.py, rate-limited by public RPCs). Multicall3 is deployed
on Monad mainnet at its usual address and answers the whole collection in under a second.
Token ids are canonical on-chain ids, not the numbers shown in the names.
"""
import argparse
import json
import time
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

MULTICALL3 = "0xcA11bde05977b3631167028862bE2a173976CA11"
GENESIS = "0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763"
OWNER_OF = "6352211e"        # ownerOf(uint256)
AGGREGATE3 = "82ad56cb"      # aggregate3((address,bool,bytes)[])
HEADERS = {"Content-Type": "application/json", "User-Agent": "Mozilla/5.0 ChogathonResearch/1.0"}


def word(value):
    return format(value, "064x")


def rpc(url, method, params):
    body = json.dumps({"jsonrpc": "2.0", "id": 1, "method": method, "params": params}).encode()
    request = urllib.request.Request(url, data=body, headers=HEADERS)
    with urllib.request.urlopen(request, timeout=90) as response:
        answer = json.load(response)
    if "result" not in answer:
        raise RuntimeError(method + " failed: " + json.dumps(answer)[:300])
    return answer["result"]


def encode_calls(last_id):
    """ABI-encode aggregate3 with one (target, allowFailure=true, ownerOf(id)) element per token."""
    target = word(int(GENESIS, 16))
    elements = ""
    for token_id in range(1, last_id + 1):
        call_data = OWNER_OF + word(token_id)
        elements += target + word(1) + word(0x60) + word(36) + call_data.ljust(128, "0")
    offsets = "".join(word(last_id * 32 + index * 192) for index in range(last_id))
    return "0x" + AGGREGATE3 + word(0x20) + word(last_id) + offsets + elements


def decode_owners(result):
    raw = result[2:]
    count = int(raw[64:128], 16)
    base = 128
    owners = {}
    for index in range(count):
        offset = int(raw[base + index * 64: base + (index + 1) * 64], 16) * 2
        element = raw[base + offset:]
        succeeded = int(element[0:64], 16) == 1
        owners[index + 1] = "0x" + element[192:256][-40:] if succeeded else None
    return owners


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rpc", default="https://rpc.monad.xyz")
    parser.add_argument("--last-id", type=int, default=1969)
    parser.add_argument("--output", type=Path, help="write the owners as JSON")
    args = parser.parse_args()

    block = rpc(args.rpc, "eth_blockNumber", [])
    started = time.monotonic()
    owners = decode_owners(rpc(args.rpc, "eth_call", [{"to": MULTICALL3, "data": encode_calls(args.last_id)}, block]))
    seconds = round(time.monotonic() - started, 2)

    held = {token: owner for token, owner in owners.items() if owner}
    summary = {
        "recorded_at_utc": datetime.now(timezone.utc).isoformat(),
        "rpc": args.rpc, "chain_id": 143, "contract": GENESIS, "multicall3": MULTICALL3,
        "block_number": int(block, 16), "scanned_ids": args.last_id, "token_count": len(held),
        "ids_without_token": sorted(token for token, owner in owners.items() if not owner),
        "unique_owners": len(set(held.values())), "seconds": seconds,
    }
    print(json.dumps(summary))
    if args.output:
        args.output.write_text(json.dumps({**summary, "owners": {str(t): o for t, o in sorted(held.items())}}, indent=1) + "\n")


if __name__ == "__main__":
    main()
