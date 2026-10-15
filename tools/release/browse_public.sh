#!/bin/sh
# One scripted visit of a deployed address in the headless browser, as a player without a
# wallet: the menu, QUICK PLAY, START, a few seconds of the match. On a public address the test
# entries (?token=, ?charged=, ?bots=) are ignored, so this is what a visitor gets.
#
#   sh tools/release/browse_public.sh https://<public address> <label> [connector | phone-connector]
#
# With "connector" the visit stops at the menu and opens the window where a wallet app is
# picked, then closes it; "phone-connector" does it through the OTHER WALLET button that a phone
# without a wallet sees beside OPEN IN METAMASK (see tools/unity/checks/browser_check.sh).
#
# Output goes to .artifacts/unity/check-<label>/. It downloads the whole game once (23 MB),
# which a free tunnel counts against its monthly allowance.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
ORIGIN=${1:?usage: browse_public.sh https://<public address> <label> [connector]}
LABEL=${2:?usage: browse_public.sh https://<public address> <label> [connector]}
export CHOG_BROWSER_EXTRA_HEADER='ngrok-skip-browser-warning: 1'
if [ "${3:-}" = "connector" ] || [ "${3:-}" = "phone-connector" ]; then
  exec sh "$ROOT/tools/unity/checks/browser_check.sh" "$3" "$ORIGIN" "$LABEL"
fi
exec sh "$ROOT/tools/unity/checks/browser_check.sh" wallet "$ORIGIN" "$LABEL" none
