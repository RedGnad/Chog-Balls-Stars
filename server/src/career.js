// The career of each Genesis: matches, wins and the badges they earn. It is keyed by token id,
// so it belongs to the Genesis and follows it when the token changes hands.
//
// Results are reported by the player's own game page. A report is accepted from a signed-in
// address that holds the token or rents it, once per match, and is only "confirmed" when
// another signed-in address of the same match reported the same winner. Nothing here replays a
// match.
//
// The same store remembers when a holder stopped lending a Genesis: passes handed out before
// that moment no longer count (see "lend" in session.js).

export const LAST_TOKEN_ID = 1969;

// At most one credited match per Genesis in this time: a match lasts longer.
const MIN_SECONDS_BETWEEN_MATCHES = 60;
const MATCH_ID = /^[A-Za-z0-9:_-]{8,80}$/;
const KEPT_MATCHES_PER_TOKEN = 200;

export const BADGES = [
  { id: 'first-match', name: 'First match', earned: career => career.matches >= 1 },
  { id: 'first-win', name: 'First win', earned: career => career.wins >= 1 },
  { id: 'three-in-a-row', name: 'Three in a row', earned: career => career.bestStreak >= 3 },
  { id: 'veteran', name: 'Veteran', earned: career => career.matches >= 10 },
];

// Mastery: what a Genesis has learnt by playing. A match gives one point, a win one more. A
// Genesis starts at level 1 and reaches level n + 1 at MASTERY_STEPS[n - 1] points. It is read
// from the career, so it follows the Genesis like the career does.
export const MASTERY_STEPS = [3, 8, 15, 25, 40];

/** Level, points and the points of the next level (0 at the last level) for a career. */
export function masteryOf({ matches, wins }) {
  const points = matches + wins;
  const reached = MASTERY_STEPS.filter(step => points >= step).length;
  return { level: reached + 1, points, next: MASTERY_STEPS[reached] ?? 0 };
}

const isTeam = value => value === 0 || value === 1;

/**
 * @param {object} options
 * @param {() => Promise<object|null>} options.load the saved state, or null when there is none
 * @param {(state: object) => Promise<void>} options.save
 * @param {() => number} [options.now] milliseconds
 */
export function createCareers({ load, save, now = Date.now }) {
  let state = null;
  // One change at a time: each one reads the state the previous one saved.
  let queue = Promise.resolve();

  async function ready() {
    state ??= (await load()) ?? { tokens: {}, matches: {} };
    return state;
  }

  // A match is confirmed for a token when another address reported the same winner and nobody
  // reported another one.
  function isConfirmed(match, tokenId) {
    const mine = match?.reports[tokenId];
    if (!mine) return false;
    const others = Object.values(match.reports).filter(report => report.address !== mine.address);
    return others.length > 0 && others.every(report => report.winnerTeam === mine.winnerTeam);
  }

  function describe(tokenId) {
    const career = state.tokens[tokenId] ?? { matches: 0, wins: 0, streak: 0, bestStreak: 0, lastPlayedAt: 0, matchIds: [] };
    return {
      tokenId,
      matches: career.matches,
      wins: career.wins,
      // How many of those matches a guest played with a pass of the holder.
      guestMatches: career.guestMatches ?? 0,
      // How many a renter played, who paid the holder in $CHOG for it.
      rentedMatches: career.rentedMatches ?? 0,
      bestStreak: career.bestStreak,
      confirmed: career.matchIds.filter(matchId => isConfirmed(state.matches[matchId], tokenId)).length,
      lastPlayedAt: career.lastPlayedAt,
      mastery: masteryOf(career),
      badges: BADGES.filter(badge => badge.earned(career)).map(({ id, name }) => ({ id, name })),
    };
  }

  /** Careers of the given token ids, in that order; a Genesis that never played has zeros. */
  async function get(tokenIds) {
    await ready();
    return tokenIds.map(describe);
  }

  /** The Genesis with the most wins, then the most matches, then the lowest token id. */
  async function top(limit) {
    await ready();
    return Object.keys(state.tokens)
      .map(tokenId => describe(Number(tokenId)))
      .filter(career => career.matches > 0)
      .sort((a, b) => b.wins - a.wins || b.matches - a.matches || a.tokenId - b.tokenId)
      .slice(0, limit);
  }

  /**
   * Credits one match to a Genesis. The caller has already checked that "address" is signed in
   * and holds the token or, for a renter, rents it at this moment, or, for a guest, that
   * "address" is the holder who lent it.
   */
  function record({ tokenId, address, matchId, team, winnerTeam, guest = false, renter = false }) {
    const change = queue.then(async () => {
      await ready();
      if (!Number.isInteger(tokenId) || tokenId < 1 || tokenId > LAST_TOKEN_ID) return { credited: false, reason: 'bad_token' };
      if (typeof matchId !== 'string' || !MATCH_ID.test(matchId)) return { credited: false, reason: 'bad_match' };
      if (!isTeam(team) || !isTeam(winnerTeam)) return { credited: false, reason: 'bad_result' };

      const match = state.matches[matchId] ??= { reports: {} };
      if (match.reports[tokenId]) return { credited: false, reason: 'already_recorded', career: describe(tokenId) };

      const career = state.tokens[tokenId] ??= { matches: 0, wins: 0, streak: 0, bestStreak: 0, lastPlayedAt: 0, matchIds: [] };
      if (now() - career.lastPlayedAt < MIN_SECONDS_BETWEEN_MATCHES * 1000) {
        if (Object.keys(match.reports).length === 0) delete state.matches[matchId];
        return { credited: false, reason: 'too_soon', career: describe(tokenId) };
      }

      const won = team === winnerTeam;
      match.reports[tokenId] = {
        address, team, winnerTeam, at: now(), ...(guest ? { guest: true } : {}), ...(renter ? { renter: true } : {}),
      };
      career.matches += 1;
      career.wins += won ? 1 : 0;
      if (guest) career.guestMatches = (career.guestMatches ?? 0) + 1;
      if (renter) career.rentedMatches = (career.rentedMatches ?? 0) + 1;
      career.streak = won ? career.streak + 1 : 0;
      career.bestStreak = Math.max(career.bestStreak, career.streak);
      career.lastPlayedAt = now();
      career.matchIds = [...career.matchIds, matchId].slice(-KEPT_MATCHES_PER_TOKEN);
      await save(state);
      return { credited: true, won, career: describe(tokenId) };
    });
    queue = change.catch(() => {});
    return change;
  }

  /** Every pass handed out for this Genesis until now stops counting. The caller checked the holder. */
  function stopLending(tokenId) {
    const change = queue.then(async () => {
      await ready();
      const stoppedAt = now();
      (state.lending ??= {})[tokenId] = stoppedAt;
      await save(state);
      return { tokenId, stoppedAt };
    });
    queue = change.catch(() => {});
    return change;
  }

  /** When lending of this Genesis was last stopped, in milliseconds; 0 if it never was. */
  async function lendingStoppedAt(tokenId) {
    await ready();
    return state.lending?.[tokenId] ?? 0;
  }

  return { get, top, record, stopLending, lendingStoppedAt };
}
