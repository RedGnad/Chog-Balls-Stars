#!/bin/sh
# Sets up, and can play through, a rehearsal of staking $CHOG on a match, on a LOCAL COPY of
# Monad mainnet. Nothing is sent to the real chain and no real key is used: the copy is an anvil
# process on this computer, and every account that acts on it is one anvil unlocks.
#
#   anvil --fork-url https://rpc.monad.xyz --network monad --port 8545      # in another terminal
#   sh tools/contracts/rehearse_wager.sh [referee address]
#   sh tools/contracts/rehearse_wager.sh play <wager address> [stake in $CHOG, default 100] [players a team, 1 to 4, default 1]
#
# Without "play" it deploys contracts/src/ChogWager.sol on the copy (a tenth of what the losers
# staked is burned, the owner is anvil's first account) and gives anvil's second and third
# accounts some MON and 10,000 $CHOG each. The referee defaults to the address of the key
# published in the web3.js documentation, a public test value that holds nothing: the game's
# services started for a rehearsal sign with that same key. The last line printed is the wager
# address, to give to the services.
#
# With "play" it runs one match on that contract: the players (anvil's accounts after the
# first, the first half on team 0) stake on tickets signed with the referee's key (REFEREE_KEY,
# by default the same public test key), a signed result that makes team 0 win is brought by a
# third party, and the script prints what each transaction used in gas and who holds what,
# then puts the copy back as it was.
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
. "$HERE/on_copy.sh"
# The key published in the web3.js documentation: public, holds nothing, only good for tests.
TEST_KEY=0x4c0883a69102937d6231471b5dbb6204fe5129617082792ae468d01a3f362318
THIRD_ACCOUNT=0x3C44CdDdB6a900fa2b585dd299e03d12FA4293BC

require_copy

chog_of() { # chog_of <address>: its $CHOG, in whole tokens
  cast from-wei "$(cast call "$CHOG" "balanceOf(address)(uint256)" "$1" --rpc-url "$RPC" | cut -d' ' -f1)"
}

if [ "$1" != "play" ]; then
  REFEREE=${1:-0x2c7536E3605D9C16a7a3D7b1898e529396a65c23}
  echo "deploying the wager on the copy..."
  WAGER=$(forge create "$ROOT/contracts/src/ChogWager.sol:ChogWager" --root "$ROOT/contracts" --rpc-url "$RPC" \
    --unlocked --from "$FIRST_ACCOUNT" --broadcast \
    --constructor-args "$CHOG" "$REFEREE" 1000 "$FIRST_ACCOUNT" | sed -n 's/^Deployed to: //p')
  [ -n "$WAGER" ] || { echo "the wager was not deployed" >&2; exit 1; }
  for PLAYER in "$SECOND_ACCOUNT" "$THIRD_ACCOUNT"; do
    give_mon "$PLAYER"
    give_chog "$PLAYER" 10000
    echo "on the copy: $PLAYER has $(chog_of "$PLAYER") \$CHOG"
  done
  echo "on the copy: the referee of the wager is $(cast call "$WAGER" "referee()(address)" --rpc-url "$RPC")"
  echo "$WAGER"
  exit 0
fi

WAGER=$2
[ -n "$WAGER" ] || { sed -n '2,21p' "$0"; exit 2; }
STAKE=$(cast to-wei "${3:-100}")
A_TEAM=${4:-1}
KEY=${REFEREE_KEY:-$TEST_KEY}
REFEREE=$(cast call "$WAGER" "referee()(address)" --rpc-url "$RPC")
if [ "$(lower "$REFEREE")" != "$(lower "$(cast wallet address --private-key "$KEY")")" ]; then
  echo "the referee of this wager is $REFEREE, which is not the address of the key given" >&2
  exit 1
fi

signed() { # signed <types> <values...>: the referee's signature of the digest of these values
  types=$1
  shift
  cast wallet sign --private-key "$KEY" "$(cast keccak "$(cast abi-encode "f($types)" "$@")")"
}

used() { # used <label> <account> <contract> <signature> [arguments...]: sends and prints the gas used
  label=$1
  account=$2
  shift 2
  gas=$(cast send "$@" --rpc-url "$RPC" --unlocked --from "$account" --json | sed -n 's/.*"gasUsed":"\(0x[0-9a-f]*\)".*/\1/p')
  echo "$label: $(cast to-dec "$gas")"
}

stake_as() { # stake_as <label> <account> <team>: gives the account what it needs, then stakes
  give_mon "$2"
  give_chog "$2" 10000
  cast send "$CHOG" "approve(address,uint256)" "$WAGER" "$STAKE" --rpc-url "$RPC" --unlocked --from "$2" >/dev/null
  ticket=$(signed "bytes32,uint256,address,bytes32,address,uint8,uint256,uint256" \
    "$TICKET" "$CHAIN" "$WAGER" "$MATCH" "$2" "$3" "$STAKE" "$DEADLINE")
  used "$1" "$2" "$WAGER" "stake(bytes32,uint8,uint256,uint256,bytes)" "$MATCH" "$3" "$STAKE" "$DEADLINE" "$ticket"
}

SNAPSHOT=$(cast rpc evm_snapshot --rpc-url "$RPC" | tr -d '"')
CHAIN=$(cast chain-id --rpc-url "$RPC")
TICKET=$(cast keccak "CHOG_WAGER_TICKET")
RESULT=$(cast keccak "CHOG_WAGER_RESULT")
# The key of a match is the hash of the name the game server gives it.
MATCH=$(cast keccak "rehearsal:$(date +%s)")
DEADLINE=$(( $(cast block latest --field timestamp --rpc-url "$RPC") + 600 ))
SUPPLY=$(cast call "$CHOG" "totalSupply()(uint256)" --rpc-url "$RPC" | cut -d' ' -f1)

# The players: anvil's own accounts after the first one, which stays the third party.
number=0
for account in $(cast rpc eth_accounts --rpc-url "$RPC" | tr -d '[]" ' | tr ',' ' '); do
  number=$((number + 1))
  [ "$number" -gt 1 ] && [ "$number" -le $((1 + 2 * A_TEAM)) ] || continue
  team=$(( number - 1 > A_TEAM ? 1 : 0 ))
  [ "$team" = 0 ] && WINNER=$account || LOSER=$account
  stake_as "stake number $((number - 1)) of the match, on team $team" "$account" "$team"
done
echo "held by the wager: $(chog_of "$WAGER") \$CHOG"

# Team 0 won: the referee signs it, and anyone brings the signature.
result=$(signed "bytes32,uint256,address,bytes32,uint8" "$RESULT" "$CHAIN" "$WAGER" "$MATCH" 0)
used "settle, $A_TEAM a side" "$FIRST_ACCOUNT" "$WAGER" "settle(bytes32,uint8,bytes)" "$MATCH" 0 "$result"

# These numbers do not fit the shell's own arithmetic. Every player started with 10,000.
BURNED=$(echo "$SUPPLY - $(cast call "$CHOG" "totalSupply()(uint256)" --rpc-url "$RPC" | cut -d' ' -f1)" | bc)
echo "after: a player of team 0 has $(chog_of "$WINNER"), a player of team 1 has $(chog_of "$LOSER"), the wager $(chog_of "$WAGER") \$CHOG; burned $(cast from-wei "$BURNED")"
echo "state of the match (2 is settled): $(cast call "$WAGER" "matchOf(bytes32)(uint8,uint256,uint64,uint8,uint8)" "$MATCH" --rpc-url "$RPC" | head -1)"
cast rpc evm_revert "$SNAPSHOT" --rpc-url "$RPC" >/dev/null
echo "the copy is back as it was"
