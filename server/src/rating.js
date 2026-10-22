// A hidden skill rating per wallet, apart from the ladder points: Elo, computed by the server
// from the same immutable match events, and only from matches where authenticated humans
// faced each other. Bots and guests are not rated and move nobody. The rating is never shown
// as a number: the API gives a band, which is what matchmaking can filter on.
//
// Formulas and constants (research/evidence/ladder-references-2026-10-21.md, section 3):
//   expected score  E = 1 / (1 + 10 ^ ((R_other - R) / 400))
//   update          R' = R + K (S - E), S = 1 win, 0.5 draw, 0 loss
//   K = 40 for a wallet's first 30 rated matches, 20 after (FIDE's schedule); start 1500.
// A team's rating is the mean of its authenticated humans' ratings.

export const RATING = Object.freeze({ start: 1500, provisionalMatches: 30, kProvisional: 40, kSettled: 20 });

/** Bands of 150 points centred on 1500, numbered 0 to 6: band 3 holds 1425 to 1574. */
export const BAND = Object.freeze({ width: 150, centre: 1500, count: 7 });

export function expectedScore(rating, opponent) {
  return 1 / (1 + 10 ** ((opponent - rating) / 400));
}

export function kFor(ratedMatches) {
  return ratedMatches < RATING.provisionalMatches ? RATING.kProvisional : RATING.kSettled;
}

export function bandFor(rating) {
  const middle = Math.floor(BAND.count / 2);
  const band = Math.floor((rating - BAND.centre + BAND.width / 2) / BAND.width) + middle;
  return Math.min(BAND.count - 1, Math.max(0, band));
}

/** The band of a wallet that was never rated: the middle one. */
export const UNRATED_BAND = bandFor(RATING.start);

/**
 * Why a match event cannot rate anyone, or null when it can: it must have scored its players
 * (no reason), have a winner or a draw, and an authenticated human on each team.
 */
export function ratingEligibility(event) {
  if (!event || event.reason) return event?.reason ?? 'unverified';
  if (![0, 1, 2].includes(event.winnerTeam)) return 'unverified';
  const teams = new Set((event.players ?? []).map(player => player.team));
  return teams.has(0) && teams.has(1) ? null : 'one_side_only';
}

const mean = values => values.reduce((sum, value) => sum + value, 0) / values.length;

/**
 * Applies one event to "ratings" (address to {rating, matches}), in place. Answers the changes,
 * one per rated wallet, or an empty list with the reason when the event rates nobody.
 */
export function applyEvent(ratings, event) {
  const reason = ratingEligibility(event);
  if (reason) return { reason, changes: [] };
  const current = address => ratings.get(address) ?? { rating: RATING.start, matches: 0 };
  const teamRating = team => mean(event.players.filter(player => player.team === team).map(player => current(player.address).rating));
  const teamRatings = [teamRating(0), teamRating(1)];
  const changes = [];
  for (const player of event.players) {
    const before = current(player.address);
    const own = teamRatings[player.team];
    const other = teamRatings[1 - player.team];
    const score = event.winnerTeam === 2 ? 0.5 : event.winnerTeam === player.team ? 1 : 0;
    const k = kFor(before.matches);
    const expected = expectedScore(own, other);
    const rating = before.rating + k * (score - expected);
    changes.push({ address: player.address, before: before.rating, after: rating, k, expected, score });
  }
  // Every change is computed from the ratings before the match, then written at once.
  for (const change of changes) {
    const before = current(change.address);
    ratings.set(change.address, { rating: change.after, matches: before.matches + 1 });
  }
  return { reason: null, changes };
}

/** The ratings of every wallet after "events" (oldest first): address to {rating, matches}. */
export function computeRatings(events) {
  const ratings = new Map();
  for (const event of events) applyEvent(ratings, event);
  return ratings;
}
