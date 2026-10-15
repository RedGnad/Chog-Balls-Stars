#!/bin/sh
# Makes a new key for the referee of a wager (the key the game services sign tickets and
# results with) and keeps it in a file only this user can read. Prints the address alone: the
# key itself is never shown, and a file that holds something is never written over.
#
#   sh tools/contracts/new_referee_key.sh <file>
#
# The address goes to the deployment (REFEREE=<address> for contracts/script/DeployTestnet.s.sol),
# the file to the services (--referee-key-file for server/src/local.js, REFEREE_KEY for host.js).
set -eu
FILE=${1:?usage: new_referee_key.sh <file>}
if [ -s "$FILE" ]; then
  echo "$FILE already holds a key; its address is $(cast wallet address --private-key "$(cat "$FILE")")" >&2
  exit 1
fi
mkdir -p "$(dirname "$FILE")"
umask 077
# cast prints {"data": [{"address", "private_key", "public_key"}], ...} (Foundry 1.8).
cast wallet new --json | jq -er '.data[0].private_key' > "$FILE"
cast wallet address --private-key "$(cat "$FILE")"
