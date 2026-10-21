import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createApi } from '../src/api.js';
import { createChain, decodeOwnerScan, encodeOwnerScan, GENESIS, LAST_TOKEN_ID, MULTICALL3 } from '../src/chain.js';

const word = value => BigInt(value).toString(16).padStart(64, '0');
const ALICE = '0x' + 'a1'.repeat(20);
const BOB = '0x' + 'b2'.repeat(20);

// What Multicall3.aggregate3 returns for a list of owners; null stands for a failed ownerOf.
function encodeScanResult(owners) {
  let offsets = '';
  let elements = '';
  let position = owners.length * 32;
  for (const owner of owners) {
    offsets += word(position);
    if (owner) {
      elements += word(1) + word(0x40) + word(32) + word(owner);
      position += 128;
    } else {
      elements += word(0) + word(0x40) + word(0);
      position += 96;
    }
  }
  return '0x' + word(0x20) + word(owners.length) + offsets + elements;
}

// A chain where Alice holds tokens 2 and 5 and Bob the rest, and where token 7 can change hands.
function fakeChain({ ownerOfSeven = BOB, failFirstEndpoint = false } = {}) {
  const state = { ownerOfSeven, calls: [], block: 100 };
  const fetch = async (url, init) => {
    const { method, params } = JSON.parse(init.body);
    state.calls.push({ url, method });
    if (failFirstEndpoint && url === 'https://first.example') return new Response('busy', { status: 429 });
    let result;
    if (method === 'eth_blockNumber') {
      result = '0x' + (state.block++).toString(16);
    } else if (params[0].to === GENESIS) {
      // ownerOf of one token, asked directly.
      assert.equal(params[0].data.slice(0, 10), '0x6352211e');
      const tokenId = parseInt(params[0].data.slice(10), 16);
      state.ownerReads = (state.ownerReads ?? 0) + 1;
      result = '0x' + (tokenId === 2 || tokenId === 5 ? ALICE : tokenId === 7 ? state.ownerOfSeven : BOB).slice(2).padStart(64, '0');
    } else {
      assert.equal(params[0].to, MULTICALL3);
      const owners = Array.from({ length: LAST_TOKEN_ID }, (_, index) =>
        index + 1 === 2 || index + 1 === 5 ? ALICE : index + 1 === 7 ? state.ownerOfSeven : BOB);
      result = encodeScanResult(owners);
    }
    return new Response(JSON.stringify({ jsonrpc: '2.0', id: 1, result }), { status: 200 });
  };
  return { state, fetch };
}

test('the owner scan asks ownerOf for every token of the Genesis contract', () => {
  const data = encodeOwnerScan(3);
  assert.equal(data.slice(0, 10), '0x82ad56cb');
  const body = data.slice(10);
  assert.equal(parseInt(body.slice(64, 128), 16), 3);
  // Three offsets, then elements of six words each.
  const first = body.slice(128 + 3 * 64, 128 + 3 * 64 + 6 * 64);
  assert.equal('0x' + first.slice(24, 64), GENESIS);
  assert.equal(first.slice(256, 264), '6352211e');
  assert.equal(parseInt(first.slice(264, 328), 16), 1);
  assert.equal(body.length, 64 * (2 + 3 + 3 * 6));
});

test('a scan result decodes to one owner per token, and a failed ownerOf to null', () => {
  const owners = decodeOwnerScan(encodeScanResult([ALICE, null, BOB]));
  assert.deepEqual([...owners], [[1, ALICE], [2, null], [3, BOB]]);
});

test('the tokens of an address are found whatever the letter case, and nobody else gets them', async () => {
  const { fetch } = fakeChain();
  const chain = createChain({ rpcUrls: ['https://first.example'], fetch });
  assert.deepEqual((await chain.ownedBy(ALICE.toUpperCase().replace('0X', '0x'))).tokens, [2, 5]);
  assert.equal((await chain.ownedBy(BOB)).tokens.length, LAST_TOKEN_ID - 2);
  assert.deepEqual((await chain.ownedBy('0x' + 'c3'.repeat(20))).tokens, []);
});

test('owners are reused for a short time, and read again for anything that credits', async () => {
  const fake = fakeChain();
  let clock = 1000;
  const chain = createChain({ rpcUrls: ['https://first.example'], fetch: fake.fetch, now: () => clock, maxAgeMs: 5000 });
  await chain.ownedBy(ALICE);
  await chain.ownedBy(BOB);
  assert.equal(fake.state.calls.length, 2, 'one block number and one scan for both reads');

  // The token is sold: the cached answer is stale, the check before a credit is not.
  fake.state.ownerOfSeven = ALICE;
  assert.deepEqual((await chain.ownedBy(ALICE)).tokens, [2, 5]);
  assert.equal((await chain.holds(ALICE, 7)).holds, true);
  assert.equal((await chain.holds(BOB, 7)).holds, false);

  clock += 6000;
  assert.deepEqual((await chain.ownedBy(ALICE)).tokens, [2, 5, 7]);
});

