import assert from 'node:assert/strict';
import { test } from 'node:test';
import { checksumAddress, createSessions, recoverSigner } from '../src/session.js';
import { createWallet, hexToBytes } from './wallet.js';

const SECRET = 'a test secret that is long enough to be accepted';
const PAGE = { domain: 'game.example', uri: 'https://game.example' };

test('addresses get the capital letters of EIP-55', () => {
  // Test vectors of the EIP-55 text.
  for (const address of [
    '0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed',
    '0xfB6916095ca1df60bB79Ce92cE3Ea74c37c5d359',
    '0xdbF03B407c01E7cD3CBea99509d93f8DDDC8C6FB',
    '0xD1220A0cf47c7B9Be7A2E6BA89F429762e7b9aDb',
  ]) {
    assert.equal(checksumAddress(address.toLowerCase()), address);
  }
});

test('the signer of a personal_sign signature is recovered, as in the web3.js documentation example', () => {
  // Private key, address and signature of 'Some data' published in the web3.js documentation
  // (eth.accounts.sign). It checks this code against an independent implementation.
  const signature = '0xb91467e570a6466aa9e9876cbcd013baba02900b8979d43fe208a4a4f339f5fd'
    + '6007e74cd82e037b800186422fc2da167c747ef045e5d18a5f5d4300f8e1a0291c';
  assert.equal(recoverSigner('Some data', signature), '0x2c7536e3605d9c16a7a3d7b1898e529396a65c23');

  const wallet = createWallet(hexToBytes('0x4c0883a69102937d6231471b5dbb6204fe5129617082792ae468d01a3f362318'));
  assert.equal(wallet.address, '0x2c7536e3605d9c16a7a3d7b1898e529396a65c23');
  assert.equal(recoverSigner('another message', wallet.personalSign('another message')), wallet.address);
  assert.notEqual(recoverSigner('a changed message', wallet.personalSign('another message')), wallet.address);
  assert.throws(() => recoverSigner('Some data', '0x1234'));
});

test('a wallet that signs the challenge gets a session for its address', async () => {
  const sessions = createSessions({ secret: SECRET });
  const wallet = createWallet();
  const { message, proof } = await sessions.challenge(wallet.address, PAGE);
  const lines = message.split('\n');
  assert.equal(lines[0], 'game.example wants you to sign in with your Ethereum account:');
  assert.equal(lines[1], checksumAddress(wallet.address));
  assert.ok(lines.includes('URI: https://game.example'));
  assert.ok(lines.includes('Chain ID: 143'));

  const result = await sessions.signIn({ message, proof, signature: wallet.personalSign(message) });
  assert.equal(result.ok, true);
  assert.equal(result.address, wallet.address);
  assert.equal(await sessions.addressOf(result.session), wallet.address);
});

test('a signature from another wallet, a rewritten challenge or a stale one opens nothing', async () => {
  let clock = Date.UTC(2026, 9, 9, 12);
  const sessions = createSessions({ secret: SECRET, now: () => clock });
  const holder = createWallet();
  const thief = createWallet();
  const { message, proof } = await sessions.challenge(holder.address, PAGE);

  // Someone else signs the holder's challenge.
  assert.deepEqual(await sessions.signIn({ message, proof, signature: thief.personalSign(message) }),
    { ok: false, reason: 'wrong_signer' });

  // The thief writes their own message naming the holder: we never issued it.
  const forged = message.replace('game.example', 'evil.example');
  assert.equal((await sessions.signIn({ message: forged, proof, signature: holder.personalSign(forged) })).reason, 'unknown_challenge');

  assert.equal((await sessions.signIn({ message, proof, signature: '0xnothing' })).reason, 'bad_signature');
  assert.equal((await sessions.signIn({ message, proof: 'not-a-proof', signature: holder.personalSign(message) })).reason, 'unknown_challenge');

  clock += 11 * 60 * 1000;
  assert.equal((await sessions.signIn({ message, proof, signature: holder.personalSign(message) })).reason, 'expired_challenge');
});

test('a session ends after a week and cannot be rewritten or made with another secret', async () => {
  let clock = Date.UTC(2026, 9, 9, 12);
  const sessions = createSessions({ secret: SECRET, now: () => clock });
  const wallet = createWallet();
  const other = createWallet();
  const { message, proof } = await sessions.challenge(wallet.address, PAGE);
  const { session } = await sessions.signIn({ message, proof, signature: wallet.personalSign(message) });

  // The same token with another address written in it.
  const rewritten = Buffer.from(JSON.stringify({ address: other.address, expiresAt: clock + 1e9 })).toString('base64url')
    + '.' + session.split('.')[1];
  assert.equal(await sessions.addressOf(rewritten), null);
  assert.equal(await sessions.addressOf('garbage'), null);
  assert.equal(await sessions.addressOf(undefined), null);
  assert.equal(await createSessions({ secret: SECRET + ' but another one' }).addressOf(session), null);

  clock += 6 * 24 * 3600 * 1000;
  assert.equal(await sessions.addressOf(session), wallet.address);
  clock += 2 * 24 * 3600 * 1000;
  assert.equal(await sessions.addressOf(session), null);
});

test('a short secret is refused', () => {
  assert.throws(() => createSessions({ secret: 'short' }));
});
