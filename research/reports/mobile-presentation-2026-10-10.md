# How the game should be opened on a phone (10 October 2026)

Question from the user, 10 October: which presentation suits a phone best (accessibility, simple
handling, wallet redirections, use of the screen without browser bars): PWA, native, or
something else. Asked as research into current practice, not as a list to follow.

Three research passes ran in parallel (the shell, the wallet connection, the distribution
surfaces and what shipped games do). Their deciding facts were then read again on the primary
pages by `research/scripts/check_mobile_facts.sh`: 10 of 10 came back as reported. Tags:
**[V]** read on the cited page on 10 October 2026, **[I]** inference, **[U]** looked for and not
found. Nothing here was tried on a phone except what the user's own screenshot shows.

## Verdict

1. **Keep the web page opened by a link. Do not build a native app or a store wrapper now.**
   Confidence: high. The App Store forbids exactly what this game is (NFT ownership that
   unlocks play) [V], and every container that brings its own wallet fails a hard constraint
   (Telegram by its terms, World by its chain, Farcaster by its portrait window) [V].
2. **"PWA mobile-first" was half right.** The web part is right. The "install it" part is not
   the lever: on Android a plain tab reaches full screen and landscape lock with one tap and no
   install [V]; on iPhone the installed page is the only web route that removes Safari's bars,
   it costs about six manual steps, and it still cannot lock landscape [V]. So: **tab first,
   install offered, never required.** Confidence: medium, because how much of the screen
   Safari keeps in landscape on an iPhone is [U].
3. **The wallet is the hard part, and the present choice is the weak spot.** The game only
   talks to a wallet injected in the page. A phone's normal browser has none (the user's phone
   reports `wallet NoProvider`), so today a holder on a phone must open the game inside the
   wallet app's browser, and MetaMask and Rabby lock that browser to portrait [V]: a landscape
   game there is the wrong way round. Confidence that this needs a second path: medium-high;
   it is one 5-minute test away from certain (see "Tests").

## What the user's phone showed

Screenshot of 10 October, 02:46 (Redmi, Android 10, Chrome 126, Mali-G52, game canvas
1508 x 560, 30 images a second at the median): the page was opened in a browser window with a
title bar (close, title "Unity Web Player | Chog Genesis", address, share, menu). Measured on
the screenshot, the status bar and that title bar take 123 of 559 lines and the system
navigation takes about 75 of 1280 columns: **the game gets about 71% of the screen.** The page
is Unity's default template: it fills the browser window on a phone, asks for nothing else, has
no install manifest, and its full-screen button is hidden on phones.

## The shell: option by criterion

| Option | Steps to first play | Browser bars | Landscape lock | Update | Store risk |
| --- | --- | --- | --- | --- | --- |
| Tab, Android Chrome | open the link, one tap | gone after one tap: `requestFullscreen` works after a user gesture [V] | `screen.orientation.lock` works in full screen [V] | at once | none |
| Tab, iPhone Safari | open the link, one tap | **no page-driven full screen on iPhone**, only iPad, through iOS 27.2 [V]; what the bars take in landscape [U] | not supported [V]; needs a "turn your phone" notice [I] | at once | none |
| Installed web app, Android | play in the tab first, install optional | manifest `display: fullscreen` hides status and navigation bars [V] | manifest `orientation` [V] | at once | none |
| Home Screen web app, iPhone | about six manual actions, no install prompt exists [V] | `fullscreen` falls back to `standalone`: Safari's bars go, status bar and home indicator in landscape [U] | manifest `orientation` unsupported [V] | at once | none |
| Play Store wrapper (Trusted Web Activity) | store page, install, open | full screen [V] | yes [V] | web content at once; the shell goes through review | NFT use allowed with a declaration [V]; a new personal account needs 12 testers for 14 days [V], longer than the contest |
| App Store wrapper or native iOS | store page, install, open | full screen | yes | every change through review | **3.1.1: "NFT ownership does not unlock features or functionality within the app"** [V]; 4.2 against repackaged websites [V] |
| Native Unity build | store page, install, open | full screen | yes | every build through review | as above; and the wallet and network code would be rewritten |

Unity 6.6 supports "iOS Safari 15 and newer" and "Chrome 58 and newer" for WebGL, and names two
phone routes itself: a WebView inside a native app, or its PWA template [V]. iPhone memory is
tighter than iPad for a web page [V, Apple forum], and how our build behaves on an iPhone is
[U]: no iPhone has opened it.

