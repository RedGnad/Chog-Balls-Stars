// The price of $CHOG in US dollars, so the game can label every amount in both (the balance on
// the home screen, a stake, a rent a day, an item of the shop). Read from public price sources
// that answer for this token on Monad, each tried in turn, and kept for a few minutes: the
// game asks these services, never the sources, and a source that is down never fails a page.
// The last price read is answered until a source answers again, marked as stale past its time.
//
// Checked on 21 October 2026, each answering for 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777
// without a key (prices that morning):
//   - CoinGecko, coin id "chog": GET /api/v3/simple/token_price/monad?contract_addresses=...
//     answered {"0x3500...": {"usd": 0.00150916}} (public plan, rate limited to a few calls a
//     minute; one call every five minutes is well within it).
//   - GeckoTerminal: GET /api/v2/networks/monad/tokens/0x3500... answered price_usd
//     "0.001507730107" (30 calls a minute).
//   - DexScreener: GET /latest/dex/tokens/0x3500... answered the pairs of the token, the
//     deepest (nad.fun, $96k of liquidity) at priceUsd "0.001522" (300 calls a minute).

import { CHOG_TOKEN } from './shop.js';

export const PRICE_KEPT_MS = 5 * 60 * 1000;
// A price older than this is still answered, but called stale.
export const PRICE_STALE_MS = 30 * 60 * 1000;
const SOURCE_TIMEOUT_MS = 8000;
// Some public endpoints refuse requests without a browser-like User-Agent.
const USER_AGENT = 'Mozilla/5.0 (compatible; ChogArena/1.0; +https://chogarena.vercel.app)';

const usdOf = value => {
  const number = typeof value === 'number' ? value : typeof value === 'string' ? Number(value) : NaN;
  return Number.isFinite(number) && number > 0 ? number : null;
};

/** The sources tried, in order; each reads the price in USD out of the JSON a source answered. */
export const PRICE_SOURCES = [
  {
    name: 'coingecko',
    url: token => `https://api.coingecko.com/api/v3/simple/token_price/monad?contract_addresses=${token}&vs_currencies=usd`,
    read: (body, token) => usdOf(body?.[token.toLowerCase()]?.usd ?? body?.[token]?.usd),
  },
  {
    name: 'geckoterminal',
    url: token => `https://api.geckoterminal.com/api/v2/networks/monad/tokens/${token}`,
    read: body => usdOf(body?.data?.attributes?.price_usd),
  },
  {
    name: 'dexscreener',
    url: token => `https://api.dexscreener.com/latest/dex/tokens/${token}`,
    // The price of the deepest pool of the token on Monad.
    read: (body, token) => {
      const pairs = (body?.pairs ?? []).filter(pair => pair?.chainId === 'monad'
        && typeof pair?.baseToken?.address === 'string' && pair.baseToken.address.toLowerCase() === token.toLowerCase());
      pairs.sort((a, b) => (b?.liquidity?.usd ?? 0) - (a?.liquidity?.usd ?? 0));
      return pairs.length > 0 ? usdOf(pairs[0].priceUsd) : null;
    },
  },
];

/**
 * @param {object} [options]
 * @param {typeof fetch} [options.fetch]
 * @param {() => number} [options.now] milliseconds
 * @param {string} [options.token] the token priced; $CHOG by default
 * @param {typeof PRICE_SOURCES} [options.sources] tried in this order
 * @param {number} [options.keptMs] how long a price read is answered without asking again
 */
export function createPrice({ fetch: send = fetch, now = Date.now, token = CHOG_TOKEN, sources = PRICE_SOURCES, keptMs = PRICE_KEPT_MS } = {}) {
  let known = null;
  let pending = null;
  let lastFailure = null;

  async function ask(source) {
    const response = await send(source.url(token), {
      headers: { Accept: 'application/json', 'User-Agent': USER_AGENT },
      signal: AbortSignal.timeout(SOURCE_TIMEOUT_MS),
    });
    if (!response.ok) throw new Error(`${source.name} answered HTTP ${response.status}`);
    const usd = source.read(await response.json(), token);
    if (usd === null) throw new Error(`${source.name} gave no price for ${token}`);
    return usd;
  }

  async function refresh() {
    const problems = [];
    for (const source of sources) {
      try {
        const usd = await ask(source);
        known = { usd, source: source.name, readAt: now() };
        lastFailure = null;
        return known;
      } catch (error) {
        problems.push(`${source.name}: ${error.message}`);
      }
    }
    lastFailure = { at: now(), why: problems.join('; ') };
    return known;
  }

  /**
   * The price now: {usd, source, readAt} as last read, refreshed when older than keptMs. Never
   * throws: without any source answering yet it answers usd null, and once a source answered it
   * answers that price until another answer comes. Concurrent callers share one refresh.
   */
  async function current() {
    const fresh = known && now() - known.readAt < keptMs;
    // A failure is not retried at once either: every source would be asked on every page.
    const failedLately = lastFailure && now() - lastFailure.at < keptMs;
    if (fresh || (failedLately && known)) return known;
    if (failedLately && !known) return null;
    pending ??= refresh().finally(() => { pending = null; });
    return pending;
  }

  /** What a page is told: the token, its price in USD (null when unknown), where and when it was read. */
  async function describe() {
    const price = await current();
    if (!price) return { token: token.toLowerCase(), usd: null, source: null, readAt: null, stale: true, problem: lastFailure?.why ?? null };
    return {
      token: token.toLowerCase(), usd: price.usd, source: price.source, readAt: new Date(price.readAt).toISOString(),
      stale: now() - price.readAt > PRICE_STALE_MS, ...(lastFailure ? { problem: lastFailure.why } : {}),
    };
  }

  /** An amount in the smallest unit of the token (decimal digits) as dollars, or null while the price is unknown. */
  async function usdOfWei(wei) {
    const price = await current();
    if (!price || typeof wei !== 'string' || !/^[0-9]+$/.test(wei)) return null;
    // Whole units and a few decimals are enough for a label; this is not accounting.
    return Number(BigInt(wei) / 10n ** 12n) / 1e6 * price.usd;
  }

  /** The price last read, without asking anything: a number, or null. */
  const lastUsd = () => known?.usd ?? null;

  return { current, describe, usdOfWei, lastUsd };
}
