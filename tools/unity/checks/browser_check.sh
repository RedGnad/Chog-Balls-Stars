#!/bin/sh
# Scripted runs of the game in a muted, touch-emulated headless Chrome (960 x 540), through the
# address of a running test server. They are the checks made before a build is given to the
# phone: what the console says is printed, the screenshots are left to be looked at.
#
#   browser_check.sh wallet <base url> <label> <address | refuse | none> [slot x, default 433]
#       menu, CONNECT WALLET, tap one Genesis of the panel, QUICK PLAY, START.
#       The wallet is a stand-in that shares that address and signs nothing.
#   browser_check.sh connector <base url> <label> [x y of a tap that closes the window, default 30 30]
#       a page without a wallet whose services give a Reown project id: menu, CONNECT WALLET
#       (the window of the wallet apps is loaded from the network and opens over the game),
#       then a tap beside it, which closes it. No wallet app answers here: this shows that the
#       window comes and goes and that the game hears of both.
#   browser_check.sh phone-connector <https base url> <label> [x y of a tap that closes the window]
#       the same on an HTTPS address, where the page posing as a phone browser offers OPEN IN
#       METAMASK (not tapped: it leaves for MetaMask's site) and, beside it, OTHER WALLET, which
#       is tapped and opens the window of wallet apps.
#   browser_check.sh signed <base url> <label> [seconds to wait for the end of the match, default 420]
#       connect, SIGN IN, play a match to its end, see the career credited. The stand-in wallet
#       really signs with the private key published in the web3.js documentation, a public test
#       value that holds nothing: the services behind the address must have been started with
#       --pretend-holder 0x2c7536E3605D9C16a7a3D7b1898e529396a65c23=408,1795,3 on their own
#       port and data folder (see server/README.md).
#   browser_check.sh match <base url> <label> <token> [seconds after the special, default 20] [bots "181,70"]
#       enter as that Genesis with its special charged, fire it once, let the match run.
#   browser_check.sh film <base url> <label> <token> [milliseconds, default 1800] [aim "x1,y1,x2,y2"] [walk ms]
#       enter as that Genesis, fire its special and keep every frame the page draws afterwards
#       (touch-browser-special-film-NN-<ms>ms.jpg). A screenshot takes several hundred
#       milliseconds here: this is the way to look at an attack that moves. The aim defaults to
#       the right of the spawn point, where the ground is clear; upwards a crate stops a beam.
#       With [walk ms] the hero first walks up for that long (5500 reaches the middle, where
#       the bots fight): nobody stands near the spawn point a few seconds after the start.
#   browser_check.sh lend <base url> <label> [share | stop | back, default share]
#       connect, SIGN IN, LEND IT, then that button of the lending screen; prints the link the
#       page prepared ("EVALUATE ..."). Same signing stand-in and same services as "signed".
#   browser_check.sh guest <link with ?pass=...> <label> [seconds to wait for the end of the match, default 420]
#       open a lent link without a wallet, look at the menu, play a match to its end and see
#       the career of the lent Genesis credited.
#   browser_check.sh respawn <base url> <label> <token> [seconds to watch, default 90] [pictures, default 30]
#       enter as that Genesis, walk up into the middle where the bots fight, and take pictures
#       while the hero is beaten and comes back: for the five seconds after a return a hero
#       cannot be hurt and the sample shows a see-through capsule around it.
#   browser_check.sh rentout <base url> <label> [offer | dearer | withdraw, default offer]
#       connect, RENT IT OUT for the chosen Genesis, then: OFFER IT at the price shown, or one
#       step dearer, or TAKE IT BACK. The stand-in wallet sends real transactions to the chain
#       behind CHOG_BROWSER_WALLET_RPC (default http://127.0.0.1:8545): only ever a local copy
#       prepared by tools/contracts/rehearse_rental.sh, with services started with --rental.
#   browser_check.sh buy <base url> <label>
#       connect, tap the first Genesis of the wallet, then the form button that sells its second form: the
#       stand-in wallet sends the approval and the purchase to the copy behind CHOG_BROWSER_WALLET_RPC.
#   browser_check.sh rent <base url> <label> [offer slot 1 to 6, default 1] [days button 1 to 3, default 1]
#                         [play seconds, default 0] [where RENT A HERO is: title | large, default title]
#       connect, RENT A HERO, pick that offer and that length, pay, and see the rented Genesis
#       chosen. With [play seconds] it then signs in and plays a match for that long, to see
#       the career of the rented Genesis credited. Same copy of the chain as "rentout".
#   browser_check.sh stake <base url> <label> [seconds to wait for the end of the match, default 420]
#       connect, SIGN IN, QUICK PLAY, STAKE on the preparation screen, START, play to the end
#       and send the record of the match. Needs the bench of tools/contracts/wager_bench.sh;
#       set CHOG_BROWSER_PROFILE to a folder to find the stake again afterwards.
#   browser_check.sh stake-host <base url> <label> [seconds to wait for the end, default 420]
#   browser_check.sh stake-guest <base url> <label> [seconds to stay, default 420]
#       two pages that stake against each other: start the host, then the guest with another
#       wallet (CHOG_STAKER_KEY=<a public test key>, for instance the key of anvil's second
#       account). The guest joins, changes team and stakes; the host stakes and starts.
#       On Monad's testnet (a public instance started with --testnet-wager): each page with its
#       own throwaway key holding testnet MON (CHOG_STAKER_KEY), CHOG_BROWSER_WALLET_SIGN=1 so the
#       stand-in signs real transactions, CHOG_BROWSER_WALLET_RPC=https://testnet-rpc.monad.xyz,
#       and CHOG_TAKE_TEST_TOKENS=1 to tap GET 1000 TEST $CHOG before staking.
#   browser_check.sh staked <base url> <label> [look | claim, default look]
#       the menu again with the same CHOG_BROWSER_PROFILE: what became of the stake, and with
#       "claim" the button that brings the referee's decision to the contract.
#   browser_check.sh duel-host <base url> <label> <token> [seconds before START, default 40] [seconds to wait for the end, default 420]
#   browser_check.sh duel-guest <base url> <label> <token> [seconds to stay, default 420]
#       two pages in one quick match, in opposite teams: start the host, then the guest a few
#       seconds later. The guest joins the room the host opened and taps Change Team; the host
#       starts the match. A token such as "1795&record=1" makes the host send the record of
#       the match to the test server when it ends (.artifacts/replays/).
#
# <base url> is for example http://192.168.1.85:8084 : use the LAN address, never 127.0.0.1,
# which browsers and Unity treat as a special case. Output goes to .artifacts/unity/check-<label>/.
# The tap positions are those of the sample's menu and of the wallet panel at 960 x 540; they
# move if those screens change.
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../../.." && pwd)
MODE=$1
BASE=$2
LABEL=$3
if [ -z "$MODE" ] || [ -z "$BASE" ] || [ -z "$LABEL" ]; then
  sed -n '2,48p' "$0"
  exit 2
