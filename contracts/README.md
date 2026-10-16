# The three contracts paid in $CHOG

## On Monad mainnet: the deployment the owner starts (decided 16 October 2026)

The owner chose that day to build on mainnet directly (the project has no users yet, and the owner
builds on mainnet). `script/DeployMainnet.s.sol` deploys the three contracts in one run, paid in the
real $CHOG (`0x350035555E10d9AfAF1566AaebfCeD5BA6C27777`, 18 decimals, read on chain) and reading the
Genesis collection (`0xc96d31F8626c6D03Fae5dCD3d61e3FB9F4a73763`); it refuses any chain but 143. The
deploying wallet owns the wager (it can only replace the referee) and the shop (it sets the prices);
the rental has no owner. A tenth of each payment is burned unless `BURNED_SHARE` says otherwise; the
shop's other $CHOG goes to `TREASURY` (default the deploying wallet), and the second form is put on
sale at `ITEM_PRICE` whole $CHOG (default 100). The referee of the public instance on mainnet is
`0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34` (its key in `.artifacts/services-public/mainnet-referee-key`,
made by `tools/contracts/new_referee_key.sh`, never shown). In the owner's terminal, with the development
wallet imported once (`~/.foundry/bin/cast wallet import chog-dev --interactive`) and about 1 MON on it:

```sh
cd contracts
REFEREE=0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34 [ITEM_PRICE=100] [BURNED_SHARE=1000] [TREASURY=0x...] \
  ~/.foundry/bin/forge script script/DeployMainnet.s.sol --rpc-url monad --account chog-dev --broadcast
```

Rehearsed on 16 October on a local copy of mainnet (`anvil --fork-url https://rpc.monad.xyz`, deployed
with a public anvil test key): 4,259,178 gas in all (forge estimated 100 gwei of base fee that day, so
about 0.43 MON, at most 0.86 at its estimated top fee); the services read the three (shop on sale at
100 $CHOG, rental open, wager on chain 143 in $CHOG, not a test). Then, on this Mac,
`sh tools/contracts/mainnet_deployment.sh 0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34 .artifacts/services-public/mainnet-contracts`
checks the three on the chain and writes the settings that `tools/release/serve_public.sh ... --mainnet`
reads. **Not deployed yet.**

**Decided by the user on 15 October 2026: the contracts go to Monad's testnet first**, deployed
with a development wallet of theirs (its key never leaves their machine: an encrypted Foundry
keystore, `cast wallet import <name> --interactive`, and a deployment they start themselves).
Monad testnet, read on `https://docs.monad.xyz/developer-essentials/testnet.md` that day:
chain id `10143` (`0x279f`), RPC `https://testnet-rpc.monad.xyz` (also
`https://rpc-testnet.monadinfra.com`, `https://rpc.ankr.com/monad_testnet`), faucet
`https://faucet.monad.xyz`, explorers `https://testnet.monadvision.com` and
`https://testnet.monadscan.com`; the testnet was reset from genesis on 2025-12-16. Neither
$CHOG nor the Genesis collection is on the testnet: the wager goes there first, in a test token
(`src/testnet/TestChog.sol`); the rental and the shop would also need a stand-in for the
collection, not written. Multicall3 (`0xcA11bde05977b3631167028862bE2a173976CA11`), which the
game services read through, has code on the testnet (checked on 15 October).

| Contract | What it is for | State |
| --- | --- | --- |
| `src/ChogShop.sol` | an item bought for a Genesis (today: the second form of its attack) | not deployed; rehearsed on a local copy of the chain |
| `src/ChogRental.sol` | a holder offers a Genesis for rent at a price a day, a player pays to play it | not deployed; rehearsed on a local copy of the chain |
| `src/ChogWager.sol` | the players of a match stake $CHOG on their own team, the winners are paid on a result the game's referee signed | ready for Monad testnet (`script/DeployTestnet.s.sol`), not deployed yet; played from the game on a local chain |
| `src/testnet/TestChog.sol` | "Test CHOG" (tCHOG): the stand-in for $CHOG on the testnet, 18 decimals, `burn`, and a tap: `claim()` gives 1,000 to any address once a day | deployed with the wager by the same script |

`forge test` runs them: 13 tests for the shop, 18 for the rental, 23 for the wager, 4 for the
test token (one is a staked match paid in it), no network needed.

## On Monad testnet: the deployment the user starts

