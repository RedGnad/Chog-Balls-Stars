#!/bin/sh
# Reads what contracts/script/DeployTestnet.s.sol deployed (forge keeps it in
# contracts/broadcast/DeployTestnet.s.sol/10143/run-latest.json) and checks it on Monad's
# testnet itself: both contracts have code, the wager is paid in that token and listens to the
# referee given. Prints the settings the game services need, nothing secret.
#
#   sh tools/contracts/testnet_deployment.sh [referee address]
#
# A rehearsal on a local chain started as the testnet writes the same file: its addresses have
# no code on the real testnet, and this check says so.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
RUN="$ROOT/contracts/broadcast/DeployTestnet.s.sol/10143/run-latest.json"
RPC=${RPC:-https://testnet-rpc.monad.xyz}
[ -f "$RUN" ] || { echo "no deployment recorded at $RUN" >&2; exit 1; }
[ "$(cast chain-id --rpc-url "$RPC")" = "10143" ] || { echo "$RPC is not Monad testnet" >&2; exit 1; }

created() { jq -er --arg name "$1" '[.transactions[] | select(.transactionType == "CREATE" and .contractName == $name) | .contractAddress] | last' "$RUN"; }
TOKEN=$(created TestChog)
WAGER=$(created ChogWager)
lower() { echo "$1" | tr 'A-F' 'a-f'; }

for CONTRACT in "$TOKEN" "$WAGER"; do
  if [ "$(cast code "$CONTRACT" --rpc-url "$RPC")" = "0x" ]; then
    echo "$CONTRACT has no code on Monad testnet: the recorded run is not a testnet deployment" >&2
    exit 1
  fi
done
PAID_IN=$(cast call "$WAGER" "chog()(address)" --rpc-url "$RPC")
REFEREE=$(cast call "$WAGER" "referee()(address)" --rpc-url "$RPC")
OWNER=$(cast call "$WAGER" "owner()(address)" --rpc-url "$RPC")
[ "$(lower "$PAID_IN")" = "$(lower "$TOKEN")" ] || { echo "the wager is paid in $PAID_IN, not $TOKEN" >&2; exit 1; }
if [ -n "${1:-}" ] && [ "$(lower "$REFEREE")" != "$(lower "$1")" ]; then
  echo "the wager listens to $REFEREE, not $1" >&2
  exit 1
fi
echo "token   $TOKEN ($(cast call "$TOKEN" "symbol()(string)" --rpc-url "$RPC"))"
echo "wager   $WAGER"
echo "owner   $OWNER"
echo "referee $REFEREE"
echo "WAGER_NETWORK=testnet WAGER_TOKEN=$TOKEN WAGER_ADDRESS=$WAGER"
