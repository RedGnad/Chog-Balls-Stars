import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createApi } from '../src/api.js';
import { createChain, decodeAggregate3, encodeAggregate3, MULTICALL3 } from '../src/chain.js';
import { CHOG_TOKEN, createShop, SECOND_FORM_ITEM } from '../src/shop.js';

const SHOP = '0x' + '5a'.repeat(20);
const word = value => BigInt(value).toString(16).padStart(64, '0');

// A chain with a shop that sells item 1 for 500 $CHOG and where the Genesis 3 and 1795 have it.
function fakeChain({ price = 500n * 10n ** 18n, forSale = true, owners = [3, 1795] } = {}) {
  const state = { requests: 0, price, forSale };
  const answerOf = data => {
    const body = data.slice(10);
    // items(uint256) answers (price, forSale); owned(uint256,uint256) answers a boolean.
    if (body.length === 64) return '0x' + word(state.price) + word(state.forSale ? 1 : 0);
    const tokenId = parseInt(body.slice(0, 64), 16);
    const itemId = parseInt(body.slice(64, 128), 16);
    return '0x' + word(itemId === SECOND_FORM_ITEM && owners.includes(tokenId) ? 1 : 0);
  };
  const fetch = async (url, init) => {
    const { method, params } = JSON.parse(init.body);
    assert.equal(method, 'eth_call');
    state.requests++;
    let result;
    if (params[0].to.toLowerCase() === MULTICALL3.toLowerCase()) {
      // Unpack the calls of the batch, answer each, pack the answers the way Multicall3 does.
      const raw = params[0].data.slice(10);
      const count = parseInt(raw.slice(64, 128), 16);
      const answers = [];
      for (let index = 0; index < count; index++) {
        const offset = parseInt(raw.slice(128 + index * 64, 128 + (index + 1) * 64), 16) * 2;
        const element = raw.slice(128 + offset);
        assert.equal('0x' + element.slice(24, 64), SHOP);
        const length = parseInt(element.slice(192, 256), 16) * 2;
        answers.push(answerOf('0x' + element.slice(256, 256 + length)));
      }
      let offsets = '';
      let elements = '';
      let offset = answers.length * 32;
      for (const answer of answers) {
        const bytes = answer.slice(2);
        offsets += word(offset);
        elements += word(1) + word(0x40) + word(bytes.length / 2) + bytes;
        offset += 32 * 3 + bytes.length / 2;
      }
      result = '0x' + word(0x20) + word(answers.length) + offsets + elements;
    } else {
      assert.equal(params[0].to, SHOP);
      result = answerOf(params[0].data);
    }
    return new Response(JSON.stringify({ jsonrpc: '2.0', id: 1, result }), { status: 200 });
  };
  return { state, fetch };
}

test('several calls are packed for Multicall3 and their answers unpacked in order', () => {
  const data = encodeAggregate3([{ to: SHOP, data: '0x12345678' + word(7) }, { to: SHOP, data: '0xabcdef01' }]);
  assert.equal(data.slice(0, 10), '0x82ad56cb');
  const body = data.slice(10);
  assert.equal(parseInt(body.slice(64, 128), 16), 2, 'two calls');
  // The first element starts after the two offsets: target, may fail, where the bytes are, their length.
  const first = body.slice(128 + 2 * 64);
  assert.equal('0x' + first.slice(24, 64), SHOP);
  assert.equal(parseInt(first.slice(64, 128), 16), 1);
  assert.equal(parseInt(first.slice(192, 256), 16), 36);
  assert.equal(first.slice(256, 264), '12345678');

  const packed = '0x' + word(0x20) + word(2) + word(64) + word(64 + 128)
    + word(1) + word(0x40) + word(32) + word(5)
    + word(0) + word(0x40) + word(0);
  assert.deepEqual(decodeAggregate3(packed), ['0x' + word(5), null]);
});

test('without a shop address nothing is sold and no Genesis owns anything', async () => {
  const { fetch, state } = fakeChain();
  const shop = createShop({ chain: createChain({ rpcUrls: ['https://first.example'], fetch }) });
  assert.deepEqual(await shop.describe(), { enabled: false });
  assert.deepEqual(await shop.ownedAmong([3, 1795]), []);
  assert.equal(state.requests, 0, 'the chain is not read');
  assert.throws(() => createShop({ chain: {}, address: 'not an address' }));
});

test('the shop says what it sells, read on the chain and kept for a short time', async () => {
  const { fetch, state } = fakeChain();
  let clock = 1000;
  const shop = createShop({ chain: createChain({ rpcUrls: ['https://first.example'], fetch }), address: SHOP.toUpperCase().replace('0X', '0x'), now: () => clock });
  const expected = { enabled: true, address: SHOP, token: CHOG_TOKEN.toLowerCase(), itemId: 1, price: '500000000000000000000' };
  assert.deepEqual(await shop.describe(), expected);
  assert.deepEqual(await shop.describe(), expected);
  assert.equal(state.requests, 1, 'the second answer came from the first read');

  // The owner of the shop changes the price or takes the item off sale: seen after the short time.
  state.price = 10n ** 18n;
  state.forSale = false;
  clock += 21000;
  assert.deepEqual(await shop.describe(), { ...expected, enabled: false, price: '1000000000000000000' });
});

test('which Genesis have the item is one request for the whole list', async () => {
  const { fetch, state } = fakeChain();
  const shop = createShop({ chain: createChain({ rpcUrls: ['https://first.example'], fetch }), address: SHOP });
  assert.deepEqual(await shop.ownedAmong([1, 3, 8, 1795, 1969]), [3, 1795]);
  assert.equal(state.requests, 1);
  assert.deepEqual(await shop.ownedAmong([0, 2000, 1.5]), [], 'ids that cannot exist are left out, without a read');
  assert.equal(state.requests, 1);
});

test('over HTTP: the shop and what is owned are public reads, and a dead chain is reported', async () => {
  const { fetch } = fakeChain();
  const chain = createChain({ rpcUrls: ['https://first.example'], fetch });
  const api = createApi({ chain, shop: createShop({ chain, address: SHOP }) });
  const get = path => api(new Request('http://game.example' + path));

  assert.equal((await (await get('/api/shop')).json()).price, '500000000000000000000');
  assert.deepEqual(await (await get('/api/shop/owned?tokens=3,8,1795')).json(), { tokens: [3, 1795] });
  assert.equal((await get('/api/shop/owned')).status, 400);
  assert.equal((await get('/api/shop/owned?tokens=0')).status, 400);

  // Services started without a shop answer the same questions with "nothing".
  const plain = createApi({ chain });
  assert.deepEqual(await (await plain(new Request('http://game.example/api/shop'))).json(), { enabled: false });
  assert.deepEqual(await (await plain(new Request('http://game.example/api/shop/owned?tokens=3'))).json(), { tokens: [] });

  const down = createChain({ rpcUrls: ['https://first.example'], fetch: async () => { throw new Error('offline'); } });
  const dead = createApi({ chain: down, shop: createShop({ chain: down, address: SHOP }) });
  assert.equal((await dead(new Request('http://game.example/api/shop'))).status, 502);
});