The game services sign with a referee key made on this Mac and never shown
(`sh tools/contracts/new_referee_key.sh <file>` prints only its address; the one of the public
instance is `.artifacts/services-public/testnet-referee-key`, address
`0x5375243CeE1b2adB0986A332dC6131AEbB097b75`). Two throwaway wallets made the same way play the
headless test matches (`.artifacts/testnet-bench/staker-a-key` `0x3dB356809Eb861225B40E3b222D7A28562177B6c`,
`staker-b-key` `0x8A7bB7CfC540993572bD520e5F7EaA51092f225d`). In the user's own terminal, with
their development wallet imported once into an encrypted keystore and about 1 MON of testnet
MON on it (the simulation of 15 October estimated 0.49 MON of gas at most, and the script sends
0.25 MON to each test wallet):

```sh
~/.foundry/bin/cast wallet import chog-dev --interactive
cd contracts
REFEREE=0x5375243CeE1b2adB0986A332dC6131AEbB097b75 \
BENCH_WALLETS=0x3dB356809Eb861225B40E3b222D7A28562177B6c,0x8A7bB7CfC540993572bD520e5F7EaA51092f225d \
~/.foundry/bin/forge script script/DeployTestnet.s.sol --rpc-url monad_testnet --account chog-dev --broadcast
```

The script refuses any chain but 10143. Then, on this Mac, `sh tools/contracts/testnet_deployment.sh
0x5375243CeE1b2adB0986A332dC6131AEbB097b75` checks on the testnet that both contracts have code
and the wager is paid in that token with that referee, and prints the settings of the services;
written one a line into `.artifacts/services-public/testnet-wager`, they are what
`tools/release/serve_public.sh ... --testnet-wager` reads. The game is told the chain by the
services (`GET /api/wager`: `chainId`, `chainName`, a public `rpcUrl`, `test`): it asks the
wallet for `0x279f`, names the stakes "TEST $CHOG", and offers GET 1000 TEST $CHOG beside the
stakes. Rehearsed on 15 October on a local chain started as the testnet
(`anvil --chain-id 10143`, Multicall3's code copied onto it): the deployment, the funding of
the test wallets, and a page that signs in without a Genesis, takes 1,000 tCHOG and stakes 10
(see `docs/STATUS.md`, 15 October).

# ChogWager: $CHOG staked on a match

**Not deployed yet; ready for Monad testnet (above).** Written on 10 October 2026 after the user
put staking on a match in scope (decision D33 in `docs/STATUS.md`). The backend that signs
tickets and results (`server/src/wager.js`, `referee.js`) and the screens that stake and collect
(`WagerPanel`, `WagerResult`) exist since 11 October. **No transaction was ever sent to Monad
mainnet.**

## What the contract does

`src/ChogWager.sol`, about 260 lines, no dependency, not upgradeable.

- A player stakes on **their own team** in a match (`stake`, after an `approve`), with a
  **ticket** the referee signed for this player, this team, this amount, this match, good until
  a deadline. Without a ticket nobody stakes: it is how the backend lets in only the players it
  admitted, on the side they really play, before the match starts.
- The first stake of a match fixes what everyone stakes. One stake per address, 16 at most.
- When the match is over the **referee signs the result** (0 or 1 for the team that won, 2 for
  a draw) and anyone brings that signature (`settle`). Each winner gets their stake back plus
  an equal share of what the losers staked, less a share that is burned; what cannot be shared
  equally is burned too. The contract keeps nothing.
- Every stake is given back when the result is a draw, when nobody staked on one of the two
  sides, when the referee calls the match off (`voidMatch`), or, **without any signature**, when
  an hour has passed since the first stake and no result came (`refundExpired`). That last one
  is what protects the players if the backend disappears.
- The owner can replace the referee (`setReferee`) and hand the contract over; nothing else.
  The token and the burned share are fixed at deployment.
- A match is known by `matchKey`, meant to be `keccak256` of the name the game server gives the
  match. A key is used once: nothing is staked on a match that was settled or refunded.

What the referee signs, always as a wallet signs a message (the Ethereum header for 32 bytes
over the digest; signature of 65 bytes r, s, v with v 27 or 28, lower half of the curve only):

| Signature | Digest |
| --- | --- |
| ticket | `keccak256(abi.encode(TICKET, chainId, wager, matchKey, player, uint8 team, uint256 amount, uint256 deadline))` |
| result | `keccak256(abi.encode(RESULT, chainId, wager, matchKey, uint8 winnerTeam))` |
| calling a match off | `keccak256(abi.encode(VOID, chainId, wager, matchKey))` |

`TICKET`, `RESULT` and `VOID` are `keccak256` of `"CHOG_WAGER_TICKET"`, `"CHOG_WAGER_RESULT"`
and `"CHOG_WAGER_VOID"`. The chain id and the contract's own address are in every digest, so a
signature is good for one contract on one chain.

| Function | Selector |
| --- | --- |
| `stake(bytes32,uint8,uint256,uint256,bytes)` | `0x0a621329` |
| `settle(bytes32,uint8,bytes)` | `0x99bcbc7f` |
| `voidMatch(bytes32,bytes)` | `0xe3c01265` |
| `refundExpired(bytes32)` | `0xcc3e049b` |
| `matchOf(bytes32)` returns state (0 none, 1 open, 2 settled, 3 refunded), stake, time of the first stake, stakers of team 0, of team 1 | `0xa700e059` |
| `stakeOf(bytes32,address)` returns staked, team | `0x771c918e` |
| `stakersOf(bytes32)` returns players, teams | `0x27c6af2c` |
| `setReferee(address)`, `transferOwnership(address)` | `0xc4a796c1`, `0xf2fde38b` |
| `referee()`, `owner()`, `chog()`, `burnedShare()` | `0x3b1bebd1`, `0x8da5cb5b`, `0x53e1cb05`, `0x27369142` |

## Tests

The 23 tests run against stand-ins: two against two with exact numbers, one winner against two
losers, the remainder that cannot be shared, a ticket used by another player, for another team,
amount, match or deadline, by another signer, after its time; a signature that is cut, has the
wrong v, or is the twin on the other half of the curve; equal stakes, one stake each, the limit
of sixteen and a settlement with sixteen payments; a draw; a side without a staker; a result
signed by someone else, for another winner, used twice, on another match, on another contract;
a ticket brought as a result; nothing staked on a match that is over; a match called off; the
refund after an hour, to the second; a changed referee; the owner's two functions; a
half-configured deployment, with an address that is not a token; no approval and no funds; a
token whose transfers answer nothing; the two lists; and a fuzz test: whatever the stake, the
number of stakers on each side (1 to 8) and the burned share, after a result the contract holds
nothing and every unit paid in is with a winner or burned.

## Rehearsing a staked match on a local copy of the chain

The whole path, with the game services signing as the referee and the judge replaying a real
match (run on 11 October 2026, [evidence](../research/evidence/staked-match-rehearsal-2026-10-11.txt)):

```sh
anvil --fork-url https://rpc.monad.xyz --network monad --port 8545        # the copy, in its own terminal
sh tools/contracts/rehearse_secure_match.sh <replay.json> <build folder> [none | late-stake | wrong-team | no-lock]
```

It deploys the wager on the copy, starts the services with the referee's key and the judge,
and plays Photon's part from the record of a match (`server/rehearsal/secure_match.mjs` says
what is real in it). The contract alone, without the services:

