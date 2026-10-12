#!/bin/sh
# Measures, on a LOCAL COPY of Monad mainnet prepared by rehearse_rental.sh, what each
# transaction of a rental costs in gas, then puts the copy back as it was. Monad charges the gas
# limit of a transaction, not what it uses: the limits the game sends (GenesisRental.cs) are set
# from these numbers.
#
#   sh tools/contracts/measure_rental_gas.sh <rental address> [player address]
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
. "$HERE/on_copy.sh"
RENTAL=$1
PLAYER=${2:-0x2c7536E3605D9C16a7a3D7b1898e529396a65c23}
[ -n "$RENTAL" ] || { sed -n '2,8p' "$0"; exit 2; }
require_copy

used() { # used <label> <account> <contract> <signature> [arguments...]
  label=$1
  account=$2
  shift 2
  cast rpc anvil_impersonateAccount "$account" --rpc-url "$RPC" >/dev/null
  gas=$(cast send "$@" --rpc-url "$RPC" --unlocked --from "$account" --json | sed -n 's/.*"gasUsed":"\(0x[0-9a-f]*\)".*/\1/p')
  cast rpc anvil_stopImpersonatingAccount "$account" --rpc-url "$RPC" >/dev/null
  echo "$label: $(cast to-dec "$gas")"
}

SNAPSHOT=$(cast rpc evm_snapshot --rpc-url "$RPC" | tr -d '"')
PRICE=$(cast to-wei 10)
used "offer, first time for a Genesis" "$PLAYER" "$RENTAL" "offer(uint256,uint128)" 1795 "$PRICE"
used "offer, a new price" "$PLAYER" "$RENTAL" "offer(uint256,uint128)" 1795 "$(cast to-wei 25)"
used "withdraw" "$PLAYER" "$RENTAL" "withdraw(uint256)" 1795
used "approve the rental for the cost" "$PLAYER" "$CHOG" "approve(address,uint256)" "$RENTAL" "$(cast to-wei 100)"
used "rent, first time for this player and Genesis" "$PLAYER" "$RENTAL" "rent(uint256,uint256,uint128)" 408 3 "$PRICE"
used "rent, more days on the same Genesis" "$PLAYER" "$RENTAL" "rent(uint256,uint256,uint128)" 408 1 "$PRICE"
cast rpc evm_revert "$SNAPSHOT" --rpc-url "$RPC" >/dev/null
echo "the copy is back as it was"
