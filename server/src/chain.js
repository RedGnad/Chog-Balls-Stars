// Read-only access to the Chog Genesis collection on Monad mainnet.
//
// The collection is not enumerable, so the owners of all tokens are read with one Multicall3
// call (ownerOf for every token id) and kept for a short time: that answers "what does this
// address hold". "Does this address hold that token now" is one ownerOf read of its own. Token
// ids are the canonical on-chain ids, not the numbers shown in the names.

export const CHAIN_ID = 143;
export const GENESIS = '0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763';
export const MULTICALL3 = '0xcA11bde05977b3631167028862bE2a173976CA11';
export const LAST_TOKEN_ID = 1969;

// Public endpoints listed in the Monad documentation, tried in this order.
export const DEFAULT_RPC_URLS = ['https://rpc.monad.xyz', 'https://rpc1.monad.xyz', 'https://rpc3.monad.xyz'];

const OWNER_OF = '6352211e'; // ownerOf(uint256)
const OWNER_READ_SHARED_MS = 2000;
const AGGREGATE3 = '82ad56cb'; // aggregate3((address,bool,bytes)[])

const word = value => BigInt(value).toString(16).padStart(64, '0');

export function isAddress(text) {
  return typeof text === 'string' && /^0x[0-9a-fA-F]{40}$/.test(text);
}

/** Call data of aggregate3 with one (Genesis, allowFailure, ownerOf(id)) element per token id. */
export function encodeOwnerScan(lastId) {
  const target = word(GENESIS);
  let offsets = '';
  let elements = '';
  for (let tokenId = 1; tokenId <= lastId; tokenId++) {
    offsets += word(lastId * 32 + (tokenId - 1) * 192);
    elements += target + word(1) + word(0x60) + word(36) + (OWNER_OF + word(tokenId)).padEnd(128, '0');
  }
  return '0x' + AGGREGATE3 + word(0x20) + word(lastId) + offsets + elements;
}

/** Owner of each token id, lower case, from the result of encodeOwnerScan; null where ownerOf failed. */
export function decodeOwnerScan(result) {
  const raw = result.slice(2);
  const count = parseInt(raw.slice(64, 128), 16);
  const base = 128;
  const owners = new Map();
  for (let index = 0; index < count; index++) {
    const offset = parseInt(raw.slice(base + index * 64, base + (index + 1) * 64), 16) * 2;
    const element = raw.slice(base + offset);
    const succeeded = parseInt(element.slice(0, 64), 16) === 1;
    const returned = parseInt(element.slice(128, 192), 16);
    owners.set(index + 1, succeeded && returned === 32 ? '0x' + element.slice(192, 256).slice(-40) : null);
  }
  return owners;
}

/**
 * Call data of aggregate3 for any list of calls, each {to, data} with data as hex; a call that
 * fails does not fail the others.
 */
export function encodeAggregate3(calls) {
  let offsets = '';
  let elements = '';
  let offset = calls.length * 32;
  for (const { to, data } of calls) {
    const bytes = data.replace(/^0x/, '');
    const padded = bytes.padEnd(Math.ceil(bytes.length / 64) * 64, '0');
    offsets += word(offset);
    elements += word(to) + word(1) + word(0x60) + word(bytes.length / 2) + padded;
    offset += 32 * 4 + padded.length / 2;
  }
  return '0x' + AGGREGATE3 + word(0x20) + word(calls.length) + offsets + elements;
}

/** What each call of an aggregate3 answered, as hex, in order; null where the call failed. */
export function decodeAggregate3(result) {
  const raw = result.slice(2);
  const count = parseInt(raw.slice(64, 128), 16);
  const base = 128;
  const answers = [];
  for (let index = 0; index < count; index++) {
    const offset = parseInt(raw.slice(base + index * 64, base + (index + 1) * 64), 16) * 2;
    const element = raw.slice(base + offset);
    const succeeded = parseInt(element.slice(0, 64), 16) === 1;
    const length = parseInt(element.slice(128, 192), 16) * 2;
    answers.push(succeeded ? '0x' + element.slice(192, 192 + length) : null);
  }
  return answers;
}

/**
 * @param {object} options
 * @param {string[]} [options.rpcUrls]
 * @param {typeof fetch} [options.fetch]
 * @param {() => number} [options.now] milliseconds
 * @param {number} [options.maxAgeMs] how long one owner scan is reused
 */
