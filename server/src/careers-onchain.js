// The career of each Genesis kept on Monad mainnet (contracts/src/ChogCareers.sol), keyed by
// token id, so it follows the Genesis when the token changes hands and outlives any restart of
// these services. It is written by the referee after a match was played again from the record
// Photon's server made (judge.js): a player's page never decides it. It answers the same as the
// careers kept in a file (career.js), mastery and badges read from the counters by the same rules,
// so the game reads it the same way.
//
// The referee also records there that a holder stopped lending a Genesis, so free passes handed
// out before are revoked on chain.

import { keccak_256 } from '@noble/hashes/sha3.js';
import { BADGES, LAST_TOKEN_ID, masteryOf } from './career.js';
import { isAddress } from './chain.js';
import { DRAW, matchKeyOf } from './referee.js';

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const selector = signature => toHex(keccak_256(encoder.encode(signature)).slice(0, 4));
const word = value => BigInt(value).toString(16).padStart(64, '0');

const RECORD = selector('record(bytes32,uint8,(uint16,uint8,uint8)[])');
const PACKED = selector('packedCareers(uint256,uint256)');
const STOP_LENDING = selector('stopLending(uint256)');
const LENDING_STOPPED_AT = selector('lendingStoppedAt(uint256)');

// How a Genesis was played, as the contract counts it.
export const PLAYED_BY = { holder: 0, renter: 1, guest: 2 };
// At most this many Genesis in one record, as in the contract (MAX_ENTRIES).
const MAX_ENTRIES = 16;
// At most one record per Genesis in this time: a match lasts longer. Bounds what a player who
// starts matches in a loop costs the referee.
const MIN_SECONDS_BETWEEN_MATCHES = 60;
// At most this many records a day, whatever happens: the referee pays each one.
const MAX_RECORDS_PER_DAY = 2000;
const KEPT_MS = 15000;

/** The counters of one packed career word (ChogCareers, "packedCareers"). */
export function unpackCareer(hexWord) {
  let value = BigInt('0x' + (hexWord || '0'));
  const take = bits => {
    const part = value & ((1n << BigInt(bits)) - 1n);
    value >>= BigInt(bits);
    return Number(part);
  };
  return {
    matches: take(32), wins: take(32), draws: take(32), rentedMatches: take(32), guestMatches: take(32),
    streak: take(16), bestStreak: take(16), lastPlayedAt: take(64),
  };
}

/** The words of a uint256[] the contract answered, as hex without 0x. */
function wordsOf(answer) {
  if (typeof answer !== 'string' || !answer.startsWith('0x')) return null;
  const raw = answer.slice(2);
  const start = parseInt(raw.slice(0, 64), 16) * 2;
  const count = parseInt(raw.slice(start, start + 64), 16);
  if (!Number.isInteger(count) || raw.length < start + 64 + count * 64) return null;
  return Array.from({ length: count }, (_, index) => raw.slice(start + 64 + index * 64, start + 128 + index * 64));
}

/** The calldata of record(matchKey, winnerTeam, entries), the entries in ascending token id. */
export function recordData(matchKey, winnerTeam, entries) {
  // abi.encode(bytes32, uint8, (uint16,uint8,uint8)[]): two static words, the offset of the array
  // (three words in), its length, then three words per entry.
  const head = matchKey.slice(2).padStart(64, '0') + word(winnerTeam) + word(96);
  const body = word(entries.length) + entries.map(entry => word(entry.tokenId) + word(entry.team) + word(entry.playedBy)).join('');
  return '0x' + RECORD + head + body;
}

/**
 * What a judged match records: one entry per Genesis played by a player the webhooks admitted
 * with a right to it (holder, renter or guest), its team as the replay found it. A Genesis
 * played by two players of the match counts once. Null when there is nothing to record.
 * @param {object} verdict the verdict judge.js kept: winnerTeam 0, 1 or DRAW, players [{player, team, tokenId}]
 * @param {object[]} admissions the admissions of the match (matches.js): {player, tokenId, right, standIn}
 */
export function entriesOf(verdict, admissions) {
  if (!verdict || verdict.void || ![0, 1, DRAW].includes(verdict.winnerTeam) || !Array.isArray(verdict.players)) return null;
  const entries = new Map();
  for (const played of verdict.players) {
    if (!Number.isInteger(played.tokenId) || played.tokenId < 1 || played.tokenId > LAST_TOKEN_ID) continue;
    if (played.team !== 0 && played.team !== 1) continue;
    const admission = admissions.findLast(candidate => candidate.player === played.player && candidate.tokenId === played.tokenId);
    // A Genesis played without a right (never admitted with it) or by a stand-in counts for nobody's career.
    if (!admission || admission.standIn || !(admission.right in PLAYED_BY)) continue;
    if (!entries.has(played.tokenId)) entries.set(played.tokenId, { tokenId: played.tokenId, team: played.team, playedBy: PLAYED_BY[admission.right] });
  }
  const sorted = [...entries.values()].sort((a, b) => a.tokenId - b.tokenId).slice(0, MAX_ENTRIES);
  return sorted.length > 0 ? sorted : null;
}

