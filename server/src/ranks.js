// Player ladder points come only from a complete, server-replayed Siege match. Each match
// has one immutable event, so duplicate webhooks and restarts cannot credit it twice.
//
// The rules (research/evidence/ladder-references-2026-10-21.md, section 5):
//   - bots are for starting: a match with no other authenticated human raises a wallet up to
//     Silver and no further, and changes nothing once the wallet is at or above Silver;
//   - the top is not generous: from Gold a win pays 75% and a loss costs 125% of the base
//     (Clash Royale, Master I: losses equal gains); from Star 50% and 150% (Brawl Stars above
//     1100: losing more than winning);
//   - a season's end brings every wallet above Gold down toward it (seasons.js decides when;
//     the reset is one immutable event per wallet here).
// Every event freezes the numbers it was scored with, so a later change alters no past result.
import { folderOf } from './matches.js';
import { isAddress } from './chain.js';
import { RATING, UNRATED_BAND, bandFor, computeRatings } from './rating.js';

export const TIERS = [
  { name: 'BRONZE', at: 0 },
  { name: 'SILVER', at: 100 },
  { name: 'GOLD', at: 250 },
  { name: 'STAR', at: 500 },
];
const tiers = TIERS;

/** A solo match (one authenticated human) cannot raise a wallet past this many points. */
export const SOLO_CAP = 100;

/** From these points a win pays and a loss costs these percentages of the base amounts. */
export const BANDS = [
  { from: 250, win: 75, loss: 125 },
  { from: 500, win: 50, loss: 150 },
];

/** At a season's end, a wallet above the floor keeps this share of what is above it. */
export const SEASON_RESET = { floor: 250, keep: 50 };

export function tierFor(points) {
  return [...tiers].reverse().find(tier => points >= tier.at).name;
}

// Only distinct authenticated humans increase the award. Guest tabs cannot prove
// separate identities. The points are snapshotted so later changes keep history stable.
export function pointsFor(authenticatedHumans) {
  if (!Number.isInteger(authenticatedHumans) || authenticatedHumans < 1 || authenticatedHumans > 6) {
    throw new RangeError('authenticated human count must be 1 to 6');
  }
  return { win: Math.round(3 + (authenticatedHumans - 1) * 17 / 5), loss: -2 * authenticatedHumans };
}

/** Everything an event freezes to score its players: the base amounts, the solo cap, the bands. */
export function scoringFor(authenticatedHumans) {
  return {
    ...pointsFor(authenticatedHumans),
    soloCap: authenticatedHumans === 1 ? SOLO_CAP : null,
    bands: BANDS.map(band => ({ ...band })),
  };
}

/**
 * The change of a wallet's points for one event: from the scoring that event froze, the points
 * the wallet had before it, and the outcome ('win', 'loss' or 'draw'). An event recorded before
 * the solo cap and the bands existed has neither, and scores as it did then.
 */
export function deltaFor(scoring, before, outcome) {
  if (outcome === 'draw') return 0;
  const won = outcome === 'win';
  const base = Math.abs(won ? (scoring?.win ?? 25) : (scoring?.loss ?? -15));
  const soloCap = scoring?.soloCap ?? null;
  if (soloCap !== null && before >= soloCap) return 0;
  const band = [...(scoring?.bands ?? [])].reverse().find(candidate => before >= candidate.from);
  const amount = band ? Math.max(1, Math.round(base * (won ? band.win : band.loss) / 100)) : base;
  if (!won) return -Math.min(amount, before) || 0;
  return soloCap !== null ? Math.min(amount, soloCap - before) : amount;
}

/** Where a season's end brings a wallet: unchanged at or below the floor, else toward it. */
export function resetFor(points, { floor, keep } = SEASON_RESET) {
  return points > floor ? floor + Math.round((points - floor) * keep / 100) : points;
}

export function rankEligibility(match, verdict) {
  if (!match || !verdict || verdict.void || ![0, 1, 2].includes(verdict.winnerTeam)) return 'unverified';
  if (verdict.mode !== 'Siege') return 'siege_only';
  if (verdict.testOptions !== false || match.app?.appVersion?.includes('-preview')) return 'preview';
  const players = verdict.players;
  if (!Array.isArray(players) || players.length < 1 || players.length > 6) return 'invalid_human_count';
  if (players.some(player => !Number.isInteger(player.player) || ![0, 1].includes(player.team))
    || new Set(players.map(player => player.player)).size !== players.length
    || players.filter(player => player.team === 0).length > 3
    || players.filter(player => player.team === 1).length > 3) return 'invalid_human_teams';
  const addresses = new Set();
  for (const player of players) {
    const admissions = (match.admissions ?? []).filter(entry => entry.player === player.player);
    const admitted = admissions.at(-1);
    if (!admitted || admitted.tokenId !== player.tokenId || admitted.standIn) return 'invalid_admission';
    if (!isAddress(admitted.address) || admitted.right === 'guest') continue;
    const address = admitted.address.toLowerCase();
    if (addresses.has(address)) return 'duplicate_wallet';
    addresses.add(address);
  }
  return addresses.size > 0 ? null : 'no_authenticated_players';
}

