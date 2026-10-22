// The referee of the wager contract (contracts/src/ChogWager.sol): the key whose signature lets
// a player stake on a match and tells the contract who won. It signs three things, each as a
// wallet signs a message (the Ethereum header for 32 bytes over a digest): a ticket, a result,
// and the calling off of a match. The digests are the ones the contract computes; they carry the
// chain id and the contract's address, so a signature is good for one contract on one chain.
//
// Nothing here decides what is signed: a ticket is asked for by the wager routes of api.js, a
// result comes from the verdict of a match. The key never leaves this file and is never logged.

import { secp256k1 } from '@noble/curves/secp256k1.js';
import { keccak_256 } from '@noble/hashes/sha3.js';
import { isAddress } from './chain.js';

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const fromHex = hex => Uint8Array.from(hex.match(/.{2}/g) ?? [], pair => parseInt(pair, 16));

// What the three kinds of signature start with, as in the contract.
const TICKET = toHex(keccak_256(encoder.encode('CHOG_WAGER_TICKET')));
const RESULT = toHex(keccak_256(encoder.encode('CHOG_WAGER_RESULT')));
const VOID = toHex(keccak_256(encoder.encode('CHOG_WAGER_VOID')));
const SIGNED_MESSAGE_HEADER = encoder.encode('\x19Ethereum Signed Message:\n32');
const UINT256 = 2n ** 256n;
// The result of a match nobody won, as in the contract.
export const DRAW = 2;

// One 32-byte word of abi.encode for a whole number given as a BigInt, a number or decimal digits.
function word(value, name) {
  const whole = typeof value === 'bigint' || Number.isSafeInteger(value) || (typeof value === 'string' && /^[0-9]+$/.test(value));
  if (!whole || BigInt(value) < 0n || BigInt(value) >= UINT256) throw new Error(`${name} must be a whole number that fits 256 bits`);
  return BigInt(value).toString(16).padStart(64, '0');
}

function addressWord(address, name) {
  if (!isAddress(address)) throw new Error(`${name} must be 0x followed by 40 hex digits`);
  return address.slice(2).toLowerCase().padStart(64, '0');
}

function keyWord(matchKey) {
  if (typeof matchKey !== 'string' || !/^0x[0-9a-fA-F]{64}$/.test(matchKey)) throw new Error('matchKey must be 0x followed by 64 hex digits');
  return matchKey.slice(2).toLowerCase();
}

function teamWord(team, highest, name) {
  if (!Number.isInteger(team) || team < 0 || team > highest) throw new Error(`${name} must be a whole number from 0 to ${highest}`);
  return word(team, name);
}

// keccak256 of abi.encode of values that each fill one word.
const digestOf = (...words) => '0x' + toHex(keccak_256(fromHex(words.join(''))));

/** The key of a match in the contract: keccak256 of the name Photon's server gives it. */
export function matchKeyOf(gameId) {
  if (typeof gameId !== 'string' || gameId === '') throw new Error('gameId must be a text');
  return '0x' + toHex(keccak_256(encoder.encode(gameId)));
}

/** What a ticket signs: this player may stake this amount on this team in this match until the deadline. */
export function ticketDigest({ chainId, wager, matchKey, player, team, amount, deadline }) {
  const staked = word(amount, 'amount');
  if (BigInt('0x' + staked) === 0n) throw new Error('amount must be more than zero');
  return digestOf(TICKET, word(chainId, 'chainId'), addressWord(wager, 'wager'), keyWord(matchKey),
    addressWord(player, 'player'), teamWord(team, 1, 'team'), staked, word(deadline, 'deadline'));
}

/** What a result signs: this team won this match (0 or 1), or nobody did (2). */
export function resultDigest({ chainId, wager, matchKey, winnerTeam, genesisPlayers }) {
  const fixed = [RESULT, word(chainId, 'chainId'), addressWord(wager, 'wager'), keyWord(matchKey), teamWord(winnerTeam, DRAW, 'winnerTeam')];
  // Version 1 of the contract signs the result alone; version 2 (22 October 2026) signs with it the
  // wallets that played a Genesis, an address[]: abi.encode puts its offset (six words in) among
  // the fixed words, then its length and its addresses.
  if (genesisPlayers === undefined) return digestOf(...fixed);
  if (!Array.isArray(genesisPlayers) || genesisPlayers.length > MAX_GENESIS_PLAYERS) throw new Error('genesisPlayers is a list of at most six addresses');
  return digestOf(...fixed, word(6 * 32, 'offset'), word(genesisPlayers.length, 'length'),
    ...genesisPlayers.map(address => addressWord(address, 'genesisPlayers')));
}

/** A result names no more Genesis players than a match has players (ChogWager.MAX_GENESIS_PLAYERS). */
export const MAX_GENESIS_PLAYERS = 6;

/** What calling a match off signs. */
export function voidDigest({ chainId, wager, matchKey }) {
  return digestOf(VOID, word(chainId, 'chainId'), addressWord(wager, 'wager'), keyWord(matchKey));
}

/**
 * @param {object} options
 * @param {string} options.privateKey 64 hex digits, with or without 0x; never sent anywhere
 * @param {number} options.chainId the chain the wager contract is on
 * @param {string} options.wager the deployed wager contract
 */
export function createReferee({ privateKey, chainId, wager }) {
  if (typeof privateKey !== 'string' || !/^(0x)?[0-9a-fA-F]{64}$/.test(privateKey)) {
    throw new Error('The referee key must be 64 hex digits');
  }
  const secretKey = fromHex(privateKey.replace(/^0x/, ''));
  if (!secp256k1.utils.isValidSecretKey(secretKey)) throw new Error('The referee key is not a key of the curve');
  if (!Number.isSafeInteger(chainId) || chainId < 1) throw new Error('The chain id of the wager must be a whole number');
  if (!isAddress(wager)) throw new Error('The wager address must be 0x followed by 40 hex digits');
  const contract = wager.toLowerCase();
  const address = '0x' + toHex(keccak_256(secp256k1.getPublicKey(secretKey, false).slice(1)).slice(-20));

  // The 65 bytes the contract reads: r, s, then 27 or 28, with s in the lower half of the curve.
  function sign(digest) {
    const message = keccak_256(Uint8Array.from([...SIGNED_MESSAGE_HEADER, ...fromHex(digest.slice(2))]));
    const signature = secp256k1.sign(message, secretKey, { prehash: false, format: 'recovered', lowS: true });
    return '0x' + toHex(signature.slice(1)) + (27 + signature[0]).toString(16);
  }

  return {
    /** The address the contract must know as its referee. */
    address,
    wager: contract,
    chainId,
    matchKeyOf,
    /** amount and deadline as decimal digits or BigInt; deadline in seconds, as the chain counts time. */
    signTicket: ({ matchKey, player, team, amount, deadline }) => sign(ticketDigest({ chainId, wager: contract, matchKey, player, team, amount, deadline })),
    signResult: ({ matchKey, winnerTeam, genesisPlayers }) =>
      sign(resultDigest({ chainId, wager: contract, matchKey, winnerTeam, genesisPlayers })),
    signVoid: ({ matchKey }) => sign(voidDigest({ chainId, wager: contract, matchKey })),
  };
}
