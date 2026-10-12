#!/usr/bin/env python3
"""Read-only scan of every Genesis token: tokenURI (metadata id) and ownerOf at one block.

No wallet and no transaction. Token ids are the canonical ERC-721 ids; the metadata id
comes from the tokenURI file name and differs from the token id.
"""
import argparse
import json
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

RPC = "https://rpc.monad.xyz"
CONTRACT = "0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763"
TOKEN_URI = "0xc87b56dd"
OWNER_OF = "0x6352211e"


def rpc(payload, attempts=8):
    """POST a JSON-RPC batch; wait and retry when the public endpoint rate-limits (HTTP 429)."""
    request = urllib.request.Request(RPC, data=json.dumps(payload).encode(), headers={
        "Content-Type": "application/json", "User-Agent": "Mozilla/5.0 ChogathonResearch/1.0"})
    delay = 2.0
    for attempt in range(attempts):
        try:
            with urllib.request.urlopen(request, timeout=40) as response:
                return json.loads(response.read())
        except urllib.error.HTTPError as error:
            if error.code != 429 or attempt == attempts - 1:
                raise
            time.sleep(delay)
            delay = min(delay * 2, 60.0)


def decode_string(result):
    raw = bytes.fromhex(result[2:])
    offset = int.from_bytes(raw[0:32], "big")
    length = int.from_bytes(raw[offset:offset + 32], "big")
    return raw[offset + 32:offset + 32 + length].decode()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--first-id", type=int, default=0)
    parser.add_argument("--last-id", type=int, default=1975)
    parser.add_argument("--batch", type=int, default=10)
    parser.add_argument("--pause", type=float, default=1.0)
    args = parser.parse_args()

    head = {r["id"]: r for r in rpc([
        {"jsonrpc": "2.0", "id": "chain", "method": "eth_chainId", "params": []},
        {"jsonrpc": "2.0", "id": "block", "method": "eth_blockNumber", "params": []}])}
    block = head["block"]["result"]
    tokens, absent = [], []
    # Resume: a partial file from an interrupted run keeps its block and the ids already read.
    partial = args.output.with_suffix(".partial.json")
    if partial.exists():
        saved = json.loads(partial.read_text())
        block, tokens, absent = saved["block_hex"], saved["tokens"], saved["ids_without_token"]
    done = {t["token_id"] for t in tokens} | set(absent)
    for start in range(args.first_id, args.last_id + 1, args.batch):
        if all(token_id in done for token_id in range(start, min(start + args.batch, args.last_id + 1))):
            continue
        ids = range(start, min(start + args.batch, args.last_id + 1))
        payload = []
        for token_id in ids:
            argument = hex(token_id)[2:].rjust(64, "0")
            payload.append({"jsonrpc": "2.0", "id": f"u{token_id}", "method": "eth_call",
                            "params": [{"to": CONTRACT, "data": TOKEN_URI + argument}, block]})
            payload.append({"jsonrpc": "2.0", "id": f"o{token_id}", "method": "eth_call",
                            "params": [{"to": CONTRACT, "data": OWNER_OF + argument}, block]})
        answers = {r["id"]: r for r in rpc(payload)}
        for token_id in ids:
            uri, owner = answers.get(f"u{token_id}", {}), answers.get(f"o{token_id}", {})
            if "result" not in uri or "result" not in owner or len(uri["result"]) < 130:
                absent.append(token_id)
                continue
            token_uri = decode_string(uri["result"])
            tokens.append({"token_id": token_id, "token_uri": token_uri,
                           "metadata_id": int(token_uri.rsplit("/", 1)[1].split(".")[0]),
                           "owner": "0x" + owner["result"][-40:]})
        partial.write_text(json.dumps({"block_hex": block, "tokens": tokens, "ids_without_token": absent}))
        time.sleep(args.pause)

    owners = {}
    for token in tokens:
        owners[token["owner"]] = owners.get(token["owner"], 0) + 1
    record = {
        "recorded_at_utc": datetime.now(timezone.utc).isoformat(), "rpc": RPC,
        "chain_id": int(head["chain"]["result"], 16), "contract": CONTRACT,
        "block_hex": block, "block_number": int(block, 16),
        "scanned_ids": [args.first_id, args.last_id], "token_count": len(tokens),
        "ids_without_token": absent, "unique_owners": len(owners),
        "largest_holdings": sorted(owners.values(), reverse=True)[:10],
        "limits": ["One block: ownership changes afterwards.",
                   "An owner address can be a marketplace, a vault or a staking contract, not a person.",
                   "A token id whose call failed is listed in ids_without_token, whatever the reason."],
        "tokens": tokens}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(record, indent=1) + "\n")
    partial.unlink(missing_ok=True)
    print(json.dumps({k: v for k, v in record.items() if k != "tokens"}, indent=1))


if __name__ == "__main__":
    main()
