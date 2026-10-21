import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createMatches, createMemoryStore } from '../src/matches.js';
import { createRanks, rankEligibility, tierFor } from '../src/ranks.js';
import { createApi } from '../src/api.js';
import { createSessions } from '../src/session.js';
import { createWallet } from './wallet.js';

const address = number => '0x' + number.toString(16).padStart(40, '0');
const players = Array.from({ length: 6 }, (_, index) => ({ player: index + 1, team: index < 3 ? 0 : 1, tokenId: index + 1 }));
const admission = player => ({ player: player.player, team: player.team, tokenId: player.tokenId, address: address(player.player), right: 'owner' });
const match = { app: { appVersion: 'sim-example-siege' }, admissions: players.map(admission) };
const verdict = { winnerTeam: 0, mode: 'Siege', testOptions: false, players };

test('ranking requires six different signed-in humans in a verified Siege', () => {
  assert.equal(rankEligibility(match, verdict), null);
  assert.equal(rankEligibility(match, { ...verdict, void: true, winnerTeam: null }), 'unverified');
  assert.equal(rankEligibility(match, { ...verdict, players: players.slice(0, 5) }), 'six_humans_required');
  assert.equal(rankEligibility(match, { ...verdict, mode: 'Coins' }), 'siege_only');
  assert.equal(rankEligibility(match, { ...verdict, testOptions: true }), 'preview');
  assert.equal(rankEligibility(match, { ...verdict, testOptions: undefined }), 'preview');
  assert.equal(rankEligibility({ ...match, app: { appVersion: 'sim-example-preview-siege' } }, verdict), 'preview');
  assert.equal(rankEligibility({ ...match, admissions: match.admissions.slice(1) }, verdict), 'authenticated_players_required');
  assert.equal(rankEligibility({ ...match, admissions: match.admissions.map((entry, index) => index === 5 ? { ...entry, address: address(1) } : entry) }, verdict), 'authenticated_players_required');
  assert.equal(rankEligibility({ ...match, admissions: match.admissions.map((entry, index) => index === 5 ? { ...entry, right: 'guest' } : entry) }, verdict), 'authenticated_players_required');
});

test('one immutable match event credits only once and survives a service restart', async () => {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  const ranks = createRanks({ store, matches, now: () => Date.parse('2026-10-21T00:00:00Z') });
  const id = 'rank-game-1';
  await matches.entered(id, { how: 'create', userId: address(1), app: match.app });
  for (const player of players) {
    await matches.admitted(id, admission(player));
  }
  assert.equal((await ranks.record(id, verdict)).reason, null);
  assert.equal((await ranks.record(id, { ...verdict, winnerTeam: 1 })).winnerTeam, 0);
  assert.equal((await createRanks({ store, matches }).of(address(1))).points, 25);
  assert.equal((await ranks.of(address(4))).points, 0);
  assert.equal((await ranks.of(address(1))).last.delta, 25);
  assert.equal(tierFor(100), 'SILVER');
});

test('bot and preview games stay visible as unranked decisions', async () => {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  const ranks = createRanks({ store, matches });
  const id = 'bot-game';
  await matches.entered(id, { how: 'create', userId: address(1), app: match.app });
  await matches.admitted(id, admission(players[0]));
  assert.equal((await ranks.record(id, { ...verdict, players: players.slice(0, 1) })).reason, 'six_humans_required');
  assert.equal((await ranks.of(address(1))).points, 0);
  assert.equal((await ranks.eventOf(id)).reason, 'six_humans_required');
});

test('four wins reach Silver and a loss updates the same player rather than the Genesis', async () => {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  const ranks = createRanks({ store, matches, now: () => Date.parse('2026-10-21T01:00:00Z') });
  for (let number = 1; number <= 5; number++) {
    const id = `round-${number}`;
    await matches.entered(id, { how: 'create', userId: address(1), app: match.app });
    for (const player of players) await matches.admitted(id, admission(player));
    await ranks.record(id, { ...verdict, winnerTeam: number === 5 ? 1 : 0 });
  }
  const score = await ranks.of(address(1));
  assert.equal(score.points, 85);
  assert.equal(score.tier, 'BRONZE');
  assert.equal(score.matches, 5);
  assert.equal(score.wins, 4);
  assert.equal(score.last.tierBefore, 'SILVER');
  assert.equal(score.last.tierAfter, 'BRONZE');
  assert.equal(score.last.delta, -15);
});

test('the player rank endpoint requires the signed wallet and reports the latest match honestly', async () => {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  const ranks = createRanks({ store, matches });
  const sessions = createSessions({ secret: 'a test secret that is long enough to be accepted' });
  const wallet = createWallet();
  const api = createApi({ sessions, matches, ranks, publicOrigin: 'https://game.example' });
  const get = token => api(new Request('https://game.example/api/rank/me', {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  }));
  assert.equal((await get()).status, 401);
  const challenge = await api(new Request('https://game.example/api/session/challenge', {
    method: 'POST', body: JSON.stringify({ address: wallet.address }),
  })).then(response => response.json());
  const signed = await api(new Request('https://game.example/api/session', {
    method: 'POST', body: JSON.stringify({ ...challenge, signature: wallet.personalSign(challenge.message) }),
  })).then(response => response.json());
  assert.equal((await get(signed.session)).status, 200);
  const id = 'still-playing';
  await matches.entered(id, { how: 'create', userId: wallet.address, app: match.app });
  await matches.admitted(id, { ...admission(players[0]), address: wallet.address });
  await matches.started(id, { Started: true });
  const pending = await (await get(signed.session)).json();
  assert.equal(pending.rank.points, 0);
  assert.equal(pending.latestMatch.status, 'verifying');
  await ranks.record(id, { ...verdict, players: players.slice(0, 1) });
  const casual = await (await get(signed.session)).json();
  assert.equal(casual.latestMatch.status, 'six_humans_required');
  assert.equal(casual.rank.points, 0);
});