```sh
anvil --fork-url https://rpc.monad.xyz --network monad --port 8545        # the copy, in its own terminal
sh tools/contracts/rehearse_wager.sh                                       # deploys the wager there, prepares two players
sh tools/contracts/rehearse_wager.sh play <address printed by the script> 100 3   # one match, 100 $CHOG each, three a side
```

The first command deploys the contract with a tenth burned and, as referee, the address of the
key published in the web3.js documentation (a public test value that holds nothing), and gives
two of anvil's accounts MON and 10,000 $CHOG. The second plays a match on it and puts the copy
back as it was: the tickets and the result are signed with that key by `cast wallet sign`, each
player approves and stakes, and a third account brings the result. Done on 10 October 2026
against the real $CHOG token on a copy (anvil on port 8546), one against one and three against
three: each winner went from 10,000 to 10,090 $CHOG, each loser to 9,900, a tenth of the lost
stakes was burned (10 and 30 $CHOG) and the contract held nothing afterwards. Gas used on the
copy:

| Transaction | Gas |
| --- | --- |
| `approve` of the real token | 62,228 (measured for the shop) |
| `stake`, the first of a match | 246,008 |
| `stake`, a later one | 177,896 |
| `settle`, one against one | 139,805 |
| `settle`, three against three | 205,710 |

Monad charges the gas limit of a transaction, not what it uses: the limits the game sends will
have to sit above these, and a settlement costs more with every winner to pay.

