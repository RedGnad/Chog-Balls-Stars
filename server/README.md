# Game services

The part of the Chog Genesis game that is not the match: who holds which Genesis, who is
signed in, and what each Genesis has done. The game page calls it on its own origin, under
`/api/`. **What runs today is a local stand-in:** the same handler that a hosted function would
run, behind Node's HTTP server, with careers in a JSON file. Nothing is deployed.

```sh
npm install                      # two dependencies, for signature checks
npm test                         # 48 tests, no network
node src/local.js                # the services alone: http://127.0.0.1:8090/api/health
node src/host.js                 # the whole game behind one address, see docs/DEPLOY.md
```

Two ways to run it. For development, `local.js` is the services alone on the loopback address;
the WebGL test server (`tools/unity/serve_webgl.py`) forwards `/api/` to port 8090, so the
phone reaches the services through the same address as the game. For a release, `host.js`
serves the game, the hero page, the portraits and the services from one process
(`src/static.js`, `src/node.js`).

## Endpoints

| Call | What it does | Who may call |
| --- | --- | --- |
| `GET /api/health` | chain id, Genesis contract, block of the last owner read; reads nothing | anyone |
| `GET /api/genesis/owned?address=0x…` | token ids that address holds | anyone |
| `POST /api/session/challenge` `{address}` | the message that wallet must sign, and a proof that we wrote it | anyone |
| `POST /api/session` `{message, signature, proof}` | a session token for the address that signed | the wallet that signs |
| `GET /api/career?tokens=3,8` | matches, wins, best streak, confirmed results and badges of those Genesis | anyone |
| `GET /api/career/top?limit=10` | the Genesis with the most wins | anyone |
| `POST /api/career/result` `{tokenId, matchId, team, winnerTeam}` with `Authorization: Bearer <session>` | adds one finished match to the career of that Genesis | the signed-in holder of that token |
| the same with `Authorization: Pass <pass>` | the same, counted as a guest's match | whoever has a pass that still counts for that token |
| `POST /api/loan` `{tokenId, days}` with `Authorization: Bearer <session>` | a pass that lends that Genesis, for 1 to 30 days (30 when `days` is left out) | the signed-in holder of that token |
| `GET /api/loan` with `Authorization: Pass <pass>` | which Genesis the pass lends, from whom, until when; 403 and a reason when it no longer counts | whoever has the pass |
| `POST /api/loan/stop` `{tokenId}` with `Authorization: Bearer <session>` | every pass handed out for that Genesis so far stops counting | the signed-in holder of that token |
| `GET /api/shop` | what is sold for $CHOG: the shop contract, the token, the item and its price, read on the chain; `{"enabled": false}` when no shop is set up or the item is off sale | anyone |
| `GET /api/shop/owned?tokens=3,8` | those of the given Genesis that have the item, read on the chain | anyone |
| `GET /api/rental` | the rental contract, the token, the longest rental in days and the share burned, in hundredths of a percent; `{"enabled": false}` when no rental is set up | anyone |
| `GET /api/rental/offers` | every Genesis its holder offers for rent now: token id, price a day, holder | anyone |
| `GET /api/rental/rented?address=0x…` | the Genesis that address rents now, each with the second its rental ends | anyone |
| `GET /api/wallet` | `{"reownProjectId": "…"}`: what the game page needs to connect a wallet app when the browser has no wallet; an empty text when none is set, and the page then offers no such connection | anyone |

A result sent with a session by an address that does not hold the Genesis is accepted when that
address rents it at that moment (`rentedMatches` counts those matches).

## What is checked, and what is not

- **Ownership is read on Monad mainnet** (chain 143, contract
  `0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763`): one Multicall3 call asks `ownerOf` for the
  1,969 tokens (about a second on `rpc.monad.xyz`, two fallback endpoints). The answer is reused
  for 20 seconds for lists. Before every credit, and at every use of a lent Genesis, the owner
  of that one token is read by itself (`ownerOf`, about 150 ms measured on 9 October, shared by
  the checks made within two seconds), so a Genesis that was sold stops crediting through the
  old wallet within two seconds (`src/chain.js`).
