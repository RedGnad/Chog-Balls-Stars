// The verdict of a match. Photon's Public Cloud does not run the simulation, so nothing it sends
// says who won; what it does send is the input of every player at every tick, as its server
// recorded it. The match is played again from that record by a program that holds the same
// simulation as the game (tools/quantum/Verifier.cs), and what comes out is the verdict. A
// player's page is never asked.
//
// "replay" is that program, given by whoever starts the services: it takes the folder of the
// match (matches.js) and answers the report of the replayed match:
//
//   { winnerTeam: 0 | 1 | null, mode, scores: [n, n], coreLostBy: 0 | 1 | null, endedAtTick,
//     selectionEndedAtTick, firstTick, lastTick, updateRate, players: [{player, team, tokenId}],
//     simulation }
//
// A report becomes a verdict here. The match is void, which gives every stake back, when it
// cannot be judged: the record is not whole, the match was played with another build of the
// game than the one the program belongs to, the match did not reach its end, or a player did
// not play the Genesis the webhooks admitted it with. When the program itself fails there is
// no verdict at all, and judging can be asked again.

import { DRAW } from './referee.js';
import { buildOf } from './modes.js';

/**
 * @param {object} options
 * @param {ReturnType<import('./matches.js').createMatches>} options.matches
 * @param {(folder: string, gameId: string) => Promise<object>} options.replay
 * @param {string | (() => Promise<string>)} [options.build] the build of the game the replay
 *   program belongs to, as that build names itself to Photon, or a function that reads it; a
 *   match played with another one is void, since replayed with another simulation it would not
 *   be the same match. Empty: nothing is compared
 */
export function createJudge({ matches, replay, build = '' }) {
  /** Judges a match once its input history is whole; answers the verdict that is kept. */
  async function judge(gameId) {
    const given = await matches.verdictOf(gameId);
    if (given) return given;
    const record = await matches.replayOf(gameId);
    if (!record.complete) {
      // A record that has not ended may still be arriving: only one that ended with a hole is judged void.
      if (!record.ended) return null;
      return matches.setVerdict(gameId, { void: true, why: 'the record of the match is not whole', missing: record.missing });
    }

    const match = await matches.get(gameId);
    const playedWith = match?.app?.appVersion ?? null;
    const judged = typeof build === 'function' ? await build() : build;
    if (judged && buildOf(playedWith) !== judged) {
      return matches.setVerdict(gameId, { void: true, why: 'the match was played with another build than the one judged here', playedWith, build: judged });
    }

    const report = await replay(matches.folderOf(gameId), gameId);
    const facts = {
      mode: report.mode ?? null, scores: report.scores, endedAtTick: report.endedAtTick, selectionEndedAtTick: report.selectionEndedAtTick,
      firstTick: report.firstTick, lastTick: report.lastTick, updateRate: report.updateRate,
      players: report.players, simulation: report.simulation, testOptions: report.testOptions,
    };
    if (report.winnerTeam !== 0 && report.winnerTeam !== 1) {
      return matches.setVerdict(gameId, { void: true, why: 'the match did not reach its end', ...facts });
    }
    if (!Number.isInteger(report.selectionEndedAtTick) || !Number.isInteger(report.firstTick) || !(report.updateRate > 0)) {
      return matches.setVerdict(gameId, { void: true, why: 'the replayed match does not say when it began', ...facts });
    }

    for (const admission of match.admissions) {
      if (!Number.isInteger(admission.player)) continue;
      const played = report.players.find(player => player.player === admission.player);
      // A player who left before the end is no longer in the last frame: nothing to compare.
      if (played && played.tokenId !== admission.tokenId) {
        return matches.setVerdict(gameId, { void: true, why: 'a player did not play the Genesis it was admitted with', ...facts });
      }
    }

    // A core that was destroyed ends the match for the other team, whatever the scores: the
    // replay names that winner and it stands.
    if (report.coreLostBy === 0 || report.coreLostBy === 1) {
      return matches.setVerdict(gameId, { winnerTeam: report.winnerTeam, coreLostBy: report.coreLostBy, ...facts });
    }

    // The game's own rule gives an even score to the second team; with stakes an even score is a draw.
    // The scores are the coins picked up, or in a siege the damage done to the structures.
    const even = Array.isArray(report.scores) && report.scores[0] === report.scores[1];
    return matches.setVerdict(gameId, { winnerTeam: even ? DRAW : report.winnerTeam, ...facts });
  }

  return { judge };
}
