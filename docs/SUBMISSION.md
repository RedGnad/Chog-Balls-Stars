# Submission draft (Chogathon, form open until 23 October 2026, 15:00 UTC)

A draft written on 15 October 2026 for the owner to correct, in the order of the form
(`research/evidence/chogathon-site-facts-2026-10-15.json`). Every sentence describes what the game
does today; what is not done yet is marked **[not yet]** and must be removed or made true before
submitting. Character counts are for the 8,000-character fields.

## 01 Basics

- **Project name:** Chog Balls Stars (chosen by the owner on 22 October 2026, after Chog Arena, a name many projects share; Chog Genesis is the NFT collection)
- **Banner:** `research/evidence/banner-key-art-2026-10-23.jpg` (made on 23 October 2026 from the game's own 3D heroes, attack
  objects and towers in Blender: `tools/blender/make_key_art.py`, `tools/release/compose_banner.py`). Read on chogathon.chog.xyz
  that day (its page's own styles): project cards show the image at 16:9 and another view at 16:7,
  both `object-fit: cover`; so 1920 by 1080 with what matters inside the middle 16:7 band.

**Short description** (asked by the owner on 23 October 2026: every feature worth showing, concise and striking; the promise is the bet on a
brutal MOBA where the Chogs come alive, not "your Genesis is your hero", which every project says: a hook,
the features grouped by what they give the player, one call to action. Checked that day against the code and the live
services: rental up to 30 days, 10% burned; second form 100 $CHOG; 5% of lost stakes to the Genesis players; ladder
Bronze to Star; seasons are off live and are not claimed)

> **Bet $CHOG on a brutal 3v3 MOBA where the Chogs come alive.**
>
> Stake on your team, fight with your own Chog Genesis, get paid on a result nobody can fake. Three-minute matches,
> fast and brutal, on your phone, on Monad.
>
> **BET ON YOUR TEAM**
> - Stake $CHOG before the match; the winners share the losers' stakes. Results come from our server's replay of
>   Photon's own record of the inputs: a win nobody can fake. Part of every payment is burned.
>
> **THE CHOGS COME ALIVE**
> - All 1,969 Genesis in 3D, built from their own traits, the skulls, the Radioactives and the ten 1:1s included.
> - Traits are powers. The outfit gives the class (brute, ranger, controller), the eyes the special, the aura its
>   effect, the rarity how it is delivered, and what it wears or holds the attack: fireballs from a Dragon suit,
>   snowballs from a Russian Hat, spinning coins from a Dollar Cap.
> - Sieges against towers that fire lasers from their eyes and a beating heart to break, or Coin races. In the
>   browser, nothing to install; bots fill the empty seats.
>
> **EARN WITH YOUR GENESIS**
> - Rent it out by the day for $CHOG, up to 30 days, without it ever leaving your wallet. The rent is earned by the
>   second.
> - Play it in staked matches: the Genesis players share a cut of what the losers staked, win or lose.
> - Lend it to a friend for free with a link.
>
> **NO GENESIS? PLAY ANYWAY**
> - Rent a hero by the day, play a holder's link, or start as a Recruit and pick the Genesis you want.
>
> **PROGRESS THAT STAYS WITH THE NFT**
> - Each Genesis keeps a career on Monad (matches, wins, badges, mastery) that follows it to its next holder.
>   Mastery 2 unlocks its second form, or buy it now with $CHOG. The best Genesis are listed in TOP.
> - Every player climbs a ladder, Bronze to Star, on verified sieges.
>
> **Stake, brawl, win. Play now on your phone.**

**Description**

> Chog Balls Stars is a real-time 3v3 arena for phones where your Chog Genesis NFT is your hero. Connect
> the wallet that holds it and the game builds your Chog in 3D from its own traits: its hat, its eyes,
> its mouth, its clothes and its skin, modelled by us for all 1,969 Genesis, the skulls, the radioactive
> Chogs and the ten 1:1 included. The
> traits decide how it plays: its outfit gives its class (brute, ranger, controller), its eyes its
> special attack, its aura the effect of that attack, its rarity how the attack is delivered, and what
> it holds or wears what its basic attack throws. First the object in its hand: knives, hot coffee,
> candles, spinning coins or hearts. Then a full costume, which throws what it is: fireballs from a
> Dragon suit, sea water from a Shark Suit, bananas from a Banana suit, a little ringed planet from an
> Astro Suit. Then a rainbow if it pukes one. Then its hat: snowballs from a Russian Hat, dollar coins
> from a Dollar Cap, its own cap spinning like a frisbee from a word cap, a leaf of its spiky crown
> when it wears nothing.
> Two modes: SIEGE, where each team defends a tower with a Chog's head that shoots lasers from its eyes
> and a beating heart for a core, three minutes to bring the other core down or to do more damage; and
> COINS, a race for the coins of the arena. Matches are played on Photon Quantum's deterministic engine; after each match our services replay
> the recorded inputs to decide the winner themselves, so a result can be trusted with $CHOG at
> stake. Each Genesis keeps a public career that follows the NFT, not the wallet (matches, wins,
> badges, mastery; mastery 2 unlocks the second form of its special), and the Genesis with the most
> wins are listed in TOP on the home screen. Every signed-in player also climbs a ladder (Bronze,
> Silver, Gold, Star) on the matches the replay has decided, shown on the end screen and at home. That career is written on Monad mainnet by our referee
> once the replay has decided the match (the ChogCareers contract), so anyone can read it and it
> follows the Genesis to its next holder. $CHOG runs through our contracts on Monad mainnet: players
> stake $CHOG on their own team and the winners are paid on the replayed result, while the players
> who played a Genesis in the match share a twentieth of what the losers staked, staked or not; a holder rents their
> Genesis out at a price a day while keeping it in their wallet, the renter's payment is earned by the
> second and the rental ends, with the rest paid back, when the holder sells the Genesis; and the
> second form of a Genesis can be bought now instead of at mastery 2. A tenth of every payment is
> burned. The home screen shows the player's $CHOG and what it is worth in dollars. A holder can also lend their Genesis for free with a link, without moving it. The screens
> follow the language of the arena brawlers players know: a home screen with the hero on its pedestal
> and its mastery road, a sheet for each of the 1,969 heroes that says which trait gives which power,
> the moment a Genesis comes to life when its wallet connects, and a coach for the first match.

## 02 Show it

- **App link:** `https://chogtanks.vercel.app/chog-balls-stars/` (pages on Vercel, services on Render behind
  `/api/`, see `docs/DEPLOY.md`). The first address, `https://chogarena.vercel.app`, sends players there:
  MetaMask's dApp scan labels that new address a "drainer" (a false positive it also gave a blank test page),
  while it judges the owner's earlier Chog game, chogtanks.vercel.app, clean (22 October 2026).
- **Demo video:** **[not yet]** (script below).
- **Tech stack:** Unity 6 (WebGL, mobile first), Photon Quantum 3 (deterministic simulation,
  server-side input recording), Node.js services (wallet sign-in, Photon custom authentication and
  webhooks, a judge that replays every match with .NET), Solidity contracts on Monad mainnet (wager
  `0xa9d1faa2e09753f5419d768cae05b1e2596b3e4f` (version 3, since 22 October 2026: one signature to stake, the services
  relay the permit; half of what was burned goes to the players who played a Genesis in the match; versions 2
  `0xc2c1e282...` and 1 `0xc7524a80...` are no longer used), rental `0x90c26090aea05a3dc18d6ba8fd80afe8f933bf86`
  (version 2, since 17 October 2026; version 1 `0x7f1271d4...` is no longer used), careers
  `0x335bf7bc610645546e2b00de6a9fd0c4709d6111`, shop `0xb23a3c3492b9ba83d80c8abc9a5484d2885f058a`; Foundry
  tests), Reown AppKit and injected wallets, Blender (all 3D modelled by script).

## 03 For judges

- **GitHub repo:** `https://github.com/RedGnad/Chog-Balls-Stars` (renamed by the owner on 22 October 2026; private today: **[not yet]** shared with
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

- **Check it in 30 seconds (draft, every line run on 17 October 2026):**

> The career of a Genesis lives on Monad mainnet, written by our referee after the replay decides the
> match: https://monadscan.com/address/0x335bf7bc610645546e2b00de6a9fd0c4709d6111 (one such write:
> https://monadscan.com/tx/0x04a6c37fbad4cabe6910d3257882590b8cc4ba10420dee74da07b8e1b0a4e30c). Read one
> yourself: `cast call 0x335bf7bc610645546e2b00de6a9fd0c4709d6111
> "careerOf(uint256)((uint32,uint32,uint32,uint32,uint32,uint16,uint16,uint64))" 1795 --rpc-url
> https://rpc.monad.xyz` answers matches, wins, draws, rented, as a guest, streak, best streak, last
> played (for #1287 that day: 3, 2, 0, 1, 1, 1, 1, 1792230364); the game reads the same through
> https://chogtanks.vercel.app/chog-balls-stars/api/career?tokens=1795. The rental, the wager and the shop are the
> contracts listed in the tech stack above.

## 04 Why Chog matters (the "NFT gate")

> Take the NFT out and there is no hero: without a Genesis you are a grey recruit with 10% less
> health and no Genesis attack. With one, everything you see and do comes from its traits: the 3D
> character is built from them, the basic attack throws what it holds or wears, the special comes from
> its eyes and its aura, the class from its outfit. Ownership is read on Monad mainnet at sign-in and
> checked again by the services when a match starts; a lent or rented Genesis plays under the same
> rules.

## 05 Under the hood (and pre-existing work to declare)

- Built on Photon's Quantum 3 Twin Stick Shooter sample (its map, recoloured in the Chog night, its
  controls, its bots and its menu framework, restyled), with its Cartoon FX assets still used for some
  effects. **Open licence point (counted 17 October 2026):** 15 files of Jean Moreno's Cartoon FX are
  still referenced by ten of the sample's effect prefabs (MeleeAttackVFX, ArrowDamageZoneVFX,
  DamageZoneVFX, ArrowHitVFX, EnergyImpact, EnergyPulse, KnightBasicAttack, StepsSmoke, Character_Base,
  CollectibleCoin), several from the legacy Cartoon FX packs, not only the free Remaster pack the
  sample's page credits; and Photon's sample licence grants its non-code "Assets" for internal
  development only (`research/research_notes/Audit architecture Photon MOBA Genesis/twin_stick_sample_reuse_licences.md`,
  KQ2 and KQ5). Before a public repository or a release: replace those effects with our own, or
  hold a licence for each pack used. **Since 17 October, evening, the fights use more of it:** 23
  Cartoon FX Remaster prefabs (the CFXR2 and CFXR3 families found in the project, copied under our names
  into `game/Assets/Chog/Effects/Resources/ChogFx`, with the textures, materials and shaders they
  reference) play the explosions, fire, ice, electricity, sparks, smoke and splashes of a fight
  (`game/README.md`, "The effects of a fight"). The owner said that day that the licence is not a problem
  for a non-profit MVP; which Cartoon FX pack each prefab comes from was not checked.
- Fonts Lilita One, Nunito and Koulen (SIL Open Font License); Koulen as on chog.xyz.
- Sounds and music: 18 CC0 files (Kenney, OpenGameArt), each with its source in
  `game/Assets/Chog/Audio/LICENSES.md`. The pictograms of the screens are drawn in code.
- Written during the hackathon: the Genesis catalogue and trait rules, the 3D heroes and attack
  objects (Blender scripts), the services and the judge, the contracts, every screen (home, hero
  sheet, collection, end of match, scoreboard, settings, coach), the match look and its effects, the
  structures (towers and cores) and the two modes (SIEGE and COINS).

## 06 What's next

> The special attacks in 3D; the ladder's seasons and matchmaking by rating in the game (both
> already kept by the services); more items for a Genesis in the shop; tournaments staked in $CHOG.

## 07 Go to market (asked by the owner on 23 October 2026: simple, highest return first, mobile, events)

> **1. Start with the people who already own a hero.** 1,969 Chog Genesis holders: each one connects
> and plays their own NFT, in 3D, with its traits as powers. We reach them where they already are,
> the Chog and Monad communities on X and Discord, with a short clip of their own Genesis fighting.
>
> **2. Mobile, zero install.** A link opens the game in the phone's browser, held sideways; it can be
> added to the home screen and opens full screen. Every post, reply or DM is a play button.
>
> **3. Loops already in the game.** A holder lends their Genesis with a free link (the friend plays
> it, the career stays on the NFT) or rents it out for $CHOG. A player without a Genesis plays a
> Recruit and sees what each Genesis would give them.
>
> **4. Events, the cheapest players to win (next).** Weekly community nights, holders against
> holders in 3v3, with $CHOG pots on the stake contract and every result replayed by our judge;
> hosted with Chog and Monad community accounts. The player ladder, Bronze to Star, brings them back.
>
> **5. Playing adds value to the NFT.** Careers, badges and mastery are written on Monad and follow
> the Genesis to its next holder.
>
> **6. Cheap to run.** Free tiers today (Photon, Vercel, Render); paid ones only when players come.

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