fi
export CHOG_BROWSER_TOUCH=1
export CHOG_BROWSER_OUTPUT_DIR="$ROOT/.artifacts/unity/check-$LABEL"

# Taps at 960 x 540. QUICK PLAY moved under the hero of the menu on 16 October (ChogMenuSkin).
QUICK_PLAY="119 376"
START="480 440"
CHANGE_TEAM="480 327"
# The STAKE button of the preparation screen and the button of the menu's stake strip.
STAKE="242 510"
# On the testnet, the button beside the stakes line that takes test tokens from the tap.
TEST_TOKENS="308 470"
CLAIM="693 27"
CONNECT="629 434"
# Beside it on a phone without a wallet: the window of wallet apps.
OTHER_WALLET="840 434"
SIGN_IN="490 451"
LEND="840 451"
RENT_OUT="704 451"
MARKET_BY_TITLE="632 176"
# The key published in the web3.js documentation: public, holds nothing, only good for tests.
TEST_KEY=0x4c0883a69102937d6231471b5dbb6204fe5129617082792ae468d01a3f362318

tap() { # tap <number> <x> <y> <milliseconds to wait after>
  suffix=$([ "$1" = 1 ] && echo "" || echo "_$1")
  export "CHOG_BROWSER_CLICK${suffix}_X=$2" "CHOG_BROWSER_CLICK${suffix}_Y=$3" "CHOG_BROWSER_AFTER_CLICK${suffix}_MS=$4"
}

