import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createApi } from '../src/api.js';
import { createCareers, MASTERY_STEPS, masteryOf } from '../src/career.js';
import { createSessions } from '../src/session.js';
import { createWallet } from './wallet.js';

const ALICE = '0x' + 'a1'.repeat(20);
const BOB = '0x' + 'b2'.repeat(20);
const MINUTE = 60 * 1000;

// Careers kept in memory, with a clock the test moves.
function memoryCareers() {
  const disk = { saved: null, clock: Date.UTC(2026, 9, 9, 12) };
  const open = () => createCareers({
    load: async () => (disk.saved ? JSON.parse(disk.saved) : null),
    save: async state => { disk.saved = JSON.stringify(state); },
    now: () => disk.clock,
  });
  return { disk, open, careers: open() };
}

const badgeIds = career => career.badges.map(badge => badge.id);

test('a Genesis that never played has an empty career', async () => {
  const { careers } = memoryCareers();
  assert.deepEqual(await careers.get([1795, 3]), [
    { tokenId: 1795, matches: 0, wins: 0, guestMatches: 0, rentedMatches: 0, bestStreak: 0, confirmed: 0, lastPlayedAt: 0, mastery: { level: 1, points: 0, next: 3 }, badges: [] },
    { tokenId: 3, matches: 0, wins: 0, guestMatches: 0, rentedMatches: 0, bestStreak: 0, confirmed: 0, lastPlayedAt: 0, mastery: { level: 1, points: 0, next: 3 }, badges: [] },
  ]);
});

test('mastery grows by one point a match and one more a win, level by level', async () => {
  assert.deepEqual(masteryOf({ matches: 0, wins: 0 }), { level: 1, points: 0, next: 3 });
  assert.deepEqual(masteryOf({ matches: 2, wins: 0 }), { level: 1, points: 2, next: 3 });
  assert.deepEqual(masteryOf({ matches: 2, wins: 1 }), { level: 2, points: 3, next: 8 }, 'two matches and a win');
  assert.deepEqual(masteryOf({ matches: 10, wins: 5 }), { level: 4, points: 15, next: 25 });
  assert.deepEqual(masteryOf({ matches: 30, wins: 10 }), { level: 6, points: 40, next: 0 }, 'the last level has no next one');
  assert.equal(MASTERY_STEPS.every((step, index) => index === 0 || step > MASTERY_STEPS[index - 1]), true);

  // It is part of what the services answer for a career, and moves with the matches credited.
  const { careers, disk } = memoryCareers();
  await careers.record({ tokenId: 9, address: ALICE, matchId: 'room-mastery:1', team: 0, winnerTeam: 0 });
  disk.clock += 2 * MINUTE;
  const second = await careers.record({ tokenId: 9, address: ALICE, matchId: 'room-mastery:2', team: 0, winnerTeam: 1 });
  assert.deepEqual(second.career.mastery, { level: 2, points: 3, next: 8 });
});

test('a match is credited once to the Genesis that played it', async () => {
  const { careers, disk } = memoryCareers();
  const first = await careers.record({ tokenId: 1795, address: ALICE, matchId: 'room-one:1', team: 0, winnerTeam: 0 });
  assert.equal(first.credited, true);
  assert.equal(first.won, true);
  assert.deepEqual(badgeIds(first.career), ['first-match', 'first-win']);

  // The page reports the same match again, for instance after a reload.
  disk.clock += 5 * MINUTE;
  const again = await careers.record({ tokenId: 1795, address: ALICE, matchId: 'room-one:1', team: 0, winnerTeam: 0 });
  assert.equal(again.credited, false);
  assert.equal(again.reason, 'already_recorded');
  assert.equal((await careers.get([1795]))[0].matches, 1);
});

test('matches reported faster than a match can be played are not credited', async () => {
  const { careers, disk } = memoryCareers();
  await careers.record({ tokenId: 8, address: ALICE, matchId: 'room-fast:1', team: 0, winnerTeam: 1 });
  disk.clock += 20 * 1000;
  const early = await careers.record({ tokenId: 8, address: ALICE, matchId: 'room-fast:2', team: 0, winnerTeam: 0 });
  assert.equal(early.credited, false);
  assert.equal(early.reason, 'too_soon');
  disk.clock += 50 * 1000;
  // The refused match was not kept: it can still be credited later.
  const later = await careers.record({ tokenId: 8, address: ALICE, matchId: 'room-fast:2', team: 0, winnerTeam: 0 });
  assert.equal(later.credited, true);
  assert.deepEqual([later.career.matches, later.career.wins], [2, 1]);
});