- **Signing in proves control of the address.** The message follows ERC-4361 and names the
  game page; the wallet signs it with `personal_sign`, which costs nothing and sends no
  transaction. The signer is recovered from the signature and must be the address named in a
  message we issued less than ten minutes before. A session lasts seven days. Nothing is stored:
  challenge and session carry an HMAC of the server secret (`src/session.js`).
  **Limit:** a smart-contract wallet (ERC-1271) cannot sign in, only a key-pair account.
- **A career belongs to the Genesis, not to the wallet.** It is keyed by token id, so it
  follows the token when it changes hands, and anyone can read it (`src/career.js`).
- **Match results are reported by the player's own game page. Nothing replays a match.** A
  report is accepted once per match and per Genesis, at most one every 60 seconds per Genesis,
  from the signed-in address that holds the token at that moment. A holder who writes their own
  requests can therefore still invent results for their own Genesis. `confirmed` counts the
  results that another signed-in address of the same match reported with the same winner; a
  match against bots only is never confirmed. The target, a result verified from the match
  itself, is described in `docs/ARCHITECTURE-QUANTUM.md` and is not built.

- **A holder can lend a Genesis without moving it.** A signed-in holder asks for a pass and
  hands out the game's address with `?pass=` and that text. Whoever opens it plays the Genesis
  without a wallet, and the matches go to the career of the Genesis (`guestMatches` counts
  them). A pass is a bearer link: several guests can use it at the same time, and anyone who
  gets hold of it can. It counts while four things hold, checked at every use: we issued it
  (HMAC, like a session), it has not run out, the holder has not stopped lending that Genesis
  since it was issued, and the lender still holds the Genesis on chain. A sale therefore kills
  every pass at once, and they come back if the Genesis does. Nothing is signed or sent to the
  chain for a loan: the signed sign-in is what proves the holder. The time of the last stop is
  kept per Genesis with the careers. A guest's match stands under the holder's address, so a
  holder and their own guest cannot confirm each other's results.

Badges are earned by play and cannot be bought or moved: First match, First win, Three in a
row, Veteran (ten matches). The table is `BADGES` in `src/career.js`.

**The shop** (`src/shop.js`) only reads. A purchase is two transactions the player's wallet
sends to the shop contract (`contracts/README.md`); the services read the price with
`items(1)` and what a Genesis has with `owned(tokenId, 1)`, several Genesis in one request
through Multicall3. Nothing about a purchase is stored here: the chain is the record, and a
purchase follows its Genesis. The shop exists only when its address is given at start
(`SHOP_ADDRESS` for `host.js`, `--shop` for `local.js`); `local.js --rpc` reads another
endpoint, which is how the purchase was rehearsed on a local copy of the chain.

**The rental** (`src/rental.js`) only reads too. Offering, taking an offer back and renting are
transactions the player's wallet sends to the rental contract (`contracts/README.md`). The
services read `standingOffers()` (kept ten seconds), `rentalsOf(address)` and, before a credit,
`isRenting(tokenId, address)` by itself and fresh. Nothing about a rental is stored here. A
renter's match stands under the renter's own address, so a renter and the holder can confirm
each other's result of a match they both played. The rental exists only when its address is
given at start (`RENTAL_ADDRESS` for `host.js`, `--rental` for `local.js`).

**Mastery** is part of every career answer (`mastery: {level, points, next}`): one point a
match, one more for a win; level 2 at 3 points, then 8, 15, 25 and 40 (`MASTERY_STEPS`). The
game unlocks the second form of a Genesis's attack at level 2. It is computed from the same
self-reported results as the career, with the same limit: a holder who writes their own
requests can raise the mastery of their own Genesis.

## The secured match: Photon, the judge, the stakes

Written on 11 October 2026 and run in a rehearsal where a script plays Photon's part; **Photon
itself has never called these services** (they have no public address yet, `docs/DEPLOY.md`).