/**
 * @param {object} options
 * @param {ReturnType<import('./chain.js').createChain>} options.chain reads Monad mainnet
 * @param {string} options.address the deployed ChogCareers
 * @param {ReturnType<import('./sender.js').createSender>} options.sender sends with the referee's key
 * @param {() => number} [options.now] milliseconds
 */
export function createChainCareers({ chain, address, sender, now = Date.now }) {
  if (!isAddress(address)) throw new Error('The careers address must be 0x followed by 40 hex digits');
  const careers = address.toLowerCase();
  let every = null;
  const lastRecorded = new Map();
  let today = { day: -1, records: 0 };

  // Every career in one read (about a million gas of eth_call), kept a few seconds.
  async function all() {
    if (every && now() - every.readAt < KEPT_MS) return every.careers;
    const words = wordsOf(await chain.call(careers, '0x' + PACKED + word(1) + word(LAST_TOKEN_ID)));
    if (!words || words.length !== LAST_TOKEN_ID) throw new Error('packedCareers() did not answer every Genesis');
    every = { readAt: now(), careers: words.map(unpackCareer) };
    return every.careers;
  }

  function describe(tokenId, career) {
    return {
      tokenId,
      matches: career.matches,
      wins: career.wins,
      draws: career.draws,
      guestMatches: career.guestMatches,
      rentedMatches: career.rentedMatches,
      bestStreak: career.bestStreak,
      // Every match on chain was played again by the judge before it was written.
      confirmed: career.matches,
      lastPlayedAt: career.lastPlayedAt * 1000,
      mastery: masteryOf(career),
      badges: BADGES.filter(badge => badge.earned(career)).map(({ id, name }) => ({ id, name })),
    };
  }

  /** Careers of the given token ids, in that order; a Genesis that never played has zeros. */
  async function get(tokenIds) {
    const known = await all();
    return tokenIds.map(tokenId => describe(tokenId, known[tokenId - 1] ?? unpackCareer('0')));
  }

  /** The Genesis with the most wins, then the most matches, then the lowest token id. */
  async function top(limit) {
    const known = await all();
    return known
      .map((career, index) => describe(index + 1, career))
      .filter(career => career.matches > 0)
      .sort((a, b) => b.wins - a.wins || b.matches - a.matches || a.tokenId - b.tokenId)
      .slice(0, limit);
  }

  /** A page's own report of a match: nothing is credited from it, the replay decides. */
  async function record({ tokenId }) {
    if (!Number.isInteger(tokenId) || tokenId < 1 || tokenId > LAST_TOKEN_ID) return { credited: false, reason: 'bad_token' };
    const [career] = await get([tokenId]);
    return { credited: false, reason: 'recorded_after_the_replay', career };
  }

  /**
   * Writes a judged match on chain. Answers what was sent, or why nothing was: no Genesis with a
   * right in it, a Genesis that played less than a minute ago, or the day's records spent.
   */
  async function recordMatch(gameId, verdict, admissions) {
    const entries = entriesOf(verdict, admissions);
    if (!entries) return { recorded: false, reason: 'no_genesis' };
    const at = now();
    const day = Math.floor(at / 86400000);
    if (today.day !== day) today = { day, records: 0 };
    if (today.records >= MAX_RECORDS_PER_DAY) return { recorded: false, reason: 'daily_limit' };
    const counted = entries.filter(entry => at - (lastRecorded.get(entry.tokenId) ?? 0) >= MIN_SECONDS_BETWEEN_MATCHES * 1000);
    if (counted.length === 0) return { recorded: false, reason: 'too_soon' };
    today.records += 1;
    for (const entry of counted) lastRecorded.set(entry.tokenId, at);
    const sent = await sender.transact({ to: careers, data: recordData(matchKeyOf(gameId), verdict.winnerTeam, counted), purpose: `the career record of ${gameId}` });
    every = null;
    return { recorded: sent.ok !== false, hash: sent.hash, entries: counted };
  }

  /** Every pass handed out for this Genesis until now stops counting. The caller checked the holder. */
  async function stopLending(tokenId) {
    const sent = await sender.transact({ to: careers, data: '0x' + STOP_LENDING + word(tokenId), purpose: `the stop of lending ${tokenId}` });
    if (sent.ok === false) throw new Error(`stopLending(${tokenId}) reverted`);
    return { tokenId, stoppedAt: now() };
  }

  /** When lending of this Genesis was last stopped, in milliseconds; 0 if it never was. */
  async function lendingStoppedAt(tokenId) {
    const answer = await chain.call(careers, '0x' + LENDING_STOPPED_AT + word(tokenId));
    if (typeof answer !== 'string' || answer.length !== 66) throw new Error(`lendingStoppedAt() returned ${String(answer).slice(0, 80)}`);
    // The chain counts whole seconds: the end of that second, so no pass handed out before the stop survives it.
    const seconds = Number(BigInt(answer));
    return seconds === 0 ? 0 : (seconds + 1) * 1000 - 1;
  }

  return { get, top, record, recordMatch, stopLending, lendingStoppedAt, address: careers };
}
