// What the game reads from the shop contract (contracts/src/ChogShop.sol): the price of the
// item it sells, and which Genesis have it. An item is bought with $CHOG for a token id and
// stays with that Genesis. The services only read: a purchase is a transaction the player's
// wallet sends to the contract, and the chain is the record of it.
//
// There is no shop until its contract is deployed and its address given to the services.

import { keccak_256 } from '@noble/hashes/sha3.js';
import { isAddress, LAST_TOKEN_ID } from './chain.js';

// The one item sold today: the second form of a Genesis's attack.
export const SECOND_FORM_ITEM = 1;
export const CHOG_TOKEN = '0x350035555E10d9AfAF1566AaebfCeD5BA6C27777';

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const selector = signature => toHex(keccak_256(encoder.encode(signature)).slice(0, 4));
const word = value => BigInt(value).toString(16).padStart(64, '0');

const ITEMS = selector('items(uint256)');
const OWNED = selector('owned(uint256,uint256)');
const PRICE_KEPT_MS = 20000;

/**
 * @param {object} options
 * @param {ReturnType<import('./chain.js').createChain>} options.chain
 * @param {string} [options.address] the deployed shop; without it there is no shop
 * @param {() => number} [options.now] milliseconds
 */
export function createShop({ chain, address, now = Date.now }) {
  if (address !== undefined && address !== '' && !isAddress(address)) throw new Error('The shop address must be 0x followed by 40 hex digits');
  const shop = address ? address.toLowerCase() : null;
  let item = null;

  /** Price and state of the item, read from the contract and kept for a short time. */
  async function readItem() {
    if (item && now() - item.readAt < PRICE_KEPT_MS) return item;
    const answer = await chain.call(shop, '0x' + ITEMS + word(SECOND_FORM_ITEM));
    if (typeof answer !== 'string' || answer.length !== 2 + 128) throw new Error(`items(${SECOND_FORM_ITEM}) returned ${String(answer).slice(0, 80)}`);
    item = {
      // In the smallest unit of $CHOG, as a decimal text: it does not fit a JSON number.
      price: BigInt('0x' + answer.slice(2, 66)).toString(),
      forSale: BigInt('0x' + answer.slice(66, 130)) === 1n,
      readAt: now(),
    };
    return item;
  }

  /** What a game page needs to offer the purchase; {enabled: false} when there is no shop. */
  async function describe() {
    if (!shop) return { enabled: false };
    const { price, forSale } = await readItem();
    return { enabled: forSale, address: shop, token: CHOG_TOKEN.toLowerCase(), itemId: SECOND_FORM_ITEM, price };
  }

  /** Those of the given token ids whose Genesis has the item; none when there is no shop. */
  async function ownedAmong(tokenIds) {
    const asked = tokenIds.filter(tokenId => Number.isInteger(tokenId) && tokenId >= 1 && tokenId <= LAST_TOKEN_ID);
    if (!shop || asked.length === 0) return [];
    const answers = await chain.callMany(asked.map(tokenId => ({ to: shop, data: '0x' + OWNED + word(tokenId) + word(SECOND_FORM_ITEM) })));
    // A call that failed, or that answered nothing, is a Genesis without the item.
    return asked.filter((tokenId, index) => typeof answers[index] === 'string' && answers[index].length === 66 && BigInt(answers[index]) === 1n);
  }

  return { describe, ownedAmong, address: shop };
}
