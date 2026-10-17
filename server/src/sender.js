// Transactions the game services send themselves, with the referee's key: the careers recorded
// after a match was played again (ChogCareers.record), a holder's stop to lending, and the
// closing of a rental whose Genesis was sold (ChogRental.close). Everything else on chain is
// sent by the players' own wallets.
//
// An EIP-1559 transaction (type 2): RLP of [chainId, nonce, maxPriorityFeePerGas, maxFeePerGas,
// gas, to, value, data, accessList], signed over keccak256(0x02 || rlp) and sent with
// eth_sendRawTransaction (https://eips.ethereum.org/EIPS/eip-1559). Monad charges the gas limit,
// not the gas used ("The gas charged for a transaction is the gas limit",
// https://docs.monad.xyz/developer-essentials/gas-pricing.md), so each limit is the estimate plus a
// small margin. One transaction at a time: each one takes the next nonce. The key is never logged.

import { secp256k1 } from '@noble/curves/secp256k1.js';
import { keccak_256 } from '@noble/hashes/sha3.js';
import { isAddress } from './chain.js';

const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const fromHex = hex => {
  const clean = String(hex).replace(/^0x/, '');
  return Uint8Array.from((clean.length % 2 ? '0' + clean : clean).match(/.{2}/g) ?? [], pair => parseInt(pair, 16));
};
// A whole number as the shortest big-endian bytes: zero is empty.
const integer = value => (BigInt(value) === 0n ? new Uint8Array() : fromHex(BigInt(value).toString(16)));

function length(size, offset) {
  if (size < 56) return Uint8Array.of(offset + size);
  const bytes = integer(size);
  return Uint8Array.of(offset + 55 + bytes.length, ...bytes);
}

/** RLP of bytes (Uint8Array) and lists of them. */
export function rlp(item) {
  if (item instanceof Uint8Array) {
    if (item.length === 1 && item[0] < 0x80) return item;
    return Uint8Array.of(...length(item.length, 0x80), ...item);
  }
  const body = item.map(rlp).reduce((all, one) => Uint8Array.of(...all, ...one), new Uint8Array());
  return Uint8Array.of(...length(body.length, 0xc0), ...body);
}

/** The raw signed transaction, as hex. */
export function signTransaction({ chainId, nonce, maxPriorityFeePerGas, maxFeePerGas, gas, to, value = 0n, data = '0x' }, secretKey) {
  const fields = [integer(chainId), integer(nonce), integer(maxPriorityFeePerGas), integer(maxFeePerGas), integer(gas),
    fromHex(to), integer(value), fromHex(data), []];
  const digest = keccak_256(Uint8Array.of(2, ...rlp(fields)));
  const signature = secp256k1.sign(digest, secretKey, { prehash: false, format: 'recovered' });
  const signed = [...fields, integer(signature[0]), integer('0x' + toHex(signature.slice(1, 33))), integer('0x' + toHex(signature.slice(33, 65)))];
  return '0x02' + toHex(rlp(signed));
}

/** The address of a secret key. */
export function addressOf(secretKey) {
  return '0x' + toHex(keccak_256(secp256k1.getPublicKey(secretKey, false).slice(1)).slice(-20));
}

// The margin over the estimate, in percent: the estimate is made on the state of a moment before.
const GAS_MARGIN_PERCENT = 15;
// What is offered to the validator above the base fee when the endpoint does not say: 2 gwei.
const DEFAULT_TIP = 2_000_000_000n;
const RECEIPT_TRIES = 30;
const RECEIPT_WAIT_MS = 1000;

/**
 * @param {object} options
 * @param {string[]} options.rpcUrls Monad endpoints, tried in order
 * @param {string} options.privateKey the referee's key, 0x and 64 hex digits
 * @param {number} [options.chainId] 143 for Monad mainnet; the endpoint must answer the same
 * @param {typeof fetch} [options.fetch]
 * @param {(ms: number) => Promise<void>} [options.sleep]
 */
export function createSender({ rpcUrls, privateKey, chainId = 143, fetch: send = fetch, sleep = ms => new Promise(done => setTimeout(done, ms)) }) {
  if (typeof privateKey !== 'string' || !/^0x[0-9a-fA-F]{64}$/.test(privateKey)) throw new Error('The sender needs a key: 0x followed by 64 hex digits');
  const secretKey = fromHex(privateKey);
  const from = addressOf(secretKey);
  let turn = Promise.resolve();
  let id = 0;

  async function ask(method, params) {
    let last = null;
    for (const url of rpcUrls) {
      try {
        const response = await send(url, {
          method: 'POST', headers: { 'content-type': 'application/json' },
          body: JSON.stringify({ jsonrpc: '2.0', id: ++id, method, params }),
        });
        const answer = await response.json();
        if (answer.error) {
          // An answer from the chain itself (a revert, a refused transaction) is the same everywhere.
          throw Object.assign(new Error(`${method}: ${answer.error.message ?? JSON.stringify(answer.error)}`), { fromChain: true });
        }
        return answer.result;
      } catch (error) {
        if (error.fromChain) throw error;
        last = error;
      }
    }
    throw last ?? new Error(`${method}: no endpoint answered`);
  }

  async function sendNow({ to, data, purpose }) {
    if (!isAddress(to)) throw new Error('the transaction must name a contract');
    const [answeredChain, nonce, block, tip] = await Promise.all([
      ask('eth_chainId', []), ask('eth_getTransactionCount', [from, 'pending']), ask('eth_getBlockByNumber', ['latest', false]),
      ask('eth_maxPriorityFeePerGas', []).catch(() => '0x' + DEFAULT_TIP.toString(16)),
    ]);
    if (Number(BigInt(answeredChain)) !== chainId) throw new Error(`the endpoint is on chain ${Number(BigInt(answeredChain))}, not ${chainId}`);
    // Refused here when it would revert: nothing is paid for a transaction that fails.
    const estimate = BigInt(await ask('eth_estimateGas', [{ from, to, data }]));
    const gas = estimate + estimate * BigInt(GAS_MARGIN_PERCENT) / 100n;
    const baseFee = BigInt(block.baseFeePerGas ?? '0x0');
    const raw = signTransaction({
      chainId, nonce, maxPriorityFeePerGas: tip, maxFeePerGas: baseFee * 2n + BigInt(tip), gas, to, data,
    }, secretKey);
    const hash = await ask('eth_sendRawTransaction', [raw]);
    for (let attempt = 0; attempt < RECEIPT_TRIES; attempt++) {
      const receipt = await ask('eth_getTransactionReceipt', [hash]).catch(() => null);
      if (receipt) {
        const ok = BigInt(receipt.status) === 1n;
        console.log(`Sent ${purpose}: ${hash} ${ok ? 'done' : 'reverted'}, gas limit ${gas}`);
        return { hash, ok, gasLimit: gas.toString() };
      }
      await sleep(RECEIPT_WAIT_MS);
    }
    console.warn(`Sent ${purpose}: ${hash}, no receipt yet`);
    return { hash, ok: null, gasLimit: gas.toString() };
  }

  /** Sends one transaction to a contract after the ones before it; answers {hash, ok, gasLimit}. */
  function transact({ to, data, purpose = 'a transaction' }) {
    const sent = turn.then(() => sendNow({ to, data, purpose }));
    turn = sent.catch(() => {});
    return sent;
  }

  /** What the sender holds, in the smallest unit of MON. */
  async function balance() {
    return BigInt(await ask('eth_getBalance', [from, 'latest']));
  }

  return { transact, balance, address: from };
}
