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
reads.

**Deployed by the owner on 16 October 2026** from the development wallet
`0x981F6E0Ea94f45fDB8ee7680DC862212E3C720e0` (owner of the wager and of the shop, treasury of the shop),
and checked on the chain with the script above:

| Contract | Address on Monad mainnet |
| --- | --- |
| ChogWager | `0xc7524a808d2b00b43a4d3e2becb8500e409b41e3` (referee `0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34`) |
| ChogRental | `0x7f1271d43b0e41e2eeddd5290f459fdc6196a19a` (a tenth of each rental burned) |
| ChogShop | `0xb23a3c3492b9ba83d80c8abc9a5484d2885f058a` (the second form on sale at 100 $CHOG) |

The public instance takes stakes, rentals and purchases on them since that day (`serve_public.sh ... --mainnet`;
`render.yaml` names them for the host).

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

**Deployed on Monad mainnet on 16 October 2026 (top of this file); never used with a real wallet yet.** Written on 10 October 2026 after the user
put staking on a match in scope (decision D33 in `docs/STATUS.md`). The backend that signs
tickets and results (`server/src/wager.js`, `referee.js`) and the screens that stake and collect
(`WagerPanel`, `WagerResult`) exist since 11 October.

## Version 2: the Genesis players' share (22 October 2026, decided by the owner; not deployed yet)

Of what the losers of a match staked, `burnedShare` is burned and `genesisShare` is split equally
between the wallets that played a Chog Genesis in the match, staked or not (at most six, named by
the referee with the result and signed with it). With 500 and 500 a winner gains exactly what
version 1 paid (a tenth is still taken from the lost stakes) and half of what version 1 burned
goes to the Genesis players; a match without one burns their share; a draw or a refund pays none.
Why: the Genesis carries the game and its players bring the collection to the arena, staked or not;
their share never depends on the result, so none gains by losing. `settle` takes the list
(`settle(bytes32,uint8,address[],bytes)`); the services read `genesisShare()` and sign version 1
results as before while `WAGER_ADDRESS` still names version 1, and the game encodes the call for the
version `/api/wager` tells it. Tests: `test/ChogWager.t.sol` (the split, the signed list, the cap,
the deploy bound, a fuzz that every unit is with a winner, a Genesis player or burned).

Deploying it (the owner, with the deploying keystore; the referee is the one version 1 knows,
`0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34` read on chain on 22 October 2026), then setting the
printed address as `WAGER_ADDRESS` of the services on Render:

```sh
cd contracts && REFEREE=0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34 ~/.foundry/bin/forge script \
  script/DeployWager.s.sol --rpc-url monad --account chog-dev --broadcast
```

Matches staked on version 1 before the switch are no longer signed for it: anyone gives their
stakes back an hour after the first stake (`refundExpired`).

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

## Deploying on mainnet (done on 16 October 2026, see the top of this file)

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
- **Probably a regulated wager.** Read on 17 October 2026 (not legal advice): in France, article L. 320-1 of the code
  de la sécurité intérieure, as rewritten by ordonnance 2019-1015, bans games with a financial sacrifice and a gain due
  even partly to chance, and adds "Cette interdiction recouvre les jeux dont le fonctionnement repose sur le savoir-faire
  des joueurs" (https://www.legifrance.gouv.fr/jorf/id/JORFSCTA000039167505); in Great Britain the Gambling Commission
  finds "no basis in legislation" for saying a bet on oneself is not a bet (2017 position paper, 4.14); in the US it
  depends on the state. Details and the professional fixes: `docs/PRODUCT-REVIEW.md`.

# ChogRental: a Genesis played for a few days against $CHOG

**Version 2, on the branch `rental-v2`, not deployed.** The version deployed on Monad mainnet on 16 October 2026 (top of
this file, `0x7f1271d43b0e41e2eeddd5290f459fdc6196a19a`, no offer and no rental made on it) let a paid rental run to its
end even when the Genesis was sold, so its buyer inherited a right they could neither see nor stop. On 17 October the
owner asked to follow current practice instead: version 2 does what the reference implementation of ERC-4907,
Yuga's ExclusiveDelegateResolver and delegate.xyz's guidance do (the right ends when the holder who was paid no longer
holds the Genesis), and holds the payment as reNFT's escrow did, so the renter gets back the time not delivered
(sources and the comparison in [docs/PRODUCT-REVIEW.md](../docs/PRODUCT-REVIEW.md), "The rental"). Written first on
10 October 2026 after the user asked for a rental where the player pays and the holder is paid.

