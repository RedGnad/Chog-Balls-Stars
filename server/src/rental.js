// What the game reads from the rental contract (contracts/src/ChogRental.sol): which Genesis
// their holders offer for rent and at what price a day in $CHOG, and which Genesis an address
// has rented. The services only read: offering, taking an offer back and renting are
// transactions the player's wallet sends to the contract, and the chain is the record of them.
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

  return { describe, offers, rentedBy, isRenting, address: rental };
}
