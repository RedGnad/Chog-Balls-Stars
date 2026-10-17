#!/bin/sh
# The test wallet of this Mac rents a Genesis that its holder offers, on Monad mainnet, so that the
# scripted browser (CHOG_BROWSER_WALLET_KEY) and the owner can still play that Genesis after it is
# sold: a rental that was paid runs to its end (ChogRental.sol), at most 30 days from now.
#
#   sh tools/contracts/rent_as_test_wallet.sh                # the wallet's address, MON, $CHOG and rentals
#   sh tools/contracts/rent_as_test_wallet.sh 1795 30        # rents token 1795 for 30 days (sends two transactions)
#
# The key is in .artifacts/test-renter/key (never printed, never committed); its address in
# .artifacts/test-renter/address. The wallet needs $CHOG for the price and a little MON for gas.
set -eu

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
WALLET="$ROOT/.artifacts/test-renter"
RPC=${MONAD_RPC:-https://rpc.monad.xyz}
CHOG=0x350035555E10d9AfAF1566AaebfCeD5BA6C27777
RENTAL=0x7f1271d43b0e41e2eeddd5290f459fdc6196a19a
export PATH="$HOME/.foundry/bin:$PATH"

ADDRESS=$(cat "$WALLET/address")
echo "test wallet $ADDRESS"
echo "MON   $(cast balance "$ADDRESS" --rpc-url "$RPC" --ether)"
echo "CHOG  $(cast from-wei "$(cast call "$CHOG" 'balanceOf(address)(uint256)' "$ADDRESS" --rpc-url "$RPC" | cut -d' ' -f1)")"
echo "rents $(cast call "$RENTAL" 'rentalsOf(address)(uint256[],uint64[])' "$ADDRESS" --rpc-url "$RPC" | tr '\n' ' ')"
[ $# -eq 0 ] && exit 0

TOKEN=$1
DAYS=$2
OFFER=$(cast call "$RENTAL" 'offerOf(uint256)(bool,uint128,address)' "$TOKEN" --rpc-url "$RPC")
OFFERED=$(echo "$OFFER" | sed -n 1p)
PRICE=$(echo "$OFFER" | sed -n 2p | cut -d' ' -f1)
[ "$OFFERED" = true ] || { echo "token $TOKEN is not offered for rent"; exit 1; }
COST=$(echo "$PRICE * $DAYS" | bc)
echo "token $TOKEN offered at $(cast from-wei "$PRICE") \$CHOG a day: $DAYS days cost $(cast from-wei "$COST") \$CHOG"

KEY=$(cat "$WALLET/key")
cast send "$CHOG" 'approve(address,uint256)' "$RENTAL" "$COST" --private-key "$KEY" --rpc-url "$RPC" >/dev/null
echo "approved"
cast send "$RENTAL" 'rent(uint256,uint256,uint128)' "$TOKEN" "$DAYS" "$PRICE" --private-key "$KEY" --rpc-url "$RPC" | grep -E '^(status|transactionHash)'
echo "rents $(cast call "$RENTAL" 'rentalsOf(address)(uint256[],uint64[])' "$ADDRESS" --rpc-url "$RPC" | tr '\n' ' ')"
