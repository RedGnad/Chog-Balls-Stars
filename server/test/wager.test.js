import assert from 'node:assert/strict';
import { test } from 'node:test';
import { secp256k1 } from '@noble/curves/secp256k1.js';
import { keccak_256 } from '@noble/hashes/sha3.js';
import { createApi } from '../src/api.js';
import { createCareers } from '../src/career.js';
import { createMatches, createMemoryStore } from '../src/matches.js';
import { createReferee, matchKeyOf, resultDigest, ticketDigest, voidDigest } from '../src/referee.js';
import { createSessions } from '../src/session.js';
import { createWager, genesisPlayersOf, TICKET_SECONDS, wagerNetwork, WEEK_MS, winnerGain } from '../src/wager.js';
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
function services({ contractReferee, network = wagerNetwork(), genesisShare } = {}) {
  const referee = createReferee({ privateKey: REFEREE_KEY, chainId: network.chainId, wager: WAGER });
  // "paid" holds the keys of the matches the contract has settled.
  const state = { clock: 1_790_000_000_000, stakes: new Map(), paid: new Set(), referee: contractReferee ?? referee.address };
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
      case selector('burnedShare()'): return '0x' + word(genesisShare === undefined ? 1000 : 500);
      // Version 1 has no genesisShare(): the call fails, which a multicall answers with null.
      case selector('genesisShare()'): return genesisShare === undefined ? null : '0x' + word(genesisShare);
      case selector('matchOf(bytes32)'):
        return '0x' + word(staked.length > 0 ? (state.paid.has(key) ? 2 : 1) : 0) + word(staked.length > 0 ? 25n * 10n ** 18n : 0) + word(staked.length > 0 ? 1_790_000_000 : 0)
          + word(staked.filter(stake => stake.team === 0).length) + word(staked.filter(stake => stake.team === 1).length);
      case selector('stakersOf(bytes32)'): return pack(staked.map(stake => stake.address), staked.map(stake => stake.team));
      default: throw new Error('unexpected call ' + data.slice(0, 10));
    }
  };
  const chain = { callMany: async calls => calls.map(answer), holds: async () => ({ holds: false }) };
  const now = () => state.clock;
  const store = createMemoryStore();
  const matches = createMatches({ store, now });
  const sessions = createSessions({ secret: SECRET, now });
  let saved = null;
  const careers = createCareers({ load: async () => saved, save: async value => { saved = value; } });
  // A $CHOG at 0.0015 USD, as on 21 October 2026.
  const price = { usdOfWei: async wei => Number(BigInt(wei) / 10n ** 12n) / 1e6 * 0.0015 };
  const wager = createWager({ chain, referee, matches, network, now, store, price });
  const api = createApi({ chain, sessions, careers, wager, publicOrigin: ORIGIN });
  return { state, api, matches, wager, referee, store };
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
    enabled: true, address: WAGER, token: '0x350035555e10d9afaf1566aaebfced5ba6c27777', chainId: 143, chainName: 'Monad',
    rpcUrl: 'https://rpc.monad.xyz', test: false, referee: referee.address,
    burnedShare: 1000, genesisShare: 0, version: 1, relay: false, ticketSeconds: TICKET_SECONDS, refundAfterSeconds: 3600,
  });
  const other = services({ contractReferee: '0x' + 'cc'.repeat(20) });
  assert.equal((await call(other.api, 'GET', '/api/wager')).body.enabled, false);
  const none = createApi({ chain: {}, sessions: createSessions({ secret: SECRET }), careers: {}, publicOrigin: ORIGIN });
  assert.deepEqual((await call(none, 'GET', '/api/wager')).body, { enabled: false });
});