case "$MODE" in
  wallet)
    [ "$4" != "none" ] && export CHOG_BROWSER_WALLET="$4"
    export CHOG_BROWSER_URL="$BASE/?perf=1" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 5000
    tap 2 "${5:-433}" 248 3000
    tap 3 $QUICK_PLAY 16000
    tap 4 $START 9000
    ;;
  connector)
    export CHOG_BROWSER_URL="$BASE/?perf=1" CHOG_BROWSER_SETTLE_MS=8000
    tap 1 $CONNECT 15000
    tap 2 "${4:-30}" "${5:-30}" 5000
    ;;
  phone-connector)
    export CHOG_BROWSER_URL="$BASE/?perf=1" CHOG_BROWSER_SETTLE_MS=8000
    tap 1 $OTHER_WALLET 15000
    tap 2 "${4:-30}" "${5:-30}" 5000
    ;;
  signed)
    export CHOG_BROWSER_WALLET_KEY=$TEST_KEY
    export CHOG_BROWSER_URL="$BASE/?perf=1&charged=1" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 5000
    tap 2 $SIGN_IN 5000
    tap 3 $QUICK_PLAY 16000
    tap 4 $START 9000
    export CHOG_BROWSER_LINGER_MS=$(( ${4:-420} * 1000 )) CHOG_BROWSER_WAIT_FOR="CHOG_CAREER"
    ;;
  match)
    export CHOG_BROWSER_URL="$BASE/?token=$4&charged=1&perf=1${6:+&bots=$6}"
    tap 1 $QUICK_PLAY 16000
    tap 2 $START 7000
    export CHOG_BROWSER_SPECIAL="692,428,692,370" CHOG_BROWSER_LINGER_MS=$(( ${5:-20} * 1000 ))
    ;;
  film)
    export CHOG_BROWSER_URL="$BASE/?token=$4&charged=1"
    tap 1 $QUICK_PLAY 16000
    tap 2 $START 7000
    export CHOG_BROWSER_SPECIAL="${6:-692,428,750,428}" CHOG_BROWSER_SPECIAL_FILM="${5:-1800}"
    if [ -n "$7" ]; then
      tap 2 $START 5000
      export CHOG_BROWSER_DRAG="120,433,120,375" CHOG_BROWSER_DRAG_MS="$7"
    fi
    ;;
  lend)
    export CHOG_BROWSER_WALLET_KEY=$TEST_KEY
    export CHOG_BROWSER_URL="$BASE/?perf=1" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 5000
    tap 2 $SIGN_IN 5000
    tap 3 $LEND 4000
    case "${4:-share}" in
      share) tap 4 471 441 3000 ;;
      stop) tap 4 701 441 4000 ;;
      back) tap 4 858 441 3000 ;;
      *) echo "unknown button: $4" >&2; exit 2 ;;
    esac
    export CHOG_BROWSER_EVALUATE="window.chogLastLink"
    ;;
  guest)
    # Here the second argument is the whole link, not a base address.
    export CHOG_BROWSER_URL="$BASE&perf=1&charged=1" CHOG_BROWSER_SETTLE_MS=7000
    tap 1 $QUICK_PLAY 16000
    tap 2 $START 9000
    export CHOG_BROWSER_LINGER_MS=$(( ${4:-420} * 1000 )) CHOG_BROWSER_WAIT_FOR="CHOG_CAREER"
    ;;
  respawn)
    export CHOG_BROWSER_URL="$BASE/?token=$4"
    tap 1 $QUICK_PLAY 16000
    tap 2 $START 5000
    export CHOG_BROWSER_DRAG="120,433,120,375" CHOG_BROWSER_DRAG_MS=6000
    export CHOG_BROWSER_LINGER_MS=$(( ${5:-90} * 1000 )) CHOG_BROWSER_LINGER_SHOTS=${6:-30}
    ;;
  rentout)
    export CHOG_BROWSER_WALLET_KEY=$TEST_KEY CHOG_BROWSER_WALLET_RPC=${CHOG_BROWSER_WALLET_RPC:-http://127.0.0.1:8545}
    export CHOG_BROWSER_URL="$BASE/?perf=1" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 6000
    tap 2 $RENT_OUT 4000
    case "${4:-offer}" in
      offer) tap 3 472 441 14000 ;;
      dearer) tap 3 668 357 1500; tap 4 472 441 14000 ;;
      withdraw) tap 3 701 441 14000 ;;
      *) echo "unknown button: $4" >&2; exit 2 ;;
    esac
    ;;
  buy)
    # The second form bought from the shop for the first Genesis of the wallet (a local copy with services
    # started with --shop, the wallet holding that Genesis and $CHOG): the form button, then the wallet.
    export CHOG_BROWSER_WALLET_KEY=$TEST_KEY CHOG_BROWSER_WALLET_RPC=${CHOG_BROWSER_WALLET_RPC:-http://127.0.0.1:8545}
    export CHOG_BROWSER_URL="$BASE/?perf=1" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 6000
    tap 2 417 263 2500
    tap 3 828 385 26000
    ;;
  rent)
    export CHOG_BROWSER_WALLET_KEY=$TEST_KEY CHOG_BROWSER_WALLET_RPC=${CHOG_BROWSER_WALLET_RPC:-http://127.0.0.1:8545}
    export CHOG_BROWSER_URL="$BASE/?perf=1&charged=1" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 6000
    if [ "${7:-title}" = large ]; then tap 2 $CONNECT 4000; else tap 2 $MARKET_BY_TITLE 4000; fi
    # Offer slots are 85 wide from x 417; day buttons are 70 apart from x 733.
    tap 3 $(( 417 + (${4:-1} - 1) * 85 )) 263 1500
    tap 4 $(( 733 + (${5:-1} - 1) * 70 )) 376 1500
    tap 5 520 441 22000
    if [ "${6:-0}" -gt 0 ]; then
      tap 6 490 451 5000
      tap 7 $QUICK_PLAY 16000
      tap 8 $START 9000
      export CHOG_BROWSER_LINGER_MS=$(( $6 * 1000 )) CHOG_BROWSER_WAIT_FOR="CHOG_CAREER"
    fi
    ;;
  stake)
    # Photon's part must have been played for this wallet before the page reaches the
    # preparation screen: node server/rehearsal/photon_standin.mjs enter --address <wallet> ...
    export CHOG_BROWSER_WALLET_KEY=$TEST_KEY CHOG_BROWSER_WALLET_RPC=${CHOG_BROWSER_WALLET_RPC:-http://127.0.0.1:8545}
    export CHOG_BROWSER_URL="$BASE/?record=1" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 6000
    tap 2 $SIGN_IN 6000
    tap 3 $QUICK_PLAY 18000
    tap 4 $STAKE 30000
    tap 5 $START 12000
    export CHOG_BROWSER_LINGER_MS=$(( ${4:-420} * 1000 )) CHOG_BROWSER_WAIT_FOR="${CHOG_BROWSER_WAIT_FOR:-CHOG_REPLAY}"
    ;;
  stake-host)
    # As "stake", but waits for the guest before staking and starting.
    export CHOG_BROWSER_WALLET_KEY=${CHOG_STAKER_KEY:-$TEST_KEY} CHOG_BROWSER_WALLET_RPC=${CHOG_BROWSER_WALLET_RPC:-http://127.0.0.1:8545}
    export CHOG_BROWSER_URL="$BASE/?record=1" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 6000
    tap 2 $SIGN_IN 6000
    tap 3 $QUICK_PLAY 55000
    if [ "${CHOG_TAKE_TEST_TOKENS:-0}" = 1 ]; then
      tap 4 $TEST_TOKENS 30000
      tap 5 $STAKE 45000
      tap 6 $START 12000
    else
      tap 4 $STAKE 35000
      tap 5 $START 12000
    fi
    export CHOG_BROWSER_LINGER_MS=$(( ${4:-420} * 1000 )) CHOG_BROWSER_WAIT_FOR="${CHOG_BROWSER_WAIT_FOR:-CHOG_REPLAY}"
    ;;
  stake-guest)
    # Joins the host's quick match, goes to the other team, stakes, and stays to the end.
    export CHOG_BROWSER_WALLET_KEY=${CHOG_STAKER_KEY:-$TEST_KEY} CHOG_BROWSER_WALLET_RPC=${CHOG_BROWSER_WALLET_RPC:-http://127.0.0.1:8545}
    export CHOG_BROWSER_URL="$BASE/" CHOG_BROWSER_SETTLE_MS=${CHOG_BROWSER_SETTLE_MS:-16000}
    tap 1 $CONNECT 6000
    tap 2 $SIGN_IN 6000
    tap 3 $QUICK_PLAY 14000
    tap 4 $CHANGE_TEAM 4000
    if [ "${CHOG_TAKE_TEST_TOKENS:-0}" = 1 ]; then
      tap 5 $TEST_TOKENS 30000
      tap 6 $STAKE 45000
    else
      tap 5 $STAKE 35000
    fi
    export CHOG_BROWSER_LINGER_MS=$(( ${4:-420} * 1000 ))
    ;;
  staked)
    # The menu again, with the profile of the run that staked: what became of the stake.
    export CHOG_BROWSER_WALLET_KEY=${CHOG_STAKER_KEY:-$TEST_KEY} CHOG_BROWSER_WALLET_RPC=${CHOG_BROWSER_WALLET_RPC:-http://127.0.0.1:8545}
    export CHOG_BROWSER_URL="$BASE/" CHOG_BROWSER_SETTLE_MS=6000
    tap 1 $CONNECT 9000
    if [ "${4:-look}" = claim ]; then tap 2 $CLAIM 25000; fi
    ;;
  duel-host)
    # Enters a quick match, waits for the other page to join and change team, then starts.
    export CHOG_BROWSER_URL="$BASE/?token=$4"
    tap 1 $QUICK_PLAY $(( ${5:-40} * 1000 ))
    tap 2 $START 7000
    export CHOG_BROWSER_LINGER_MS=$(( ${6:-420} * 1000 )) CHOG_BROWSER_WAIT_FOR="${CHOG_BROWSER_WAIT_FOR:-CHOG_REPLAY}"
    ;;
  duel-guest)
    # Joins the quick match the host opened, goes to the other team, and stays to its end.
    export CHOG_BROWSER_URL="$BASE/?token=$4"
    tap 1 $QUICK_PLAY 14000
    tap 2 $CHANGE_TEAM 4000
    export CHOG_BROWSER_LINGER_MS=$(( ${5:-420} * 1000 ))
    ;;
  *)
    echo "unknown check: $MODE" >&2
    exit 2
    ;;
esac

# The guide of the first visit (ChogGuide, 16 October) would cover the menu the taps aim at.
case "$CHOG_BROWSER_URL" in
  *\?*) export CHOG_BROWSER_URL="$CHOG_BROWSER_URL&noguide=1" ;;
  *) export CHOG_BROWSER_URL="$CHOG_BROWSER_URL?noguide=1" ;;
esac

node "$ROOT/tools/unity/qualify_webgl.mjs" 2>&1 | tail -2
python3 "$HERE/summarise_log.py" "$CHOG_BROWSER_OUTPUT_DIR/touch-browser-smoke-events.json"
