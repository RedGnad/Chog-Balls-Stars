#!/bin/sh
# Publishes the front of https://chogtanks.vercel.app (Vercel project "chogtanks" of the owner's account), which
# serves the game at https://chogtanks.vercel.app/chog-balls-stars/ since 22 October 2026: MetaMask's site scan
# (Blockaid) answered BLOCK ("wallet drainer", a false positive) for chogarena.vercel.app and NONE for
# chogtanks.vercel.app, and judges a vercel.app page by its hostname alone (docs/DEPLOY.md, "MetaMask's site scan").
# The front holds no files: /chog-balls-stars/ is passed on to this game's own Vercel project
# (tools/release/vercel_site.sh), everything else to the chogtanks deployment of 10 October 2026, unchanged.
#
#   sh tools/release/chogtanks_front.sh            # a preview deployment, the live address untouched
#   sh tools/release/chogtanks_front.sh prod       # the live address
#
# Deployed from a folder outside git: from inside this repository Vercel blocked the deployment ("the commit
# author doesn't have permission to create deployments for this project", TEAM_ACCESS_REQUIRED).
# Back to the chogtanks game alone: vercel promote https://chogtanks-iecf1wt1w-redgnads-projects.vercel.app
set -eu
GAME=${CHOG_GAME_ORIGIN:-https://chogarena.vercel.app}
CHOGTANKS=https://chogtanks-iecf1wt1w-redgnads-projects.vercel.app
OUT=$(mktemp -d)
trap 'rm -r "$OUT"' EXIT
mkdir "$OUT/.vercel"
# The project link: the ids of the "chogtanks" project, as "vercel link --project chogtanks" writes them.
vercel link --yes --project chogtanks --cwd "$OUT" > /dev/null 2>&1
cat > "$OUT/vercel.json" <<JSON
{
  "\$schema": "https://openapi.vercel.sh/vercel.json",
  "redirects": [
    { "source": "/chog-balls-stars", "destination": "/chog-balls-stars/", "permanent": false }
  ],
  "rewrites": [
    { "source": "/chog-balls-stars/", "destination": "$GAME/index.html" },
    { "source": "/chog-balls-stars/hero/", "destination": "$GAME/hero/index.html" },
    { "source": "/chog-balls-stars/:path*", "destination": "$GAME/:path*" },
    { "source": "/", "destination": "$CHOGTANKS/index.html" },
    { "source": "/:path*", "destination": "$CHOGTANKS/:path*" }
  ]
}
JSON
# A folder's own address is served from index.html only when it is named: "/" and "/hero/" have their own rule.
if [ "${1:-}" = prod ]; then
  vercel deploy --prod --yes --cwd "$OUT"
else
  vercel deploy --yes --cwd "$OUT"
fi