// Season keys are flat ("seasons/<id>_reset_<wallet>"): the match store keeps one folder and one
// name, so a season id has no underscore.
export const SEASON_ID = /^[A-Za-z0-9-]{1,40}$/;

export function createRanks({ store, matches, now = Date.now }) {
  const keyOf = gameId => `ranked/${folderOf(gameId)}`;
  const resetKeyOf = (seasonId, address) => `seasons/${seasonId}_reset_${address}`;
  // Events are immutable: once read they are kept, and only the key listing is asked again.
  const cache = new Map();

  async function cached(key) {
    if (cache.has(key)) return cache.get(key);
    const text = await store.get(key);
    if (text === null) return null;
    const value = JSON.parse(text);
    cache.set(key, value);
    return value;
  }

  async function writeOnce(key, event) {
    if (store.setIfAbsent) {
      if (await store.setIfAbsent(key, JSON.stringify(event))) return event;
      return JSON.parse(await store.get(key));
    }
    const existing = await store.get(key);
    if (existing !== null) return JSON.parse(existing);
    await store.set(key, JSON.stringify(event));
    return event;
  }

  async function record(gameId, verdict) {
    const key = keyOf(gameId);
    const existing = await store.get(key);
    if (existing !== null) return JSON.parse(existing);
    const match = await matches.get(gameId);
    const reason = rankEligibility(match, verdict);
    const players = reason ? [] : verdict.players.flatMap(player => {
      const admitted = match.admissions.filter(entry => entry.player === player.player).at(-1);
      return isAddress(admitted.address) && admitted.right !== 'guest'
        ? [{ address: admitted.address.toLowerCase(), team: player.team }] : [];
    });
    const humanCount = reason ? null : verdict.players.length;
    const authenticatedHumans = reason ? null : players.length;
    const scoring = reason ? null : scoringFor(authenticatedHumans);
    const event = { gameId, at: new Date(now()).toISOString(), reason, winnerTeam: verdict?.winnerTeam ?? null,
      humanCount, authenticatedHumans, scoring, players };
    return writeOnce(key, event);
  }

  async function eventOf(gameId) {
    const text = await store.get(keyOf(gameId));
    return text === null ? null : JSON.parse(text);
  }

  /** Every match event, oldest first. */
  async function allEvents() {
    const events = [];
    for (const key of await store.list('ranked/')) events.push(await cached(key));
    events.sort((a, b) => a.at.localeCompare(b.at) || a.gameId.localeCompare(b.gameId));
    return events;
  }

  /** Every season reset event, oldest first; of one wallet when given. */
  async function allResets(address = null) {
    const resets = [];
    for (const key of await store.list('seasons/')) {
      const parts = key.slice('seasons/'.length).split('_');
      if (parts.length !== 3 || parts[1] !== 'reset' || (address && parts[2] !== address)) continue;
      resets.push(await cached(key));
    }
    resets.sort((a, b) => a.at.localeCompare(b.at) || a.seasonId.localeCompare(b.seasonId));
    return resets;
  }

  /** Every wallet that has a match event. */
  async function wallets() {
    const seen = new Set();
    for (const event of await allEvents()) for (const player of event.players) seen.add(player.address);
    return [...seen].sort();
  }

  /**
   * What happened to a wallet, in order: its match events and its season resets, each with
   * the points before and after it. "until" (ISO time) stops the timeline there, inclusive.
   */
  async function timelineOf(address, { until = null } = {}) {
    const wallet = address.toLowerCase();
    const steps = [];
    for (const event of await allEvents()) {
      const player = event.players.find(entry => entry.address === wallet);
      if (player && (until === null || event.at <= until)) steps.push({ kind: 'match', at: event.at, event, team: player.team });
    }
    for (const reset of await allResets(wallet)) {
      if (until === null || reset.at <= until) steps.push({ kind: 'reset', at: reset.at, event: reset });
    }
    // At one instant a match comes before a reset: the reset closes everything up to its time.
    steps.sort((a, b) => a.at.localeCompare(b.at) || (a.kind === b.kind ? 0 : a.kind === 'match' ? -1 : 1)
      || (a.event.gameId ?? '').localeCompare(b.event.gameId ?? ''));
    let points = 0;
    for (const step of steps) {
      step.before = points;
      if (step.kind === 'match') {
        const outcome = step.event.winnerTeam === 2 ? 'draw' : step.event.winnerTeam === step.team ? 'win' : 'loss';
        step.outcome = outcome;
        points = Math.max(0, points + deltaFor(step.event.scoring, points, outcome));
      } else {
        points = step.event.after;
      }
      step.after = points;
    }
    return steps;
  }

  async function standingOf(address, options = {}) {
    const steps = await timelineOf(address, options);
    const points = steps.length > 0 ? steps.at(-1).after : 0;
    let matchesPlayed = 0;
    let wins = 0;
    let last = null;
    let lastReset = null;
    for (const step of steps) {
      if (step.kind === 'reset') {
        lastReset = { seasonId: step.event.seasonId, at: step.at, before: step.before, after: step.after,
          tierBefore: tierFor(step.before), tierAfter: tierFor(step.after) };
        continue;
      }
      matchesPlayed++;
      if (step.outcome === 'win') wins++;
      last = { gameId: step.event.gameId, delta: step.after - step.before, before: step.before, after: step.after,
        tierBefore: tierFor(step.before), tierAfter: tierFor(step.after) };
    }
    const next = tiers.find(tier => tier.at > points);
    return { points, tier: tierFor(points), next: next?.at ?? null, matches: matchesPlayed, wins, last, lastReset };
  }

  const of = address => standingOf(address);

  /**
   * The end of a season: every wallet above the floor at "at" (ISO time) gets one immutable
   * reset event that brings it toward the floor. Asking again writes nothing and answers the
   * same events; a wallet at or below the floor gets none.
   */
  async function resetSeason({ seasonId, at, floor = SEASON_RESET.floor, keep = SEASON_RESET.keep }) {
    if (!SEASON_ID.test(seasonId ?? '')) throw new RangeError('a season id is 1 to 40 letters, digits or -');
    if (!Number.isFinite(Date.parse(at ?? ''))) throw new RangeError('a season ends at an ISO time');
    const resets = [];
    for (const wallet of await wallets()) {
      const key = resetKeyOf(seasonId, wallet);
      const existing = await cached(key);
      if (existing) { resets.push(existing); continue; }
      const { points } = await standingOf(wallet, { until: at });
      if (points <= floor) continue;
      const reset = { kind: 'reset', seasonId, address: wallet, at, before: points, after: resetFor(points, { floor, keep }), floor, keep };
      resets.push(await writeOnce(key, reset));
    }
    return resets;
  }

  /**
   * What a wallet did between "startsAt" (inclusive; null for the beginning) and "endsAt"
   * (inclusive): matches and wins in that window, the best tier it held in it (its tier at the
   * start counts), and the points and tier it ended on, before any reset at the end.
   */
  async function summaryOf(address, { startsAt = null, endsAt }) {
    const steps = await timelineOf(address, { until: endsAt });
    let points = 0;
    let matchesPlayed = 0;
    let wins = 0;
    let best = 0;
    for (const step of steps) {
      if (startsAt !== null && step.at < startsAt) { points = step.after; continue; }
      if (step.kind === 'reset') { if (step.at < endsAt) points = step.after; continue; }
      points = step.after;
      matchesPlayed++;
      if (step.outcome === 'win') wins++;
      best = Math.max(best, points);
    }
    const started = startsAt === null ? 0 : (steps.findLast(step => step.at < startsAt)?.after ?? 0);
    best = Math.max(best, started);
    return { points, tier: tierFor(points), bestTier: tierFor(best), matches: matchesPlayed, wins };
  }

  /**
   * The hidden rating of a wallet (rating.js), computed from every match event where
   * authenticated humans faced each other; a wallet never rated starts at 1500 with 0 matches.
   * Never answered to a page as a number: the API gives bandOf.
   */
  async function ratingOf(address) {
    const rated = computeRatings(await allEvents()).get(address.toLowerCase());
    const rating = rated?.rating ?? RATING.start;
    return { rating, matches: rated?.matches ?? 0, band: bandFor(rating) };
  }

  /** The matchmaking band of a wallet: what a room it creates carries, and what it filters on. */
  async function bandOf(address) {
    return isAddress(address) ? (await ratingOf(address)).band : UNRATED_BAND;
  }

  return { record, eventOf, of, standingOf, timelineOf, summaryOf, allEvents, allResets, wallets, resetSeason, ratingOf, bandOf };
}