## What the contract does

`src/ChogRental.sol`, about 350 lines, no dependency, not upgradeable, **no owner at all**.

- The holder of a Genesis offers it at a price a day in $CHOG (`offer`), changes that price by
  offering again, or takes the offer back (`withdraw`). Only the address that holds the token
  at that moment can offer it. Taking the offer back never ends a running rental.
- **One player at a time** rents an offered Genesis for a whole number of days (`rent` after an
  `approve` of price x days), as ERC-4907, Axie's delegation and reNFT do for one token. `rent`
  carries the price the renter saw: if the holder raised it meanwhile the rental is refused, if
  the holder lowered it the lower price is charged. While a rental runs nobody else can rent
  that Genesis (`AlreadyRented`); its renter adds days after those already paid, nobody is paid
  up further than 30 days ahead.
- **The payment is held by the contract** and the holder earns it **by the second** (reNFT's
  formula, seconds x price / 86400). Days added at another price are spread, with what is left
  of the first payment, evenly over the time left.
- **The Genesis never moves and nothing is locked.** A rental is a right the game reads:
  `isRenting(tokenId, renter)` is true while the paid time runs **and the holder who was paid
  still holds the Genesis**. The holder keeps playing.
- **A sale ends the right at once.** The contract cannot see when a Genesis changed hands, so
  anyone may `close` the rental once its holder no longer holds it (or once its time is over):
  the holder has earned the time up to that moment, the renter gets the rest back. The game
  services are meant to close it as soon as they see the transfer; until then the seller earns
  that time. The renter may also end a rental early (`endRental`) and get the rest back.
- **Each party takes its own money out** (`claim(tokenIds)`): what it earned as a holder, less
  the burned share, and what it is owed back as a renter, whole. The rentals listed are settled
  first, by whoever calls, and those whose holder sold or whose time is over are closed. A
  transfer the token refuses to one address only fails that address's claim.
- A holder cannot rent their own Genesis. An offer made by a former holder cannot be rented.
- `standingOffers()` lists every offer that stands and is not rented now, `rentalsOf(address)`
  the rentals of an address that still count: one read each, so no indexer is needed (Monad's
  public endpoints limit log searches to 100 blocks). A burned Genesis does not stop them.
- The token, the collection and the burned share cannot be changed afterwards.

What changed for the callers (selectors unchanged unless said):

| Function | Selector | Version 2 |
| --- | --- | --- |
| `offer(uint256,uint128)` | `0x92f6d20c` | unchanged |
| `withdraw(uint256)` | `0x2e1a7d4d` | unchanged (takes the offer back, never ends a rental) |
| `rent(uint256,uint256,uint128)` | `0xcfb0c2ab` | same call; the payment goes to the contract, not to the holder; refused with `AlreadyRented(tokenId, until)` while another player rents it |
| `isRenting(uint256,address)` | `0xec06af47` | false as soon as the holder who was paid no longer holds the Genesis |
| `standingOffers()` | `0x96f6c6be` | leaves out a Genesis rented now, so its renter finds its price with `offerOf` to add days |
| `rentalsOf(address)` | `0xf43163ac` | leaves out a rental whose holder sold the Genesis |
| `offerOf(uint256)` | `0xa3cb2f44` | unchanged (still true while the Genesis is rented) |
| `rentedUntil(uint256,address)` | `0x4c3dce8b` | was a public mapping, now a view: the end of that address's rental on record, 0 when none |
| `claim(uint256[])` | `0x6ba4c138` | new: settles the rentals listed and pays the caller what they earned (less the burned share) and are owed back |
| `close(uint256)` | `0x0aebeb4e` | new: anyone, once the holder sold or the time is over |
| `endRental(uint256)` | `0x97491d6f` | new: the renter ends early |
| `rentalOf(uint256)` | `0xfc6779f9` | new view: renter, holder paid, end, payment still held, part of it earned since the last settlement |
| `earnings(address)`, `refunds(address)` | `0x543fd313`, `0xbc3da535` | new views: what an address can claim, before the burned share for earnings |

