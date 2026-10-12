import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createApi } from '../src/api.js';
import { createCareers } from '../src/career.js';
import { createSessions } from '../src/session.js';
import { createWallet } from './wallet.js';

const SECRET = 'a test secret that is long enough to be accepted';
const DAY = 24 * 3600 * 1000;
const MINUTE = 60 * 1000;

// The services over HTTP with a holder of the Genesis 1795 and 3, another holder of 8, a
// visitor who holds nothing, a clock and a record of who holds what that the test can move.
function services({ secret = SECRET } = {}) {
  const world = { clock: Date.UTC(2026, 9, 9, 12), saved: null };
  const holder = createWallet();
  const neighbour = createWallet();
  const visitor = createWallet();
  const held = new Map([[holder.address, [1795, 3]], [neighbour.address, [8]]]);
  const chain = {
    ownedBy: async address => ({ tokens: held.get(address.toLowerCase()) ?? [], block: 1 }),
    holds: async (address, tokenId) => ({ holds: (held.get(address.toLowerCase()) ?? []).includes(tokenId), block: 1 }),
  };
  const now = () => world.clock;
  const careers = createCareers({
    load: async () => (world.saved ? JSON.parse(world.saved) : null),
    save: async state => { world.saved = JSON.stringify(state); },
    now,
  });
  const api = createApi({ chain, careers, sessions: createSessions({ secret, now }), publicOrigin: 'https://game.example' });
  const call = (method, path, { body, session, pass } = {}) => api(new Request('http://127.0.0.1:8090' + path, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(session ? { Authorization: 'Bearer ' + session } : {}),
      ...(pass ? { Authorization: 'Pass ' + pass } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  }));
  const signIn = async wallet => {
    const challenge = await (await call('POST', '/api/session/challenge', { body: { address: wallet.address } })).json();
    const answer = await call('POST', '/api/session', { body: { ...challenge, signature: wallet.personalSign(challenge.message) } });
    return (await answer.json()).session;
  };
  const lend = async (session, body) => (await call('POST', '/api/loan', { session, body })).json();
  const refusal = async response => [response.status, (await response.json()).error];
  return { world, holder, neighbour, visitor, held, call, signIn, lend, refusal };
}

test('a holder lends a Genesis: the pass says which one, from whom and until when', async () => {
  const { world, holder, call, signIn, lend } = services();
  const loan = await lend(await signIn(holder), { tokenId: 1795 });
  assert.deepEqual([loan.tokenId, loan.lender, loan.expiresAt], [1795, holder.address, world.clock + 30 * DAY]);

  // Whoever has the pass reads it, without a wallet and without a session.
  const read = await call('GET', '/api/loan', { pass: loan.pass });
  assert.equal(read.status, 200);
  assert.deepEqual(await read.json(), { tokenId: 1795, lender: holder.address, expiresAt: world.clock + 30 * DAY });
});

test('only the signed-in holder of a Genesis lends it, for 1 to 30 days', async () => {
  const { holder, visitor, call, signIn, refusal } = services();
  assert.deepEqual(await refusal(await call('POST', '/api/loan', { body: { tokenId: 1795 } })), [401, 'not_signed_in']);
  assert.deepEqual(await refusal(await call('POST', '/api/loan', { session: await signIn(visitor), body: { tokenId: 1795 } })), [403, 'not_the_holder']);

  const session = await signIn(holder);
  assert.deepEqual(await refusal(await call('POST', '/api/loan', { session, body: { tokenId: 8 } })), [403, 'not_the_holder'], 'held by a neighbour');
  for (const body of [{}, { tokenId: '1795' }, { tokenId: 1795, days: 0 }, { tokenId: 1795, days: 31 }, { tokenId: 1795, days: 1.5 }]) {
    assert.deepEqual(await refusal(await call('POST', '/api/loan', { session, body })), [400, 'bad_request'], JSON.stringify(body));
  }
  assert.equal((await call('POST', '/api/loan', { session, body: { tokenId: 1795, days: 1 } })).status, 200);
});

test('a pass that was rewritten, or made with another secret, lends nothing', async () => {
  const { holder, call, signIn, lend, refusal } = services();
  const loan = await lend(await signIn(holder), { tokenId: 1795 });
  const [payload, proof] = loan.pass.split('.');
  const forged = Buffer.from(JSON.stringify({ tokenId: 3, lender: holder.address, issuedAt: 0, expiresAt: 4e12 })).toString('base64url');
  for (const pass of [forged + '.' + proof, payload + '.' + proof.slice(0, -2) + 'AA', payload, 'made.up', '']) {
    assert.deepEqual(await refusal(await call('GET', '/api/loan', { pass })), [403, 'bad_pass'], pass.slice(0, 20));
  }

  const elsewhere = services({ secret: 'another secret, as long as the first one is' });
  const foreign = await elsewhere.lend(await elsewhere.signIn(elsewhere.holder), { tokenId: 1795 });
  assert.deepEqual(await refusal(await call('GET', '/api/loan', { pass: foreign.pass })), [403, 'bad_pass']);
});

