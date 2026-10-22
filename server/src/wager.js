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
//
// The wager can be on Monad's testnet while the Genesis are read on mainnet (chain.js): there
// $CHOG does not exist and the stakes are in a test token (contracts/src/testnet/TestChog.sol),
// which the game is told so it says so.

import { keccak_256 } from '@noble/hashes/sha3.js';
import { DEFAULT_RPC_URLS, isAddress } from './chain.js';
import { folderOf } from './matches.js';
import { listOf } from './rental.js';
import { MAX_GENESIS_PLAYERS, matchKeyOf } from './referee.js';
import { CHOG_TOKEN } from './shop.js';

// The chains a wager can be on, with the public endpoints the Monad documentation lists for
// each (https://docs.monad.xyz/developer-essentials/testnet.md, read on 15 October 2026). The
// first one is what a wallet that does not know the chain is offered.
export const WAGER_NETWORKS = {
  mainnet: { chainId: 143, chainName: 'Monad', rpcUrls: DEFAULT_RPC_URLS, test: false },
  testnet: {
    chainId: 10143, chainName: 'Monad Testnet', test: true,
    rpcUrls: ['https://testnet-rpc.monad.xyz', 'https://rpc-testnet.monadinfra.com', 'https://rpc.ankr.com/monad_testnet'],
  },
};

/**
 * Where the wager is: its chain, the endpoints the services read it on, the token staked.
 * Throws on settings that cannot be right: an unknown network, no token on the testnet, or
 * anything but $CHOG on mainnet.
 * @param {object} [options]
 * @param {string} [options.network] "mainnet" (default) or "testnet"
 * @param {string[]} [options.rpcUrls] endpoints to read the wager on, else the public ones
 * @param {string} [options.token] the token staked: required on the testnet
 */
export function wagerNetwork({ network = 'mainnet', rpcUrls = [], token = '' } = {}) {
  const known = WAGER_NETWORKS[network];
  if (!known) throw new Error(`The wager network is "mainnet" or "testnet", not "${network}"`);
  const staked = (token || (known.test ? '' : CHOG_TOKEN)).toLowerCase();
  if (!isAddress(staked)) throw new Error('On the testnet the wager needs the address of the test token it is paid in');
  if (!known.test && staked !== CHOG_TOKEN.toLowerCase()) throw new Error('On mainnet the wager is paid in $CHOG only');
  return {
    network, chainId: known.chainId, chainName: known.chainName, test: known.test, token: staked,
    rpcUrls: rpcUrls.length > 0 ? rpcUrls : known.rpcUrls,
    // The endpoint offered to a wallet is always a public one: the services may read the chain
    // through an endpoint with a key in its address.
    publicRpcUrl: known.rpcUrls[0],
  };
}

// How long a ticket can be used, and how far behind ours the chain's clock is allowed to be.
export const TICKET_SECONDS = 120;
const CLOCK_MARGIN_SECONDS = 15;
// As in the contract.
const REFUND_AFTER_SECONDS = 3600;
const STATES = ['none', 'open', 'settled', 'refunded'];
const SETTINGS_KEPT_MS = 60000;
const UINT128 = 2n ** 128n;
const BASIS_POINTS = 10000n;
// The week of "won this week", and how long one wallet's week is answered without reading the chain again.
export const WEEK_MS = 7 * 24 * 3600 * 1000;
const WEEK_KEPT_MS = 30000;

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
const selector = signature => toHex(keccak_256(encoder.encode(signature)).slice(0, 4));

const REFEREE = selector('referee()');
const BURNED_SHARE = selector('burnedShare()');
// Version 2 of the contract only (22 October 2026): a call to version 1 fails, and the services sign as for it.
const GENESIS_SHARE = selector('genesisShare()');
const MATCH_OF = selector('matchOf(bytes32)');
// Version 3 (22 October 2026): a stake the player signs once (an EIP-2612 permit) and these services bring to the chain.
const STAKE_WITH_PERMIT = selector('stakeWithPermit(address,bytes32,uint8,uint256,uint256,bytes,uint256,uint8,bytes32,bytes32)');
const TOKEN_NAME = selector('name()');
const TOKEN_NONCES = selector('nonces(address)');
const TOKEN_DOMAIN_SEPARATOR = selector('DOMAIN_SEPARATOR()');
const PERMIT_SECONDS = 600;
const word = value => BigInt(value).toString(16).padStart(64, '0');
const hexWord = text => text.replace(/^0x/, '').toLowerCase().padStart(64, '0');
const STAKERS_OF = selector('stakersOf(bytes32)');

const sameBook = (a, b) => a.length === b.length
  && a.every(one => b.some(other => other.address === one.address && other.team === one.team));

