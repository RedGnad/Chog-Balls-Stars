// Player ladder points come only from a complete, server-replayed Siege match. Each match
// has one immutable event, so duplicate webhooks and restarts cannot credit it twice.
import { folderOf } from './matches.js';
import { isAddress } from './chain.js';

const tiers = [
  { name: 'BRONZE', at: 0 },
  { name: 'SILVER', at: 100 },
  { name: 'GOLD', at: 250 },
  { name: 'STAR', at: 500 },
];

export function tierFor(points) {
  return [...tiers].reverse().find(tier => points >= tier.at).name;
}

export function rankEligibility(match, verdict) {
  if (!match || !verdict || verdict.void || ![0, 1, 2].includes(verdict.winnerTeam)) return 'unverified';
  if (verdict.mode !== 'Siege') return 'siege_only';
  if (verdict.testOptions !== false || match.app?.appVersion?.includes('-preview')) return 'preview';
  const players = verdict.players;
  if (!Array.isArray(players) || players.length !== 6) return 'six_humans_required';
  if (players.filter(player => player.team === 0).length !== 3 || players.filter(player => player.team === 1).length !== 3) return 'three_per_team_required';
  const addresses = new Set();
  for (const player of players) {
    const admissions = (match.admissions ?? []).filter(entry => entry.player === player.player);
    const admitted = admissions.at(-1);
    if (!admitted || !isAddress(admitted.address) || admitted.right === 'guest' || admitted.standIn
      || admitted.tokenId !== player.tokenId || addresses.has(admitted.address.toLowerCase())) return 'authenticated_players_required';
    addresses.add(admitted.address.toLowerCase());
  }
  return null;
}

export function createRanks({ store, matches, now = Date.now }) {
  const keyOf = gameId => `ranked/${folderOf(gameId)}`;

  async function record(gameId, verdict) {
    const key = keyOf(gameId);
    const existing = await store.get(key);
    if (existing !== null) return JSON.parse(existing);
    const match = await matches.get(gameId);
    const reason = rankEligibility(match, verdict);
    const players = reason ? [] : verdict.players.map(player => ({
      address: match.admissions.filter(entry => entry.player === player.player).at(-1).address.toLowerCase(),
      team: player.team,
    }));
    const event = { gameId, at: new Date(now()).toISOString(), reason, winnerTeam: verdict?.winnerTeam ?? null, players };
    await store.set(key, JSON.stringify(event));
    return event;
  }

  async function eventOf(gameId) {
    const text = await store.get(keyOf(gameId));
    return text === null ? null : JSON.parse(text);
  }

  async function of(address) {
    let points = 0;
    let matchesPlayed = 0;
    let wins = 0;
    const events = [];
    for (const key of await store.list('ranked/')) {
      const event = JSON.parse(await store.get(key));
      if (event.players.some(player => player.address === address.toLowerCase())) events.push(event);
    }
    events.sort((a, b) => a.at.localeCompare(b.at) || a.gameId.localeCompare(b.gameId));
    let last = null;
    for (const event of events) {
      const player = event.players.find(entry => entry.address === address.toLowerCase());
      const before = points;
      const won = event.winnerTeam === player.team;
      const delta = event.winnerTeam === 2 ? 0 : won ? 25 : -15;
      points = Math.max(0, points + delta);
      matchesPlayed++;
      if (won) wins++;
      last = { gameId: event.gameId, delta: points - before, before, after: points, tierBefore: tierFor(before), tierAfter: tierFor(points) };
    }
    const next = tiers.find(tier => tier.at > points);
    return { points, tier: tierFor(points), next: next?.at ?? null, matches: matchesPlayed, wins, last };
  }

  return { record, eventOf, of };
}
