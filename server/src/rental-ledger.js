// What each Genesis earned its holders by being rented, since the rental contract was deployed:
// the sum of the contract's Earned events for the token, and how many times it was rented
// (Rented events). The contract keeps earnings by holder, not by Genesis, so this is read from
// its events: on Monad's public endpoints eth_getLogs is capped by block range (100 blocks on
// rpc.monad.xyz, "too large" past a few hundred on rpc1 and rpc3; 10,000 blocks on
// rpc2.monad.xyz, checked on 21 October 2026, each refusing 50,000), so the events are read in
// windows of 10,000 blocks, from the block the contract was deployed at, in the background, and
// kept in memory; then every minute from where the last read stopped. A restart reads them
// again (about 130 windows for the first four days of the contract). While the read is behind,
// the figures are answered as they stand with the block they are read up to; an endpoint that
// stops serving windows leaves the figures where they are, never a failed page.

import { keccak_256 } from '@noble/hashes/sha3.js';
import { isAddress } from './chain.js';

/** The block ChogRental v2 was deployed at on Monad mainnet (eth_getCode by halves on rpc2.monad.xyz, 21 October 2026). */
export const RENTAL_DEPLOYED_AT_BLOCK = 114126084;
/** The public endpoint that serves 10,000-block windows of eth_getLogs (the others cap far lower). */
export const LOGS_RPC_URLS = ['https://rpc2.monad.xyz'];
export const WINDOW_BLOCKS = 10000;
const READ_EVERY_MS = 60000;
const RETRY_AFTER_MS = 15000;
const USER_AGENT = 'Mozilla/5.0 (compatible; ChogArena/1.0; +https://chogarena.vercel.app)';

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const topicOf = signature => '0x' + toHex(keccak_256(encoder.encode(signature)));

/** The topics of the two events read: Earned(tokenId, lender, amount) and Rented(tokenId, renter, lender, days, paid, until). */
export const EARNED_TOPIC = topicOf('Earned(uint256,address,uint256)');
export const RENTED_TOPIC = topicOf('Rented(uint256,address,address,uint256,uint256,uint64)');

/**
 * @param {object} options
 * @param {string} options.address the rental contract
 * @param {number} [options.fromBlock] the first block read; the deployment block by default
 * @param {string[]} [options.rpcUrls] endpoints that serve windows of eth_getLogs, tried in order
 * @param {number} [options.windowBlocks]
 * @param {typeof fetch} [options.fetch]
 * @param {(ms: number) => Promise<void>} [options.wait] how the background read pauses
 */
export function createRentalLedger({
  address, fromBlock = RENTAL_DEPLOYED_AT_BLOCK, rpcUrls = LOGS_RPC_URLS, windowBlocks = WINDOW_BLOCKS,
  fetch: send = fetch, wait = ms => new Promise(resolve => setTimeout(resolve, ms).unref?.()),
}) {
  if (!isAddress(address)) throw new Error('The rental ledger needs the address of the rental contract');
  const rental = address.toLowerCase();
  // Token id to what it earned (smallest unit, as a bigint) and how many rentals it had.
  const tokens = new Map();
  let upToBlock = fromBlock - 1;
  let latestSeen = null;
  let lastProblem = null;
  let stopped = false;

  async function rpc(method, params) {
    let lastError;
    for (const url of rpcUrls) {
      try {
        const response = await send(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'User-Agent': USER_AGENT },
          body: JSON.stringify({ jsonrpc: '2.0', id: 1, method, params }),
          signal: AbortSignal.timeout(20000),
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

  const entry = tokenId => {
    let known = tokens.get(tokenId);
    if (!known) {
      known = { earned: 0n, rentals: 0 };
      tokens.set(tokenId, known);
    }
    return known;
  };

  /** Reads one window of events and adds them up; the window must be at most windowBlocks long. */
  async function readWindow(from, to) {
    const logs = await rpc('eth_getLogs', [{
      address: rental, fromBlock: '0x' + from.toString(16), toBlock: '0x' + to.toString(16), topics: [[EARNED_TOPIC, RENTED_TOPIC]],
    }]);
    if (!Array.isArray(logs)) throw new Error('eth_getLogs did not answer a list');
    for (const log of logs) {
      if (!Array.isArray(log.topics) || log.topics.length < 2) continue;
      const tokenId = Number(BigInt(log.topics[1]));
      if (log.topics[0] === EARNED_TOPIC && typeof log.data === 'string' && log.data.length >= 66) {
        entry(tokenId).earned += BigInt('0x' + log.data.slice(2, 66));
      } else if (log.topics[0] === RENTED_TOPIC) {
        entry(tokenId).rentals += 1;
      }
    }
    upToBlock = to;
  }

  /** Reads every window from where the last read stopped up to the chain's latest block. */
  async function catchUp() {
    const latest = Number(BigInt(await rpc('eth_blockNumber', [])));
    latestSeen = latest;
    while (upToBlock < latest) {
      const from = upToBlock + 1;
      await readWindow(from, Math.min(from + windowBlocks - 1, latest));
    }
    lastProblem = null;
  }

  /** Starts the background read: catches up, then reads again every minute; after a failure it waits and tries again. */
  function start() {
    (async () => {
      while (!stopped) {
        let failed = false;
        try {
          await catchUp();
        } catch (error) {
          failed = true;
          lastProblem = error.message;
          console.warn(`Rental ledger: ${error.message} (read up to block ${upToBlock})`);
        }
        await wait(failed ? RETRY_AFTER_MS : READ_EVERY_MS);
      }
    })();
  }

  const stop = () => { stopped = true; };

  /** Whether the read reached the latest block it saw. */
  const caughtUp = () => latestSeen !== null && upToBlock >= latestSeen;

  /**
   * What these Genesis earned by being rented and how often, as read so far: amounts in the
   * smallest unit as decimal text, before the burned share a holder pays when claiming.
   */
  function earnedOf(tokenIds) {
    return {
      enabled: true,
      fromBlock,
      upToBlock: Math.max(upToBlock, fromBlock - 1),
      caughtUp: caughtUp(),
      ...(lastProblem ? { problem: lastProblem } : {}),
      tokens: tokenIds.map(tokenId => {
        const known = tokens.get(tokenId);
        return { tokenId, earned: (known?.earned ?? 0n).toString(), rentals: known?.rentals ?? 0 };
      }),
    };
  }

  return { catchUp, start, stop, earnedOf, caughtUp, address: rental };
}
