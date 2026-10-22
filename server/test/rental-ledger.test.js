import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createApi } from '../src/api.js';
import { createRentalLedger, EARNED_TOPIC, RENTAL_DEPLOYED_AT_BLOCK, RENTED_TOPIC, WINDOW_BLOCKS } from '../src/rental-ledger.js';

const RENTAL = '0x' + '7e'.repeat(20);
const HOLDER = '0x' + 'a1'.repeat(20);
const RENTER = '0x' + 'c3'.repeat(20);
const word = value => '0x' + BigInt(value).toString(16).padStart(64, '0');

// A chain whose rental emitted these events at these blocks, and that serves windows of at most
// 10,000 blocks, as rpc2.monad.xyz does; `requests` lists the windows asked.
function fakeChain({ latest, events, maxWindow = WINDOW_BLOCKS, down = false }) {
  const requests = [];
  const state = { latest };
  const fetch = async (url, init) => {
    if (down) return new Response('busy', { status: 503 });
    const { method, params } = JSON.parse(init.body);
    assert.equal(init.headers['User-Agent'].startsWith('Mozilla/5.0'), true, 'rpc2 refuses other agents');
    if (method === 'eth_blockNumber') return Response.json({ jsonrpc: '2.0', id: 1, result: '0x' + state.latest.toString(16) });
    assert.equal(method, 'eth_getLogs');
    const [filter] = params;
    assert.equal(filter.address, RENTAL);
    assert.deepEqual(filter.topics, [[EARNED_TOPIC, RENTED_TOPIC]]);
    const from = Number(BigInt(filter.fromBlock));
    const to = Number(BigInt(filter.toBlock));
    requests.push([from, to]);
    if (to - from + 1 > maxWindow) return Response.json({ jsonrpc: '2.0', id: 1, error: { code: -32012, message: 'getLogs request exceeded max allowed range' } });
    const result = events.filter(event => event.block >= from && event.block <= to).map(event => (event.earned !== undefined
      ? { topics: [EARNED_TOPIC, word(event.tokenId), word(HOLDER)], data: word(event.earned), blockNumber: '0x' + event.block.toString(16) }
      : { topics: [RENTED_TOPIC, word(event.tokenId), word(RENTER), word(HOLDER)], data: word(3) + word(30n * 10n ** 18n).slice(2) + word(1_790_000_000).slice(2), blockNumber: '0x' + event.block.toString(16) }));
    return Response.json({ jsonrpc: '2.0', id: 1, result });
  };
  return { fetch, requests, state };
}

const EVENTS = [
  { block: 100_005, tokenId: 1795, rented: true },
  { block: 100_005, tokenId: 1795, earned: 0n },
  { block: 112_000, tokenId: 1795, earned: 10n * 10n ** 18n },
  { block: 125_000, tokenId: 8, rented: true },
  { block: 131_000, tokenId: 1795, earned: 20n * 10n ** 18n },
  { block: 131_000, tokenId: 8, earned: 25n * 10n ** 17n },
];

test('the events are read in windows of ten thousand blocks from the deployment block, and added up by Genesis', async () => {
  const chain = fakeChain({ latest: 131_500, events: EVENTS });
  const ledger = createRentalLedger({ address: RENTAL.toUpperCase().replace('0X', '0x'), fromBlock: 100_000, fetch: chain.fetch });
  assert.equal(ledger.caughtUp(), false);
  assert.deepEqual(ledger.earnedOf([1795]).tokens, [{ tokenId: 1795, earned: '0', rentals: 0 }], 'nothing read yet');

  await ledger.catchUp();
  assert.deepEqual(chain.requests, [[100_000, 109_999], [110_000, 119_999], [120_000, 129_999], [130_000, 131_500]]);
  assert.equal(ledger.caughtUp(), true);
  assert.deepEqual(ledger.earnedOf([1795, 8, 3]), {
    enabled: true, fromBlock: 100_000, upToBlock: 131_500, caughtUp: true,
    tokens: [
      { tokenId: 1795, earned: '30000000000000000000', rentals: 1 },
      { tokenId: 8, earned: '2500000000000000000', rentals: 1 },
      { tokenId: 3, earned: '0', rentals: 0 },
    ],
  });

  // Later: only the blocks since the last read, and a new event counts.
  EVENTS.push({ block: 131_600, tokenId: 3, earned: 1n });
  chain.requests.length = 0;
  chain.state.latest = 131_700;
  await ledger.catchUp();
  assert.deepEqual(chain.requests, [[131_501, 131_700]]);
  assert.deepEqual(ledger.earnedOf([3]).tokens, [{ tokenId: 3, earned: '1', rentals: 0 }]);
  EVENTS.pop();

  // Nothing new: the latest block is asked, no window is.
  chain.requests.length = 0;
  await ledger.catchUp();
  assert.deepEqual(chain.requests, []);
});

