// Sign-in with a wallet signature (ERC-4361 message, signed with personal_sign), then a session
// token; and the passes a signed-in holder hands out to lend a Genesis. Nothing is stored: the
// challenge, the session and the pass are authenticated with an HMAC of the server secret, so
// any instance of the services can check them.
//
// Limit: the signer is recovered from the signature, which only works for a key-pair account.
// A smart-contract wallet (ERC-1271) cannot sign in yet.

import { secp256k1 } from '@noble/curves/secp256k1.js';
import { keccak_256 } from '@noble/hashes/sha3.js';
import { CHAIN_ID, isAddress } from './chain.js';

const encoder = new TextEncoder();
const CHALLENGE_SECONDS = 10 * 60;
const SESSION_SECONDS = 7 * 24 * 3600;
export const MAX_LOAN_DAYS = 30;
const STATEMENT = 'Sign in to Chog Arena to play your Genesis and record its career. This costs nothing and sends no transaction.';

const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const fromHex = hex => Uint8Array.from(hex.match(/.{2}/g) ?? [], pair => parseInt(pair, 16));
const toBase64Url = bytes => btoa(String.fromCharCode(...bytes)).replaceAll('+', '-').replaceAll('/', '_').replaceAll('=', '');
const fromBase64Url = text => Uint8Array.from(atob(text.replaceAll('-', '+').replaceAll('_', '/')), char => char.charCodeAt(0));

/** The address with the capital letters of EIP-55, as ERC-4361 messages carry it. */
export function checksumAddress(address) {
  const lower = address.toLowerCase().slice(2);
  const hash = toHex(keccak_256(encoder.encode(lower)));
  return '0x' + [...lower].map((char, index) => (parseInt(hash[index], 16) >= 8 ? char.toUpperCase() : char)).join('');
}

/** Address that signed "message" with personal_sign, lower case; throws on a malformed signature. */
export function recoverSigner(message, signature) {
  if (typeof signature !== 'string' || !/^0x[0-9a-fA-F]{130}$/.test(signature)) throw new Error('malformed signature');
  const bytes = fromHex(signature.slice(2));
  // Wallets end the signature with 27 or 28; some send 0 or 1.
  const recovery = bytes[64] >= 27 ? bytes[64] - 27 : bytes[64];
  if (recovery !== 0 && recovery !== 1) throw new Error('malformed signature');
  const text = encoder.encode(message);
  const prefix = encoder.encode(`\x19Ethereum Signed Message:\n${text.length}`);
  const digest = keccak_256(Uint8Array.from([...prefix, ...text]));
  const parsed = secp256k1.Signature.fromBytes(Uint8Array.from([recovery, ...bytes.slice(0, 64)]), 'recovered');
  const publicKey = parsed.recoverPublicKey(digest).toBytes(false);
  return '0x' + toHex(keccak_256(publicKey.slice(1)).slice(-20));
}

/**
 * @param {object} options
 * @param {string} options.secret at least 32 characters, never sent to a client
 * @param {() => number} [options.now] milliseconds
 */
export function createSessions({ secret, now = Date.now }) {
  if (typeof secret !== 'string' || secret.length < 32) throw new Error('The session secret must be at least 32 characters');
  const key = crypto.subtle.importKey('raw', encoder.encode(secret), { name: 'HMAC', hash: 'SHA-256' }, false, ['sign', 'verify']);

  const seal = async text => toBase64Url(new Uint8Array(await crypto.subtle.sign('HMAC', await key, encoder.encode(text))));
  const sealed = async (text, proof) => {
    try {
      return await crypto.subtle.verify('HMAC', await key, fromBase64Url(proof), encoder.encode(text));
    } catch {
      return false;
    }
  };

  /** The message a wallet must sign to prove it controls "address", and the proof we wrote it. */
  async function challenge(address, { domain, uri }) {
    if (!isAddress(address)) throw new Error('bad address');
    const issued = new Date(now());
    const expires = new Date(now() + CHALLENGE_SECONDS * 1000);
    const nonce = toHex(crypto.getRandomValues(new Uint8Array(12)));
    const message = [
      `${domain} wants you to sign in with your Ethereum account:`,
      checksumAddress(address),
      '',
      STATEMENT,
      '',
      `URI: ${uri}`,
      'Version: 1',
      `Chain ID: ${CHAIN_ID}`,
      `Nonce: ${nonce}`,
      `Issued At: ${issued.toISOString()}`,
      `Expiration Time: ${expires.toISOString()}`,
    ].join('\n');
    return { message, proof: await seal('challenge:' + message) };
  }

  /** A session for the address named in a challenge we issued, once its signature checks out. */
  async function signIn({ message, signature, proof }) {
    if (typeof message !== 'string' || typeof proof !== 'string' || !(await sealed('challenge:' + message, proof))) {
      return { ok: false, reason: 'unknown_challenge' };
    }
    const lines = message.split('\n');
    const address = (lines[1] ?? '').toLowerCase();
    const expires = Date.parse((lines.find(line => line.startsWith('Expiration Time: ')) ?? '').slice(17));
    if (!isAddress(address) || !(expires > now())) return { ok: false, reason: 'expired_challenge' };
    let signer;
    try {
      signer = recoverSigner(message, signature);
    } catch {
      return { ok: false, reason: 'bad_signature' };
    }
    if (signer !== address) return { ok: false, reason: 'wrong_signer' };

    const expiresAt = now() + SESSION_SECONDS * 1000;
    const payload = toBase64Url(encoder.encode(JSON.stringify({ address, expiresAt })));
    return { ok: true, address, expiresAt, session: payload + '.' + (await seal('session:' + payload)) };
  }

  /** Address of a session token that we issued and that has not expired, else null. */
  async function addressOf(session) {
    if (typeof session !== 'string') return null;
    const [payload, proof] = session.split('.');
    if (!payload || !proof || !(await sealed('session:' + payload, proof))) return null;
    try {
      const { address, expiresAt } = JSON.parse(new TextDecoder().decode(fromBase64Url(payload)));
      return isAddress(address) && expiresAt > now() ? address : null;
    } catch {
      return null;
    }
  }

  /**
   * A pass: the text a holder hands out to lend one Genesis. Whoever presents it plays that
   * Genesis in the holder's name, without a wallet. The caller has checked that "lender" is
   * signed in and holds the token; whether a pass still counts is decided when it is used.
   */
  async function lend({ tokenId, lender, days = MAX_LOAN_DAYS }) {
    const issuedAt = now();
    const expiresAt = issuedAt + days * 24 * 3600 * 1000;
    const payload = toBase64Url(encoder.encode(JSON.stringify({ tokenId, lender, issuedAt, expiresAt })));
    return { pass: payload + '.' + (await seal('loan:' + payload)), tokenId, lender, issuedAt, expiresAt };
  }

  /** What a pass that we issued says, with whether it has run out; null for any other text. */
  async function loanOf(pass) {
    if (typeof pass !== 'string') return null;
    const [payload, proof] = pass.split('.');
    if (!payload || !proof || !(await sealed('loan:' + payload, proof))) return null;
    try {
      const { tokenId, lender, issuedAt, expiresAt } = JSON.parse(new TextDecoder().decode(fromBase64Url(payload)));
      if (!Number.isInteger(tokenId) || !isAddress(lender)) return null;
      return { tokenId, lender, issuedAt, expiresAt, expired: !(expiresAt > now()) };
    } catch {
      return null;
    }
  }

  return { challenge, signIn, addressOf, lend, loanOf };
}
