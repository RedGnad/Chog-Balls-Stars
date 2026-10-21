# Ladder references and the rules proposed for Chog Arena

Read on 21 October 2026 (stream E). Every claim below names its source; what could not be
read today is said so. Nothing in the "our rules" section is a measured balance: the numbers
are taken from the references and the reason for each is written beside it.

## 1. Brawl Stars: trophies (participation) versus Ranked (skill)

### Trophies

Source: Supercell, "Trophy Season Rework is Coming!", 15 October 2024,
<https://supercell.com/en/games/brawlstars/blog/news/trophy-season-rework-is-coming>.

- One trophy count per Brawler; "you will progress a Tier every 20 Trophies"; Max Tier at 1000.
- Gains and losses: "adjusted to fit better with the new system and be more forgiving under
  1000 Trophies"; "You only start losing more Trophies than you win after 1100 Trophies".
  The post gives no per-range table, and the community tables (Brawl Stars wiki, esports sites)
  could not be read today (HTTP 403, 402 and 405), so no per-range table is cited here.
- Seasonal reset: "The now monthly Trophy Season reset will always reset all Brawlers back to
  1000 Trophies"; "There's no Trophy reset for any Brawler below 1000 Trophies"; "Any Trophies
  gained above 1000 Trophies are now Season Trophies", which fill a Trophy Box (cosmetics).
- The season "will start and end at the same time as the Brawl Pass season".

Source: Supercell, "Release Notes February 2026",
<https://supercell.com/en/games/brawlstars/blog/release-notes/release-notes-february-2026/>.

- "Matchmaking will now be based on hidden MMR decoupled from Trophies."
- Bot matches are on at Prestige 0 to 2 and off from Prestige 2 upwards; the Free Play slot has
  "random matchmaking & no Trophy gain/loss"; "Win-trading protection more strict at Prestige 2+";
  a "Trophy exploit where intentionally losing Free Play matches could result in bot matches" was
  fixed. Bots are an entry ramp, not a way up.

### Ranked

Source: Supercell, "Ranked is dead, and Ranked is back!!", 13 February 2025,
<https://supercell.com/en/games/brawlstars/blog/news/ranked-is-dead-and-ranked-is-back/>.

- Seasons are monthly: "Ranked 2.0 seasons will start on the third Thursday of each month".
- Points ("Elo") per result: "Elo loss is more punishing for Legendary and above"; each Masters
  tier needs 1,000 Elo and each Legendary tier 750. The post gives no per-match amount; the
  amount depends on the opponents' hidden rating (the February 2026 notes above say matchmaking
  uses hidden MMR).
- Demotion: "you can now be demoted back to Diamond if you lose more than you win in Mythic";
  rank floors stop at Mythic.
- Season end: "it's always -6 minor ranks from your ending rank" for Diamond I and above;
  "Gold 3 and below reset to Silver I".
- Matchmaking "is more tightly restricted to your current rank at higher ranks" and no longer
  widens the Elo range beyond the rank restriction.

## 2. Clash Royale: trophies, gates, leagues, reset

Source: Supercell, "April Update: What Is Trophy Road?", 19 April 2019,
<https://supercell.com/en/games/clashroyale/blog/release-notes/new-update-feature-enter-the-trophy-road>.

- Trophy Gates: "When you pass through a Trophy Gate, you will NOT drop below that threshold";
  "Once you hit 4000+ Trophies, Trophy Gates do not exist".
- Season reset: "the Trophy reset amount will be 75% of a players current Trophies, down to a
  minimum of 4000" (it "replaces the old method of the Season Trophy reset being 50%"); the
  example is 5000 to 4250, and the rewards from 4000 to 4250 can be collected again.

Source: Supercell, "Summer Update", 4 June 2021,
<https://supercell.com/en/games/clashroyale/blog/release-notes/summer-update-is-here-2>.

- Leagues start at 5000, the road ends at 8000; the reset became dynamic: "the more you climb,
  the less you'll get reset at the end of a season"; below 5000 after reset you leave the Leagues.
- "Starting Master I, you will now lose the same amount of trophies for a defeat as you gain for
  a victory. The exact amount will still depend on your opponent's trophy count."

Source: Clash Royale support, "Seasonal Trophy Road", updated 17 October 2026,
<https://support.clashroyale.com/hc/en-us/articles/49484906823195> (read through the help
centre's JSON API; the HTML page sits behind a browser check).

- The seasonal road "unlocks once you reach 14,000 Trophies"; its own count starts at 0 each
  season and "Your Trophy Road progress remains unchanged" at season end; past 500 seasonal
  trophies "your Trophy gains per battle will be reduced from 30 Trophies to 6 Trophies".

What the two games share: a participation count that is generous at the bottom (bots, floors,
gates), that stops being generous at the top (equal or larger losses, smaller gains), and a
seasonal reset that keeps a floor and takes back part of what is above it.

## 3. A simple rating: Elo, and why not Glicko-2 here

Source: Wikipedia, "Elo rating system", <https://en.wikipedia.org/wiki/Elo_rating_system>.

- Expected score of A against B: `E_A = 1 / (1 + 10^((R_B - R_A) / 400))`.
- Update after a game with score `S_A` (1 win, 0.5 draw, 0 loss): `R'_A = R_A + K (S_A - E_A)`.
- FIDE's K: 40 for a new player until 30 games are played, 20 for players rated under 2400,
  10 at 2400 and above. "New players are assigned provisional ratings, which are adjusted more
  drastically than established ratings."

Source: Mark E. Glickman, "Example of the Glicko-2 system", Boston University, 22 March 2022,
<http://www.glicko.net/glicko/glicko2.pdf> (the PDF's text streams were decoded locally).