test('an endpoint that refuses the window leaves the figures where they are, and the read goes on later', async () => {
  const small = fakeChain({ latest: 131_500, events: EVENTS, maxWindow: 100 });
  const ledger = createRentalLedger({ address: RENTAL, fromBlock: 100_000, fetch: small.fetch });
  await assert.rejects(ledger.catchUp(), /max allowed range/);
  const answer = ledger.earnedOf([1795]);
  assert.equal(answer.upToBlock, 99_999);
  assert.equal(answer.caughtUp, false);

  const down = createRentalLedger({ address: RENTAL, fromBlock: 100_000, fetch: fakeChain({ latest: 1, events: [], down: true }).fetch });
  await assert.rejects(down.catchUp(), /HTTP 503/);
  assert.throws(() => createRentalLedger({ address: 'not an address' }), /address/);
  assert.equal(RENTAL_DEPLOYED_AT_BLOCK, 114126084);
});

test('the background read catches up, waits, and tries again after a failure', async () => {
  const chain = fakeChain({ latest: 131_500, events: EVENTS });
  const waits = [];
  let failures = 0;
  const flaky = async (url, init) => {
    if (JSON.parse(init.body).method === 'eth_blockNumber' && failures++ === 0) return new Response('busy', { status: 503 });
    return chain.fetch(url, init);
  };
  let stopIt;
  const ledger = createRentalLedger({
    address: RENTAL, fromBlock: 100_000, fetch: flaky,
    wait: async ms => { waits.push(ms); if (waits.length === 2) stopIt(); },
  });
  const quiet = console.warn;
  console.warn = () => {};
  try {
    stopIt = ledger.stop;
    ledger.start();
    while (waits.length < 2) await new Promise(resolve => setTimeout(resolve, 5));
  } finally {
    console.warn = quiet;
  }
  assert.deepEqual(waits, [15000, 60000], 'a short wait after the failure, a minute after a read');
  assert.equal(ledger.earnedOf([1795]).tokens[0].earned, '30000000000000000000');
  assert.equal(ledger.earnedOf([1795]).problem, undefined, 'the failure is forgotten once a read went through');
});

test('over HTTP: what Genesis earned is public, and answered as not read without a ledger', async () => {
  const chain = fakeChain({ latest: 131_500, events: EVENTS });
  const ledger = createRentalLedger({ address: RENTAL, fromBlock: 100_000, fetch: chain.fetch });
  await ledger.catchUp();
  const api = createApi({ ledger });
  const get = path => api(new Request('http://game.example' + path));
  const answer = await (await get('/api/rental/earned?tokens=1795,8')).json();
  assert.equal(answer.enabled, true);
  assert.deepEqual(answer.tokens.map(token => token.earned), ['30000000000000000000', '2500000000000000000']);
  assert.equal((await get('/api/rental/earned')).status, 400);
  assert.equal((await get('/api/rental/earned?tokens=0')).status, 400);
  assert.deepEqual(await (await createApi({})(new Request('http://game.example/api/rental/earned?tokens=1795'))).json(), { enabled: false });
});
