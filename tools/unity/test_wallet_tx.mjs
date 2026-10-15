// Signs and sends a transaction for the stand-in wallet of the headless browser
// (qualify_webgl.mjs), when it plays on a real chain where nobody can be impersonated: Monad's
// testnet, with a throwaway key made for the test (tools/contracts/new_referee_key.sh makes one
// the same way). The key is read from the environment by the caller and never printed.
//
// An EIP-1559 transaction (type 2): RLP of [chainId, nonce, maxPriorityFeePerGas, maxFeePerGas,
// gas, to, value, data, accessList], signed over keccak256(0x02 || rlp), sent with
// eth_sendRawTransaction (https://eips.ethereum.org/EIPS/eip-1559).

const modules = new URL('../../server/node_modules/@noble/', import.meta.url);
const { secp256k1 } = await import(new URL('curves/secp256k1.js', modules));
const { keccak_256 } = await import(new URL('hashes/sha3.js', modules));

const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const fromHex = hex => {
  const clean = hex.replace(/^0x/, '');
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

/**
 * Sends {from, to, data, gas} as the wallet of "secretKey" does: reads the chain id, the nonce
 * and the fees from the endpoint, signs, sends. Answers the transaction hash.
 * "ask(method, params)" calls the endpoint and answers its "result" (or throws).
 */
export async function sendTransaction(params, secretKey, ask) {
  const from = '0x' + toHex(keccak_256(secp256k1.getPublicKey(secretKey, false).slice(1)).slice(-20));
  if (params.from && params.from.toLowerCase() !== from) throw new Error('the page asked to send from another address');
  const [chainId, nonce, block, tip] = await Promise.all([
    ask('eth_chainId', []), ask('eth_getTransactionCount', [from, 'pending']), ask('eth_getBlockByNumber', ['latest', false]),
    ask('eth_maxPriorityFeePerGas', []).catch(() => '0x77359400'),
  ]);
  const baseFee = BigInt(block.baseFeePerGas ?? '0x0');
  const gas = params.gas ?? await ask('eth_estimateGas', [{ from, to: params.to, data: params.data, value: params.value ?? '0x0' }]);
  const raw = signTransaction({
    chainId, nonce, maxPriorityFeePerGas: tip, maxFeePerGas: baseFee * 2n + BigInt(tip), gas, to: params.to,
    value: params.value ?? 0n, data: params.data ?? '0x',
  }, secretKey);
  return ask('eth_sendRawTransaction', [raw]);
}
