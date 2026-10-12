// The stakes of a match (contracts/src/ChogWager.sol), seen from the game services. The $CHOG
// never passes through here: players stake on the contract and are paid by it. The services do
// three things, each with the referee's signature (referee.js):
//
//   - a ticket: a wallet the webhooks admitted to a match may stake a given amount on a team;
//   - the lock: when every ticket handed out was used or has run out, the stakes are closed and
//     who staked is written down. The match is meant to begin only then;
//   - the decision, once the match was played again and judged: the team that won, or the
//     match called off. It is called off whenever the stakes cannot be trusted: never locked,
//     locked after the match began, a stake that is not in the book, a staker who did not play
//     in the team staked on, a match that could not be judged. Called off means everyone gets
//     their stake back, so the doubt never pays anyone.
//
// A decision is made once for a match and kept: the referee never signs two different ones.
//
// There is no wager until the contract is deployed and the services hold its referee's key.

import { keccak_256 } from '@noble/hashes/sha3.js';
import { listOf } from './rental.js';
import { matchKeyOf } from './referee.js';
import { CHOG_TOKEN } from './shop.js';

// How long a ticket can be used, and how far behind ours the chain's clock is allowed to be.
export const TICKET_SECONDS = 120;
const CLOCK_MARGIN_SECONDS = 15;
// As in the contract.
const REFUND_AFTER_SECONDS = 3600;
const STATES = ['none', 'open', 'settled', 'refunded'];
const SETTINGS_KEPT_MS = 60000;
const UINT128 = 2n ** 128n;

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const selector = signature => toHex(keccak_256(encoder.encode(signature)).slice(0, 4));

const REFEREE = selector('referee()');
const BURNED_SHARE = selector('burnedShare()');
const MATCH_OF = selector('matchOf(bytes32)');
const STAKERS_OF = selector('stakersOf(bytes32)');

const sameBook = (a, b) => a.length === b.length
  && a.every(one => b.some(other => other.address === one.address && other.team === one.team));

/**
 * @param {object} options
 * @param {ReturnType<import('./chain.js').createChain>} options.chain
 * @param {ReturnType<import('./referee.js').createReferee>} [options.referee] without it there is no wager
 * @param {ReturnType<import('./matches.js').createMatches>} options.matches
 * @param {() => number} [options.now] milliseconds
 */