test('on the testnet the game is told the chain and the test token, and tickets are for that chain', async () => {
  const TEST_TOKEN = '0x' + 'dd'.repeat(20);
  const network = wagerNetwork({ network: 'testnet', token: TEST_TOKEN, rpcUrls: ['https://key-in-the-address.example/secret'] });
  const { api, matches, referee } = services({ network });
  const described = (await call(api, 'GET', '/api/wager')).body;
  assert.equal(described.chainId, 10143);
  assert.equal(described.chainName, 'Monad Testnet');
  assert.equal(described.token, TEST_TOKEN);
  assert.equal(described.test, true);
  assert.equal(described.rpcUrl, 'https://testnet-rpc.monad.xyz', 'a wallet is only ever offered a public endpoint');

  await matchOfTwo(matches);
  const alice = await sessionOf(api, ALICE);
  const { body: ticket } = await call(api, 'POST', '/api/wager/ticket', alice, { team: 0, amount: AMOUNT });
  const signed = chainId => signerOf(ticketDigest({ chainId, wager: WAGER, matchKey: ticket.matchKey, player: ALICE.address, team: 0, amount: AMOUNT, deadline: ticket.deadline }), ticket.signature);
  assert.equal(signed(10143), referee.address);
  assert.notEqual(signed(143), referee.address, 'a testnet ticket is worth nothing on mainnet');
});