test('a pass stops counting when it runs out, when the holder stops lending, and when the Genesis is sold', async () => {
  const { world, holder, visitor, held, call, signIn, lend, refusal } = services();
  const session = await signIn(holder);

  const short = await lend(session, { tokenId: 1795, days: 1 });
  world.clock += DAY - 1;
  assert.equal((await call('GET', '/api/loan', { pass: short.pass })).status, 200, 'one millisecond before the end');
  world.clock += 1;
  assert.deepEqual(await refusal(await call('GET', '/api/loan', { pass: short.pass })), [403, 'pass_expired']);

  const first = await lend(session, { tokenId: 1795 });
  const other = await lend(session, { tokenId: 3 });
  assert.deepEqual(await refusal(await call('POST', '/api/loan/stop', { body: { tokenId: 1795 } })), [401, 'not_signed_in']);
  assert.deepEqual(await refusal(await call('POST', '/api/loan/stop', { session: await signIn(visitor), body: { tokenId: 1795 } })), [403, 'not_the_holder']);
  assert.deepEqual(await refusal(await call('POST', '/api/loan/stop', { session, body: {} })), [400, 'bad_request']);
  world.clock += MINUTE;
  assert.deepEqual(await (await call('POST', '/api/loan/stop', { session, body: { tokenId: 1795 } })).json(), { tokenId: 1795, stoppedAt: world.clock });
  assert.deepEqual(await refusal(await call('GET', '/api/loan', { pass: first.pass })), [403, 'lending_stopped']);
  assert.equal((await call('GET', '/api/loan', { pass: other.pass })).status, 200, 'the other Genesis is still lent');

  // Lending again after a stop gives a pass that counts; the earlier one stays dead.
  world.clock += MINUTE;
  const second = await lend(session, { tokenId: 1795 });
  assert.equal((await call('GET', '/api/loan', { pass: second.pass })).status, 200);
  assert.deepEqual(await refusal(await call('GET', '/api/loan', { pass: first.pass })), [403, 'lending_stopped']);

  // The holder sells the Genesis: the pass dies with the sale, and comes back with the Genesis.
  held.set(holder.address, [3]);
  assert.deepEqual(await refusal(await call('GET', '/api/loan', { pass: second.pass })), [403, 'lender_not_the_holder']);
  held.set(holder.address, [1795, 3]);
  assert.equal((await call('GET', '/api/loan', { pass: second.pass })).status, 200);
});

test('a guest adds to the career of the Genesis they were lent, and of no other', async () => {
  const { world, holder, call, signIn, lend, refusal } = services();
  const session = await signIn(holder);
  const loan = await lend(session, { tokenId: 1795 });
  const result = { tokenId: 1795, matchId: 'room-guest:1', team: 0, winnerTeam: 0 };

  const credited = await (await call('POST', '/api/career/result', { pass: loan.pass, body: result })).json();
  assert.deepEqual([credited.credited, credited.career.matches, credited.career.wins, credited.career.guestMatches], [true, 1, 1, 1]);
  assert.deepEqual(await refusal(await call('POST', '/api/career/result', { pass: loan.pass, body: { ...result, tokenId: 3 } })), [403, 'not_the_lent_genesis']);
  assert.deepEqual(await refusal(await call('POST', '/api/career/result', { pass: 'made.up', body: result })), [403, 'bad_pass']);

  // The holder plays it too: one career, and the count of guest matches does not move.
  world.clock += 2 * MINUTE;
  const own = await (await call('POST', '/api/career/result', { session, body: { ...result, matchId: 'room-guest:2', winnerTeam: 1 } })).json();
  assert.deepEqual([own.career.matches, own.career.wins, own.career.guestMatches], [2, 1, 1]);

  // After a stop the guest's results are refused.
  world.clock += 2 * MINUTE;
  await call('POST', '/api/loan/stop', { session, body: { tokenId: 1795 } });
  assert.deepEqual(await refusal(await call('POST', '/api/career/result', { pass: loan.pass, body: { ...result, matchId: 'room-guest:3' } })), [403, 'lending_stopped']);
});

test('a holder and their own guest do not confirm each other; another wallet does', async () => {
  const { holder, neighbour, call, signIn, lend } = services();
  const session = await signIn(holder);
  const loan = await lend(session, { tokenId: 1795 });
  const match = { matchId: 'room-three:7', winnerTeam: 1 };
  const confirmed = async tokenId => (await (await call('GET', `/api/career?tokens=${tokenId}`)).json()).careers[0].confirmed;

  await call('POST', '/api/career/result', { pass: loan.pass, body: { ...match, tokenId: 1795, team: 1 } });
  await call('POST', '/api/career/result', { session, body: { ...match, tokenId: 3, team: 1 } });
  assert.equal(await confirmed(1795), 0, 'both reports stand under the holder\'s address');

  await call('POST', '/api/career/result', { session: await signIn(neighbour), body: { ...match, tokenId: 8, team: 0 } });
  assert.equal(await confirmed(1795), 1);
});
