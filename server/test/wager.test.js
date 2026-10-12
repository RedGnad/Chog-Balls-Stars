import assert from 'node:assert/strict';
import { test } from 'node:test';
import { secp256k1 } from '@noble/curves/secp256k1.js';
import { keccak_256 } from '@noble/hashes/sha3.js';
import { createApi } from '../src/api.js';
import { createCareers } from '../src/career.js';
import { createMatches, createMemoryStore } from '../src/matches.js';
import { createReferee, matchKeyOf, resultDigest, ticketDigest, voidDigest } from '../src/referee.js';
import { createSessions } from '../src/session.js';
import { createWager, TICKET_SECONDS } from '../src/wager.js';
import { createWallet, hexToBytes } from './wallet.js';

const SECRET = 'a test secret that is long enough to be accepted';
const ORIGIN = 'https://game.example';
const WAGER = '0x' + 'aa'.padStart(40, '0');
const PLAYER = '0x' + 'bb'.padStart(40, '0');
// A key made for this test: it signs nothing outside it.
const REFEREE_KEY = '11'.repeat(32);
const ALICE = createWallet();
const BOB = createWallet();
const word = value => BigInt(value).toString(16).padStart(64, '0');
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const selector = signature => toHex(keccak_256(new TextEncoder().encode(signature)).slice(0, 4));

// The address that signed a digest the way the contract recovers it.
function signerOf(digest, signature) {
  const bytes = hexToBytes(signature);
  const message = keccak_256(Uint8Array.from([...new TextEncoder().encode('\x19Ethereum Signed Message:\n32'), ...hexToBytes(digest)]));
  const parsed = secp256k1.Signature.fromBytes(Uint8Array.from([bytes[64] - 27, ...bytes.slice(0, 64)]), 'recovered');
  return '0x' + toHex(keccak_256(parsed.recoverPublicKey(message).toBytes(false).slice(1)).slice(-20));
}

test('the referee signs the digests the contract computes', () => {
  // Reference values computed by cast (tools/contracts/wager_digests.sh).
  const matchKey = matchKeyOf('0:eu:room~1234');
  assert.equal(matchKey, '0x3ed1720486303eb00d7fa1f4b59ffac849dc5553fe292b20555ae44e3387317a');
  const ticket = { chainId: 143, wager: WAGER, matchKey, player: PLAYER, team: 1, amount: '25000000000000000000', deadline: 1790000120 };
  assert.equal(ticketDigest(ticket), '0x56fe37ff00fb639306f5e07e8962afba2b25d2d6a046c761aa6dfe692946ba42');
  assert.equal(resultDigest({ chainId: 143, wager: WAGER, matchKey, winnerTeam: 1 }), '0xa900b696d244f90d13e84152360d48a7a1e37312c8d0f9c49dd23d210f954a12');
  assert.equal(voidDigest({ chainId: 143, wager: WAGER, matchKey }), '0x86c153e762d73e126bf599a46ad55a84dc437b4a601eecd627b32ccac34f3c82');

  const referee = createReferee({ privateKey: REFEREE_KEY, chainId: 143, wager: WAGER });
  const signature = referee.signTicket(ticket);
  assert.match(signature, /^0x[0-9a-f]{128}(1b|1c)$/);
  assert.equal(signerOf(ticketDigest(ticket), signature), referee.address);
  assert.notEqual(signerOf(ticketDigest({ ...ticket, team: 0 }), signature), referee.address, 'a ticket is good for one team');
  assert.throws(() => referee.signTicket({ ...ticket, amount: '0' }), /more than zero/);
  assert.throws(() => referee.signResult({ matchKey, winnerTeam: 3 }), /0 to 2/);
  assert.throws(() => createReferee({ privateKey: 'short', chainId: 143, wager: WAGER }), /64 hex digits/);
});

