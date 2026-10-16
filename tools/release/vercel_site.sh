#!/bin/sh
# Lays out the pages of the game for Vercel and, with "deploy", publishes them there.
#
#   sh tools/release/vercel_site.sh            # lays out .artifacts/vercel/chog-genesis
#   sh tools/release/vercel_site.sh deploy     # and publishes it to production (vercel CLI, logged in)
#
# Vercel serves what a browser opens: the game (site/game), the hero page (web/hero) and the
# portraits (site/genesis-art), under the same paths as on Render. Everything under /api/ is passed
# on to the services on Render, so the page and its services keep one address for the browser.
# Photon's calls still go to Render directly. Nothing of the judge is copied.
# Render's PUBLIC_ORIGIN must name the Vercel address: it is written in the sign-in message.
set -eu

HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
SERVICES=${CHOG_SERVICES_ORIGIN:-https://chog-genesis-game.onrender.com}
# The folder's name is the Vercel project's name on the first deploy.
OUT="$ROOT/.artifacts/vercel/chog-genesis"

mkdir -p "$OUT/hero" "$OUT/genesis-art"
# Kept in step with the sources, files gone from them included; the project link Vercel keeps in
# .vercel/ and the folders of the other two sources are left alone.
rsync -a --delete --exclude .vercel --exclude hero --exclude genesis-art --exclude vercel.json "$ROOT/site/game/" "$OUT/"
rsync -a --delete "$ROOT/web/hero/" "$OUT/hero/"
rsync -a --delete "$ROOT/site/genesis-art/" "$OUT/genesis-art/"

# The three .gz files of the Unity build are sent as they are, with the header that tells the
# browser they are compressed (as server/src/static.js does on Render).
cat > "$OUT/vercel.json" <<EOF
{
  "\$schema": "https://openapi.vercel.sh/vercel.json",
  "rewrites": [
    { "source": "/api/:path*", "destination": "$SERVICES/api/:path*" }
  ],
  "headers": [
    { "source": "/Build/(.*).js.gz", "headers": [
      { "key": "Content-Type", "value": "application/javascript; charset=utf-8" },
      { "key": "Content-Encoding", "value": "gzip" } ] },
    { "source": "/Build/(.*).wasm.gz", "headers": [
      { "key": "Content-Type", "value": "application/wasm" },
      { "key": "Content-Encoding", "value": "gzip" } ] },
    { "source": "/Build/(.*).data.gz", "headers": [
      { "key": "Content-Type", "value": "application/octet-stream" },
      { "key": "Content-Encoding", "value": "gzip" } ] },
    { "source": "/genesis-art/(.*)", "headers": [
      { "key": "Cache-Control", "value": "public, max-age=86400" } ] }
  ]
}
EOF

echo "laid out $OUT: $(find "$OUT" -type f ! -path '*/.vercel/*' | wc -l | tr -d ' ') files, $(du -sh "$OUT" | cut -f1)"
if [ "${1:-}" = deploy ]; then
  cd "$OUT"
  vercel deploy --prod --yes
fi