## Deploying on mainnet (not done)

For the testnet, see "On Monad testnet" at the top. Moving the wager from the testnet to mainnet
changes no code: the same contract, deployed with the real $CHOG token, and the services started
without `WAGER_NETWORK` (mainnet is the default) and with the new `WAGER_ADDRESS`; the game then
asks the wallet for Monad (`0x8f`), says $CHOG and offers no test tap. Seen on 15 October with
build `20261015T103308125Z` on a local copy of mainnet (`tools/contracts/wager_bench.sh`): a
stake of 10 $CHOG, locked at START, given back after the judge called the match off. With the
development wallet's keystore the command below can take `--account chog-dev` in place of
`--interactive`.

```sh
forge create contracts/src/ChogWager.sol:ChogWager --root contracts --rpc-url https://rpc.monad.xyz \
  --broadcast --interactive \
  --constructor-args 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777 <referee address> \
  <burned share, 0 to 10000> <owner>
```

The referee is the address of the key the backend signs with; the owner is a wallet kept
apart, able to replace it. This command was not run against the real chain.

## Limits

- **The referee is trusted to tell the winner.** The contract checks a signature, not a match.
  Whoever holds the referee's key can send every open stake to the side they choose, and can
  let anyone stake on any side. A stolen key is answered by `setReferee` from the owner (old
  signatures then count for nothing, also for open matches); a lost key, or a backend that is
  gone, by `refundExpired` after an hour. Stakes of matches already settled cannot be taken
  back.
- **A result must be brought within the hour.** After that anyone, a loser included, can have
  every stake given back, even if a signed result exists and nobody brought it.
- **Stakes must close before the match starts,** and the contract does not know when that is:
  it only checks the deadline of the ticket. A backend that signs a ticket during a match lets
  a player stake once the outcome is likely.
- Winners and refunds are paid in one transaction. If the token refused to pay one of the
  players (a blocklist, a pause), the payment of the whole match would fail, `refundExpired`
  included. Whether $CHOG can refuse a transfer that way was not checked.
- The contract was not reviewed by anyone else and not audited.
- Never used with a real wallet. From the game only with test wallets in the headless browser,
  on a local copy of mainnet (11 October) and a local chain started as the testnet (15 October).

# ChogRental: a Genesis played for a few days against $CHOG

**Not deployed.** Written on 10 October 2026 after the user asked for a rental where the player
pays and the holder is paid. Once the contract is deployed and its address given to the game
services, the menu lets a holder offer the chosen Genesis and lets any connected wallet rent
one that is offered. Until then nothing is shown. **No transaction was ever sent to the real
chain and no key of the project was used.**

## What the contract does

`src/ChogRental.sol`, about 200 lines, no dependency, not upgradeable, **no owner at all**.

- The holder of a Genesis offers it at a price a day in $CHOG (`offer`), changes that price by
  offering again, or takes the offer back (`withdraw`). Only the address that holds the token
  at that moment can offer it.
- Anyone rents an offered Genesis for a whole number of days (`rent` after an `approve`). The
  payment goes **straight from the renter to the holder**, less a share that is burned; the
  contract keeps nothing. `rent` carries the price the renter saw: if the holder raised it
  meanwhile the rental is refused, if the holder lowered it the lower price is charged.
- **The Genesis never moves and nothing is locked.** A rental is a right the game reads:
  `isRenting(tokenId, renter)`. The holder keeps playing, and several players can rent the
  same Genesis at once.
- More days are added after those already paid; nobody is paid up further than 30 days ahead.
- A rental that was paid runs to its end even if the offer is taken back or the Genesis sold.
  An offer made by a former holder cannot be rented any more: `rent` checks that its author
  still holds the token. The new holder clears it or replaces it with their own.
- A holder cannot rent their own Genesis.
- `standingOffers()` lists every offer that stands and `rentalsOf(address)` what an address
  rents now: one read each, so no indexer is needed (Monad's public endpoints limit log
  searches to 100 blocks).
- The token, the collection and the burned share cannot be changed afterwards.

## Tests

The 18 tests run against stand-ins: the payment split and its events, the end of a rental to
the second, days added after those paid, the 30-day limit, several renters at once, who may
offer, a Genesis that is not offered, a raised and a lowered price, a holder renting their own,
an offer taken back, a Genesis sold while rented and while offered, a former holder clearing
their offer, no approval and no funds, a token whose transfer answers nothing, the two lists,
a half-configured deployment, and a fuzz test that every unit paid is either burned or with
the holder.

