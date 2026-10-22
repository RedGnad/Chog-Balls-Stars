#!/bin/sh
# Lays out the pages of the game for Vercel and, with "deploy", publishes them there.
#
#   sh tools/release/vercel_site.sh            # lays out .artifacts/vercel/chogarena (https://chogarena.vercel.app)
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
OUT="$ROOT/.artifacts/vercel/chogarena"

mkdir -p "$OUT/hero" "$OUT/genesis-art"
# Kept in step with the sources, files gone from them included; the project link Vercel keeps in
# .vercel/ and the folders of the other two sources are left alone.
rsync -a --delete --exclude .vercel --exclude hero --exclude genesis-art --exclude vercel.json "$ROOT/site/game/" "$OUT/"
rsync -a --delete "$ROOT/web/hero/" "$OUT/hero/"
rsync -a --delete "$ROOT/site/genesis-art/" "$OUT/genesis-art/"

# Since 22 October 2026 the game's public address is https://chogtanks.vercel.app/chog-balls-stars/ (MetaMask's
# scan blocks this project's own address; tools/release/chogtanks_front.sh passes that path on to here). A page
# opened at chogarena.vercel.app goes there, so the sign-in message (Render's PUBLIC_ORIGIN) names the page the
# player is on; a page reached through chogtanks keeps its own address. CHOG_MOVED_TO= (empty) leaves it out.
MOVED_TO=${CHOG_MOVED_TO-https://chogtanks.vercel.app/chog-balls-stars}
if [ -n "$MOVED_TO" ]; then
  for page in "$OUT/index.html" "$OUT/hero/index.html"; do
    python3 - "$page" "$MOVED_TO" <<'PY'
import sys
from pathlib import Path
page, moved_to = Path(sys.argv[1]), sys.argv[2]
text = page.read_text(encoding='utf-8')
script = ('<script>if(location.hostname==="chogarena.vercel.app")location.replace("' + moved_to
          + '"+location.pathname+location.search+location.hash)</script>')
if script not in text:
    page.write_text(text.replace('<head>', '<head>' + script, 1), encoding='utf-8')
PY
  done
fi

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