export function createWager({ chain, referee, matches, now = Date.now }) {
  const contract = referee?.wager ?? null;
  let settings = null;

  /** What a game page needs to stake; {enabled: false} when there is no wager or it is set up wrong. */
  async function describe() {
    if (!contract) return { enabled: false };
    if (!settings || now() - settings.readAt > SETTINGS_KEPT_MS) {
      const [signer, share] = await chain.callMany([{ to: contract, data: '0x' + REFEREE }, { to: contract, data: '0x' + BURNED_SHARE }]);
      if (typeof signer !== 'string' || signer.length !== 66 || typeof share !== 'string' || share.length !== 66) {
        throw new Error(`The wager contract ${contract} did not answer referee() and burnedShare()`);
      }
      settings = { readAt: now(), referee: '0x' + signer.slice(-40).toLowerCase(), burnedShare: Number(BigInt(share)) };
    }
    // The contract only listens to the referee it knows: with another key here no ticket would work.
    if (settings.referee !== referee.address) return { enabled: false, reason: 'the contract knows another referee' };
    return {
      enabled: true, address: contract, token: CHOG_TOKEN.toLowerCase(), chainId: referee.chainId, referee: referee.address,
      burnedShare: settings.burnedShare, ticketSeconds: TICKET_SECONDS, refundAfterSeconds: REFUND_AFTER_SECONDS,
    };
  }

  /** Where the stakes of a match stand on the chain: its state, what each stakes, who staked on which team. */
  async function onChain(matchKey) {
    const key = matchKey.slice(2);
    const [state, stakers] = await chain.callMany([{ to: contract, data: '0x' + MATCH_OF + key }, { to: contract, data: '0x' + STAKERS_OF + key }]);
    const players = listOf(stakers, 0);
    const teams = listOf(stakers, 1);
    if (typeof state !== 'string' || state.length !== 2 + 5 * 64 || !players || !teams || players.length !== teams.length) {
      throw new Error(`The wager contract ${contract} did not answer for the match ${matchKey}`);
    }
    return {
      state: STATES[Number(BigInt('0x' + state.slice(2, 66)))] ?? 'unknown',
      amount: BigInt('0x' + state.slice(66, 130)).toString(),
      firstStakeAt: Number(BigInt('0x' + state.slice(130, 194))),
      stakers: players.map((player, index) => ({ address: '0x' + player.slice(-40), team: parseInt(teams[index], 16) })),
    };
  }

  /**
   * A ticket for the match this wallet is in: the referee's word that it may stake "amount"
   * (the smallest unit of $CHOG, as decimal digits) on "team". The first ticket of a match
   * fixes what everyone stakes. Answers {refused} when it may not.
   */
  async function ticket({ address, team, amount }) {
    if (!contract) return { refused: 'no_wager' };
    if (team !== 0 && team !== 1) return { refused: 'bad_team' };
    if (typeof amount !== 'string' || !/^[1-9][0-9]*$/.test(amount) || BigInt(amount) >= UINT128) return { refused: 'bad_amount' };
    const match = await matches.currentOf(address);
    if (!match) return { refused: 'not_in_a_match' };
    const deadline = Math.floor(now() / 1000) + TICKET_SECONDS;
    const granted = await matches.ticket(match.gameId, { address, team, amount, deadline });
    if (granted.refused) return granted;
    const matchKey = matchKeyOf(match.gameId);
    return {
      gameId: match.gameId, matchKey, team, amount, deadline,
      signature: referee.signTicket({ matchKey, player: address, team, amount, deadline }),
    };
  }

  /**
   * Locks the stakes of the match this wallet is in: its page asks when its player starts the
   * match. Answers {locked: true, book} once nobody can stake any more, or {locked: false,
   * waitSeconds} while a ticket is still out. Any wallet let into the match may ask, staker or
   * not: locking early only closes the stakes, and a lock that comes late calls the match off.
   */
  async function lock({ address }) {
    if (!contract) return { refused: 'no_wager' };
    const match = await matches.currentOf(address);
    if (!match) return { refused: 'not_in_a_match' };
    const { stakers } = await onChain(matchKeyOf(match.gameId));
    return matches.lock(match.gameId, { stakers, nowSeconds: Math.floor(now() / 1000), marginSeconds: CLOCK_MARGIN_SECONDS });
  }

  /** What the page of a wallet shows of the stakes of the match it is in; {inMatch: false} when it is in none. */
  async function mine({ address }) {
    if (!contract) return { enabled: false };
    const match = await matches.currentOf(address);
    if (!match) return { enabled: true, inMatch: false };
    const matchKey = matchKeyOf(match.gameId);
    return {
      enabled: true, inMatch: true, gameId: match.gameId, roomName: match.app?.roomName ?? null, matchKey,
      amount: match.tickets.amount, lockedAt: match.tickets.lockedAt, stakes: await onChain(matchKey),
    };
  }

  // What is signed for a judged match. Every doubt calls the match off.
  async function decide(match) {
    const matchKey = matchKeyOf(match.gameId);
    const calledOff = why => ({ matchKey, void: true, why, signature: referee.signVoid({ matchKey }) });
    const { verdict, tickets } = match;
    if (verdict.void) return calledOff(verdict.why ?? 'the match could not be judged');
    if (!tickets.lockedAt) return calledOff('the stakes were never locked');
    const { stakers } = await onChain(matchKey);
    if (!sameBook(tickets.book, stakers)) return calledOff('a stake was made after the stakes were locked');
    const beganAt = Date.parse(match.startedAt) + ((verdict.selectionEndedAtTick - verdict.firstTick) / verdict.updateRate) * 1000;
    if (!(Date.parse(tickets.lockedAt) <= beganAt)) return calledOff('the match began before the stakes were locked');
    for (const staker of tickets.book) {
      const admission = match.admissions.findLast(candidate => candidate.address === staker.address && Number.isInteger(candidate.player));
      const played = admission && verdict.players.find(player => player.player === admission.player);
      if (!played) return calledOff('a staker did not play the match');
      if (played.team !== staker.team) return calledOff('a staker did not play in the team staked on');
    }
    return { matchKey, winnerTeam: verdict.winnerTeam, signature: referee.signResult({ matchKey, winnerTeam: verdict.winnerTeam }) };
  }

  /**
   * What anyone may bring to the contract for a match: {winnerTeam, signature} for settle, or
   * {void: true, signature} for voidMatch. {pending: true} while the match is not judged, null
   * for a match Photon never spoke of.
   */
  async function decision(gameId) {
    if (!contract) return null;
    const match = await matches.get(gameId);
    if (!match) return null;
    if (!match.verdict) return { pending: true, matchKey: matchKeyOf(gameId) };
    return matches.decideOnce(gameId, 'wager', () => decide(match));
  }

  /**
   * The decision of a match with where its stakes stand on the chain now, for the page that
   * shows a player what became of a stake: paid, given back, or still to be brought.
   */
  async function outcome(gameId) {
    const decided = await decision(gameId);
    return decided && { ...decided, stakes: await onChain(decided.matchKey) };
  }

  return { describe, onChain, ticket, lock, mine, decision, outcome, address: contract };
}
