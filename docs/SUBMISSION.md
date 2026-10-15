# Submission draft (Chogathon, form open until 23 October 2026, 15:00 UTC)

A draft written on 15 October 2026 for the owner to correct, in the order of the form
(`research/evidence/chogathon-site-facts-2026-10-15.json`). Every sentence describes what the game
does today; what is not done yet is marked **[not yet]** and must be removed or made true before
submitting. Character counts are for the 8,000-character fields.

## 01 Basics

- **Project name:** Chog Genesis (working title, see `MenuTitle.Title`)
- **Banner:** to make from a capture of the menu with a hero in 3D, or of the heroes sheet
  (`research/evidence/heroes-and-attack-objects-headless-2026-10-15.png`).

**Description**

> Chog Genesis is a real-time 3v3 arena for phones where your Chog Genesis NFT is your hero. Connect
> the wallet that holds it and the game builds your Chog in 3D from its own traits: its hat, its eyes,
> its mouth, its clothes and its skin, modelled by us for all 1,869 Genesis of the Origin base. The
> traits decide how it plays: its outfit gives its class (brute, ranger, controller), its eyes its
> special attack, its aura the effect of that attack, its rarity how the attack is delivered, and its
> hat what its basic attack throws: snowballs from a Russian Hat, dollar coins from a Dollar Cap, its
> own cap spinning like a frisbee from a word cap, a leaf of its spiky crown when it wears nothing.
> Matches are played on Photon Quantum's deterministic engine; after each match our services replay
> the recorded inputs to decide the winner themselves, so a result can be trusted with $CHOG at
> stake. A holder can also lend their Genesis with a link, without moving it, and **[not yet]** rent
> it out for $CHOG.

## 02 Show it

- **App link:** the public address (today a tunnel from the development Mac,
  `https://a6c8-92-88-170-244.ngrok-free.app`; **[not yet]** a host that stays up through judging).
- **Demo video:** **[not yet]** (script below).
- **Tech stack:** Unity 6 (WebGL, mobile first), Photon Quantum 3 (deterministic simulation,
  server-side input recording), Node.js services (wallet sign-in, Photon custom authentication and
  webhooks, a judge that replays every match with .NET), Solidity contracts on Monad (wager, rental,
  shop; Foundry tests), Reown AppKit and injected wallets, Blender (all 3D modelled by script).

## 03 For judges

- **GitHub repo:** `https://github.com/RedGnad/chogathon` (private today: **[not yet]** shared with
  the organisers or made public).
- **Judge access instructions (draft):**

> Open the app link on a phone or a desktop browser. Without a wallet you play a recruit (a grey Chog
> without traits): tap QUICK PLAY, then START; bots fill the empty places. To play a Genesis without
> owning one, open the loan link below: it lends you CHOG #____ for the judging week, without
> moving the NFT. If you hold a Genesis, CONNECT WALLET (or OPEN IN METAMASK on a phone), pick it,
> and play it. `?heroes=1` after the address shows a few heroes in 3D from close.

  The loan link needs a holder to create it (the owner's Genesis, held until the end of judging,
  see the chat of 15 October).

## 04 Why Chog matters (the "NFT gate")

> Take the NFT out and there is no hero: without a Genesis you are a grey recruit with 15% less
> health and no Genesis attack. With one, everything you see and do comes from its traits: the 3D
> character is built from them, the basic attack throws what its hat shows, the special comes from
> its eyes and its aura, the class from its outfit. Ownership is read on Monad mainnet at sign-in and
> checked again by the services when a match starts; a lent or rented Genesis plays under the same
> rules.

## 05 Under the hood (and pre-existing work to declare)

- Built on Photon's Quantum 3 Twin Stick Shooter sample (its map, its controls, its bots and its menu
  framework, restyled), with its Cartoon FX assets still used for some effects.
- Fonts Koulen and Convergence (SIL Open Font License), as on chog.xyz.
- Written during the hackathon: the Genesis catalogue and trait rules, the 3D heroes and attack
  objects (Blender scripts), the services and the judge, the contracts, the screens, the structures
  (towers and cores).

## 06 What's next

> Heads for the 100 Genesis on the Skull, Radioactive and 1:1 bases; the special attacks in 3D;
> stakes in $CHOG on mainnet after testnet; rental of Genesis for $CHOG; seasons and a ladder.

## The 3-minute video (script)

1. 0:00 to 0:20. The menu: a Genesis in 3D turning, its name, "THROWS ITS TISM CAP". One line: your
   NFT is your hero.
2. 0:20 to 0:50. The heroes sheet: six Genesis, their pictures beside their 3D heroes; how each trait
   is modelled.
3. 0:50 to 1:50. A match on a phone: the hat thrown as the basic attack, the special from the eyes, a
   tower falling, the end screen with the hero cheering.
4. 1:50 to 2:30. Trust: the match replayed by the services, the stake paid on chain (testnet).
5. 2:30 to 3:00. A recruit without NFT next to a Genesis: what the NFT changes.
