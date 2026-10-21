# Post-match progression and competitive identity

Design contract, 20 October 2026. This is the intended product behavior, not a claim that
every item below has shipped. The result screen, mastery road, on-chain career and Photon
Quick Play already exist. A player rating and a ranked queue do not.

## 21 October local ranked-points candidate

This candidate adds player ladder points, not a skill rating or a separate ranked queue.
Photon still fills a compatible Siege room first and starts with bots after the lobby clock;
the preparation caption now states the human count. No client claims a rank result. The
server writes one event per replayed match and credits authenticated humans in a completed
Siege 3v3 with normal simulation options and a non-void verdict. The replay counts real
humans at selection end; bots fill the remaining seats. Only distinct signed-in wallets
increase the award, so guest tabs cannot inflate it. A win gives 3, 6, 10, 13, 17 or
20 points for one through six authenticated humans respectively. A loss takes 2, 4, 6, 8, 10 or 12,
down to a floor of zero; a draw changes nothing. The event freezes its own scoring so
later balance changes do not alter earlier results.
Bronze begins at 0, Silver at 100, Gold at 250 and Star at 500. These thresholds are
initial design values, not tuned against a real player distribution. The rank belongs to
the signed-in wallet, not its Genesis. Guests can occupy a human seat but neither earn
points nor increase the reward scale. Preview, Coins, unverified and identity-invalid games award no player
points. A bot-filled Siege now gives slower rank progress, not zero. Genesis mastery remains
separate. This is a participation-weighted ladder, not a skill rating: repeated solo bot
wins can still raise it. Measure that behavior before tuning limits or calling it competitive.

The player chip shows a compact rank badge. The result shows `VERIFYING` until the
server resolves the match. Only after the signed-in player's latest match is matched
to an immutable server event does it display the actual point difference and animate a
rank card. The card uses one ink-edged surface, a single gold point or tier headline,
one short chime and a brief scale settle; reduced motion uses a fade. A casual result
states `NO RANK POINTS`. The rank reveal waits for a Genesis mastery reveal so the two
never compete for attention. No badge is placed above a fighter's head.

This candidate must not be called a durable public ranked system yet. The free Render
instance has no disk: local match records are erased on a restart or deploy. The code
now supports a separate durable Upstash store for immutable player rank events, while
Render remains the game host. Its public health endpoint confirmed writable rank
storage on 21 October, but rank credit remains disabled.
A signed-in bot match and a six-human match still need live end-to-end verification of
their rank credit. The present matchmaking is room filling, not skill matching. The
host keeps rank disabled unless `RANK_ENABLED=1` and a startup write probe succeeds;
the public deployment leaves it off.

Local runtime evidence from 21 October: [lobby human count](../research/evidence/chog-rank-lobby-runtime-2026-10-21.png),
[casual bot result](../research/evidence/chog-rank-casual-result-2026-10-21.png), and
[rank-up visual fixture](../research/evidence/chog-player-rank-preview-2026-10-21.png).
The last image uses a synthetic 75-to-100 point transition and says so on the screen;
it is evidence of WebGL layout and animation, not of a server credit. A signed-in bot
match and durable rank event still need end-to-end proof. The points rule has server tests,
not a claimed live rank result.

## Current implementation and evidence

The deployed client adds a confirmed-result reveal to the existing mastery road. It uses
the old and new career snapshots, waits for `GenesisCareers.Recorded`, and infers a zero
baseline only when the confirmed career says this was its first match. A private local
`?rewardpreview=1` fixture demonstrates the gain, level and badge states without
writing a career. The fixture labels its data as a design preview. These captures show
the [desktop reveal](../research/evidence/chog-post-match-preview-desktop-2026-10-20.png)
and [touch-emulated final state](../research/evidence/chog-post-match-preview-touch-2026-10-20.png).
The [Reward Animation setting](../research/evidence/chog-reward-animation-settings-2026-10-20.png)
is scoped to this reveal; turning it off retains a fade and the final state.
The [recruit result](../research/evidence/chog-post-match-recruit-runtime-2026-10-20.png)
comes from a completed local bot match in the final build.

The preview is not proof that a signed-in Genesis career was credited on Monad. No
ranked queue, player rating, skill filter or new in-match rank badge has been shipped
by this slice. The client was deployed on 21 October with player rank gated off.
A real signed-in career result,
including its first match and a refused credit, still needs end-to-end verification.

## Product decision

One match must answer three questions in order: who won, what was actually recorded, and
what the Genesis can do next. Use the existing per-Genesis mastery points as the sole
post-match progress value. Do not rename them XP or add an unbacked second counter.
Mastery belongs to the NFT and follows a transfer. A future competitive rating belongs
to the authenticated player, never the NFT. Recruit or guest sessions without a durable
player identity remain unranked. No $CHOG, stake or paid action affects rating.

## The reward moment

The result composition remains: the 3D hero on the left, the mastery road on the right,
PLAY AGAIN and HOME beneath it. The right card has three mutually exclusive states:

| State | Copy | Motion | Source |
| --- | --- | --- | --- |
| Waiting | `VERIFYING MATCH` and `Mastery updates after the referee records it` | None beyond the existing result entrance | No trusted career delta yet |
| Confirmed, no prior snapshot | `RECORDED ON MONAD`, current level and points | Fade in the confirmed state; no numeric gain claim, except the first-ever match whose prior total is certainly zero | Refreshed career only |
| Confirmed, prior snapshot | `+1` or `+2 MASTERY`, then the new level and any new badge | One gain reveal, a single bar sweep, then a level or badge reveal if earned | Difference between known before and confirmed after |

