"""Reproduce trait counts from the recorded raw public metadata, without network."""

import argparse
import collections
import hashlib
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--expected", type=int, default=1969)
    args = parser.parse_args()
    data = {}
    duplicates = []
    cids = set()
    for line in args.input.read_text().splitlines():
        record = json.loads(line)
        if hashlib.sha256(record["raw_json"].encode()).hexdigest() != record["raw_sha256"]:
            raise ValueError(f"Raw hash mismatch for metadata {record['metadata_id']}")
        i = record["metadata_id"]
        if i in data:
            duplicates.append(i)
        data[i] = record
        cids.add(record["cid"])
    if len(cids) != 1:
        raise ValueError("Metadata must share one root CID")
    counts = collections.defaultdict(collections.Counter)
    tier_base = collections.defaultdict(collections.Counter)
    per_tier = collections.defaultdict(list)
    examples = {}
    malformed = []
    keys_by_id = {}
    for i, record in sorted(data.items()):
        metadata = json.loads(record["raw_json"])
        traits = {}
        for attribute in metadata["attributes"]:
            key, value = attribute["trait_type"], attribute["value"]
            if key in traits:
                malformed.append({"metadata_id": i, "duplicate_trait": key})
            traits[key] = value
            counts[key][str(value)] += 1
        keys_by_id[i] = set(traits)
        tier = traits.get("Tier", "ABSENT")
        tier_base[tier][traits.get("Base", "ABSENT")] += 1
        per_tier[tier].append(i)
        examples.setdefault(tier, {"metadata_id": i, "name": metadata.get("name"), "traits": traits})
    missing_ids = sorted(set(range(1, args.expected + 1)) - set(data))
    unexpected_ids = sorted(set(data) - set(range(1, args.expected + 1)))
    result = {
        "observed_on_client_date": "2026-10-07",
        "source_file": str(args.input),
        "source_sha256": hashlib.sha256(args.input.read_bytes()).hexdigest(),
        "base_cid": next(iter(cids)),
        "expected_directory_entries": args.expected,
        "retrieved_unique_entries": len(data),
        "complete_directory": not missing_ids and not duplicates and not unexpected_ids,
        "missing_metadata_ids": missing_ids,
        "unexpected_metadata_ids": unexpected_ids,
        "duplicate_metadata_ids": duplicates,
        "malformed_attributes": malformed,
        "trait_categories": len(counts),
        "trait_value_counts": {k: dict(sorted(v.items())) for k, v in sorted(counts.items())},
        "missing_trait_counts": {k: sum(k not in keys for keys in keys_by_id.values()) for k in sorted(counts)},
        "tier_base_counts": {k: dict(v) for k, v in sorted(tier_base.items())},
        "metadata_ids_by_tier": dict(sorted(per_tier.items())),
        "example_by_tier": examples,
        "limits": [
            "Counts cover retrieved metadata entries, not owners, players or activity.",
            "Metadata IDs differ from canonical ERC721 token IDs; use metadataId()/tokenURI().",
            "HTTPS gateway bytes and SHA256 are recorded; IPFS DAG/CID verification was not performed.",
            "A tier label, individual trait frequency and market rarity rank are different measures.",
            "No game balance or retention is established by the counts."
        ],
    }
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({"entries": len(data), "complete": result["complete_directory"],
                      "tiers": dict(counts["Tier"]), "missing_traits": result["missing_trait_counts"]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
