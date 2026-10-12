#!/bin/sh
# Plays the recorded matches of tests/fixtures/replays/ again with the judge of the assembled
# site (site/judge/, site/quantum-db.json) and compares each winner with the one the game
# announced when the match was recorded, which is written in the name of the file
# (...-winner0-... or ...-winner1-...). Ends with an error at the first difference.
#
#   sh tools/release/check_judge.sh [path to dotnet] [folder of the site]
#
# Run on the machine that will judge: it shows that its .NET runtime starts the program and
# that the program reads the assets of the build that is online. The records were made on
# 12 October 2026, with the towers and the cores; a change to the rules of the simulation
# makes them obsolete (those of 11 October no longer reach an end), and they must then be
# recorded again: sh tools/unity/checks/browser_check.sh match <base url> <label>
# '1795&record=1' 330 plays a whole match in the headless browser and leaves its record in
# .artifacts/replays/.
set -eu

ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
DOTNET=${1:-dotnet}
SITE=${2:-$ROOT/site}
OUT=$(mktemp -d)

if [ ! -f "$SITE/judge/Chog.Quantum.Verifier.dll" ] || [ ! -f "$SITE/quantum-db.json" ]; then
  echo "No judge in $SITE: assemble the site on a machine with the .NET SDK." >&2
  exit 1
fi

count=0
for record in "$ROOT"/tests/fixtures/replays/*.json; do
  name=$(basename "$record" .json)
  expected=$(echo "$name" | sed -n 's/.*-winner\([01]\)-.*/\1/p')
  "$DOTNET" "$SITE/judge/Chog.Quantum.Verifier.dll" "$record" "$SITE/quantum-db.json" "$SITE/judge/lut" "$OUT/$name.json" >/dev/null
  found=$(sed -n 's/.*"winnerTeam": *\(-\{0,1\}[0-9]*\).*/\1/p' "$OUT/$name.json" | head -1)
  if [ "$found" != "$expected" ]; then
    echo "JUDGE DIFFERS on $name: the game announced team $expected, the replay says '$found'" >&2
    exit 1
  fi
  echo "judge agrees on $name: team $found"
  count=$((count + 1))
done
echo "$count recorded matches replayed, all with the winner the game announced"
