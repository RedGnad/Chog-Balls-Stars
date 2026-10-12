#!/bin/sh
# Asks a deployed address for what a player's browser and Photon would ask, and prints what
# came back. It changes nothing and needs no secret.
#
#   sh tools/release/check_public.sh https://<public address>
#
# The header ngrok-skip-browser-warning only matters behind a free ngrok tunnel, which puts a
# warning page in front of a browser's first visit; elsewhere it is ignored.
set -u

ORIGIN=${1:?usage: check_public.sh https://<public address>}
SKIP='ngrok-skip-browser-warning: 1'

status() {
  curl -s -o /dev/null --max-time 30 -H "$SKIP" -w '%{http_code} %{size_download} bytes, %{content_type}' "$@"
}

echo "page                 $(status "$ORIGIN/")"
echo "health               $(curl -s --max-time 30 -H "$SKIP" "$ORIGIN/api/health")"
# The four files of a Unity Web build as this project makes it (gzip, named webgl). A browser
# only reads the three .gz files when they come with "Content-Encoding: gzip". Asked as a
# browser does (it accepts gzip) and for the headers alone: the files are 23 MB together, and
# a tunnel may count every byte.
for file in webgl.loader.js webgl.framework.js.gz webgl.wasm.gz webgl.data.gz; do
  headers=$(curl -s -I --max-time 30 -H "$SKIP" -H 'Accept-Encoding: gzip' "$ORIGIN/Build/$file" | tr -d '\r')
  code=$(printf '%s\n' "$headers" | head -1 | cut -d' ' -f2)
  encoding=$(printf '%s\n' "$headers" | grep -i '^content-encoding' | cut -d' ' -f2)
  length=$(printf '%s\n' "$headers" | grep -i '^content-length' | cut -d' ' -f2)
  echo "Build/$file   $code, ${length:-?} bytes, encoding ${encoding:-none}"
done
echo "portrait 1795        $(status "$ORIGIN/genesis-art/256/1795.jpg")"
echo "hero page            $(status "$ORIGIN/hero/?n=1287")"
echo "photon, for the game $(curl -s --max-time 30 -H "$SKIP" "$ORIGIN/api/photon")"
echo "wager, for the game  $(curl -s --max-time 30 -H "$SKIP" "$ORIGIN/api/wager")"
echo "auth without key     $(status "$ORIGIN/api/photon/auth")"
echo "hook without secret  $(status -X POST -H 'Content-Type: application/json' -d '{}' "$ORIGIN/api/photon/hook/game/create")"
echo "simulation assets    $(status "$ORIGIN/quantum-db.json") (must not be served)"
echo "release notes        $(status "$ORIGIN/RELEASE.json") (must not be served)"