## Rehearsing a rental on a local copy of the chain

```sh
anvil --fork-url https://rpc.monad.xyz --network monad --port 8545        # the copy, in its own terminal
sh tools/contracts/rehearse_rental.sh                                      # deploys the rental there, prepares a player
sh tools/contracts/measure_rental_gas.sh <address printed by the script>   # optional: what each transaction costs
node server/src/local.js --port 8091 --data .artifacts/services-fork \
     --rpc http://127.0.0.1:8545 --rental <address printed by the script>
python3 tools/unity/serve_webgl.py --latest --host 127.0.0.1 --port 8085 --services http://127.0.0.1:8091
sh tools/unity/checks/browser_check.sh rent http://127.0.0.1:8085 my-rental      # or "rentout"
```

The rehearsal script burns a tenth of each payment, offers the Genesis 3 and 408 at 10 $CHOG a
day from their real holders (impersonated on the copy only), and gives the player some MON,
10,000 $CHOG and the Genesis 1795. Gas used on the copy, with the real $CHOG token: 155,365 for
a first offer, 62,379 for a new price, 68,365 to take an offer back, 262,740 for a first rental
and 172,689 for more days. The game sends them with limits of 250,000, 120,000 and 400,000.

## Deploying (not done)

```sh
forge create contracts/src/ChogRental.sol:ChogRental --root contracts --rpc-url https://rpc.monad.xyz \
  --broadcast --interactive \
  --constructor-args 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777 0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763 \
  <burned share, 0 to 10000>
```

Then give the address to the services (`RENTAL_ADDRESS` for `server/src/host.js`, `--rental`
for `local.js`). This command was not run against the real chain.

## Limits

- The contract was not reviewed by anyone else and not audited.
- The match itself does not check a rental: like ownership, it is checked by the game services
  when they credit a career. On a public address the game only lets a wallet pick a Genesis it
  holds or rents, but a modified page could declare any Genesis in a match.
- A holder cannot end a rental that was paid, and a buyer of the Genesis inherits the rentals
  that are running (30 days at most).
- Nothing limits how many players rent one Genesis.
- Never made with a real wallet: the stand-in wallet of the rehearsal accepts every request.

## To decide before any deployment (the user)

| Question | What is built | Other options |
| --- | --- | --- |
| Share burned | fixed at deployment; the rehearsal used a tenth | none, or a part to a project treasury (needs a small change) |
| Who sets the price | each holder, per day, among steps from 1 to 5,000 $CHOG in the menu | a floor or a ceiling |
| One renter or several | several at once, the holder keeps playing | one at a time (scarcer, needs a queue) |
| The free link loan | kept beside the rental | removed once the rental is live |
| Who deploys | whoever has a funded Monad wallet; the contract has no owner afterwards | |

# ChogShop: items bought for a Genesis with $CHOG

**Not deployed.** This folder holds the contract and its tests. Since 10 October 2026 the game
knows how to use it: once the contract is deployed and its address given to the game services,
the menu offers the second form of a Genesis's attack for $CHOG (item 1), and the purchase is
two transactions of the player's wallet. Until then nothing is offered. The whole purchase was
rehearsed on a local copy of Monad mainnet (below); **no transaction was ever sent to the real
chain and no key of the project was used.** What is left to decide is at the bottom.

## What the contract does

`src/ChogShop.sol`, about 130 lines, no dependency, not upgradeable.

- The owner lists items with a price in $CHOG (`setItem`). That is the owner's only power.
- Anyone buys an item **for a Genesis token id** (`buy` after an `approve`, or `buyWithPermit`
  in one transaction for a key-pair wallet). The item stays with that token id: it follows the
  Genesis when it is sold, like its career, and a friend can offer it.
- Each payment is split between a treasury and a burn, in a share fixed at deployment.
- An item is bought once per Genesis. A token id that is not a Genesis is refused by the
  collection itself. `owned(tokenId, itemId)` and `ownedMany` are what a game or a page reads.
- The token, the collection, the treasury and the burned share cannot be changed afterwards.

## Tests

```sh
forge test                                                                    # 13 tests, no network
MONAD_FORK=1 forge test --network monad --match-contract ChogShopForkTest -vv   # 2 tests on a local copy of Monad mainnet
```

