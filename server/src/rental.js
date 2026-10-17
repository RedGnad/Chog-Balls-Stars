// What the game reads from the rental contract (contracts/src/ChogRental.sol, version 2): which
// Genesis their holders offer for rent and at what price a day in $CHOG, which Genesis an address
// rents, and the money each party may take out (what a holder earned, what a renter is owed back).
// Offering, renting, ending early and claiming are transactions the player's wallet sends; the
// only one these services send is the closing of a rental whose Genesis was sold, so the renter is
// paid back the time not delivered and the seller is paid no further (the contract cannot see a
// sale by itself).
//
// There is no rental until the contract is deployed and its address given to the services.

import { keccak_256 } from '@noble/hashes/sha3.js';
import { isAddress, LAST_TOKEN_ID } from './chain.js';
import { CHOG_TOKEN } from './shop.js';

// The longest time a renter can be paid up, fixed in the contract (MAX_DAYS).
export const MAX_RENTAL_DAYS = 30;

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const selector = signature => toHex(keccak_256(encoder.encode(signature)).slice(0, 4));
const word = value => BigInt(value).toString(16).padStart(64, '0');

const BURNED_SHARE = selector('burnedShare()');
const STANDING_OFFERS = selector('standingOffers()');
const RENTALS_OF = selector('rentalsOf(address)');
const IS_RENTING = selector('isRenting(uint256,address)');
const RENTALS_ON_RECORD = selector('rentalsOnRecord()');
const EARNINGS = selector('earnings(address)');
const REFUNDS = selector('refunds(address)');
const OFFER_OF = selector('offerOf(uint256)');
const CLOSE = selector('close(uint256)');
const BALANCE_OF = selector('balanceOf(address)');
// A rental on record is seven words: tokenId, renter, lender, until, escrow, earnedSinceSettled, counting.
const RECORD_WORDS = 7;
const OFFERS_KEPT_MS = 10000;

/**
 * The words of one of the lists a contract answered, as hex without 0x: `position` is the rank
 * of the list among the answered values. Null when the answer is not shaped like that.
 */
export function listOf(answer, position) {
  if (typeof answer !== 'string' || !answer.startsWith('0x')) return null;
  const raw = answer.slice(2);
  const start = parseInt(raw.slice(position * 64, (position + 1) * 64), 16) * 2;
  const length = parseInt(raw.slice(start, start + 64), 16);
  if (!Number.isInteger(start) || !Number.isInteger(length) || raw.length < start + 64 + length * 64) return null;
  const words = [];
  for (let index = 0; index < length; index++) words.push(raw.slice(start + 64 + index * 64, start + 128 + index * 64));
  return words;
}

/**
 * @param {object} options
 * @param {ReturnType<import('./chain.js').createChain>} options.chain
 * @param {string} [options.address] the deployed rental contract; without it there is no rental
 * @param {() => number} [options.now] milliseconds
 */