Visual hierarchy: the earned number is the largest element in the right card, gold on
night purple with one near-black contour. The filled road is green. A level threshold
changes its node from pending gold to earned green. At that instant, a short `MASTERY N`
banner appears over the road, with `SECOND FORM UNLOCKED` only at level 2 if that
Genesis has a second form. New career badges appear one at a time as small earned chips,
not as a second currency or random drop. The overlay never obscures the hero, buttons,
or winner. No confetti, spray, rays, double strokes or unrelated decoration.

Timing at 60 fps, with unscaled time so paused simulation does not stall the result:

1. 0 to 0.18 s: card veil fades down; `RECORDED ON MONAD` becomes legible.
2. 0.18 to 0.46 s: the earned number scales from 0.86 to 1.05 to 1.00, with one low chime.
3. 0.46 to 1.25 s: the mastery road advances from the known old total to the confirmed
   total. A second chime sounds only for a second point.
4. 1.25 to 2.15 s: if a threshold or new badge was crossed, the relevant node or chip
   is emphasized. The new state remains visible after the transient overlay clears.

The reveal is skippable by tapping its card, or by PLAY AGAIN/HOME. No mandatory wait is
added to those buttons. Reduce-motion mode should replace scale and travel with a short
fade while preserving the final number, level, badge and audio toggle. The baseline
version must never flash the whole screen.

## Badge placement

The current First Match, First Win, Three in a Row and Veteran badges are *career*
achievements, not competitive ranks. Their earned state can be shown on the hero card,
result road and team preparation card. On a phone, no badge sits over a fighter's head:
health, team and attack cues retain priority. A future competitive badge belongs in the
player chip, preparation slot and scoreboard, labelled `RANK`, never as a Genesis trait.

## Current visual debt, observed rather than assumed

The [20 October lobby capture](../research/evidence/chog-button-system-lobby-2026-10-20.png)
uses the new title and button system, but `Frame / Ping` at the top left is still a
technical sample readout, produced by the legacy `Text` field in
`game/Assets/Photon/Quantum/Runtime/QuantumStats.cs`. The [19 October running match capture](../research/evidence/chog-visual-runtime-match-desktop-2026-10-19.png)
also shows dense names/health readouts and small utilitarian HUD copy. These are not
evidence that the whole live UI uses Arial: the older 9 October result capture predates
the new fonts. They *are* evidence that global UI polish is unfinished. Review the actual
TMP font assignments and visual hierarchy per screen before changing them; the order is
result, preparation, combat HUD, then optional diagnostics. Do not put new rank badges
above a fighter's head while that nameplate needs simplification.

## Small-population matchmaking

Photon already performs random room matching. The live app has two separate modes, Siege
and Coins, and a lobby that starts after 30 seconds and fills empty places with bots.
Do not present this as skill matching. The next safe improvement is to measure and show
the human count in preparation, preserve the chosen mode and a common region, and prefer
the oldest compatible room. A rating filter needs server-issued player identity and
match history, not a client-chosen room property. Do not split the population into several
rank buckets before concurrent human play supports it.

For a future skill-rated competitive queue, use one opt-in Siege queue, six authenticated
humans, no bots, a verified and idempotent result, and a server-held rating separate from
the participation-weighted ladder above. Never adjust that skill rating from a bot match,
an incomplete replay or a disconnected game whose outcome cannot be judged. Show an
honest bot fallback after a bounded wait. This is a later implementation gate, not a
feature of this slice.

## Acceptance evidence

- Capture the same running WebGL result at waiting, gain and level-up states beside this
  contract. A static drawing alone is not acceptance.
- Verify win and loss both gain the existing mastery amount, only after the record arrives.
- Verify level crossing, no crossing, first read with no prior snapshot, unrecorded match,
  recruit, late record after leaving, and an already credited match.
- Check the result at 1920 x 1080 and a 960 x 540 touch viewport. The hero, buttons and
  winning team remain readable, and the animation can be skipped.
- Run screen typecheck, view tests, WebGL build and a real local match. Public ranked,
  wallet identity and multi-human matching require separate end-to-end verification.

## References

- [Apple motion guidance](https://developer.apple.com/design/human-interface-guidelines/motion): motion should convey status, stay brief and be cancellable.
- [Apple accessibility guidance](https://developer.apple.com/design/human-interface-guidelines/accessibility): reduce automatic zoom and travel in a reduced-motion state.
- [Brawl Stars ranked notes](https://supercell.com/en/games/brawlstars/blog/release-notes/patch-notes-ranked-update/): a rank is visibly distinct from character progression and changes from scored competitive games.
- [Brawl Stars February 2026 notes](https://supercell.com/en/games/brawlstars/blog/release-notes/release-notes-february-2026/): bot matches remain available at lower trophy levels while matchmaking uses a hidden MMR decoupled from trophies. This is a precedent for separating participation progress from skill, not evidence that this draft point scale is balanced.
- [Photon Quantum matchmaking](https://doc.photonengine.com/quantum/v3/manual/game-session/async-extensions): existing random rooms and custom lobby properties; these do not themselves establish a trusted skill rating.
- [Photon result verification](https://doc.photonengine.com/quantum/v3/manual/cheat-protection): replay and game-result validation are the basis for durable competitive progress.
