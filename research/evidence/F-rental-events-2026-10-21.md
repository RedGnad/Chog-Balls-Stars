# Reading the rental's events on Monad: what each Genesis earned by being rented (21 October 2026)

Stream F, lot 3. The rental contract keeps earnings by holder (`earnings(address)`), not by Genesis, so "what this Genesis
earned by being rented" can only come from its `Earned(tokenId, lender, amount)` events (and `Rented` for the count).

## What the public endpoints allow for `eth_getLogs` (checked with `curl`, address = the rental, latest block 115,368,212)

| Endpoint | 1,000 blocks | 10,000 blocks | 50,000 blocks |
| --- | --- | --- | --- |
| rpc.monad.xyz | "eth_getLogs is limited to a 100 range" | same | same |
| rpc1.monad.xyz | "block range too large" | same | same |
| rpc2.monad.xyz | `[]` (served) | `[]` (served) | "getLogs request exceeded max allowed range" |
| rpc3.monad.xyz | "Block range is too large" | same | same |

rpc2.monad.xyz answers a `node` User-Agent but refuses `Python-urllib` (HTTP 403); the ledger sends a browser-like one.
Explorer APIs: `api.monadscan.com` V1 is deprecated, Etherscan V2 (`chainid=143`) wants an API key. Not used.

## Deployment blocks (binary search of `eth_getCode` on rpc2.monad.xyz, `scratchpad/find_deploy_block.py`)

- ChogRental v2 `0x90c26090aea05a3dc18d6ba8fd80afe8f933bf86`: block **114,126,084**.
- ChogWager `0xc7524a808d2b00b43a4d3e2becb8500e409b41e3`: block 113,859,575 (not read today; noted for a later ledger).

## The ledger, read live (`server/src/rental-ledger.js`, windows of 10,000 blocks from the deployment block)

Run from this Mac at about 14:40 UTC: **8.7 s** for the whole history (about 125 windows), caught up to block
115,371,730. Figures found: Genesis **1795** earned `58900462962962962` (0.0589 $CHOG, about $0.0001 at $0.0015 a $CHOG)
over **1 rental**; no other Genesis has an `Earned` or `Rented` event yet. The services read this in the background at
startup, then every minute from the last block read; `GET /api/rental/earned?tokens=1795` answers it with `upToBlock`
and `caughtUp`.
