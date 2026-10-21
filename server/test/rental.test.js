import assert from 'node:assert/strict';
import { test } from 'node:test';
import { keccak_256 } from '@noble/hashes/sha3.js';
import { createApi } from '../src/api.js';
import { createCareers } from '../src/career.js';
import { createChain } from '../src/chain.js';
import { createRental, listOf, MAX_RENTAL_DAYS } from '../src/rental.js';
import { createSessions } from '../src/session.js';
import { CHOG_TOKEN } from '../src/shop.js';
import { createWallet } from './wallet.js';

const RENTAL = '0x' + '7e'.repeat(20);
const HOLDER = '0x' + 'a1'.repeat(20);
const OTHER_HOLDER = '0x' + 'b2'.repeat(20);
const SECRET = 'a test secret that is long enough to be accepted';
const word = value => BigInt(value).toString(16).padStart(64, '0');
const selector = signature => Array.from(keccak_256(new TextEncoder().encode(signature)).slice(0, 4), byte => byte.toString(16).padStart(2, '0')).join('');

// Packs lists of words the way a contract answers several lists.
function pack(...lists) {
  let heads = '';
  let tails = '';
  let offset = lists.length * 32;
  for (const list of lists) {
    heads += word(offset);
    tails += word(list.length) + list.map(word).join('');
    offset += 32 * (1 + list.length);
  }
  return '0x' + heads + tails;
}

// A chain with a rental contract that burns a tenth, where two Genesis are offered and where
// the test decides who rents what until when. `clock` is in seconds, as on the chain.
function fakeChain() {
  const state = {
    requests: 0,
    clock: 1_790_000_000,
    offers: [
      { tokenId: 1795, pricePerDay: 10n * 10n ** 18n, lender: HOLDER },
      { tokenId: 8, pricePerDay: 25n * 10n ** 17n, lender: OTHER_HOLDER },
    ],
    // "tokenId:renter" to the second the rental ends.
    rented: new Map(),
  };
  const fetch = async (url, init) => {
    const { method, params } = JSON.parse(init.body);
    assert.equal(method, 'eth_call');
    assert.equal(params[0].to, RENTAL);
    state.requests++;
    const data = params[0].data.slice(2);
    const body = data.slice(8);
    let result;
    switch (data.slice(0, 8)) {
      case selector('burnedShare()'):
        result = '0x' + word(1000);
        break;
      case selector('standingOffers()'):
        result = pack(state.offers.map(offer => offer.tokenId), state.offers.map(offer => offer.pricePerDay), state.offers.map(offer => offer.lender));
        break;
      case selector('rentalsOf(address)'): {
        const renter = '0x' + body.slice(24, 64);
        const running = [...state.rented].filter(([key, until]) => key.endsWith(':' + renter) && until > state.clock);
        result = pack(running.map(([key]) => Number(key.split(':')[0])), running.map(([, until]) => until));
        break;
      }
      case selector('isRenting(uint256,address)'): {
        const until = state.rented.get(parseInt(body.slice(0, 64), 16) + ':0x' + body.slice(88, 128)) ?? 0;
        result = '0x' + word(until > state.clock ? 1 : 0);
        break;
      }
      default:
        assert.fail('unexpected call ' + data.slice(0, 8));
    }
    return new Response(JSON.stringify({ jsonrpc: '2.0', id: 1, result }), { status: 200 });
  };
  return { state, fetch };
}

test('lists a contract answered are read by rank, and a cut answer is refused', () => {
  const answer = pack([7, 9], [], [3]);
  assert.deepEqual(listOf(answer, 0), [word(7), word(9)]);
  assert.deepEqual(listOf(answer, 1), []);
  assert.deepEqual(listOf(answer, 2), [word(3)]);
  assert.equal(listOf(answer.slice(0, answer.length - 10), 2), null);
  assert.equal(listOf('0x', 0), null);
  assert.equal(listOf(undefined, 0), null);
});

test('without a rental address nothing is offered or rented and the chain is not read', async () => {
  const { fetch, state } = fakeChain();
  const rental = createRental({ chain: createChain({ rpcUrls: ['https://first.example'], fetch }) });
  assert.deepEqual(await rental.describe(), { enabled: false });
  assert.deepEqual(await rental.offers(), []);
  assert.deepEqual(await rental.rentedBy(HOLDER), []);
  assert.equal(await rental.isRenting(1795, HOLDER), false);
  assert.equal(state.requests, 0);
  assert.throws(() => createRental({ chain: {}, address: 'not an address' }));
});

