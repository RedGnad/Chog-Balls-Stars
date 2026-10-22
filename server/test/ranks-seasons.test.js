// Seasons (seasons.js): the summary of a wallet in the configured season, the reward list
// written once at the close for whoever pays it, and what the API says of the season.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createMatches, createMemoryStore } from '../src/matches.js';
import { createRanks } from '../src/ranks.js';
import { createSeasons, seasonFrom } from '../src/seasons.js';
import { createApi } from '../src/api.js';
import { createSessions } from '../src/session.js';
import { createWallet } from './wallet.js';

const address = number => '0x' + number.toString(16).padStart(40, '0');
const players = Array.from({ length: 6 }, (_, index) => ({ player: index + 1, team: index < 3 ? 0 : 1, tokenId: index + 1 }));
const admission = player => ({ player: player.player, team: player.team, tokenId: player.tokenId, address: player.address ?? address(player.player), right: 'owner' });
const app = { appVersion: 'sim-example-siege' };
const verdict = { winnerTeam: 0, mode: 'Siege', testOptions: false, players };
const seventh = { ...players[0], address: address(7) };

function ladder() {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  let clock = Date.parse('2026-10-21T00:00:00Z');
  const ranks = createRanks({ store, matches, now: () => clock });
  let counter = 0;
  async function play(lineup, { winnerTeam = 0 } = {}) {
    const id = `game-${++counter}`;
    clock += 1000;
    await matches.entered(id, { how: 'create', userId: address(1), app });
    for (const player of lineup) await matches.admitted(id, admission(player));
    return ranks.record(id, { ...verdict, winnerTeam, players: lineup });
  }
  return { store, matches, ranks, play, now: () => clock, advanceTo: iso => { clock = Date.parse(iso); } };
}

// Before the season: thirty six-human wins (the winners at 510, Star) and a solo wallet; in
// the season: a loss, a win, a loss for the winners (489, Gold, having been Star).
async function seasonPlayed() {
  const field = ladder();
  for (let number = 1; number <= 30; number++) await field.play(players);
  for (let number = 1; number <= 3; number++) await field.play([seventh]);
  field.advanceTo('2026-10-22T00:00:00Z');
  await field.play(players, { winnerTeam: 1 });
  await field.play(players);
  await field.play(players, { winnerTeam: 1 });
  const season = seasonFrom({ id: 'oct', startsAt: '2026-10-22T00:00:00Z', endsAt: '2026-10-31T00:00:00Z' });
  return { ...field, season };
}

test('the season summary counts the window only, keeps the best tier and the points before the reset', async () => {
  const { ranks, season, advanceTo, now, store } = await seasonPlayed();
  const window = { startsAt: season.startsAt, endsAt: season.endsAt };
  assert.deepEqual(await ranks.summaryOf(address(1), window), { points: 489, tier: 'GOLD', bestTier: 'STAR', matches: 3, wins: 1 });
  assert.deepEqual(await ranks.summaryOf(address(4), window), { points: 28, tier: 'BRONZE', bestTier: 'BRONZE', matches: 3, wins: 2 });
  assert.deepEqual(await ranks.summaryOf(address(7), window), { points: 9, tier: 'BRONZE', bestTier: 'BRONZE', matches: 0, wins: 0 }, 'played before the season only');
  assert.deepEqual(await ranks.summaryOf(address(8), window), { points: 0, tier: 'BRONZE', bestTier: 'BRONZE', matches: 0, wins: 0 });
  assert.deepEqual(await ranks.summaryOf(address(1), { endsAt: season.endsAt }), { points: 489, tier: 'GOLD', bestTier: 'STAR', matches: 33, wins: 31 }, 'without a start the window begins at the beginning');
  const seasons = createSeasons({ ranks, store, season, now });
  assert.deepEqual(await seasons.summaryOf(address(1)), { points: 489, tier: 'GOLD', bestTier: 'STAR', matches: 3, wins: 1 });
  advanceTo('2026-10-31T00:00:00Z');
  await seasons.closeIfEnded();
  assert.equal((await ranks.of(address(1))).points, 370, 'the reset took it toward Gold');
  assert.deepEqual(await ranks.summaryOf(address(1), window), { points: 489, tier: 'GOLD', bestTier: 'STAR', matches: 3, wins: 1 }, 'the summary is the season as played, before the reset');
  assert.equal((await createSeasons({ ranks, store, season: null, now }).summaryOf(address(1))), null);
});

