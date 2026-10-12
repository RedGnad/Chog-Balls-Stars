#!/bin/sh
# One staked match from end to end on this computer: sign-in, admission of the Genesis, tickets,
# stakes, lock, the record of a real match, the judge that plays it again, the referee's
# signature, the payment by the contract. See server/rehearsal/secure_match.mjs for what is real
# and what is played (Photon's cloud is played).
#
#   anvil --fork-url https://rpc.monad.xyz --network monad --port 8545      # in another terminal
#   sh tools/contracts/rehearse_secure_match.sh <replay.json> <build folder> [none | late-stake | wrong-team | no-lock]
#
# <replay.json> is the record a game page sent with ?record=1 (.artifacts/replays/), of a match
# of two players in opposite teams (browser_check.sh duel-host and duel-guest); <build folder>
# is the folder of the build that match was played with (.artifacts/unity/<stamp>-<id>), which
# holds the simulation assets the judge needs. The last argument makes a player cheat, to see
# the match called off and every stake given back.
#
# It only acts on a LOCAL COPY of Monad mainnet (an anvil process): on_copy.sh stops it
# otherwise. The referee's key is the one published in the web3.js documentation, a public test
# value that holds nothing. The game services it starts listen on the loopback address only.
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
. "$HERE/on_copy.sh"
RECORD=$1
BUILD=$2
CHEAT=${3:-none}
if [ ! -f "$RECORD" ] || [ ! -f "$BUILD/quantum-db.json" ]; then
  sed -n '2,19p' "$0"
  exit 2
fi
# The key published in the web3.js documentation: public, holds nothing, only good for tests.
TEST_KEY=0x4c0883a69102937d6231471b5dbb6204fe5129617082792ae468d01a3f362318
DOTNET=${DOTNET:-/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet}
DATA="$ROOT/.artifacts/services-wager"
PORT=${PORT:-8091}

require_copy

echo "building the program that plays a match again..."
python3 "$ROOT/tools/quantum/headless.py" verify --dotnet "$DOTNET" >/dev/null
VERIFIER="$ROOT/.artifacts/dotnet-build/bin/Chog.Quantum.Verifier/release/Chog.Quantum.Verifier.dll"

WAGER=$(sh "$HERE/rehearse_wager.sh" | tail -1)
echo "wager on the copy: $WAGER"

mkdir -p "$DATA"
printf '%s\n' "$TEST_KEY" > "$DATA/referee-key"
node "$ROOT/server/src/local.js" --port "$PORT" --data "$DATA" --rpc "$RPC" --wager "$WAGER" --referee-key-file "$DATA/referee-key" \
  --verifier "$VERIFIER" --quantum-db "$BUILD/quantum-db.json" --dotnet "$DOTNET" > "$DATA/services.log" 2>&1 &
SERVICES=$!
trap 'kill $SERVICES 2>/dev/null' EXIT
tries=0
until curl -s -o /dev/null "http://127.0.0.1:$PORT/api/health"; do
  tries=$((tries + 1))
  [ "$tries" -lt 50 ] || { echo "the game services did not start: see $DATA/services.log" >&2; exit 1; }
  sleep 0.2
done

node "$ROOT/server/rehearsal/secure_match.mjs" --record "$RECORD" --wager "$WAGER" --services "http://127.0.0.1:$PORT" --data "$DATA" --rpc "$RPC" --cheat "$CHEAT"