test('a wager is set on a known chain, in its token', () => {
  assert.throws(() => wagerNetwork({ network: 'sepolia' }), /mainnet" or "testnet/);
  assert.throws(() => wagerNetwork({ network: 'testnet' }), /test token/);
  assert.throws(() => wagerNetwork({ network: 'mainnet', token: '0x' + 'dd'.repeat(20) }), /\$CHOG only/);
  assert.equal(wagerNetwork().token, '0x350035555e10d9afaf1566aaebfced5ba6c27777');
  const referee = createReferee({ privateKey: REFEREE_KEY, chainId: 143, wager: WAGER });
  const testnet = wagerNetwork({ network: 'testnet', token: '0x' + 'dd'.repeat(20) });
  assert.throws(() => createWager({ chain: {}, referee, matches: {}, network: testnet }), /chain 143 and the wager is on chain 10143/);
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
async function stakedAndJudged(judged, { lock = true, lateStake = false, lockAfterSeconds = 5, genesisShare } = {}) {
  const made = services({ genesisShare });
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

test('what a winner gains is what the contract pays: an equal share of what was lost, less the burned share', () => {
  assert.equal(winnerGain(AMOUNT, 1, 1, 1000), 22_500_000_000_000_000_000n);
  assert.equal(winnerGain(AMOUNT, 3, 3, 1000), 22_500_000_000_000_000_000n);
  assert.equal(winnerGain(AMOUNT, 2, 1, 1000), 11_250_000_000_000_000_000n);
  assert.equal(winnerGain(AMOUNT, 1, 3, 0), 75_000_000_000_000_000_000n);
  assert.equal(winnerGain(AMOUNT, 1, 0, 1000), 0n);
  assert.equal(winnerGain(AMOUNT, 0, 2, 1000), 0n);
});

test('a wallet reads what it won and lost on the stakes of its week, on the referee\'s decisions', async () => {
  const { api, key, state, store } = await stakedAndJudged(verdict());
  assert.equal((await call(api, 'GET', '/api/wager/week')).status, 401, 'signed in only');
  const bob = await sessionOf(api, BOB);
  const alice = await sessionOf(api, ALICE);

  // Bob was in the team that won: he gains Alice's stake less the burned tenth; the chain has not paid yet.
  const won = (await call(api, 'GET', '/api/wager/week', bob)).body;
  assert.equal(won.enabled, true);
  assert.equal(won.address, BOB.address);
  assert.deepEqual([won.matches, won.pending, won.unpaid], [1, 0, 1]);
  assert.deepEqual([won.won, won.lost, won.net], ['22500000000000000000', '0', '22500000000000000000']);
  assert.ok(Math.abs(won.wonUsd - 0.03375) < 1e-9);
  assert.equal(won.entries[0].outcome, 'won');
  assert.equal(won.entries[0].gameId, 'game');
  assert.equal(won.since, new Date(state.clock - WEEK_MS).toISOString());

  const lost = (await call(api, 'GET', '/api/wager/week', alice)).body;
  assert.deepEqual([lost.won, lost.lost, lost.net], ['0', '25000000000000000000', '-25000000000000000000']);
  assert.ok(Math.abs(lost.netUsd + 0.0375) < 1e-9);

  // Once the contract paid, the match is no longer unpaid (read again after the short time).
  state.paid.add(key);
  state.clock += 31_000;
  assert.equal((await call(api, 'GET', '/api/wager/week', bob)).body.unpaid, 0);

  // A stranger who never staked has an empty week; a week older than seven days is not counted.
  const nobody = (await call(api, 'GET', '/api/wager/week', await sessionOf(api, createWallet()))).body;
  assert.deepEqual([nobody.matches, nobody.won, nobody.lost], [0, '0', '0']);
  state.clock += WEEK_MS;
  assert.equal((await call(api, 'GET', '/api/wager/week', await sessionOf(api, BOB))).body.matches, 0, 'a week later (a new session: the old one ran out)');
  assert.equal((await store.list('staked-' + BOB.address + '/')).length, 1, 'the note of the match stays');
});

test('a match that is not judged yet counts as pending, and one called off as given back', async () => {
  const { api, matches, state } = services();
  await matchOfTwo(matches);
  const alice = await sessionOf(api, ALICE);
  const bob = await sessionOf(api, BOB);
  const key = matchKeyOf('game');
  await call(api, 'POST', '/api/wager/ticket', alice, { team: 0, amount: AMOUNT });
  await call(api, 'POST', '/api/wager/ticket', bob, { team: 1, amount: AMOUNT });
  state.stakes.set(key, [{ address: ALICE.address, team: 0 }, { address: BOB.address, team: 1 }]);
  assert.equal((await call(api, 'POST', '/api/wager/lock', alice)).body.locked, true);
  const pending = (await call(api, 'GET', '/api/wager/week', alice)).body;
  assert.deepEqual([pending.matches, pending.pending, pending.won, pending.lost], [1, 1, '0', '0']);

  await matches.setVerdict('game', { void: true, why: 'the match did not reach its end' });
  state.clock += 31_000;
  const off = (await call(api, 'GET', '/api/wager/week', alice)).body;
  assert.deepEqual([off.pending, off.won, off.lost, off.entries[0].outcome], [0, '0', '0', 'called_off']);

  // Without a store nothing is noted, and the week is not answered.
  const plain = createWager({ chain: { callMany: async () => [] }, matches, network: wagerNetwork(), referee: createReferee({ privateKey: REFEREE_KEY, chainId: 143, wager: WAGER }) });
  assert.deepEqual(await plain.weekOf({ address: ALICE.address }), { enabled: false });
});

test('version 2 signs the wallets that played a Genesis with the result, as Solidity encodes them', async () => {
  // abi.encode(RESULT, 143, wager, keccak256("eu:test"), 1, [0x11..11, 0x22..22]), hashed by cast on 22 October 2026.
  const matchKey = matchKeyOf('eu:test');
  const genesisPlayers = ['0x' + '11'.repeat(20), '0x' + '22'.repeat(20)];
  assert.equal(resultDigest({ chainId: 143, wager: WAGER, matchKey, winnerTeam: 1, genesisPlayers }),
    '0xff142f94f8dbc90360373c33f68a3c62df7bc94ef60be9c458922e142cf28248');
  assert.notEqual(resultDigest({ chainId: 143, wager: WAGER, matchKey, winnerTeam: 1, genesisPlayers: [] }),
    resultDigest({ chainId: 143, wager: WAGER, matchKey, winnerTeam: 1 }), 'an empty list is still a list for version 2');
  assert.throws(() => resultDigest({ chainId: 143, wager: WAGER, matchKey, winnerTeam: 1, genesisPlayers: Array(7).fill(genesisPlayers[0]) }), /at most six/);

  const { api, result, key, referee } = await stakedAndJudged(verdict(), { genesisShare: 500 });
  const described = (await call(api, 'GET', '/api/wager')).body;
  assert.deepEqual([described.version, described.burnedShare, described.genesisShare], [2, 500, 500]);
  // Alice played the Genesis #1795, Bob the recruit: only Alice is paid the share.
  assert.deepEqual(result.genesisPlayers, [ALICE.address.toLowerCase()]);
  assert.equal(signerOf(resultDigest({ chainId: 143, wager: WAGER, matchKey: key, winnerTeam: 1, genesisPlayers: result.genesisPlayers }), result.signature), referee.address);
});

test('the Genesis players of a match are the wallets the judge saw with their Genesis, a stand-in excepted', () => {
  const match = {
    verdict: { players: [{ player: 2, team: 1, tokenId: 9 }, { player: 0, team: 0, tokenId: 1795 }, { player: 1, team: 0, tokenId: 0 }, { player: 3, team: 1, tokenId: 44 }, { player: 4, team: 1, tokenId: 12 }] },
    admissions: [
      { player: 0, address: '0xAA' + 'a'.repeat(38), tokenId: 1795 },
      { player: 1, address: '0x' + 'b'.repeat(40), tokenId: 0 },
      { player: 2, address: '0x' + 'c'.repeat(40), tokenId: 9, standIn: true },
      { player: 3, address: null, tokenId: 44 },
      { player: 4, address: '0x' + 'e'.repeat(40), tokenId: 13 },
    ],
  };
  assert.deepEqual(genesisPlayersOf(match), ['0x' + 'a'.repeat(40)], 'the judged Genesis, a signed-in wallet, not a stand-in, the same Genesis it was let in with');
});

test('a stake signed once is brought by the services with the referee key, its calldata exactly as the contract reads it', async () => {
  const { createWager } = await import('../src/wager.js');
  const sent = [];
  const contract = '0x' + 'c2'.repeat(20);
  const token = '0x' + '35'.repeat(20);
  const referee = { address: '0x' + 'aa'.repeat(20), wager: contract, chainId: 143, signTicket: () => '0x' };
  // referee(), burnedShare(), genesisShare() all answer one word; the code holds the selector of stakeWithPermit.
  const chain = {
    rpc: async () => '0x6080' + '471070ff' + '00',
    callMany: async calls => calls.map(() => '0x' + '0'.repeat(24) + 'aa'.repeat(20)),
  };
  const wager = createWager({ chain, referee, matches: {}, network: { token, chainId: 143, chainName: 'Monad' }, store: {},
    sender: { transact: async request => { sent.push(request); return { hash: '0x' + '11'.repeat(32), ok: true }; } } });
  const player = '0x' + 'ab'.repeat(20);
  const request = {
    address: player, matchKey: '0x' + '01'.repeat(32), team: 1, amount: '100000000000000000000', deadline: 1792700000,
    signature: '0x' + 'cd'.repeat(65), permitDeadline: 1792700600, v: 27, r: '0x' + '02'.repeat(32), s: '0x' + '03'.repeat(32),
  };
  const answer = await wager.relay(request);
  assert.deepEqual(answer, { hash: '0x' + '11'.repeat(32), done: true });
  const words = sent[0].data.slice(10).match(/.{64}/g);
  assert.equal(sent[0].data.slice(0, 10), '0x471070ff');
  assert.equal(words[0], '0'.repeat(24) + 'ab'.repeat(20), 'the player');
  assert.equal(BigInt('0x' + words[5]), 320n, 'the ticket after the ten head words');
  assert.equal(BigInt('0x' + words[10]), 65n, 'a 65 byte ticket');
  assert.equal(words.length, 14);
  assert.equal((await wager.relay({ address: player, matchKey: 'nope' })).refused, 'bad_request');
  assert.equal(sent.length, 1);
});