test('the check before a credit reads that token alone, and checks made together share one read', async () => {
  const fake = fakeChain();
  let clock = 1000;
  const chain = createChain({ rpcUrls: ['https://first.example'], fetch: fake.fetch, now: () => clock });
  const together = await Promise.all([chain.holds(BOB, 7), chain.holds(ALICE, 7), chain.holds(BOB, 7)]);
  assert.deepEqual(together.map(answer => answer.holds), [true, false, true]);
  assert.deepEqual(fake.state.calls.map(call => call.method), ['eth_call'], 'one read, and no scan of the collection');

  // Another token is another read; the same token two seconds later is read again.
  assert.equal((await chain.holds(ALICE, 5)).holds, true);
  fake.state.ownerOfSeven = ALICE;
  clock += 1999;
  assert.equal((await chain.holds(ALICE, 7)).holds, false, 'still the shared read');
  clock += 1;
  assert.equal((await chain.holds(ALICE, 7)).holds, true);
  assert.equal(fake.state.ownerReads, 3);

  // A token id that cannot exist is held by nobody, without a read.
  for (const tokenId of [0, 1970, 1.5, '7', null]) assert.equal((await chain.holds(BOB, tokenId)).holds, false);
  assert.equal(fake.state.ownerReads, 3);
});

test('a failed ownership read is not kept: the next check asks again', async () => {
  const fake = fakeChain();
  let down = true;
  const fetch = async (url, init) => { if (down) throw new Error('offline'); return fake.fetch(url, init); };
  const chain = createChain({ rpcUrls: ['https://first.example'], fetch, now: () => 1000 });
  await assert.rejects(chain.holds(BOB, 7));
  down = false;
  assert.equal((await chain.holds(BOB, 7)).holds, true);
});

test('a failing endpoint is skipped for the next one', async () => {
  const fake = fakeChain({ failFirstEndpoint: true });
  const chain = createChain({ rpcUrls: ['https://first.example', 'https://second.example'], fetch: fake.fetch });
  assert.deepEqual((await chain.ownedBy(ALICE)).tokens, [2, 5]);
  assert.ok(fake.state.calls.some(call => call.url === 'https://second.example'));
});

test('the owned endpoint answers the tokens, refuses a malformed address, and reports a dead chain', async () => {
  const api = createApi({ chain: createChain({ rpcUrls: ['https://first.example'], fetch: fakeChain().fetch }) });
  const owned = await api(new Request(`http://game.example/api/genesis/owned?address=${ALICE}`));
  assert.equal(owned.status, 200);
  const body = await owned.json();
  assert.deepEqual(body.tokens, [2, 5]);
  assert.equal(body.address, ALICE);
  assert.equal(body.chainId, 143);

  const health = await (await api(new Request('http://game.example/api/health'))).json();
  assert.deepEqual([health.ok, health.chainId, health.lastOwnerReadBlock], [true, 143, 100]);
  assert.equal(health.rankStorage, 'not_configured');
  assert.equal(health.rankEnabled, false);
  const connected = createApi({ chain: createChain({ rpcUrls: ['https://first.example'], fetch: fakeChain().fetch }), rankStorageStatus: 'connected' });
  assert.equal((await (await connected(new Request('http://game.example/api/health'))).json()).rankStorage, 'connected');

  assert.equal((await api(new Request('http://game.example/api/genesis/owned?address=0x1234'))).status, 400);
  assert.equal((await api(new Request('http://game.example/api/genesis/owned'))).status, 400);
  assert.equal((await api(new Request('http://game.example/api/unknown'))).status, 404);

  const dead = createApi({ chain: createChain({ rpcUrls: ['https://first.example'], fetch: async () => { throw new Error('offline'); } }) });
  const failed = await dead(new Request(`http://game.example/api/genesis/owned?address=${ALICE}`));
  assert.equal(failed.status, 502);
  assert.equal((await failed.json()).error, 'upstream_failed');
  // The health answer does not depend on the chain: a host must not restart us because an RPC is down.
  const alive = await dead(new Request('http://game.example/api/health'));
  assert.equal(alive.status, 200);
  assert.equal((await alive.json()).lastOwnerReadBlock, null);
});
