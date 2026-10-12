#!/bin/sh
# Starts the game as a host would (server/src/host.js: the game, the hero page, the portraits
# and the services on one address) from the assembled site/, with the judge of this Mac. Meant
# to stand behind a tunnel, which gives the game a public https address while this Mac runs.
#
#   python3 tools/release/assemble_site.py --latest
#   ngrok http 8087                      (in another terminal: it prints the public address)
#   sh tools/release/serve_public.sh https://<public address> [--port 8087] [--photon-auth]
#
# --photon-auth tells the game to connect to Photon with the player's session or pass. Use it
# only once Photon's dashboard asks these services who a player is (docs/DEPLOY.md, "Photon"):
# before that, nobody could connect.
#
# Its data (careers, sessions, the secret Photon must send, the records of the matches) is kept
# apart from the local test services, in .artifacts/services-public/. No stake is taken here:
# no wager contract is deployed. A file reown-project-id in that folder (the Reown project id,
# 32 hex digits) lets a phone's normal browser connect a wallet app; without it only the
# wallet of the browser is offered.
set -eu

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
ORIGIN=${1:?usage: serve_public.sh https://<public address> [--port 8087] [--photon-auth]}
shift
PORT=8087
PHOTON_AUTH=
while [ $# -gt 0 ]; do
  case "$1" in
    --port) PORT=$2; shift 2 ;;
    --photon-auth) PHOTON_AUTH=1; shift ;;
    *) echo "unknown option $1" >&2; exit 2 ;;
  esac
done

# The .NET that runs the judge on this Mac: the one Unity ships, unless another is named.
DOTNET=${DOTNET:-/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet}

if [ ! -f "$ROOT/site/game/index.html" ]; then
  echo "No assembled site: run python3 tools/release/assemble_site.py --latest" >&2
  exit 1
fi

# The judge comes with the assembled site (site/judge/, site/quantum-db.json): host.js finds
# it there. Without it the game is served the same and no match gets a verdict.
if [ ! -f "$ROOT/site/judge/Chog.Quantum.Verifier.dll" ] || [ ! -f "$ROOT/site/quantum-db.json" ]; then
  echo "The assembled site has no judge: this build is served without one." >&2
fi

export PUBLIC_ORIGIN="$ORIGIN" PORT PHOTON_AUTH DOTNET
export DATA_DIR="$ROOT/.artifacts/services-public"
exec node "$ROOT/server/src/host.js"