## The wallet on a phone: option by criterion

| Option | Holder with MetaMask | Other wallets | Landscape game | Monad 143 | Cost and weight |
| --- | --- | --- | --- | --- | --- |
| A. Injected wallet only, plus "open in wallet" link (**today**) | tap the link, the page opens in MetaMask's browser, connect, sign | one link per wallet: Backpack, Trust, OKX documented or quoted; Rabby only found in its source code | **poor: MetaMask and Rabby lock portrait** [V manifests]; other wallets [U] | MetaMask, Rabby, Backpack, OKX, Trust listed by Monad; Phantom dropped it [V] | nothing |
| B. MetaMask Connect (`@metamask/connect-evm`) | stays in Safari or Chrome, a link opens the app to approve [V]; the way back [U] | not served [V] | good: a real browser turns | any chain through a list of endpoints [V] | free; entry file 58 KB [V] |
| C. Reown AppKit (WalletConnect) | stays in the browser, pick the wallet, approve in the app [V]; the way back [U] | "500+ wallets" [V] | good | custom chains supported [V]; the user's earlier game ran Monad testnet this way [V, local code] | free while nobody signs in through Reown: "only authenticated users count toward this, not self-custodial connections" [V]; 4.2 MB of script, 731 KB compressed, loaded when asked [V measured]; phone bugs reported and closed in 2025 [V issue 178] |
| D. Embedded wallet (Privy, Dynamic, thirdweb) plus the linked NFT wallet | logging in is easy, but the wallet that holds the Genesis must still connect and sign once, through A, B or C [I] | same | good | several providers list Monad mainnet [V] | free tiers of 500 to 1,200 monthly users [V]; their Unity kits do not reach external wallets on WebGL [V] |
| E. Delegation (delegate.xyz) | the holder first acts from the NFT wallet anyway [I] | same | not relevant | registry v2 is not deployed on Monad mainnet [V, `cast code`] | a transaction |
| F. Our own pairing: a light page signs in the wallet's browser, the game stays in the normal browser | tap, sign on a small upright page in the wallet, come back to the game [I] | every wallet with a browser [I] | good | as A | nothing to depend on; about two days of our work; the return to the game is by hand [I] |

Links that open a page in a wallet's browser: MetaMask `https://link.metamask.io/dapp/{url}`
[V], already used by the game; Backpack `https://backpack.app/ul/v1/browse/<url>?ref=<ref>`
[V]; Trust `https://link.trustwallet.com/open_url?coin_id=60&url=<url>` [V]; OKX and Rabby only
from secondary sources, untested.

## Containers that bring their own wallet

| Container | Why it does not fit |
| --- | --- |
| Telegram Mini App | the screen would suit (full screen and landscape since Bot API 8.0 [V]), but the terms allow only wallets "connected via the TON Connect SDK" and forbid NFTs of other chains [V] |
| Farcaster Mini App | "should be rendered in a vertical modal" [V]; no full-screen or orientation call; its chain list knows Monad testnet only [V]; the wallet is the host's, not the holder's MetaMask [V] |
| World App | "World Chain only (chainId 480)" [V] |
| Base app (the renamed Coinbase Wallet) | since April 2026 it treats every app as an ordinary web page in its browser [V]: nothing to build, nothing gained |
| Monad App Hub | a directory that links out, 143 apps [V]; worth a listing, not a container |

## What shipped games do

Nine entertainment sites of the Monad hub were opened [V]: five are web pages (three with an
install manifest, two of those locked to portrait), two are Telegram mini apps, one is a store
app, and the only Unity WebGL game (Lumiterra) refuses phones outright. Their wallet scripts:
Privy on five, WalletConnect or Reown on three. The four Chogathon rivals with a live page are
plain web pages with Reown, WalletConnect or RainbowKit and no manifest. **None is a real-time
landscape game playable in a phone browser**: a phone build that plays well is rare in this
field, which is an opening and a warning at once. No trustworthy number on install rates or
wallet drop-off was found [U].

## The present decision, contested