// Services with a wager contract whose stakes the test writes, and a clock the test moves.
function services({ contractReferee } = {}) {
  const referee = createReferee({ privateKey: REFEREE_KEY, chainId: 143, wager: WAGER });
  const state = { clock: 1_790_000_000_000, stakes: new Map(), referee: contractReferee ?? referee.address };
  const pack = (...lists) => {
    let heads = '';
    let tails = '';
    let offset = lists.length * 32;
    for (const list of lists) {
      heads += word(offset);
      tails += word(list.length) + list.map(word).join('');
      offset += 32 * (1 + list.length);
    }
    return '0x' + heads + tails;
  };
  const answer = ({ data }) => {
    const key = '0x' + data.slice(10);
    const staked = state.stakes.get(key) ?? [];
    switch (data.slice(2, 10)) {
      case selector('referee()'): return '0x' + word(state.referee);
      case selector('burnedShare()'): return '0x' + word(1000);
      case selector('matchOf(bytes32)'):
        return '0x' + word(staked.length > 0 ? 1 : 0) + word(staked.length > 0 ? 25n * 10n ** 18n : 0) + word(staked.length > 0 ? 1_790_000_000 : 0)
          + word(staked.filter(stake => stake.team === 0).length) + word(staked.filter(stake => stake.team === 1).length);
      case selector('stakersOf(bytes32)'): return pack(staked.map(stake => stake.address), staked.map(stake => stake.team));
      default: throw new Error('unexpected call ' + data.slice(0, 10));
    }
  };
  const chain = { callMany: async calls => calls.map(answer), holds: async () => ({ holds: false }) };
  const now = () => state.clock;
  const matches = createMatches({ store: createMemoryStore(), now });
  const sessions = createSessions({ secret: SECRET, now });
  let saved = null;
  const careers = createCareers({ load: async () => saved, save: async value => { saved = value; } });
  const wager = createWager({ chain, referee, matches, now });
  const api = createApi({ chain, sessions, careers, wager, publicOrigin: ORIGIN });
  return { state, api, matches, wager, referee };
}

async function sessionOf(api, wallet) {
  const post = (path, body) => api(new Request(ORIGIN + path, { method: 'POST', body: JSON.stringify(body) })).then(response => response.json());
  const { message, proof } = await post('/api/session/challenge', { address: wallet.address });
  return (await post('/api/session', { message, proof, signature: wallet.personalSign(message) })).session;
}

const call = (api, method, path, session, body) => api(new Request(ORIGIN + path, {
  method, headers: session ? { Authorization: 'Bearer ' + session } : {}, body: body ? JSON.stringify(body) : undefined,
})).then(async response => ({ status: response.status, body: await response.json() }));

// Alice (player 0) and Bob (player 1) are let into the match "game" and its record starts.
async function matchOfTwo(matches, gameId = 'game') {
  await matches.entered(gameId, { how: 'create', userId: ALICE.address, authCookie: null, app: { roomName: 'party' } });
  await matches.admitted(gameId, { userId: ALICE.address, actorNr: 1, playerSlot: 0, address: ALICE.address, tokenId: 1795 });
  await matches.added(gameId, { userId: ALICE.address, actorNr: 1, playerSlot: 0, player: 0 });
  await matches.admitted(gameId, { userId: BOB.address, actorNr: 2, playerSlot: 0, address: BOB.address, tokenId: 0 });
  await matches.added(gameId, { userId: BOB.address, actorNr: 2, playerSlot: 0, player: 1 });
  await matches.started(gameId, { GameId: gameId });
}

const AMOUNT = (25n * 10n ** 18n).toString();
// Alice in the first team and Bob in the second, the selection of characters over 20 seconds after the start.
const verdict = changes => ({
  winnerTeam: 1, scores: [3, 11], endedAtTick: 3235, selectionEndedAtTick: 660, firstTick: 60, lastTick: 3239, updateRate: 30,
  players: [{ player: 0, team: 0, tokenId: 1795 }, { player: 1, team: 1, tokenId: 0 }], ...changes,
});

test('the game is told where to stake, and nothing when the contract knows another referee', async () => {
  const { api, referee } = services();
  const described = (await call(api, 'GET', '/api/wager')).body;
  assert.deepEqual(described, {
    enabled: true, address: WAGER, token: '0x350035555e10d9afaf1566aaebfced5ba6c27777', chainId: 143, referee: referee.address,
    burnedShare: 1000, ticketSeconds: TICKET_SECONDS, refundAfterSeconds: 3600,
  });
  const other = services({ contractReferee: '0x' + 'cc'.repeat(20) });
  assert.equal((await call(other.api, 'GET', '/api/wager')).body.enabled, false);
  const none = createApi({ chain: {}, sessions: createSessions({ secret: SECRET }), careers: {}, publicOrigin: ORIGIN });
  assert.deepEqual((await call(none, 'GET', '/api/wager')).body, { enabled: false });
});

