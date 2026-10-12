#!/bin/sh
# Every automatic check of the repository that runs without the Unity Editor, one after the
# other, with one line of result each. A release starts from a run where all of them pass.
#
#   sh tools/release/check_all.sh [--fork]
#
# --fork also runs the shop contract against a local copy of Monad mainnet (needs the network).
# The view tests need the Editor to have compiled the project once (they reuse its compiler
# arguments, see tools/dev/README.md); the other Chog.*.Tests assemblies only run in the Unity
# Test Runner. Not covered here: the scripted browser runs
# (tools/unity/checks/browser_check.sh), which need a build and a running test server.
ROOT=$(cd "$(dirname "$0")/../.." && pwd)
DOTNET=${DOTNET:-/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet}
failed=0

run() { # run <label> <command...>
  label=$1
  shift
  if output=$("$@" 2>&1); then
    echo "pass  $label: $(echo "$output" | grep -v '^$' | tail -1 | cut -c1-110)"
  else
    echo "FAIL  $label"
    echo "$output" | tail -15
    failed=1
  fi
}

run "roster is up to date with the catalog" python3 "$ROOT/tools/hero-catalog/run.py" roster --check
run "hero page data is up to date" python3 "$ROOT/tools/web/build_hero_data.py" --check
run "catalog and simulation tests" python3 "$ROOT/tools/quantum/headless.py" test --dotnet "$DOTNET"
run "view tests" sh "$ROOT/tools/dev/view_tests.sh"
run "game services tests" node --test "$ROOT/server/test/chain.test.js" "$ROOT/server/test/session.test.js" \
  "$ROOT/server/test/career.test.js" "$ROOT/server/test/loan.test.js" "$ROOT/server/test/shop.test.js" \
  "$ROOT/server/test/rental.test.js" "$ROOT/server/test/host.test.js"
run "shop, rental and wager contract tests" forge test --root "$ROOT/contracts"
if [ "$1" = "--fork" ]; then
  MONAD_FORK=1 run "shop contract on a copy of Monad mainnet" forge test --root "$ROOT/contracts" --network monad \
    --match-contract ChogShopForkTest
fi

[ "$failed" = 0 ] && echo "all checks passed" || echo "some checks FAILED"
exit $failed
