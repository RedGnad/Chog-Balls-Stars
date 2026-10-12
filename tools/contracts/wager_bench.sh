#!/bin/sh
# A bench to try the game's own staking screens on this computer: it prepares a LOCAL COPY of
# Monad mainnet and runs game services that stake on it, until stopped.
#
#   anvil --fork-url https://rpc.monad.xyz --network monad --port 8545      # in another terminal
#   sh tools/contracts/wager_bench.sh <build folder>                         # stays in the foreground
#   python3 tools/unity/serve_webgl.py --latest --host 0.0.0.0 --port 8085 --services http://127.0.0.1:8091
#   sh tools/unity/checks/browser_check.sh stake http://<this machine>:8085 <label>
#
# <build folder> is the build the test server serves (.artifacts/unity/<stamp>-<id>): the judge
# replays matches with its simulation assets. On the copy it deploys the wager, and gives two
# test wallets what a staker needs (MON, 10,000 $CHOG, a Genesis): the wallet of the key
# published in the web3.js documentation (Genesis 1795), which the scripted browser signs with,
# and anvil's second account (Genesis 408). Both keys are public test values that hold nothing
# on the real chain. Photon cannot reach this computer: its part is played by
# server/rehearsal/photon_standin.mjs. Nothing here can reach the real chain (on_copy.sh).
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
. "$HERE/on_copy.sh"
BUILD=$1
if [ ! -f "$BUILD/quantum-db.json" ]; then
  sed -n '2,17p' "$0"
  exit 2
fi
# The scripted browser signs with the key published in the web3.js documentation; this is its
# address. The referee is anvil's tenth account, whose key anvil publishes: on this bench a
# player and the referee must not share a key.
BROWSER_WALLET=0x2c7536E3605D9C16a7a3D7b1898e529396a65c23
REFEREE=0xa0Ee7A142d267C1f36714E4a8F75612F20a79720
REFEREE_KEY=0x2a871d0798f97d79848a013d4936a73bf4cc922c825d33c1cf7073dff6d409c6
DOTNET=${DOTNET:-/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet}
DATA="$ROOT/.artifacts/services-wager"
PORT=${PORT:-8091}

require_copy
python3 "$ROOT/tools/quantum/headless.py" verify --dotnet "$DOTNET" >/dev/null
WAGER=$(sh "$HERE/rehearse_wager.sh" "$REFEREE" | tail -1)
give_mon "$BROWSER_WALLET"
give_chog "$BROWSER_WALLET" 10000
give_genesis "$BROWSER_WALLET" 1795
give_genesis "$SECOND_ACCOUNT" 408
echo "wager on the copy: $WAGER"
echo "stakers on the copy: $BROWSER_WALLET (Genesis 1795) and $SECOND_ACCOUNT (Genesis 408)"

mkdir -p "$DATA"
printf '%s\n' "$REFEREE_KEY" > "$DATA/referee-key"
exec node "$ROOT/server/src/local.js" --port "$PORT" --data "$DATA" --rpc "$RPC" --wager "$WAGER" --referee-key-file "$DATA/referee-key" \
  --verifier "$ROOT/.artifacts/dotnet-build/bin/Chog.Quantum.Verifier/release/Chog.Quantum.Verifier.dll" \
  --quantum-db "$BUILD/quantum-db.json" --dotnet "$DOTNET"