test('a ticket is only for a wallet let into the match, and fixes what everyone stakes', async () => {
  const { api, matches, referee, state } = services();
  const alice = await sessionOf(api, ALICE);
  assert.equal((await call(api, 'POST', '/api/wager/ticket', null, { team: 0, amount: AMOUNT })).status, 401);
  const early = await call(api, 'POST', '/api/wager/ticket', alice, { team: 0, amount: AMOUNT });
  assert.equal(early.status, 409);
  assert.equal(early.body.error, 'not_in_a_match');

  await matchOfTwo(matches);
  assert.equal((await call(api, 'POST', '/api/wager/ticket', alice, { team: 2, amount: AMOUNT })).body.error, 'bad_team');
  assert.equal((await call(api, 'POST', '/api/wager/ticket', alice, { team: 0, amount: 25 })).body.error, 'bad_amount');

  const { status, body: ticket } = await call(api, 'POST', '/api/wager/ticket', alice, { team: 0, amount: AMOUNT });
  assert.equal(status, 200);
  assert.equal(ticket.gameId, 'game');
  assert.equal(ticket.matchKey, matchKeyOf('game'));
  assert.equal(ticket.deadline, state.clock / 1000 + TICKET_SECONDS);
  const digest = ticketDigest({ chainId: 143, wager: WAGER, matchKey: ticket.matchKey, player: ALICE.address, team: 0, amount: AMOUNT, deadline: ticket.deadline });
  assert.equal(signerOf(digest, ticket.signature), referee.address, 'the ticket is the one the contract will accept from this wallet');

  const bob = await sessionOf(api, BOB);
  const dearer = await call(api, 'POST', '/api/wager/ticket', bob, { team: 1, amount: (50n * 10n ** 18n).toString() });
  assert.equal(dearer.body.error, 'not_the_stake_of_the_match');
  assert.equal(dearer.body.amount, AMOUNT);

  const stranger = await sessionOf(api, createWallet());
  assert.equal((await call(api, 'POST', '/api/wager/ticket', stranger, { team: 1, amount: AMOUNT })).body.error, 'not_in_a_match');

  const mine = (await call(api, 'GET', '/api/wager/mine', alice)).body;
  assert.equal(mine.inMatch, true);
  assert.equal(mine.roomName, 'party');
  assert.equal(mine.amount, AMOUNT);
  assert.deepEqual(mine.stakes.stakers, []);
});

test('the stakes are locked only when no ticket can be used any more, and then no ticket is given', async () => {
  const { api, matches, state } = services();
  await matchOfTwo(matches);
  const alice = await sessionOf(api, ALICE);
  const bob = await sessionOf(api, BOB);
  const key = matchKeyOf('game');
  await call(api, 'POST', '/api/wager/ticket', alice, { team: 0, amount: AMOUNT });
  await call(api, 'POST', '/api/wager/ticket', bob, { team: 1, amount: AMOUNT });

  assert.equal((await call(api, 'POST', '/api/wager/lock', await sessionOf(api, createWallet()))).body.error, 'not_in_a_match', 'only a wallet of the match locks');
  state.stakes.set(key, [{ address: ALICE.address, team: 0 }]);
  const waiting = (await call(api, 'POST', '/api/wager/lock', alice)).body;
  assert.equal(waiting.locked, false, 'the ticket of Bob can still be used');
  assert.ok(waiting.waitSeconds > TICKET_SECONDS && waiting.waitSeconds <= TICKET_SECONDS + 20);

  state.stakes.set(key, [{ address: ALICE.address, team: 0 }, { address: BOB.address, team: 1 }]);
  const locked = (await call(api, 'POST', '/api/wager/lock', alice)).body;
  assert.equal(locked.locked, true);
  assert.deepEqual(locked.book, [{ address: ALICE.address, team: 0 }, { address: BOB.address, team: 1 }]);
  assert.equal((await call(api, 'POST', '/api/wager/ticket', bob, { team: 1, amount: AMOUNT })).body.error, 'stakes_locked');
  assert.equal((await call(api, 'POST', '/api/wager/lock', bob)).body.lockedAt, locked.lockedAt, 'locking twice changes nothing');
});

