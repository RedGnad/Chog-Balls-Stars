#!/bin/sh
# Sets up a rehearsal of the $CHOG purchase on a LOCAL COPY of Monad mainnet. Nothing is sent to
# the real chain and no real key is used: the copy is an anvil process on this computer, and
# every account that acts on it is one anvil unlocks or impersonates.
#
#   anvil --fork-url https://rpc.monad.xyz --network monad --port 8545      # in another terminal
#   sh tools/contracts/rehearse_shop.sh [buyer address] [token id, default 408] [price in $CHOG, default 500]
#
# It deploys contracts/src/ChogShop.sol on the copy (half of each payment burned, the rest to a
# treasury that is anvil's second account), puts item 1 on sale, and prepares a buyer: some MON
# for gas, 10,000 $CHOG, and the Genesis with that token id, taken from its real holder on the
# copy only. The buyer defaults to the address of the key published in the web3.js
# documentation, the one the scripted browser wallet signs with. The last line printed is the
# shop address, to give to the services:
#
#   node server/src/local.js --port 8091 --data .artifacts/services-fork \
#        --rpc http://127.0.0.1:8545 --shop <address>
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
. "$HERE/on_copy.sh"
BUYER=${1:-0x2c7536E3605D9C16a7a3D7b1898e529396a65c23}
TOKEN_ID=${2:-408}
PRICE=${3:-500}
# The shop's owner and its treasury.
OWNER=$FIRST_ACCOUNT
TREASURY=$SECOND_ACCOUNT

require_copy

echo "deploying the shop on the copy..."
SHOP=$(forge create "$ROOT/contracts/src/ChogShop.sol:ChogShop" --root "$ROOT/contracts" --rpc-url "$RPC" \
  --unlocked --from "$OWNER" --broadcast \
  --constructor-args "$CHOG" "$GENESIS" "$TREASURY" 5000 "$OWNER" | sed -n 's/^Deployed to: //p')
[ -n "$SHOP" ] || { echo "the shop was not deployed" >&2; exit 1; }
cast send "$SHOP" "setItem(uint256,uint128,bool)" 1 "$(cast to-wei "$PRICE")" true \
  --rpc-url "$RPC" --unlocked --from "$OWNER" >/dev/null

echo "preparing the buyer $BUYER..."
give_mon "$BUYER"
give_chog "$BUYER" 10000
give_genesis "$BUYER" "$TOKEN_ID"

echo "on the copy: token $TOKEN_ID is held by $(holder_of "$TOKEN_ID")"
echo "on the copy: the buyer has $(cast call "$CHOG" "balanceOf(address)(uint256)" "$BUYER" --rpc-url "$RPC") of \$CHOG, item 1 costs $PRICE \$CHOG"
echo "$SHOP"