test('closing a season writes the reward list once, for the payer, and hands it to the hook', async () => {
  const { ranks, store, season, advanceTo, now } = await seasonPlayed();
  const closings = [];
  const seasons = createSeasons({ ranks, store, season, now, onClosed: closing => { closings.push(closing); } });
  assert.equal(await seasons.rewardsOf('oct'), null, 'nothing before the close');
  advanceTo('2026-10-31T00:00:00Z');
  const closing = await seasons.closeIfEnded();
  assert.equal(closing.seasonId, 'oct');
  assert.equal(closing.resets.length, 3);
  const expected = [
    { address: address(1), tier: 'GOLD', points: 489, bestTier: 'STAR', matches: 3, wins: 1 },
    { address: address(2), tier: 'GOLD', points: 489, bestTier: 'STAR', matches: 3, wins: 1 },
    { address: address(3), tier: 'GOLD', points: 489, bestTier: 'STAR', matches: 3, wins: 1 },
    { address: address(4), tier: 'BRONZE', points: 28, bestTier: 'BRONZE', matches: 3, wins: 2 },
    { address: address(5), tier: 'BRONZE', points: 28, bestTier: 'BRONZE', matches: 3, wins: 2 },
    { address: address(6), tier: 'BRONZE', points: 28, bestTier: 'BRONZE', matches: 3, wins: 2 },
  ];
  assert.deepEqual(closing.rewards, expected, 'the solo wallet played before the season only: not listed');
  assert.deepEqual(closings, [closing]);
  const rewards = await seasons.rewardsOf('oct');
  assert.deepEqual(rewards, { seasonId: 'oct', startsAt: '2026-10-22T00:00:00.000Z', endsAt: '2026-10-31T00:00:00.000Z', wallets: expected });
  assert.equal(await seasons.rewardsOf('nov'), null);
  assert.equal(await seasons.rewardsOf('not a season'), null);
  assert.equal(await seasons.closeIfEnded(), null);
  assert.equal(closings.length, 1);
  assert.deepEqual((await store.list('seasons/')).filter(key => !key.includes('_reset_')), ['seasons/oct_closed', 'seasons/oct_rewards']);
  // A match played after the close belongs to the next season and does not change the list.
  const marker = JSON.parse(await store.get('seasons/oct_closed'));
  assert.equal(marker.rewarded, 6);
  assert.equal(marker.resets, 3);
});

test('the API tells the season, its summary for the signed wallet, and the reward list once closed', async () => {
  const { matches, ranks, store, season, play, advanceTo, now } = await seasonPlayed();
  const seasons = createSeasons({ ranks, store, season, now });
  const sessions = createSessions({ secret: 'a test secret that is long enough to be accepted' });
  const wallet = createWallet();
  const api = createApi({ sessions, matches, ranks, seasons, publicOrigin: 'https://game.example' });
  const get = path => api(new Request(`https://game.example${path}`));
  assert.deepEqual(await (await get('/api/season')).json(), { enabled: true, id: 'oct', startsAt: '2026-10-22T00:00:00.000Z', endsAt: '2026-10-31T00:00:00.000Z', ended: false, closed: false });
  assert.equal((await get('/api/season/rewards?season=oct')).status, 404);
  assert.equal((await get('/api/season/rewards')).status, 404);
  const challenge = await api(new Request('https://game.example/api/session/challenge', { method: 'POST', body: JSON.stringify({ address: wallet.address }) })).then(response => response.json());
  const signed = await api(new Request('https://game.example/api/session', { method: 'POST', body: JSON.stringify({ ...challenge, signature: wallet.personalSign(challenge.message) }) })).then(response => response.json());
  const me = () => api(new Request('https://game.example/api/rank/me', { headers: { Authorization: `Bearer ${signed.session}` } })).then(response => response.json());
  let answer = await me();
  assert.deepEqual(answer.season, { id: 'oct', startsAt: '2026-10-22T00:00:00.000Z', endsAt: '2026-10-31T00:00:00.000Z', ended: false, closed: false, summary: { points: 0, tier: 'BRONZE', bestTier: 'BRONZE', matches: 0, wins: 0 } });
  await play([{ ...players[0], address: wallet.address }, players[3]]);
  answer = await me();
  assert.deepEqual(answer.season.summary, { points: 6, tier: 'BRONZE', bestTier: 'BRONZE', matches: 1, wins: 1 });
  assert.equal(answer.rank.points, 6);
  advanceTo('2026-10-31T00:00:00Z');
  await seasons.closeIfEnded();
  assert.deepEqual(await (await get('/api/season')).json(), { enabled: true, id: 'oct', startsAt: '2026-10-22T00:00:00.000Z', endsAt: '2026-10-31T00:00:00.000Z', ended: true, closed: true });
  const rewards = await (await get('/api/season/rewards?season=oct')).json();
  assert.equal(rewards.wallets.length, 7);
  assert.deepEqual(rewards.wallets.find(entry => entry.address === wallet.address.toLowerCase()), { address: wallet.address.toLowerCase(), tier: 'BRONZE', points: 6, bestTier: 'BRONZE', matches: 1, wins: 1 });
  answer = await me();
  assert.equal(answer.season.closed, true);
  assert.equal(answer.rank.lastReset, null, 'six points: nothing to reset');
  // Without a season, or without the ladder, the answers say so.
  const bare = createApi({ sessions, matches, ranks, publicOrigin: 'https://game.example' });
  assert.deepEqual(await (await bare(new Request('https://game.example/api/season'))).json(), { enabled: false });
  assert.equal((await bare(new Request('https://game.example/api/season/rewards?season=oct'))).status, 404);
  const off = createApi({ sessions, matches, seasons, publicOrigin: 'https://game.example' });
  assert.deepEqual(await (await off(new Request('https://game.example/api/season'))).json(), { enabled: false });
});