/**
 * What one staker gains on top of their stake when their team wins: an equal share of what
 * the losers staked, less the burned share, as the contract pays it (settle). Zero when a side
 * has nobody, which the contract refunds.
 */
export function winnerGain(stakeWei, winners, losers, burnedShare, genesisShare = 0) {
  if (winners <= 0 || losers <= 0) return 0n;
  const lost = BigInt(stakeWei) * BigInt(losers);
  return (lost - lost * BigInt(burnedShare) / BASIS_POINTS - lost * BigInt(genesisShare) / BASIS_POINTS) / BigInt(winners);
}

/**
 * The wallets that played a Chog Genesis in a judged match, as version 2 of the contract pays them:
 * each player the judge saw with a Genesis, whose admission names that Genesis and a signed-in
 * wallet (owner, renter or guest; a stand-in is a bot), in the order of the players, once each.
 */
export function genesisPlayersOf(match) {
  const found = [];
  for (const played of [...(match.verdict?.players ?? [])].sort((a, b) => a.player - b.player)) {
    if (!(played.tokenId > 0)) continue;
    const admission = match.admissions.findLast(candidate => candidate.player === played.player && Number.isInteger(candidate.player));
    if (!admission || admission.standIn || admission.tokenId !== played.tokenId || !admission.address) continue;
    const address = admission.address.toLowerCase();
    if (!found.includes(address)) found.push(address);
  }
  return found.slice(0, MAX_GENESIS_PLAYERS);
}

/**
 * @param {object} options
 * @param {ReturnType<import('./chain.js').createChain>} options.chain the chain the wager is on
 * @param {ReturnType<import('./referee.js').createReferee>} [options.referee] without it there is no wager
 * @param {ReturnType<import('./matches.js').createMatches>} options.matches
 * @param {ReturnType<typeof wagerNetwork>} [options.network] where the wager is; mainnet by default
 * @param {{get: Function, set: Function, list: Function}} [options.store] where the matches each
 *   wallet staked on are noted (the store of the matches); without it "won this week" is not answered
 * @param {ReturnType<import('./price.js').createPrice>} [options.price] the price of $CHOG, for the dollars of a week
 * @param {() => number} [options.now] milliseconds
 */