test('the rental says its terms, and lists the offers read on the chain for a short time', async () => {
  const { fetch, state } = fakeChain();
  let clock = 1000;
  const rental = createRental({
    chain: createChain({ rpcUrls: ['https://first.example'], fetch }), address: RENTAL.toUpperCase().replace('0X', '0x'), now: () => clock,
  });
  const terms = { enabled: true, address: RENTAL, token: CHOG_TOKEN.toLowerCase(), maxDays: MAX_RENTAL_DAYS, burnedShare: 1000 };
  assert.deepEqual(await rental.describe(), terms);
  assert.deepEqual(await rental.describe(), terms);
  assert.equal(state.requests, 1, 'the burned share is fixed, so it is read once');

  const offers = [
    { tokenId: 8, pricePerDay: '2500000000000000000', lender: OTHER_HOLDER },
    { tokenId: 1795, pricePerDay: '10000000000000000000', lender: HOLDER },
  ];
  assert.deepEqual(await rental.offers(), offers, 'by token id');
  assert.deepEqual(await rental.offers(), offers);
  assert.equal(state.requests, 2, 'the second list came from the first read');

  state.offers.pop();
  clock += 11000;
  assert.deepEqual(await rental.offers(), [offers[1]], 'a withdrawn offer is gone after the short time');
});

test('the cheapest offers come first, with the dollars of a day when the price is known', async () => {
  const { fetch, state } = fakeChain();
  state.offers.push({ tokenId: 3, pricePerDay: 10n * 10n ** 18n, lender: HOLDER }, { tokenId: 1, pricePerDay: 2n * 10n ** 18n, lender: HOLDER });
  const price = { usdOfWei: async wei => Number(BigInt(wei) / 10n ** 12n) / 1e6 * 0.0015 };
  const rental = createRental({ chain: createChain({ rpcUrls: ['https://first.example'], fetch }), address: RENTAL, price });
  const cheapest = await rental.cheapest(3);
  assert.equal(cheapest.offered, 4);
  assert.deepEqual(cheapest.offers.map(offer => [offer.tokenId, offer.pricePerDay, offer.usdPerDay]), [
    [1, '2000000000000000000', 0.003],
    [8, '2500000000000000000', 0.00375],
    [3, '10000000000000000000', 0.015],
  ], 'by price a day, then token id');
  assert.equal((await rental.cheapest(1)).offers.length, 1);

  // Without a price the dollars are null; without a rental there is nothing.
  const plain = createRental({ chain: createChain({ rpcUrls: ['https://first.example'], fetch }), address: RENTAL });
  assert.equal((await plain.cheapest(3)).offers[0].usdPerDay, null);
  assert.deepEqual(await createRental({ chain: {} }).cheapest(3), { offers: [], offered: 0 });

  const api = createApi({ chain: {}, rental });
  const get = path => api(new Request('http://game.example' + path));
  assert.deepEqual((await (await get('/api/rental/cheapest')).json()).offers.map(offer => offer.tokenId), [1, 8, 3]);
  assert.deepEqual((await (await get('/api/rental/cheapest?limit=2')).json()).offers.map(offer => offer.tokenId), [1, 8]);
  assert.equal((await get('/api/rental/cheapest?limit=0')).status, 400);
  assert.deepEqual(await (await createApi({ chain: {} })(new Request('http://game.example/api/rental/cheapest'))).json(), { offers: [], offered: 0 });
});

test('what an address rents is read fresh, and a rental that ran out is not one', async () => {
  const { fetch, state } = fakeChain();
  const rental = createRental({ chain: createChain({ rpcUrls: ['https://first.example'], fetch }), address: RENTAL });
  const renter = '0x' + 'c3'.repeat(20);
  assert.deepEqual(await rental.rentedBy(renter), []);

  state.rented.set('1795:' + renter, state.clock + 86400);
  state.rented.set('8:' + renter, state.clock + 3 * 86400);
  assert.deepEqual(await rental.rentedBy(renter.toUpperCase().replace('0X', '0x')), [
    { tokenId: 8, until: state.clock + 3 * 86400 },
    { tokenId: 1795, until: state.clock + 86400 },
  ]);
  assert.equal(await rental.isRenting(1795, renter), true);
  assert.equal(await rental.isRenting(3, renter), false);
  assert.equal(await rental.isRenting(0, renter), false, 'an id that cannot exist, without a read');

  state.clock += 2 * 86400;
  assert.deepEqual(await rental.rentedBy(renter), [{ tokenId: 8, until: state.clock + 86400 }]);
  assert.equal(await rental.isRenting(1795, renter), false);
});