test('badges follow what the Genesis did: three wins in a row, ten matches', async () => {
  const { careers, disk } = memoryCareers();
  const results = [0, 0, 1, 0, 0, 0, 1, 1, 1, 1];
  let career;
  for (let index = 0; index < results.length; index++) {
    disk.clock += 2 * MINUTE;
    career = (await careers.record({ tokenId: 408, address: ALICE, matchId: `room-long:${index}`, team: 0, winnerTeam: results[index] })).career;
    if (index === 4) assert.deepEqual(badgeIds(career), ['first-match', 'first-win'], 'two wins, a loss, two wins: no streak of three yet');
  }
  assert.deepEqual([career.matches, career.wins, career.bestStreak], [10, 5, 3]);
  assert.deepEqual(badgeIds(career), ['first-match', 'first-win', 'three-in-a-row', 'veteran']);
});

test('a result is confirmed only when another player of the match reported the same winner', async () => {
  const { careers, disk } = memoryCareers();
  await careers.record({ tokenId: 3, address: ALICE, matchId: 'room-duo:1', team: 0, winnerTeam: 0 });
  assert.equal((await careers.get([3]))[0].confirmed, 0, 'one report confirms nothing');
  await careers.record({ tokenId: 9, address: BOB, matchId: 'room-duo:1', team: 1, winnerTeam: 0 });
  assert.deepEqual((await careers.get([3, 9])).map(career => [career.wins, career.confirmed]), [[1, 1], [0, 1]]);

  // Two Genesis of the same wallet agreeing with each other confirm nothing.
  disk.clock += 2 * MINUTE;
  await careers.record({ tokenId: 3, address: ALICE, matchId: 'room-solo:1', team: 0, winnerTeam: 0 });
  await careers.record({ tokenId: 4, address: ALICE, matchId: 'room-solo:1', team: 0, winnerTeam: 0 });
  assert.equal((await careers.get([4]))[0].confirmed, 0);

  // A match whose players disagree is confirmed for nobody.
  disk.clock += 2 * MINUTE;
  await careers.record({ tokenId: 3, address: ALICE, matchId: 'room-clash:1', team: 0, winnerTeam: 0 });
  await careers.record({ tokenId: 9, address: BOB, matchId: 'room-clash:1', team: 1, winnerTeam: 1 });
  assert.deepEqual((await careers.get([3, 9])).map(career => career.confirmed), [1, 1]);
});

test('the top lists the Genesis by wins, then by matches, and leaves out those that never played', async () => {
  const { careers, disk } = memoryCareers();
  const play = async (tokenId, matchId, winnerTeam) => {
    disk.clock += 2 * MINUTE;
    await careers.record({ tokenId, address: ALICE, matchId, team: 0, winnerTeam });
  };
  await play(8, 'room-top:1', 0);
  await play(8, 'room-top:2', 1);
  await play(3, 'room-top:3', 0);
  await play(3, 'room-top:4', 0);
  await play(1795, 'room-top:5', 0);
  await play(9, 'room-top:6', 1);
  assert.deepEqual((await careers.top(10)).map(career => [career.tokenId, career.wins, career.matches]),
    [[3, 2, 2], [8, 1, 2], [1795, 1, 1], [9, 0, 1]]);
  assert.deepEqual((await careers.top(2)).map(career => career.tokenId), [3, 8]);
  assert.deepEqual(await memoryCareers().careers.top(10), []);
});