export function createWager({ chain, referee, matches, network = wagerNetwork(), store, price, sender, now = Date.now }) {
  const contract = referee?.wager ?? null;
  if (referee && referee.chainId !== network.chainId) {
    throw new Error(`The referee signs for chain ${referee.chainId} and the wager is on chain ${network.chainId}`);
  }
  let settings = null;
  // One wallet's week, kept a short time: it reads the chain once per match of the week.
  const weeks = new Map();

  // Where the matches a wallet staked on are noted: one key per wallet and match.
  const stakedKey = (address, gameId) => `staked-${address.toLowerCase()}/${folderOf(gameId)}`;

  /** What a game page needs to stake; {enabled: false} when there is no wager or it is set up wrong. */
  async function describe() {
    if (!contract) return { enabled: false };
    if (!settings || now() - settings.readAt > SETTINGS_KEPT_MS) {
      const [signer, share, genesis] = await chain.callMany([
        { to: contract, data: '0x' + REFEREE }, { to: contract, data: '0x' + BURNED_SHARE }, { to: contract, data: '0x' + GENESIS_SHARE },
      ]);
      if (typeof signer !== 'string' || signer.length !== 66 || typeof share !== 'string' || share.length !== 66) {
        throw new Error(`The wager contract ${contract} did not answer referee() and burnedShare()`);
      }
      // No genesisShare(): version 1, which takes no list of Genesis players with a result.
      const version2 = typeof genesis === 'string' && genesis.length === 66;
      // Version 3 is version 2 with stakeWithPermit: its selector is in the code of the contract (a dispatcher compares it).
      const code = await chain.rpc?.('eth_getCode', [contract, 'latest']).catch(() => '') ?? '';
      const version3 = version2 && typeof code === 'string' && code.toLowerCase().includes(STAKE_WITH_PERMIT);
      settings = {
        version3,
        readAt: now(), referee: '0x' + signer.slice(-40).toLowerCase(), burnedShare: Number(BigInt(share)),
        genesisShare: version2 ? Number(BigInt(genesis)) : null,
      };
    }
    // The contract only listens to the referee it knows: with another key here no ticket would work.
    if (settings.referee !== referee.address) return { enabled: false, reason: 'the contract knows another referee' };
    return {
      enabled: true, address: contract, token: network.token, chainId: network.chainId, chainName: network.chainName,
      rpcUrl: network.publicRpcUrl, test: network.test, referee: referee.address,
      burnedShare: settings.burnedShare, genesisShare: settings.genesisShare ?? 0,
      version: settings.genesisShare === null ? 1 : settings.version3 ? 3 : 2,
      // One signature in the wallet, no gas: these services bring the stake (stakeWithPermit) with the referee's key.
      relay: Boolean(settings.version3 && sender),
      ticketSeconds: TICKET_SECONDS, refundAfterSeconds: REFUND_AFTER_SECONDS,
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
    const locked = await matches.lock(match.gameId, { stakers, nowSeconds: Math.floor(now() / 1000), marginSeconds: CLOCK_MARGIN_SECONDS });
    // The book is fixed now: each staker's week will find this match.
    if (locked.locked && store && Array.isArray(locked.book)) {
      for (const staker of locked.book) {
        if (isAddress(staker.address)) await store.set(stakedKey(staker.address, match.gameId), JSON.stringify({ gameId: match.gameId, at: locked.lockedAt }));
      }
    }
    return locked;
  }

  // What one match of a wallet's week came to: its stake, what was won or lost on the
  // referee's decision, and whether the chain has paid it yet.
  async function matchOfWeek(address, gameId) {
    const matchKey = matchKeyOf(gameId);
    const [decided, stakes] = await Promise.all([decision(gameId), onChain(matchKey)]);
    const who = address.toLowerCase();
    const mine = stakes.stakers.find(staker => staker.address.toLowerCase() === who);
    if (!mine) return null;
    const entry = { gameId, matchKey, stake: stakes.amount, team: mine.team, state: stakes.state, won: '0', lost: '0', outcome: 'pending' };
    if (!decided || decided.pending) return entry;
    const refunded = decided.void || decided.winnerTeam === 2;
    const winners = refunded ? 0 : stakes.stakers.filter(staker => staker.team === decided.winnerTeam).length;
    const losers = stakes.stakers.length - winners;
    if (refunded || winners === 0 || losers === 0) return { ...entry, outcome: decided.void ? 'called_off' : 'refunded' };
    if (mine.team === decided.winnerTeam) {
      return { ...entry, outcome: 'won', won: winnerGain(stakes.amount, winners, losers, settings?.burnedShare ?? 0).toString() };
    }
    return { ...entry, outcome: 'lost', lost: stakes.amount };
  }

  /**
   * What a wallet won and lost on its stakes of the last seven days, on the referee's
   * decisions, with where each match's stakes stand on the chain ("settled" once paid). The
   * matches counted are those whose stakes were locked with this wallet in the book since the
   * services started noting them; {enabled: false} without a wager or without a store.
   */
  async function weekOf({ address }) {
    if (!contract || !store) return { enabled: false };
    const who = address.toLowerCase();
    const kept = weeks.get(who);
    if (kept && now() - kept.readAt < WEEK_KEPT_MS) return kept.week;
    await describe();
    const since = now() - WEEK_MS;
    const noted = [];
    for (const key of await store.list(`staked-${who}/`)) {
      const text = await store.get(key);
      if (text === null) continue;
      const note = JSON.parse(text);
      if (Date.parse(note.at) >= since) noted.push(note);
    }
    noted.sort((a, b) => b.at.localeCompare(a.at));
    const entries = (await Promise.all(noted.map(note => matchOfWeek(who, note.gameId).catch(() => null)))).filter(Boolean);
    const sum = field => entries.reduce((total, entry) => total + BigInt(entry[field]), 0n);
    const won = sum('won');
    const lost = sum('lost');
    const usd = price ? await price.usdOfWei(won.toString()) : null;
    const week = {
      enabled: true, address: who, since: new Date(since).toISOString(), token: network.token,
      matches: entries.length,
      pending: entries.filter(entry => entry.outcome === 'pending').length,
      unpaid: entries.filter(entry => entry.outcome !== 'pending' && entry.state === 'open').length,
      won: won.toString(), lost: lost.toString(), net: (won - lost).toString(),
      wonUsd: usd, netUsd: usd === null ? null : usd - (await price.usdOfWei(lost.toString()) ?? 0),
      entries,
    };
    weeks.set(who, { readAt: now(), week });
    return week;
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
    if (!settings) await describe();
    if (settings.genesisShare === null) {
      return { matchKey, winnerTeam: verdict.winnerTeam, signature: referee.signResult({ matchKey, winnerTeam: verdict.winnerTeam }) };
    }
    // Version 2: the wallets that played a Genesis are paid their share with the result, signed with it.
    const genesisPlayers = genesisPlayersOf(match);
    return {
      matchKey, winnerTeam: verdict.winnerTeam, genesisPlayers,
      signature: referee.signResult({ matchKey, winnerTeam: verdict.winnerTeam, genesisPlayers }),
    };
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
  /**
   * What a wallet signs so that the contract may take its stake (EIP-2612 permit of the token, version 3): the domain of the
   * token, checked against its DOMAIN_SEPARATOR, its nonce for that wallet, the spender and how long the permit lasts.
   */
  async function permit({ address }) {
    if (!contract) return { refused: 'no_wager' };
    if (!settings) await describe();
    if (!settings.version3 || !sender) return { refused: 'no_relay' };
    const token = network.token;
    const [nameAnswer, nonceAnswer, separator] = await chain.callMany([
      { to: token, data: '0x' + TOKEN_NAME }, { to: token, data: '0x' + TOKEN_NONCES + hexWord(address) },
      { to: token, data: '0x' + TOKEN_DOMAIN_SEPARATOR },
    ]);
    const nameLength = Number(BigInt('0x' + nameAnswer.slice(66, 130)));
    const name = Buffer.from(nameAnswer.slice(130, 130 + nameLength * 2), 'hex').toString('utf8');
    const domain = { name, version: '1', chainId: network.chainId, verifyingContract: token };
    const typeHash = keccak_256(encoder.encode('EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)'));
    const parts = [toHex(typeHash), toHex(keccak_256(encoder.encode(name))), toHex(keccak_256(encoder.encode('1'))),
      word(network.chainId), hexWord(token)];
    const computed = '0x' + toHex(keccak_256(Uint8Array.from(Buffer.from(parts.join(''), 'hex'))));
    if (computed !== separator.toLowerCase()) return { refused: 'unknown_permit_domain' };
    return {
      domain, spender: contract, nonce: BigInt(nonceAnswer).toString(), deadline: Math.floor(now() / 1000) + PERMIT_SECONDS,
    };
  }

  /**
   * Brings a stake the wallet signed (its ticket, from ticket(), and its permit for exactly the amount) to the contract, with the
   * referee's key paying the gas. The contract checks both signatures; the sender refuses a transaction that would revert, so a
   * wrong request costs nothing.
   */
  async function relay({ address, matchKey, team, amount, deadline, signature, permitDeadline, v, r, s }) {
    if (!contract) return { refused: 'no_wager' };
    if (!settings) await describe();
    if (!settings.version3 || !sender) return { refused: 'no_relay' };
    const hex32 = text => typeof text === 'string' && /^0x[0-9a-fA-F]{64}$/.test(text);
    if (!hex32(matchKey) || !hex32(r) || !hex32(s) || ![27, 28].includes(Number(v)) || ![0, 1].includes(Number(team))
      || !/^[1-9][0-9]*$/.test(String(amount)) || !Number.isInteger(Number(deadline)) || !Number.isInteger(Number(permitDeadline))
      || typeof signature !== 'string' || !/^0x[0-9a-fA-F]{130}$/.test(signature)) {
      return { refused: 'bad_request' };
    }
    const ticket = signature.slice(2).toLowerCase();
    const data = '0x' + STAKE_WITH_PERMIT + hexWord(address) + hexWord(matchKey) + word(team) + word(amount) + word(deadline)
      + word(10 * 32) + word(permitDeadline) + word(v) + hexWord(r) + hexWord(s)
      + word(65) + ticket.padEnd(192, '0');
    try {
      const sent = await sender.transact({ to: contract, data, purpose: `a stake of ${address} on ${matchKey}` });
      return sent.ok === false ? { refused: 'reverted', hash: sent.hash } : { hash: sent.hash, done: sent.ok === true };
    } catch (error) {
      return { refused: 'refused_by_chain', message: String(error.message).slice(0, 200) };
    }
  }

  async function outcome(gameId) {
    const decided = await decision(gameId);
    if (decided) return { ...decided, stakes: await onChain(decided.matchKey), refundAfterSeconds: REFUND_AFTER_SECONDS };
    // A match these services never heard of, or lost (a new container before the mirror of 22 October 2026): its stakes are still
    // on the chain, and the contract gives them back to anyone who asks REFUND_AFTER_SECONDS after the first one (refundExpired).
    if (!contract || typeof gameId !== 'string' || gameId.length === 0) return null;
    const matchKey = matchKeyOf(gameId);
    const stakes = await onChain(matchKey);
    if (stakes.state === 'none') return null;
    return { lost: true, matchKey, stakes, refundAfterSeconds: REFUND_AFTER_SECONDS };
  }

  return { describe, onChain, ticket, lock, mine, decision, outcome, weekOf, permit, relay, address: contract };
}
