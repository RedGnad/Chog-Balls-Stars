#!/bin/sh
# Re-reads, on their primary pages, the facts the mobile presentation report rests on
# (research/reports/mobile-presentation-2026-10-10.md), and prints the sentence found for each.
# A fact whose line prints nothing after its label has moved or changed: read the page again.
#
#   sh research/scripts/check_mobile_facts.sh
AGENT="Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36"
get() { curl -sS -L -m 30 -A "$AGENT" -H "Accept: text/html,application/json,*/*" "$1"; }

echo "1. Fullscreen API on iPhone (caniuse data):"
get https://raw.githubusercontent.com/Fyrd/caniuse/main/features-json/fullscreen.json \
  | python3 -I -c 'import json,sys; d=json.load(sys.stdin); s=d["stats"]["ios_saf"]; last=sorted(s, key=lambda v:[float(p) for p in v.split("-")[0].split(".")])[-1]; print("   ios_saf", last, "=", s[last]); [print("   note", k, ":", v) for k, v in d["notes_by_num"].items() if "iPhone" in v]'

echo "2. MetaMask Mobile, Android manifest, orientation of its main activity:"
get https://raw.githubusercontent.com/MetaMask/metamask-mobile/main/android/app/src/main/AndroidManifest.xml \
  | grep -n "screenOrientation" | head -3

echo "3. Rabby Mobile, Android manifest:"
get https://raw.githubusercontent.com/RabbyHub/rabby-mobile/develop/apps/mobile/android/app/src/main/AndroidManifest.xml \
  | grep -n "screenOrientation" | head -3

echo "4. App Store Review Guidelines, 3.1.1, NFTs:"
get https://developer.apple.com/app-store/review/guidelines/ \
  | grep -o "Apps may allow users to view their own NFTs[^<]*" | head -1

echo "5. Telegram, terms for bot developers, wallets:"
get https://telegram.org/tos/bot-developers | grep -o "must only interface with wallets[^<]*" | head -1

echo "6. Farcaster Mini Apps, how a mini app is shown:"
get https://miniapps.farcaster.xyz/llms-full.txt | grep -o "A Mini App should be rendered in a vertical modal[^\n]\{0,80\}" | head -1

echo "7. Reown AppKit, what counts towards its free limit:"
get https://docs.reown.com/appkit/faq.md | grep -i -o "only authenticated users count[^.]*" | head -1

echo "8. MetaMask, link that opens a page in its in-app browser:"
get https://docs.metamask.io/metamask-connect/evm/guides/metamask-exclusive/use-deeplinks.md | grep -o "https://link.metamask.io/dapp/[^ \`\"<]*" | head -1

echo "9. Wallets listed by Monad for mainnet:"
get https://docs.monad.xyz/tooling-and-infra/wallets/software-wallets.md | grep -i -o "MetaMask\|Rabby\|Backpack\|OKX Wallet\|Trust Wallet\|Phantom\|Coinbase Wallet" | sort | uniq -c | tr '\n' ' '
echo

echo "10. Unity 6.6 manual, mobile browsers:"
get https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-browsercompatibility.html \
  | grep -o "The Unity Web platform supports some mobile browsers[^<]*\|iOS Safari [0-9]* and newer\|Progressive Web App (PWA) template" | sort -u