The first set runs against stand-ins: payment split, permit, a permit someone submitted first,
a permit from another key, a gift, once per Genesis, a token that does not exist, no approval,
an item taken off sale, ownership, a half-configured deployment, and a fuzz test that every
unit paid is either burned or in the treasury.

The second set runs the same contract against **the real $CHOG token**
(`0x350035555E10d9AfAF1566AaebfCeD5BA6C27777`) **and the real Chog Genesis collection**
(`0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763`) on a local copy of the chain made by Foundry.
A test key is given $CHOG on that copy only. Result on 9 October 2026: the real token accepts
the EIP-2612 permit built from its on-chain domain separator, the buyer pays exactly the price
(no transfer fee), half reaches the treasury, the total supply drops by the burned half, and
token id 1970 is refused. This is the "dust permit" check the research asked for, done on a
copy: **a real transaction on mainnet has still never been sent.**

## Rehearsing the purchase on a local copy of the chain

```sh
anvil --fork-url https://rpc.monad.xyz --network monad --port 8545        # the copy, in its own terminal
sh tools/contracts/rehearse_shop.sh                                        # deploys the shop there, prepares a buyer
node server/src/local.js --port 8091 --data .artifacts/services-fork \
     --rpc http://127.0.0.1:8545 --shop <address printed by the script>
python3 tools/unity/serve_webgl.py --latest --host 127.0.0.1 --port 8085 --services http://127.0.0.1:8091
```

The script acts only on an anvil endpoint whose chain id is Monad's, with accounts anvil
unlocks or impersonates. Done on 10 October 2026, with the game in a headless browser whose
stand-in wallet forwards its transactions to the copy (`CHOG_BROWSER_WALLET_RPC` of
`tools/unity/qualify_webgl.mjs`): the menu showed the price read from the contract, the wallet
was asked for the approval then the purchase, `owned(408, 1)` became true, the buyer paid
exactly the price, half went to the treasury, and the game unlocked the second form
([screens](../research/evidence/genesis-chog-purchase-on-fork-headless-2026-10-10.jpg)). A
wallet without enough $CHOG was told so before signing anything. Gas used on the copy: 62,228
for the approval of the real token, 205,100 for a first purchase and 188,112 for a later one;
the game sends them with limits of 100,000 and 300,000.

## Deploying (not done)

The constructor takes the $CHOG token, the Genesis collection, the treasury, the share burned
in basis points, and the owner who will set prices:

```sh
forge create contracts/src/ChogShop.sol:ChogShop --root contracts --rpc-url https://rpc.monad.xyz \
  --broadcast --interactive \
  --constructor-args 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777 0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763 \
  <treasury> <burned share, 0 to 10000> <owner>
cast send <shop> "setItem(uint256,uint128,bool)" 1 $(cast to-wei <price in $CHOG>) true \
  --rpc-url https://rpc.monad.xyz --interactive
```

`--interactive` asks for the key at the prompt, so it is never written in a file or a command.
Then give the address to the services (`SHOP_ADDRESS` for `server/src/host.js`, `--shop` for
`local.js`). These two commands were not run against the real chain: on the copy the same
contract was deployed with `forge create` and listed with `cast send`, from an account anvil
unlocks instead of a key (`tools/contracts/rehearse_shop.sh`).

## Limits

- A smart-contract wallet cannot use `buyWithPermit` (the token's permit takes a key-pair
  signature only); it uses `approve` then `buy`. The game uses `approve` then `buy` for every
  wallet: two confirmations, no typed-data signature.
- The contract was not reviewed by anyone else and not audited.
- Monad charges gas on the limit of a transaction, not on what it uses. The limits the game
  sends come from the costs measured on the copy; a token upgrade could change them.
- The purchase was never made with a real wallet: the stand-in accepts every request at once.
- Nothing reads `Purchased` events: the game services read `owned` directly.

## To decide before any deployment (the user)

| Question | Options |
| --- | --- |
| What is sold | Built: the second form of a Genesis's attack, at once instead of at mastery 2 (item 1). It is a side-grade, not strength, and mastery gives it for free. Other items (a ring around the portrait, for instance) need their own design. |
| At what price | Set by the owner with `setItem`, changeable at any time. The rehearsals used 500 and 750 $CHOG, numbers without meaning. |
| Where the $CHOG goes | A treasury address, a burn, or a split (`burnedShare`, fixed for the life of the contract). |
| Who deploys and owns it | A deployment needs a funded Monad wallet; its owner sets the prices. |
