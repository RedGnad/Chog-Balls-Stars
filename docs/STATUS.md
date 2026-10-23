# État courant et décisions

**23 October 2026, 00:25 UTC (lead: audit of the money path, stake and rental; fixes deployed).**
- **Audit** (read-only, then each finding checked in the code before any change): nothing lets anyone steal, forge or double-pay, every stake stays refundable (`refundExpired`), the signatures bind chain, contract and match (no replay between v2 and v3), the permit and the stake succeed or fail together, the payouts lose nothing (fuzz tests), the rental pays by the second and stops on a sale. Found and fixed:
  - **A winner could lose the gain:** the contract gives every stake back to anyone an hour after the first stake while the match is still open, whatever the referee decided, and only a player's CLAIM brought the decision. The services now bring it themselves as soon as the match is judged (`wager.bring`, `settlementData`: settle with the Genesis players, or voidMatch), which also spares the winner the claim.
  - A signed stake someone else sent first now counts as made (the relayed copy reverts); relayed stakes are limited to one a wallet every 8 s and six at once (the sender's queue is shared).
  - A wallet that cannot sign the permit, or whose signed stake the chain refuses, stakes with its two transactions instead.
  - A staked match played with test options is called off, as it does not rank.
  - "Your Genesis earns" no longer shows when a side had no stake (the contract then pays no share).
  - Rental: a holder's claim no longer ends in a false failure while a rental runs (seen paid once less is waiting); END NOW is reached by a wallet that also rents one out; a choice of days that would pass 30 in all, counting the days paid, is off (it reverted on chain).
  - Not changed: a first ticket can fix an absurd amount or a stake on the wrong team forces a void (no money lost); the start of a match is dated by the arrival of Photon's first webhook; the owner's admin wallet may stake while trying any Genesis.
- **Deployed:** build `20261023T001503061Z-bc844e9ccbc340ffb58f59d4732cccd8`, stamp unchanged.
- **Verified:** 136 service tests (the settle and void calldata, the relay throttle); 95 contract tests; screen typecheck 0 errors; judge fixtures agree. Not yet seen: a live staked match paid by the services.

**23 October 2026, 00:12 UTC (lead: the 1:1 closer to their art; deployed).**
- **The owner:** the 1:1 "not ambitious or accurate enough yet". Compared with their art (`try-ones-baseline.jpg`, `try-ones-baseline2.jpg`).
- **Deployed:** build `20261023T000812982Z-78b0bd0674594ef0bbc89260d300f076`, stamp unchanged. Devil #1376: a small smile with one white fang (a gold-tooth grin until now), larger brighter horns with a white light along each. Skull #392 and zombie #494: big black sockets lit by an orange ember or a green light, no coloured ring. Zombie: pale fur and skin, the purple left to its shirt. Angel #1159: seven larger white feathers a wing, lit from within, a thicker glowing halo. Warrior #1290: a gold wing sweeping up and back from each side of the helm. Manga #1006: a warmer grey (the game's purple shading still tints it). Sheets `try-ones-try1.jpg`, `try-ones-try2.jpg`.
- **Verified:** export 1,970 Genesis, 0 failed (each under 8,000 triangles); judge fixtures agree. Not seen in a match.

**23 October 2026, 23:52 UTC (lead: thrown objects point where they go, the spear is a real spear; deployed).**
- **The owner:** "some thrown objects look backwards: the fireball, the water, the spear? And the spear is small, is that normal?" In a match a thrown object's view follows the simulation's rotation, which is the way the attack goes: the spear already pointed ahead. What looked backwards: the brute's blow whirled every object flat round the vertical, so the dragon's fireball and the shark's water showed their tails half the time; and the viewer `?impacts=` left its attacks pointing up the screen. The spear was small because the game brings each object to the same largest size: for a spear its length, so a 0.028 shaft became a line.
- **Deployed:** build `20261022T234617682Z-58255847c2d744d1820872ef302b185a`, stamp unchanged. A pointed object (`AttackStyle.Point`) strikes point first; a long object (three times longer than wide) is drawn 1.6 times larger (`LongObjectRatio`, `LongObjectGrowth`); the spear is a chunky toy spear (shaft 0.084, a leaf blade 0.23 wide, a butt spike), the arrow thicker; the viewer turns its attacks the way they fly and `&t=-0.15` stops it before the impact.
- **Verified:** `?impacts=21,27,16,215,773,1299&t=-0.18`: the spear flies tip first and reads, the fireball and the water strike head first, the flame and the drops behind; export 1,970 Genesis, 0 failed; judge fixtures agree.

**23 October 2026, 23:25 UTC (lead: the shark's upper teeth and the dragon's upper fangs read from below too; banner v3; deployed).**
- **The owner, on banner v2:** "I see neither the upper teeth of the shark nor of the dragon, are we sure the model is right?" From the hero stage and the game camera they showed (`try-teeth-out.jpg`); from the banner's camera near the ground the brow of the hood stood in front of them, laid flat on the brow that faces up. Now the shark's upper teeth stand in front of the rim and off the face, facing between the front and the way out of the head; the dragon's upper fangs are longer and stand out of the head (`UPPER_FANG_OUT`).
- **Deployed:** build `20261022T232109509Z-0e43abf44dc042b7a0d840525f25186f`, stamp unchanged. Banner `research/evidence/banner-key-art-v3-2026-10-23.jpg`: "BET $CHOG. WIN THE BRAWL.", the two towers large and sharp with their beams on the clash, the camera at the height of the faces; a whole costume keeps its head in line with its body in a pose (the head turns inside the opening of the body's volume).
- **Verified:** export 1,970 Genesis, 0 failed; judge fixtures agree.

**23 October 2026, 01:15 local, 23:15 UTC (lead: the dragon's head round, the shark a whole mascot; deployed).**
- **The owner, on the banner:** the dragon's lower teeth do not follow the face, the dragon suit "makes odd angles on the head", and the shark suit "is not a big worked costume like the others but the plain suit the banana and the shark had before".
- **Deployed:** build `20261022T230719300Z-e0aa439b3e164638af306cdd4e60b7f4`, stamp unchanged (`7e77d8cf4a7c1e99`). Dragon (`MASCOT`): the jaw standing out over the face is gone (the pictures draw none; it made a flat ledge across the brow); two more rings round the top of the skull; the eyes and the nostrils sit on the dome (`DOME_EYES`, `DOME_NOSTRILS`); the lower fangs nearer the chin, lying along the face (`LOWER_FANG`). Shark: a whole mascot like the dragon (`shape: "shark"`, `add_shark`): the round volume, a white belly, nine white teeth along the top of the mouth and nine along the bottom, each as long as it is wide and pointing at the middle of the face (the lower ones stand in front of the chin, facing forward), a dorsal fin, side fins, the tail and its fin, dark gills, small dark eyes. Sheets `try-dragon-dome.jpg` and `try-shark-mascot11.jpg` (art, live, try, three views).
- **Verified:** budget: none over 8,000 (largest #1644 7,990); export 1,970 Genesis, 0 failed; a local siege without error; judge fixtures agree.

**22 October 2026, 22:30 UTC (lead: every live match was void; the room now closes after its record holds the end; the judge stays running; deployed).**
- **Found:** no live match had been judged with a winner since 21 October evening, when the result screen began closing the Photon room at its first frame (f965bc7, never verified live). Photon's record stops a few ticks before the room closes: on a public guest siege the client verified GAME OVER at frame 6515 and the record ended at tick 6509; the judge needs the end tick (a fixture cut at its end tick still names the winner, one tick earlier does not). So every match was void, "the match did not reach its end": no rank points, no mastery, every stake given back, and the rank animation of the result screen never had a verdict to show (the owner: "the XP bar animation is never seen"). The owner's two staked matches of 21:43 and 21:51 were two of them.
- **Deployed:** build `20261022T222504708Z-8af0e2a9a0364cc4997e4c667aec72fc`, stamp `7e77d8cf4a7c1e99`. The result screen closes the room 2.5 s after it shows (`CloseAfterSeconds`, 75 ticks of margin), and a player who leaves at once waits for that close. The judge program stays running (`Verifier --serve`, `residentReplay`), warmed on a recorded match at start: 0.2 to 1.3 s a match on a laptop instead of 6.4 s, the same reports field for field on the five recorded matches; on Render the verdict took 145 s. The result screen's stake card says the result the game showed at once ("YOU WON +90 $CHOG"), marked "confirming the replay, about 2 min (0:42)" with the time moving, and a match the services lost says when its stake comes back. Services: `/api/careers/recent` gives each verdict's ticks and judging time, `/api/match/record?gameId=` the record Photon sent (`tools/quantum/fetch_live_record.py`); the v3 stake (permit and relay) is in, inert until the v3 contract is set.
- **Verified:** 135 service tests (resident judge: two; the record route); 176 view tests not rerun for this change (screens only); screen typecheck 0 errors; judge fixtures agree with the assembled judge, which also answers in resident mode. Three public guest sieges played muted in headless Chrome against the previous build: all void, with the tick evidence above.
- **Verified live (22:36 UTC):** a public guest siege on this build, played to its end in headless Chrome, was judged with a winner (team 1, 0 to 300; end at tick 5522, record to 5590), in 14.4 s on Render (145 s before), 17 s after the match ended. Not yet seen: a signed-in player's rank animation after such a verdict.
- **Banner:** `research/evidence/banner-key-art-2026-10-23.jpg` (1920 by 1080, the middle 16:7 band holds the title and the heroes): `tools/blender/make_key_art.py` renders each hero of `content/key-art/banner.json` from the game's library, posed with the animator's numbers, each attack object and each tower, through one camera; `tools/release/compose_banner.py` lays them with the team glows, trails, beams, the burst and the title in Lilita One. Short description and go-to-market text in `docs/SUBMISSION.md`.

**22 October 2026, 21:35 UTC (lead: matches survive a deploy, a stake never stays stuck, admin back; deployed).**
- **Found:** Render's free instance has no disk: every deploy (seven that evening) replaced the container and lost the match records, a staked match among them (the owner's 100 $CHOG): `/api/wager/result` answered 404 and the menu showed "asking the game server" forever. The keep-alive ping only stops the instance from sleeping; it does not keep files across a new container.
- **Deployed:** build `20261022T212556292Z-cfccaacd3e8943db8ac2f9ca45775da8`, stamp `7e77d8cf4a7c1e99`. Services: the match records (all but the replay) are mirrored to the Upstash Redis of the ranks (`mirroredMatchStore`, `rank-store.js` `set`), read from it when the disk lacks them; an unknown match answers its on-chain stakes and `refundAfterSeconds` (`lost: true`) instead of 404. Game: a `Lost` outcome; `GenesisWager.RefundExpired` calls the contract's `refundExpired(bytes32)` (0xcc3e049b) once the hour has passed. The menu's stake card says one short line ("Your 100 $CHOG comes back at 23:45", "You won +90 $CHOG"), one action at most (CLAIM, GET IT BACK), a close (X) that hides it until it changes, and nothing before the services answered. Admin testing on again (`PlayerWallet.AdminTesting`).
- **Verified:** 132 service tests (two for the mirror); 176 view tests; a local siege with the new services without error; judge fixtures agree.
- **Not verified:** the mirror against the real Upstash on Render, and the GET IT BACK path on mainnet (the owner's stake is the first case).

**22 October 2026, 21:05 UTC (lead: hoods without the hole; deployed).**
- **Deployed:** build `20261022T210103909Z-cedef602b2444728914d767d8c6f7f3d`, stamp `7e77d8cf4a7c1e99`. The owner: "the right side of the hoods has a hole". Behind the rim, on one side only, the cheek and the eye showed through the hood's wall (#376 shark, #537 gangster, turned around at 1000 points). `HOOD` SIZE 1.14, 1.24, 1.26 (was 1.12, 1.20, 1.22) and 24 points round the head (18); the shark's hood the same size. Sheet `try-hoods.jpg`; both sides checked on the turnaround.
- **Verified:** export 1,970 Genesis, 0 failed, none over 8,000; 176 view tests; a local siege with the two hooded bots without error; judge fixtures agree.

**22 October 2026, 20:55 UTC (lead: the player profile; deployed).**
- **Deployed:** build `20261022T204810762Z-0a2072c290e5405fa52f1de5eda02279`, stamp `7e77d8cf4a7c1e99`. The chip at the top left opens a PROFILE (`ChogProfile`): name and CHANGE NAME (the sample's window), the player rank with its tier, points and bar, the verified sieges played and won with the win rate and the last season's tier, the wallet's Genesis (up to eight) with their mastery and matches, the badges they earned. Every number from the services (PlayerRanks, GenesisCareers). Modelled on the profiles of Clash Royale and Brawl Stars (wikis and trackers, not official documents).
- **Verified:** 176 view tests; the profile with a holder test wallet (not signed in); a local siege without error; judge fixtures agree.
- **Not verified:** the profile of a signed-in wallet with a rank (the owner's).

**22 October 2026, 20:45 UTC (lead: smirk, coin, Angry eyes, ushanka colour, home card, stake notice, admin off; deployed).**
- **Deployed:** build `20261022T203149800Z-026af36ab42042d2b5bd0e185d888873`, stamp `7e77d8cf4a7c1e99` (unchanged since 20:20). Smirk: both eyes tilted the same way, as the seven Smirk pictures (`SAME_TILT`); the coin held in the teeth upright and turned to the stage camera (`COIN_TURN` 34, -6), `try-coin.jpg`; Angry on Side Left a black half disc under a flat lid with a large glint (#410, #29, #276; `try-angry.jpg`); the ushanka one fur, crown as the flap, squarer (`try-chapka3.jpg`). Home: the card's foot at 0.305, clear of SIEGE and PLAY when hovered. The stake notice of the menu a gold-framed card with the $CHOG coin in the middle of the top bar, in the theme's fonts. Admin testing off (`PlayerWallet.AdminTesting`), at the owner's request; the services still accept it.
- **Verified:** export 1,970 Genesis, 0 failed, none over 8,000; 176 view tests; a local siege without error; the home with a holder test wallet (card clear of the buttons); judge fixtures agree.

**22 October 2026, 20:20 UTC (lead: tower and core health +50%, the 1:1 super attack as one wider strike; NEW SIMULATION STAMP; deployed).**
- **Deployed:** build `20261022T200608935Z-abcc80756c2e4f10bd34a0a08c8c0b7a`, stamp `7e77d8cf4a7c1e99` (was `b8d600c2135da877`). `Structures.TowerHealth` 12,000 and `CoreHealth` 18,000 (8,000 and 12,000; the owner: matches could be dispatched too fast). The inversion delivery (1:1) is one strike 1.6 times as wide at 125% (`GenesisSignatureImporter`, `AreaScale`, `Sweep.Scaled`; the puddle's zone too) in place of three copies 120 degrees apart; its golden cast burst larger (`ChogFxRecipes.ForDelivery`, stars 1.15, 18 rays, 2 rings). Card text and `web/hero/heroes.json` regenerated.
- **Verified:** 32 headless simulation tests; the local build connected to Photon with the new version and played a full siege with five 1:1 bots, recorded as the new judge fixture `tests/fixtures/replays/20261022T201430-siege-health-winner1-tick3521.json` (a core fell at 117 s with five 1:1 bots charged from the start); the judge agrees on all four fixtures.
- **Not verified:** the wider strike seen on screen by the owner; whether +50% is enough with humans.

**22 October 2026, 20:05 UTC (lead: Side Left happy eyes, the owner's home as a holder's; deployed).**
- **Deployed:** build `20261022T195628615Z-ad2f6303e0a744878b5103c483658aef`, stamp unchanged. Happy on Side Left: two equal arches (a long stroke beside a short arch read as deformed, #26; `try-happy-left.jpg`). The Genesis the owner picks with ADMIN shows on the home as in a holder's wallet (YOUR GENESIS, in the list), no admin wording.
- **Verified:** export 1,970 Genesis, 0 failed; 176 view tests; judge fixtures agree.
- **Waiting for the owner's go:** tower and core health +50% (8,000 and 12,000 now) and the 1:1 super attack as one larger attack (area x1.6, golden effect, recharge +30%) in place of three directions, together (a new simulation stamp); a PROFILE panel on the player button.

**22 October 2026, 19:55 UTC (lead: Spartan helmet, ushanka composition, closed crying eyes, Vertical eyes, rainbow throw, preparation screen; deployed).**
- **Deployed:** build `20261022T194800483Z-bdfb88c8a6194ade82545b3357b77344`, stamp unchanged. Each 3D change on a sheet art | live | try (`research/evidence/try-*.jpg`): the Spartan helmet as the art's Corinthian helmet (close shell, broad nose guard, cheek guards to the jaw, back to the nape, thin bristling crest; the spikes go through it, `THROUGH`), `try-spartan.jpg`; the ushanka a tall fur crown with its front flap turned up against it and the ear flaps (`try-chapka2.jpg`); Closed Eye Crying as lines that sag (◡), traced on #806 and #24, tears ending on the jaw (`try-closed-crying.jpg`); Vertical eyes a long flat almond with a capsule pupil (`try-vertical.jpg`); Retro Glass irises inside the lenses (`try-retro-smaller.jpg`); the Bob Hair band clear of the curls (`try-bob-band.jpg`); the rainbow thrown as a flat arch of six bands with two clouds, spinning, a trail of rainbow stars. Preparation screen: the Genesis card shorter and higher, the countdown under it, slot texts fit their slots, the stake panel framed in gold with the $CHOG coin and a beating gold button. Attack and trait lines without "its" ("Shoots SNOWBALLS, fast, from a distance. From RUSSIAN HAT."; "Eyes: BTC Glass.").
- **Verified:** export 1,970 Genesis, 0 failed, none over 8,000 (`--budget`); 176 view tests; a local siege without error; the visitor's preparation screen (countdown visible); judge fixtures agree.
- **Not verified:** the Genesis card and the stake panel on the preparation screen (a signed-in local session cannot pass Photon, whose authentication asks the public services).

**22 October 2026, 19:20 UTC (lead: the Russian Hat as an ushanka; deployed).**
- **Deployed:** build `20261022T191353266Z-d53874b2800c45b2aba3c9ba4c2e545b`, stamp unchanged. The owner: "an ushanka has NO crown, it has a raised front". `build_ushanka` is now a cap that fits the head (`dome`, CAP_RIM), the front flap of fur turned up over the forehead with a furry top edge (FRONT, a `slab` round the front), the ear flaps turned to show their face; colours read on #39 and #91. Sheet `research/evidence/try-chapka.jpg` (art | live | try). Includes the 19:15 UTC changes (home kit panel, siege crates, Retro Glass eyes, smirk, dragon belly).
- **Verified:** export 1,970 Genesis, 0 failed (the spikes under the rim at the back clear the new cap); turnaround of #91; judge fixtures agree.

**22 October 2026, 19:15 UTC (lead: home kit panel, siege crates, eyes and hats against live; deploying).**
- **Build** `20261022T190222981Z-64ab90f7eb4a4e8a8d514aafc360970e`, stamp unchanged. The home's right panel shows the played Genesis' kit (class, what its basic attack throws, its special) in place of the mastery road (the owner: "the mastery road window in the landing, I don't know what to do with it"); an ADMIN pick reads ADMIN TEST. The siege's crates take the candy skin again: its rebuilt map names them "Crate <cell>" (`ChogArenaCoverSkin.IsCrate`). 3D, each judged against the live hero (`tools/blender/try_against_live.py`): Retro Glass eyes, smirk, Russian Hat, dragon belly. Spartan side spikes not done.
- **Verified:** export 1,970 Genesis, 0 failed; 176 view tests; a local siege shows textured pink crates; the home with a holder test wallet shows the kit panel.

**22 October 2026, 18:45 UTC (lead: result screen, ADMIN pick, siege skins; deployed).**
- **Deployed:** build `20261022T183533030Z-7c491239504d44999618ef1b9c7ebbda`, stamp unchanged. The result screen shows every player, a Genesis too, the player rank card and its bar where the points fly (no mastery road there; the owner: an unexplained window that hid the rank moment). A cheering hero gets headroom (its arms were cut at the top). ADMIN on a hero sheet picks the Genesis for the home and the next match without starting one; on the picked one it reads BACK TO RECRUIT; the owner's own rank counts in such a match (`ranks.js`), never the Genesis' career or a wager share. Arena skins (floor, walls, crates) move into the scene they dress (`ChogArenaSkin.KeepIn`): made while the next arena loaded, they went into the old scene and were unloaded with it (the owner saw a siege without the pink crates).
- **Verified:** 176 view tests; 130 service tests (admin rank allowed, stand-in still refused); `?rewardpreview=1` shows the +25 PTS moment over the rank card, then the hero cheering in full; judge fixtures agree.
- **Not verified:** a siege after another match with the skins (not reproduced locally); the ADMIN pick and the rank moment in a signed public match.

**22 October 2026, 18:35 UTC (lead: the day's eye and jaw work taken back; deployed).**
- **Why:** the owner found many heroes worse than before: eyes deformed (the strict audit's two different eyes on Side Left, batches 4 to 8), closed crying eyes upside down, a smirk that is no smirk, the Skull and Radioactive jaw worse while the vertical strokes they had pointed at stayed, the crown of the headbands turned into a dome with flaps.
- **Deployed:** build `20261022T182550395Z-b45bf7b9d3184018a9bf2ae7f57ebe2f`, stamp unchanged. `chog_trait_faces.py` back to batch 1 (70ab510) with the morning's smirk: kept the Retro Glass frown, the coin raised in the grin, centred mouths, beams leaving the middle of the eye. `make_chog_traits.py`: batches 2 to 4 reversed, batch 1's eye sizes (DARK_EYE, HAPPY, ROUND) back, mouth centring and beam start kept, no lower jaw volume. `chog_trait_bases.py`: the morning's teeth rows and Radioactive jaw line, no mouth band, no cheekbones (the vertical strokes). Headbands: FULL_CROWN again, band kept on the brow. Kept: ribs only on bare skin, Radioactive bands, every clothes, armour, costume, 1:1 and attack change. Also: the signature asked of any connected wallet without a session, sessions of 30 days, the BASIC row titled with what it throws.
- **Verified:** export 1,970 Genesis, 0 failed; `?heroes=17,27,2,7,45,43` and `12,121,42,91,26,44` on the build (sound shapes, #12's crown back); judge fixtures agree.
- **Open, one at a time against the live model:** the Retro Glass iris circle (the art: a black eye), a real smirk, the Russian hat, the Spartan side spikes, the dragon belly colours, the hair through the red headband of #43.

**22 October 2026, 17:55 UTC (lead: the public address moves to chogtanks; deployed).**
- **Done:** the owner set `PUBLIC_ORIGIN` to `https://chogtanks.vercel.app` on Render and allowed the domain at Reown; the sign-in message now reads "chogtanks.vercel.app wants you to sign in". `chogarena.vercel.app` sends a page to `https://chogtanks.vercel.app/chog-balls-stars/` (seen in the browser: the game loads there, no error). SUBMISSION, HANDOFF, DEPLOY and render.yaml name the new address.
- **Not verified:** a wallet connection through Reown's QR on the new address; MetaMask's verdict after more traffic (NONE at 17:40 UTC).

**22 October 2026, 17:30 UTC (lead: owner's ADMIN button, golden armor, last eye and clothes batches, chogtanks front; deployed).**
- **Deployed:** build `20261022T171846464Z-8c642506578d468e988c5c9cac6976fb`, stamp unchanged (`b8d600c2135da877`). Every hero sheet shows ADMIN: PLAY IT to the owner's wallet (`0x981F...20e0`, `PlayerWallet.IsAdmin`); the services admit that signed-in wallet with any Genesis as a stand-in (`photon.js` `isAdmin`: no career, no rank, no wager share). Golden Armor with domed pauldrons and plated chest (`clothes-golden-armor-2026-10-22.jpg`); clothes batch 4 and its fix (`clothes-batch4-2026-10-22.jpg`, `clothes-batch4-fix-2026-10-22.jpg`: army camouflage and flag, Gojo button, Guko white emblem, BBC and ZERO THOTS lower and readable); eyes: XD chevrons off the cheek, batch 6 (taller VR visors), Left lasers from the middle of each eye, Chog Viper frame (`heroes-eyes-batch6/7/8`). Guide and coach texts say the basic attack comes from what the Genesis holds or wears. `tools/dev/heavy_lock.sh` releases once and stops its job when interrupted (a Mac restart at 14:49 UTC had left the lock taken).
- **Address:** MetaMask's scan answers BLOCK ("wallet drainer") for `chogarena.vercel.app` and NONE for `chogtanks.vercel.app`, also with a path. The chogtanks project now serves the game at `https://chogtanks.vercel.app/chog-balls-stars/` (`tools/release/chogtanks_front.sh`: rewrites to this project, everything else to the chogtanks deployment of 10 October, its files compared byte for byte). Still to do for the switch: the owner sets `PUBLIC_ORIGIN` to `https://chogtanks.vercel.app` on Render (then Manual Deploy) and adds the domain at Reown; then the old address sends players there.
- **Verified:** export 1,970 Genesis, 0 failed; 176 view tests; 130 service tests (one for the admin right); a local siege with bots without exception; judge fixtures agree.
- **Not verified:** the 1:1 super attack's three directions on screen (the scripted film of a bots match could not catch it; the owner tests it with the ADMIN button); the ADMIN button in a signed match.

**22 October 2026, 14:20 UTC (lead: eyes batches 3 and 4, clothes batch 2; deployed).**
- **Deployed:** build `20261022T141558736Z-c653e0d33f9145f4996ad9407543ce1c`, stamp unchanged. Eyes batch 3 (`heroes-eyes-batch3-2026-10-22.jpg`: visor lenses with round ends, Round Eye tall ovals, Closed Eye Crying drooping, Tired) and batch 4 (`heroes-eyes-batch4-2026-10-22.jpg`: on Side Left the near and far eyes are two drawings, Side Eye, Base, Happy, Vertical, Flat, smoother goggles); clothes batch 2 (`clothes-batch2-2026-10-22.jpg`: hibiscus, Chinese toggles, yukata V, CWO and MILF words, Wizard ties).
- **Verified:** export 1,970 Genesis, 0 failed; 175 view tests; `?heroes=17,121,212,56,30,181` (the Skull #121's polo shows no ribs); a local siege without exception; judge fixtures agree.

**22 October 2026, 14:00 UTC (lead: Radioactive bands, Skull and Radioactive jaws, bones only on bare skin, clothes batch 1; deployed).**
- **Deployed:** build `20261022T134637493Z-ce78135128e34c6e8cf3f7409383a757`, stamp unchanged (`b8d600c2135da877`). Library format 6: a Radioactive skin runs through the bands of colour the art draws (the Multi Colored green, yellow, orange, pink, white from chest to crown; `ChogPartsLibrary.BandAt`), format 5 still read for the structures. Faces lot 9 (`stream/heroes-faces` 4f28646, `heroes-radioactive-skins`, `heroes-jaws`, `heroes-bones-2026-10-22.jpg`): a lower jaw with its teeth standing out on Skulls, a jaw band on Radioactives, ribs only where the art shows bare skin. Clothes batch 1 (`stream/heroes-hats` ad7ac71, `clothes-batch1-2026-10-22.jpg`): the Jesus scarf, Inmate label, Squid Game triangle, tuxedo V, Kings Robe fur.
- **Verified:** export 1,970 Genesis, 0 failed; 175 view tests (one asserts the bands: green at the chest, pink at the forehead); `?heroes=140,1187,343,54,1079,47` shows the bands; a local siege with Radioactive and 1:1 bots without exception; judge fixtures agree.

**22 October 2026, 13:05 UTC (lead: the ten 1:1 reworked from their art; deployed).**
- **Deployed:** build `20261022T125816725Z-50944746180f4b2985600b5e4854c47b`, stamp unchanged (`b8d600c2135da877`). The ten 1:1 brought to the detail of their 1000 px art (`stream/heroes-hats` 5365441 and eed08f4, `heroes-one-of-one-2026-10-22.jpg`, `heroes-one-of-one-pass2-2026-10-22.jpg`): Flame with a head of fire tongues, glowing eyes, collar, clasp and cape; the flaming skull's flames; The Scream's hands, eyes and mouth; the zombie's brain, blood and slime; Pepe's lips and ruff; the manga page's brows; the angel's halo and wings; the warrior's gem, paint and fur collar; the gold Chog's metal streaks; the devil's horns, wings and tail. What each throws is unchanged.
- **Verified:** export 1,970 Genesis, 0 failed, none over 8,000 triangles; 175 view tests; `?heroes=53,434,494,973,1159,1290`; a local siege with 1:1 bots without exception; judge fixtures agree.
- **Not done (README 7.7):** the painted brushwork, the manga screentone, glowing outlines (the outline material is shared), the skull's cracks, arm poses.

**22 October 2026, 12:30 UTC (lead: radioactive burst, 1:1 super attack, dragon belly, balaclava; new simulation stamp; deployed).**
- **Deployed:** build `20261022T122518050Z-580f0e9fa5e04227921ba32cb513e908`, **new simulation stamp `b8d600c2135da877`** (was `880d50523c1273a1`). Ruleset genesis-0.4.0: every Radioactive Genesis (30) fires the radioactive burst, the nova whatever its Eyes, drawn as the sign of radioactivity (a new eye glyph) and keeping its toxic zone; its card says the special comes from its radioactivity. The 1:1 delivery `inversion`, never built until now, is a super attack: three at once at full strength, turned 120 degrees apart for beam, flash and wave, three strikes 0.35 s apart for the nova and the puddle (`GenesisSignatureSkillData.Spread`). The dragon's belly is painted on the body's own faces (`stream/heroes-hats` 0bb0a7a, `costumes-dragon-belly-2026-10-22.jpg`); the balaclava is one knitted shell with ribs and almond eye holes (a1133b9, `costumes-balaclava-2026-10-22.jpg`). The siege's start banner reads SIEGE, BRING DOWN THEIR TOWER AND THEIR CORE! (seen in a local siege).
- **Verified:** catalog 31 tests (one new), headless simulation 32 tests, 175 view tests; the roster resolves 1,969 of 1,969 (nova 610, wave 464); the skill assets carry Spread 2 at 120 degrees; `?specials=1531,1657,53,1376,1006,1287` draws the radioactive sign on the Radioactive bursts; a local coins match against the 1:1 Redd, Grim, Midas and two Radioactive bots with `charged=1`, Photon accepting the new version, without exception; the judge still agrees on its three recorded matches.
- **Not verified:** the three copies of a 1:1 super attack were not singled out on a capture (they are drawn by the same view as one attack); a public match with a Radioactive or 1:1 player.

**22 October 2026, 12:00 UTC (lead: headbands, Spartan helmet, eyes batch 2, basic attack words by class; deployed).**
- **Deployed:** build `20261022T114221166Z-6ae2d94d6ad14dba84e240239af24f50`, stamp unchanged. Headbands, durag and bandana on the brow with no hair through them (`costumes-headbands-2026-10-22.jpg`); Spartan helmet spikes out of its rear edge, none pointing down (`costumes-spartan-2026-10-22.jpg`); eyes batch 2 (Round Crying, Round Tear, Teary as tall ovals, Side Eye, Anime Glass) and Smirk Left's near eye a heavy-lidded almond (`heroes-eyes-batch2-2026-10-22.jpg`). The basic attack is said in the class's words: the hero sheet's row is CLOSE STRIKE, LONG SHOT or LOBBED ZONE over "Shoots SNOWBALLS from its RUSSIAN HAT, fast, from a distance."; the result screen and the reveal use the class's verb (LOBS, SHOOTS, STRIKES WITH).
- **Verified:** export 1,970 Genesis, 0 failed; 175 view tests; `?heroes=12,65,21,242,6,51`; a local siege without exception (a tower fell); judge fixtures agree.
- **In progress, not deployed (worktree `lead/specials`):** a radioactive special for every Radioactive Genesis (catalog 0.4.0: the Epic tier gives the nova whatever the Eyes, drawn as the sign of radioactivity in its glow) and the 1:1 super attack (the inversion delivery: three at once at full strength, in three directions for a shape that aims). It changes the simulation: a new Photon version, judge fixtures to re-check.

**22 October 2026, 10:45 UTC (lead: session paused on its usage limit; open work).**
- **Live:** build `20261022T102129035Z-ddb46fd0faf2491da81b7eb454778963` (faces batch 1, ushanka, clown nose), wager version 2.
- **Accepted, on branches, not merged:** `stream/heroes-hats` 8282f74 headbands on the brow with no hair through (`costumes-headbands-2026-10-22.jpg`), 469e583 Spartan helmet spikes out at the sides, none down (`costumes-spartan-2026-10-22.jpg`). `stream/heroes-faces`: eyes batch 2 and Smirk's second pass, pushed but not reviewed.
- **Still to do from the owner's review:** dragon belly from the side, balaclava rebuilt, clothes and their motifs audited against the art, the strict eye audit down the frequency list then visors and glasses.
- **New requests (owner, 10:40):** Radioactive skins with more colours in their gradient as the art; Radioactive and Skull jaws drawn horizontally as the art, not a line going up; no skeleton lines over a T-shirt on Radioactive and Skull bodies; the ten 1:1 heroes worked as much as the art; a radioactive special attack for Radioactive Genesis instead of their eyes' one, and a special super attack for each 1:1 (simulation and view: specials are in the simulation, a Photon version); the basic attack line under a hero reworked: short, says the attack type of its class (brute, ranger, controller) and what it throws, with a clear text design.
**Next action:** merge both branches and deploy after review; then the new requests, the special attacks batched as one simulation change.

**22 October 2026, 10:30 UTC (lead: owner's hero review, first lots deployed).**
- **Deployed:** build `20261022T102129035Z-ddb46fd0faf2491da81b7eb454778963`, stamp unchanged. Faces lot 8 batch 1 (`stream/heroes-faces` 70ab510, `heroes-eyes-batch1-2026-10-22.jpg`, `heroes-lot8-lasers-glasses-coin-2026-10-22.jpg`): a strict eye audit (Smirk Left, Retro Glass were wrongly graded faithful; Happy, Round, Dead, Normal Eye, Heart closer), laser beams from the visible eye, every mouth centred on the face's middle line, the Mon coin raised. Costumes (`stream/heroes-hats` 5b536f7): the ushanka tall with its fur roll and flaps (`costumes-ushanka-2026-10-22.jpg`), the clown nose a little out (`costumes-clown-nose-out-2026-10-22.jpg`).
- **Verified:** export 1,970 Genesis, 0 failed; budget largest #630 7,991, none over 8,000; 175 view tests; `?heroes=39,34,210,549,35,242`; a local siege without exception; judge fixtures agree.
**Next action:** eyes batch 2 and Smirk's second pass; costumes items 3+8 (headbands), 4 to 7.

**22 October 2026, 10:05 UTC (lead: a full costume before the rainbow; deployed).**
- **Deployed:** build `20261022T095836261Z-578db447190e46aa898f2c21da2accf4`, stamp unchanged. The owner's correction: a dragon that pukes a rainbow throws fire; the order is what the hand holds, the full costume, the rainbow, the hat, the bone of a skull, the crown's spikes. `?impacts=226,112,70,16,74,1287`: #226 fire, #112 rainbow, #70 candle, #74 frog.

**22 October 2026, 09:50 UTC (lead: the game is Chog Balls Stars; held things and rainbows thrown first; deployed).**
- **Deployed:** build `20261022T093755118Z-4b247ef2a30b4626b558767eb8c7cb3c`, stamp unchanged (`880d50523c1273a1`). The owner renamed the game Chog Balls Stars (Chog Arena is shared by many projects): menu title, loading page, page and app names, share texts, sign-in message, submission draft; the address `chogarena.vercel.app` is kept. The basic attack is chosen in the owner's order: the thing held in the hand (Accessory, 195 Genesis, the very part thrown as a hat is), then the rainbow of a Rainbow Puke mouth (145), then the full costume, the hat, the bone of a skull, the crown's spikes; each held thing has its own impact (`docs/ATTACKS-FROM-THE-ART.md`).
- **Verified:** `?impacts=28,70,26,45,591,1002` and `23,74,103,112,226,1287`: candles, cup, coin, knife in flight, rainbows on their impact; export 1,970 Genesis, 0 failed; 175 view tests; 129 service tests; judge fixtures agree; `/api/wager` still version 2 after the services restarted.
- **Known:** a dragon with a Rainbow Puke (#226) now throws rainbows, not fire, as the owner's order says. The 09:36 push restarted the services while the owner may have been playing.
**Next action:** the owner's 3D review lots (costumes item 2 onwards, faces lot 8).

**22 October 2026, 09:10 UTC (wager version 2 live on the services).**
- The owner set `WAGER_ADDRESS` to `0xc2c1e282dd8c1cc2a4689972ac9c48d3b1454425` on Render and redeployed by hand: `/api/wager` answers version 2, burnedShare 500, genesisShare 500, the referee of version 1. The dashboard value is what the service reads (the service is not synced from `render.yaml`; "Save only" keeps the old process running until a manual deploy).
- Owner's review of the heroes in the game, sent to the two 3D agents as lots: eyes under glasses, mouth shapes and offsets, laser beams off the pupil (faces); ushanka height, clown nose a little out, hair through the red headband, Spartan helmet spikes, dragon belly from the side, balaclava, clothes and their motifs, headband height (costumes).
**Next action:** 3D lots; owner plays a signed and a staked match.

**22 October 2026, 09:00 UTC (lead: the rank points fly into the bar, home card in the theme, clown nose, banner by mode, wager version 2 live; deployed).**
- **Deployed:** build `20261022T084602124Z-08eece834cb7452c9ef95b9963414f56`, stamp unchanged (`880d50523c1273a1`). On the result screen a verified gain now flies: one gold comet a point (twelve at most) from the "+N" window, moved over the hero's half, to the rank bar, the counter ticking up and the bar filling and pulsing at each landing, a note up the scale each; a loss drains the bar; reduced motion keeps the counted fade. The home card is a raised card of the theme with the portrait in a gold medallion (the cream paper the owner found out of place); the clown nose is a half-sunk ball with one outline (`stream/heroes-hats` 21c2ae0, `costumes-clown-nose-2026-10-22.jpg`); the sample's start banner names the mode played. `render.yaml` names wager version 2 (`0xc2c1e282dd8c1cc2a4689972ac9c48d3b1454425`): the owner's change on Render was put back to version 1 by each deploy, which took the address from this file.
- **Verified:** the flight on the local preview (`?rankpreview=1`: comets in flight between the window and the bar, the bar filled after); a local siege without exception; export 1,970 Genesis, 0 failed; 175 view tests; judge fixtures agree.
- **Not verified:** the flight after a real signed match; `/api/wager` answering version 2 (checked right after this deploy).
**Next action:** owner plays a signed match (no deploy under way) and a staked one on version 2.

**22 October 2026, 08:00 UTC (lead: a leftover arena scene is unloaded when a match scene loads; deployed).**
- **Deployed:** build `20261022T074538326Z-268ac06904a4400bb42ec71a96841f12`, stamp unchanged. The owner played coins then a siege and saw the crates of both maps (two arena scenes loaded together): `ArenaScenes` now unloads any other arena scene when one loads. Wager version 2 is deployed by the owner at `0xc2c1e282dd8c1cc2a4689972ac9c48d3b1454425` (read back: referee as version 1, burnedShare 500, genesisShare 500); the services still name version 1 until `WAGER_ADDRESS` is changed on Render.
- **Verified:** a local coins match to its end, HOME, SIEGE, a siege match: the siege map alone, no exception (the leftover did not happen on that path, so the guard was not exercised); judge fixtures agree.
- **Known:** every deploy restarts the free Render service and loses the replays of matches it had not judged yet: the owner's matches played during the 07:05 and 07:35 deploys earned no verified rank, so no reveal.
**Next action:** owner sets `WAGER_ADDRESS`; owner replays a signed match with no deploy under way; the owner's exact path to the merged maps.

**22 October 2026, 07:35 UTC (lead: face top fixed, winning arms in a V, stake controls at once, wager version 2 ready; deployed).**
- **Deployed:** build `20261022T072708592Z-f2cf028461454c7495d2e39fbdb52130`, stamp unchanged (`880d50523c1273a1`). The head no longer shows through the top of the face (`stream/heroes-faces` 8035f3c, `heroes-face-top-2026-10-22.jpg`: the brow had no edge point, a flat span sagged under the head); a win raises the arms 115 degrees in a V instead of 150 over the head; the pre-match stake controls show at once (the button works once the services know the player is in the match, asked every second until then). The game and the services speak both versions of the wager: version 2 (`contracts/src/ChogWager.sol`, 92 contract tests, 129 service tests, 175 view tests, encodings checked against cast) pays 5 of the 10% taken from the lost stakes to the wallets that played a Genesis; it is **not deployed** (the owner deploys `script/DeployWager.s.sol`, then sets `WAGER_ADDRESS` on Render).
- **Verified:** export 1,970 Genesis, 0 failed; `?bodies=1` shows the V; `?heroes=0,76,75`; a local siege without exception; judge fixtures agree.
- **Not verified:** the stake panel timing and the rank reveal with a signed public match; a Genesis player who did not stake sees nothing of their share in the game yet (paid on chain only).
**Next action:** owner deploys wager version 2; owner plays a signed match.

**22 October 2026, 07:05 UTC (lead: wider face, tidied shark, dragon eyes, toad eyes, connected hose, rank reveal, home fixes deployed).**
- **Deployed:** build `20261022T065500605Z-9a9fb0cddb904e20804d34de12248ee4`, stamp unchanged (`880d50523c1273a1`). Face to the sides and the chin (`stream/heroes-faces` b7cd804, `heroes-face-wide-2026-10-22.jpg`); shark head one volume with an even row of teeth on the mouth rim, dragon eyes on the hood, toad eyes at the hood's top corners, the astronaut hose from the side unit into the chest box (`stream/heroes-hats` ea8693e, `costumes-lot5-2026-10-22.jpg`); stream G's rank reveal (d3242e0: the result is immediate, a verified point change animates on the result or on HOME if the player left; nothing shown before the server names the exact room); one connect button on the home and `eth_accounts` before `eth_requestAccounts`; the home hero turns by dragging; hover on the hero list cells; the leftover flat pink floor behind the pedestal removed.
- **Verified:** export 1,970 Genesis, 0 failed, largest hero 7,991 triangles; 170 view tests; heroes and the home in the build; a local siege without exception; the judge agrees on its three fixtures.
- **Not verified:** the rank reveal with a signed match (no wallet in the test browser; no staging, the server only ranks public matches); the wallet connection change with the owner's wallet.
**Next action:** owner plays one signed match to see the rank reveal and the connection; decision on the Genesis players' share of the wager rake (proposal: 5 of the 10% burned, new wager contract).

**22 October 2026, 06:15 UTC (lead: shark and astronaut suits, frowning laser eyes, mirrored tears deployed).**
- **Deployed:** build `20261022T060543794Z-34341090b06243b7865a2de58cc2d1ef`, stamp unchanged since 05:50 (`880d50523c1273a1`). Shark suit as a shark (white blade teeth on the mouth rim, snout, dorsal, back and tail fins, side eyes and gills inferred, hip fins instead of the arm plates, broad white belly; `stream/heroes-hats` 536b9f7, sheet `costumes-shark-2026-10-22.jpg`); astronaut with an opaque white helmet, the hair fringe in the visor, white legs and grey boots, the glass mode unused (ce4df51, `costumes-astro-2026-10-22.jpg`); Side Right laser eyes as frowning almonds (`stream/heroes-faces` c500363, `heroes-lasers-right-2026-10-22.jpg`); tears on the outer side of each eye (1a89333, `heroes-tears-2026-10-22.jpg`). Banana, toad and gangster reviewed, unchanged.
- **Verified:** export 1,970 Genesis, 0 failed, largest hero 7,981 triangles; 170 view tests; `?heroes=116,249,16,321,107,75` in the build; a local siege without exception; the judge agrees on its three fixtures.
- **Not verified:** on a phone; the costumes' sheets were rendered before the face lot 5a (re-rendered spot checks agree).
**Next action:** owner review in a match.

**22 October 2026, 05:50 UTC (lead: siege map A, dragon as a dragon, face to the chin, deployed; new simulation stamp).**
- **Deployed:** build `20261022T054122345Z-edaffa4617514742a14f1152fa054a10`, **new simulation stamp `880d50523c1273a1`** (was `f4a81f1ddbd919b6`; the owner approved the siege batch). The siege plays on its own map (three lanes, a walled base: `content/maps/siege-three-lanes.txt`, rebaked by SiegeMapBuilder from today's Gameplay scene, 381 static colliders, scene GameplaySiege), coins keep Map1; the core's collision radius is 1.5; a tower shoots only a hero it sees (blocks stop its sight, bushes do not). The dragon suit is a dragon head (snout and nostrils over the face, four fangs on the mouth, bat wings, belly, ridge; `stream/heroes-hats` 3746dae). The face reaches the chin, less forehead, and decals 0.0020 over it (the white specks in the cheeks were the face showing through a cheek lying 0.0008 over it; `stream/heroes-faces` cc5a603). The rim light is a screen (no white fringe on the cream faces). A wallet without a Genesis is asked the one signature on connecting (the button reads SIGN ONCE TO EARN RANK only if declined). The $CHOG pill shows the token's logo as a coin. The test recorder takes its record at game over again (broken since the result screen leaves the room at once).
- **Verified:** 32 headless simulation tests; the map check (blocks, bushes, walls, points, navmesh, Map1 unchanged, every sight ray of the layout); six-bot sieges on the map damage and bring down structures; a full local siege on the new map to the result screen, recorded, and the judge agrees on it (new fixture `tests/fixtures/replays/20261022T073733-siege-map-winner1-tick6509.json`) and on the two older ones; a coins match on Map1; 170 view tests, 127 service tests; export 1,970 Genesis, 0 failed; the local build connected to Photon with the new version.
- **Not verified:** a public siege with humans; the coin and the automatic signature with a real wallet (no wallet in the test browser); the shark, astronaut and eye lots are still in progress.
**Next action:** costumes lot 2 (shark) and 3 (astronaut), faces 5b (Side Right laser eyes) and 5c (tears mirrored); owner review of the siege map in a match.

**22 October 2026, 04:00 UTC (lead: Skull eyes, phone-size suits and Side Left lasers deployed).**
- **Deployed:** build `20261022T035034836Z-ffe01db972864bf58bd1f17629b63a28`, stamp unchanged (`f4a81f1ddbd919b6`). Lots 4b to 4d of `stream/heroes-faces` (38cd450 to 2a8ee19): the 8 Skull eye values never audited compared with the art (4 faithful, Happy on Side Left fixed for 33 Origin and 4 Skull Genesis, Side Eye approximate, the two lasers in 4d), sheet `research/evidence/heroes-skull-eyes-2026-10-22.jpg`; shark teeth 14 larger instead of 20, the astronaut hose thicker and hanging outside the helmet, the flag on the shoulder as the art draws it (`heroes-phone-size-2026-10-22.jpg`); face lasers on Side Left as thin as on Side Right and turned up 8 degrees to match the art's slant (`heroes-lasers-left-2026-10-22.jpg`).
- **Verified:** export 1,970 Genesis, 0 failed, largest Origin hero 7,971 triangles; 168 view tests; `?heroes=116,249,127,252,1207,418` loads them; a local bot match without exception; judge fixtures agree.
- **Not verified:** the astronaut flag stays hard to see from the game camera (the helmet covers the chest; kept where the art puts it); Side Eye's pink far iris is not drawn; Side Right beams were not changed.
**Next action:** owner review in a match; siege map A only with the owner.

**22 October 2026, 03:30 UTC (lead: glass astronaut helmet and the last hat impacts deployed).**
- **Deployed:** build `20261022T032043343Z-b94ee421cd304209abbb0e53ed88bc83`, stamp unchanged (`f4a81f1ddbd919b6`). The hero shader gains a glass way of drawing (light 3, vertex alpha 0.4: a share of the pixels kept in a fixed screen pattern, a third facing the camera, most at the edges, no transparent sorting); lot 4a of `stream/heroes-faces` (72f6e2d) makes the astronaut's dome glass with the crown inside it (sheet `research/evidence/heroes-astro-glass-2026-10-22.jpg`); ram horns, demon/devil/void horns, the taped banana and the 1:1 brain get their own impact (no thrown object keeps the generic pop, tested); the submission draft describes the costume attacks, the player ladder and the balance in dollars.
- **Verified:** the shader compiled fresh for WebGL with no error; export 1,970 Genesis, 0 failed, largest Origin hero 7,971 triangles; 168 view tests; `?heroes=249,1529,675,708` shows the hair colour through the glass from the game camera; `?impacts=` names the new impacts; a local bot match ran without exception; judge fixtures agree.
- **Not verified:** the glass on a phone screen (its grain is visible at 1280x720 on desktop); the impact moments themselves were not caught on a capture.
**Next action:** lots 4b (eight Skull eye values), 4c (shark teeth, astronaut flag and hose at phone size) and 4d (Side=Left face lasers level and thin) on `stream/heroes-faces`, one sheet each.

**22 October 2026, 02:10 UTC (lead: dragon, shark and astronaut suits deployed).**
- **Deployed:** build `20261022T015853880Z-0955479927304adda8f68a176060d297`, stamp unchanged. Lot 3 of `stream/heroes-faces` (f66276a), from all 72 suited Genesis' art: dragon with four blunt tan horns, smaller wings, no tail spikes, its "w" marks and a larger belly; shark with a full row of 20 teeth round the face and pectoral fins; astronaut rebuilt as a full suit with a round helmet, side unit, hose, flag patch, chest panel and pack (`research/evidence/heroes-costumes-2026-10-22.jpg`). All Origin heroes under 8,000 triangles (largest 7,971).
- **Verified:** export 1,970 Genesis, 0 failed; 168 view tests; the suits load and throw their objects in `?impacts=`; judge fixtures agree.
- **Still open:** the astronaut glass is opaque (the art shows the hair through it); hose, flag and teeth are small at phone size; the backs of the suits are not in any picture.
**Next action:** owner review of the three lots in a match (sheets in `research/evidence/heroes-*-2026-10-22.jpg`).

**22 October 2026, 01:45 UTC (lead: eyes and mouths lot deployed).**
- **Deployed:** build `20261022T013733208Z-4f2e75ac4a1a42adbc50bc44b8a3350c`, stamp unchanged. Lot 2 of `stream/heroes-faces` (6af4e6d): audit of the 64 Origin eye and mouth rows against the art (`research/evidence/heroes-faces-audit-2026-10-22.md`: 48 faithful, 16 approximate, none wrong; Round, Round Eye and Dead fixed); the 17 Radioactive eye values drawn with the ink of their variant as the art does, Round Eye without pupil, the third eye as a white almond (`research/evidence/heroes-faces-fix-2026-10-22.jpg`). Skull: 17 of 25 eye values looked at.
- **Verified:** export 1,970 Genesis, 0 failed; 168 view tests (the type-check tool now finds the Editor's renamed compile folder); heroes load in `?impacts=`; judge fixtures agree.
**Next action:** lot 3 (dragon, astronaut, shark suits) running on `stream/heroes-faces`.

**22 October 2026, 01:20 UTC (lead: costumes attack, full face, ladder and economy lots deployed).**
- **Deployed:** build `20261022T010049645Z-64406dda6bec4f97800607be4f87ed3d` (simulation stamp unchanged, `f4a81f1ddbd919b6`, no new Photon version), from `lead/integration` fast-forwarded into `main`.
- **In it:** full costumes give the basic attack (`chog_attack_objects.ATTACK_OF_BODY`: Dragon fireball with a flame trail, Shark water with bubbles, Banana, Astro planet, Toad lily pad, Gangster money bag; 147 Genesis that threw a crown leaf under their hood); the face fills the front of the head (`stream/heroes-faces` e6f9091, sheet `research/evidence/heroes-face-fill-2026-10-22.jpg`); ladder lots 3-4 (hidden Elo as a band, seasons on the API) with the room band C0 kept off behind `RATING_BAND_ROOMS=1` until a public room creation is checked; economy lots 2-3 (stake card on the result screen, cheapest rentals and rental ledger, home balance in dollars). Build requests accept `clean` (a stale IL2CPP cache failed to link the result screen's async methods).
- **Verified:** 127 service tests, 168 view tests, typecheck of Chog.Match, Chog.Editor and Assembly-CSharp; library export 1,970 Genesis, 0 failed; `?impacts=16,116,37,249,32,40` shows each costume throwing its object and its impact; a local Recruit bot match through to the result screen without exception; judge fixtures agree.
- **Not verified:** the stake card with a real stake, the rental card with live offers, the dragon/shark/astronaut suits themselves (remodel is lot 3 of the 3D stream), eyes and mouths (lot 2 running).
**Next action:** owner review of the costumes in a match; lot 2 (eyes and mouths) then lot 3 (dragon, astronaut, shark suits) on `stream/heroes-faces`.

**21 October 2026 (result-room release deployed for owner review).**
- **Deployed with explicit owner authorization:** `stream/result-home` was fast-forwarded into `main` at `bba8b5e`, then Vercel deployed `https://chogarena.vercel.app/`. Its public page serves WebGL version `20261021T180233353Z`; the Render Photon endpoint reports the same build. The packaged build is `20261021T180233353Z-10023a260d094a11a4f9bda80ed53acb`, with unchanged simulation stamp `f4a81f1ddbd919b6`.
- **Verified before deployment:** the full release suite and two judge fixtures passed as recorded below. A local Recruit bot match reached and retained the result screen after Photon room closure. The release page and backend version were checked publicly after deployment.
- **Not yet verified:** a signed public match, a positive player-rank award, the exact-match rank reveal on the result screen, tier-up timing and the owner's visual acceptance. The local preview match did not qualify for rank points. No production result was fabricated.
**Next action:** play one signed public Recruit match on the deployed site, stay on the result screen until the server judges it, confirm that the rank delta appears there without HOME, then check the home badge against the same result. If the live reveal fails, inspect its room ID, replay verdict and rank API before changing presentation.

**21 October 2026, 18:27 UTC (result-room release candidate prepared, not deployed).**
- **Prepared on `stream/result-home`:** WebGL build `20261021T180233353Z-10023a260d094a11a4f9bda80ed53acb` was assembled with its judge into `site/` and committed as `e3f0ab2`. The owner-authorized takeover did not change the live site or `main`: the direct main push was rejected by the repository approval policy because the team workflow assigns integration to the lead. No alternate deployment path was attempted.
- **Release checks:** all checks in `tools/release/check_all.sh` passed when local loopback permission was granted, including 166 view tests, 109 service tests, 31 catalogue/simulation tests and 86 contract tests. The assembled judge replayed two recorded matches and agreed with both winners. The local preview-room result capture is in the entry below; it does not verify a signed rank award.
**Next action:** obtain explicit approval for the precise main-branch integration and production deployment, or have the lead merge `stream/result-home`; then test a signed Recruit result and confirm that its exact-match rank reward appears before returning HOME.

**21 October 2026, 18:22 UTC (result room-close sequencing exercised locally).**
- **Implemented on `stream/result-home`, not deployed:** the result screen snapshots the final verified match frame and its Photon room ID before cleanup, then begins `DisconnectAsync` as soon as the persistent result screen starts. The earlier arbitrary 0.8-second wait was removed. The player sees the result immediately; the rank reward appears only after the server confirms this exact match. An older match can no longer trigger a false reward overlay.
- **Verified locally:** screen typecheck has 0 errors; 166 view tests pass. Unity WebGL build `20261021T180233353Z-10023a260d094a11a4f9bda80ed53acb` succeeded with 0 errors and unchanged simulation stamp `f4a81f1ddbd919b6`. A local Photon preview room completed a Recruit bot match; its [result capture](../research/evidence/result-home-room-close-2026-10-21.png) remained visible for more than two minutes after match end, and the public backend judged the replay void because its preview build differs from the production judge. The browser reported no JavaScript exception. This confirms result persistence and replay delivery, not a ranked point award.
- **Still open:** the `rankpreview` fixture shows an existing Genesis Mastery Road, so its late screenshot is not evidence of a Recruit rank reveal. No signed live win, confirmed rank animation timing, production deployment, or owner visual approval was performed for this refinement.
**Next action:** exercise a signed Recruit win on the deployed production build, confirm the exact game ID and player points before calling the rank reveal complete; review the rank overlay separately from Genesis mastery.

**21 October 2026, 17:09 UTC (result/home stream, pending runtime review).**
- **Implemented on `stream/result-home`, not deployed:** commit `7b486d3` keeps the result canvas and hero stage alive while the finished Photon room disconnects, then polls for the server-confirmed player rank on the result screen. The Recruit result card and the home rank chip refresh from that same server answer. No rank point or match verdict is synthesized client-side.
- **Verified locally:** the screen typecheck reports 0 errors and the current view suite reports 166 passed, 0 failed. A Unity WebGL build of the same source changes succeeded with 0 errors before the branch commit. The existing [public result](../research/evidence/chog-player-rank-public-result-2026-10-21.png) and [public home](../research/evidence/chog-player-rank-public-home-2026-10-21.png) captures show the previous behavior, not the new branch. A new result capture and a signed live win remain unverified while the shared heavy-job lock is held by the lead.
- **Cross-stream request:** the current `CharacterDefeated` event exposes only the victim, so a personal kill or assist toast would be fabricated. The simulation owner should define deterministic killer and assist attribution, emit the credited actors, and have an in-match UI owner add a compact defeat toast and synchronized SFX/VFX. No such toast was added in this stream.
**Next action:** capture and inspect the new result screen on local WebGL after the heavy-job lock is free, then have the lead merge/build/deploy the branch and verify a signed public win on the live site.

**21 October 2026, 13:00 UTC (public player ladder enabled and exercised).**
- **Implemented and deployed:** `RANK_ENABLED=1` is on Render, with Upstash as the durable event store. Connected wallets without a Genesis can now use `SIGN IN FOR RANK` and play Recruit for player points. Commits `19e9197` and `95a8d5a` were pushed to `main`; the production client, judge and Photon endpoint report build `20261021T122023603Z` with unchanged simulation stamp `f4a81f1ddbd919b6`.
- **Verified:** public health returned `rankStorage: writable` and `rankEnabled: true`. A fresh signed wallet entered a public Photon Siege with Recruit and bots. The judge produced a non-void defeat, and the public rank API returned one ranked match for that wallet, with zero points after the loss at the Bronze floor. The public client showed its Bronze badge; the match result and returned home are captured [here](../research/evidence/chog-player-rank-public-result-2026-10-21.png) and [here](../research/evidence/chog-player-rank-public-home-2026-10-21.png). The browser reported no JavaScript exception. The local WebGL build succeeded with zero errors, screen typecheck had zero errors, 155 view tests passed, and the assembled judge agreed with both recorded winners.
- **Not yet verified:** a live win and its positive point award, a tier-up reveal, six authenticated humans, skill-based matching, or persistence of a completed rank event across a Render restart. The result screen remained on `VERIFYING THE MATCH` while the player stayed in the room; the event was confirmed after HOME. The timing of the post-match rank reveal needs a dedicated fix before calling that hero moment complete. Unfinished replays remain on Render's ephemeral storage.
**Next action:** make the verified point award appear on the result screen without requiring HOME, then play a signed public win to check the positive award and reveal against the server event. Do not alter rank scores manually.

**21 October 2026, 12:05 UTC (durable player rank store connected, ladder still gated).**
- **Owner-confirmed configuration:** the Upstash REST URL and token were added to the Render service environment. Their values were never copied to Git or this record.
- **Implemented and deployed:** immutable player rank events now use Upstash Redis via `SET NX`; local replay files remain on Render. The host checks both connectivity and short-lived write permission at startup, reports only a credential-free status on `/api/health`, and refuses to enable rank if storage is unavailable. Commits `1a94b3d`, `ad07174` and `4a89899` were pushed to `main` and Render deployed them. The Unity client and simulation were not rebuilt.
- **Verified:** 93 service tests and 155 view tests passed. The public Render health endpoint returned `rankStorage: writable` and `rankEnabled: false`, proving the deployed server can write with the configured token while public rank credit remains off. The write probe uses an expiring key, not a player event. No signed-in match, actual rank credit, restart persistence of a real event, or public rank UI has been verified.
- **Limit:** a Render restart during an unfinished match can still lose its local replay and prevent a verdict. Upstash makes completed rank events durable; it does not store replays or wagers.
**Next action:** run a controlled signed-wallet bot match behind a narrow rank test gate, confirm the replay verdict, rank event and result UI, then deliberately decide whether to enable public rank. Do not claim the ladder is live yet.

**21 October 2026, 08:13 UTC (bot-weighted player ladder candidate, client deployed).**
- **Product rule updated in code:** a completed, verified Siege with bots can now change the signed-in player's ladder. One authenticated human earns 3 points for a win or loses up to 2; six distinct authenticated humans earn 20 or lose up to 12. Intermediate win values are 6, 10, 13 and 17; loss magnitudes are 4, 6, 8 and 10. The floor is zero, and draws give zero. Guest seats do not inflate the award because distinct guests cannot be proven from separate tabs. Each match event freezes its human count and scoring. This is a participation-weighted ladder, not a skill rating.
- **Verified in tests:** 89 service tests and 155 view tests passed. Rank tests cover bot wins and losses, mixed human/guest rooms, previews, duplicate wallets, event idempotency, tier changes and the signed-wallet endpoint. The Unity build and private match evidence in the next entry are unchanged. No live wallet-backed rank credit has been demonstrated.
- **Deployed and checked:** commit `695d989` is pushed to `main`. Vercel serves client version `20261021T072357423Z`; Render's Photon endpoint reports the same version. A fresh test wallet received `enabled: false` from the public rank API. No public match was entered. The free Render instance has no persistent rank store, so the public host keeps `RANK_ENABLED` off. This scoring rule is not yet a live ladder.
**Next action:** obtain persistent storage and prove one signed-in bot match end to end before enabling public rank points. Ask the owner to review the deployed client progression visuals, which have only local runtime evidence so far.

**21 October 2026, 07:55 UTC (player rank candidate, local verification).**
- **Implemented locally:** player ladder points are separated from Genesis mastery. The server uses one immutable event per replayed Siege match and refuses bot, guest, preview, partial and unauthenticated games. Wins add 25, losses subtract 15 to a floor of zero; Bronze, Silver, Gold and Star thresholds are provisional. The signed-in player chip, preparation screen and result can show the confirmed rank. The result waits for the server event before revealing the point difference or a new tier. Photon still fills compatible rooms; the preparation screen now shows the human count and bot fallback. This is not skill matching.
- **Verified locally:** server tests 86 passed, 0 failed; view tests 155 passed, 0 failed; match and screen typechecks have 0 errors. Unity WebGL build `20261021T072357423Z-9b6b74ea3ff04010895a5a385211973d` succeeded with 0 errors and unchanged simulation stamp `f4a81f1ddbd919b6`. The final build reached a private Photon bot match without browser exception. A preceding build of the same code path completed a bot match and showed the casual result. The [lobby](../research/evidence/chog-rank-lobby-runtime-2026-10-21.png), [casual result](../research/evidence/chog-rank-casual-result-2026-10-21.png) and [clearly marked local rank-up preview](../research/evidence/chog-player-rank-preview-2026-10-21.png) are WebGL captures, not static mockups. The preview is not proof that six signed-in humans earned points.
- **Launch gate:** Render's free instance has no persistent disk. The host therefore leaves player rank disabled unless `RANK_ENABLED=1` is deliberately set after persistent storage exists. No paid hosting change was made. No six-human signed-in game or durable rank credit has been verified, so the ladder must not be advertised as live. The earlier Genesis mastery reveal remains a local candidate too.
**Next action:** deploy only the verified casual/mastery UI with rank gated off if desired; launch player rank only after persistent storage and a real six-human end-to-end match prove the credit and badge.

**20 October 2026, 18:17 UTC (local post-match progression candidate, not deployed).**
- **Implemented locally:** the existing per-Genesis mastery road now reveals a confirmed point gain, a crossed level and newly earned career badges in sequence. The result distinguishes verified credit, no credit, preview and unresolved verification. Its first confirmed match can reveal from the known zero-point baseline. The settings can turn Reward Animation off while keeping the result readable. A private local preview exercises the visual states without writing a career. The [design contract and evidence](POST-MATCH-PROGRESSION-2026-10-20.md) distinguish this Genesis mastery from a future player rank; no rating, ranked queue or skill matching is implemented.
- **Verified locally:** WebGL build `20261020T180102412Z-f824a67d1acc4e9398af98a6435c6337` succeeded with unchanged simulation stamp `f4a81f1ddbd919b6`. Its local design fixture was captured at desktop and 960 x 540 touch-emulated sizes. A private bot match reached team preparation, gameplay and the recruit result without a browser exception. Final build `20261020T181948572Z-36c282a93bf042c4887f8c019dbfceac` changed only the Reward Animation setting's name and icon; it succeeded and its settings panel was visually inspected. It has not run a match. Screen typecheck: 0 errors. View tests: 155 passed, 0 failed.
- **Still open:** the preview does not prove an actual wallet-authorized career credit, its first-match event, or a refused credit. The post-match treatment is a candidate, not accepted visual polish. The live technical `Frame / Ping` overlay is still the sample's legacy text component, and small combat HUD copy remains utilitarian; the newer Chog screens use Lilita One and Nunito rather than Arial. Physical-phone and multi-human matching were not tested. This build has not been deployed.
**Next action:** verify a signed-in Genesis career result end to end, including the first match and no-credit case; review the reward capture with the owner before public deployment or a broader screen-by-screen UI polish pass.

**20 October 2026, 13:04 UTC (shared button hover feedback deployed).**
- **Implemented:** every screen button styled by `ChogTheme` now responds to a desktop hover. It brightens slightly, rises 3 canvas units, grows by 2.5 percent and enables its existing hard shadow. Leaving restores the exact original position and scale. The feedback is one shared component, so landing, lobby, hero, result and settings buttons use the same interaction language without new decorative stripes. No map or simulation files changed.
- **Verified locally:** WebGL build `20261020T125246678Z-c7f2c120e4544dc6a95a8aa3fa1ff278` succeeded with unchanged simulation stamp `f4a81f1ddbd919b6`. [The PLAY hover capture](../research/evidence/chog-button-hover-2026-10-20.png) shows the raised surface. The local client entered a bot match without exception. Screen typecheck: 0 errors. View tests: 155 passed, 0 failed. The assembled judge replayed two recorded matches and agreed with both announced winners.
- **Deployed and checked:** commit `bed1070` is pushed to `main`. Vercel reports the production alias ready and the public page serves version `20261020T125246678Z`. Render's Photon endpoint reports the same build. No public `PLAY` was pressed.
- **Still open:** this was verified with a desktop pointer. Touch controls retain their current press state and were not physically tested.
**Next action:** owner reviews the live hover feel before another UI change.

**20 October 2026, 12:41 UTC (Chog head mark safe area deployed).**
- **Implemented:** the supplied Chog head mark is now scaled into a fixed safe area before its ink contour is added. Its full silhouette and outline fit inside both the player portrait and the HEROES rail tile, instead of being clipped at their edges. No map or simulation files changed.
- **Verified locally:** WebGL build `20261020T123019054Z-d41546da025947e9aec68fa06e0d7b70` succeeded with unchanged simulation stamp `f4a81f1ddbd919b6`. The profile and rail both show the complete mark in this [desktop capture](../research/evidence/chog-head-mark-safe-area-2026-10-20.png). The local client entered a bot match without exception. Screen typecheck: 0 errors. View tests: 155 passed, 0 failed. The assembled judge replayed two recorded matches and agreed with both announced winners.
- **Deployed and checked:** commit `429b4e3` is pushed to `main`. Vercel reports the production alias ready and the public page serves version `20261020T123019054Z`. Render's Photon endpoint reports the same build. No public `PLAY` was pressed.
- **Still open:** connected-wallet landing text and physical-phone behaviour were not exercised in this slice.
**Next action:** owner reviews the live Chog icon sizing before another UI change.

**20 October 2026, 12:09 UTC (gold stage label and Chog loading mark deployed).**
- **Implemented:** the label below the selected home hero now uses the same gold badge language as the player chip. It reads `RECRUIT` for the recruit and the selected Genesis name for a holder. The former specific Genesis portrait in the WebGL template has been replaced by the supplied monochrome Chog head mark on dark violet, used by the loading screen, favicon and PWA icons. No map or simulation files changed.
- **Verified locally:** WebGL build `20261020T115502664Z-aa297c841b2f47c39f1715c75e63c2f6` succeeded with unchanged simulation stamp `f4a81f1ddbd919b6`. The desktop landing showed the gold lower badge, [capture](../research/evidence/chog-home-gold-stage-badge-2026-10-20.png), and its built loader asset is [recorded here](../research/evidence/chog-loading-chog-mark-2026-10-20.png). The local client entered team preparation and a bot match without exception. Screen typecheck: 0 errors. View tests: 155 passed, 0 failed. The assembled judge replayed two recorded matches and agreed with both announced winners.
- **Deployed and checked:** commit `400fa93` is pushed to `main`. Vercel reports the production alias ready and the public page serves version `20261020T115502664Z`; the public icon hash matches the assembled icon. Render's Photon endpoint reports the same build. No public `PLAY` was pressed.
- **Still open:** connected-wallet landing text and physical-phone behaviour were not exercised in this slice.
**Next action:** owner reviews the live gold lower badge and monochrome loading mark before another UI change.

**20 October 2026, 11:47 UTC (landing role badge deployed).**
- **Implemented:** the landing player chip now gives its second line to a gold role badge with an ink contour. It says `RECRUIT` without a Genesis, or the actual resolved Genesis role (`BRUTE`, `RANGER` or `CONTROLLER`) for a connected holder. The portrait keeps the Genesis level, so the small mastery-progress strip is no longer competing with the role.
- **Verified locally:** WebGL build `20261020T093437171Z-f5991717215a4361b7c1a35bae9f1782` succeeded with unchanged simulation stamp `f4a81f1ddbd919b6`. The real desktop WebGL loaded after dismissing only the first-run tutorial, and no browser exception occurred. [Landing capture](../research/evidence/chog-home-role-badge-2026-10-20.png). Screen typecheck: 0 errors. View tests: 155 passed, 0 failed. The assembled judge replayed two recorded matches and agreed with both announced winners.
- **Deployed and checked:** commit `82cf56f` is pushed to `main`. Vercel reports the production alias ready and the public page serves version `20261020T093437171Z`. Render's Photon endpoint reports the same build. No public `PLAY` was pressed.
- **Still open:** the connected-wallet route was not exercised in this visual slice; its role text is read from the same roster role already used by the hero card and kit chips.
**Next action:** owner reviews the live landing badge before another UI change.

**20 October 2026, 09:10 UTC (shared button surface deployed).**
- **Implemented:** the shared button style now draws one sliced surface with a broad lighter upper face, saturated lower face, darker bottom side and one ink contour. This applies through the common theme to menu, collection, hero, settings, lobby and result controls; changing a button's state colour preserves the ink contour. The supplied Chog head mark has a thicker outline in the profile and left rail. No simulation or map files changed.
- **Verified locally:** WebGL build `20261020T085152060Z-c03047a4f7f641849248824cf7cf4560` succeeded with unchanged simulation stamp `f4a81f1ddbd919b6`. The actual WebGL was opened in Chrome: home, collection, lobby and a local bot match were reached with no JavaScript exception. A 960 x 540 touch-emulated home was inspected, but not a physical phone. [Home](../research/evidence/chog-button-system-home-2026-10-20.png), [collection](../research/evidence/chog-button-system-collection-2026-10-20.png), [lobby](../research/evidence/chog-button-system-lobby-2026-10-20.png), [touch emulation](../research/evidence/chog-button-system-touch-2026-10-20.png). The full release suite passed: 31 catalogue and simulation tests, 155 view tests, 80 service tests, and 86 contract tests with one skipped. Both recorded matches replayed with the announced winners.
- **Deployed and checked:** commit `2e3450b` is pushed to `main`. Vercel reports the production alias ready and the public page serves version `20261020T085152060Z`. Render's Photon endpoint reports the same build. No public `PLAY` was pressed.
- **Still open:** the owner's live visual review, physical-phone behaviour and wallet-connected screens were not verified in this pass. The common style does not restyle non-button match HUD controls.
**Next action:** owner reviews the live menu, collection and lobby before another visual change.

**20 October 2026, 08:24 UTC (integrated home action relief deployed).**
- **Implemented:** `SIEGE` and `PLAY` now draw as one continuous sliced button sprite. The coloured face and darker lower side sit inside one near-black outer contour. The separate offset `ReliefBase` object has been removed. The left rail, home layout and action behaviour were not changed in this correction.
- **Verified locally:** WebGL build `20261020T080937392Z-5177c6fc9995432c821349c4ad44d430` succeeded with unchanged simulation stamp `f4a81f1ddbd919b6`. View tests: 155 passed, 0 failed. Release catalogue, simulation and service controls passed. The assembled judge replayed two recorded matches and agreed with both announced winners. The full desktop home after WebGL load is captured [here](../research/evidence/chog-home-grid-runtime-2026-10-20.png). No physical phone or real wallet was used.
- **Deployed and checked:** commit `65475a8` is pushed to `main`. Vercel deployed the public site and the Render Photon endpoint reports `20261020T080937392Z`. No public `PLAY` was pressed.
**Next action:** owner reviews the two home actions on the live site before another visual slice.

**20 October 2026, 07:31 UTC (extended rail and raised actions deployed).**
- **Implemented:** the home profile and HEROES rail use the supplied Chog head outline. The head mark is a high-resolution hard silhouette with the same thick ink contour as the menu pictograms. The five rail entries are children of one vertical layout, with one shared width, tile size, label zone and spacing. Its grid now reaches from below the player chip to immediately above the kit chips, so the left column uses its available vertical space. The connected empty-wallet card has one clear route, `RENT` in the left rail: the duplicate `RENT A HERO` and unexplained stake sign-in calls are absent. The mode picker and `PLAY` share the exact outer edges of the right card and each now has a separate darker block below its face, making the ingot depth structural rather than a painted line. Standard controls retain one saturated face and one thick plain contour. The sample class chooser is hidden before it can flash; a player without a Genesis is shown as `RECRUIT` in team preparation. The generic wager panel stays hidden until a player actually participates in a wager.
- **Match audio:** `MusicMatch` now contains the existing Chog match loop and the retained Photon sample arena loop. One clip is selected when `Gameplay` opens and stays fixed for that match. With more than one clip, the immediately previous match clip is skipped. This is presentation only and does not touch Quantum state.
- **Explicitly deferred by the owner:** no additional terrain, arena floor mark, platform or wall work was made in this slice.
- **Verified locally:** WebGL build `20261020T072831513Z-973b44c2ea6a49dc949892ac05dace60` succeeded with unchanged simulation stamp `f4a81f1ddbd919b6`. The screen and match typechecks report 0 errors. View tests: 155 passed, 0 failed. Release catalogue, simulation and service controls pass. The assembled judge replayed two recorded matches and agreed with the announced winner of both. The desktop home was opened after full WebGL load; the capture is [here](../research/evidence/chog-home-grid-runtime-2026-10-20.png). No physical phone or real wallet was used.
- **Deployed and checked:** commit `77b1b4b` is pushed to `main`. Vercel deployed the public site and the Render Photon endpoint reports `20261020T072831513Z`. No public `PLAY` was pressed.
- **Still open:** owner review of the live candidate, physical phone behaviour, real-wallet connection and an audible two-match music check. The accepted Skull and Radioactive work does not close the astronaut, cap-spike or eyes/mouths hero tasks.
**Next action:** let the owner review the public home and lobby before the next product slice.

**19 October 2026, 17:00 UTC (corrected visual build, deployed for public review).**
- **Implemented locally after the owner's correction:** the home hero moved into the left focal column, the original stage tile was kept, and the duplicate `SEE EVERY HERO` action was removed. In the arena, the 198 visible sample interior-floor renderers are replaced by one painted berry-slab surface, so the old brick pattern no longer shows through. The 86 sample crates retain their gameplay geometry and now have painted pink visual tops. The near `CenterWall` has a two-course stone face and separate caps instead of a flat dark band. These changes are cosmetic; no Photon map, collision, trigger, cover point, spawn or simulation asset changed.
- **Verified locally:** WebGL build `20261019T170012093Z-1832effe278c49ac8ea39f8d65e834bb` succeeded with 0 errors and the unchanged simulation stamp `f4a81f1ddbd919b6`. A desktop and a touch-emulated bot match both reached gameplay without a JavaScript exception; the corrected floor, blocks and near wall were visible in the running matches. View tests: 155 passed, 0 failed. Screen typecheck: 0 errors. Service tests: 80 passed outside the sandbox, which blocks their temporary localhost listener. No physical-phone, real-wallet or performance check was made.
- **Still open:** the exact approved Chog-head marks are absent from the arena floor; no substitute contour was invented. The connected Genesis card, far-end wall, bushes and physical phone still need visual review. This is a candidate for owner review on the live site, not a claim that every element matches the board. The earlier accepted Skull and Radioactive work does not close the astronaut, cap-spike or eyes/mouths hero tasks.
- **Deployed and checked:** the public page at `https://chogarena.vercel.app/` serves WebGL version `20261019T170012093Z`, and the Render Photon endpoint reports the same build. The assembled judge agreed with the announced winner of both recorded matches. No public PLAY was pressed, so the public room flow itself was not tested in this release check.
**Next action:** wait for the owner's live visual review of the home and arena before changing the composition again. The exact floor marks and remaining board gaps require a separate, evidence-backed pass.

**19 October 2026, 11:55 UTC (earlier local visual production slice).**
- The owner rejected the gap between the approved menu/arena comparison sheets and the previous public build.
  The current task is a measured translation of those sheets into the actual Unity menu and the Photon sample
  map. [The production map and direct visual comparisons](../research/evidence/chog-visual-production-2026-10-19.md)
  record which screen component and map material corresponds to each part of the approved images.
- **Implemented locally:** a candidate large painted Chog-head menu mural; a single cream visitor card using a
  real Genesis #1078 portrait clearly labelled as a collection example; a 3D gold stage with a hand-cut outline,
  pink contact paint and a stable three-quarter hero view; a painted berry slab floor projected onto the existing
  sample floor; and pink/teal/plum arena materials. The 86 sample crate renderers are now hidden at runtime and
  replaced by three batched visual surfaces with dark outlines, berry sides and rounded pink tops. The unapproved
  ornate frame and random menu marks were removed. No source map layout, collider, bush trigger, cover point,
  spawn, Quantum simulation asset or service changed. The connected Genesis card plus sample bush and wall shapes
  remain to be checked or redesigned; the local captures do not yet match the approved boards.
- **Verified locally:** build `20261019T114810487Z-bf9042d09d1743cd84cd33828261f0b2` succeeded with the
  unchanged simulation stamp `f4a81f1ddbd919b6`; its desktop home and touch-emulated bot match were captured.
  View tests: 155 passed, 0 failed. The screen assembly typecheck reports 0 errors. The match ran through recruit
  preparation into gameplay; the only browser console error was Chrome's known non-cancelable touchstart warning.
  There has been no physical-phone test, real wallet check or performance measurement for these new bitmaps and
  batched cover mesh. The public site still serves the old build.
- **Owner feedback received:** the home hero is too far right; the existing stage tile is already sufficient and
  must not receive further platform or stamp work; `SEE EVERY HERO` duplicates the rail. In the arena, the floor
  visibly combines the sample brick language with the new slab language, the exact Chog marks are absent, and the
  lower boundary wall reads as broken.
  The renderer and composition diagnosis is recorded in
  [the visual runtime contract](../research/evidence/chog-visual-runtime-contract-2026-10-19.md). The floor issue
  is specifically a shader multiplying the original `WoodDark` albedo by the slab texture. The lower band is the
  sample's lower `CenterWall`, painted through shared material roles and global foot shading.
**Next action at that point:** map the live renderers to ground, wall-top, wall-face, edge, cover, bush, objective and HUD
roles. Then replace the home and arena presentation as complete systems against the approved boards, beginning with
the home composition and the single ground material family, not isolated UI or palette changes. Capture each
result beside the matching board before review. The Skull/Radioactive corrections accepted earlier do not close the
astronaut, cap-spike or eyes/mouths hero tasks.

**18 October 2026, 20:37 (the home direction made real, deployed `20261018T202454468Z`, same simulation stamp).**
- **Implemented in the game, not only on boards:** the home now gives the Genesis stage more room, puts the
  compact icon rail around it, frames it with Chog purple, pink and gold marks, and removes button shine,
  stacked lips, bounce and duplicate outlines. The wallet card, connection state and player data remain the
  real values already used by the game.
- **Built and checked:** `20261018T202454468Z-081da580762d4da0a41dfd534dd4ad8e` opened its new home and then a
  local touch match against bots. View tests: 155 passed, 0 failed. The judge replayed two recorded matches
  with the announced winner; the service suite passed 80 of 80 outside the sandbox that forbids its local port.
  The public address now serves this build and its home was opened without pressing PLAY.
- **Still not checked:** a physical phone and a real wallet connection. The 3D hero audit remains open after
  the accepted Skull and Radioactive correction: full astronaut costume, cap spikes, then eyes and mouths.
**Next action:** the owner's eye on the public home, then the next approved Genesis correction or the phone
measurement with `?perf=1`.

**Since 12 October 2026, 01:36, the game has a public https address** through a tunnel from
the development Mac, allowed by the user that night: the same program a host would start
(`server/src/host.js` on the assembled `site/`, with the judge), reachable while this Mac and
the tunnel run; the address is drawn at random at each start of the tunnel
([docs/DEPLOY.md](DEPLOY.md), "A public address from this Mac", gives the commands and the
limits). Seen through it: the page, the build, the portraits, the services, and one run from
the menu into a match in the headless browser as a recruit. Not seen: a phone, a real
wallet, Photon calling in. The user confirmed D34 and D35 that night ("je suis tes reco si
elles sont pertinentes pour un produit qui va au market sur le long terme"), asked whether
the game could be deployed on GitHub Pages (no: it runs nothing, see DEPLOY.md), said they
had not understood what to do in Photon's dashboard (explained step by step in the chat, and
offered to be done for them in their browser), found the Chog body "pas mal" but improvable,
and asked whether the wallet on a phone is ready to test and whether Reown is still the
reference. **Written that night, entries of 12 October below:** the wallet connection that
stays in the phone's own browser (Reown AppKit loaded by the page on request; the user gave
a project id, and the window lists real wallets through the public address); a tower and a
core for each team, with the match ended by a fallen core; a body drawn after the official
Chog (slim, pink mittens and feet); and the container for a host (Dockerfile, never built).
**Since 03:17 the secured path runs with Photon's real cloud** (entry of 03:00 below): the
user saved the dashboard, Photon asks us who each player is and sends us each match, and
the judge gave its first verdict on a record that came from Photon. A private repository
exists (`https://github.com/RedGnad/chogathon`). The user found the new body worse than the
first (no head, no spikes, no face left in 3D): the first body is back in the game and a
whole 3D Chog was drawn as pictures only, with three heads
(`research/evidence/chog-full-3d-heads-blender-2026-10-12.png`).
**Decided by the user on 12 October after seeing them:** the head H3 (thick blades, a row
behind, head turned up 20 degrees) is the direction. The Genesis picture laid over the 3D
face is refused. For a hero to look like its Genesis, **the traits are to be modelled in 3D
by us, recognisable, a few at a time**, polished and extended over time; they asked for my
view before any work starts, and allowed the push of the repository. Nothing of H3 is in the
game. What the collection asks for, counted that day by
`tools/genesis-art/trait_coverage.py` and looked at on sheets of real pictures
(`tools/genesis-art/trait_sheet.py`): 66 Head values are 24 shapes, and one cap mesh, with
another colour and print each time, is the hat of 951 Genesis (28 values; 1,204 hats are
right with it, counting the 253 Genesis that wear none); 4 shapes give 1,531 right hats, 10
give 1,834. The 58 Eyes values are 49 kinds: four objects (laser eyes 277, glasses 161,
visors 157, VR headsets 51) and 45 drawn looks. 15 mouths. With 6 hat shapes and 14 eye
kinds, 1,195 Genesis have the right hat and eyes; with 10 and 20, 1,587; with 15 and 30,
1,928. Clothing is the long family: 96 values, 38 of them worn by 20 Genesis or more, and
some are whole costumes with a hood or a helmet (shark, astronaut). 100 Genesis stand on
another base (Skull, Radioactive, 1:1) and need their own head. The grouping into shapes
was checked on pictures for the caps, bucket caps, beanies and headbands; the rest is read
from the names.
**The user said go to the view given on the traits that day** and added that the special
Chogs (the skeletons, the 1:1, the radioactive ones and any other) must be made too. The
plan they accepted: the H3 base polished first (softer spikes; the eyes and the mouth leave
the base, they are traits); then their own Genesis #1287 whole (TISM cap, Anime glasses,
gold tooth, red skin) and five very different ones, as pictures beside their NFT, for them
to judge the level before parts are made in number; then the parts by the number of Genesis
they dress (hats, eye objects, the most worn looks, the mouths), clothing first as a colour
and a few cuts, the prints of the caps in a second pass; costumes, accessories and the 100
Genesis on a special base last, each special base with its own head (a skull and a skeleton
body for Skull; a melted, gradient Chog with horns or a gem for Radioactive; ten made one by
one for the 1:1). Rules: a drawn look or mouth is a flat coloured shape laid on the curved
face, made by us; a trait that is not modelled yet is never replaced by a wrong one, the
hero shows what exists and its card says which traits are in 3D; the NFT picture stays in
the menus and never goes on the model; a screen shows the hero from close (at the pick of
the hero, beside the NFT picture, and at the victory); every batch is shown as pictures
before the game. Target given for 23 October, an estimate: 10 hat shapes, 20 eye kinds, the
15 mouths and clothing as colours. **Started:** the base and #1287, as pictures only
(scripts under `tools/blender/`, outputs under `.artifacts/chog-body/traits/`).
**Waiting on the user:** a try of CONNECT WALLET on their phone through the public address, since
nothing after the pick of a wallet was ever run; the creation of the service on Render from
the repository (`render.yaml`); and whether to make a second Photon application for
development, since the test entries `?token=` no longer give a Genesis now that every match
passes the admission.
**Since 06:10 that day the bots defend and push the structures** (build
`20261012T035911141Z`, assembled in `site/` and served by the public address;
[game/README.md](../game/README.md), "A tower and a core for each team"). A structure that
is hit calls the bots of its team to the nearest attacker; a team with more heroes alive
pushes the other team's tower, then its core, and the bots follow a player who goes under
it. The player reads "YOUR TOWER IS UNDER ATTACK" for as long as it lasts
([capture](../research/evidence/tower-under-attack-alarm-headless-2026-10-12.png)). The
structures were lowered to 8,000 (tower) and 12,000 (core) after watching the bots:
`tools/quantum/watch_bots.py` replays one record with other seeds, each a match of bots in
under a second. With six bots a tower falls in 8 matches of 10 and no core falls (the coins
end the match after about two minutes); with an idle player on one side that side lost its
core in 2 matches of 6. 29 simulation tests, 68 view tests, the judge agrees with the game
on the two records made with this simulation (the two older records no longer replay the
same and were set aside). **Not seen:** a structure destroyed in a match played in a
browser, the bots coming back to defend.
**Found on the way, to fix before stakes go public:** the judge only gives the right answer
for a match played with the same build of the simulation. The public instance judged the two
test matches above, which Photon sent it, with the judge of the previous build: same winner
by luck, but another last tick (3,144 for a match the game ended at 3,177) and other scores.
Nothing tells the judge today which build a match was played with, and two players whose
browsers hold different builds can meet. **Written that morning, the guard:** a build names
itself to Photon with its own stamp (`Chog.Screens.BuildIdentity`, the stamp set as the
version by `QualificationBuild`), so only pages of the same build meet; the page reloads
itself once when the server serves another build; the services refuse to open a room of
another build (`AnotherBuild`) and give a void verdict for a match played with another build
than the one they judge. Tested in the services (69 tests), built (`20261012T041344613Z`,
assembled in `site/`). **Not seen:** a match through the public address with it, so it is not
yet known that the game really gives the stamp to Photon.

**14 October 2026, 21:00.** The session of the 12th ended without warning and everything
that ran on the Mac stopped with it (tunnel, public instance, local services). The user
validated: commit and push the work (and push from now on without asking), verify the guard
with one headless match through the public address, and go on to the 3D parts in number
after seeing the second batch (`.artifacts/chog-body/traits/overview_six.png`: #1287, #1241,
#80, #1408, #1078, #83, #1948, each beside its picture and seen from the game camera; the
brim of the cap shortened and raised so the glasses show under it). Seen on that sheet: from
the game camera a hero reads by its hat, its colour and the traits with volume (visor,
lasers, bubble, rainbow); the drawn looks and mouths are hidden by the angle of the head, and
show on a close view only. Correction: several Head values named "Cap" are drawn as bucket
hats on the pictures of Side=Right (Balls Cap, #1078, among them), so the count of 951
Genesis for one cap mesh given above is too high by at least 313; to be counted again from
pictures. `tools/release/check_all.sh` passed that evening (29 simulation, 68 view, services,
54 contract tests). The tunnel was started again and got another address
(`--url 'https://'`, which ngrok's manual gives for the account's dev domain, also drew a new
one). The two agents that made the 3D parts were run in parallel and started three Blender
processes at once; the user cut the session before the Mac crashed. Since then: one 3D agent
at a time, one Blender at a time, with 4 threads. Found by the hat agent before it stopped,
from the pictures: caps 592, bucket hats 490; the 10 most worn hat shapes give 1,850 Genesis
their right hat (the 253 without one counted).

**15 October 2026, 08:00.** The tunnel and the public instance now run apart from the
session (`nohup`, logs in `.artifacts/services-public/ngrok.log` and `public.log`), so a cut
of the session no longer changes the address:
`https://a6c8-92-88-170-244.ngrok-free.app`. With the user's word, its two addresses were
written in Photon's dashboard ("Chog Arena": the authentication URL, and `WebHookBaseUrl`
of the Quantum 3 plugin) and seen saved. **The build guard is seen working end to end:** a
match played by the headless browser from the local test server (`browser_check.sh match`,
port 8084, build `20261012T041344613Z`) reached Photon, Photon asked the public instance
who the player was (200), and sent `game/create` with `AppVersion` `20261012T041344613Z`,
which the instance accepted; the record holds that version and the judge replayed it (0.73 s).
Its verdict is void because the run stopped the match before its end, as it should. In that
replay the bots of the other team brought down the player's tower (tick 2,681): **the first
structure fallen in a match played in a browser**, seen through the judge, not on screen.
A visit through the public address itself (`browse_public.sh`) loaded the game, which logged
`CHOG_BUILD version 20261012T041344613Z`, but stayed on the menu: Reown's relay refused the
page 213 times ("Unauthorized: origin not allowed"), since the Reown project allows the
domains it was given and the address is new. Reown's manual
(`https://docs.reown.com/cloud/relay`) allows a wildcard per label (`https://*.ngrok-free.app`)
and says a change takes 15 minutes. **Running on the Mac:** the tunnel, the public instance,
the local services (8090) and the test server (8084, `http://10.169.166.151:8084/` on the
local network), one 3D agent (hats, clothing, eyes and mouths, pictures in
`.artifacts/chog-body/traits/batch-2/`).
**Next action (dev):** the 3D sheets shown to the user; the fall of a structure seen on
screen; numbers tuned by playing.
**Waiting on the user:** the domain of the game in Reown's dashboard (a wildcard for the
tunnel until Render, or the exact address each time); Render, to stop depending on the Mac
and on the tunnel's address; a try of CONNECT WALLET on their phone.

**15 October 2026, 09:30.** The user added the domain in Reown's dashboard (which entry is
not known here). A visit through the public address in the headless browser
(`browse_public.sh ... connector`, `.artifacts/unity/check-public-reown-connector/`) opened
Reown's window over the game with its list of wallets, then closed it, and the game heard
both; **no line "origin not allowed" this time** (930 in the visit of 08:00). Not seen: a real
wallet app answering, which only a phone can show.
**The 3D parts in number exist as pictures** (one agent, one Blender at a time;
`tools/blender/chog_trait_hats.py`, `chog_trait_faces.py`, `make_chog_traits_batch.py`,
`make_chog_traits_review.py`; [tools/blender/README.md](../tools/blender/README.md), "batch 2").
Built: 10 hat shapes, each cap and bucket hat with its word or logo; 30 eye kinds; the 15
mouths; the 32 Body values worn by 20 Genesis or more, as colours on three cuts (tee, jacket,
track). Counted again by `make_chog_traits.py -- --coverage` that morning: **on the base Origin
(1,869 Genesis) hat right 1,791, eyes 1,869, mouth 1,869; hat, eyes and mouth together 1,791;
the clothing too 1,110.** Clothing is the gap: 989 of the 1,762 Genesis that wear one have a
built part; left are the costumes (Gangster 41, Duck Float 29, Banana 25, Shark 25, Astro 24,
Dragon 23) and many Side=Left values worn by 11 to 18 Genesis each (hoodies, sleeveless,
jerseys, tees). Hats not built: Clown Nose 31, Plumber 25, Robber 24 and small ones. Largest
dressed hero 7,994 triangles (limit 8,000). Looked at on the sheets
(`.artifacts/chog-body/traits/batch-2/*.png`, the random one kept as
[evidence](../research/evidence/chog-traits-batch2-random-twelve-2026-10-15.png)): from close,
the hat, eyes, mouth and colour of the clothing read as the Genesis; weak are the Ushanka (ear
flaps barely show), the curly wigs (smooth, not fluffy), the Spartan crest (a thin stick),
prints on clothing and some cap prints (listed in orange on each sheet). From the game camera
at phone size: the hats, beams, visors, goggles, headsets, glasses, the bubble and the rainbow
show; the drawn eyes and mouths do not. **Nothing of it is in the game**; the older renders of
#1287 and `overview_six.png` are out of date (shared numbers were lowered for the budget).
**A match played in a browser was ended by a fallen core, seen on screen** (09:23 to 09:27,
headless browser on the local test server, through Photon's cloud): the player's tower fell,
then its core, and the game showed "YOUR CORE FELL" and "Defeat"; the judge's verdict on the
record from Photon agrees (tower at tick 2,599, core at 3,644, team 1 wins 9 to 6, not void).
Pictures in [game/README.md](../game/README.md), "A tower and a core for each team".
**Next action (dev):** the sheets to the user for their yes before any import in Unity;
numbers tuned by playing (the player's team, a hero short since the player stood still, lost
its core in under two minutes, as `watch_bots.py` predicted).
**Waiting on the user:** their yes on the 3D batch; CONNECT WALLET tried on their phone through
`https://a6c8-92-88-170-244.ngrok-free.app`; Render; testnet or mainnet for the contracts, and
the wallet that deploys them.

**15 October 2026, 11:15.** The user answered:
- **3D:** yes, to be improved. Hats must hide the spikes (today the leaves go through them);
  some Head values are hair or something else and replace the spikes; the weak points listed
  (Side=Left clothing, costumes, Ushanka, wigs, Spartan crest, prints) are to be done. Mouths
  and clothing stay in the game if they cost nothing, else only on the close views. One agent,
  one Blender at a time, works on it (batch 3, `.artifacts/chog-body/traits/batch-3/`); it
  waits while `.artifacts/heavy-job.lock` says `busy` (written during Unity builds).
- **Their phone:** CONNECT WALLET through Reown opened MetaMask, they approved, and back in the
  browser the game still said CONNECT WALLET. Cause found and the fix built: see
  [game/README.md](../game/README.md), "Tried on the user's phone on 15 October". Build
  `20261015T085635226Z` (assembled in `site/`, served by the public instance): on a phone
  without a wallet, OPEN IN METAMASK is the main button again and OTHER WALLET opens Reown's
  window. Their Genesis sits on a Ledger, reachable from a computer; Ledger documents the Nano X
  with MetaMask mobile over Bluetooth, not the Nano S Plus.
- **Contracts:** testnet first, deployed with a development wallet of theirs whose key stays on
  their machine (see [contracts/README.md](../contracts/README.md), top).
**Next action (dev):** the user tries OPEN IN METAMASK on the phone; a deployment script for the
testnet (test token, a stand-in for the collection, the wager first) and the game and services
told which chain to use; the 3D batch 3 sheets to the user.
**Waiting on the user:** the phone try; the public address of the development wallet, testnet
MON from `https://faucet.monad.xyz` on it, and the keystore made in their own terminal.

**15 October 2026, 12:15.** 3D batch 3 is drawn, as pictures only (nothing of it is in the
game): no leaf goes through a hat any more (each of the 10 hat shapes removes, keeps or brings
out under its rim each line of leaves, and every build fails a Genesis whose leaves cross its
hat); Bob Hair, Rainbow Wig and the hoods replace the crown; the clothing of Side=Left, the
six costumes, Clown Nose, Plumber, Robber, the prints asked for, and the fixes (ushanka flaps,
round curls, Spartan crest, durag hair points, tuxedo lapels). Recounted by me after the agent
(`-- --coverage`, `-- --budget`): on the base Origin (1,869 Genesis) hat, eyes and mouth are
right for all 1,869, and 1,789 have their clothing too (1,110 in batch 2); on all 1,969, 1,921
and 1,821. Largest dressed hero 7,940 triangles (#470), median 6,527, none over 8,000. One
hero is 6 to 10 mesh objects and 9 to 24 materials: that, not the triangles, is what the
import has to merge. Still wrong on the sheets: faceted curls, leaves under a brim splaying
like wings from the front, the bare top of the head inside the open crown seen from above,
lapels and shirt prints lost at the game camera, the Buttcoin bars faint, "10K" reading
"1OK", no word on the tees whose word changes per Genesis, no glass on the astronaut helmet,
no camouflage on the Army Vest, small dragon wings. Details and commands:
[tools/blender/README.md](../tools/blender/README.md), "Batch 3"; sheets
[before and after](../research/evidence/chog-traits-batch3-spikes-before-after-2026-10-15.png),
[twelve at random](../research/evidence/chog-traits-batch3-random-twelve-2026-10-15.png), and
the rest in `.artifacts/chog-body/traits/batch-3/`.
**Next action (dev):** the sheets to the user; on their yes, the import into Unity with each
hero merged into few meshes and materials, measured on their phone with `?perf=1` before and
after; the testnet deployment script meanwhile.

**15 October 2026, 12:45.** The wager is ready for Monad's testnet, not deployed: a test token
(`contracts/src/testnet/TestChog.sol`, "TEST CHOG", a tap of 1,000 a day), a deployment script
for the user's own terminal (`contracts/script/DeployTestnet.s.sol`, refuses any chain but
10143, also funds two throwaway test wallets), a referee key made on this Mac and never shown
(address `0x5375243CeE1b2adB0986A332dC6131AEbB097b75`), the services told which chain the wager
is on while the Genesis stay read on mainnet (`WAGER_NETWORK`, `WAGER_TOKEN`,
`WAGER_RPC_URLS`; `--wager-network`, `--wager-token`, `--wager-rpc`), the public instance able
to take it (`serve_public.sh --testnet-wager`), and the game asking the wallet for the chain the
services name, calling the stakes TEST $CHOG, offering GET 1000 TEST $CHOG, and letting a
wallet without a Genesis sign in to stake as a recruit (a product choice made alone, so that a
judge without a Genesis can try a stake). The stand-in wallet of the headless browser can now
sign real transactions with a throwaway key (`tools/unity/test_wallet_tx.mjs`, checked on a
local chain). **Seen** on a local chain started as the testnet (`anvil --chain-id 10143`, the
real deployment script, Multicall3's code copied from the testnet), with Photon's real cloud
and bench services on the loopback address sharing the public instance's session secret: the
menu offers SIGN IN TO STAKE TEST $CHOG to a wallet without a Genesis, the preparation screen
offers GET 1000 TEST $CHOG and STAKE 10 TEST $CHOG, the wallet is asked for `0x279f`, the
wallet ends with 990 tCHOG and the wager holds 10, the stakes are locked at START. Simulated
against the real testnet without sending: 2.42 million gas, at most 0.49 MON, plus 0.25 MON to
each test wallet. Build `20261015T103308125Z` is on the public address since 12:40 with no
wager set (`GET /api/wager` answers `enabled: false`). Tests: 29 simulation, 71 view, the
services, 58 contract (4 new). **Not seen:** anything on the real testnet, a match paid out on
it, a real wallet (MetaMask) staking.
**Waiting on the user:** the development wallet imported in their terminal, about 1 MON of
testnet MON on it, and the one command of `contracts/README.md` ("On Monad testnet").
**Then (dev):** `testnet_deployment.sh`, the public instance with `--testnet-wager`, two
headless pages staking against each other through the public address and Photon's real cloud,
judged and paid on the testnet; then the user stakes from MetaMask.

**15 October 2026, 14:20.** The user answered:
- **3D:** better, but not done case by case: some beanies hide every spike in the art (seen on
  #4 Choggie Beanie and #39 Russian Hat, where batch 3 shows spikes all around), the spikes are
  a little long, Bob Hair is hair under a red headband (not hair with a red top), the Spartan
  helmet is badly made, and others. One agent (one Blender at a time) draws batch 4: each of
  the 67 Head values decided from the pictures of its own Genesis, not from its shape family,
  in `.artifacts/chog-body/traits/batch-4/`. Still pictures only.
- **Next big work after the 3D:** the attacks must show what the Genesis shows, in 3D where it
  does not cost performance. Today the special is drawn from the Eyes (flat shapes, one unlit
  material), while the basic attack and the class move of each class still use the sample's
  effects (`brute.strike`, `ranger.shot`, `controller.bolt`, and the charge, dash, slow zone)
  and represent no trait; the Head, the trait seen most from the game camera, decides nothing.
- **Testnet:** fine if moving to mainnet is trivial, otherwise straight to mainnet. For the
  wager it is: the same contract, and the mainnet path still works with today's build. **Seen**
  on a local copy of Monad mainnet (`wager_bench.sh`, the real $CHOG contract copied, build
  `20261015T103308125Z`, Photon's real cloud): the services name Monad (`chainId` 143,
  `test: false`), the wallet is asked for `0x8f`, the panel says STAKE 10 $CHOG with no test
  tap ([capture](../research/evidence/wager-mainnet-copy-staked-2026-10-15.png)), the stake
  is made and locked at START, the judge calls the match off, GET IT BACK pays it back and the
  wallet holds its 10,000 $CHOG again
  ([capture](../research/evidence/wager-mainnet-copy-given-back-2026-10-15.png)). The match
  was called off because on this bench Photon's custom authentication and webhooks reach the
  public instance, which reads the real chain, where the bench wallet holds no Genesis: the
  page plays as a recruit while the stand-in declared Genesis 1795. A paid win between two
  wallets was last seen on 11 October. For the rental and the shop the testnet is not a short
  way: the collection is not on it, and a stand-in collection plus services reading two chains
  would be thrown away; they stay rehearsed on local copies of mainnet and go to mainnet.
- **Phone:** they asked whether the earlier failure is fixed: the cause and the change are in
  [game/README.md](../game/README.md) ("Tried on the user's phone on 15 October"); the page
  posing as a phone was seen offering OPEN IN METAMASK, never MetaMask itself.
- This Mac's local address changed to `10.159.150.151` (another network); the public address
  answers. `wager_bench.sh` now passes extra arguments to the services and takes a `DATA`
  folder, and gives the second test wallet $CHOG too.
**Waiting on the user:** their eye on the batch 4 sheets; the testnet deployment (unchanged);
the phone try of OPEN IN METAMASK.

**15 October 2026, 15:05.** 3D batch 4 is drawn, as pictures only: every Head value decided
from its own pictures (`HEAD_VALUES` in `tools/blender/chog_trait_hats.py`, 53 lines of value
and Side; what each picture shows in `tools/blender/head-values-from-the-art.md`); on Side=Left
no cap, beanie or Russian Hat shows a spike; on Side=Right caps keep a few short points on the
far side, no longer bent like wings; spikes 30% shorter; Bob Hair is yellow curls under a red
band; the Spartan helmet has a brow, a nose guard, cheek guards and a taller plume; the Crown
has a floor seen from above; "10K" reads 10K. Recounted by me: on the base Origin hat, eyes
and mouth 1,869 of 1,869, clothing too 1,789; largest dressed hero 7,924 triangles (#470),
median 6,569, none over 8,000. Looked at on every sheet
([before and after](../research/evidence/chog-traits-batch4-before-after-2026-10-15.jpg),
per value [1](../research/evidence/chog-traits-batch4-per-value-1-2026-10-15.jpg),
[2](../research/evidence/chog-traits-batch4-per-value-2-2026-10-15.jpg),
[3](../research/evidence/chog-traits-batch4-per-value-3-2026-10-15.jpg),
[4](../research/evidence/chog-traits-batch4-per-value-4-2026-10-15.jpg)). Still wrong: the
points of Side=Left heads are in the skin colour where the art draws them cream (Plumber, Clown
Nose of Side=Left), under caps 1 or 2 points where the art shows 2 to 4, the Spartan's cheek
guards narrower than drawn and its eyes not cut, the Bob Hair band less frilled than drawn,
the Durag keeps seven points of hair the art does not show (asked in batch 3), no glass on the
astronaut helmet; 17 rare Head values (37 Genesis, all on Skull and Radioactive heads) not
modelled. Details: [tools/blender/README.md](../tools/blender/README.md), "Batch 4".
**Next action (dev):** the sheets to the user; on their yes, the import into Unity.

**15 October 2026, 16:00.** The user judged batch 4 not coherent (long spikes on some, two on each
side, none behind, some spikes and hair unlike the pictures, the crown worse) and the work done from
one angle, without reasoning in 3D. Batch 5, pictures only: every head is now judged turned around
(`render_chog_traits.py --only turnaround`: eight turns, from above, the game's two angles; sheets by
`make_chog_traits_review.py turnaround`). Seen that way, batch 4 kept pairs of the crown's lines under
each hat: from behind and above, two spikes on each side, none at the back, a collar at the neck. Now a
hat is treated as a volume on a spiky head: under every hat of Side=Right that lets the spikes out, one
row of nine leaves is planted under the rim of the hat as built (found by rays out of the head), the
same for all, longer under bucket hats; the heads of Side=Left are cream, as drawn; the crown is an open
band with the head and a short top leaf inside. 48 Genesis (one per value and Side) built, none failed;
coverage unchanged; largest hero 7,924 triangles, median 6,180. Details:
[tools/blender/README.md](../tools/blender/README.md), "Batch 5";
[before and after, turned around](../research/evidence/chog-traits-batch5-before-after-turned-2026-10-15.jpg).
Still wrong: Bob Hair and Rainbow Wig as puffs instead of one wavy mass in blocks of colour; the
bucket hats' row shows only its points from the game camera; the bare head of Side=Left keeps the
whole star; the durag's row looks like ears from the front; the Spartan helmet.
**Next action (dev):** the sheets to the user; the hair; on their yes, the import into Unity.

**15 October 2026, 17:15.** The user: no push while the 3D is tuned; batch 5 right on the caps,
wrong on others with the same head (bucket hats, durag behind, Side=Left caps), the crown's lone
spike; the process is wrong. Causes found: the row under the rim silently dropped the leaves that
crossed the hat (2 of 9 kept under bucket hats); the pictures of Side=Left had been read as showing
no spike, when their spiky head fills the frame around the face. Batch 6 (pictures only, not
committed): one head for all, no leaf dropped (moved until it fits, or the build fails), and a check
on every hero that spike tips are seen all around from above and from the game's angle; hair as big
clumps. 49 Genesis built through both checks; coverage unchanged; largest hero 7,984 triangles.
Details: [tools/blender/README.md](../tools/blender/README.md), "Batch 6".
**Waiting on the user:** their eye on the batch 6 sheets.

**15 October 2026, 20:10.** The user found batch 6 good enough for now, to be improved later
(committed and pushed with three sheets as evidence), and asked for the next steps and what their
own Genesis is still needed for, since they would like to sell it soon. Read on the hackathon page
that evening ([facts](../research/evidence/chogathon-site-facts-2026-10-15.json)): submissions are
open until 23 October 15:00 UTC through a form (banner, description, demo video of three minutes
at most, app link, repository, judge access instructions), and judges score after that, over about
seven days, from the description, the video and the links. Nothing asks a participant to hold a
Chog. What depends on a Genesis held in a real wallet: the first run of the holder's path on the
real chain (never done with a real wallet yet), the video's holder path, a stake in $CHOG on
mainnet with a Genesis, and the loan pass a judge without a Genesis would play with, which stops
when its lender no longer holds the Genesis (`server/README.md`). The test benches do not depend
on it: they take Genesis 1795 from whoever holds it on the local copy (`tools/contracts/on_copy.sh`).

**15 October 2026, 21:50 (the night the user asked to go on alone).** The user said yes to the 3D
in the game and to the attacks drawn from the Genesis ("bien beau, accurate, et optimisé"), noted the
menu was still the sample's, and asked that the project be taken as far as possible without them,
keeping for them what needs them. Done since, built and seen in the headless browser on this Mac's
local address (no phone yet):
- **The 3D heroes are in the game** for the 1,869 Genesis of the base Origin, from a library of parts
  (`game/README.md`, "The 3D heroes"; checked against every one of those Genesis at export); a recruit
  is a bare grey Chog; the 100 Genesis on another base keep the earlier look until their heads exist.
- **The screens wear the Chog identity** (`game/README.md`, "The look of the screens"): the menu, our
  panels and the team preparation; the player's hero turns in 3D on the menu.
- **The basic attacks show what the hat gives** (`docs/ATTACKS-FROM-THE-ART.md`): 22 objects and the
  hats themselves, seen beside their heroes and thrown in a match.
- The user's questions of the evening were answered in the chat (why the robber's spikes differ from
  the plumber's: a rule taken from one angle and a check too weak; the fix written down, not made; the
  development wallet; the plan).
**Not seen:** any of it on a phone, its cost there (`?perf=1`), a strike of the brute filmed.
**Waiting on the user:** their eye on the menu and the heroes; the phone; the testnet deployment.

**15 October 2026, 23:20 (same night, continued alone).** Built, seen in the headless browser, pushed,
and served by the public address (build `20261015T210119927Z`):
- **The end of a match** shows the result in Koulen and the player's hero in 3D, cheering or slumping
  (`ChogResultScreen`); names over heroes, hit numbers and the alarms of structures are in Koulen; the
  touch sticks are lavender, pink and gold.
- **Under the hero of the menu, what its basic attack throws and from which trait** ("THROWS SNOWBALLS
  FROM ITS RUSSIAN HAT"; library format 4); the classes read BRUTE, RANGER, CONTROLLER on the
  preparation screen.
- **A guide of three cards at the first visit** (what the Genesis does, the controls on a phone and a
  computer, how a match is won) and a HOW TO PLAY button (`ChogGuide`); scripted visits pass
  `noguide=1` (`browser_check.sh` adds it).
- **When a special fires, the eyes it comes from pop up in 3D above the hero** (`ChogSignatureFlash`):
  seen on the viewer page (`?heroes=...&flash=1`), not caught in a match picture yet.
- **A census of the submitted projects** (4 of 451 applications; the nearest is Chog World, a social
  island) and **a draft of the submission** (`docs/SUBMISSION.md`), every claim checked against the game.
- **3D batch 7 as pictures, on the branch `3d-batch-7`, not in the game** (`tools/blender/README.md`,
  "Batch 7"): the whole crown through the robber's knitted mask; the Skull base (60 Genesis, ready in the
  export of the branch, checked against every Genesis); the Radioactive base (30, gradient skins, horns,
  gem, third eye, seal, laser, mushrooms; the gradient needs a change of the game before it can go in).
**Waiting on the user:** yes or no on batch 7 (robber, skulls, radioactive); the rest unchanged.

**16 October 2026, 00:30.** The library of the game is in format 5 (a skin may run to another colour at the
top; nothing changes for the heroes of today), so the Radioactive base can go in. On the branch
`3d-batch-7` the export covers 1,959 Genesis (Origin, Skull, Radioactive; only the ten 1:1 left), all
checked; a preview build made with it on the local network drew skulls, gradients, horns and the robber's
crown as the pictures do (`research/evidence/chog-traits-batch7-in-game-preview-headless-2026-10-15.png` on
the branch); the game's own library was put back and the main build served again. To put batch 7 in
the game after a yes: merge the branch, run `export_chog_parts.py`, build. `tools/release/check_all.sh`
passed (29 simulation, 80 view, services, 58 contracts). The public address serves build
`20261015T221411009Z`. Learned: with the user's Chrome open (about 5 GB) the Mac swapped 7 GB after many
builds; the Editor was restarted (memory note `unity-isolated-compile`).
**Next action (dev):** the user's eye on the night's work, the phone with `?perf=1`, then batch 7 or
its corrections; the Genesis leaderboard (`GET /api/career/top` exists, no screen shows it yet).

**16 October 2026, 05:05 (the user said "go, do what there is to do alone"; they do their steps
tomorrow: the phone, batch 7, the testnet, the submission draft).** Built, seen in the headless browser,
and served by the public address (build `20261016T025947729Z`, checked with `check_public.sh` and one
visit that played a few seconds of a match through the tunnel):
- **The arena in the colours of the Chog night** (`ChogArenaSkin`, `game/README.md`, "The arena, the
  leaderboard and the 3D structures"): indigo floor, lavender and deep purple crates, mint plants; five
  palettes compared in matches (`research/evidence/arena-palettes-headless-2026-10-16.jpg`).
- **TOP GENESIS** on the menu (`ChogHall`): the ten Genesis with the most wins from the services, with
  picture, wins, matches and mastery. The public instance has no recorded career, so it says so
  (`research/evidence/top-genesis-no-career-yet-headless-2026-10-16.png`); seen filled with five example
  careers on a test bench of its own (`...-example-careers-...`).
- **A 3D tower and core, as a proposal behind `?structures3d=1`** (a Chog head on it: held back like any new
  hero look until the user's yes): a totem with the H3 head, glowing laser eyes and gritted teeth that turns
  to whom it shoots and fires from its eyes, and a pink heart in a crown of Chog leaves that beats faster
  when hit; ruins in greys. Seen turned around in Blender and in a match
  (`research/evidence/chog-structures-3d-turn-2026-10-16.jpg`,
  `research/evidence/structures-2d-and-3d-in-match-headless-2026-10-16.png`). To show it to everyone: make
  `ChogStructureModel.On` true without the option and build.
- **Batch 7, on its branch, ready to merge on a yes** (`54a7488`, pushed): the ram horns made again after
  the pictures, thick, ridged, curling out to the side; the skull narrowing into its jaw under the
  cheekbones, a skeleton's hands and feet in bone; the branch's library exported again with Red Bandana too (1,959
  Genesis built or checked, all matching), the view tests of the branch passing on it, and seen in the game in a local
  preview build (`main`'s own library put back and built again right after). Merging needs no export: merge,
  then build.
- **Seen on film, the brute's basic attack:** its impact shards fly out in front; the strike itself falls
  in the 224 ms the headless browser takes to release the touch, twice: not caught on a frame.
- **Found:** a scripted visit with `?token=` now always plays a recruit (the admission reads the real chain
  through Photon and the public instance), so a Genesis's own special cannot be filmed in a match without a
  wallet that holds one. `tools/dev/typecheck.py` takes new files with `--add` (with `--sources`, which
  takes folders, two new screens were not checked and the build caught their errors).
`tools/release/check_all.sh` passed (29 simulation, 84 view, services, 58 contracts).
- **The ten 1:1 Genesis, on the branch `3d-one-of-one`** (`dd232df`, pushed, started from batch 7): each given a
  look read on its picture and the parts no other Genesis wears (`tools/blender/README.md` of the branch,
  "The ten 1:1 Genesis"): fire with a cape, a skull in flames, The Scream, a zombie with its brain out, Pepe
  in a frog suit, a manga page in greys, an angel with halo and wings, a warrior in a gold helmet, a Chog of
  gold, a devil with horns that throws its trident. That branch's library covers all 1,969 Genesis, every
  one built or checked; the view tests pass on it; seen in the game in a local preview build (`main`'s own
  library put back and built again right after).
**Next action (dev):** the user's yes or no on the 3D structures, batch 7 and the 1:1 (merging
`3d-one-of-one` brings batch 7 with it); the phone with `?perf=1`; then the video's shots.

**16 October 2026, 09:00 (the user tried the public address: "pour les perso 3D c'est vraiment beaucoup
mieux", the gameplay good for a start; a new round of 3D corrections, case by case).**
- **What stands on the head no longer takes the place of the spikes**, on `3d-one-of-one` (`7a8e198`,
  committed, **not pushed** while the user judges the 3D): the ram horns grow from the corners of the brow and
  curl above the head, the demon horns are five among the spikes as drawn, the plunger sits at the front of
  the top, the holy seal is a large gold wheel over the brow, the mushrooms have the colours of #1100, the blue
  laser leaves the brow at 45 degrees, the 1:1 devil's horns sit on its brow; every leaf of the crown is kept
  under all of them (table in that branch's `tools/blender/README.md`). Library exported again (1,970 heroes,
  438 pieces, all built or checked, none failed), 84 view tests pass on it, seen in a local preview build
  (`main`'s library put back and built again: 1,870 heroes, 347 parts). Boards of every value of every base:
  `.artifacts/chog-body/traits/batch-8/`.
- **Answered, nothing started (the user decides):** the coins still decide most matches (a tower falls in 8 of
  10 matches between six bots, never a core); two modes, a siege without coins and the coins of the sample,
  would cost about half a day. The stakes and loans follow one rule (the NFT never moves, every right is read
  by token, payments go player to player); not coherent yet: the wager goes to the testnet while the rental
  and the shop read the real collection (proposed: a mirror of the collection on the testnet kept by the
  services), the free link and the rental overlap, a staked match needs a human on each side, and the judges'
  loan dies if #1287 is sold before judging ends.
- **Found, the slower loading through the tunnel:** the build did not grow (24 MB); that morning the Mac was on
  a phone's shared connection ("Redmi 12C"), and the tunnel gave 0.46 MB/s (one download of the program
  timed from the Mac, 101 s for the 46 MB that ngrok unpacks for a client that does not ask for gzip): about
  a minute for a first visit, a reload of everything after each new build. With 1 GB a month on the free
  plan (about 40 first visits; that test took 47 MB of it), the judges need a host before 23 October.

**16 October 2026, 13:30 (the user: yes to the 3D in the game and to two modes; B, build on mainnet
directly; the Genesis is to be sold the next day, so everything that needs it is tested today; Render with
a ping from cron-job.org for the host).**
- **In the game for everyone** (public address, build `20261016T075943857Z`): every Genesis plays its 3D
  hero (batch 7, the 1:1 and the head objects merged), the tower and the core in 3D, and **two modes**
  chosen beside QUICK PLAY: SIEGE (towers and cores, no coin, 180 seconds, the damage to the structures
  decides) and COINS (the sample's Coin Grab, no structure); rooms are kept apart by mode
  (`<build>-siege`, `<build>-coins`). Seen through Photon's cloud and judged: a whole siege (99 to 80 on
  damage) and a match of coins (12 to 2); ten sieges of six bots replayed: five ended by a core.
  31 simulation, 85 view, 72 service tests; the judge agrees on the recorded fixtures.
- **Mainnet, ready to deploy** (`contracts/script/DeployMainnet.s.sol`, the wager, the rental and the shop in
  real $CHOG; rehearsed on a local copy of mainnet, about 0.43 MON of gas that day; the mainnet referee
  `0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34`; `tools/contracts/mainnet_deployment.sh` then
  `serve_public.sh ... --mainnet`). **Not deployed: waiting for the user's run with their wallet.**
- **Batch 9 of the 3D, on `3d-batch-9`** (committed, not pushed while the user judges it): a second row of
  spikes under every hat, the whole crown of spikes under the crown and the headbands, the laser straight
  out of the brow, socket eyes for six skulls, a skull's grin in two rows of teeth and a radioactive jaw line,
  deeper radioactive colour runs, the six headwear only skulls wear (thorns in gold and silver, laurel,
  taped banana, fedora, turban), the dragon suit with fangs, nostrils and a tail, the banana bent and ridged,
  and the clothing of the 117 Genesis that had none (toad suits, armour, Sci Fi, yukata, Guko, Gojo, cape,
  robe, mummy, vests, stripes, tanks, holed jackets, a blue suit), most of them in a cut already built, what
  it does not draw said in `print_missing`. Library exported: 1,970 heroes, 497 pieces, all built or checked;
  85 view tests pass on it. Boards in `.artifacts/chog-body/traits/batch-9/`.
- **Found:** cron-job.org sets no number of jobs (fair use, every minute at most, 30 seconds a call); Render
  gives 750 free hours a month per workspace and suspends every free service of it when they run out, and
  a free service loses its files at each restart (`https://render.com/docs/free.md`).
**Next action:** the user's mainnet deployment, then the tests that need #1287 (sign-in, career, loan,
rental, shop, a staked match) and the video's shots with it, today; Render; the user's eye on batch 9.

**17 October 2026, 21:55 (the effects of a fight; on `main`: 68c336a, built `20261017T193628403Z`, not deployed).**
- **Asked by the owner:** the licence is no problem (non-profit MVP) and "effects come from a Chog" is a direction:
  explosions, flashes with light, lightning are welcome as long as they respect the traits. **Implemented**
  (`game/README.md`, "The effects of a fight"): every effect is picked and coloured by a trait (`ChogFxRecipes`,
  `ChogFxKit`). The object of the hat bursts in its own way where a basic attack ends (cloth and a puff for a cap,
  coins and sparkles for a coin, lightning from the top of the screen for a Thunder Cap, snow and ice for a
  snowball, and so on for every object); a special flashes at the caster's eyes in their colours, shows its Aura
  (lightning from the eyes for haste) and its rarity (echo rings, Skull, Radioactive, 1:1); the Aura hits its
  target with fire, ice, stars, sparks, electricity, smoke or bubbles, with flames, runes or a poison cloud while
  it lasts; falls, returns, structure hits and structure falls (blasts, a shock wave, smoke; the core's heart
  breaks under a BOOM) have theirs. 23 Cartoon FX Remaster prefabs copied into
  `game/Assets/Chog/Effects/Resources/ChogFx`, pooled and recoloured; light faked with additive flares, rings and
  columns (`ChogLight`) and flickering bolts (`ChogLightning`); no real-time light. A lighter path for phones and
  any quality below Good. View code only: the simulation stamp is unchanged (`f4a81f1ddbd919b6`).
- **Seen** (muted headless Chrome on the Mac's GPU): on a new private test page, `?impacts=1`, `=aura`, `=big`
  (`ChogImpactsViewer`, each case looped and frozen at chosen moments, before and after on one build with
  `fxlevel=0`), on `?specials=1`, and in matches against bots on Photon's cloud:
  [impacts](../research/evidence/fight-fx-impacts-before-after-2026-10-17.jpg) (with the phone's path),
  [auras](../research/evidence/fight-fx-auras-before-after-2026-10-17.jpg),
  [big moments](../research/evidence/fight-fx-big-moments-2026-10-17.jpg),
  [specials](../research/evidence/fight-fx-specials-film-2026-10-17.jpg),
  [matches](../research/evidence/fight-fx-match-before-after-2026-10-17.jpg). No exception in any console. 147
  view tests pass (9 new, `ChogFxTests`).
- **Measured** (`?perf=1`, 123 s of a match each, M1 with Metal, the Mac swapping 11 to 12 GB): main thread busy
  median and 95th percentile 4.7 / 6.9 and 4.6 / 7.6 ms with the effects, 4.6 / 7.1 and 4.7 / 7.0 with `fx=0`,
  4.4 / 7.5 and 4.5 / 6.8 on the phone's path; the build of before 4.2 / 6.4 (its effects) and 4.5 / 6.7 (`fx=0`).
  Frame 17.0 ms (60 Hz) everywhere, worst 284 ms once with the effects, 45 to 69 ms otherwise. No difference out of
  the noise. About ten particle effects start a second in such a match, the cap of 40 skips about 6% of them; the
  managed heap is up to about 7 MB larger; the build's data grew by 0.53 MB compressed.
- **Found on the way:** a lob's end was placed at an absolute height, wrong on any page whose floor is not at 0
  (fixed); a bolt straight from the sky is seen end on by the 75 degree camera, so bolts come down from the top of
  the screen; the first explosions were the size of a third of the screen (cut to about a tower and a half). Once,
  a test page connected to Photon without asking its identity ("Authentication type 'None' not supported", 21:25);
  the next four connected normally; not investigated.
- **Not checked:** a phone (GPU overdraw of the additive flares and particles, the lighter path on a real device),
  the owner's eye on how loud the flashes are in a busy fight, the lasting effects on a target that moves fast.
**Next action:** the owner's eye on the sheets and a match on a computer; `?perf=1` on the phone with and without
`fx=0`; tune the flash sizes from that.

**17 October 2026, 22:30 (aim, towers, core, robustness; deployed: build `20261017T202256588Z`, stamp unchanged).**
- **Asked by the owner:** lobs "impossible to aim" since a click throws at once; does a held button fire on its own;
  how do the references do it. Answered (Brawl Stars: drag shows the arc and the point of impact, release throws, tap
  auto-aims, no hold-to-fire, three ammo; League of Legends: quick cast, quick cast with indicator, normal cast) and
  the owner chose to follow the references. Then: towers "all turned the same way, down", cores too small, more 3D
  traits, the terrain and minions (answered: no lane minions, a Brawl Stars Siege bot at most, terrain and bot change
  the stamp), and "a real audit and research pass" on performance. The owner followed the order by return:
  performance, aim, towers and core, then traits.
- **Implemented and deployed:** the mouse's aim shows all the time in a match (`ChogAimGuide`: dots along the very arc
  of the throw, the landing ring under the pointer within reach, a faint circle of the reach; red without energy;
  hidden when down, outside play, over a button); a click still throws at once, a held button never repeats. The
  lob's own shadow and landing ring were drawn under the floor's planks (y 0.03) and never seen: now at 0.18.
  Towers face the arena and turn their head to the hero they will shoot next (the simulation's rule); the core is
  drawn 1.5 times larger. `PhotonIdentity` assumes the services ask until they answer and asks again when the
  question fails (one failed answer at start used to leave a player refused by Photon until a reload). A room whose
  host is away starts anyway: every player sees the clock and another player's game starts the match 8 s after zero
  (`LobbyCountdown.StartsNow`). Bars, team indicators and attack colours wait for the local hero when joining a
  match under way (12 exceptions on the public site). The fight effects (68c336a) went online with this build.
- **Seen:** matches against bots on this Mac's build (`.artifacts/unity/aim-match-4`, `-5`: the aim at two pointer
  positions, a lob in flight with its landing ring, a tower turning to its target, the larger core; no exception);
  the public site loads the build and Photon takes the identity (`live-6`). On the public site a scripted client
  joined a real room that had waited 22 minutes in team preparation (its host's tab idle): public checks no longer
  press PLAY. Local services must run with `--photon-auth` (Photon's dashboard refuses anonymous clients).
- **Audit:** `docs/PERF-AUDIT.md` (phase 1). The phone of 10 October was main-thread bound near 30 fps; no phone
  measured since the 3D heroes, specials and effects. Quick wins listed (pixel ratio cap, per-frame strings, shader
  warm-up, cheaper effect shader, Unity physics off, logging) and deeper view work (entity view pooling, canvases).
- **Not checked:** a phone; the lobby fallback with two players; the trait audit (an agent is on it).
**Next action:** the perf quick wins and the phone measurement (the owner opens `?perf=1` on a phone); the trait 3D
audit and fixes; the owner's eye on the aim, the towers and the core.

**17 October 2026, 23:30 (night, the owner away and following the recommendations; deployed: `20261017T212207675Z`).**
- **Asked by the owner:** simulation changes are fine when worth it ("on peut changer ce qu'il faut changer"),
  maps will change. The 3D heroes still have problems seen in the app: faces slightly off the head ("feuille en
  suspension"), the astronaut not a full costume like the banana and the dragon, Skull and Radioactive heads not
  faithful nor cute, spikes longer under caps, some eyes and mouths wrong or misplaced.
- **Deployed:** the audit's quick wins (`20261017T204324452Z`: effect shaders warmed under the floor while a match
  loads, a phone's pixel ratio capped at 2, Unity physics off, no per-frame texts, combat log only with
  `combatlog=1`), then `20261017T212207675Z`: the first trait set from the art (cd63001: laser eyes, Side=Left smile
  and frown, angry skull sockets, the Accessory held in hand by 195 Genesis) and attack views kept for the next
  attack (`ChogAttackViewPool`, 2585a5a; `viewpool=0` on a private page). Checked: matches against bots in siege
  and coins, no exception; the public site loads the build (`live-8`, 1,970 heroes, 507 parts).
- **Not measured:** the pool's gain. With the Mac swapping 11 GB, frames of 1.8 s appeared with and without it.
- **Under way:** an agent on the owner's five 3D points; an agent studying the siege map (how the arena is built,
  references, three layouts, a plan grouping the map and the core's collision radius in one simulation change).
**Next action:** build, check and deploy the 3D fixes as they come; the owner picks a siege layout; the phone
measurement (`http://<mac-ip>:8096/?perf=1`).

**18 October 2026, 09:50 (the weekly credits ran out: finalised; deployed `20261018T074507080Z`, stamp unchanged).**
- **Deployed:** the heroes' faces laid on their heads (38048fa: face, line, cheeks, eyes and mouths no longer float;
  measured gaps halved; sheet `research/evidence/hero-3d-faces-on-the-head-2026-10-18.jpg`). Checked: a match
  against bots without exception, the judge agrees, Render serves it.
- **Put aside on the pushed branch `wip/siege-map-and-heroes` (42dea34), not finished:** the siege map A (three
  lanes, siege-only scene and map, baked with 381 colliders), core radius 1.5 and towers needing line of sight
  (simulation, tested headless only), and the start of the heroes' point 2 (Skull and Radioactive heads). To finish:
  check the baked map, bots on it, one WebGL build connected to Photon, both modes, re-record the judge fixtures.
- **Still open from the owner:** Skull and Radioactive heads, full costumes (astronaut), longer spikes under caps,
  misplaced eyes and mouths; the phone measurement. The Mac slept in the night and froze the agents: `caffeinate
  -dimsu` now keeps it awake while it runs (it does not survive a restart).

**17 October 2026, 20:10 (the specials in 3D; on `main`, built `20261017T175516922Z`, not deployed).**
- **Asked by the owner:** some attacks were "not in 3D, just sprites thrown on the attack". **Implemented**
  (`game/README.md`, "The specials in 3D"): the five specials, which come from the Eyes trait, are 3D objects
  made in code, in the colours of the caster's Eyes value and with its own eye drawing: the laser as two
  outlined tubes with a lighter core, glows at the eyes, sparks at the head and a glow on the floor; the flash
  as a see-through wedge of light with a bright edge and a glint on the glasses; the tear as a 3D drop on the
  lobbed arc of the basic attacks (same helpers, shadow and landing ring), splashing into a low liquid puddle
  with ripples; the wave as the hero's two eyeballs rolling forward behind a leaning see-through front; the
  nova as eight eyeballs bursting out over a shock ring. The towers fire the same 3D laser. The flat look
  stays as the fallback (and `special3d=0` on a private page). View code only: the simulation stamp of the
  build is unchanged (`f4a81f1ddbd919b6`), so Photon's version too.
- **Seen** on a new private test page, `?specials=` (`ChogSpecialsViewer`: each Genesis fires its special in a
  loop with the numbers of its own attack data, seen by the match camera at 1280 x 720 scale and from the side):
  [before and after](../research/evidence/specials-3d-before-after-2026-10-17.jpg),
  [six moments of each](../research/evidence/specials-3d-film-2026-10-17.jpg),
  [as an enemy's, on the day floor](../research/evidence/specials-3d-enemy-day-2026-10-17.jpg),
  [six more Genesis](../research/evidence/specials-3d-variety-2026-10-17.jpg). Headless Chrome with the GPU on
  the Mac, muted. View tests: 138 passed, 0 failed (13 new).
- **Found on the way:** eye drawings laid on a ball as layered geometry speckled on that page (their small lift
  is below the depth precision of the picture); the eye is now painted once into a 128-pixel picture per look
  and laid on the ball by the shader. An eye turned with the world's up leaned on screen under the 75-degree
  camera: what faces the camera turns with the camera's up.
- **Not seen:** a real match (Photon's version limit that day), a phone (the cost on its GPU: the effects add a
  few hundred triangles each, pooled; one see-through pass), the second forms and the echo, return and zone
  deliveries in play (they draw through the same code).
**Next action:** the owner's eye on the sheets; a match with specials once Photon accepts the build, and a run
with `?perf=1` on the phone.

**17 October 2026, 18:45 (the look of a match; on `main`: 54f62ae, faf163f; in main's build `20261017T155010541Z`).**
- **Implemented** (`game/README.md`, "The look of a match"): heroes and arena drawn with a diffuse matcap
  (`ChogHero.shader` keeps `_Flash` as it was; `ChogArena.shader` on the pack's materials); each team's key light
  turned to the upper left of its own camera, #FFE8C2, the same full shadow, shown at 75% toward violet, no fog; a
  soft shadow and a ring under every hero (green, blue, red) and structure, in place of the sample's circle
  (`ChogTeamRing`); grading, glow, vignette and FXAA at the quality level Good and up only (`ChogMatchPost`); the
  palette "Chog day" beside the night, default unchanged (`ArenaPalettes`, `?arena=day`); `?matchlook=0` shows the
  look of before on the same build, `?looktune=` tries other numbers, `?camera=` another angle (`ChogCameraRig`,
  read by `LocalPlayerCameraFollow`). 123 view tests pass (16 new in `MatchLookTests`).
- **Seen in matches on Photon's cloud** (muted headless Chrome on Metal, against bots): every step against the look
  of before, night against day, four camera angles (`research/evidence/match-look-steps-2026-10-17.jpg`,
  `match-night-vs-day-2026-10-17.jpg`, `match-camera-angles-2026-10-17.jpg`, `match-camera-faces-2026-10-17.png`).
- **Measured** (`?perf=1`, a minute each): the median frame is 17.0 ms (60 Hz) in every configuration, Good, Fast and
  2560 x 1440 with post-processing; main-thread median 3.3 to 3.9 ms at Good and 2.6 to 3.1 at Fast, before and
  after alike. The Mac swapped 11 to 12 GB of 12 to 13 GB, so one-second frames appear in every run (the look of
  before too): only the medians compare, and no cost stands out of the noise.
- **Camera:** 62 to 65 degrees keep the floor seen around the player and show more of the tower's face and the lower
  half of a hero's face, but the recruit's drawn eyes stay under the fringe; at 55 the near wall hides a hero at its
  base. Default kept at the sample's 75.
- **Not checked:** a phone (frame time, look on a small screen); team 1's side (the lights are set for both, only
  team 0 was played); the menu's hero stage with the new shader (it uses the same material).
**Waiting on the owner:** night or day; whether to keep the post-processing on computers; a camera angle (or the
eyes made to show from above, which is the 3D work, not the camera).
**Next action:** `?perf=1` on the phone with `?matchlook=0` and without; a match played as team 1 to see its shadows.

**17 October 2026, 17:25 (juice and sound, in the view only; on `main`: 73c65d2, fd11ae1, 9740baf, 634985b).**
- **Implemented** (`game/README.md`, "Juice and sound"): pooled fight effects; a 90 ms white flash, a flinch (0.85 high,
  1.1 wide, back in 0.25 s) and, on blows involving the local hero, a 0.06 s hit stop of the poses; damage numbers that
  pop, coloured by who was hit, sized by the share of health, merged within 0.2 s, taken back when Quantum cancels the
  blow; a fallen hero that swells where it fell and bursts in its colours, a return with a Back ease; trailing health
  bars; structures that wobble, flash and burst when they fall; a special-ready pop with a chime; a light camera shake
  (trauma squared); eight pooled audio voices with variants and pitch 0.92 to 1.08, at most two of a clip per 50 ms;
  button clicks from `ChogPress.Pressed`; a menu loop and a match loop; Android vibration. Settings on the device:
  `ChogSounds.MusicVolume` and `SoundVolume`, `ChogCameraShake.Enabled`, `ChogHaptics.Enabled` (the settings screen
  reads them); `ChogSounds.Reward(step)` for the result screen. 18 CC0 files (Kenney, OpenGameArt), sources and
  licences in `game/Assets/Chog/Audio/LICENSES.md`; the sample's arena music (2.3 MB, credited to FreePD, which is
  offline since) is no longer referenced: the build's Sounds went from 2.4 MB to 2.0 MB (Unity's build report).
- **Seen in preview builds on Photon's cloud** (muted headless browser with Metal, `?perf=1`, siege against bots,
  build `20261017T152132143Z`, `.artifacts/unity/check-juice-7`): no exception; in about 150 s, 195 hits on heroes,
  193 flashed (a 3D hero found), 10 on the local hero, 10 hit stops, 42 hits on structures, a tower fell, 35 heroes
  fell and came back, trauma reached 1.0; 42 pooled objects made and 724 reused; 803 sounds played, 610 refused by
  the 50 ms rule (mostly the sample's attack sounds of several heroes at once); the music went from the menu loop to
  the match loop. 123 view tests pass.
- **Not checked:** anything heard (every launch is muted: levels, the loops' seams, the mix are unheard; the files
  were picked from measurements); the flash, flinch, pop and shake as a person sees them (still screenshots only);
  vibration on a phone; the frame rate: in every run that evening frames took about a second once a second, the
  earlier build `20261017T144416439Z` (before the sounds) included, with the Mac swapping 12.3 to 12.7 GB of 13.3 GB,
  so no cost can be read from those runs.
**Next action:** listen to the sounds and the two loops on a computer and a phone and keep or replace them; measure
`?perf=1` on the phone with and without `?fx=0`; then directional impacts and the attacker's kick (DESIGN-BRIEF, Juice).

**17 October 2026, 19:25 (Photon accepts again; the stamp build is online).**
- Photon refused new versions from 18:41 to at least 18:56 and accepted again at 19:17 (the retry script): the
  limit counts versions in use over some time, about half an hour here; Photon publishes no figure.
- **Deployed** build `20261017T165404384Z`, the first that names itself to Photon `sim-f4a81f1ddbd919b6`
  (RELEASE.json `simulation`); Render serves it; a visitor's match on the public site started at 19:23 (an
  admission recorded, the scoreboard running). The judge agrees on the recorded fixtures.
- **The controller's lob:** a whole match with five controller bots and a recruit controller ran with no
  exception in the console; the arc, shadow and landing ring were **not** made out in the captures (the
  headless film is too coarse for a throw). To see on a real screen.

**17 October 2026, 19:00 (deployed, then Photon's limit; the owner's questions on v3, music and attacks).**
- **Deployed and seen online** (build `20261017T155745998Z` on chogarena.vercel.app and Render, the judge
  agreeing on the recorded fixtures): the home, hero sheet, collection of 1,969 (42 pages of 48 portraits,
  one picture each), settings, reveal, scoreboard, team preparation with a 30 s clock that started a match by
  itself, the first match coach, keycaps, the loading page; a whole 3 minute match played headless to its end
  screen. A visitor's match on the public site reached team preparation at 18:50.
- **Found at 18:41: Photon refuses every new application version** ("Too many application versions are in
  use", 32767; docs/DEPLOY.md, "Photon's limit on application versions"). Production kept working. Fixed for
  the future (6059de6): the game names itself by a stamp of its simulation (`sim-f4a81f1ddbd919b6` today,
  the same over two builds), so UI builds open no new version. **Not deployed: a build with that stamp is still
  refused** (tried 18:42, 18:50, 18:56); a script retries every 20 minutes. No deployment until it passes.
- **Built, not seen in a match (blocked by the above):** the controller's lobbed basic attack on a high arc
  with its shadow and a landing ring (tests of the arc pass); `?recruitclass=` on a private page.
- **Answered the owner:** v3 with permit saves no wallet window (two the first time either way, then one) and
  about 0.0102 MON once per contract (102 gwei, our 100,000 gas limit), against three redeployments; a relayer
  saves the need for MON, not windows; a paymaster needs smart accounts MetaMask does not offer on Monad. The
  only v3 that removes windows is one spender for rent, shop and stakes. Decision is the owner's. Music: a menu
  loop (new) and a match loop replacing the sample's, both CC0, not heard by anyone yet.
- **Counted:** every special (the five shapes from the eyes, all 1,969 Genesis) is drawn flat; an agent is
  making them 3D, with an offline preview page since matches of new builds cannot connect. 15 Cartoon FX files
  are still used by ten sample effect prefabs (licence point in docs/SUBMISSION.md, section 05).
**Next action:** when Photon accepts the stamp, deploy the stamp build and see the lob in a match; merge the 3D
specials; replace the Cartoon FX effects.

**17 October 2026, 17:35 (the redesign, screens: the owner validated the mockups and said go; on `main`, built,
seen in headless Chrome at 1280x720, not deployed yet).**
- **Implemented and seen** (builds `20261017T151852241Z` and `20261017T152751287Z`, captures in
  `.artifacts/unity/home-1` to `home-4`, `reveal-1`, `hud-2`): the home screen as the mockup (`ChogHome`: the
  player's chip with the picture and mastery of the Genesis played and the player's name, the logo, the $CHOG held,
  the wallet, the settings; a rail HEROES, TOP, RENT, FRIENDS, GUIDE; the hero on a gold pedestal, its name on a
  ribbon, its kit on four chips; the wallet card with the mastery road and the Genesis of the wallet; mode and
  PLAY); the hero sheet (`ChogHeroSheet`: 3D hero turned by dragging, the NFT, class and kit with the trait behind
  each part, the career on Monad, PLAY IT, RENT OUT, LEND, RENT IT, the second form); renting, offering and lending
  in a centred window; settings (`ChogSettings`: music, sounds, camera shake, vibration, MORE for the sample's);
  the moment a Genesis comes to life the first time a browser plays it (`ChogReveal`, about 4 s, a tap skips);
  the end of a match in team halves with the mastery road and what the match added once recorded (`+N MASTERY`
  from the career before and after, with chimes; not seen yet: no match was played to its end that day); the match
  scoreboard (`ChogScoreboard`: team pills, clock, what is counted); team preparation in team halves, CANCEL in red.
- **Real data only:** the $CHOG pill is the wallet's `balanceOf` read by the services (`/api/rental/account` now
  answers `balance`); the mastery road and the chip read the career the services read on chain; the hall reads
  the top careers. Seen with services started locally with `--pretend-holder` for the Ledger's address
  (`0x80fb...cd64`) and a careers file holding token 1795's career as Render answered it that day (3 matches,
  2 wins, mastery 2, 5 of 8): a local test, never shipped.
- **Pictograms are drawn in code** (`ChogIcons`, 20 of them on a 24 grid with the ink outline and hard shadow);
  shared builders in `ChogUi`; cards are outlined in ink.
- **Not checked:** a phone (layout at other ratios, safe areas), the reveal on a real wallet connection, the
  result screen after a recorded match, the sounds (every run muted).
**Next action:** deploy a build of `main` once the match-look work is committed (its files are still uncommitted in
the shared tree, so every build carries them); then the lobby countdown, the desktop key hints, the collection
grid of 1,969 heroes behind HEROES, the loading screen and the submission banner.

**17 October 2026, 16:30 (the recruit's eyes exported and seen turned around, on `recruit-eyes`; not on `main`, not
built).**
- **Exported** after the owner freed the disk (5.4 GB free; swap 11.8 GB of 12.3 GB; one Blender, `-t 4`):
  `export_chog_parts.py -- --check 5000` wrote 1,970 heroes, 495 pieces, 749 materials, 199,204 triangles in the
  pieces, 123 Genesis built and 1,846 more checked, 0 failed, 0 lacking a part: the counts of `main`. The file
  differs from `main`'s only in the recruit's record: pieces (0, 18, 2, 3) became (0, 18, 2, 3, 109), 109 being
  `Eyes_SideEye`; every byte before and after that record is the same. View tests: 90 passed, 0 failed.
- **Seen turned around** (`render_recruit.py`, from the library itself, before and after;
  [sheet](../research/evidence/chog-recruit-eyes-turnaround-2026-10-17.jpg), full size in
  `.artifacts/chog-body/recruit-eyes/recruit_turnaround.png`): from the front, three quarter and both sides the eyes
  lie on the face, follow its curve, sit level and symmetric about the nose, and glance the same way as on the
  Genesis. **From the game camera (73 degrees, in front) they barely show**: a few dark points under the fringe,
  unreadable at phone size. CHOG #112 (the same head and eyes) shows the same, as does every Genesis with drawn
  eyes: the fringe and the head turned up hide the middle of the face from above. So the recruit no longer has a
  blank face on the menu, the result screen and the hero card (front views), but in a match it looks as before.
**Next action:** the owner's eye on the sheet; on a yes, merge `recruit-eyes` and build. Readable eyes from the game
camera are a separate decision for every hero (a head turned up further, eyes drawn higher or a shorter fringe).

**17 October 2026, 16:15 (the owner: the recruit has no eyes; branch `recruit-eyes`, not in the game).**
- **Found:** the recruit (number 0 of the library) wears only the body, the bare head and the arms
  (`export_chog_parts.py`); eyes are a trait, so its face shows the fringe and the cheeks and nothing else.
- **Changed (implemented, not exported):** it now also takes the eyes of "Side Eye" (`RECRUIT_EYES`), the very piece
  `Eyes_SideEye` the Genesis of that value wear (CHOG #112 wears it on the same bare head): a dark eye half closed
  under a straight lid, glancing aside, the Genesis drawing of the official mascot's half-lidded look, and the most
  worn eye piece that is neither closed nor an object (106 heroes). No new mesh. The view test now expects 5 pieces
  for the recruit, `Eyes_SideEye` among them, the same piece as #112's. `render_recruit.py` renders any hero of the
  library as the game assembles it, turned around; `make_recruit_sheet.py` lays the pictures on one sheet.
- **Not done: the export, the pictures, the tests on the new library.** The disk had 1.0 GB free (swap 11.4 GB of
  12.3 GB), under the 3 GB the owner asks before any Blender run, so no Blender was started. With the library of
  `main` the view tests give 89 passed, 1 failed: that recruit test (4 pieces where 5 are expected), as planned.
  Library of `main` (the "before"): 1,970 heroes, 495 pieces, 749 materials, 30 skins.
**Next action:** free disk to 3 GB or more, then on `recruit-eyes`: `export_chog_parts.py -- --check 5000` (expect
1,970 heroes and 495 pieces, none failed, none lacking a part), `sh tools/dev/view_tests.sh`, `render_recruit.py`
(and with `--library` set to `main`'s file and `--name recruit_before`), `make_recruit_sheet.py` into
`.artifacts/chog-body/recruit-eyes/` (the commands are in `tools/blender/README.md`, "The recruit's eyes"); once
exported, say it in `game/README.md` ("The 3D heroes") and the summary of `ChogHeroModel`; the owner's eye on the
sheet, then merge and build.

**17 October 2026, morning (the owner sells #1287 this afternoon and tests what needs it this morning; the domain
is bought later).**
- **Read on the chain that morning:** the Ledger holds token 1795, 39.03 $CHOG and 79.4 MON; #1287 is not offered
  for rent; its career on Render is empty (0 matches).
- **How a Genesis stays playable after the sale, without faking ownership:** a rental paid before the sale runs to
  its end (`ChogRental.sol`, at most 30 days from the payment); an offer from a former holder no longer stands; a
  free loan pass stops with the holding. A test wallet was made on this Mac for that
  (`0xa69B8e6Bc63426868e7eF1FE0272E80A6de73AbB`, key in `.artifacts/test-renter/key`, never printed);
  `tools/contracts/rent_as_test_wallet.sh 1795 30` rents with it (run by the owner, since it pays). The scripted
  browser plays as that renter with `CHOG_BROWSER_WALLET_KEY`.
- **Known limits for judging:** careers and records on Render's free plan are lost at each restart of the service
  (each deploy among them); after the sale RENT A HERO is empty unless another holder offers a Genesis.
- **Questioned by the owner the same morning:** a rental that outlives a sale leaves the buyer a right they cannot
  see or stop; the reference implementation of ERC-4907 erases the renter on transfer
  (https://eips.ethereum.org/EIPS/eip-4907), and delegate.xyz tells integrators to check that the delegating wallet
  still holds the token (https://docs.delegate.xyz/integrate-in-your-project/token-ownership-claiming). Also asked:
  tests through the screens with a wallet the owner controls, on a test collection, instead of a script wallet. The
  test-wallet rental above is suspended. Proposed, waiting for the owner: a rental whose right ends with the holding
  and whose unused days are paid back (payment held by the contract), and a staging setup on Monad testnet (a test
  collection, test $CHOG, the three contracts, services on this Mac with a second Photon application), the code
  the same and the addresses set by configuration (today `server/src/chain.js` fixes the collection and chain 143).
- **Seen at 08:30:** #1287's career on Render still reads 0 matches although the owner says they played it.
- **Decided by the owner that morning** (after `docs/PRODUCT-REVIEW.md`): the rental follows current practice (payment
  held and earned by the second, the right ends with the holding, unused time paid back, one renter at a time; branch
  `rental-v2`); the stakes stay ("un mvp crypto worldwide ... expérimental"), the legal reading is recorded in the
  review; careers go on chain, and as much as can go on chain or through an indexer does (branch `careers-onchain`);
  no testnet, full mainnet; the video shows the features in action, no wallet scenes, filmed late with a real match
  where an NFT the owner keeps stands in for #1287 (to build: a stand-in setting of the services, off by default,
  that writes no career for the Genesis it stands in for).
- **The owner's match of 16 October evening left no career:** the report path answers through Vercel (`POST
  /api/career/result` gives 401 `not_signed_in` unsigned); a restart of the free service overnight is the likely cause,
  not proven (no logs). A match signed in on 17 October settles it.
**Next action:** the owner's match signed in with #1287 and the career read right after; LEND IT tested; the rental v2
and the career registry built, deployed by the owner and wired; the stand-in for the video.

**17 October 2026, 09:30 to 10:35 (on chain, and the player's path).**
- **Deployed by the owner** (`contracts/script/DeployRentalAndCareers.s.sol`, checked on chain): ChogRental version 2
  `0x90c26090aea05a3dc18d6ba8fd80afe8f933bf86` and ChogCareers `0x335bf7bc610645546e2b00de6a9fd0c4709d6111` (owner the
  development wallet, referee `0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34`, funded with 5 MON by the owner). Render reads
  both (`render.yaml`). The services send, with the referee's key (`server/src/sender.js`): the careers of each judged
  match (`careers-onchain.js`), a holder's stop to lending, the close of a rental whose Genesis was sold. A stand-in
  (`standin.js`, `STAND_INS`, off) lets an NFT the owner keeps play #1287 for the video, never counted for a career.
  `/api/careers/recent` shows what happened to the last judged matches, since Render's log is not readable from here.
- **Seen:** the owner's match of 09:43 was credited by the old file careers (the report path works; the match of 16
  October evening was lost to a restart). After 09:46 the referee sent nothing (nonce 0): the owner's matches fell
  during five redeploys of that quarter of an hour (most likely; no log). **Seen at 10:57:** the owner's signed match
  with #1287 (holder, team 0, won) judged on Render and written on chain by the referee, transaction
  `0x04a6c37fbad4cabe6910d3257882590b8cc4ba10420dee74da07b8e1b0a4e30c` (block 114141758, 110,052 gas, about 0.011 MON):
  `careerOf(1795)` reads 1 match, 1 win, best streak 1; the game's career answer reads it (badges First match, First
  win). https://monadscan.com/tx/0x04a6c37fbad4cabe6910d3257882590b8cc4ba10420dee74da07b8e1b0a4e30c
- **The player's path, rebuilt and checked in a muted browser** (build `20261017T082930801Z`, on Render and Vercel):
  the end of a match has PLAY AGAIN and MENU (Enter, Escape), the score of both teams, the Genesis's career once on
  Monad; a whole siege played from the public address and left with MENU back to the menu, then a new match entered.
  The home screen is laid out as an arena game's (hero large at the left and facing the player, wallet at the top
  right, mode and a large PLAY at the bottom right), on a computer and a phone. The recruit note no longer runs over the
  team names; the rental and loan screens are opaque.
- The disk had 1.8 GB free: six merged worktrees removed (`git worktree remove`), 4.9 GB free after.
**Next action:** a signed match with #1287 and its career on chain; the NFT the owner keeps for the stand-in; the
"Starting" screen and an automatic start of the team preparation; the rental screens tried with a wallet.

**16 October 2026, 21:40 (the game is Chog Arena, at https://chogarena.vercel.app).**
- **Name:** the owner named the game Chog Arena (menu title, page name, manifest, share text, sign-in message);
  Chog Genesis stays the collection's name.
- **Address:** pages on Vercel (`tools/release/vercel_site.sh`), services on Render behind `/api/`, Render's pages
  redirect there, `PUBLIC_ORIGIN` set in `render.yaml` (DEPLOY.md, "The public address"). A scripted match from it
  works. The trial projects `chog-genesis` and `plain-page-probe` are deleted from Vercel.
- **MetaMask:** its dApp scan answers DRAINER (CRITICAL) for this address too, as for any new one tried that evening,
  a blank page included; the connection modal behind it dates from 13 August 2026 (DEPLOY.md). The owner decides
  whether to report the address to Blockaid; Rabby is an option for their own tests.
**Next action:** the owner adds `chogarena.vercel.app` at Reown; their session with #1287 on that address
(connect, sign, SECOND FORM, a match); whether to report the address; the video.

**16 October 2026, 20:30 (desktop controls, the owner's first session with #1287 on Render, MetaMask's block).**
- **Desktop controls on Render** (build `20261016T182139759Z`): the sticks show only while a finger is used
  (`ControlModeWatcher`, rule `ControlModeRule` in `Chog.Match`, tested); on a computer a panel names the
  keys and fills with the energy and the special's charge (`ChogDesktopControls`); Space fires the special.
  A mouse attack leaves on the press: decided in the simulation (`QuantumDemoInputTopDown.User.cs`: the
  unused `Dash` button carries "aimed with a pointer"; older records carry it up and the judge still agrees
  on both fixtures). Checked on Render with Metal drawing (`CHOG_BROWSER_GPU=1`) and the probe `CHOG_PROBE`
  (`?perf=1`): a held click near or far attacks, also while W is held. The run that held D first and clicked
  later saw the server replace the late inputs (`ReplacedByServer`) and repeat the walk: taken as the scripted
  browser's lag, not seen by a person yet. Not verified: Space, a click held for more than one attack (one
  attack per press for now).
- **The owner played as a recruit with #1287 chosen**: the services only let a signed-in wallet play its
  Genesis, and the button only spoke of a career. Now the signature is asked right after connecting, the button
  reads SIGN IN TO PLAY IT, and the recruit note says "connect ... and sign in" (checked on Render with a
  stand-in wallet: asked at once). The owner bought the second form of #1287 with the Ledger (100 $CHOG); the
  form button now says FIRST FORM / SECOND FORM. A recruit gets no class buttons (one character).
- **MetaMask blocks the Render address**: any unknown subdomain of `onrender.com` and other free hosts
  (DEPLOY.md, "MetaMask's site scan"). Needs a domain of our own.
- **The Mac's public instance, local services and test server stopped**: Photon's hooks and sign-in now go to
  Render, so a local build can only be tried there (a build must equal Render's to enter a match).
- **The disk filled twice** (swap reached 9 GB with the Editor and the browsers): old builds pruned, old renders
  and browser profiles deleted by the owner.
**Next action:** the owner's domain for MetaMask; their session with #1287 signed in; which screen they find
messy; recording the video.

**16 October 2026, 18:45 (the owner played a match, validated the 3D, and found the game unplayable with
a mouse and keyboard).**
- **Batch 11 merged and in the game** (build `20261016T162441148Z`, seen in the build with `?heroes=`); all
  checks pass (85 view tests, 31 simulation, services, 58 contract tests, the judge replays both fixtures).
  Render serves it since 18:30 (the push deployed it; the docs push before it was the last one that deploys
  without touching the image, `buildFilter` in `render.yaml`).
- **Photon half switched:** the owner's match (18:19 to 18:29) asked Render who the player was (the Mac got
  no `photon/auth`), but its record still went to the Mac (`photon/hook/...` in
  `.artifacts/services-public/public.log`): `WebHookBaseUrl` still names the tunnel. Until it names Render, a
  staked match started on Render cannot be settled there (refund after an hour).
- **Why a computer cannot play** (seen in a scripted desktop run, headless Chrome without touch emulation,
  `.artifacts/unity/check-desktop-1/`): the touch layout shows on a desktop page, because
  `MobileButtonsManager` shows it whenever `Touchscreen.current` exists, and Unity's web player reports one
  there; and `CombatInputReader` ignores the mouse buttons and the pointer's aim whenever that layout is
  shown (written so that a finger on a stick does not also count as a click). W A S D moves; clicking does
  nothing. Not changed yet: the owner asked how professional games do it first.
**Next action:** the owner's choice on desktop controls; `WebHookBaseUrl` on Render; the cron; the session
with #1287 on Render.

**16 October 2026, 18:10 (the owner created the Render service from the Blueprint and added its domain at
Reown).**
- **Render live:** `https://chog-genesis-game.onrender.com` (the name asked in `render.yaml`, no suffix, so
  `PUBLIC_ORIGIN` is right). Checked from the Mac with `check_public.sh`: the page, the four build files
  (the three `.gz` with `Content-Encoding: gzip`), a portrait, the hero page, the build
  `20261016T092135229Z`, Photon's two entries refused without their secret (403), the simulation assets and
  release notes not served; the wager, rental and shop read on Monad mainnet with the referee
  `0x125eE3EA9b3c0759C96dD3576A7CdB9D9Ba64E34`; the owner's address reads token 1795 through the public RPC;
  the sign-in message names `chog-genesis-game.onrender.com`. Not yet: Photon still calls the Mac's tunnel,
  so no match has been played against Render; cron-job.org not set.
**Next action:** the owner points Photon's two addresses at Render and sets the cron; then a match from the
Render address, and the session with #1287 there; the owner's yes on batch 11.

**16 October 2026, 14:30 (the owner refused batch 10's two costumes, its tape and the mummy's head, and asked
for the Render steps again).**
- **Batch 11 on `3d-batch-10`** (not pushed, not in the game): the banana and the dragon are whole costumes
  (`build_mascot` in `tools/blender/chog_trait_hats.py`): one volume on "Body" from above the feet to over the
  head, as wide round the body as round the head, the face looking out through an opening whose collar sits on
  "Head" and follows it; the mittens out of its sides, the boots under it. The banana: five flat sides from
  green over the head to orange at the feet, its end bending back to its stalk. The dragon: round, a light
  belly, tan horns round the face, nostrils and dark scales, ribbed wings, a curled tail, claws on the boots.
  The tape now wraps over the middle of the banana (it was centred off it, then lifted behind it); the mummy's
  bandage lies tight on the skull under the nose, in two turns (it stood off the head like a flange). Library
  exported (1,970 heroes, 0 lacking a part, 0 failed, heaviest 7,984 triangles), 85 view tests pass. Sheets
  `.artifacts/chog-body/traits/batch-11/before_after_1.png` and `more_suits.png`.
- **Render:** the steps and the free plan's limits as Render's pages gave them today are in
  [DEPLOY.md](DEPLOY.md), "Render, step by step". Not started: the owner creates the service.
**Next action:** the owner's yes on batch 11; their session with #1287 once they hold $CHOG; Render.

**16 October 2026, 12:15 (the owner deployed the three contracts on Monad mainnet and validated batch 9 but
four points; they buy $CHOG and set up Render later).**
- **Mainnet live:** wager `0xc7524a808d2b00b43a4d3e2becb8500e409b41e3`, rental
  `0x7f1271d43b0e41e2eeddd5290f459fdc6196a19a`, shop `0xb23a3c3492b9ba83d80c8abc9a5484d2885f058a` (owner
  `0x981F6E0Ea94f45fDB8ee7680DC862212E3C720e0`), checked on the chain (`mainnet_deployment.sh`); the public
  instance runs on them (`serve_public.sh ... --mainnet`; its menu reads the rental open, the shop at 100 $CHOG
  and the wager on Monad). Rehearsed from the game on a local copy of mainnet, the deployed contracts as they
  are: a staked match paid (+22.5 / -25, 2.5 burned), a rental both ways, the second form of #1287 bought
  (`research/evidence/mainnet-contracts-rehearsal-on-copy-2026-10-16.txt`). Neither the owner's Ledger
  (holds #1287) nor the development wallet holds $CHOG yet: the real tests wait for it.
- **Batch 9 merged and in the game** (build `20261016T092135229Z`). **Batch 10 on `3d-batch-10`** (not
  pushed): costumes over the whole body (trousers, seat, sleeves into the mittens, the hood closed under the
  chin; the dragon's snout and feet, the banana's faceted peel), the mummy in real bandages, the taped banana
  after Cattelan's. Library exported, 85 view tests pass; sheet
  `.artifacts/chog-body/traits/batch-10e/before_after.png`.
- **Decided:** no loan link for the judges; Render on the free plan with a ping from cron-job.org.
**Next action:** the owner's yes on batch 10; their real session with #1287 once they hold $CHOG; Render.

**Where things stood, 11 October 2026, 15:00.** The secured version asked for
on 10 October (D33) exists up to the point where Photon's cloud must call us. What is written
and tested: the game services answer Photon's question "who connects" (a signed-in wallet, a
guest with a pass, a visitor), receive its eleven webhooks, let a player into a match with a
Genesis only when its wallet holds or rents it or its pass lends it (anything else enters as a
recruit), keep the input of every tick as Photon's server records it, **play the match again
outside the game and read who won from the simulation itself**, and sign that result for the
wager contract; a player stakes $CHOG on its own team with a ticket the referee signs, the
stakes are locked before the match begins, and any doubt calls the match off, which gives
every stake back. **What was seen run:** a match played by two browser pages in opposite teams
on Photon's Public Cloud was replayed outside the game (5,448 ticks in under a second, same
winner as the game announced), and a rehearsal on a local copy of Monad ran the whole staked
match with that record: two wallets stake 25 $CHOG each, the winner ends with +22.5, the loser
with -25, 2.5 are burned; three ways of cheating all end with every stake given back
([evidence](../research/evidence/staked-match-rehearsal-2026-10-11.txt)). **What is not
there:** in that rehearsal Photon's cloud is played by a script that sends what Photon's manual
describes; Photon itself has never called these services, because they have no public address
and its dashboard is not set up (two things only the user can do or allow: see
[docs/DEPLOY.md](DEPLOY.md), "Photon"). **Since 15:25 the game has its staking screens**
(entry of 15:30): on the Team Preparation screen a signed-in player stakes $CHOG on its team,
the stakes are locked at START, and back on the menu a strip says what became of the stake
and offers to claim it. Seen in the headless browser with two test wallets on the local copy
of the chain: both stake 10, the winner's menu ends on "YOUR TEAM WON: +9 $CHOG, PAID"
(10,009 against 9,990 on the copy). No contract is deployed, so none of this shows anywhere
but on the test bench. Decisions taken alone on 11 October: D34 and D35, confirmed by the
user on 12 October.
**Next action (the user):** ten minutes in Photon's dashboard with the values of
`docs/DEPLOY.md` (the tunnel was allowed and opened on 12 October).
Since 15:28 every Genesis hero wears a Chog body made by a Blender script (round, big
fists, spikes on the back) in place of the sample's knight, archer or wizard: seen in a
headless match, not on a phone.
**Next action (dev):** a tower and a core on the current map. Looked at on 11 October,
nothing written yet: the map (`Map1`) is one lane, team 0 spawns at y = -14 and team 1 at
y = +16 (x = -3, 0, 3), the objective points are around the centre
(`python3 tools/quantum/list_map_points.py <quantum-db.json> [x,y radius]` lists them and the
colliders near a spot); an existing attack hurts anything that has `Health` and `TeamInfo`
(`AttackData.CanAffect`), so a structure with those, `Attributes` and a collider on the
character layer is hit by every attack as it is; `HealthSystem` assumes a character when
health reaches zero (it drops coins, hides and respawns) and must leave structures to their
own system; the winner is decided in `TriggerQuantumEvent` from the scores alone, and the
judge's draw rule (`server/src/judge.js`) reads the scores too: both must learn that a
destroyed core ends the match for the other team.

**Before that, 10 October 2026, 07:45.** A player can connect the
wallet of the browser, see the Genesis it holds (read on Monad), pick one, play a 3v3 match as
that hero, sign in, have the career of that Genesis recorded, and lend it with a link that a
guest opens to play it without a wallet; a player without a Genesis and without such a link
plays a weaker recruit. A Genesis earns mastery by playing, and at mastery 2 it may play a
second form of its attack, chosen on the menu. Three things are paid in $CHOG and **none is
live, because no contract is deployed**: the second form bought at once, and, since 10 October,
a holder offering a Genesis for rent at a price a day and any wallet renting one (the holder is
paid, a share is burned, the Genesis never moves); all three were rehearsed on a local copy of
the chain only. Every special attack and its marks on the targets are drawn from the traits
(eyes, aura, rarity). In a match a hero is a body under the image of its Genesis: until
10 October the image hid the whole body (the user's report from the phone, "the heroes are
invisible"). A light web page shows any Genesis as its hero card. All of this runs on the
development Mac only (`http://192.168.1.85:8083/`, with `node server/src/local.js` running)
and was checked in an automated browser with a stand-in wallet. **The user played it on the
phone on 10 October (about 30 images a second, "rather fluid"); no real wallet and no public
address yet.** The decisions the dev agent took alone are D21 to D30 in the decision log, to
be confirmed; D31 (the rental and its terms) and D32 (the game's page and the wallet path on
a phone) were settled with the user on 10 October. A first census of the field (eight public repositories out of 311 applications,
read in their code) found no other real-time game, four entries already live at a public
address and one that burns real $CHOG
([census](../research/reports/chogathon-field-census-2026-10-09.md)). How the game should be
opened on a phone was researched on 10 October
([report](../research/reports/mobile-presentation-2026-10-10.md)): stay a web page opened by a
link, full screen on the first tap on Android, installation offered and never required, and a
second way to connect a wallet, because the wallets' own browsers are locked to portrait.
Since 04:15 the game is served in its own page (whole screen on its side at the first touch,
a load that fails starts again by itself); the user saw it on the phone at 04:30: the whole
screen is used and the bodies read.
**Order set by the user at 06:20, raised at 07:40 (D33):** the secured version of the game
and staking $CHOG on a match come first, then the match itself (a tower and a core), then the
look of the heroes; the Reown project id and the deployments are the user's to do. The
secured version needs a public address at once (Photon's servers call our services), which
the user had put off: a tunnel from this Mac gives one in minutes, on their yes.
**Next action (the user):** ask the Chog team for the layer files of the collection (one
see-through picture per trait value); the Chog body model; the Reown project id.
**Next action (dev):** the cut-out heads are in the game for the 1,277 Genesis that look
right (entry of 07:00); next the 692 others, then the colours of the clothing on the body. `sh tools/release/check_all.sh` runs every
check that needs no Editor.

Mise à jour : 9 octobre 2026 (dev agent takeover, 02:45). **Décision de développement réexaminée : Unity + Quantum 3
dans `game/` pour le MOBA Genesis.** [Raisonnement, coûts et travail borné](DECISION-RESEAU-REVUE.md).
La partie locale avec bots a été construite en WebGL et lancée dans Chrome. L'utilisateur
rapporte qu'elle fonctionne sur son Redmi 11C, « assez fluide » avec un peu de lag sur ce
téléphone peu performant ; aucune cadence d'image n'a été mesurée. **On 9 October the user
played the first online Quantum match between the Mac Editor and the phone (2 humans,
4 bots, party code): fluid and playable by their report, not measured.** Result
validation remains to do. Le candidat Fusion Server,
précédemment validé, est conservé dans `game-fusion/` comme preuve et repli, sans nouveau
contenu de jeu en parallèle.

**Preuve du candidat Fusion antérieur :** Fusion **2.1.3 build 2390**, Unity **6000.6.4f1 arm64**,
Mono.Cecil **1.10.2**. La première tranche Archer du sample est portée dans Fusion :
volée de sept tirs au relâchement, projectile spécial et zone à impulsions, ressources,
visée/aperçu, modèle animé, sons, caméra, HUD et bot d'entraînement. Le bot attend un
premier impact puis laisse cinq secondes avant de riposter. Huit contrôles réseau passent
via Photon avec un serveur sans joueur local et deux clients dans **un même processus Unity**
avec physiques séparées. [Preuve Archer, captures et empreintes](../research/evidence/fusion-archer-slice.json).

Les contrôles valident connexion/scènes, autorité, mouvement, dégâts répliqués,
volée exacte sans double cast, zone spéciale, départ/despawn et nouvel arrivant.
Ils ne démontrent pas une reprise authentifiée du héros initial, un serveur Linux déployé
ou 100 CCU. Le build WebGL Archer réussit (~12,9 Mo) et l'entraînement fonctionne dans
Chrome 155 avec souris/clavier ou deux contacts tactiles émulés. Les captures montrent
volée, zone et direction de visée. Il ne s'agit pas d'un essai Redmi 11C ou d'une mesure
de performance mobile. Multijoueur navigateur, pause/reprise et téléphone restent ouverts.
Genesis, classes/capacités supplémentaires, objectifs MOBA, carrière et $CHOG restent à
intégrer. Le catalogue C# est référencé par package local ; les héros sont des démos.

Le retour utilisateur sur le meilleur ressenti du sample Quantum a motivé ce portage ciblé.
La comparaison sur le même appareil et les essais joueurs restent à faire ; aucune parité
globale au sample n'est revendiquée. [Inspection des sources](../research/evidence/quantum-to-fusion-reuse-inspection.json)
et [frontière du portage](ARCHITECTURE.md#31-reprendre-lexpérience-du-twin-stick-shooter).

Le 8 octobre, l'utilisateur a signalé « Render process gone: crashed (10) » sur
`http://127.0.0.1:8080/`. Le serveur local répond encore **HTTP 200** et le build Archer
se charge dans un nouveau Chrome 155 automatisé (entraînement, 62 événements, aucune
erreur console). Ces observations ne déterminent pas la cause du crash du processus de
rendu signalé, ni le comportement du navigateur intégré ou du Redmi 11C. Prochain
diagnostic : ouvrir la même URL dans Chrome autonome, puis relever le contexte et les
journaux du processus qui plante si l'erreur s'y reproduit.

**Preuve du choix Quantum :** le coût du portage Fusion ne suffit pas à décider. Le sample Quantum local a maintenant produit un build WebGL de la
scène Gameplay avec bots sous Unity 6000.6.4f1 : 22 193 639 octets, zéro erreur de build.
Chrome headless a affiché le menu des trois héros puis une partie après Start ; aucune
exception ni erreur console observée dans ce parcours. [Preuve et limites](../research/evidence/quantum-webgl-local-2026-10-08.json).
Cela établit une base web jouable sur Mac et un premier essai positif sur Redmi, **pas** les performances mesurées sur Redmi, le réseau
Quantum, les résultats authentifiés ni les licences de distribution. Quantum est la base
de développement ; un échec mobile attribué à sa simulation impose un réexamen avant
d'étendre le contenu. Les achats et résultats ne sont pas déclarés sécurisés par ce test.

L'utilisateur accepte **100 connexions Photon simultanées pour le MVP**. L'application PUN2
chogtanks conserve son AppId et son forfait. Aucune modification de compte/forfait ; activation
et éligibilité du Free 100 CCU non vérifiées. [Décision et coûts](DECISION-RESEAU.md).

L'AppID fourni est appliqué dans le vrai `PhotonAppSettings.asset`, version `chog-p0-fusion-1`,
région `eu`. [`config/photon-fusion.json`](../config/photon-fusion.json) alimente l'outil
Editor de préparation ; il n'est pas relu pendant un match. La connexion cloud a été testée.
L'archive locale fournie par l'utilisateur a levé le blocage d'acquisition : **542 fichiers
importés avec leurs GUID**, empreintes conservées. [Relevé SDK](../research/evidence/fusion-sdk-qualification.json).
Un correctif initialise le registre de scènes s'il est nul au démarrage ; le cas a été
observé après le vrai `Start` Unity en qualification sans rendu. [Patch ciblé à revoir au
prochain SDK](../tools/unity/patches/fusion-2.1.3-scene-initialization.patch). Les 20 autres
fichiers fournisseur modifiés sont des métadonnées d'import d'images.

Lire [PLAN.md](PLAN.md), puis [l'architecture Quantum active](ARCHITECTURE-QUANTUM.md).
Les sections d'architecture décrivent la cible ; elles ne constituent pas une preuve de livraison.

## Prochaine étape

L'utilisateur a créé l'**AppID Quantum 3** `625f17f2-5dc8-4bfe-a3cb-59c34ce730dc` ; il est
renseigné dans `game/Assets/QuantumUser/Resources/PhotonServerSettings.asset`. Contrôle
statique réussi : identifiant valide, menu et Gameplay présents dans le build, menu utilisant
ce réglage global. Aucun autre AppID local n'a été modifié ; le forfait de la nouvelle app
n'a pas été consulté. La compilation WebGL réseau lancée en batch le 9 octobre a été
interrompue avant la méthode de build : le client de licence Unity n'a pas réussi à
s'initialiser (délai de 60 puis 75 s), même après relance avec accès système.
[Journal](../.artifacts/unity/quantum-online-appid-build.log).
L'utilisateur rapporte que le parcours proposé dans le menu Quantum fonctionne dans
l'éditeur (création de salon). Ce retour confirme un premier usage du menu, sans journal
de connexion examiné ici. Le build WebGL réseau demandé ensuite a réussi sous Unity
6000.6.4f1 : deux scènes, zéro erreur, 32 avertissements, 22 684 934 octets. Le nouveau
build est servi sur `http://192.168.1.85:8082/` tant que l'adresse Wi-Fi et le serveur
local restent actifs. Un Chrome isolé sur le Mac a ouvert le menu, créé un salon, affiché
Team Preparation avec un ping, puis lancé la partie à bots. Aucune exception de jeu n'a été
observée dans ce parcours. [Preuve et limites](../research/evidence/quantum-online-webgl-2026-10-09.json).

**First two-device online match, 9 October (user report):** the Editor on the Mac created
a party room, the phone joined it with the session code from the build on port 8082, and
two humans plus four bots played. The user reports it fluid and playable. Quick Play did
not pair the two clients pressed a few seconds apart; the code shows Quick Play lets each
client pick its best region by ping while the party code carries the region, so a region
split is the most likely cause, not attributed for lack of logs. Nothing was measured, no
reconnection was tried (PlayerTTL is 0), and there were no minions or towers.
[Evidence and limits](../research/evidence/quantum-online-two-devices-2026-10-09.json).

**First measured phone run, 9 October 05:05:** the overlay (`game/Assets/Chog/Diagnostics/`,
off by default, `?perf=1`) ran in the WebGL build on the user's phone, online, six heroes,
60 Hz, untuned build. GPU string: Mali-G52 MC2, a Helio G85 class chip, consistent with a
Redmi 12C. Over 113 sampled seconds: **about 28 frames per second on average, median frame
33 ms, p95 57 ms, p99 104 ms, worst 785 ms; Quantum update median 12.8 ms per frame with 6.9
predicted ticks on average.** The user finds it fluid, but the audit's bar (p95 at or under
33.3 ms) is **not met**, with no minion or tower yet. Frame and simulation times rose during the
run, cause not attributed.
[Readings, screenshots and limits](../research/evidence/quantum-online-perf-phone-2026-10-09.json).

**Overlay extended, 9 October 05:30 (compiled and unit-tested, not yet run on device):** it now
shows main-thread busy time and the non-simulation share, frames under 20 ms / over 40 ms /
over 100 ms, the running session's rate, rollback window and checksum interval, the target
frame rate, the quality level and the managed heap. With `?perf=1`, URL parameters override
test settings without a rebuild: `hz`, `rollback`, `checksum` (session config, effective only
when this client creates the room), `fps` (`-1` lets the browser pace frames) and `quality`
(Unity quality index; the WebGL build starts at 0 "Fastest", see the A/B result below). 21 sampling tests pass
headless.

**Five baseline repetitions, 9 October 05:24 to 05:36:** the user ran the five tuning URLs
solo on the phone, but no new build had been made, so the overrides were ignored and all five
runs used 60 Hz, checksums on and the default quality, later read as "Fastest". They form a repeatability baseline: median
frame 36 to 41 ms (about 24 to 27 fps), p95 54 to 62 ms, Quantum update median 14 to 16 ms,
with a drift of 10 to 15 percent across the series.
[Readings and proof](../research/evidence/quantum-baseline-repeat-phone-2026-10-09.json).

**Builds no longer need the menus, 9 October 05:40:** `Assets/Chog/Editor/BuildRequestWatcher.cs`
makes the open Editor build WebGL when the file `.artifacts/unity/webgl-build-request` appears
(it refreshes assets first, skips Play Mode, and writes `webgl-build-status.json`).
`tools/unity/serve_webgl.py --latest` now follows the newest finished build without a restart.
Verified once: the request written at 05:40 started a build at 05:41 after the Editor was
brought to the front.
That build (`20261009T034106742Z`, 296 s, 0 errors) was then checked end to end in headless
Chrome on the Mac with `?perf=1&checksum=0&hz=30&fps=-1&quality=1`: the console logged the
override request, a party match started, and the overlay read `quality 1 Fast` and
`session 30 Hz  rollback 60  checksum 0  target fps -1`, with the sample's frame counter at
about 29 ticks per second.
[Screenshot](../research/evidence/perf-overlay-overrides-headless-2026-10-09.png), SHA-256 `7263c0e16a649a14...`.
Headless Chrome renders in software, so its frame times say nothing about the phone.

**First tuned run, 9 October 05:54:** with `?perf=1&checksum=0&hz=30&fps=-1&quality=1` the
phone reads median frame 30 ms (about 32 fps against 24 to 27 untuned), Quantum update median
8.1 ms (against 14 to 16), 2.9 predicted ticks (against 7.2 to 7.8). The simulation gain comes
from 30 Hz. New fact: non-simulation main-thread work is a median 17.7 ms even at quality Fast,
so this phone is now bounded by rendering and view code and a steady 30 fps is the realistic
target. Tick-counted timings to convert before switching the project to 30 Hz: sensor
`TickRate` values (1, 10, 30), `TICK_INTERVAL` of the AI director and the `FindCoverSpot`
interval; everything else uses delta time.
[Readings](../research/evidence/quantum-tuning-runs-phone-2026-10-09.json).

**A/B on the phone, 9 October 05:54 to 06:00, same build, three runs:** 30 Hz with checksums
off gives about 32 fps, median frame 30 to 31 ms, 12 to 14 percent of frames over 40 ms and a
Quantum update of 8 ms; untuned gives 26 fps, 37 ms, 36.5 percent and 15 ms. Non-simulation
main-thread work is 17 to 18 ms in both. Two corrections from the overlay: frames were already
browser-paced (target frame rate -1), and the WebGL build starts at quality **0 "Fastest"**,
not "Good" as written earlier from the project file; there is no per-platform default for
WebGL. So no rendering setting is left to lower on this phone, a steady 30 fps is the target
there, and stronger devices currently get a flatter picture than they could.

**Applied, 9 October 06:05:** `SessionConfig.asset` is now 30 Hz with `ChecksumInterval` 0
(rollback window left at 60 as tested). Bot timings that counted ticks go through
`Quantum.TickIntervals` (AI director interval, tactics sensor rate, cover-spot interval), so
bots keep the same real-time pace. Verified: the full simulation assembly compiles with
Unity's own compiler arguments, and `python3 tools/quantum/headless.py test --dotnet
<Unity>/Contents/Resources/Scripting/DotNetSdk/dotnet` passes 27 catalog and 15 Quantum
tests, one of them new for the tick conversion. To check determinism while writing simulation
code, turn checksums back on for a session with `?perf=1&checksum=60` (room creator) or in the
asset.

**30 Hz build verified, 9 October 06:14:** build `20261009T040520862Z` (0 errors) is served on
port 8083. In headless Chrome with `?perf=1` and no override the overlay reads `session 30 Hz`,
`checksum 0`, and a party match ran 95 s with bots scoring. A first run right after the build
hit `ClientTimeout` while the Mac's load average was about 80 and that Chrome stalled 8.4 s;
the rerun was clean. The user reports the game felt fluid at 30 Hz.
[Screenshot](../research/evidence/perf-30hz-default-build-headless-2026-10-09.png).

**Quality per device, 9 October 06:45 (user request):** `Assets/Chog/Presentation/` picks the
first-run quality level by device class, "Good" on desktop and "Fast" on mobile, unless the
sample menu already stored a level (settings screen, gear icon, Graphics Quality). Verified in
build `20261009T042612611Z` with headless Chrome: a desktop profile reads `quality 3 Good`
with shadows on screen, an emulated phone reads `quality 1 Fast`
([desktop](../research/evidence/quality-default-desktop-good-2026-10-09.png),
[phone](../research/evidence/quality-default-mobile-fast-2026-10-09.png)); 3 unit tests pass
headless. Limit: strong phones also start at "Fast"; none has been measured.

**NFT differentiation, direction given by the user on 9 October:** go. Proposal on the table:
(1) the NFT picture is the hero sheet, one visible trait deciding one thing (Body the class,
Eyes the signature shape, Aura its effect, Tier its delivery, Skin and Background the colours);
(2) the living collection: the 1,969 Genesis are the only cast, bots are real Genesis with
their own name, traits and kit, and each token keeps one public career. The user holds one
Genesis, **token 1795 = CHOG #1287** (no Body, Eyes Anime Glass, Aura Clean, Common, Skin Red;
[on-chain read and metadata](../research/evidence/user-genesis-1795.json)); other characters
come from public metadata. Working assumption for the first playable: two buttons, the class
attack and the Genesis signature in the existing special slot; a third slot later if tests ask
for it. In progress: a scan of every token for metadata id and owner
(`research/scripts/scan_genesis_bindings.py`) and the fetch of the remaining metadata
(302 of 1,969 recorded at the start). Not built yet: kits from traits, bots as Genesis, careers.
The field was first surveyed on 9 October after 17:00 Paris: see
[the census](../research/reports/chogathon-field-census-2026-10-09.md).

**Catalog extended to the known collection, 9 October 07:30:** ruleset `genesis-0.2.0`
(schema 2) separates the signature into a shape decided by Eyes and an effect decided by Aura;
Body alone decides the class and Head is cosmetic. Tables cover every value seen in the 552
Genesis recorded so far (87 Body, 44 Eyes, 21 Aura entries, absent values included). Verified:
`python3 tools/hero-catalog/run.py coverage` resolves 552 of 552 with the real resolver, none
rejected, 160 distinct kits, classes 34/38/28 percent; 30 catalog tests and 15 Quantum tests
pass headless. The user's CHOG #1287 resolves to brute, flash, purify, direct.
[Catalog notes](../content/README.md),
[coverage](../research/evidence/genesis-catalog-coverage-2026-10-09.json).
Collection scan at block 111801894: 1,969 tokens, ids 1 to 1969, 1,011 owner addresses
([bindings](../research/evidence/genesis-bindings-2026-10-09.json)). Metadata: 552 of 1,969; the
public gateways rate-limit, so `research/scripts/fetch_genesis_metadata_patient.py` keeps
completing the rest across four gateways. **Not built yet:** the signatures themselves. The
sample's attack classes already provide the five shapes as data (projectile, moving overlap that
hits each target once, lobbed zone, area with interval), so the next step is an attack-data
override in `SkillData`, an Editor generator for skill and attack assets, and the loadout
selection in a real match.

**Version control (user decision, 9 October):** no repository yet; commits start on
10 October. Until then, source snapshots are kept under `.artifacts/snapshots/`.

**User feedback, 9 October (applies to every report):** the percentage table of the catalog
was not understood and looked like simulation. Report against what a player can actually see
and play; each block of work ends with something playable.

**A Genesis enters a real match, 9 October 08:00:** with `?token=1795` in the URL, the player
enters an online match as CHOG #1287. Implemented: roster generated by the real resolver
(`content/genesis-roster.json`, 553 Genesis), imported into the `GenesisRoster` simulation
asset before each requested build; `GenesisHero` component on every Genesis character; class
locked to the one the Body trait gives; bots drawn from the roster without repeats; Genesis
name above the character and on the selection screen. Verified in headless Chrome on the
online build: selection screen shows only the Knight and "CHOG #1287 - Knight", the match
labels the player "CHOG #1287" and a bot "CHOG #313"; 30 catalog tests and 18 Quantum tests
pass. **Not verified:** the phone, two devices, and more than one bot label on screen.
**Not built:** wallet connection and ownership check (the token id is typed by hand, anyone can
type any id), signatures (shape, effect and expression are carried but change nothing in
combat), Genesis art on the character. A first build silently fell back to the sample cast
because Quantum registers new assets late; the import now verifies the registration and fails
the build otherwise. [Evidence](../research/evidence/genesis-entry-headless-2026-10-09.json),
[how to use](../game/README.md).

**The whole collection is playable, 9 October 09:00:** metadata recorded for all 1,969 Genesis
(through `ipfs2.seadn.io`, whose bytes match the other gateways), catalog raised to ruleset
`genesis-0.3.0` (25 late trait values placed with their families, six more 1:1 heroes authored
from their picture), `coverage` resolves 1,969 of 1,969 into 237 kits, roster regenerated and
imported (build status: "1969 heroes, 3 of 3 roles with a character"). Verified in headless
Chrome: bots numbered 1566 and 775 appear, the latter with its portrait; 30 catalog tests and
18 Quantum tests pass. Portrait copies exist for the whole collection under
`.artifacts/genesis-art/256/`. [Coverage](../research/evidence/genesis-catalog-coverage-2026-10-09.json).

**The real Genesis image on the hero, 9 October 08:35 (first step after D18):** every Genesis
character shows its image as a round portrait above its name, and beside its slot on the
preparation screen. Implemented: `tools/genesis-art/fetch_portraits.py` (256 px JPEG copies of
the IPFS originals, kept out of version control), a `/genesis-art/` route in the local server,
`GenesisPortrait` in `Assets/Chog/Match/`. Verified in headless Chrome on the online build: the
portrait of CHOG #1287 on both screens and a portrait on the bot CHOG #200; five Chog.Match
tests pass outside Unity. **Not verified:** legibility on the phone (size tunable with
`?portrait=`). **Open:** the right to redistribute the art (question for the Chog team, to be
sent by the user), tint from Skin and Background, a Chog body instead of the sample's.
[Evidence](../research/evidence/genesis-portrait-headless-2026-10-09.json).

**Collection reads made fast, 9 October 08:15:** `research/scripts/scan_genesis_owners_multicall.py`
reads the owner of all 1,969 Genesis in one Multicall3 call (0.68 s, 1,011 owners at block
111821264, [result](../research/evidence/genesis-owners-multicall-2026-10-09.json)); it
replaces the slow per-token scan for ownership checks. The metadata fetcher gained the
`ipfs.filebase.io` gateway, whose bytes for metadata 1287 hash to the recorded fingerprint;
794 of 1,969 metadata files were recorded when it restarted. When it completes, run
`python3 tools/hero-catalog/run.py coverage` to list the trait values the catalog does not map
yet, then `roster`.

**Research run, 9 October:** five notes under
`research/research_notes/Héros Genesis traits et liaison NFT/` (Monad NFT capabilities, NFT
binding standards, hero kit design and balance, trait to visual pipeline, web3 game
architecture) and a synthesis in `research/reports/`. They are inputs, not decisions; the
decisions taken from them are recorded in the decision log once the user confirms them.

**The special of a Genesis is the signature of its Eyes, 9 October 09:20:** five shapes, raw
and direct. Implemented: `SkillData.AttackDataOverride` and `BallisticAttackData.PostImpactDataOverride`
(one attack prefab serves several skills); `GenesisSweepAttackData`, an area that affects every
target it touches once (narrow and fast is the beam, wide and slow the wave, standing in front
the flash, standing around the caster the nova); the puddle reuses the lobbed projectile and
ground zone with its own data; `GenesisSignatureImporter` authors the eleven data assets under
`QuantumUser/Resources/Genesis/`; `GenesisRoster.ShapeSkills` and `GenesisCast.Tag` give each
Genesis its special; `SkillData.Reach` keeps a bot's special for a target it can touch and
drives the aim preview of a Genesis special; `CombatLog` writes skill and damage lines with
`?perf=1`. Verified in two headless online matches: all five signatures fired and each
authored damage value was dealt at least once, no class special of the sample fired, no error;
19 Quantum tests pass. **Not verified:** a human firing one, the aim preview, the feel of each
area, the phone. **Placeholders:** numbers (no kit budget yet) and visuals (borrowed from the
sample's attacks, not sized to the areas).
[Evidence](../research/evidence/genesis-signatures-headless-2026-10-09.json).

**User test on the phone and decisions, 9 October 09:30:** the portrait never appeared on the
phone and the user saw no visual difference between characters. Cause: Unity refuses plain-HTTP
downloads except from the machine itself, so every check on `127.0.0.1` passed while the phone
got nothing ("Insecure connection not allowed", thrown inside the loading coroutine). Fixed:
`PlayerSettings.insecureHttpOption = AlwaysAllowed` in the qualification build, the exception
is caught and reported, a grey disc shows while an image is missing, and the performance overlay
prints a portrait status line. Verified through the LAN address `http://192.168.1.85:8083/`:
six portraits requested and loaded. **Method change:** browser checks run on the LAN address.
The user also judged the flash indistinct (it borrows the Spellcaster zone visual) and asked
that what happens be readable on screen: signature visuals come before the Aura effects.

**Each signature has its own visual, 9 October 10:05:** one generic attack prefab
(`Resources/Genesis/GenesisAttack.prefab`) whose view, `GenesisAttackView`, shows the effect
named by the attack data (`AttackData.Visual`, `VisualScale`); effects come from the Cartoon FX
pack bundled with the sample (about a thousand prefabs under
`TwinStickShooter/Third Party Assets/`) and from the sample's own VFX. Beam: light ball with a
trail. Wave: the sample's energy pulse, enlarged. Flash: a light burst in front. Nova: a
cartoon explosion around the hero. Puddle: unchanged. Test option `charged=1` in the page URL
gives every character a charged special at spawn (`RuntimeConfig.StartWithFullSpecial`).
Verified in headless Chrome on the LAN address: nova and flash seen, flash aim cone seen, no
console error. **Not seen:** beam and wave (the automated browser is too slow to capture them).
**Open:** effects are not tinted by team; the flash cone is longer than its area; numbers are
placeholders. A first build shipped without the generic prefab's prototype and broke every
signature for about four minutes; the importer now checks it.
[Evidence](../research/evidence/genesis-phone-fix-and-visuals-2026-10-09.json).

**User test, 9 October 10:20, and the Aura effects built in answer:** the portraits now show.
Feedback: some attack visuals are weak; everything was "damage on touch"; the puddle's lobbed
shot is not visible; all the examples given were Knights (they were: every token suggested
was a brute); it is not readable that traits decide the character; specials did not feel
strong. Built: fifty signatures, five shapes by ten Aura effects, generated by
`GenesisSignatureImporter` (burn and toxin over time, chill slows, hex roots for 1.2 s, mend
heals the caster, purify drains attack energy, rally boosts the caster's damage, haste refills
its energy, veil removes special charge); damage raised (flash 2600 against 1600);
`GenesisRoster.SignatureSkills` by shape and effect; each attack tinted with its Aura colour;
event `GenesisSignatureHit` and `GenesisCombatFeedback` show a burst in that colour on each
target and a lasting effect for burn, hex and toxin; the preparation screen prints
"class / shape / effect" under the Genesis name. Verified in one headless online match through
the LAN address: raw, burn and rally signatures fired with the authored numbers (the burn's
ticks and the rally bonus on later arrows are visible in the damage log), no error; 19 Quantum
tests pass. **Not seen:** the seven other effects, and all the on-target visuals.
**Still open from the feedback:** the puddle's lobbed shot, the weak visuals (which ones is to
be asked), and the real answer to "traits decide the character", which is a hero card showing
each trait and what it gives.
[Evidence](../research/evidence/genesis-aura-effects-headless-2026-10-09.json).

**Performance regression reported by the user, 9 October 11:00:** "since the effects the game
lags a lot", worse as the match goes on, and already soon after the start. Measured on the Mac
(headless, software rendering, three minutes, one report a minute): simulation p50 1.6 to
1.8 ms, main-thread work p50 about 5 ms and managed heap 30 to 33 MB, all flat; nothing grows
on the CPU or memory side. Inference, not measured: the cost is on the phone's GPU (overdraw of
the cartoon effects), and the worsening is thermal throttling. Done without waiting for phone
data: `GenesisEffects` is now the single place that spawns an effect (no real-time light, no
camera shake, at most 10 live effects, at most 30 particles per system on mobile, shorter
fades); `fx=0` in the page URL turns all Genesis effects off; the hidden preparation screen no
longer refreshes during the match; the performance overlay posts its minute report to the local
server (`/perf-report`, kept in `.artifacts/perf-reports.jsonl`), so a phone run is read
without screenshots. Verified: a report from the LAN address reached the server ("fx on, 18
spawned, 0 skipped"), no console error, 19 Quantum tests pass. **Not measured yet:** the phone.
Also unmeasured since it was changed: the mobile default quality "Fast" instead of "Fastest".
The build under test is pinned on port 8083; newer builds are served on 8084.

**Phone result, 9 October 11:13:** the user played the lightened build and found it fluid; the
two reports posted by the phone read 31 and 32 fps, median frame 32 and 31 ms, p95 53 and
48 ms, simulation 8.9 and 8.3 ms, with 44 effects shown in two minutes and none skipped. That
is the level of the morning's 30 Hz baseline, with no decline in the second minute. Which of
the changes fixed it is unknown; the runs without effects were not needed and not played.
[Evidence](../research/evidence/genesis-effects-perf-phone-2026-10-09.json). From now on a
checked build is pinned on port 8083 for the phone and the newest build is served on 8084.

**Hero card and the laser drawn from the art, 9 October 11:35.** The preparation screen now
replaces the class buttons, for a Genesis, by a card: portrait, name, then for the outfit, the
eyes and the aura the value of the trait and what it gives (`GenesisHeroCard`,
`GenesisRoster.CardLines`; trait values added to `content/genesis-roster.json`; sentences
authored next to the numbers in the importers). The beam is now the laser of the caster's own
eyes: `GenesisBeam` draws two beams, one per eye, each a dark outline, a flat colour and a
lighter core, in the colours of the four laser values of the Eyes trait, as the collection
draws them. Verified in headless Chrome through the LAN address: the card for CHOG #1287, the
first frame of the fire laser of CHOG #1494, no console error; 20 Quantum tests pass.
**Not seen:** the laser at full length and its three other colours; the card on the phone.
[Evidence](../research/evidence/genesis-card-and-laser-headless-2026-10-09.json).

**Autonomous session, started 9 October 11:55 (user instruction: "je te laisse travailler,
j'aimerais que tu sois autonome et que tu en fasses le plus possible"; tests must be silent,
`--mute-audio` added to the browser smoke test).** The user confirmed the build with the card
and the laser works on the phone, and announced a Blender Chog body (four rigid parts named
`Body`, `Head`, `armLeft`, `armRight`, no skeleton, one material; spec given in chat). The
lane MOBA question is still unanswered: the 3v3 mode stays until the user says otherwise.
Work order for this session, each item verified here before it is put on port 8083:

1. Recognisable heroes without new models: the real Genesis face as the head of each hero,
   body tinted with the Skin colour.
2. Attacks drawn from the art for the next Eyes families (tears and their lobbed drop, visors
   and glasses), as was done for the lasers (D20).
3. The delivery of each Tier, shown on the hero card.
4. Wallet connection through the browser's own wallet, the list of the player's Genesis, and
   the default character for a player without one (D19). No Reown package yet: a wallet cannot
   be operated from here, so this slice is checked with a stand-in provider and stays to be
   tried with a real wallet by the user.
5. A career per Genesis (matches, wins, badges) kept by the local test server, shown on the
   card, as a stand-in for the real backend.
6. The census of the hackathon gallery, which opens at 17:00 Paris.

Progress of this session:

- **Item 1 done, about 12:10.** Every Genesis hero wears its real image as its head (a round image
  that always faces the camera, in place of the model's own head and helmet) and its body takes
  the colour of its Skin trait. `GenesisAppearance`, `GenesisArt` (one image loader shared by
  the screens and the heads); Skin value of every Genesis added to the roster; the 14 skin
  colours are derived from the portraits by `tools/genesis-art/derive_trait_colors.py`
  (`content/genesis-skin-colors.json`; 13 derived, the yellow chosen by eye). Verified in
  headless Chrome on the LAN address: the player's hero and a bot show their faces, no console
  error ([capture](../research/evidence/genesis-face-on-hero-headless-2026-10-09.png)). On
  port 8083 from about 12:10 (build `20261009T100826778Z`, built 12:08 Paris). Not measured on the phone. The portrait
  disc above the name is gone, the head replaces it.

- **Item 2 done for three Eyes families, about 12:20.** After looking at the images of thirteen
  Genesis: the flash of visors, headsets and glasses is a flat fan of light opening from the
  hero, in the colours of that visor or pair of glasses (the two sides of the 3D glasses are
  blue and red); crying eyes lob a pale blue tear that leaves a puddle of tears. Drawn by
  `GenesisShape` (a few triangles and one unlit material, no particles) through
  `AttackData.VisualKind`; colours per Eyes value in `GenesisSignatureImporter.EyeLooks`
  (seventeen values authored: four lasers, three visors, six headsets and glasses, four
  crying). An attack of the other team keeps its colours and is outlined in red. Verified in
  headless Chrome: a fan fired by a bot, a puddle of tears on the ground, no console error
  ([fan](../research/evidence/genesis-fan-headless-2026-10-09.png),
  [tears](../research/evidence/genesis-tears-puddle-headless-2026-10-09.png)). Not seen: the
  tear in flight, and each of the thirteen colour sets one by one. Still generic: the wave (hard
  stares) and the nova (soft eyes), 42 Eyes values whose art shows an expression and no object;
  they need a design idea, not a colour. On port 8083 from about 12:20 (build
  `20261009T101601842Z`, built 12:16 Paris). The clock times first written for items 1 and 2
  (12:20 and 12:45) were estimates and were wrong; they are corrected here from the build stamps.

- **Item 3 done for the echo, 12:40.** The rarity of a Genesis now changes its special: an
  Uncommon Genesis (590 of 1,969) fires it twice, half a second apart, each at 60% of the
  damage. The hero card has a fourth line, "RARITY : Uncommon / ECHO fires twice, half a second
  apart, 60% each"; a Common card reads "DIRECT one hit at full strength". One hundred
  signatures are authored (fifty pairs of shape and effect, direct and echo);
  `GenesisRoster.TryGetSignature(expression, shape, effect)` falls back to the direct one.
  **Not built:** return (60 Rare Skull), persistent-zone (30 Epic Radioactive) and inversion
  (10 Legendary 1:1) play like a direct hit and their card says "not built yet, plays like a
  direct hit". Verified in a muted headless match on the LAN address: echo skills log their
  actions in pairs about half a second apart, a raw echo nova deals 1200 (60% of 2000), the
  card of token 408 shows the four lines, no console error; 20 Quantum tests pass
  ([evidence](../research/evidence/genesis-echo-delivery-headless-2026-10-09.json),
  [card](../research/evidence/genesis-echo-card-headless-2026-10-09.png)). The 60% share is a
  first number: two hits that both land are 120% of a direct hit. On port 8083 since 12:36
  (build `20261009T102725474Z`). Not measured on the phone.

- **Item 4 done with a stand-in wallet, 14:00.** The main menu has a panel that takes a
  player from the browser's wallet to a hero: CONNECT WALLET, the portraits of the Genesis the
  address holds (read on Monad by the game services), a tap to choose, and the match starts as
  that Genesis. No wallet, a refusal or a wallet without Genesis lead to a **recruit**: the
  plain character of the class the player picks, with 85% of its health (D22), and a note on
  the preparation screen saying so. Bridge `ChogWallet.jslib` (the wallet the browser provides,
  no wallet library), state `PlayerWallet`, panel `WalletPanel`, rule `Recruit`. Verified in
  headless Chrome on the LAN address with a stand-in for the wallet, five cases: none, refusal,
  an address without Genesis, the real address that holds token 1795 (its one Genesis shows),
  a real address that holds sixteen (three pages, the tapped one enters the match).
  **Not done: a real wallet.** MetaMask, Rabby or Backpack on a phone or as an extension has
  not been tried, and cannot be from here.

- **Item 5 done on a local stand-in, 14:00.** `server/` holds the game services, in Node,
  written as one handler a hosted function could run (D21): ownership read on chain, sign-in
  with a wallet signature (ERC-4361 message, `personal_sign`, no transaction), a career per
  token (matches, wins, best streak, badges First match, First win, Three in a row, Veteran),
  a top list. With a Genesis chosen, SIGN IN TO RECORD ITS CAREER appears; after an online
  match the game sends the result, a band says "career recorded", and the menu panel and the
  hero card show the career. The career is kept per token and follows the Genesis when sold;
  the services check on chain that the signed-in address holds the token before each credit.
  Verified end to end in headless Chrome with a public test key that really signs, against a
  test-only copy of the services where that key is answered as a holder (no real holder's key
  exists here): sign-in, two whole matches, "credited: 2 matches, 1 win", the band at the end.
  21 service tests, including the signature example of the web3.js documentation.
  **Limits:** a result is what the player's own page reports, nothing replays a match (D23);
  a smart-contract wallet cannot sign in; nothing is deployed, the careers are one JSON file on
  this Mac. [Evidence](../research/evidence/genesis-wallet-career-headless-2026-10-09.json),
  [services](../server/README.md).

- **Beyond the work order, 14:05.**
  - *Every special is now drawn from the art (D20).* The wave is the hero's own pair of eyes
    on a strip in its colour, rolling forward; the nova is eight of its eyes opening in a ring.
    Forty-one more Eyes values are drawn as the collection draws them (X for Dead, hearts,
    lidded and angry eyes, pupils apart, clown make-up, the ski goggles of MOG and Chog Viper,
    the pixel sunglasses of Thug). 1,074 Genesis were on generic effects before this. Seen in
    captures for five of them; eleven rare colour variants were mapped without looking at an
    image.
  - *Rare and Epic have their delivery.* Return (60 Rare Skull): out at 60%, back at 80%,
    towards where the hero then stands. Persistent zone (30 Epic Radioactive): 50%, then a zone
    for 3 s. Checked in a headless match by ticks and damage (960 then 1280; 810 then 450 a
    tick). Inversion (10 Legendary 1:1) is still not built. 180 signatures authored.
  - *A web page to find one's hero* (`web/hero/`, D24): a Genesis number or a wallet address
    gives the hero cards, with career and a top ten; about 100 KB where the game is 22 MB. On
    port 8083 at `/hero/`. Not deployed.
  - *The way online is prepared, not taken* ([DEPLOY](DEPLOY.md)): `server/src/host.js` serves
    the game, the hero page, the portraits and the services from one Node process on one
    address; `tools/release/assemble_site.py` fills `site/` from a checked build (63 MB, done
    once with the build on port 8083); `render.yaml` describes the service for Render, the host
    of the earlier game's server. Tried on the Mac through the LAN address, a whole
    menu-to-match run included. A free Render instance would erase the careers at each restart
    and sleep after 15 idle minutes (their documentation): the paid-or-not choice, the address
    and the fate of the `?token=` entry are the user's.
  - *Groundwork for $CHOG, nothing live* ([contracts](../contracts/README.md)): `ChogShop`,
    a small contract where an item is bought for a Genesis token id with $CHOG and follows the
    token, as the research proposed. 13 tests, and 2 more against the real $CHOG token and the
    real collection on a local copy of Monad mainnet: the real token accepts the permit, takes
    no fee and burns ([evidence](../research/evidence/chog-shop-fork-test-2026-10-09.json)).
    The token is `0x3500...7777`; the address in the research report's Sourcify link is its
    implementation and answers nothing. Not deployed, no transaction sent, and the game does
    not read it: what is sold, where the $CHOG goes and who deploys are the user's to decide.
  - *Test option* `?bots=181,70`: the bots are those Genesis, to watch chosen kits.
  - *Corrections found on the way:* the Aura colour on the hero card was on the wrong line
    after the fourth line was added (fixed); the README still described a portrait above the
    name and an unanswered art-rights question (both rewritten); a dead page option
    (`portrait=`) was removed.
  [Evidence](../research/evidence/genesis-eyes-and-rare-deliveries-headless-2026-10-09.json).
  Tests at this point: 30 catalog, 22 Quantum, 13 view logic, 27 services, 13 contract
  (plus 2 on a copy of the chain).

- **14:33, the ten 1:1 Genesis and two details.** Each 1:1 has its own colours for its
  attack and its hero (Blaze, Burning Skully and Midas captured); an eye drawn on a skin of
  nearly its own colour gets a lighter skin (hearts on red were hard to read); every label
  writes the names of the 1:1 the same way. 16 view tests.

- **14:57, no Genesis signature uses the bundled effect pack any more.** The marks on the
  targets (ten Cartoon FX prefabs before) are now ten drawn signs in the colour of the Aura:
  burst, flame, snowflake, star, plus, ring, arrow, bolt, cloud, bubbles; the sign pops on
  the target and a smaller one stays beside its head while the effect lasts. Reason, beyond
  D20: the project's own licence audit lists the Cartoon FX folders among what must be
  acquired or replaced before a public build. The pack is still used by the sample's basic
  attacks, and the audit's list (Photon animations, UI images, effect prefabs, one sound) is
  untouched: **a public build is still not clear of it.** New page option `?lookbook=1`: one
  screen with every drawing (ten signs, 47 pairs of eyes), captured
  ([lookbook](../research/evidence/genesis-lookbook-headless-2026-10-09.png)); the signs were
  seen there, not yet on a target in a match capture. A headless match with the new marks
  ran without error.

- **15:08, a phone browser without a wallet gets a way in.** On an HTTPS page opened on a
  phone that has no wallet, the menu panel shows OPEN IN METAMASK, which leaves for
  `https://link.metamask.io/dapp/<address of the game>`, the link MetaMask documents for
  opening an address in the browser of its app; the hero page has the same link. Tried on a
  throwaway local HTTPS host with a phone user agent: the button shows
  ([capture](../research/evidence/genesis-open-in-metamask-headless-2026-10-09.png)), the tap
  leaves for the link, and MetaMask answers with the Android intent that opens its app, which
  the test browser cannot follow. **Not tried on a phone**, and only once the game is online
  over HTTPS can it be. No such link was found documented for Rabby or Backpack: their users
  still open the address by hand in the wallet's browser.

- **15:21, a read-through of the day's code, three fixes.** A result whose sending fails
  because the services or the chain read fail is sent again (three attempts, six seconds
  apart; the services credit a match once). A wallet with more than 64 Genesis (one holder has
  68) gets the career of its chosen Genesis read by itself. Leaving a session with DISCONNECT
  brings back the menu with the wallet panel intact (seen in a capture). The whole signed path
  ran again on this build: a third match credited. 18 view tests.

- **15:30, two things a player reads in the match.** Under the special stick, a label names
  the special of the Genesis ("FLASH" over "PURIFY" in the Aura's colour). The end screen says
  "Victory!" or "Defeat" for the player's own team, where the sample said "Left Team wins!".
  Both seen at the end of a whole headless match
  ([capture](../research/evidence/genesis-end-screen-and-special-label-headless-2026-10-09.png)).

- **15:37, a player who comes back finds everything.** The chosen Genesis and the sign-in are
  now written to the browser's storage at once, not at a quit that a web page never has.
  Checked with a page reload in the same browser: the wallet is recognised without a tap, the
  third Genesis is still the chosen one and the career line shows, so the session held
  ([capture](../research/evidence/genesis-wallet-after-reload-headless-2026-10-09.png)).

- **15:50, the first screen no longer says "photon Quantum".** The menu shows "CHOG GENESIS"
  and "3v3 arena. Your Genesis is your hero." where the sample showed the logo of its engine,
  and the large emblem is hidden (`MenuTitle`, D25;
  [capture](../research/evidence/genesis-menu-title-headless-2026-10-09.png)). The menu is
  still the sample's, with its two buttons (its SELECT SCENE entry is hidden: the game has one
  arena), and the logo images are still in the build: this is a first impression, not the
  replacement of Photon's interface that the licence audit asks for before distribution.
  Two headless runs, at 15:59 and 16:03, ended on a Photon "ClientTimeout" pop-up while
  joining the room. Eight short matches run right after, alternating this build and the
  previous one, all played: it came with the moment, not with the build. The cause on
  Photon's or the network's side was not looked for.

- **16:18, the page is named after the game.** The browser tab and the line under the game
  read "Chog Genesis" where they read "quantum.unity" (the product name, set by the build
  script, same working title as the menu, D25). It moves what a browser keeps for the game,
  so settings chosen on the phone with earlier builds are asked again once.

- **16:32, a visitor without a Genesis has somewhere to go.** On the menu, when the browser has
  no wallet or the wallet holds no Genesis, a "SEE EVERY HERO" button leaves the game for the
  hero page next to it (`PlayerWallet.OpenHeroPage`, `GameServices.HeroPageUrl`). It is hidden
  for a holder. Tapped in a headless browser through the LAN address: the page lands on
  `/hero/`; the wallet check was run again with a holder and with a wallet that holds nothing
  ([capture](../research/evidence/genesis-menu-see-every-hero-headless-2026-10-09.png)).
  One run recorded the career read as cancelled by the browser while the services logged it
  answered (200); two further runs did not show it. Not looked into further.

- **16:49, a beam starts at the eyes, and the return is seen coming back.** Filming the special
  frame by frame (new `film` mode of `tools/unity/checks/browser_check.sh`; a screenshot takes
  several hundred milliseconds in the headless browser and missed everything that moves) showed
  that a beam was drawn from where its attack stood when the view woke up, not from where it
  was created: a few ticks late, so the laser began ahead of the hero (most of the way in the
  slow headless browser, an estimated one to three units on a phone, not measured there).
  `GenesisAttackView.StartOf` now takes the creation point from the simulation. Same film after
  the fix, return delivery (CHOG #1647): the beam leaves the eyes, reaches the wall, and a
  second one comes back from the far end to the hero
  ([eight frames](../research/evidence/genesis-return-beam-film-headless-2026-10-09.jpg)); a
  direct beam (CHOG #1467) also starts at the eyes. Also learnt: aimed straight up from the
  spawn point a beam is stopped by a crate after about one unit, while its return still comes
  back from the full distance, over the wall. That is the rule as written (the way back ignores
  walls), and it may be the wrong rule: to decide when balancing.
  The same film on a Genesis that leaves a zone (CHOG #1813, wave, veil): the eyes travel in the
  hero's colours, and where they end a disc in the Aura's grey stays on the ground for about
  three seconds ([twelve frames](../research/evidence/genesis-zone-film-headless-2026-10-09.jpg)).
  A return on a shape that does not travel (CHOG #1819, nova, mend) opens its ring of eyes
  twice around the hero
  ([twelve frames](../research/evidence/genesis-standing-return-film-headless-2026-10-09.jpg)).
  The return leg and the zone's colour were the two visuals of the rare deliveries not yet seen;
  both are now seen in the headless browser, not on a phone. The drawn sign on a hero that is
  hit is also seen in a live match: walking to the fight first (the check tool can now push the
  move stick with a finger), a green star, the sign of hex, follows an enemy rooted by an ally
  bot ([frame](../research/evidence/genesis-sign-on-target-film-headless-2026-10-09.jpg)). The
  larger sign that pops on the body at the moment of the hit was not isolated in a frame.

- **Item 6 done, 17:15: first census of the field.** The site's gallery is empty and, in the
  current version of the page, cannot be filled: adding a project is refused once sign-up is
  closed. The page counts 311 applications and has no form yet for the final deliverables.
  Eight public repositories were found on GitHub and read in their code: none is a real-time or
  multiplayer game; one (chog-mischief) already ships "traits decide powers" and "history kept
  per token", live, with a guest path; one (ryoko-chog) burns real $CHOG on mainnet; four
  answer at a public address. Eight out of 311 is a sample, to run again
  ([census](../research/reports/chogathon-field-census-2026-10-09.md)).

- **17:20, the working loop is in the repository.** The scripts this session used from a
  temporary folder (compile an assembly outside Unity, run the view tests, ask the Editor for a
  build, snapshot the sources, prune builds) are now in `tools/dev/`
  ([how to use them](../tools/dev/README.md)); a reboot would have lost them.
  `sh tools/release/check_all.sh` now also runs the 18 view tests.

- **Disk space.** Between 0.6 and 2.4 GB free around 17:15 (it moves with the memory the
  system swaps to disk). At 20:15 the user asked the dev agent to free space itself: 17 GB of
  state folders left under `~/.foundry/anvil/tmp` by local chain processes that had ended
  (1 to 3 October) were removed, nothing else was touched; 21 GB free afterwards. Other
  regenerable caches seen and left alone: Playwright browsers 2.3 GB, Homebrew downloads
  1.7 GB, the npm cache 2 GB, the pnpm store 3.1 GB.

- **20:40, a holder lends a Genesis with a link, and a guest plays it without a wallet (D26).**
  On the menu a signed-in holder taps LEND IT: the panel shows a link, SHARE THE LINK hands it
  to the phone's share sheet or copies it, STOP LENDING kills the links handed out so far.
  Whoever opens the link sees "YOU ARE A GUEST", the lent Genesis, who lent it and for how many
  days, plays it as its holder would, and the match goes to the career of the Genesis (counted
  as a guest's). A link lasts thirty days at most and dies when the holder stops lending or the
  Genesis leaves the wallet. The test options `?token=`, `?charged=` and `?bots=` are now
  ignored on a public address and keep working on this computer and the local network.
  Checked in two separate headless browsers through the LAN address, with the services in test
  mode (a public test key answered as a holder): the holder signs in, lends and copies the
  link; a guest without a wallet opens it, plays a whole match and the career is credited
  ("4 matches, 1 win", one of them by a guest); a loan stopped while a guest was in a match
  refuses that guest's result; a dead link says why on the menu; through a public name that
  points at this computer, `?token=1795` enters as a recruit
  ([six captures](../research/evidence/genesis-lending-headless-2026-10-09.jpg)). Tests: 35 for
  the services (six for lending), 26 for the view code. Also: the ownership check before a
  credit or for a lent Genesis now reads that one token (about 150 ms on Monad mainnet, read
  live) and no longer the whole collection. **Not tried:** a real wallet, the share sheet of a
  real phone, two guests at once. **Limits:** a link is a bearer link, whoever gets it plays;
  the match still trusts what each game declares.

- **10 October 00:55, mastery, and a second form of each attack to unlock (D27).** A Genesis
  earns mastery by playing (one point a match, one more a win; level 2 at 3 points). The menu,
  the hero card and the band at the end of a match show it ("Mastery 2  5/8"), and so does the
  hero page. At mastery 2 the Genesis may play the second form of its attack: broad beam, long
  stare, wide flash, tight nova, wide puddle (the table is in the game notes). A button on the
  menu switches the form, and says "SECOND FORM AT MASTERY 2" before that; the card names the
  form played. 180 more signatures are generated; the download grew by 32 KB. Checked: 23
  simulation tests and 29 view tests; in the headless browser, the broad beam, the wide flash
  and the tight nova filmed next to their first forms
  ([films](../research/evidence/genesis-second-forms-film-headless-2026-10-10.jpg)), the menu
  button locked for a Genesis at mastery 1 and switching for one at mastery 2, the card and
  the end band ([captures](../research/evidence/genesis-mastery-and-form-menu-headless-2026-10-10.jpg)).
  **Not seen:** the long stare and the wide puddle in a film (the puddle lands after the film
  ended; their generated numbers were read in the assets). **Not played by a person:** whether
  the two forms of a shape feel different and fair is unknown.

- **Found while filming, not fixed: attacks that are only predicted are drawn.** With the
  check tool the special sometimes showed a stray fan in a wrong direction before the real one.
  The combat log now says who starts an attack and whether that start was confirmed or undone
  (`CombatLog`, `tools/unity/checks/summarise_log.py`): in one held press of five seconds, six
  starts were predicted and undone and one, at the release, confirmed. They are predictions the
  game corrects, not attacks: nothing is spent and nobody is hit. They are frequent in the
  headless browser, which draws about twelve images a second and sends its inputs late; how
  often a phone shows one is unknown. It existed before today (the first log of this session
  already showed one use written at several ticks). To look at with the phone in hand before
  changing anything: drawing a signature only once its start is confirmed would hide them and
  delay every signature by the same few frames.

- **10 October 01:25, bodies are moved by code, and the Chog body has its place (D28).** The
  three class models no longer play the sample's animation clips: `ChogBodyAnimator` moves any
  body made of a `Body`, a head and two arms (run, attack, win, lose). A model saved as
  `ChogBody` in a Resources folder becomes the body of every Genesis hero, with its image as
  the head and its Skin colour; the game notes say what the model must respect (four parts,
  their names, orientation, origins at the shoulders). `?bodies=1` shows every body in every
  state ([capture](../research/evidence/chog-body-viewer-headless-2026-10-10.png)), and
  `?body=standin` tries the place with a stand-in of four plain parts. Checked: 34 view tests
  (the pose is a plain function), the viewer in the headless browser, a match with the
  stand-in and one without, no error. **Not seen:** the bodies moving in a match, where the
  image of the Genesis covers most of them from above. The animation clips and their
  controller are no longer played, but they are still in the project and in the build: the
  character prefabs still point at them. Taking them out is an Editor change to those prefabs,
  not done.

- **10 October 02:05, the second form can be bought with $CHOG, on a copy of the chain only
  (D29).** With a deployed shop contract known to the services, the form button reads
  "MASTERY 2, OR NOW FOR 500 $CHOG" for a connected holder; a tap asks the wallet to be on
  Monad, to let the shop take the price, then to buy for that Genesis. The purchase is kept by
  the contract for the token id, so it follows the Genesis. The services only read the chain
  (price, who owns the item; five new tests, 41 in all), the game has 39 view tests. Rehearsed
  end to end on a local copy of Monad mainnet (`tools/contracts/rehearse_shop.sh`): shop
  deployed on the copy, the game in a headless browser with a stand-in wallet that forwards
  its transactions to the copy, purchase at 500 then at 750 $CHOG, the form unlocked, the
  buyer's balance down by exactly the price, half of it in the treasury (the burn of the other
  half is what the contract's own test on a copy checks); a wallet without $CHOG is told
  before signing; with no shop set up the game offers nothing
  ([screens](../research/evidence/genesis-chog-purchase-on-fork-headless-2026-10-10.jpg)).
  **Nothing is deployed and no transaction was sent to the real chain. Never tried with a real
  wallet,** which shows two confirmations the stand-in accepts at once. To go live: decide the
  price, where the $CHOG goes and who owns the contract, deploy it, set `SHOP_ADDRESS`
  ([contract notes](../contracts/README.md)).

- **10 October 02:30, the phone can report the predicted attacks by itself.** With `perf=1`
  the overlay, and the report a phone posts every minute, carry one more line: "own attacks
  confirmed N undone M", the attack starts of the player's own hero that the verified match
  kept or corrected (`CombatLog`). A match on the phone with `perf=1` therefore tells how often
  an attack is drawn and then undone there, without anyone having to watch for it. Seen in a
  report posted by the headless browser. Also established from the system's power log: the
  Mac stopped from 21:04 to 00:34 on 9 October because it ran on battery down to 1% ("Low
  Power Sleep"), not because of an idle timer; on the charger its sleep timer is already off.

- **10 October 02:46, first numbers from the user's phone.** The user played the build of
  02:30 with `perf=1` on the Redmi (Android 10, Chrome 126, Mali-G52, canvas 1508 x 560) and
  found it "rather fluid". Two reports reached `.artifacts/perf-reports.jsonl`: median image
  31 ms (about 30 a second), the slowest 5% above 48 ms, simulation 8 ms at the median;
  **own attacks confirmed 42, undone 4**. So about one attack start in eleven is drawn and
  then corrected on that phone. In a headless control match the undone start was the same
  attack started one tick later (tick 681 cancelled, 682 confirmed), which draws two nearly
  identical shapes; nothing was changed. The cheaper fix than delaying every signature is to
  remove the drawing of a start once it is known undone: not done.

- **10 October 03:05, heroes have a visible body again (D30).** The user's report from the
  same test: "the heroes are invisible". Cause: not a missing model. The image of the Genesis,
  1.7 units wide on the head, covered the whole body from a camera that looks down at 73
  degrees, since 9 October; the entry of 01:25 above even noted that the bodies were "not
  seen" and left it there. Now the image is 1.2 wide and raised up the screen, the body is
  drawn 1.55 times larger, and the name, the health and the coins move up with the image
  (`GenesisLook`, `CharacterUIFollow`). Six layouts were compared in headless matches through
  the test option `?look=`
  ([before and after](../research/evidence/genesis-body-under-head-headless-2026-10-10.png)).
  41 view tests then. **Not seen on the phone yet**, and the body is still the class model of
  the sample in one flat colour.

- **10 October 03:10, how the game should be opened on a phone: researched, nothing built.**
  [Report](../research/reports/mobile-presentation-2026-10-10.md), three research passes, its
  ten deciding facts read again on their primary pages
  (`research/scripts/check_mobile_facts.sh`). Stay a web page opened by a link: the App Store
  forbids NFT ownership that unlocks functions, Telegram allows TON wallets only, World App
  one chain, Farcaster shows a vertical window. On Android one tap gives full screen and a
  landscape lock without installing anything; on the user's screenshot the browser bars take
  about 29% of the screen. On iPhone a page cannot go full screen: only the Home Screen
  install removes Safari's bars. The weak spot is the wallet: a phone browser has none, and
  the browsers inside MetaMask and Rabby are locked to portrait (read in their Android
  manifests), so the present "open in MetaMask" path would show a landscape game upright.
  Recommended, in order: our own game page (block 1), a 5-minute test by the user inside
  MetaMask's browser, then one connection that stays in the normal browser (Reown AppKit
  first, loaded only when the page has no wallet).

- **10 October 03:30, a Genesis can be rented for $CHOG, on a copy of the chain only (D31).**
  Asked by the user ("ok on veut ca"). New contract `contracts/src/ChogRental.sol` (no owner,
  not upgradeable, 18 tests): a holder offers a Genesis at a price a day, any wallet pays for
  1 to 30 days, the payment goes to the holder less a share burned, the Genesis never moves,
  several players can rent it at once, paid days survive a sale. The services read the offers
  and what an address rents (`server/src/rental.js`, three public routes, 7 tests) and credit
  a renter's match to the Genesis (`rentedMatches`). On the menu a holder gets RENT IT OUT
  (price steps, change, take back) and any connected wallet RENT A HERO (the offers, 1, 3 or
  7 days, pay); a rented Genesis is then picked and played like one's own. Rehearsed on a
  local copy of Monad mainnet with the real $CHOG token and collection and a stand-in wallet
  that sends real transactions to the copy
  ([screens](../research/evidence/genesis-rental-on-fork-headless-2026-10-10.jpg)): the holder
  of token 1795 offered it at 10 $CHOG a day; the same wallet rented token 3 for three days,
  paid exactly 30, its holder received 27 and 3 were burned; the rented Genesis was chosen,
  marked "RENTED 2 d 23 h", played a whole match after a sign-in, and its career was
  credited ("1 match, 0 wins", `rentedMatches` 1). Gas measured on the copy
  (`tools/contracts/measure_rental_gas.sh`). 46 view tests, 48 services tests, 31 contract
  tests. Along the way the payment steps shared with the purchase moved to
  `MonadTransactions`, and a latent fault was removed: a wallet answering stopped every call
  in flight, the read of the shop included. **Not done: a real wallet, the real chain, a
  phone.** Not deployed, so neither button shows today. The free link loan is unchanged.

- **10 October 04:20, the game has its own page, and a failed load recovers (D32).** After
  the report of 03:35 the user answered: the rental terms are accepted (D31), the page is
  approved, and the wallet recommendation of the report is followed. The user also met
  "unable to load file build/webgl.framework.js.gz" when reloading the phone. The log of the
  test server shows that load: the phone asked for the page, the loader and the data, and
  never asked for the framework file, which the server does serve; the cause on the phone's
  side is not known (the first load, 78 seconds earlier, had worked). Three changes: the test
  server accepts 64 waiting connections instead of 5; the engine's page is replaced by ours
  (`game/Assets/WebGLTemplates/Chog/`), which asks for the whole screen on its side at the
  first touch on a phone, shows the game's title and icon, carries an install manifest, and
  opens itself again when a file does not arrive (the engine's loader only reports such a
  failure and stops); the test server can drop one request on purpose
  (`--drop-first framework.js`) and with it the same error was reproduced, then seen to
  recover by itself. Checked in the headless browser through the LAN address: a match, the
  wallet stand-in, the loading screen, the notice when the phone is held upright. **Not
  seen on the phone**, where it matters: the bars, and whether the browser the user's links
  open in accepts the whole screen.

- **10 October 06:10, the page and the bodies seen on the phone; a solid capsule fixed.** The
  user played the build of 04:15 on the Redmi, in Chrome 126 (the test server now logs the
  browser). **The whole screen works there:** the reports give a picture of 1650 x 720 where
  it was 1508 x 560, about 40% more points, at the same pace (median image 33 ms, the slowest
  5% above 51 ms, worst 232 ms where it was 1233); own attacks confirmed 38, undone 1. The
  user: "much better", and the heroes "are well visible now". They also saw "a big capsule"
  on heroes at times. Cause: the sample shows a see-through capsule around a hero for the
  five seconds after it comes back, when it cannot be hurt, and the Skin colour was applied
  to every mesh under a character, that capsule included, which made it solid. The colour
  now goes to the hero's model only (`GenesisAppearance.ModelOf`). Seen in a headless match
  where heroes came back
  ([close-up](../research/evidence/genesis-immunity-see-through-headless-2026-10-10.png),
  `browser_check.sh respawn`). Not seen on the phone yet.

- **10 October 06:40, heroes that look like their Genesis: a direction proposed, a first trial.**
  The user set the order (the public address, the contracts and the video later; the Reown id
  and the deployments are theirs) and asked what comes next for "each hero recognisable",
  "knowing that we want to be ambitious". Counted from the recorded metadata
  (`tools/genesis-art/trait_census.py`): 280 visual trait values (96 Body, which is the
  clothing, 66 Head, 58 Eyes, 22 Aura, 15 Mouth, 14 Skin, 9 Accessory), drawn as a bust in
  three-quarter view; 1,309 Genesis look right, 650 look left, 10 have no side. Proposed, not
  yet accepted: do not model the collection in 3D; make the drawing itself the character. In
  order: (A) the head is the real drawing cut out of its background, in place of the round
  picture; (B) the user's Chog body in place of the sample's models; (C) the body takes the
  colours of the Genesis's clothing, computed from its picture; (D) the hero reacts with its own
  traits (eyes that light up when it attacks, another mouth when it wins or loses), which needs
  the collection's layer files; (E) the class readable at a glance. **Trial of (A) without the
  layer files** (`tools/genesis-art/cut_out.py`, numpy in a private environment under
  `.artifacts/tools-venv`): the background of each group of Genesis laid out alike is rebuilt
  from the collection (the colour most pictures agree on, point by point,
  [rebuilt layers](../research/evidence/genesis-rebuilt-backgrounds-2026-10-10.png)) and each
  Genesis is cut by difference. Among the Genesis that look left, about 3 of 16 drawn at random are cut cleanly
  ([sheet](../research/evidence/genesis-cut-out-trial-looking-left-2026-10-10.png)): there the
  background shows behind the hat in a minority of pictures only, so "what most pictures agree
  on" is a hat, not the background. Skulls (60), Radioactive (30) and the ten 1:1 have their
  own layouts. Counted by eye on small samples: an order of size, not a measure.

- **10 October 07:00, the Genesis that look right have a cut-out head in the game (step A,
  two thirds of the collection).** For the 1,309 Genesis that look right the method works:
  the background is one flat colour behind a zigzag of spikes, with the Aura as a band along
  it. Two things had to be added after a first version went into the game: a second pass
  that removes the same flat colour where the rebuilt layer knew nothing, and a rule that a
  point whose rebuilt colour is the same for most backgrounds of the group is the character
  (its face), never background. **That second one was found by looking at the game, not at
  the sheet:** the first sheet showed the cuts on a light ground, on which a pale face that
  had been cut away looked intact; in the match the crate behind the hero showed through its
  face. The sheets now use squares of two loud colours. On 46 Genesis drawn at random among
  those that look right (two draws), every one of the usual kind is cut cleanly
  ([sheet](../research/evidence/genesis-cut-out-trial-looking-right-2026-10-10.png)); the
  Skulls and two astronaut helmets are not, and are left out by rule. `cut_out.py all` writes
  what to keep of each trusted portrait (`.artifacts/genesis-art/cut/`, 5 MB for **1,277
  Genesis, 65% of the collection**) and their list; `GenesisArt` reads the list once and
  shows a Genesis that has a cut without its background, inside the same round frame, in a
  match and on the menu; the other 692 keep their whole round picture. Seen in a headless
  match
  ([close-up](../research/evidence/genesis-cut-out-heads-in-match-headless-2026-10-10.png)).
  48 view tests. `assemble_site.py` carries the cuts into `site/`. **Not seen on the
  phone.** Next for the 692 others: the most frequent colour among those that vary least and
  a vote by outlined region, in place of the median; or the layer files, if the Chog team
  gives them, which make every cut exact and step (D) possible.

- **11 October 14:25 to 15:00, the secured match and the stakes, up to Photon's own calls
  (D33, D34, D35).** Four pieces, each run:
  1. **A match is played again outside the game.** A page opened with `?record=1` on a
     private address sends the engine's record of its match when it ends
     (`MatchRecorder`, kept by the test server under `.artifacts/replays/`). Each build now
     writes the simulation's assets beside itself (`quantum-db.json`; the engine's own export
     throws on this Unity version, which writes object ids on 64 bits, so
     `QualificationBuild` writes the same file itself). `python3 tools/quantum/headless.py
     verify <record> <quantum-db.json> <report.json>` replays the record with the real
     simulation and writes who won, the score, when the selection of characters ended and who
     played in which team with which Genesis. Seen: a match of one page and bots, 3,239
     ticks in 0.78 s, team 1 by 0 to 11, the winner the game had announced; a match of two
     pages in opposite teams, 5,448 ticks, team 0 by 15 to 2, also as announced. Three rules
     of the sample's game manager live beside the simulation folder and are compiled into the
     replay's simulation (`Assets/QuantumUser/*.cs`).
  2. **The services answer Photon** (`server/src/photon.js`, `matches.js`, `judge.js`):
     custom authentication (`GET /api/photon/auth`, guarded by a key only the dashboard
     sends), the eleven webhooks under `/api/photon/hook/` (guarded by the `X-SecretKey`
     header), the admission rule at `player/add`, the record kept as received, the judge
     started when the last slice arrives. A room gets a name of its own at `game/create`,
     since a room name can be used again and the stakes are held under that name.
  3. **The stakes** (`server/src/wager.js`, `referee.js`, `contracts/src/ChogWager.sol`):
     `GET /api/wager`, `GET /api/wager/mine`, `POST /api/wager/ticket`, `POST
     /api/wager/lock`, `GET /api/wager/result`. The rules are D34.
  4. **A rehearsal of all of it** on a local copy of Monad
     (`sh tools/contracts/rehearse_secure_match.sh <record> <build folder> [cheat]`), with
     the record of the two-page match: honest match paid (+22.5, -25, 2.5 burned of 25 a
     head), and three cheats called off with every stake given back (a stake on the other
     team, no lock, a ticket kept for later, which the contract then refuses)
     ([evidence](../research/evidence/staked-match-rehearsal-2026-10-11.txt)).
  Tests: 67 for the services (19 new), 23 simulation, 48 view, 54 contracts, all passing.
  **Limits, plainly.** Photon has never called these services: its part is played by the
  rehearsal script from the manual read on 11 October (webhooks and custom authentication
  pages), so the exact shape of Photon's requests, the room name given back at `game/create`
  and the order of its calls are unverified until the dashboard is set up against a public
  address. The game connects with the wallet's session only once the services say so
  (`PhotonIdentity`, `GET /api/photon`): compiled, never run against Photon. The game has no
  staking screen. The configs of a match are still the ones the first page sends: the
  judge reports the test options (`testOptions`) but nothing refuses a match played with
  another map or other bots yet. A hosted service needs .NET 8 beside Node for the judge:
  `render.yaml` has none, so only this Mac can judge today. One player leaving does not stop
  a match; if every remaining player leaves before the end the match is void and stakes
  come back.
  **Found on the way.** The Editor would not start on 9 and 11 October "for lack of a
  licence": a licensing helper left behind by an earlier Editor (parent process 1) held the
  lock, and every new one gave up; stopping that helper fixed it. `typecheck.py --use
  Chog.Diagnostics` hides a missing assembly reference: `MatchRecorder` passed it and failed
  in the Editor.

- **11 October 15:30, the staking screens in the game, and a Chog body.**
  **Staking.** `GenesisWager` (the ticket, the three wallet steps, the lock, the claim),
  `WagerPanel` on the Team Preparation screen, `WagerResult` on the menu, two hooks in the
  sample's `CharacterSelectionUI` (show the panel; lock before the start command). The rule
  for the lock changed on the way: any wallet of the match may ask for it, since it is the
  page of the player who presses START that asks. Seen in the headless browser, on the
  bench (`tools/contracts/wager_bench.sh`: local copy of Monad, the wager, services with the
  judge; Photon's part by `server/rehearsal/photon_standin.mjs`):
  one wallet alone stakes 10, plays, and gets "NOBODY STAKED ON THE OTHER TEAM: YOUR STAKE OF
  10 $CHOG WAS GIVEN BACK" after tapping GET IT BACK
  ([before](../research/evidence/wager-screen-before-stake-headless-2026-10-11.png),
  [staked](../research/evidence/wager-screen-staked-headless-2026-10-11.png),
  [given back](../research/evidence/wager-menu-stake-given-back-headless-2026-10-11.png));
  two wallets in opposite teams stake 10 each ("2 STAKED TO WIN +9",
  [capture](../research/evidence/wager-screen-two-stakers-headless-2026-10-11.png)), the
  match is played and judged (team 0 by 12 to 0), the winner taps CLAIM and reads "YOUR TEAM
  WON: +9 $CHOG, PAID"
  ([capture](../research/evidence/wager-menu-winner-paid-headless-2026-10-11.png)), the loser
  reads "YOUR TEAM LOST" ([capture](../research/evidence/wager-menu-loser-headless-2026-10-11.png));
  on the copy the winner holds 10,009 $CHOG and the loser 9,990. 64 view tests (16 new: the
  three calls are encoded as `cast` encodes them), 67 service tests.
  **Limits.** Test wallets and a local copy only; never a real wallet, a phone or Photon's
  own calls. In these runs the stand-in tells the services who is in the match before the
  page joins and sends the record after the match, so the timing rule of D34 (locked before
  play begins) is only exercised by the tests and the rehearsal, not by these runs. The
  screens of `Assets/Chog/Screens` have no unit tests, only these runs.
  **The Chog body.** A background agent rewrote `tools/blender/make_chog_body.py`: a round
  body, big fists and shoes, eight spikes on the back, 1,900 triangles in the four rigid
  parts the game expects, checked on the exported file
  (`tools/blender/check_chog_body.py`;
  [renders](../research/evidence/chog-body-blender-renders-2026-10-11.png)). The file is in
  `game/Assets/Chog/Resources/ChogBody.fbx` since 15:28, so every Genesis hero wears it in
  place of the sample's class model, in its Skin colour. Seen in the body viewer of the build
  `20261011T132831340Z` (facing the right way, the five poses,
  [capture](../research/evidence/chog-body-viewer-headless-2026-10-11.png)) and on a hero in
  a headless match ([close-up](../research/evidence/chog-body-in-match-headless-2026-10-11.png)).
  **Not seen on a phone.** Open: the clothing colours on the body, the bush transparency a
  class model had, the body's width against walls (about 1.9 units with the hands).

- **12 October 01:30 to 02:15, a public address, the wallet from a phone's own browser, the
  container, and what the user said that night.**
  *The public address.* The user allowed the tunnel. `ngrok http 8087` in front of
  `tools/release/serve_public.sh` (the host program on the assembled `site/`), checked with
  `tools/release/check_public.sh` and one run from the menu into a match in the headless
  browser ([capture](../research/evidence/public-address-match-as-recruit-headless-2026-10-12.png)).
  The test server cannot stand there: it drops Photon's secret header and cuts bodies at
  100 KB. A first check downloaded the build unpacked and spent 140 MB of the tunnel's
  monthly gigabyte; the check now asks for headers only.
  *The wallet (D32, block 4).* A page without a wallet now offers CONNECT WALLET when the
  services give a Reown project id (`GET /api/wallet`): the page loads Reown AppKit from
  jsDelivr at a fixed version on request, opens its window over the game, and hands the game
  the connected app as the same kind of wallet object it already speaks to
  (`TemplateData/wallet-connector.js`, `ChogWallet.jslib`, `PlayerWallet.ThroughApp`; see
  `game/README.md`). The user gave a project id (their earlier project "Chog Zombies",
  "je crois"); it is kept in the data folders, never in the repository. Seen in the headless
  browser, on the local address and through the public one: the menu offers the connection
  ([capture](../research/evidence/wallet-connect-offered-without-browser-wallet-headless-2026-10-12.png)),
  the window opens and lists wallets (MetaMask, Trust, Binance, a search of "90+";
  [capture](../research/evidence/wallet-window-public-address-headless-2026-10-12.png)), a
  tap beside it closes it and the game goes back to waiting. **Not seen: any wallet app
  answering**, so nothing after the pick is verified: the approval, the way back to the
  browser, a signature, a transaction. Reown answers 403 to its own usage report from these
  addresses, which may mean the project lists its allowed domains: to be looked at in its
  dashboard. Found on the way: Unity's loader answers any error of the page, even of another
  script, with a dialog that freezes the game; the page now writes those to the console
  (`errorHandler` in `index.html`) and keeps the dialog for errors of the game itself.
  *Photon.* The user asked the dev agent to fill the dashboard in their browser ("fait le").
  Done for the Quantum application "Chog Arena" except the secret, which the browser rules
  of the agent forbid it to type: two forms are filled and left open (the authentication
  provider with its address and the pair `key`; the plugin with sixteen settings, see
  DEPLOY.md), the secret is in the user's clipboard, and the user pastes it twice and saves.
  **Not saved when this was written.**
  *The container.* The user accepts Render's free plan kept awake by a ping. Written:
  `Dockerfile` (Node 22 and the .NET 8 runtime; building it replays the recorded matches of
  `tests/fixtures/replays/` and fails on a wrong winner), `.dockerignore`, `render.yaml`
  (free plan, no disk: careers and records are lost when the container is replaced; the paid
  variant is in a comment), `.github/workflows/release-image.yml` (builds and starts the
  image on GitHub's machines). `assemble_site.py` now puts the judge and its tables in
  `site/judge/` and `host.js` finds them there. **The image has never been built**: no
  container engine runs on this Mac. Waiting for the user's word to create a private
  repository.
  *The body.* Asked by the user: the model must look like the real Chog. The official
  pictures (addresses in `tools/blender/README.md`) show the spikes on the head, a slim plain
  body and big pink hands and feet: the present body (a belly, spikes on the back) is wrong
  on all three. Variants that follow the pictures are being drawn; nothing new is in the game.
  68 service tests, 64 view tests.

- **12 October 02:15 to 02:45, a tower and a core for each team, and a body that looks like a
  Chog.**
  *The tower and the core* (the next dev action since 10 October; rules, places and limits in
  `game/README.md`, "A tower and a core for each team"). Each team defends a core in front of
  its spawn points and a tower 4 units further out. A tower takes 500 health a second from the
  nearest enemy hero within 5 units; a core that is destroyed ends the match at once for the
  other team, whatever the coins; the coins still decide a match in which no core falls.
  Nothing of the sample's assets was changed: the structures are made in code when the match
  begins, every existing attack hurts them, and the machine that runs the match ends it
  because the clock it reads says so once a core fell. The judge follows (`coreLostBy` in the
  replay's report; an even score is then no draw). On the screen the tower is a totem with a
  Chog's head that shoots with the laser of its eyes, the core a heart in a crown of spikes,
  each on a base in the colour of its side under a bar of its health.
  Checked: 28 simulation tests (five new), 68 service tests, a replay in which a core is made
  to fall (`--test-core-falls`: the match ends that second for the other team, both ways),
  the structures seen standing in a headless match
  ([close-up](../research/evidence/structures-and-body-b-close-up-headless-2026-10-12.png)),
  and whole matches recorded with them that the judge replays with the winner the game
  announced. In the first of those the towers hit 43 times (31 by one, 12 by the other;
  matches before had no hit of exactly 500) and the score was 17 to 0: the bots walked under
  the towers. A bot now keeps out of the reach of an enemy tower (`Structures.KeepAway`); in
  the next recorded match the towers hit 4 and 3 times (score 0 to 11, by coins). A line on
  the screen says when a structure falls ("ENEMY TOWER DESTROYED", "YOUR CORE FELL").
  **Not seen: the laser of a tower on a picture, a structure destroyed by play, that line, a
  match ended by a core.** The bots neither attack nor defend a structure. The numbers are
  first guesses. The records of 11 October no longer reach an end under the new rules: the
  fixture of the judge was recorded again (`tests/fixtures/replays/`).
  *The body.* Variant B of the three drawn after the official pictures (the comparison sheet
  is `research/evidence/chog-body-variants-blender-2026-10-12.png`; recommended to the user,
  who had not answered when it went in): a slim body in the Skin colour, big pink mittens
  and feet, a dark outline, no spikes. The game now colours each material of a part by its
  name. Seen on a hero in a headless match (the same close-up): a red body with pink mittens
  under the picture; and in the body viewer, its five poses with the new shoulders
  ([capture](../research/evidence/chog-body-b-viewer-poses-headless-2026-10-12.png)). Not
  seen on a phone. 67 view tests.
  The public address serves the build with all of this
  (`20261012T005009618Z-67649349660c4ffc9995937ba8b0cb9f`) since 02:54.

- **12 October 03:00 to 03:20, Photon really calls us, a private repository, and the body
  judged by the user.**
  *Photon.* The user pasted the secret and saved ("c'est fait"). Found then: the plugin had
  been saved with the secret alone (the fifteen other pairs sat in a form that was never
  saved); the dev agent added them to the saved configuration and saved it. The box "Allow
  anonymous clients" is not ticked, so a game that does not identify its player cannot
  connect: the public instance now runs with `--photon-auth`, and so do the local test
  services (port 8090). **Seen with Photon's real cloud, for the first time:** the game
  identifies its player and Photon asks us who it is (`GET /api/photon/auth`, 200, a
  visitor); then, for a match played in the headless browser from the public instance,
  `game/create`, `game/configs`, `player/add`, `player/added`, `replay/start` and a
  `replay/chunk` every ten seconds, all answered 200 and kept under
  `.artifacts/services-public/matches/`. The player had asked for Genesis 1795 by the test
  entry `?token=`, without any right to it: the admission recorded `askedToken 1795`,
  `tokenId 0`, a recruit (D35 at work), and the game did play a recruit. When the room closed
  Photon sent `game/result`, `player/removed`, `game/leave` and `game/close`, and the judge
  replayed Photon's own record (36 slices) within seconds: team 1 won, 6 to 10, which is
  what the game had announced ([journal](../research/evidence/photon-first-real-match-journal-2026-10-12.json),
  [verdict](../research/evidence/photon-first-real-match-verdict-2026-10-12.json)). Not seen
  yet: a signed-in wallet or a guest going through, two players, a stake.
  **Consequence for development:** every match of this
  Photon application now passes the admission, from any address, so the test entries
  `?token=` no longer give a Genesis to the player (the bots still are Genesis); a Genesis
  is played by its holder, its renter or a guest with a pass. A second Photon application
  without webhooks, for local builds, would give the test entries back; not created.
  *The repository.* Asked by the user for Render ("oui crée le dépôt"): 
  `https://github.com/RedGnad/chogathon`, private, one commit by the user's GitHub identity
  (13,113 files, 188 MB packed; `.artifacts/`, the build outputs of the contracts, a crash
  dump and the downloaded archives stay out). The image build on GitHub was refused before
  it began (the account cannot run Actions on a private repository: a payment or a spending
  limit to settle): the workflow is now started by hand only, a change not pushed yet. So
  the container still has never been built; Render's own build will be its first.
  *The body.* The user looked at the models and found variant B worse than the first body:
  taken alone it has no head, so no spikes and no face, and nothing of a Chog is left in 3D
  ("il n'a même plus de tête ... et encore moins de visage"); they asked whether the dev
  agent can make 3D characters at all, and offered to make it themselves. Answered: yes by
  script, stylised; a whole 3D Chog (big head, crown of spikes, cream face, eyes, body,
  mittens) is being drawn in three head styles, as pictures first, with a try of the
  Genesis's own face on the 3D head. The first body is back in the game (build
  `20261012T011501109Z-c65f190d2ad8495a86c41c7e61bf78bb`, online since 03:18).

- **Running on the Mac since 12 October 02:10.** The public address is the tunnel
  (`ngrok http 8087`, a background task of the dev agent's session) in front of
  `serve_public.sh` on port 8087, data in `.artifacts/services-public/`; it ends with that
  session or this Mac. Port 8084 follows the newest build (the wallet connector, the Chog
  body, the staking screens, which stay hidden there because its services, port 8090, have no
  wager; port 8090 has no Reown id either, so 8084 offers no connector). The connector test
  bench (port 8086 in front of services on port 8092, data in
  `.artifacts/services-connector-test/`) was stopped after its runs; the connector is tried
  on the public address. Port 8083 still serves the build of 10 October below. The Unity Editor is
  open on `game/` (opened by the dev agent; builds are asked from it). The staking bench
  (port 8085 in front of services on port 8091, with a wager on a local copy of Monad) was
  stopped after its runs: it is started again with `tools/contracts/wager_bench.sh`, and
  must stay a test bench, since its wager only exists on the copy.

- **On port 8083 since 07:00, 10 October:** build `20261010T044401171Z`, with the cut-out
  heads, the game's own page, the visible bodies, the see-through capsule and the rental
  screens, which stay hidden while no contract is set up (`site/` was last assembled from the
  build of 02:30).
  Game `http://192.168.1.85:8083/` (test entry `?token=1795&charged=1`, add `&perf=1` for
  reports), hero page `http://192.168.1.85:8083/hero/`. The local services
  (`node server/src/local.js`, port 8090) must be running for the wallet, the careers and the
  wallet lookup of the page. Port 8084 follows the newest build.

Not doable without the user: a public deployment (needs their hosting account and a place to
keep the careers), a real wallet test, and anything seen on the phone.
**Next action:** see "Where things stand" at the top. Earlier wording kept for reference: the Aura effect on the signature,
largest share first (`raw` is already half
the collection, then burn, chill and hex), with one colour and one keyword per effect and the
caps on control agreed in D18; then the delivery of each Tier with its price. Before tuning any
number, write the kit budget rule. In parallel, for the user: the art-permission question to
the Chog team and the legibility of portraits on the phone. After the effects: the wallet with
Reown, so the Genesis is chosen from those the player owns instead of typed in the URL.
Open performance items, by expected value: the 17 ms of non-simulation work (draw submission,
view scripts, UI), not yet broken down.

**Housekeeping found on 9 October:** the hackathon site extended sign-up to 72 h, so the
derived end is **23 October 17:00 Paris**, not 22 October
([site re-read](../research/evidence/chogathon-site-facts-2026-10-09.json)); the user confirms
the team is registered. The submission asks for a shared repository and the pre-existing work
used; the project is not under version control yet. The previous agent binary-patched the
Bot SDK editor DLL for Unity 6000.6 (`OnOpenAsset` int overload removed, replaced by
`CircuitWindowUnityCallbacks.cs`; tool `tools/unity/PatchCircuitOnOpenAsset.cs`; original in
`.artifacts/unity/vendor-backups/`); editor-only, not in builds. Unity left a crash-recovery
scene in `game/Assets/_Recovery/` at 01:58; it is now git-ignored and must not ship.

Le projet Fusion reste disponible pour comparer le ressenti et tester un repli si le
profilage mobile Quantum le justifie ; ses huit contrôles réseau Editor ne prouvent pas
un serveur de production ni un client navigateur multijoueur. [Procédure Fusion](../game-fusion/README.md).

## Historique conservé : installation et qualification initiale Quantum

Les sections suivantes décrivent les preuves du candidat Quantum précédent.
L'utilisateur a testé la scène Gameplay et confirme son bon fonctionnement. Le journal
montre deux démarrages en mode Local ; aucun résultat par capacité ou fin de match mesuré
n'est revendiqué. Ce test précède l'adaptation tactile ci-dessous.

Les contrôles tactiles ne dépendent plus du symbole Android/WebGL : visibilité selon le
périphérique, lecture des boutons/sticks, conservation de la visée lors du relâchement,
protection du doigt propriétaire et remise à zéro lors d'une pause/perte de focus.
`Chog.Controls` isole la lecture des inputs et la capture des pointeurs ; la simulation
reste inchangée. Les quatre assemblages concernés compilent avec les références réelles
de Unity. **Neuf tests EditMode ont passé dans Unity 6000.6.4f1**, le 8 octobre à
01:31:25 UTC, avec le vrai Input System et son fixture officiel. La demande automatique
a été consommée ; résultat dans `.artifacts/unity/latest-controls-tests.json` et XML détaillé.
Ces tests vérifient la lecture des inputs et la capture des pointeurs, pas la livraison
des événements UI ni l'ergonomie sur un téléphone réel.

Le menu **Chog → Qualification → Build Local WebGL (Bots)** construit la seule scène
Gameplay sans modifier la liste des scènes réseau ni demander d'AppId. Le build a
maintenant réussi et le match local a démarré dans Chrome (preuve plus haut). Le parcours
en ligne Quantum avec `QuantumSampleMenu.unity` a désormais un AppID Quantum 3 distinct ;
sa connexion n'a pas encore été exécutée. Téléphone déclaré : **Redmi 11C** ; navigateur,
RAM et version Android non confirmés.
Le ZIP demandé a été fourni et extrait : Quantum 3.0.9, Unity d'origine 2021.3.45f2.
Les 6 656 fichiers avaient été vérifiés à l'extraction. La migration et l'intégration
Genesis ont depuis modifié certains fichiers ; le ZIP et ses empreintes restent la référence.
La procédure et les points d'intégration sont dans [game/README.md](../game/README.md).

Après les échecs `ENOENT` de l'ancienne installation, l'utilisateur a installé et ouvert
**6000.6.4f1 arm64, sans Rosetta**, avec WebGL présent dans `PlaybackEngines/WebGLSupport`
à la racine de cette installation. Le premier import est entré en Safe Mode. Les compteurs
du script de build, les anciens IDs Unity et des API Editor du sample ont été corrigés.
Unity IAP, sans référence dans le code ou les scènes, a été retiré du manifest.
Advertisement Legacy (`com.unity.ads` 4.19.0), également inutilisé, a été retiré du manifest
et du lock résolu par Unity. Son script Editor demandait automatiquement MDR à chaque
rechargement, ce qui explique les fenêtres « Mobile Dependency Resolver required » et
« Missing signature ». Aucune installation MDR n'est nécessaire pour le jeu actuel.

Les compilations Unity de Chog.Editor, Photon.Realtime, Chog.Heroes, Chog.Genesis et
Quantum.Unity ont réussi pendant la session. Le dernier Quantum.Unity.Editor corrigé et
trois assemblages dépendants compilent également avec le compilateur et les références
réels de cet éditeur, en sortie isolée. **Unity a ensuite recompilé Quantum.Unity.Editor,
Assembly-CSharp et Assembly-CSharp-Editor avec succès** ; aucun nouvel échec C# après les
corrections dans le journal live. L'utilisateur confirme ensuite un redémarrage sans erreur ;
le nouveau journal montre le mode Default. Le blocage de compilation et de Safe Mode est levé.
Pas de stubs Unity ni de masquage des erreurs obsolètes.
[Relevé de compilation](../research/evidence/unity-6000.6-qualification.json).

Diagnostic de stockage du 8 octobre, effectué en lecture seule : environ 13,3 Go libres
avant nettoyage. Applications : 8,8 Go ; dossier utilisateur Library : 52,8 Go, dont
18,9 Go de caches ; projets CascadeProjects : 42,7 Go. Foundry/anvil/tmp (18,4 Go) et
Colima (11,5 Go) ont été mesurés mais exclus du nettoyage proposé. Certains chemins
protégés sont inaccessibles ; les sommes de `du` ne constituent pas une comptabilité APFS
exhaustive. Aucun document, source ou asset supprimé ; aucune installation Unity lancée par l'agent.

L'utilisateur a autorisé le nettoyage de cinq caches précis totalisant environ 10,8 Go mesurés :
les deux caches Bee de l'ancien chogtanks, le cache npm `_cacache`, le cache `go-build`
et le cache de compilation Go de Homebrew. Sources, assets, dépendances installées,
données Colima/Anvil et sauvegardes exclus. Plan et diagnostic locaux :
`.artifacts/storage/cleanup-plan.json` et `.artifacts/storage/diagnostic.json`.
Les cinq caches ont été supprimés après cet accord. Gain d'espace libre observé :
**9,77 Go**, soit **23,07 Go libres** après nettoyage. Les dossiers Assets, Packages et
ProjectSettings de l'ancien projet sont présents ; Anvil et Colima conservés.
Résultat détaillé : `.artifacts/storage/cleanup-result.json`. Le script affiche seulement
le plan par défaut ; son exécution explicite a été limitée aux cinq chemins autorisés.
Après installation de l'éditeur et des modules : environ 2,1 Go libres au relevé suivant,
puis des relevés fluctuants entre 0,55 et 2,65 Go pendant l'import.
Un échec temporaire `no space left on device` a été observé ; prévoir davantage de marge
avant un build WebGL. Aucun nettoyage supplémentaire hors des cinq caches n'a été effectué.

Diagnostic complémentaire en lecture seule : le dossier utilisateur mesure environ 151,3 Go,
dont 49,8 Go dans Library et **101,4 Go hors Library**. Le dossier Documents ne représente
que 0,45 Go ; projets 38,7 Go, états Anvil 18,4 Go et Colima 11,5 Go expliquent une grande
partie du volume hors Library. Un disque virtuel Colima déclare 107,4 Go logiques mais
n'alloue que 10,4 Go. Le classement macOS « Documents » n'est pas réconcilié fichier par fichier.
Certains fichiers protégés restent inaccessibles. Relevé :
`.artifacts/storage/visibility-diagnostic.json`. Aucune suppression supplémentaire.

La migration Unity du sample est désormais la base de travail. La suite vise deux clients
Quantum dont un téléphone, trois actions, deux profils Genesis visibles, puis les objectifs
MOBA. Le catalogue et la carrière gardent l'identité canonique ; l'admission et le résultat
doivent être vérifiés par backend/webhook et, si nécessaire, rejeu avant crédit définitif.

## Avancement réel

| Élément | État | Preuve / reste à faire |
| --- | --- | --- |
| Direction produit | Confirmée par l'utilisateur | MOBA multijoueur, 3D/isométrique, préférence PWA, NFT et rareté centraux, $CHOG payant, prototype vite puis enrichissement. |
| Choix réseau | Réexaminé le 8 octobre ; Quantum 3 retenu pour le développement | Sample local WebGL et premier essai Redmi fonctionnels. Fusion Server reste une preuve et un repli ; aucun gain de FPS Fusion établi. Plafond de 100 connexions Photon accepté pour le MVP. [Décision sourcée](DECISION-RESEAU-REVUE.md). Online match Mac + phone fluid per user on 9 Oct, unmeasured. |
| AppID / SDK Fusion | Configurés et exécutés | AppID appliqué, SDK 2.1.3 build 2390 importé ; compilation et connexion Photon passent. [Qualification SDK](../research/evidence/fusion-sdk-qualification.json). Aucun forfait modifié. |
| Arène Fusion | Tranche Archer livrée ; huit contrôles réseau passés | Serveur + deux clients dans un même processus Unity, physiques séparées ; autorité, mouvement, volée, zone, dégâts, départ et nouvel arrivant. Entraînement avec bot, souris et tactile émulé dans Chrome. [Preuve](../research/evidence/fusion-archer-slice.json). Respawn vu sur deux clients, multijoueur navigateur et appareil réel restent à qualifier. |
| Lecture CHOGTANKS | Effectuée, statique | [Relevé](../research/evidence/moba-feasibility-and-reuse.json) ; aucune migration réalisée. |
| Contrats Genesis et CHOG | Lectures publiques effectuées | [Preuves](../research/evidence/README.md) ; aucune transaction ni audit complet. |
| Métadonnées Genesis | 302 fichiers récupérés et analysés sur 1 969 attendus | [Recensement partiel](../research/evidence/genesis-trait-census.json), cinq tiers ; tous les metadataId 1 à 300 récupérés. Reprise limitée terminée, zéro échec sur les 62 nouvelles entrées. |
| Plan gameplay / progression / architecture | Proposition écrite | [Plan](PLAN.md) et [architecture active](ARCHITECTURE-QUANTUM.md) ; comportements à tester. |
| Sample Quantum | Build local WebGL et partie Chrome réussis ; Redmi essayé qualitativement | SDK 3.0.9.1932, Unity 6000.6.4f1. L'utilisateur rapporte un jeu assez fluide sur Redmi 11C, avec quelques saccades. [Preuve](../research/evidence/quantum-webgl-local-2026-10-08.json). FPS et replay pas mesurés. Online party match Mac + phone played on 9 Oct, fluid per user. [Evidence](../research/evidence/quantum-online-two-devices-2026-10-09.json). |
| Résolution et intégration des héros C# | Implémentées et exécutées hors gameplay | 3 fixtures réelles, 27 tests catalogue et 14 tests d'intégration Quantum passés sous .NET 8, dont application des références dans un vrai Frame. [Résultats](../research/evidence/quantum-headless-qualification.json). Binder encore non appelé par le lobby ; aucun cast/dégât ou achat exécuté. |
| Bindings NFT de prototype | Relus sur Monad au bloc 111362105 | Tokens 512 → metadata 4, 561 → 53, 973 → 465. [Réponses RPC](../research/evidence/prototype-genesis-bindings.json). |
| Unity + build mobile web | Quantum local Chrome/Redmi qualitatif ; Fusion Chrome émulé | Quantum WebGL/IL2CPP gzip ~22,2 Mo ; partie locale à bots. Fusion Archer ~12,9 Mo, sans essai Redmi. Aucune mesure comparable de temps d'image. |
| Contrôles web tactiles | Deux capacités essayées dans Chrome émulé | Deux joysticks simultanés, volée au relâchement, aperçu puis impact de la zone dans la direction visée. [Captures](../research/evidence/fusion-archer-slice.json). Aucun essai téléphone réel. |
| Écarts sample → MOBA/PWA | Identifiés ; adaptation tactile engagée | Deux slots seulement ; objectifs Coin Grab et commandes debug à adapter. [Points d'intégration](../game/README.md). |
| P0 / P1 à P6 | P0 Quantum partiel ; tranche Fusion archivée comme preuve | Quantum fonctionne localement sur Chrome et selon l'utilisateur sur Redmi ; two online clients (Mac + phone) played on 9 Oct; no MOBA/Genesis match, measured FPS, purchase or verified result. |

Preuves : [qualification C# et premier diagnostic P0](../research/evidence/p0-qualification.json),
[profils réellement résolus](../research/evidence/prototype-resolved-heroes.json) et
[inspection du sample reçu ensuite](../research/evidence/quantum-sample-inspection.json).
Le SDK .NET 8.0.425 local et le SDK Quantum officiel ont servi aux 41 tests conservés dans
[ce relevé](../research/evidence/quantum-headless-qualification.json). Ces tests SDK ne valident
pas un replay complet ou un match réseau ; le build WebGL est prouvé séparément ci-dessus.
Le premier réseau Fusion est documenté séparément ; son téléphone n'a pas été essayé.

## Journal des décisions

| ID | Statut | Décision et raison | Revoir quand |
| --- | --- | --- | --- |
| D01 | Confirmé utilisateur | Produit autonome MOBA en vue haute 3D ; PWA préférée ; placeholders d'abord. | Nouvelle instruction utilisateur ou limite mobile mesurée. |
| D02 | Recommandé | Première livraison 3v3, une voie, trois archétypes, attaque + deux capacités. | Première partie entière et essais tactiles. |
| D03 | Recommandé | Traits résolus par un catalogue versionné ; règles dédiées aux absences et 1:1. | Inventaire complet et tests de lisibilité/équilibrage. |
| D04 | Recommandé | Tier = expression mécanique de la signature ; commun viable ; variantes avec contreparties. | Tests comparatifs ; aucun équilibre supposé acquis. |
| D05 | Recommandé | Carrière et spécialisations accompagnent le NFT ; préférences/rang suivent le joueur. | Avant finalisation des règles de transfert et du commerce. |
| D06 | Historique, remplacé le 8 octobre | Fusion 2 Server avait été validé ; [motifs d'origine](DECISION-RESEAU.md). Aucun avantage mobile n'avait été mesuré. | Conservé comme référence technique. |
| D07 | Révisé avec D14 | Backend C# et résolution partagée ; admission Quantum contrôlée, résultats comparés puis rejoués si litigieux avant progression, écriture idempotente. | Premier webhook, replay et reprise après panne. |
| D08 | Recommandé | Achat $CHOG de spécialisations connues, attachées au Genesis ; base jouable sans achat. | Essais des spécialisations et fixation des paramètres de vente. |
| D09 | Recommandé | Construire une partie complète tôt, puis enrichir ; réserver la fin à la livraison. | Mesures P0, fin de chaque jalon et échéance confirmée. |
| D10 | Mis en œuvre | Bibliothèque C# pure limitée à 3 fixtures ; package UPM partagé, binder et loadout Quantum intégrés. 41 tests .NET passent avec le SDK officiel ; le lobby et les vrais assets de signature restent à brancher. | Premier combat et build IL2CPP ; extension du catalogue. |
| D11 | Intégration Fusion historique vérifiée | Unity 6000.6.4f1 arm64 + Fusion 2.1.3 build 2390 + Cecil 1.10.2 ; compilation et réseau Editor passent. Patch ciblé conservé. | Revoir seulement si Fusion redevient actif. |
| D12 | Confirmé utilisateur | 100 connexions Photon simultanées acceptables au MVP ; vérifier l'éligibilité de l'offre gratuite Quantum/Fusion pour l'AppId Quantum. Préserver PUN2 chogtanks. Aucun forfait activé par l'agent. | Éligibilité du compte et mesures de charge avant lancement. |
| D13 | Tranche Fusion historique implémentée | Archer, deux capacités, visée, caméra, feedback et bot qualifiés ; Tanknarok reste documentaire. [Périmètre antérieur](ARCHITECTURE.md#31-reprendre-lexpérience-du-twin-stick-shooter). | Repli si mesure mobile le justifie. |
| D14 | Décision active du 8 octobre | Quantum 3 dans `game/` ; sample WebGL joué dans Chrome et déclaré fonctionnel sur Redmi 11C. Pas de développement parallèle Fusion ; profils et résultats à valider par backend/webhooks/replay. [Réexamen](DECISION-RESEAU-REVUE.md). | Charge 3v3 représentative ou nouvelle exigence d'autorité/secret. |
| D15 | Audit 2026-10-09 | Independent audit keeps Quantum 3 on the Public Cloud, conditional on gate B0 (online representative load measured on the phone); result path = client results compared, replay stream archived, re-simulation for disputes. [Audit](PHOTON-ARCHITECTURE-AUDIT.md). | B0 fails after tuning, or a secret-information or money-per-match requirement appears (audit section 9). |
| D16 | Revised by the dev agent, 9 Oct 05:30 | The gate was relaxed on a qualitative report, then the first measured run showed about 28 fps and p95 57 ms at six heroes, 60 Hz: the bar is not met. Tuning comes first: one build with URL overrides compares checksum off, 30 Hz, browser-paced frames and a cheaper quality level; the best settings are applied before minions and towers. [Readings](../research/evidence/quantum-online-perf-phone-2026-10-09.json). | Tuned run still above 33 ms p95 with simulation dominant: native Android build, then audit section 9. |
| D17 | Applied by the dev agent, 9 Oct 06:05 | Simulation runs at 30 Hz with checksums off: on the phone the A/B gives about 32 fps against 26 and a Quantum update of 8 ms against 15, beyond the measured noise. Tick-counted bot timings are converted so behaviour keeps its real-time pace. [A/B](../research/evidence/quantum-tuning-runs-phone-2026-10-09.json). | Controls feel sluggish at 30 Hz in user tests, or a 60 Hz feel is wanted on strong devices (then test 30 Hz simulation with input delay tuning before going back). |
| D18 | User, 9 Oct 08:16: "je valide les directions que tu a donné" | The directions summarised to the user from the research are accepted: more than three readable roles, a price for each delivery so rarity is not power, strict caps on stun and invisibility, no Aura as a real kit, the real Genesis image on a shared body, wallet sign-in proven to a server, career keyed by token id, $CHOG purchases recorded per token. The user's own idea for binding is non-speculative badges, possibly with powers; they are unsure it is worth it. Dev agent's reading (not the user's words): badges are the non-transferable form of the career and come after the wallet step. The twelve detailed proposals of the [report](../research/reports/Héros%20Genesis%20traits%20et%20liaison%20NFT.md) were not reviewed one by one by the user; "who can play" (decision 7) and whether a lane MOBA replaces the sample's 3v3 mode are still theirs to settle. | A proposal fails its flip test in the report, or the organisers answer the art-permission and judge-access questions differently. |
| D19 | User, 9 Oct 09:30 | (1) Art: "on a tous les droits, on fait ce qu'on veut", so the Genesis images may be displayed, copied and cropped; no message to the Chog team is needed. (2) Who can play: holders play their Genesis normally; a non-holder can still play, with a default character that is somewhat weaker. This replaces the report's proposal 7 (anyone plays any Genesis) and its "full budget baseline": the weaker default is the user's choice. (3) Tone: the game will later lean adult and provocative; gameplay first for now. (4) What happens in a fight must be readable on screen: distinct visuals for attacks and effects come before more mechanics. Not answered yet: lane MOBA or the sample's 3v3 mode. | The organisers publish a rule on art or on holder-only access that contradicts this. |
| D20 | User, 9 Oct 11:25 | The differentiation of the project is that, in the end, every effect, every item and every attack represents a Chog trait or characteristic. The bundled effect pack is scaffolding: visuals have to be created from what the Genesis art shows. The user's example: the laser attack did not look like the lasers drawn on the Genesis. Applied first to the lasers (two beams from the eyes, in the colours of the Eyes value). Dev agent's reading: for each trait family, look at the real images first, then build the attack; a generic effect is a placeholder to be named as such. | A trait family cannot be drawn recognisably at game size on the phone; then it keeps a generic effect and its link to the trait is carried by colour and by the hero card. |
| D21 | Dev agent, 9 Oct 13:00, **to be confirmed by the user** | The game services are written in Node as one handler on the web-standard Request and Response (`server/src/api.js`), with storage and secret passed in, so the same code can sit behind a hosted function. Locally it runs behind Node's HTTP server and keeps careers in a file. Reason: the earlier game's backend was Node, a hosted function is the cheapest way to a public address, and no hosting account was known when the code was written. The architecture document's "services" layer is unchanged. | The user names the hosting; a platform cannot run this handler as it is. |
| D22 | Dev agent, 9 Oct 13:00, **to be confirmed by the user** | A recruit (a player without a Genesis, D19) has 85% of the health of the class and its plain special; nothing else differs. Reason: "a little weaker" had to become a number, and a single visible number is easier to explain and to tune than several. | Playtests show a recruit is hopeless or indistinguishable; the user wants another handicap. |
| D23 | Dev agent, 9 Oct 13:30, **to be confirmed by the user** | A career is credited from the result the player's own page reports, once per match and per Genesis, from the signed-in address that holds the token on chain at that moment. This is weaker than the architecture's rule ("never a victory declared by a single page") and is recorded as such: `confirmed` counts results another signed-in player of the same match reported identically, and nothing replays a match. Reason: it gives the retention loop now, on honest labels, and keeps the place for verification. | Before any reward, ranking with stakes or $CHOG use depends on a career: results must then be verified. |
| D25 | Dev agent, 9 Oct 15:50, **to be confirmed by the user** | The menu carries a working title, "CHOG GENESIS", and one line, "3v3 arena. Your Genesis is your hero.", in place of the engine's logo. Reason: the game has no name, and the first screen of a judge should not be the logo of a sample. It is the name of the collection, not a brand choice. | The user names the game; the menu is rebuilt as the game's own. |
| D24 | Dev agent, 9 Oct 14:00, **to be confirmed by the user** | A light web page (`web/hero/`) lets a holder look up any Genesis as its hero card and share it, outside the game. Reason: distribution among the about one thousand holders is the cheapest lever, and the game is a 22 MB download. It shows the same card as the game from generated data and decides nothing. | The user prefers one surface only, or wants the page inside the game's own site design. |
| D35 | Dev agent, 11 Oct; accepted by the user on 12 Oct ("je suis tes reco si elles sont pertinentes pour un produit qui va au market sur le long terme") | A player whose page declares a Genesis it has no right to is not refused: it enters the match as a recruit (the webhook answers with its data put right). Reason: a refusal leaves the page inside a match with no character and no message, and the same case happens honestly (a Genesis sold or a rental that ran out during a session). The same answer clears a loadout named directly and a character left to the machine, and gives the second form only to a Genesis that has it. | A screen that tells the player why it plays a recruit; the first real webhook. |
| D34 | Dev agent, 11 Oct; accepted by the user on 12 Oct on the same words as D35. Told to the user as still to settle before real amounts: where the referee's key lives (it signs every result; today a file), the share burned and a ceiling on a stake (theirs to choose), and a legal review (staking money on the outcome of a game is regulated in many countries) | How a match is staked on. (1) A wallet the webhooks let into a match asks the referee for a ticket and stakes on the contract; the first ticket fixes the stake of the match; a ticket lasts 120 seconds. (2) Before the match begins the stakes are locked: the game asks for it when its player presses START, and any wallet of the match may ask; the lock waits until every ticket handed out was used or has run out, so nobody can stake once play has begun. (3) The team is the one the player's page declares; it is checked afterwards against the replayed match. (4) The result is the verdict of the replay; an even score is a draw, which the game itself gives to the second team. (5) The match is called off, and every stake given back, whenever something cannot be trusted: never locked, locked after the selection of characters ended, a stake that is not in the book, a staker who did not play or played in the other team, a Genesis other than the one admitted, a record that is not whole or does not reach the end. (6) The referee decides once for a match and never signs two decisions. Reason for refunding rather than punishing: a doubt must never pay anyone, and the contract already gives every stake back after an hour without a result. Known holes: the configs of a match are those of the first page; a team that is losing cannot call the match off alone, but all remaining players leaving together can. | Photon's first real calls; the staking screen; the user's view on the stake sizes and the burned share (a tenth in the rehearsal). |
| D33 | User, 10 Oct 07:40 ("aller vers la version du jeu sécurisée pour eviter le cheat et pourvoir lancer le pari est bien meilleur ROI que le cosmetique", "je retire ce cadrage", "Ton calendrier est trop laxiste") | (1) Staking $CHOG on a match is in scope; the line "no wagering and no token rewards per match" in the scope of the audit of 9 October is withdrawn. (2) The secured version of the game comes before cosmetics: who plays which Genesis and who won must not depend on what a player's page declares. The path is the one the audit verified on Photon's documentation: custom authentication, the AddPlayer and GameConfigs webhooks, the input history streamed by Photon's server, and a re-simulation of the match outside Unity that gives the result; a stake is paid out on that result only. (3) The project is a show of force: ambition is wanted wherever it serves fun and wow, without over-engineering; the calendar of the previous point was judged too slow, the user works on it full time. (4) Otherwise the recommendations of the 07:20 point are followed: a tower and a core on the present map first, a Chog body tried by script, a home screen. The user announced attacks for adults to come, not specified yet. |
| D32 | User, 10 Oct 04:00 ("je valide", "faisons au mieux je suis ta reco") | The game stays a web page opened by a link, with its own page: the whole screen on its side at the first touch, installation offered and never required, no native app. For the wallet on a phone, a connection that stays in the normal browser is to be added (Reown AppKit first, loaded only when the page has no wallet), per the [report](../research/reports/mobile-presentation-2026-10-10.md). The page is built; the connection is not, and needs a Reown project id and a public https address. |
| D31 | User, 10 Oct ("ok on veut ca, les juges sont sensé avoir un wallet et du $chog"); terms proposed by the dev agent and **accepted by the user on 10 Oct, 04:00 ("ok")** | A Genesis can be rented for $CHOG through a contract (`ChogRental`). Asked by the user: the renter pays, the holder is paid. The terms, still changeable before a deployment: the holder sets a price a day; the payment goes straight to the holder less a share burned, fixed at deployment (a tenth in the rehearsal); the Genesis never moves and the holder keeps playing; several players may rent the same Genesis at once; 1 to 30 days, more days added after those paid; paid days survive the offer being taken back and the Genesis being sold; a renter's matches count for the career of the Genesis; the free link loan (D26) stays beside it; the contract has no owner. Not deployed. |
| D30 | Dev agent, 10 Oct 03:05, **to be confirmed by the user on the phone** | A hero is drawn as a body under the image of its Genesis: image 1.2 units wide, raised 0.55 up the screen, body 1.55 times the size of the model, name and bars raised with the image. Reason: the user reported invisible heroes; the image, 1.7 wide, hid the whole body from the game's camera. Values picked from six headless captures, changeable with `?look=` on a private address. |
| D29 | Dev agent, 10 Oct 02:05, **to be confirmed by the user** | The first thing sold for $CHOG is the second form of a Genesis's attack, at once instead of at mastery 2, through the shop contract written on 9 October (item 1, `approve` then `buy`, recorded per token id). Reason: it is the plan's first paid use (section 5: a permanent specialisation bought for a Genesis, also reachable by mastery, never strength), it uses $CHOG in a way that belongs to the NFT, and it needs no new contract. Built so that nothing shows until a shop address is set. The price, the treasury, the burned share and the deployment are the user's. | The user wants something else sold (a cosmetic), wants the purchase in one signature (permit), or wants no purchase at all; the token's rules change. |
| D28 | Dev agent, 10 Oct 01:25, **to be confirmed by the user** | Bodies are moved by code (`ChogBodyAnimator`) and not by animation clips, for the class models now and for the Chog body the user announced (four rigid parts, no skeleton). Reason: the user's model has no skeleton, so something has to move it; the same code removes the game's need for the sample's eleven clips and controller, which the licence audit lists among what cannot ship. The Chog body is taken from `Resources/ChogBody` when it exists; until then the class models stay. | The user's model comes with a skeleton and its own clips; the moves read badly on the phone. |
| D27 | Dev agent, 10 Oct 00:55, **to be confirmed by the user** | A Genesis earns mastery by playing (one point a match, one more a win; level 2 at 3 points, then 8, 15, 25, 40), and at mastery 2 it may play a second form of its attack, chosen on the menu: wider or longer at the price of damage or reach, never plainly better. This is the plan's "mastery opens a specialisation" (section 5) and the user's own idea of non-speculative badges that may carry powers (D18), built on the career so it follows the Genesis to a new holder or a guest. It is also the place where a $CHOG purchase can later unlock the same form at once; nothing is sold today. Mastery comes from the same self-reported results as the career (D23): a holder can cheat it for their own Genesis, which gains a side-grade. | Playtests show one form is plainly better; the user wants forms earned otherwise, sold only, or not at all; results become verified and the thresholds need another scale. |
| D26 | Dev agent, 9 Oct 20:30, **to be confirmed by the user** | (1) A holder can lend a Genesis with a link: whoever opens it plays that Genesis without a wallet, for thirty days at most, until the holder stops lending or the Genesis leaves the wallet; the guest's matches count for the career of the Genesis. This is extension E6 of the plan, taken now because it answers three things at once: a judge or a friend without a Genesis can play one (the census found a rival with a guest path and none for us), every holder gets a reason to pass a link on, and D19 stays whole (only holders decide who plays a Genesis; everyone else is a recruit). A link is a bearer link: several guests may use it at once. Nothing is signed for a loan; the signed sign-in proves the holder. (2) The test options `?token=`, `?charged=` and `?bots=` only work in the Editor and on a private address; on a public address they are ignored, as the plan requires ("no mock of ownership stays usable in the public version"). | The user wants lending limited (one guest at a time, a named wallet, a shorter life), signed by the wallet each time, or not at all; the organisers give judges Genesis of their own. |

## Points ouverts concrets

| Sujet | Comment le résoudre | Bloque |
| --- | --- | --- |
| Multijoueur Quantum | AppID Quantum 3 set; party-code match Mac Editor + phone played on 9 Oct, fluid per user, unmeasured. Quick Play did not pair two clients (likely region split). Reconnection impossible while PlayerTTL is 0. [Evidence](../research/evidence/quantum-online-two-devices-2026-10-09.json). | Reconnection and pinned-region matchmaking before public tests. |
| Marge disque | Espace libre fluctuant : jusqu'à 0,3 Go pendant le premier build Archer, puis ~26 Go après changement de l'état du Mac. Un SDK .NET temporaire de nos artefacts a été supprimé ; aucun fichier utilisateur n'a été touché. Mesurer avant les prochains builds/modules. | Installation du module Linux et futurs builds. |
| Vérification des résultats Quantum | Implémenter admission par webhook, GameResult, comparaison et rejeu des litiges ; aucun résultat client seul ne crédite la carrière. | Carrière publique fiable. |
| Téléphones de référence | Phone GPU Mali-G52 MC2 (Helio G85 class, consistent with Redmi 12C). At 30 Hz: about 32 fps, median frame 30 to 31 ms, six heroes, lowest quality level; non-simulation work 17 to 18 ms is the limit. [A/B](../research/evidence/quantum-tuning-runs-phone-2026-10-09.json). iOS and a mid-range phone not measured. | Budget check as minions and towers land. |
| Droits des animations du prototype | Meshes KayKit et sons ont une source CC0 identifiée. Les cinq `.anim` et le contrôleur copiés du sample Quantum restent à remplacer par des exports KayKit directs et un contrôleur original, ou à couvrir par licence individuelle. [Provenance](../research/evidence/fusion-archer-slice.json). | Distribution publique du build. |
| Mapping exhaustif des vrais traits | Inventaire, table créative explicite et validation de couverture. | Ouverture du catalogue à tous les Genesis. |
| Visuels des 1:1 | Examen individuel lors de l'attribution de leurs signatures ; placeholders possibles d'abord. | Personnalisation fidèle de chaque 1:1, pas le premier combat. |
| Prix, treasury, hébergement et domaine | Choix avant transactions ou publication ; préparer les configurations auparavant. | Achat réel et déploiement public. |
| Calendrier officiel | Sign-up extended to 72 h; derived end 23 Oct 2026 17:00 Paris from the site's client code, re-read 9 Oct. Team registered (user). Deliverables include a shared repository and declared pre-existing work. [Site re-read](../research/evidence/chogathon-site-facts-2026-10-09.json). | Exact freeze; repository and licence declaration before submission. |

## Règle de mise à jour

Au début d'une session : lire l'état courant, reprendre la prochaine tâche et les décisions
applicables. À la fin : noter changements, tests réellement exécutés, preuves et prochaine
action. Si une décision change, modifier le document actif et la ligne D correspondante ;
ne pas laisser deux recommandations contradictoires avec le même statut.

Une tâche terminée mentionnera le build/commit, le scénario testé, l'appareil si pertinent,
le résultat et les limites. Une dette temporaire mentionnera son effet et le jalon qui doit
la solder. Aucun résultat ne sera ajouté rétroactivement parce qu'il semblait probable.