New events: `Earned(tokenId, lender, amount)`, `Ended(tokenId, renter, lender, refunded)`, `Claimed(account, paid,
burned)`. New errors: `AlreadyRented`, `NotRenting`, `NoRental`, `StillRunning`.

## Tests

The 27 tests of `ChogRentalTest` run against stand-ins: the payment held and earned by the
second, the holder's claim less the burned share, the end of a rental to the second and its
closing, a sale ending the right at once and the closing that refunds the time not delivered,
a renter whose own claim closes the rental after a sale, no closing while a rental runs, a
burned Genesis, an early end by the renter and by nobody else, one renter at a time and the
next one after the end, days added after those paid and at another price, the 30-day limit,
who may offer, a Genesis that is not offered, a raised and a lowered price, a holder renting
their own, an offer taken back, a former holder clearing their offer, no approval and no funds,
a payment refused to the holder that does not block the renter's refund, a claim of nothing,
a token whose transfers answer nothing, the two lists, a half-configured deployment, and a
fuzz test that every unit paid in is paid out, refunded or burned, none created or lost,
whatever the price, the days added, the burned share and when the holder claims and sells.

Gas of each step with every account cold, as a transaction of its own (`ChogRentalGasTest`,
`forge test --match-contract ChogRentalGasTest -vv`, with the stand-in token, 17 October 2026):

| Step | Gas |
| --- | --- |
| first offer | 163,865 |
| new price | 62,467 |
| take an offer back | 50,733 |
| first rental | 232,013 |
| more days | 126,050 |
| a rental again after an early end | 167,409 |
| the holder's claim, one Genesis | 107,737 |
| the renter ends early | 58,854 |
| the renter's claim of a refund | 49,957 |
| close after a sale | 88,808 |

Deployed size 8,611 bytes. Not measured with the real $CHOG token, whose transfers cost more
than the stand-in's (the copy of mainnet gave 262,740 for a first rental of version 1, which
made two transfers and a burn where version 2 makes one transfer).

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
10,000 $CHOG and the Genesis 1795. Gas used on the copy by version 1, with the real $CHOG token:
155,365 for a first offer, 62,379 for a new price, 68,365 to take an offer back, 262,740 for a
first rental and 172,689 for more days. The game sends them with limits of 250,000, 120,000 and
400,000. Version 2 was not rehearsed on the copy yet.

## Deploying (done on 16 October 2026, see the top of this file)

```sh
forge create contracts/src/ChogRental.sol:ChogRental --root contracts --rpc-url https://rpc.monad.xyz \
  --broadcast --interactive \
  --constructor-args 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777 0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763 \
  <burned share, 0 to 10000>
```

Then give the address to the services (`RENTAL_ADDRESS` for `server/src/host.js`, `--rental`
for `local.js`). Version 1 was deployed on 16 October 2026 by the owner's script
(`script/DeployMainnet.s.sol`); version 2 is not deployed.

## Limits

- The contract was not reviewed by anyone else and not audited.
- The match itself does not check a rental: like ownership, it is checked by the game services
  when they credit a career. On a public address the game only lets a wallet pick a Genesis it
  holds or rents, but a modified page could declare any Genesis in a match.
- Between a sale and the closing of the rental the seller earns the time (the contract cannot
  see the sale); the right itself stops at the sale. If the seller buys the Genesis back before
  anyone closes the rental, it counts again as if it had never stopped.
- A holder cannot end a rental that runs: it ends at its time, by its renter, or by a sale.
- Never made with a real wallet: the stand-in wallet of the rehearsal accepts every request.

## Before deploying version 2

- The services: `isRenting` and `rentalsOf` already give the new answers; they should also
  call `close` (or have it called) when they see a rented Genesis change hands.
- The game: a renter adds days with the price read by `offerOf` (the rented Genesis leaves
  `standingOffers`); the holder needs a CLAIM button (`claim` with the Genesis they offer), the
  renter one to end early and one to take a refund out; the text of the offer screen says one
  player at a time and that a sale ends the rental.