test('a ticket that ran out no longer holds the lock back', async () => {
  const { api, matches, state } = services();
  await matchOfTwo(matches);
  const alice = await sessionOf(api, ALICE);
  const bob = await sessionOf(api, BOB);
  await call(api, 'POST', '/api/wager/ticket', alice, { team: 0, amount: AMOUNT });
  await call(api, 'POST', '/api/wager/ticket', bob, { team: 1, amount: AMOUNT });
  state.stakes.set(matchKeyOf('game'), [{ address: ALICE.address, team: 0 }]);
  state.clock += (TICKET_SECONDS + 30) * 1000;
  const locked = (await call(api, 'POST', '/api/wager/lock', alice)).body;
  assert.equal(locked.locked, true);
  assert.deepEqual(locked.book, [{ address: ALICE.address, team: 0 }]);
});

// Both stake, the stakes are locked five seconds after the start, the match is judged.
async function stakedAndJudged(judged, { lock = true, lateStake = false, lockAfterSeconds = 5 } = {}) {
  const made = services();
  const { api, matches, state } = made;
  await matchOfTwo(matches);
  const alice = await sessionOf(api, ALICE);
  const bob = await sessionOf(api, BOB);
  const key = matchKeyOf('game');
  await call(api, 'POST', '/api/wager/ticket', alice, { team: 0, amount: AMOUNT });
  await call(api, 'POST', '/api/wager/ticket', bob, { team: 1, amount: AMOUNT });
  state.stakes.set(key, [{ address: ALICE.address, team: 0 }, { address: BOB.address, team: 1 }]);
  state.clock += lockAfterSeconds * 1000;
  if (lock) assert.equal((await call(api, 'POST', '/api/wager/lock', alice)).body.locked, true);
  if (lateStake) state.stakes.get(key).push({ address: '0x' + 'dd'.repeat(20), team: 1 });
  const waiting = (await call(api, 'GET', '/api/wager/result?gameId=game')).body;
  assert.equal(waiting.pending, true);
  assert.equal(waiting.matchKey, key);
  assert.equal(waiting.stakes.state, 'open', 'the page is told where the stakes stand while the match is not judged');
  await matches.setVerdict('game', judged);
  return { ...made, key, result: (await call(api, 'GET', '/api/wager/result?gameId=game')).body };
}

test('a judged match gets the result the contract pays on, signed once', async () => {
  const { api, result, key, referee, state } = await stakedAndJudged(verdict());
  assert.equal(result.winnerTeam, 1);
  assert.equal(result.matchKey, key);
  assert.equal(signerOf(resultDigest({ chainId: 143, wager: WAGER, matchKey: key, winnerTeam: 1 }), result.signature), referee.address);

  // Whatever the chain says later, the decision of a match is the one already made.
  state.stakes.get(key).push({ address: '0x' + 'dd'.repeat(20), team: 0 });
  const again = (await call(api, 'GET', '/api/wager/result?gameId=game')).body;
  assert.deepEqual([again.winnerTeam, again.signature, again.at], [result.winnerTeam, result.signature, result.at]);
  assert.equal(again.stakes.stakers.length, 3, 'while the stakes are read on the chain each time');
  assert.equal((await call(api, 'GET', '/api/wager/result?gameId=unknown')).status, 404);

  const draw = await stakedAndJudged(verdict({ winnerTeam: 2, scores: [7, 7] }));
  assert.equal(draw.result.winnerTeam, 2);
});

test('every doubt calls the match off, which gives each stake back', async () => {
  const calledOff = async (judged, options, why) => {
    const { result, key, referee } = await stakedAndJudged(judged, options);
    assert.equal(result.void, true, why.source);
    assert.match(result.why, why);
    assert.equal(result.winnerTeam, undefined);
    assert.equal(signerOf(voidDigest({ chainId: 143, wager: WAGER, matchKey: key }), result.signature), referee.address);
  };
  await calledOff({ void: true, why: 'the match did not reach its end' }, {}, /did not reach its end/);
  await calledOff(verdict(), { lock: false }, /never locked/);
  await calledOff(verdict(), { lateStake: true }, /after the stakes were locked/);
  // The selection ended 20 seconds after the start: a lock 30 seconds after it came too late.
  await calledOff(verdict(), { lockAfterSeconds: 30 }, /began before the stakes were locked/);
  await calledOff(verdict({ players: [{ player: 0, team: 1, tokenId: 1795 }, { player: 1, team: 1, tokenId: 0 }] }), {}, /team staked on/);
  await calledOff(verdict({ players: [{ player: 1, team: 1, tokenId: 0 }] }), {}, /did not play the match/);
});
