import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createMatches, createMemoryStore } from '../src/matches.js';
import { BANDS, createRanks, pointsFor, rankEligibility, tierFor } from '../src/ranks.js';
import { createApi } from '../src/api.js';
import { createSessions } from '../src/session.js';
import { createWallet } from './wallet.js';

const address = number => '0x' + number.toString(16).padStart(40, '0');
const players = Array.from({ length: 6 }, (_, index) => ({ player: index + 1, team: index < 3 ? 0 : 1, tokenId: index + 1 }));
const admission = player => ({ player: player.player, team: player.team, tokenId: player.tokenId, address: address(player.player), right: 'owner' });
const match = { app: { appVersion: 'sim-example-siege' }, admissions: players.map(admission) };
const verdict = { winnerTeam: 0, mode: 'Siege', testOptions: false, players };

test('ranking accepts one to six replayed humans and requires a signed-in recipient', () => {
  assert.equal(rankEligibility(match, verdict), null);
  assert.equal(rankEligibility(match, { ...verdict, players: players.slice(0, 1) }), null);
  assert.equal(rankEligibility(match, { ...verdict, void: true, winnerTeam: null }), 'unverified');
  assert.equal(rankEligibility(match, { ...verdict, players: [] }), 'invalid_human_count');
  assert.equal(rankEligibility(match, { ...verdict, players: [...players, players[0]] }), 'invalid_human_count');
  assert.equal(rankEligibility(match, { ...verdict, players: [players[0], players[0]] }), 'invalid_human_teams');
  assert.equal(rankEligibility(match, { ...verdict, mode: 'Coins' }), 'siege_only');
  assert.equal(rankEligibility(match, { ...verdict, testOptions: true }), 'preview');
  assert.equal(rankEligibility(match, { ...verdict, testOptions: undefined }), 'preview');
  assert.equal(rankEligibility({ ...match, app: { appVersion: 'sim-example-preview-siege' } }, verdict), 'preview');
  assert.equal(rankEligibility({ ...match, admissions: match.admissions.slice(1) }, verdict), 'invalid_admission');
  assert.equal(rankEligibility({ ...match, admissions: match.admissions.map((entry, index) => index === 5 ? { ...entry, address: address(1) } : entry) }, verdict), 'duplicate_wallet');
  assert.equal(rankEligibility({ ...match, admissions: match.admissions.map((entry, index) => index === 5 ? { ...entry, address: null, right: 'guest' } : entry) }, verdict), null);
  assert.equal(rankEligibility({ ...match, admissions: [{ ...admission(players[0]), address: null, right: 'guest' }] },
    { ...verdict, players: players.slice(0, 1) }), 'no_authenticated_players');
});

test('the frozen reward scale starts at three bot-match points and reaches twenty with six humans', () => {
  assert.deepEqual(Array.from({ length: 6 }, (_, index) => pointsFor(index + 1)), [
    { win: 3, loss: -2 }, { win: 6, loss: -4 }, { win: 10, loss: -6 },
    { win: 13, loss: -8 }, { win: 17, loss: -10 }, { win: 20, loss: -12 },
  ]);
  assert.throws(() => pointsFor(0), RangeError);
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
  assert.equal((await createRanks({ store, matches }).of(address(1))).points, 20);
  assert.equal((await ranks.of(address(4))).points, 0);
  assert.equal((await ranks.of(address(1))).last.delta, 20);
  assert.deepEqual((await ranks.eventOf(id)).scoring, { win: 20, loss: -12, soloCap: null, bands: BANDS });
  assert.equal(tierFor(100), 'SILVER');
});

test('a solo human earns three points against bots, with no credit for a preview', async () => {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  const ranks = createRanks({ store, matches });
  const id = 'bot-game';
  await matches.entered(id, { how: 'create', userId: address(1), app: match.app });
  await matches.admitted(id, admission(players[0]));
  const event = await ranks.record(id, { ...verdict, players: players.slice(0, 1) });
  assert.equal(event.reason, null);
  assert.equal(event.humanCount, 1);
  assert.deepEqual(event.scoring, { win: 3, loss: -2, soloCap: 100, bands: BANDS });
  assert.equal((await ranks.of(address(1))).points, 3);
  const previewId = 'preview-game';
  await matches.entered(previewId, { how: 'create', userId: address(1), app: match.app });
  await matches.admitted(previewId, admission(players[0]));
  assert.equal((await ranks.record(previewId, { ...verdict, players: players.slice(0, 1), testOptions: true })).reason, 'preview');
  assert.equal((await ranks.of(address(1))).points, 3);
});

test('guest seats cannot inflate a mixed human match reward', async () => {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  const ranks = createRanks({ store, matches });
  const id = 'three-humans-with-guest';
  const lineup = [players[0], players[1], players[3]];
  await matches.entered(id, { how: 'create', userId: address(1), app: match.app });
  await matches.admitted(id, admission(players[0]));
  await matches.admitted(id, { ...admission(players[1]), address: null, right: 'guest' });
  await matches.admitted(id, admission(players[3]));
  const event = await ranks.record(id, { ...verdict, players: lineup });
  assert.equal(event.reason, null);
  assert.equal(event.humanCount, 3);
  assert.equal(event.authenticatedHumans, 2);
  assert.equal(event.players.length, 2);
  assert.deepEqual(event.scoring, { win: 6, loss: -4, soloCap: null, bands: BANDS });
  assert.equal((await ranks.of(address(1))).points, 6);
  assert.equal((await ranks.of(address(4))).points, 0);
  assert.equal((await ranks.of(address(2))).matches, 0);
});

test('a bot-match loss subtracts its frozen amount but never takes points below zero', async () => {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  const ranks = createRanks({ store, matches });
  for (let number = 1; number <= 3; number++) {
    const id = `solo-${number}`;
    await matches.entered(id, { how: 'create', userId: address(1), app: match.app });
    await matches.admitted(id, admission(players[0]));
    await ranks.record(id, { ...verdict, players: players.slice(0, 1), winnerTeam: number === 1 ? 0 : 1 });
  }
  const score = await ranks.of(address(1));
  assert.equal(score.points, 0);
  assert.equal(score.matches, 3);
  assert.equal(score.wins, 1);
  assert.equal(score.last.delta, -1);
});

test('five full-human wins reach Silver, then a loss lowers the player rank', async () => {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  const ranks = createRanks({ store, matches, now: () => Date.parse('2026-10-21T01:00:00Z') });
  for (let number = 1; number <= 6; number++) {
    const id = `round-${number}`;
    await matches.entered(id, { how: 'create', userId: address(1), app: match.app });
    for (const player of players) await matches.admitted(id, admission(player));
    await ranks.record(id, { ...verdict, winnerTeam: number === 6 ? 1 : 0 });
  }
  const score = await ranks.of(address(1));
  assert.equal(score.points, 88);
  assert.equal(score.tier, 'BRONZE');
  assert.equal(score.matches, 6);
  assert.equal(score.wins, 5);
  assert.equal(score.last.tierBefore, 'SILVER');
  assert.equal(score.last.tierAfter, 'BRONZE');
  assert.equal(score.last.delta, -12);
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
  const ranked = await (await get(signed.session)).json();
  assert.equal(ranked.latestMatch.status, 'ranked');
  assert.equal(ranked.rank.points, 3);
});
