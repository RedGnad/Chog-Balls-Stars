#!/bin/sh
# Sets up a rehearsal of the rental of Genesis for $CHOG on a LOCAL COPY of Monad mainnet.
# Nothing is sent to the real chain and no real key is used: the copy is an anvil process on
# this computer, and every account that acts on it is one anvil unlocks or impersonates.
#
#   anvil --fork-url https://rpc.monad.xyz --network monad --port 8545      # in another terminal
#   sh tools/contracts/rehearse_rental.sh [player address] [price a day in $CHOG, default 10]
#
# It deploys contracts/src/ChogRental.sol on the copy (a tenth of each payment burned) and
# prepares one player for both sides of a rental:
#   - as a renter: some MON for gas and 10,000 $CHOG, and two Genesis (token ids 3 and 408)
#     offered at that price by their real holders, on the copy only;
#   - as a lender: the Genesis 1795, taken from its real holder on the copy only, not offered yet.
# The player defaults to the address of the key published in the web3.js documentation, the one
# the scripted browser wallet signs with. The last line printed is the rental address, to give
# to the services:
#
#   node server/src/local.js --port 8091 --data .artifacts/services-fork \
#        --rpc http://127.0.0.1:8545 --rental <address>
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
. "$HERE/on_copy.sh"
PLAYER=${1:-0x2c7536E3605D9C16a7a3D7b1898e529396a65c23}
PRICE=${2:-10}
OFFERED="3 408"
OWN=1795

require_copy

echo "deploying the rental on the copy..."
RENTAL=$(forge create "$ROOT/contracts/src/ChogRental.sol:ChogRental" --root "$ROOT/contracts" --rpc-url "$RPC" \
  --unlocked --from "$FIRST_ACCOUNT" --broadcast \
  --constructor-args "$CHOG" "$GENESIS" 1000 | sed -n 's/^Deployed to: //p')
[ -n "$RENTAL" ] || { echo "the rental was not deployed" >&2; exit 1; }

echo "preparing the player $PLAYER..."
give_mon "$PLAYER"
give_chog "$PLAYER" 10000
give_genesis "$PLAYER" "$OWN"

for TOKEN_ID in $OFFERED; do
  LENDER=$(holder_of "$TOKEN_ID")
  if [ "$(lower "$LENDER")" = "$(lower "$PLAYER")" ]; then
    echo "token $TOKEN_ID is the player's own: not offered here" >&2
    continue
  fi
  send_as "$LENDER" "$RENTAL" "offer(uint256,uint128)" "$TOKEN_ID" "$(cast to-wei "$PRICE")"
  echo "on the copy: token $TOKEN_ID is offered at $PRICE \$CHOG a day by $LENDER"
done

echo "on the copy: token $OWN is held by $(holder_of "$OWN")"
echo "on the copy: the player has $(cast call "$CHOG" "balanceOf(address)(uint256)" "$PLAYER" --rpc-url "$RPC") of \$CHOG"
echo "$RENTAL"
