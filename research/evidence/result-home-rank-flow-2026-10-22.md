# Player rank result flow, 22 October 2026

## Scope and product decision

The result appears immediately. PLAY AGAIN and HOME remain usable while the referee checks the match. The UI never calls the pending amount a reward. It reveals a point change only when the signed rank response names the exact finished room in `rank.last.gameId`; the result also requires `latestMatch.status == ranked` for that room. Player rank points are distinct from Genesis mastery and are not labelled XP.

The official [Brawl Stars Ranked update](https://supercell.com/en/games/brawlstars/blog/release-notes/patch-notes-ranked-update/) uses post-match rank score gains and losses. That is a reference for making the change legible, not a source for Chog's scoring numbers. The points, tiers and next threshold shown here come from the Chog server response. The owner's constraint is that no player must wait on the result screen for the animation.

## Implemented states

| State | Result | Home |
| --- | --- | --- |
| Verifying | Immediate result and nonblocking status; no invented gain | Continues checking the exact pending room |
| Ranked with nonzero delta | Shows signed delta, counted points and a rank progress bar; brief nonblocking moment | Brief nonblocking reveal if the result was left before its reveal finished |
| Ranked with zero delta | Shows the confirmed total without a gain animation | Clears the pending room without a gain animation |
| No rank event or rank disabled | No points promised | Clears a terminal pending room without a fake reward |
| Service unavailable | Explains that the status is unavailable | Keeps the pending room for a later signed-in visit |

The pending room is stored per wallet address in local player preferences. A wallet change stops the old reveal. The same room is cleared after its gain is shown, avoiding an ordinary duplicate reveal on HOME.

## Verification

- `sh tools/dev/view_tests.sh`: 170 passed, 0 failed.
- `python3 tools/dev/typecheck.py Assembly-CSharp --use Quantum.Simulation Chog.Match --sources Assets/Chog/Screens`: 0 errors.
- `git diff --check`: clean.
- Not yet verified in a running Unity build or with a real signed match. There is no after capture. The open Unity Editor uses the shared main project, not this isolated worktree; a build there would not test this branch. A build and result-to-home capture must happen after integration under `tools/dev/heavy_lock.sh`.

## Integration checks for the lead

1. Signed recruit wins a bot-filled Siege: the result is instant, then a verified `+3` appears on the result or HOME after the exact server event.
2. Leave with HOME and with PLAY AGAIN before the event: no wait and no lost credit. Reload HOME after the event: the pending room is still matched by ID.
3. A casual, refused or service-unavailable match never displays a fabricated point gain. Switching wallet cannot reveal the previous wallet's event.
4. Confirm visual fit at desktop and phone width and capture the actual runtime screens before deploying.

No push, merge or deployment was performed for this lot.
