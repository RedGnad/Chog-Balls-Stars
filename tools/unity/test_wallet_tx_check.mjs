// Checks tools/unity/test_wallet_tx.mjs against a local chain (anvil, never a real one): a
// throwaway key made here gets some MON from anvil, takes test tokens from the tap of a
// deployed TestChog with a transaction this module signs, and the chain must say it went
// through and the balance must be there.
//
//   node tools/unity/test_wallet_tx_check.mjs <anvil endpoint> <TestChog address>

import { randomBytes } from 'node:crypto';
import { sendTransaction } from './test_wallet_tx.mjs';

const [endpoint, token] = process.argv.slice(2);
if (!endpoint || !token) {
  console.error('usage: test_wallet_tx_check.mjs <anvil endpoint> <TestChog address>');
  process.exit(2);
}
const ask = async (method, params) => {
  const answer = await (await fetch(endpoint, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ jsonrpc: '2.0', id: 1, method, params }),
  })).json();
  if (answer.error) throw new Error(`${method}: ${answer.error.message}`);
  return answer.result;
};
if (!(await ask('web3_clientVersion', [])).toLowerCase().includes('anvil')) {
  console.error('this check only runs on anvil');
  process.exit(1);
}

const modules = new URL('../../server/node_modules/@noble/', import.meta.url);
const { secp256k1 } = await import(new URL('curves/secp256k1.js', modules));
const { keccak_256 } = await import(new URL('hashes/sha3.js', modules));
const secretKey = Uint8Array.from(randomBytes(32));
const address = '0x' + Buffer.from(keccak_256(secp256k1.getPublicKey(secretKey, false).slice(1)).slice(-20)).toString('hex');
await ask('anvil_setBalance', [address, '0x56BC75E2D63100000']);

const hash = await sendTransaction({ from: address, to: token, data: '0x4e71d92d', gas: '0x30d40' }, secretKey, ask);
let receipt = null;
for (let attempt = 0; attempt < 20 && !receipt; attempt++) {
  receipt = await ask('eth_getTransactionReceipt', [hash]);
  if (!receipt) await new Promise(resolve => setTimeout(resolve, 250));
}
const balance = BigInt(await ask('eth_call', [{ to: token, data: '0x70a08231' + address.slice(2).padStart(64, '0') }, 'latest']));
console.log(`type ${receipt?.type}, status ${receipt?.status}, gas used ${BigInt(receipt?.gasUsed ?? 0)}, balance ${balance / 10n ** 18n} tCHOG`);
process.exit(receipt?.status === '0x1' && balance === 1000n * 10n ** 18n ? 0 : 1);
