# Submission draft (Chogathon, form open until 23 October 2026, 15:00 UTC)

A draft written on 15 October 2026 for the owner to correct, in the order of the form
(`research/evidence/chogathon-site-facts-2026-10-15.json`). Every sentence describes what the game
does today; what is not done yet is marked **[not yet]** and must be removed or made true before
submitting. Character counts are for the 8,000-character fields.

## 01 Basics

- **Project name:** Chog Arena (chosen by the owner on 16 October 2026; Chog Genesis is the NFT collection)
- **Banner:** to make from a capture of the menu with a hero in 3D, or of the heroes sheet
  (`research/evidence/heroes-and-attack-objects-headless-2026-10-15.png`).

**Description**

> Chog Arena is a real-time 3v3 arena for phones where your Chog Genesis NFT is your hero. Connect
> the wallet that holds it and the game builds your Chog in 3D from its own traits: its hat, its eyes,
> its mouth, its clothes and its skin, modelled by us for all 1,969 Genesis, the skulls, the radioactive
> Chogs and the ten 1:1 included. The
> traits decide how it plays: its outfit gives its class (brute, ranger, controller), its eyes its
> special attack, its aura the effect of that attack, its rarity how the attack is delivered, and its
> hat what its basic attack throws: snowballs from a Russian Hat, dollar coins from a Dollar Cap, its
> own cap spinning like a frisbee from a word cap, a leaf of its spiky crown when it wears nothing.
> Two modes: SIEGE, where each team defends a tower with a Chog's head that shoots lasers from its eyes
> and a beating heart for a core, three minutes to bring the other core down or to do more damage; and
> COINS, a race for the coins of the arena. Matches are played on Photon Quantum's deterministic engine; after each match our services replay
> the recorded inputs to decide the winner themselves, so a result can be trusted with $CHOG at
> stake. Each Genesis keeps a public career that follows the NFT, not the wallet (matches, wins,
> badges, mastery; mastery 2 unlocks the second form of its special), and the Genesis with the most
> wins are listed in TOP on the home screen. That career is written on Monad mainnet by our referee
> once the replay has decided the match (the ChogCareers contract), so anyone can read it and it
> follows the Genesis to its next holder. $CHOG runs through our contracts on Monad mainnet: players
> stake $CHOG on their own team and the winners are paid on the replayed result; a holder rents their
> Genesis out at a price a day while keeping it in their wallet, the renter's payment is earned by the
> second and the rental ends, with the rest paid back, when the holder sells the Genesis; and the
> second form of a Genesis can be bought now instead of at mastery 2. A tenth of every payment is
> burned. A holder can also lend their Genesis for free with a link, without moving it. The screens
> follow the language of the arena brawlers players know: a home screen with the hero on its pedestal
> and its mastery road, a sheet for each of the 1,969 heroes that says which trait gives which power,
> the moment a Genesis comes to life when its wallet connects, and a coach for the first match.

## 02 Show it

- **App link:** `https://chogarena.vercel.app` (pages on Vercel, services on Render behind `/api/`, see
  `docs/DEPLOY.md`). **Known on 16 October 2026:** MetaMask's dApp scan labels this new address a "drainer",
  as it did a blank test page the same evening; MetaMask then shows "Continue at your own risk" when
  connecting. The owner decides whether to report it to Blockaid.
- **Demo video:** **[not yet]** (script below).
- **Tech stack:** Unity 6 (WebGL, mobile first), Photon Quantum 3 (deterministic simulation,
  server-side input recording), Node.js services (wallet sign-in, Photon custom authentication and
  webhooks, a judge that replays every match with .NET), Solidity contracts on Monad mainnet (wager
  `0xc7524a808d2b00b43a4d3e2becb8500e409b41e3`, rental `0x90c26090aea05a3dc18d6ba8fd80afe8f933bf86`
  (version 2, since 17 October 2026; version 1 `0x7f1271d4...` is no longer used), careers
  `0x335bf7bc610645546e2b00de6a9fd0c4709d6111`, shop `0xb23a3c3492b9ba83d80c8abc9a5484d2885f058a`; Foundry
  tests), Reown AppKit and injected wallets, Blender (all 3D modelled by script).

## 03 For judges

- **GitHub repo:** `https://github.com/RedGnad/chogathon` (private today: **[not yet]** shared with
  the organisers or made public).
- **Judge access instructions (draft):**

> Open the app link on a phone (held sideways) or a desktop browser. Without a wallet you play a
> recruit (a grey Chog with 10% less health and no Genesis attack): pick SIEGE or COINS with the mode
> button, tap PLAY; the match starts by itself after 30 seconds, or at once with START NOW, and bots
> fill the empty places. If you hold a Genesis, CONNECT WALLET (or OPEN IN METAMASK on a phone): the
> game reads your Genesis on Monad, brings it to life and asks one signature so its matches are
> written in its career. Tap your hero for its sheet; HEROES shows all 1,969; RENT rents a Genesis a
> holder offers. `?heroes=1287,1376,16` after the address shows those heroes in 3D from close.

  (The owner decided on 16 October 2026: no loan link for the judges; judges who hold a Chog connect it.)

## 04 Why Chog matters (the "NFT gate")

> Take the NFT out and there is no hero: without a Genesis you are a grey recruit with 10% less
> health and no Genesis attack. With one, everything you see and do comes from its traits: the 3D
> character is built from them, the basic attack throws what its hat shows, the special comes from
> its eyes and its aura, the class from its outfit. Ownership is read on Monad mainnet at sign-in and
> checked again by the services when a match starts; a lent or rented Genesis plays under the same
> rules.

## 05 Under the hood (and pre-existing work to declare)

- Built on Photon's Quantum 3 Twin Stick Shooter sample (its map, recoloured in the Chog night, its
  controls, its bots and its menu framework, restyled), with its Cartoon FX assets still used for some
  effects.
- Fonts Lilita One, Nunito and Koulen (SIL Open Font License); Koulen as on chog.xyz.
- Sounds and music: 18 CC0 files (Kenney, OpenGameArt), each with its source in
  `game/Assets/Chog/Audio/LICENSES.md`. The pictograms of the screens are drawn in code.
- Written during the hackathon: the Genesis catalogue and trait rules, the 3D heroes and attack
  objects (Blender scripts), the services and the judge, the contracts, every screen (home, hero
  sheet, collection, end of match, scoreboard, settings, coach), the match look and its effects, the
  structures (towers and cores) and the two modes (SIEGE and COINS).

## 06 What's next

> The special attacks in 3D; seasons and a ladder per mode; more items for a Genesis in the shop;
> tournaments staked in $CHOG.

## The 3-minute video (script)

The owner's direction (17 October 2026): the features in action, fast and fun, no wallet or chain
windows on screen; filmed near the end with a Genesis the owner holds then, shown as #1287.

1. 0:00 to 0:15. The loading page, then the home screen: the hero in 3D on its pedestal, its mastery
   road. One line: your NFT is your hero.
2. 0:15 to 0:35. The wallet connects: the Genesis comes to life, its traits fly onto the hero.
3. 0:35 to 1:00. Its sheet: each trait and what it gives; HEROES, the 1,969 heroes; a few of them in 3D.
4. 1:00 to 2:10. A siege: the hat thrown as the basic attack, the special from the eyes, hits, a tower
   falling, the clock, the end screen with the hero cheering and the mastery it earned.
5. 2:10 to 2:40. What $CHOG does, on the screens: a Genesis rented out, the second form, the stakes
   of a match, the career that follows the NFT.
6. 2:40 to 3:00. A recruit beside a Genesis: what the NFT changes.
