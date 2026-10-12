// A key-pair wallet for tests: an address and personal_sign, as a browser wallet produces them.

import { secp256k1 } from '@noble/curves/secp256k1.js';
import { keccak_256 } from '@noble/hashes/sha3.js';

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');

export function createWallet(secretKey = secp256k1.keygen().secretKey) {
  const publicKey = secp256k1.getPublicKey(secretKey, false);
  const address = '0x' + toHex(keccak_256(publicKey.slice(1)).slice(-20));

  /** The 65-byte signature of personal_sign: r, s, then 27 or 28. */
  function personalSign(message) {
    const text = encoder.encode(message);
    const prefix = encoder.encode(`\x19Ethereum Signed Message:\n${text.length}`);
    const digest = keccak_256(Uint8Array.from([...prefix, ...text]));
    const signature = secp256k1.sign(digest, secretKey, { prehash: false, format: 'recovered' });
    return '0x' + toHex(signature.slice(1)) + (27 + signature[0]).toString(16);
  }

  return { address, personalSign };
}

export const hexToBytes = hex => Uint8Array.from(hex.replace(/^0x/, '').match(/.{2}/g), pair => parseInt(pair, 16));