| Call | What it does | Who may call |
| --- | --- | --- |
| `GET /api/photon` | `{authentication}`: whether the game must tell Photon who its player is when it connects | anyone |
| `GET /api/photon/auth?key=…&session=…` or `&pass=…` | Photon's custom authentication: names the player (a wallet by its address, a guest and a visitor by a name of their own) and sets a cookie the player never sees | Photon's cloud, with the secret as `key` |
| `POST /api/photon/hook/<name>` | one of Photon's eleven webhooks: `game/create`, `game/join`, `game/leave`, `game/close`, `game/configs`, `player/add`, `player/added`, `player/removed`, `game/result`, `replay/start`, `replay/chunk` | Photon's cloud, with the secret as the `X-SecretKey` header |
| `GET /api/wager` | the wager contract, the token, the referee, the share burned, how long a ticket lasts; `{"enabled": false}` when no wager is set up or the contract knows another referee | anyone |
| `GET /api/wager/mine` with a session | the match that wallet is in and its stakes as the chain has them | the signed-in wallet |
| `POST /api/wager/ticket` `{team, amount}` with a session | the referee's signature that lets that wallet stake that amount on that team in the match it is in | a wallet the webhooks let into a match |
| `POST /api/wager/lock` with a session | closes the stakes of that match, or says how long a ticket is still out; the game asks when its player starts the match | a wallet the webhooks let into that match |
| `GET /api/wager/result?gameId=…` | what anyone may bring to the contract once the match is judged: the winner or the calling off, signed; and where the stakes stand on the chain | anyone |

`photon.js` holds the authentication and the webhooks, `matches.js` what is kept of each match
(plain JSON files under `matches/` in the data folder), `judge.js` the verdict, `wager.js` and
`referee.js` the stakes. The rules of a staked match are decision D34 of `docs/STATUS.md`.

- **Admission** (`player/add`): the Genesis a page declares is kept only when the wallet of
  the cookie holds it or rents it, or the pass of the cookie lends it and still counts;
  the second form only when that Genesis bought it or reached mastery 2. Otherwise the player
  enters as a recruit. A loadout named directly and a character left to the machine are
  cleared. When the chain cannot be read the answer is 503, which Photon asks again.
- **The verdict** comes from playing the match again with the input Photon's server
  recorded (`tools/quantum/Verifier.cs`, run by `node.js` when `--verifier` and
  `--quantum-db` are given). What the players' pages claim (`game/result`) is kept and decides
  nothing. A match that cannot be judged is void.
- **The referee's key** signs tickets, results and the calling off of a match. It is read from
  a file (`--referee-key-file`) or from `REFEREE_KEY`, never logged, and never sends a
  transaction: players and anyone else bring its signatures to the contract.
- **Not covered:** the careers are still credited by `POST /api/career/result` from what a
  page reports; the configs of a match are those of its first page; one instance only (the
  turns of a match are taken in memory).

`sh tools/contracts/rehearse_secure_match.sh <record> <build folder> [cheat]` runs a whole
staked match on a local copy of Monad (`server/rehearsal/secure_match.mjs` says what is real
in it and what is played).

## Local data and test mode

`node src/local.js` keeps its data under `.artifacts/services/` (not under version control):
`careers.json` and `session-secret`, made on first start. Deleting the secret signs everyone
out; deleting the careers erases them.

`--pretend-holder 0xaddress=408,1795` answers that address as the holder of those tokens,
whatever the chain says. It exists so the signed path can be run end to end with a throwaway
key (`tools/unity/qualify_webgl.mjs` with `CHOG_BROWSER_WALLET_KEY`), on a separate port and
data folder. Never start a server that players reach with it.

## To deploy

Not done on a host; the prepared path is in `docs/DEPLOY.md` (`host.js`, a `Dockerfile` that
carries Node and the .NET runtime of the judge, `render.yaml`). Since 12 October 2026 the
same `host.js` runs behind a tunnel from the development Mac
(`tools/release/serve_public.sh`), which gives the game a public address while that Mac is
on. The address
written in the sign-in message is `PUBLIC_ORIGIN`, a setting, and `host.js` refuses to start
without it: a request cannot choose it. Only `local.js`, on the loopback address behind our own
test server, takes it from the forwarded headers. `createApi` in `src/api.js` takes a
web-standard `Request` and returns a `Response`, and the careers are behind a `load` and a
`save`, so another runtime or another store can replace `node.js`.
