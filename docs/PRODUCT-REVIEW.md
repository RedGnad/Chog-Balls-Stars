# Product review of the rental, the stakes and the careers (17 October 2026)

Asked by the owner on 17 October 2026, before selling #1287: is the rental and wager model sound, measured against
current professional practice and sources, with innovation welcome where it holds. Read that day; V = read on the
primary page, in the code or on chain with `cast`; I = inferred. Not legal advice.

## The rental

### What practice does when the NFT is sold during a rental

| Model | On sale | Renter protection | Custody | Exclusive | Payment | Alive in 2026 |
| --- | --- | --- | --- | --- | --- | --- |
| ERC-4907 reference (https://eips.ethereum.org/EIPS/eip-4907) | the transfer hook deletes the user (V) | none: the owner "can change the NFT user and expires at any time" (V) | none, the collection must implement it | one user (V) | out of scope | Final; its authors' repositories last pushed 2024-03-16 (V) |
| ERC-5006 (https://github.com/ethereum/ERCs/blob/master/assets/erc-5006/contracts/ERC5006.sol) | rented units move into the token contract, so cannot be sold (V) | none | inside the token contract | several users of ERC-1155 units (V) | out of scope | Final |
| ERC-7066 (https://eips.ethereum.org/EIPS/eip-7066) | a transfer reverts while locked (V) | the lock | the collection must implement it | n/a | n/a | Final |
| delegate.xyz v2 (https://github.com/delegatexyz/delegate-registry) | the record survives, the registry never reads `ownerOf` (V); integrators must "double check that the wallet who delegated this token owns the NFT" (https://docs.delegate.xyz/integrate-in-your-project/token-ownership-claiming, V) | none | none | not enforced (V) | none | pushed 2026-08-18 (V) |
| Yuga ExclusiveDelegateResolver (https://github.com/yuga-labs/ExclusiveDelegateResolver) | starts from the current `ownerOf`, so the seller's delegations stop counting (V) | none | none | one delegate, expiry (V) | none | used on Otherside and ApeChain per delegate.xyz (V) |
| Delegate Market (https://github.com/delegatexyz/delegate-market) | NFT escrowed; the claim at expiry is tradeable (V) | full | custodial | one | sold on marketplaces | pushed 2025-09-29 (V) |
| reNFT v2 "Sylvester" (https://github.com/re-nft/_v2_.contracts-sylvester) | NFT escrowed; `stopLend` reverts while rented (V) | an early return refunds the unused time (V) | custodial | one renter for ERC-721 (V) | paid upfront into escrow, the lender earns `seconds x price / 86400` (V) | last push 2023-04-28; dormant (I) |
| reNFT v3 (https://github.com/re-nft/_v3_.smart-contracts) | NFT in the renter's Safe, a guard blocks transfers (V) | cannot be stopped before expiry (V) | renter's Safe | per order | escrow, split pro rata (V) | last push 2024-06-03; dormant (I) |
| Axie Infinity delegation (https://support.axieinfinity.com/hc/en-us/articles/29990965331867-Axie-Delegation-Guide, updated 2026-09-11) | "you cannot transfer Axies during delegation until revoked or expired" (V) | the owner may revoke only after 24 h (V) | stays in the owner's wallet, transfers blocked (V) | "one axie can only be delegated to one user at a time" (V) | no rent; reward splitting not in the beta (https://blog.axieinfinity.com/p/axie-delegation-is-live, V) | live; Ronin uses delegate.xyz v2 (https://docs.roninchain.com/developers/tools/delegation, V) |
| **ChogRental, deployed 16 October** | **the right survives** | full | none | several at once | upfront, straight to the holder, no refund | |

Nothing found keeps a paid rental running through an unrestricted sale: where the right survives, the sale is blocked
(Axie, reNFT) or the buyer knowingly buys an encumbered claim (Delegate Market). Systems that leave the NFT in place end
the right when the token changes hands. The Genesis collection cannot lock or hook transfers: it answers false for the
ERC-7066 and ERC-4907 interfaces and its EIP-1967 slot is empty (V, `cast`), so without custody nothing can stop a sale.

### The design that follows practice

1. The rental contract holds the renter's payment; the holder earns it by the second (reNFT Sylvester's formula) and
   withdraws what is earned; the burned tenth is taken from the earned part, so refunds are whole.
2. The right ends with the holding: `isRenting` is true only while `ownerOf(tokenId)` is still the lender (ERC-4907
   reference, Yuga's resolver, delegate.xyz's guidance).
3. After a sale the renter withdraws the unused time; anyone may close the rental, and the services, which already read
   owners, close it when they see the transfer, so the seller is paid only for the time delivered.
4. One renter at a time, as Axie, ERC-4907 and reNFT for ERC-721 (several at once has only ERC-5006's units as a
   precedent); at most 30 days.

An idea without precedent found (I): the rental follows the token, the new holder receives what is earned after the
sale and may end the rentals with refunds, as a buyer of a let house in French law (Civil Code art. 1743). Its weak
point: marketplaces will not show those rentals to a buyer.

## The stakes on a match

### Practice of established products

| Practice | Source |
| --- | --- |
| Players matched by skill inside a paid contest | Skillz 10-K FY2025 (https://www.sec.gov/Archives/edgar/data/1801661/000180166126000020/sklz-20251231.htm, V) |
| A challenge at a fixed amount that the other side accepts; in teams every teammate accepts first | GamerSaloon (https://www.gamersaloon.com/docs/how_it_works, V) |
| Server authority, inputs signed by each player, a replay players can check | Elympics (https://www.elympics.ai/solutions/security-fairness, V) |
| Statistical cheat detection, sandbagging and multi-accounting detected, proctoring for prize events | Chess.com (https://www.chess.com/fair-play, V) |
| One account per person, collusion and throwing banned; 18+; geofencing and identity checks | GamerSaloon terms (https://www.gamersaloon.com/docs/terms, V); Skillz 10-K (V) |

### Law, from the texts (not legal advice)

| Where | Text | A skill match with token stakes |
| --- | --- | --- |
| France | Code de la sécurité intérieure art. L. 320-1 as rewritten by ordonnance 2019-1015 (https://www.legifrance.gouv.fr/jorf/id/JORFSCTA000039167505, V): games with a financial sacrifice and a gain due "même partiellement" to chance are banned, and "Cette interdiction recouvre les jeux dont le fonctionnement repose sur le savoir-faire des joueurs"; the sacrifice exists "même si un remboursement ultérieur est rendu possible". Only sports and horse betting and poker are licensed online (https://anj.fr/offre-de-jeu-et-marche/categories-de-jeux-et-canaux-de-distribution, V) | Most likely banned (I); no ruling on a token stake found |
| France, JONUM | SREN law art. 40, three-year experiment from 21 May 2024, "à l'exclusion de l'obtention de tout gain monétaire" (https://www.legifrance.gouv.fr/jorf/article_jo/JORFARTI000049563458, V); decree 2026-60 in force since 7 February 2026: rewards paid by the operator, capped (https://www.legifrance.gouv.fr/jorf/id/JORFTEXT000053443861, V) | A pot where losers pay winners is not this model (I) |
| Great Britain | Gambling Act 2005 s.6, s.9, s.12 (https://www.legislation.gov.uk/ukpga/2005/19/section/12, V); the Gambling Commission's 2017 paper finds "no basis in legislation" for saying a bet on oneself is not a bet (4.14) and counts tradeable tokens as money's worth (3.8) (V) | Probably betting needing a licence (I) |
| United States | federal UIGEA (https://www.law.cornell.edu/uscode/text/31/5362, V); states differ, e.g. Arizona covers a "contest of chance or skill" (https://www.azleg.gov/ars/13/03301.htm, V) | Legal in some states, not in others; operators geofence |

### Gaps of ChogWager against that practice

1. In a 3v3, teammates who did not stake can decide the result; practice stakes only in 1v1 or full premade teams.
2. One stake per address does not stop one person with several wallets; practice verifies one person per account.
3. The referee key decides every open stake; the inputs are recorded by the game server, not signed by the players.
4. The owner is one development wallet that can replace the referee at once; practice uses a multisig and a delay.
5. Not audited.
6. The refund clock starts at the first stake, and every payment goes out in one loop (a refused transfer blocks all).
7. No age check, identity check or geofencing.

## Careers that follow the NFT

On Render's free plan local files are lost "every time the service redeploys, restarts, or spins down" and "Render might
restart a Free web service at any time" (https://render.com/docs/free.md, V); every new build redeploys. Careers are also
declared by the player's page today, not by the replay.

| Option | Follows the NFT | Cost (17 October) | Source |
| --- | --- | --- | --- |
| A registry on Monad mainnet keyed by token id, written by the referee after the replay | by construction | gas 102 gwei, MON 0.0227 USD (CoinGecko, 09:05); a write estimated at 50,000 to 150,000 gas, not measured: at most about 0.0004 USD a match | `cast gas-price` (V) |
| ERC-6551 token-bound accounts | yes | registry deployed on Monad at `0x000000006551c19487814612e58FE06813775758` (571 bytes, V); the standard is still in Review | https://eips.ethereum.org/EIPS/eip-6551 |
| EAS attestations | yes | not deployed on Monad (V) | https://github.com/ethereum-attestation-service/eas-contracts/tree/master/deployments |
| Neon Postgres free | no, a database | 1 GB a project, "None of these limits delete your data" (V) | https://neon.com/docs/introduction/plans |
| Render Postgres free | no | "expire 30 days after creation" (V) | https://render.com/docs/free.md |

## Building blocks on Monad mainnet (checked with `cast code`, 17 October 2026)

| Contract | Address | Code |
| --- | --- | --- |
| delegate.xyz DelegateRegistry v2 | `0x00000000000000447e69651d841bD8D104Bed493` | none (Monad listed only as a testnet) |
| delegate.xyz DelegationRegistry v1 | `0x00000000000076A84feF008CDAbe6409d2FE638B` | 6,230 bytes |
| Immutable CREATE2 factory | `0x0000000000FFe8B47B3e2130213B802212439497` | present, so the registry v2 can be deployed at its usual address (I) |
| ERC-6551 registry | `0x000000006551c19487814612e58FE06813775758` | 571 bytes |
| Sablier Lockup v4.0 / Flow v3.0 | `0x82723C1ffEc9D43dE5FA80b25Da8df99AfD470ba` / `0x95004df5abe86a246664d8f5fb2683f24df768d1` | present (https://docs.sablier.com/guides/lockup/deployments) |
| EAS, Superfluid | | not on Monad |