export function createRental({ chain, address, now = Date.now }) {
  if (address !== undefined && address !== '' && !isAddress(address)) throw new Error('The rental address must be 0x followed by 40 hex digits');
  const rental = address ? address.toLowerCase() : null;
  let burnedShare = null;
  let listed = null;

  /** What a game page needs to offer or rent; {enabled: false} when there is no rental. */
  async function describe() {
    if (!rental) return { enabled: false };
    if (burnedShare === null) {
      // Fixed at deployment, so it is read once.
      const answer = await chain.call(rental, '0x' + BURNED_SHARE);
      if (typeof answer !== 'string' || answer.length !== 66) throw new Error(`burnedShare() returned ${String(answer).slice(0, 80)}`);
      burnedShare = Number(BigInt(answer));
    }
    // burnedShare is in basis points: 1000 is a tenth of every payment.
    return { enabled: true, address: rental, token: CHOG_TOKEN.toLowerCase(), maxDays: MAX_RENTAL_DAYS, burnedShare };
  }

  /**
   * Every Genesis that can be rented now: token id, price a day in the smallest unit of $CHOG
   * as a decimal text, and the holder who is paid. Read on the chain and kept for a short time.
   */
  async function offers() {
    if (!rental) return [];
    if (listed && now() - listed.readAt < OFFERS_KEPT_MS) return listed.offers;
    const answer = await chain.call(rental, '0x' + STANDING_OFFERS);
    const tokenIds = listOf(answer, 0);
    const prices = listOf(answer, 1);
    const lenders = listOf(answer, 2);
    if (!tokenIds || !prices || !lenders || prices.length !== tokenIds.length || lenders.length !== tokenIds.length) {
      throw new Error(`standingOffers() returned ${String(answer).slice(0, 80)}`);
    }
    listed = {
      readAt: now(),
      offers: tokenIds
        .map((tokenId, index) => ({
          tokenId: parseInt(tokenId, 16),
          pricePerDay: BigInt('0x' + prices[index]).toString(),
          lender: '0x' + lenders[index].slice(-40),
        }))
        .sort((a, b) => a.tokenId - b.tokenId),
    };
    return listed.offers;
  }

  /** The Genesis an address may play as their renter now, each with the second its rental ends. */
  async function rentedBy(renter) {
    if (!rental) return [];
    const answer = await chain.call(rental, '0x' + RENTALS_OF + word(renter));
    const tokenIds = listOf(answer, 0);
    const until = listOf(answer, 1);
    if (!tokenIds || !until || until.length !== tokenIds.length) throw new Error(`rentalsOf() returned ${String(answer).slice(0, 80)}`);
    return tokenIds
      .map((tokenId, index) => ({ tokenId: parseInt(tokenId, 16), until: parseInt(until[index], 16) }))
      .sort((a, b) => a.tokenId - b.tokenId);
  }

  /** Whether an address rents that Genesis at this moment: a read of its own, for anything that credits. */
  async function isRenting(tokenId, renter) {
    if (!rental || !Number.isInteger(tokenId) || tokenId < 1 || tokenId > LAST_TOKEN_ID) return false;
    const answer = await chain.call(rental, '0x' + IS_RENTING + word(tokenId) + word(renter));
    return typeof answer === 'string' && answer.length === 66 && BigInt(answer) === 1n;
  }

  /**
   * Every rental on record, running or waiting to be closed: token id, renter, the holder who is
   * paid, the second it ends, the payment held, the part earned since the last settlement, and
   * whether it still counts (its time runs and that holder still holds the Genesis).
   */
  async function records() {
    if (!rental) return [];
    const answer = await chain.call(rental, '0x' + RENTALS_ON_RECORD);
    if (typeof answer !== 'string' || !answer.startsWith('0x')) throw new Error(`rentalsOnRecord() returned ${String(answer).slice(0, 80)}`);
    const raw = answer.slice(2);
    const start = parseInt(raw.slice(0, 64), 16) * 2;
    const count = parseInt(raw.slice(start, start + 64), 16);
    if (!Number.isInteger(count) || raw.length < start + 64 + count * RECORD_WORDS * 64) throw new Error('rentalsOnRecord() is cut short');
    return Array.from({ length: count }, (_, index) => {
      const at = start + 64 + index * RECORD_WORDS * 64;
      const field = position => raw.slice(at + position * 64, at + (position + 1) * 64);
      return {
        tokenId: parseInt(field(0), 16),
        renter: '0x' + field(1).slice(-40),
        lender: '0x' + field(2).slice(-40),
        until: parseInt(field(3), 16),
        escrow: BigInt('0x' + field(4)).toString(),
        earned: BigInt('0x' + field(5)).toString(),
        counting: BigInt('0x' + field(6)) === 1n,
      };
    });
  }

  /**
   * What an address may see of its money in the rental: what it earned as a holder and may claim
   * (before the burned share), what it is owed back as a renter, the rentals it is paid for, and
   * those it pays for with the price a day it would add days at.
   */
  async function account(address) {
    if (!rental || !isAddress(address)) return { enabled: Boolean(rental) };
    const who = address.toLowerCase();
    const [earnings, refunds, onRecord, balance] = await Promise.all([
      chain.call(rental, '0x' + EARNINGS + word(who)),
      chain.call(rental, '0x' + REFUNDS + word(who)),
      records(),
      // The $CHOG the address holds, for the menu; the rest of the account does not wait on it.
      chain.call(CHOG_TOKEN, '0x' + BALANCE_OF + word(who)).catch(() => null),
    ]);
    const lent = onRecord.filter(record => record.lender === who);
    const rented = onRecord.filter(record => record.renter === who);
    // What the holder may claim now: what was moved to their earnings plus what their rentals earned since.
    const claimable = lent.reduce((sum, record) => sum + BigInt(record.earned), BigInt(earnings ?? '0x0'));
    // A renter whose Genesis was sold, or whose time is over, gets the rest back once it is closed: claiming closes it.
    const owedBack = rented
      .filter(record => !record.counting)
      .reduce((sum, record) => sum + BigInt(record.escrow) - BigInt(record.earned), BigInt(refunds ?? '0x0'));
    // The price a day of each, as offerOf answers it (three words: whether the offer stands, its
    // price, its author): what a renter adds days at, what a holder offers it at.
    const tokenIds = [...new Set([...lent, ...rented].map(record => record.tokenId))].sort((a, b) => a - b);
    const answers = await Promise.all(tokenIds.map(tokenId => chain.call(rental, '0x' + OFFER_OF + word(tokenId))));
    const priceOf = new Map(tokenIds.map((tokenId, index) => {
      const answer = answers[index];
      return [tokenId, typeof answer === 'string' && answer.length >= 2 + 64 * 3 ? BigInt('0x' + answer.slice(66, 130)).toString() : '0'];
    }));
    return {
      enabled: true,
      address: who,
      balance: typeof balance === 'string' && /^0x[0-9a-fA-F]+$/.test(balance) ? BigInt(balance).toString() : '',
      earned: claimable.toString(),
      owedBack: owedBack.toString(),
      // The token ids a claim must name so that their rentals are settled or closed first.
      toSettle: tokenIds,
      lent: lent.map(({ tokenId, renter, until, counting }) => ({ tokenId, renter, until, counting, pricePerDay: priceOf.get(tokenId) })),
      rented: rented.map(({ tokenId, lender, until, counting }) => ({ tokenId, lender, until, counting, pricePerDay: priceOf.get(tokenId) })),
    };
  }

  /**
   * Closes, with the referee's key, every rental on record whose Genesis left the holder it pays
   * while its time still ran: the renter is then owed the time not delivered. Answers the token
   * ids it closed.
   */
  async function closeSold(sender) {
    if (!rental || !sender) return [];
    const nowSeconds = Math.floor(now() / 1000);
    const sold = (await records()).filter(record => !record.counting && record.until > nowSeconds);
    const closed = [];
    for (const record of sold) {
      try {
        const sent = await sender.transact({ to: rental, data: '0x' + CLOSE + word(record.tokenId), purpose: `the close of the rental of ${record.tokenId}` });
        if (sent.ok !== false) closed.push(record.tokenId);
      } catch (error) {
        console.warn(`The rental of ${record.tokenId} could not be closed: ${error.message}`);
      }
    }
    return closed;
  }

  return { describe, offers, rentedBy, isRenting, records, account, closeSold, address: rental };
}
