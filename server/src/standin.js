// Stand-ins: an NFT of another collection, held by a wallet the owner of the game names, that lets
// that wallet play one Genesis it does not hold, to film or test the game with that Genesis after
// it was sold. The holding of the stand-in is read on chain like a Genesis; nothing else changes
// in the match. A stand-in never writes a career (careers-onchain.js leaves it out), never lends
// and never rents the Genesis out: those need the Genesis itself on chain. Off unless the
// services are started with STAND_INS, so the public game has none by default.
//
//   STAND_INS="0xCollection:tokenId=genesisTokenId[,...]"

import { isAddress, LAST_TOKEN_ID } from './chain.js';

const OWNER_OF = '6352211e'; // ownerOf(uint256)
const word = value => BigInt(value).toString(16).padStart(64, '0');

/** The stand-ins a setting names; an empty list for an empty setting. Throws on a malformed one. */
export function parseStandIns(text) {
  return String(text ?? '').split(',').map(part => part.trim()).filter(Boolean).map(part => {
    const match = /^(0x[0-9a-fA-F]{40}):([0-9]+)=([0-9]+)$/.exec(part);
    if (!match) throw new Error(`STAND_INS: "${part}" is not 0xCollection:tokenId=genesisTokenId`);
    const genesis = Number(match[3]);
    if (!Number.isInteger(genesis) || genesis < 1 || genesis > LAST_TOKEN_ID) throw new Error(`STAND_INS: no Genesis ${match[3]}`);
    return { collection: match[1].toLowerCase(), tokenId: BigInt(match[2]), genesis };
  });
}

/**
 * @param {object} options
 * @param {ReturnType<import('./chain.js').createChain>} options.chain
 * @param {string} [options.setting] the STAND_INS setting
 */
export function createStandIns({ chain, setting = '' }) {
  const standIns = parseStandIns(setting);
  if (standIns.length > 0) console.log(`Stand-ins: ${standIns.map(one => `${one.collection}:${one.tokenId} plays Genesis ${one.genesis}`).join(', ')}`);

  async function holderOf(standIn) {
    const answer = await chain.call(standIn.collection, '0x' + OWNER_OF + word(standIn.tokenId)).catch(() => null);
    return typeof answer === 'string' && answer.length === 66 ? '0x' + answer.slice(-40).toLowerCase() : null;
  }

  /** The Genesis an address may play through the stand-ins it holds now. */
  async function genesisOf(address) {
    if (standIns.length === 0 || !isAddress(address)) return [];
    const who = address.toLowerCase();
    const holders = await Promise.all(standIns.map(holderOf));
    return standIns.filter((_, index) => holders[index] === who).map(standIn => standIn.genesis);
  }

  /** Whether an address plays that Genesis through a stand-in it holds now. */
  async function standsIn(address, genesis) {
    return (await genesisOf(address)).includes(genesis);
  }

  return { genesisOf, standsIn, enabled: standIns.length > 0 };
}
