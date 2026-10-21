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

// Only distinct authenticated humans increase the award. Guest tabs cannot prove
// separate identities. The points are snapshotted so later changes keep history stable.
export function pointsFor(authenticatedHumans) {
  if (!Number.isInteger(authenticatedHumans) || authenticatedHumans < 1 || authenticatedHumans > 6) {
    throw new RangeError('authenticated human count must be 1 to 6');
  }
  return { win: Math.round(3 + (authenticatedHumans - 1) * 17 / 5), loss: -2 * authenticatedHumans };
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

export function createRanks({ store, matches, now = Date.now }) {
  const keyOf = gameId => `ranked/${folderOf(gameId)}`;

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
    const scoring = reason ? null : pointsFor(authenticatedHumans);
    const event = { gameId, at: new Date(now()).toISOString(), reason, winnerTeam: verdict?.winnerTeam ?? null,
      humanCount, authenticatedHumans, scoring, players };
    if (store.setIfAbsent) {
      return await store.setIfAbsent(key, JSON.stringify(event)) ? event : JSON.parse(await store.get(key));
    }
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
      const delta = event.winnerTeam === 2 ? 0 : won ? (event.scoring?.win ?? 25) : (event.scoring?.loss ?? -15);
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