- The owner deploys it, then the services' `RENTAL_ADDRESS` changes; version 1 has no offer
  and no rental, so nothing is left behind on it.

| Question | What is built | Other options |
| --- | --- | --- |
| Share burned | fixed at deployment, taken from what the holder earns; the rehearsal used a tenth | none, or a part to a project treasury (needs a small change) |
| Who sets the price | each holder, per day, among steps from 1 to 5,000 $CHOG in the menu | a floor or a ceiling |
| The free link loan | kept beside the rental | removed once the rental is live |
| Who deploys | whoever has a funded Monad wallet; the contract has no owner afterwards | |

# ChogShop: items bought for a Genesis with $CHOG

**Deployed on Monad mainnet on 16 October 2026 (top of this file).** The owner bought the second form of #1287 (token
1795) with their Ledger that day, for 100 $CHOG: the shop's only use by a real wallet so far. With its address given to the
game services, the menu offers the second form of a Genesis's attack for $CHOG (item 1), two transactions of the player's
wallet. What is left to decide is at the bottom.

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

## Deploying (done on 16 October 2026, see the top of this file)

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

# ChogCareers: the career of each Genesis, on chain

**Written on 17 October 2026, not deployed.** The owner asked for as many services on chain as possible ("on veut le
plus de service possible onchain"). Until now careers are kept by the game services in a file that Render's free plan
loses "every time the service redeploys, restarts, or spins down" (https://render.com/docs/free.md), and a match is
credited on the word of the player's own page. Here the referee records each match once the judge has replayed it,
and the career is keyed by token id, so it follows the Genesis when the token is sold (`docs/PRODUCT-REVIEW.md`,
"Careers that follow the NFT").

## What the contract does

`src/ChogCareers.sol`, no dependency, not upgradeable.

- `record(bytes32 matchKey, uint8 winnerTeam, Entry[] entries)`: only the referee (`msg.sender`), once per match key
  (meant to be `keccak256` of the name the game server gives the match, as for the wager). `winnerTeam` is 0 or 1, 2 for
  a draw. An `Entry` is `(uint16 tokenId, uint8 team, uint8 playedBy)`, `playedBy` 0 holder, 1 renter, 2 guest; entries
  come in strictly ascending token id (which is what makes a Genesis count once per match), 1 to 16 of them, token ids
  1 to 1969. Bots and recruits have no entry.
- Per Genesis, one storage word: `matches`, `wins`, `draws`, `rentedMatches`, `guestMatches`, `streak` (wins in a row up
  to the last match; a loss or a draw ends it), `bestStreak`, `lastPlayedAt` (seconds).
- Events: `MatchRecorded(matchKey indexed, winnerTeam, entries)` and, for each entry, `Played(matchKey indexed,
  tokenId indexed, team, playedBy, outcome)` with outcome 0 lost, 1 won, 2 draw, so the whole history can be rebuilt
  from logs. Logs of `rpc.monad.xyz` are limited to 100 blocks a request, so the game reads the counters, not the logs.
- Reads: `careerOf(tokenId)`, `careersOf(uint256[])`, `resultOf(matchKey)` (recorded, winner), and
  `packedCareers(firstTokenId, count)`, the raw word of each career for a reader that ranks every Genesis in one call.
  From the lowest bits: matches 32, wins 32, draws 32, rentedMatches 32, guestMatches 32, streak 16, bestStreak 16,
  lastPlayedAt 64.
- The owner can only replace the referee (`setReferee`) and hand the contract over (`transferOwnership`).
- Mastery and badges are not stored: they are read from the counters off chain, with the rule of
  `server/src/career.js` (`masteryOf`, `BADGES`), which stays the source.
- The careers are a fixed array declared first, one word per token id from slot 1, rather than a mapping. Monad prices
  cold storage by page of 128 consecutive slots since its revision MONAD_TEN (MIP-8, active on mainnet since
  2026-09-02 14:30 UTC, https://docs.monad.xyz/developer-essentials/changelog/releases.md), and "each key of a mapping
  still resolves to its own page" (https://docs.monad.xyz/developer-essentials/opcode-pricing.md): with the array, 128
  careers share a page and reading all 1,969 touches 16 pages.

## Gas, measured on Monad mainnet itself

Measured on 17 October 2026 with `eth_estimateGas` on `https://rpc.monad.xyz`, the contract's runtime code and storage
given as a state override (nothing deployed, nothing sent), so with Monad's own gas rules
(`python3 -I tools/contracts/estimate_careers_gas.py` measures again). The figures include the 21,000 of any
transaction.

| Call | First match of these Genesis | Genesis that already played |
| --- | --- | --- |
| `record`, 1 Genesis | 95,698 | 78,630 |
| `record`, 3 Genesis | 164,239 | 111,811 |
| `record`, 6 Genesis on 6 pages | 258,399 | 154,397 |
| `record`, 6 Genesis on one page (token ids 1 to 127) | 211,985 | 108,166 |
| `packedCareers(1, 1969)` as an `eth_call` | 960,361 | |
| `careersOf` of the 1,969 ids as an `eth_call` | 3,057,770 | |

The public endpoints allow 200M gas per `eth_call` (Monad's JSON-RPC overview, read 10 October 2026), so one call ranks
every Genesis. About 28,000 gas of each first record is the match key, which is what keeps a match from being counted
twice.

**Monad charges the gas limit, not the gas used.** Its documentation says so: "The gas charged for a transaction is the
**gas limit**", `gas_paid = gas_limit * price_per_gas`, a choice made for asynchronous execution
(https://docs.monad.xyz/developer-essentials/gas-pricing.md, read 17 October 2026; minimum base fee 100 MON-gwei,
transaction gas limit 30M). So the services must send each record with a limit close to its estimate, not a round
ceiling.

Cost per match at the price read that morning (102 gwei from `cast gas-price`, MON at 0.0227 USD from CoinGecko at
09:05), with a limit equal to the estimate:

| Match | MON | USD |
| --- | --- | --- |
| 1 Genesis, already played | 0.0080 | 0.00018 |
| 1 Genesis, first match | 0.0098 | 0.00022 |
| 6 Genesis, already played | 0.0157 | 0.00036 |
| 6 Genesis, first match | 0.0264 | 0.00060 |

At that price 10 MON pays for about 380 matches of six Genesis playing for the first time, and about 1,250 of one
Genesis that already played. The tests in `test/ChogCareers.t.sol` print the same calls
under Ethereum's rules, which are not Monad's.

## Tests

`forge test --match-contract ChogCareersTest`: 16 tests, one of them a fuzz test of the counters over 24 random results.
Only the referee records, a match key once, a whole 3v3 with every way of playing, streaks across wins, losses and
draws, a draw for everyone, rented and guest counts, token ids out of the collection, a Genesis twice or out of order
in one match (nothing of the refused match is kept), bad teams and kinds, 0 and 17 entries, the events of a match,
`careersOf` and `packedCareers` over the 1,969 ids (the packed words decoded and compared), the owner's two powers,
the constructor.

## Deploying (not done)

In the owner's terminal, with the development wallet imported once
(`~/.foundry/bin/cast wallet import chog-dev --interactive`) and a little MON on it (the deployment is one
transaction):

```sh
cd contracts
REFEREE=<address of the key the services record with> \
  ~/.foundry/bin/forge script script/DeployCareers.s.sol --rpc-url monad --account chog-dev --broadcast
```

The script refuses any chain but 143 and a referee equal to the deploying wallet. The referee then needs MON for the
records; the services do not send any record before they are given the contract's address.

## Limits

- **The referee is trusted.** The contract checks who sends a record, not the match. Whoever holds the referee's key
  can write any result for any Genesis; a stolen key is answered by `setReferee` from the owner, but what it wrote
  stays. The services write only after the judge's replay, from the inputs Photon's server recorded.
- **The referee pays the gas.** Its MON balance is the hard limit on what can be recorded. A player who plays many
  matches costs the project their gas; the services bound it: a match is recorded only with a verdict of the judge
  and at least one Genesis played by a signed-in holder, renter or guest, at most one match a minute per Genesis
  (as `career.js` does today), and within a daily number of records. To be written in the services.
- A match that the services never record (a restart before the verdict, a referee without MON) is simply not in the
  careers.
- Not reviewed by anyone else, not audited.