// The services over HTTP with a holder of the Genesis 1795, a renter who holds nothing, a
// clock for the careers and the rental chain above.
function services({ withRental = true } = {}) {
  const world = { clock: Date.UTC(2026, 9, 10, 12), saved: null };
  const holder = createWallet();
  const renter = createWallet();
  const { fetch, state } = fakeChain();
  const chain = {
    ...createChain({ rpcUrls: ['https://first.example'], fetch }),
    holds: async (address, tokenId) => ({ holds: address.toLowerCase() === holder.address && tokenId === 1795 }),
  };
  const now = () => world.clock;
  const api = createApi({
    chain,
    rental: withRental ? createRental({ chain, address: RENTAL, now }) : undefined,
    careers: createCareers({
      load: async () => (world.saved ? JSON.parse(world.saved) : null),
      save: async saved => { world.saved = JSON.stringify(saved); },
      now,
    }),
    sessions: createSessions({ secret: SECRET, now }),
    publicOrigin: 'https://game.example',
  });
  const call = (method, path, { body, session } = {}) => api(new Request('http://127.0.0.1:8090' + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(session ? { Authorization: 'Bearer ' + session } : {}) },
    body: body ? JSON.stringify(body) : undefined,
  }));
  const signIn = async wallet => {
    const challenge = await (await call('POST', '/api/session/challenge', { body: { address: wallet.address } })).json();
    const answer = await call('POST', '/api/session', { body: { ...challenge, signature: wallet.personalSign(challenge.message) } });
    return (await answer.json()).session;
  };
  return { world, state, holder, renter, call, signIn };
}

test('over HTTP: terms, offers and what an address rents are public reads', async () => {
  const { state, renter, call } = services();
  assert.equal((await (await call('GET', '/api/rental')).json()).address, RENTAL);
  assert.deepEqual((await (await call('GET', '/api/rental/offers')).json()).offers.map(offer => offer.tokenId), [8, 1795]);

  state.rented.set('1795:' + renter.address, state.clock + 86400);
  assert.deepEqual(await (await call('GET', '/api/rental/rented?address=' + renter.address)).json(), {
    address: renter.address, rentals: [{ tokenId: 1795, until: state.clock + 86400 }],
  });
  assert.equal((await call('GET', '/api/rental/rented')).status, 400);
  assert.equal((await call('GET', '/api/rental/rented?address=0x12')).status, 400);

  // Services started without a rental answer the same questions with "nothing".
  const plain = services({ withRental: false });
  assert.deepEqual(await (await plain.call('GET', '/api/rental')).json(), { enabled: false });
  assert.deepEqual(await (await plain.call('GET', '/api/rental/offers')).json(), { offers: [] });
  assert.deepEqual((await (await plain.call('GET', '/api/rental/rented?address=' + renter.address)).json()).rentals, []);
});

test('a renter\'s match is credited to the Genesis while the rental runs, and never after', async () => {
  const { world, state, holder, renter, call, signIn } = services();
  const session = await signIn(renter);
  const result = matchId => call('POST', '/api/career/result', { session, body: { tokenId: 1795, matchId, team: 0, winnerTeam: 0 } });

  const before = await result('match-before-renting');
  assert.deepEqual([before.status, (await before.json()).error], [403, 'not_the_holder']);

  state.rented.set('1795:' + renter.address, state.clock + 86400);
  const credited = await (await result('match-while-renting')).json();
  assert.equal(credited.credited, true);
  assert.deepEqual([credited.career.matches, credited.career.wins, credited.career.rentedMatches, credited.career.guestMatches], [1, 1, 1, 0]);

  // The holder reports the same match for the same Genesis: it was already credited.
  const own = await (await call('POST', '/api/career/result', {
    session: await signIn(holder), body: { tokenId: 1795, matchId: 'match-while-renting', team: 0, winnerTeam: 0 },
  })).json();
  assert.equal(own.reason, 'already_recorded', 'one credit a match for a Genesis, whoever played it');

  // Another Genesis, rented by nobody: refused.
  const other = await call('POST', '/api/career/result', { session, body: { tokenId: 8, matchId: 'match-on-another', team: 0, winnerTeam: 0 } });
  assert.equal(other.status, 403);

  world.clock += 5 * 60 * 1000;
  state.clock += 2 * 86400;
  const after = await result('match-after-the-rental');
  assert.deepEqual([after.status, (await after.json()).error], [403, 'not_the_holder']);
  assert.equal((await (await call('GET', '/api/career?tokens=1795')).json()).careers[0].matches, 1);
});

test('without a rental contract, an address that does not hold the Genesis is credited nothing', async () => {
  const { state, renter, call, signIn } = services({ withRental: false });
  state.rented.set('1795:' + renter.address, state.clock + 86400);
  const answer = await call('POST', '/api/career/result', {
    session: await signIn(renter), body: { tokenId: 1795, matchId: 'match-no-rental', team: 0, winnerTeam: 0 },
  });
  assert.equal(answer.status, 403);
  assert.equal(state.requests, 0, 'the chain was not asked about a rental');
});