test('a malformed result is refused and changes nothing', async () => {
  const { careers, disk } = memoryCareers();
  for (const result of [
    { tokenId: 0, matchId: 'room-bad:1', team: 0, winnerTeam: 0 },
    { tokenId: 2000, matchId: 'room-bad:1', team: 0, winnerTeam: 0 },
    { tokenId: 1795, matchId: 'short', team: 0, winnerTeam: 0 },
    { tokenId: 1795, matchId: 'room with spaces', team: 0, winnerTeam: 0 },
    { tokenId: 1795, matchId: 'room-bad:1', team: 2, winnerTeam: 0 },
    { tokenId: 1795, matchId: 'room-bad:1', team: 0, winnerTeam: -1 },
  ]) {
    assert.equal((await careers.record({ address: ALICE, ...result })).credited, false);
  }
  assert.equal(disk.saved, null);
});

test('the career survives a restart of the services', async () => {
  const { careers, open } = memoryCareers();
  await careers.record({ tokenId: 1795, address: ALICE, matchId: 'room-kept:1', team: 1, winnerTeam: 1 });
  assert.deepEqual((await open().get([1795])).map(career => [career.matches, career.wins]), [[1, 1]]);
});

test('over HTTP: only the signed-in holder of a Genesis adds to its career, and anyone reads it', async () => {
  const holder = createWallet();
  const visitor = createWallet();
  const held = new Map([[holder.address, [1795]]]);
  const chain = {
    owners: async () => ({ block: 1 }),
    ownedBy: async address => ({ tokens: held.get(address.toLowerCase()) ?? [], block: 1 }),
    holds: async (address, tokenId) => ({ holds: (held.get(address.toLowerCase()) ?? []).includes(tokenId), block: 1 }),
  };
  const api = createApi({ chain, careers: memoryCareers().careers, sessions: createSessions({ secret: 'a test secret that is long enough to be accepted' }) });
  const post = (path, body, session) => api(new Request('http://127.0.0.1:8090' + path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Forwarded-Host': 'game.example', 'X-Forwarded-Proto': 'https',
      ...(session ? { Authorization: 'Bearer ' + session } : {}) },
    body: JSON.stringify(body),
  }));
  const signIn = async wallet => {
    const challenge = await (await post('/api/session/challenge', { address: wallet.address })).json();
    assert.ok(challenge.message.startsWith('game.example wants you to sign in'), 'the message names the page, not the services');
    const answer = await post('/api/session', { ...challenge, signature: wallet.personalSign(challenge.message) });
    assert.equal(answer.status, 200);
    return (await answer.json()).session;
  };
  const result = { tokenId: 1795, matchId: 'room-http:42', team: 1, winnerTeam: 1 };

  assert.equal((await post('/api/career/result', result)).status, 401, 'no session');
  assert.equal((await post('/api/career/result', result, 'made.up')).status, 401, 'invented session');
  assert.equal((await post('/api/career/result', result, await signIn(visitor))).status, 403, 'signed in, but not the holder');
  assert.equal((await post('/api/session', { message: 'x', proof: 'y', signature: 'z' })).status, 401);
  assert.equal((await post('/api/session/challenge', { address: 'nope' })).status, 400);

  const session = await signIn(holder);
  const credited = await post('/api/career/result', result, session);
  assert.equal(credited.status, 200);
  assert.equal((await credited.json()).credited, true);
  const repeated = await (await post('/api/career/result', result, session)).json();
  assert.deepEqual([repeated.credited, repeated.reason], [false, 'already_recorded']);
  assert.equal((await post('/api/career/result', { ...result, matchId: 'x' }, session)).status, 400);

  // The holder sells the Genesis: the session is still valid, the credit is not.
  held.set(holder.address, []);
  assert.equal((await post('/api/career/result', { ...result, matchId: 'room-http:43' }, session)).status, 403);

  const read = await api(new Request('http://127.0.0.1:8090/api/career?tokens=1795,3'));
  assert.deepEqual((await read.json()).careers.map(career => [career.tokenId, career.matches, career.wins]), [[1795, 1, 1], [3, 0, 0]]);
  const top = await api(new Request('http://127.0.0.1:8090/api/career/top?limit=5'));
  assert.deepEqual((await top.json()).careers.map(career => career.tokenId), [1795]);
  assert.equal((await api(new Request('http://127.0.0.1:8090/api/career/top?limit=0'))).status, 400);
  assert.equal((await api(new Request('http://127.0.0.1:8090/api/career?tokens=0'))).status, 400);
  assert.equal((await api(new Request('http://127.0.0.1:8090/api/career'))).status, 400);
});