- An unrated player starts at rating 1500, RD 350, volatility 0.06; the system constant tau is
  chosen "between 0.3 and 1.2"; the internal scale is `mu = (r - 1500) / 173.7178`.
- "The Glicko-2 system works best when the number of games in a rating period is moderate to
  large, say an average of at least 10-15 games per player in a rating period."

Why Elo and not Glicko-2 for us: Glicko-2 updates once per rating period and wants 10 to 15
games per player per period; our field plays a handful of verified human matches a day. Elo
updates one match at a time, is a two-line formula anyone can audit against the event log, and
FIDE's K schedule already encodes "new players move fast, settled players move slowly".

## 4. Photon: carrying a rating band on a room

Source: Photon Realtime manual, "Matchmaking and Lobby",
<https://doc.photonengine.com/realtime/current/lobby-and-matchmaking/matchmaking-and-lobby>.

- SQL lobbies keep rooms in a table with "up to 10 special SQL filtering properties", named
  "C0" to "C9", integer or string; `JoinRandomRoom` takes a WHERE filter, up to three chained
  filters separated by `;`, "tried in order"; only `FillRoom` matching is supported.
- The manual's own skill-matching recipe: "each room gets a fixed skill that players should
  have to join it. This value should not change"; filter "skill +/- X", relax over time, then
  create a room with your own skill.

Source: Photon Quantum manual, "Webhooks", <https://doc.photonengine.com/quantum/current/manual/webhooks>.

- The `game/create` request carries `EnterRoomParams.RoomOptions.CustomRoomProperties`, and the
  response may return `EnterRoomParams` to "Enforce selected Room Options during its creation";
  `CustomRoomPropertiesForLobby` takes at most 3 keys.

So the services can set the band of a room themselves, in the `game/create` answer, from the
creator's wallet in the cookie: a page cannot claim a band it does not have.

## 5. Our rules (proposal for the lots that follow)

Tiers stay at Bronze 0, Silver 100, Gold 250, Star 500. The base scale stays: with 1 to 6
authenticated humans a win gives 3, 6, 10, 13, 17, 20 and a loss takes 2, 4, 6, 8, 10, 12.

1. **Bots are for starting.** A match with no other authenticated human (one signed wallet,
   the rest bots or guests) can raise a wallet only up to Silver (100): the gain is cut at 100,
   and at or above 100 such a match changes nothing, win or loss. Below 100 a loss still costs
   its frozen amount, to the floor of 0. Reference: Brawl Stars turns bot matches off past the
   first Prestige levels and gives Free Play no gain or loss.
2. **The top is not generous.** From Gold (250) a win pays 75% and a loss costs 125% of the
   base, so with six humans 15 and 15: the Clash Royale Master I rule, losses equal gains. From
   Star (500) a win pays 50% and a loss costs 150%: 10 and 18 with six humans, the Brawl Stars
   rule above 1100, losing more than winning. Rounded to the nearest point, never below 1.

   | Humans | Bronze and Silver | Gold (250 to 499) | Star (500 and up) |
   | --- | --- | --- | --- |
   | 1 | +3 / -2 (capped at 100) | 0 / 0 | 0 / 0 |
   | 2 | +6 / -4 | +5 / -5 | +3 / -6 |
   | 3 | +10 / -6 | +8 / -8 | +5 / -9 |
   | 4 | +13 / -8 | +10 / -10 | +7 / -12 |
   | 5 | +17 / -10 | +13 / -13 | +9 / -15 |
   | 6 | +20 / -12 | +15 / -15 | +10 / -18 |

3. **Seasonal reset with a floor.** At the season's end every wallet above Gold (250) comes
   down to `250 + round(0.5 x (points - 250))`; a wallet at or below 250 does not move. Gold is
   kept like Clash Royale keeps 4000 and Brawl Stars keeps 1000; Star has to be earned again
   every season. 50% of what is above the floor is Clash Royale's value before 2019, between
   its current 75% and Brawl Stars' 100%; our seasons are short and the field small, so the top
   must be re-earned rather than kept. One immutable reset event per wallet, with before, after
   and the rule used.
4. **Every event freezes its own scoring**: the base amounts, the solo cap, the band factors and
   their thresholds are written in the match event; the reset writes its floor and its share.
   A later change of any number changes no past result.
5. **Hidden rating**: Elo, start 1500, K 40 for a wallet's first 30 rated matches and 20 after
   (FIDE), from verified replays with at least one authenticated human on each team. A team's
   rating is the mean of its authenticated humans; bots and guests are not rated and do not
   move anyone. Draws score 0.5. The rating never resets with the season (Brawl Stars keeps the
   hidden MMR apart from trophies). It is never shown as a number: the API gives a band only.
6. **Bands**: 150 points wide, centred on 1500, numbered 0 to 6 (band 3 holds 1425 to 1574).
   One band apart is at most 300 points, where Elo's expected score is still 15% to 85%; a
   `+/- 1` band filter is the first relaxation, `+/- 2` the second.
7. **Room property**: SQL lobby, key `C0` = the creator's band, written by the services in the
   `game/create` answer and listed in the lobby; join with the chained filter
   `C0 = b;C0 BETWEEN b-1 AND b+1;C0 BETWEEN b-2 AND b+2`, then create a room when nothing
   matches. The exact client change is in the stream E report (the lead owns the menu).
8. **Season**: an id and an end date in the services' configuration; a summary per wallet (best
   tier, matches, wins, points) and, at the close, the list of wallets with their final tier for
   a $CHOG reward paid later by stream F.