| Earlier position | Still holds? | Why |
| --- | --- | --- |
| "PWA, mobile first" (project brief) | the web page: yes. Installation as the way in: no | install is optional on Android and laborious on iPhone; nobody in the field requires it |
| Injected wallet only, with a MetaMask link (built on 9 October) | on a computer: yes. On a phone: no, as the only path | the wallet browsers lock portrait; found today, not known when the choice was made |
| Unity's default page | no | it shows browser bars, a foreign title and no way to full screen on a phone |
| No native app | yes | store rules and the two-week window |

## What was done with this report

10 October, 04:00: the user accepted the recommendation ("je valide", "faisons au mieux je suis
ta reco"). Blocks 1 and 2 were built the same night (`game/Assets/WebGLTemplates/Chog/`, see
"The page the game is served in" in `game/README.md`) and checked in a headless browser only.
Block 3 was not run: the user chose to follow the recommendation without it. Block 4 waits for
a Reown project id and a public https address.

12 October: block 4 was written (Reown AppKit in the page, see `game/README.md`), not yet
run with a real project id or a phone. Facts read that day, to save the search next time:

- The current packages are `@reown/appkit` and `@reown/appkit-cdn` 1.8.24, both published
  that week (`npm view`). The CDN build is 214 files, 11 MB unpacked; its entry
  (`dist/appkit.js`) exports `createAppKit`, `WagmiAdapter`, `networks`, `Connectors`,
  `WagmiCore`, `Viem`, `SolanaAdapter`; its main part is 4.4 MB, 878 KB over the wire.
  `networks.monad` is chain 143 with `https://rpc.monad.xyz`.
- The object `createAppKit` returns, read in that build and not in the documentation, which
  names methods that are not there (`getAddress`, `getIsConnected`, `subscribeProvider`):
  `open({view, namespace})`, `close()`, `ready()`, `getAccount(namespace)` (address,
  isConnected, status), `subscribeAccount(callback, namespace)`, `getWalletProvider()`,
  `getState()` and `subscribeState(callback)` (open), `disconnect(namespace)`.
- Reown's Unity package runs wagmi on WebGL (`Runtime/WebGL/Wagmi/` of
  `com.reown.appkit.unity` 1.4.1, in the user's earlier game). It is not used here: the game
  is a web page and speaks to any wallet object, so the web script is enough.
- No page of Reown read that day says its window knows which wallet apps are installed when
  it runs in a phone browser; it shows a chosen list (`featuredWalletIds`, by default MetaMask
  and Trust) and a search of all wallets (`allWallets`). On the way back, Reown's wallet
  documents say a wallet either asks the user to return or returns by itself, and one of its
  test cases expects the return on Android.
- Monad's documentation still carries a Reown AppKit guide, with wagmi
  (`https://docs.monad.xyz/guides/reown`).

One more observation from that night, which belongs to "where the link will really open": the
user's phone screenshots show a browser window with a close cross and a minimise arrow, as
when a link is opened inside another app, and one reload there failed with "unable to load
file build/webgl.framework.js.gz" although the test server never received the request. Not
explained. The page now starts again by itself in that case.

## Recommendation, in blocks that can each be tested

| Block | What | Work | Test that says it worked |
| --- | --- | --- | --- |
| 1. Our own game page | first tap asks for full screen and locks landscape where the browser allows; a "turn your phone" notice in portrait; our title and icon; an install manifest (`display: fullscreen`, `orientation: landscape`) without any offline cache, so a new build is never hidden by an old copy | half a day | on the Redmi: one tap, the bars are gone and the picture fills the screen |
| 2. iPhone | the same page plus the Home Screen tags; a short "Add to Home Screen" hint offered once, never required | included in 1 | an iPhone opens the link in Safari, then from the Home Screen: what is left of the bars in each case |
| 3. See the wallet problem for real | no code: open the present build in the MetaMask app's browser | 5 minutes of the user's time | does the game stand upright in a locked portrait window, can it be played |
| 4. One wallet path that stays in the normal browser | a connector loaded only when the page has no injected wallet, handed to the code we already have (it speaks to any standard wallet object). First choice Reown AppKit, because one integration covers MetaMask, Rabby, Backpack, OKX and Trust; MetaMask Connect is the smaller fallback for MetaMask alone | about a day, needs a public https address and a Reown project id | on the phone: connect, sign in, come back to the game without the page reloading |
| 5. Fallback if 4 fails on the way back | block F: the light pairing page inside the wallet's browser | about two days | the same test |

