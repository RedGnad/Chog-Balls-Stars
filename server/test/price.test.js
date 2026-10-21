import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createApi } from '../src/api.js';
import { createPrice, PRICE_KEPT_MS, PRICE_SOURCES, PRICE_STALE_MS } from '../src/price.js';
import { CHOG_TOKEN } from '../src/shop.js';

const TOKEN = CHOG_TOKEN.toLowerCase();

// What each source answered on 21 October 2026, shortened.
const ANSWERS = {
  coingecko: { [TOKEN]: { usd: 0.00150916 } },
  geckoterminal: { data: { id: 'monad_' + TOKEN, attributes: { address: TOKEN, price_usd: '0.001507730107' } } },
  dexscreener: {
    pairs: [
      { chainId: 'monad', baseToken: { address: CHOG_TOKEN }, priceUsd: '0.0019', liquidity: { usd: 1200 } },
      { chainId: 'monad', baseToken: { address: CHOG_TOKEN }, priceUsd: '0.001522', liquidity: { usd: 96259.53 } },
      { chainId: 'monad', baseToken: { address: '0x' + '11'.repeat(20) }, priceUsd: '5', liquidity: { usd: 1e9 } },
    ],
  },
};

// A web where some sources are down: `down` names them; `asked` counts the requests to each.
function web({ down = [] } = {}) {
  const asked = { coingecko: 0, geckoterminal: 0, dexscreener: 0 };
  const fetch = async url => {
    const name = url.includes('coingecko.com') ? 'coingecko' : url.includes('geckoterminal') ? 'geckoterminal' : 'dexscreener';
    asked[name]++;
    if (down.includes(name)) return new Response('busy', { status: 429 });
    return new Response(JSON.stringify(ANSWERS[name]), { status: 200, headers: { 'Content-Type': 'application/json' } });
  };
  return { asked, fetch, down };
}

test('each source reads the price of $CHOG out of what it answers', () => {
  const [coingecko, geckoterminal, dexscreener] = PRICE_SOURCES;
  assert.equal(coingecko.read(ANSWERS.coingecko, CHOG_TOKEN), 0.00150916);
  assert.equal(geckoterminal.read(ANSWERS.geckoterminal, CHOG_TOKEN), 0.001507730107);
  assert.equal(dexscreener.read(ANSWERS.dexscreener, CHOG_TOKEN), 0.001522, 'the deepest pool of the token, not another token');
  assert.equal(coingecko.read({}, CHOG_TOKEN), null);
  assert.equal(geckoterminal.read({ data: { attributes: { price_usd: 'n/a' } } }, CHOG_TOKEN), null);
  assert.equal(dexscreener.read({ pairs: [] }, CHOG_TOKEN), null);
  for (const source of PRICE_SOURCES) assert.ok(source.url(CHOG_TOKEN).includes(CHOG_TOKEN));
});

test('the first source that answers gives the price, which is kept for a few minutes', async () => {
  const { asked, fetch } = web();
  let clock = 1_790_000_000_000;
  const price = createPrice({ fetch, now: () => clock });
  const first = await price.describe();
  assert.deepEqual(first, { token: TOKEN, usd: 0.00150916, source: 'coingecko', readAt: new Date(clock).toISOString(), stale: false });
  assert.deepEqual(asked, { coingecko: 1, geckoterminal: 0, dexscreener: 0 });

  clock += PRICE_KEPT_MS - 1000;
  assert.equal((await price.describe()).readAt, first.readAt);
  assert.equal(asked.coingecko, 1, 'answered from what was read');
  clock += 2000;
  assert.equal((await price.describe()).readAt, new Date(clock).toISOString());
  assert.equal(asked.coingecko, 2, 'read again after the time');
  assert.equal(price.lastUsd(), 0.00150916);
});

test('a source that is down is passed over, and the last price read is answered while every source is down', async () => {
  const made = web({ down: ['coingecko'] });
  let clock = 1_790_000_000_000;
  const price = createPrice({ fetch: made.fetch, now: () => clock });
  const read = await price.describe();
  assert.equal(read.source, 'geckoterminal');
  assert.equal(read.usd, 0.001507730107);

  made.down.push('geckoterminal', 'dexscreener');
  clock += PRICE_KEPT_MS + 1;
  const kept = await price.describe();
  assert.equal(kept.usd, 0.001507730107, 'the last price read');
  assert.equal(kept.stale, false);
  assert.match(kept.problem, /coingecko: .*429.*geckoterminal.*dexscreener/);
  const before = { ...made.asked };
  await price.describe();
  assert.deepEqual(made.asked, before, 'a failure is not retried on every page');

  clock += PRICE_STALE_MS + 1;
  assert.equal((await price.describe()).stale, true, 'called stale once it is old');
});

test('without any source answering yet, the page is told there is no price, and nothing throws', async () => {
  const made = web({ down: ['coingecko', 'geckoterminal', 'dexscreener'] });
  const price = createPrice({ fetch: made.fetch, now: () => 1_790_000_000_000 });
  const none = await price.describe();
  assert.equal(none.usd, null);
  assert.equal(none.stale, true);
  assert.equal(await price.usdOfWei('1000000000000000000000'), null);
  const thrown = createPrice({ fetch: async () => { throw new Error('offline'); } });
  assert.equal((await thrown.describe()).usd, null);
});

test('an amount in the smallest unit is turned into dollars', async () => {
  const price = createPrice({ fetch: web().fetch, now: () => 1_790_000_000_000 });
  assert.ok(Math.abs(await price.usdOfWei('1000000000000000000000') - 1.50916) < 1e-9, 'a thousand $CHOG');
  assert.ok(Math.abs(await price.usdOfWei('2500000000000000000') - 0.0037729) < 1e-9, 'two and a half');
  assert.equal(await price.usdOfWei('not digits'), null);
});

test('over HTTP: the price is public, and answered without a price when no source is up', async () => {
  const api = createApi({ price: createPrice({ fetch: web().fetch, now: () => 1_790_000_000_000 }) });
  const answer = await api(new Request('http://game.example/api/price'));
  assert.equal(answer.status, 200);
  const body = await answer.json();
  assert.equal(body.usd, 0.00150916);
  assert.equal(body.source, 'coingecko');

  const dead = createApi({ price: createPrice({ fetch: async () => { throw new Error('offline'); } }) });
  const none = await (await dead(new Request('http://game.example/api/price'))).json();
  assert.equal(none.usd, null);
  assert.equal(none.token, TOKEN);

  const plain = createApi({});
  assert.deepEqual(await (await plain(new Request('http://game.example/api/price'))).json(), { token: TOKEN, usd: null, source: null, readAt: null, stale: true });
});
