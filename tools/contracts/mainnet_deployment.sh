#!/bin/sh
# Reads what contracts/script/DeployMainnet.s.sol deployed (forge keeps it in
# contracts/broadcast/DeployMainnet.s.sol/143/run-latest.json) and checks it on Monad mainnet
# itself: the three contracts have code, each is paid in the real $CHOG, the rental and the shop
# read the Genesis collection, the wager listens to the referee given, the second form is on sale.
# Prints the settings of the game services, nothing secret: one a line, they are what
# tools/release/serve_public.sh --mainnet reads from .artifacts/services-public/mainnet-contracts.
#
#   sh tools/contracts/mainnet_deployment.sh <referee address> [settings file to write]
#
# RPC=http://127.0.0.1:8546 checks a rehearsal on a local copy of mainnet instead.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
RUN="$ROOT/contracts/broadcast/DeployMainnet.s.sol/143/run-latest.json"
RPC=${RPC:-https://rpc.monad.xyz}
CHOG=0x350035555e10d9afaf1566aaebfced5ba6c27777
GENESIS=0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763
REFEREE_GIVEN=${1:?usage: mainnet_deployment.sh <referee address> [settings file]}
[ -f "$RUN" ] || { echo "no deployment recorded at $RUN" >&2; exit 1; }
[ "$(cast chain-id --rpc-url "$RPC")" = "143" ] || { echo "$RPC is not Monad mainnet" >&2; exit 1; }

created() { jq -er --arg name "$1" '[.transactions[] | select(.transactionType == "CREATE" and .contractName == $name) | .contractAddress] | last' "$RUN"; }
lower() { echo "$1" | tr 'A-F' 'a-f'; }
WAGER=$(created ChogWager)
RENTAL=$(created ChogRental)
SHOP=$(created ChogShop)
for CONTRACT in "$WAGER" "$RENTAL" "$SHOP"; do
  if [ "$(cast code "$CONTRACT" --rpc-url "$RPC")" = "0x" ]; then
    echo "$CONTRACT has no code on this chain: the recorded run is not a deployment on it" >&2
    exit 1
  fi
done
for CONTRACT in "$WAGER" "$RENTAL" "$SHOP"; do
  [ "$(lower "$(cast call "$CONTRACT" "chog()(address)" --rpc-url "$RPC")")" = "$CHOG" ] || { echo "$CONTRACT is not paid in \$CHOG" >&2; exit 1; }
done
for CONTRACT in "$RENTAL" "$SHOP"; do
  [ "$(lower "$(cast call "$CONTRACT" "genesis()(address)" --rpc-url "$RPC")")" = "$GENESIS" ] || { echo "$CONTRACT does not read the Genesis collection" >&2; exit 1; }
done
REFEREE=$(cast call "$WAGER" "referee()(address)" --rpc-url "$RPC")
[ "$(lower "$REFEREE")" = "$(lower "$REFEREE_GIVEN")" ] || { echo "the wager listens to $REFEREE, not $REFEREE_GIVEN" >&2; exit 1; }
ITEM=$(cast call "$SHOP" "items(uint256)(uint128,bool)" 1 --rpc-url "$RPC" | tr '\n' ' ')
echo "wager   $WAGER (owner $(cast call "$WAGER" "owner()(address)" --rpc-url "$RPC"), referee $REFEREE)"
echo "rental  $RENTAL (burned share $(cast call "$RENTAL" "burnedShare()(uint16)" --rpc-url "$RPC") of 10000)"
echo "shop    $SHOP (owner $(cast call "$SHOP" "owner()(address)" --rpc-url "$RPC"), treasury $(cast call "$SHOP" "treasury()(address)" --rpc-url "$RPC"), second form: $ITEM)"
SETTINGS="WAGER_ADDRESS=$WAGER
RENTAL_ADDRESS=$RENTAL
SHOP_ADDRESS=$SHOP"
echo "$SETTINGS"
if [ -n "${2:-}" ]; then
  printf '%s\n' "$SETTINGS" > "$2"
  echo "written to $2"
fi