export function createChain({ rpcUrls = DEFAULT_RPC_URLS, fetch: send = fetch, now = Date.now, maxAgeMs = 20000 } = {}) {
  let cached = null;
  let pending = null;

  async function rpc(method, params) {
    let lastError;
    for (const url of rpcUrls) {
      try {
        const response = await send(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ jsonrpc: '2.0', id: 1, method, params }),
          signal: AbortSignal.timeout(15000),
        });
        if (!response.ok) throw new Error(`${url} answered HTTP ${response.status}`);
        const answer = await response.json();
        if (answer.result === undefined) throw new Error(`${url}: ${JSON.stringify(answer.error ?? answer).slice(0, 200)}`);
        return answer.result;
      } catch (error) {
        lastError = error;
      }
    }
    throw new Error(`${method} failed on every endpoint: ${lastError?.message}`);
  }

  async function scan() {
    const block = await rpc('eth_blockNumber', []);
    const result = await rpc('eth_call', [{ to: MULTICALL3, data: encodeOwnerScan(LAST_TOKEN_ID) }, block]);
    const owners = decodeOwnerScan(result);
    if (owners.size !== LAST_TOKEN_ID) throw new Error(`Owner scan returned ${owners.size} entries, expected ${LAST_TOKEN_ID}`);
    return { owners, block: parseInt(block, 16), readAt: now() };
  }

  /** Owners of every token, at most maxAgeMs old unless fresh is asked. */
  async function owners({ fresh = false } = {}) {
    if (!fresh && cached && now() - cached.readAt < maxAgeMs) return cached;
    // Concurrent callers share one scan.
    pending ??= scan().then(
      value => { cached = value; pending = null; return value; },
      error => { pending = null; throw error; });
    return pending;
  }

  /** Token ids held by an address, ascending, with the block they were read at. */
  async function ownedBy(address, options) {
    const wanted = address.toLowerCase();
    const snapshot = await owners(options);
    const tokens = [];
    for (const [tokenId, owner] of snapshot.owners) if (owner === wanted) tokens.push(tokenId);
    return { tokens, block: snapshot.block, readAt: snapshot.readAt };
  }

  // The owner of one token, read by itself: a check before a credit or for a lent Genesis must
  // not cost a scan of the collection. Callers that ask within two seconds share one read.
  const ownerReads = new Map();

  function ownerNow(tokenId) {
    const known = ownerReads.get(tokenId);
    if (known && now() - known.at < OWNER_READ_SHARED_MS) return known.owner;
    const owner = rpc('eth_call', [{ to: GENESIS, data: '0x' + OWNER_OF + word(tokenId) }, 'latest']).then(result => {
      if (typeof result !== 'string' || result.length !== 66) throw new Error(`ownerOf(${tokenId}) returned ${String(result).slice(0, 80)}`);
      return '0x' + result.slice(-40).toLowerCase();
    });
    ownerReads.set(tokenId, { at: now(), owner });
    // A failed read is not kept: the next caller asks again.
    owner.catch(() => { if (ownerReads.get(tokenId)?.owner === owner) ownerReads.delete(tokenId); });
    return owner;
  }

  /**
   * Whether an address holds a token now, for anything that credits or lends: a read of that
   * token alone, never the cached scan.
   */
  async function holds(address, tokenId) {
    if (!Number.isInteger(tokenId) || tokenId < 1 || tokenId > LAST_TOKEN_ID) return { holds: false };
    return { holds: (await ownerNow(tokenId)) === address.toLowerCase() };
  }

  /** Block of the last owner read, or null before the first one. Reads nothing. */
  const lastBlock = () => cached?.block ?? null;

  /** The answer of a read-only call to a contract, as hex. */
  const call = (to, data) => rpc('eth_call', [{ to, data }, 'latest']);

  /**
   * Several read-only calls in one request (Multicall3). Answers the hex each call returned,
   * in order, and null for a call that failed.
   */
  async function callMany(calls) {
    if (calls.length === 0) return [];
    return decodeAggregate3(await call(MULTICALL3, encodeAggregate3(calls)));
  }

  return { rpc, owners, ownedBy, holds, lastBlock, call, callMany };
}