Embedded wallets and delegation are not recommended: both add a step without removing the one
hard step, which is a signature from the wallet that holds the Genesis.

## What would change this, and the fastest test

| If this turns out true | Then | Fastest test |
| --- | --- | --- |
| The game is playable inside MetaMask's browser (it turns, or it is usable upright) | block 4 drops in priority; links to more wallets are enough | block 3, 5 minutes |
| Coming back from the wallet app reloads the game page on a mid-range phone | prefer block F, and keep the session across a reload (already the case for the sign-in) | a test page that connects then signs, on the Redmi |
| Safari keeps large bars in landscape on an iPhone | push the Home Screen install harder on iPhone, with a picture of the steps | block 2 |
| The build does not hold on an iPhone (memory, reload) | lower the texture and memory settings before anything else | one full match on an iPhone |
| Judges test on a computer only | phone work ranks below gameplay; keep block 1, defer 4 | ask the organisers on Discord |

## Unknown

- What an iPhone shows, in Safari and from the Home Screen; no iPhone has opened the build.
- The way back from a wallet app to the browser tab, on both systems, and whether the game
  page survives it.
- Whether Backpack, OKX, Trust and Coinbase browsers turn to landscape.
- Which wallets Chog holders use, and how many play on a phone. A poll in the Chog Discord
  would answer both.
- How Chogathon judges test: the page asks for "a working demo, a demo video of up to 3 minutes,
  and simple testing instructions" [V] and says nothing about devices.

## Sources

Read on 10 October 2026.

- Full screen on iPhone: https://raw.githubusercontent.com/Fyrd/caniuse/main/features-json/fullscreen.json
  ("Partial support refers to supporting only iPad, not iPhone"); WebKit bug 206854 still open.
- Installed web apps: https://web.dev/learn/pwa/installation , https://web.dev/articles/install-criteria ,
  https://support.apple.com/guide/iphone/open-as-web-app-iphea86e5236/ios
- Full screen and orientation on Android: https://developer.mozilla.org/en-US/docs/Web/API/Element/requestFullscreen ,
  https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/lock
- Store rules: https://developer.apple.com/app-store/review/guidelines/ (3.1.1, 4.2);
  https://support.google.com/googleplay/android-developer/answer/13607354 ;
  https://support.google.com/googleplay/android-developer/answer/14151465 ;
  https://developer.android.com/develop/ui/views/layout/webapps/trusted-web-activities
- Unity: https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-browsercompatibility.html ,
  https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-memory.html
- Wallet browsers locked to portrait: `android/app/src/main/AndroidManifest.xml` of
  https://github.com/MetaMask/metamask-mobile and of https://github.com/RabbyHub/rabby-mobile
  (`apps/mobile/android/...`), both `android:screenOrientation="portrait"`.
- Wallet links and connectors: https://docs.metamask.io/metamask-connect/evm/guides/metamask-exclusive/use-deeplinks ,
  https://docs.metamask.io/metamask-connect/supported-platforms , https://docs.backpack.app/deeplinks/other-methods/browse ,
  https://developer.trustwallet.com/developer/develop-for-trust/deeplinking , https://docs.reown.com/appkit/faq ,
  https://github.com/reown-com/reown-dotnet/issues/178
- Monad: https://docs.monad.xyz/tooling-and-infra/wallets/software-wallets , https://app.monad.xyz/app-hub
- Containers: https://telegram.org/tos/bot-developers (7.2), https://core.telegram.org/bots/blockchain-guidelines ,
  https://miniapps.farcaster.xyz/llms-full.txt , https://docs.world.org/mini-apps/migration/standalone-dapp ,
  https://github.com/base/docs (commit 6c84157)
- Delegation: https://docs.delegate.xyz/technical-documentation/delegate-registry/contract-addresses ;
  `cast code 0x00000000000000447e69651d841bD8D104Bed493` on rpc.monad.xyz answers `0x`.

Routes that failed: caniuse.com and MDN compatibility tables as pages (their raw data files
were read instead); the Coinbase Wallet deep-link page (redirects; only an archive copy of 2024
has the format); the OKX deep-link page (404 on three addresses and in the archive); the
Wayback availability API (429); playkami.io and lumiterra.itch.io (403).

Side finding: the Chogathon page now says registrations stay open until 10 October 19:00 UTC;
its end date is unchanged (23 October 15:00 UTC).
