// What the services remember of each match Photon's cloud tells them about: who entered, with
// which Genesis each player was admitted, the input history Photon's server streams, what the
// clients claimed, the tickets handed out to stake on it, and the verdict another component
// gives once it has played the match again from that input history.
//
// Everything is text behind one small store (get, set, list by prefix). Each match has its own
// folder of plain JSON files, so that a human can open them:
//
//   <folder>/match.json                 the journal, in our own words: entries, admissions,
//                                       players, start, end, tickets
//   <folder>/configs.json               the GameConfigs request, as received
//   <folder>/replay-start.json          the ReplayStart request, as received
//   <folder>/replay-chunk-000000.json   each ReplayChunk request, as received, under its number
//   <folder>/claims.json                the GameResult requests, as received: what the players'
//                                       own pages say, which decides nothing
//   <folder>/verdict.json               the verdict, once there is one
//   <folder>/wager.json                 what the referee signs for the stakes, decided once
//   players/<address>.json              the match a wallet was last let into
//
// The folder is the GameId made safe for a file name, followed by a few digits of its hash so
// that two GameIds never share one.

import { sha256 } from '@noble/hashes/sha2.js';

if (typeof JSON.rawJSON !== 'function') {
  throw new Error('This runtime cannot keep large numbers exact (JSON.rawJSON is missing): Node 22 or later is needed');
}

const encoder = new TextEncoder();
const toHex = bytes => Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');

/**
 * JSON text to a value without losing a digit. Photon's requests carry 64-bit asset ids, which
 * JavaScript rounds: a number that would not be written back the way it came is kept as its
 * source text, and JSON.stringify writes that text back unchanged.
 */
export function parseExact(text) {
  return JSON.parse(text, (key, value, context) => (
    typeof value === 'number' && String(value) !== context.source ? JSON.rawJSON(context.source) : value));
}

/** The number a value stands for, also when it was kept as its source text; undefined for anything else. */
export function numberOf(value) {
  if (typeof value === 'number') return value;
  return JSON.isRawJSON(value) && Number.isFinite(Number(value.rawJSON)) ? Number(value.rawJSON) : undefined;
}

const written = value => JSON.stringify(value, null, 2) + '\n';

/** A store kept in memory, for tests. */
export function createMemoryStore() {
  const texts = new Map();
  return {
    get: async key => texts.get(key) ?? null,
    set: async (key, text) => { texts.set(key, text); },
    list: async prefix => [...texts.keys()].filter(key => key.startsWith(prefix)).sort(),
  };
}

/** The folder of a match in the store: readable, safe as a file name, and its own. */
export function folderOf(gameId) {
  const readable = gameId.replace(/[^A-Za-z0-9_-]+/g, '_').replace(/^[^A-Za-z0-9]+/, '').slice(0, 60);
  return `${readable || 'match'}-${toHex(sha256(encoder.encode(gameId))).slice(0, 12)}`;
}

const chunkName = number => `replay-chunk-${String(number).padStart(6, '0')}`;

/**
 * @param {object} options
 * @param {{get: (key: string) => Promise<string|null>, set: (key: string, text: string) => Promise<void>,
 *   list: (prefix: string) => Promise<string[]>}} options.store keys are "folder/name"
 * @param {() => number} [options.now] milliseconds
 */
export function createMatches({ store, now = Date.now }) {
  const time = () => new Date(now()).toISOString();
  // One change at a time for each match: each one reads what the previous one saved.
  const turns = new Map();

  function inTurn(gameId, change) {
    const done = (turns.get(gameId) ?? Promise.resolve()).then(change);
    const settled = done.catch(() => {});
    turns.set(gameId, settled);
    settled.then(() => { if (turns.get(gameId) === settled) turns.delete(gameId); });
    return done;
  }

  async function read(gameId, name) {
    const text = await store.get(`${folderOf(gameId)}/${name}`);
    return text === null ? null : parseExact(text);
  }

  const save = (gameId, name, value) => store.set(`${folderOf(gameId)}/${name}`, written(value));

  const blank = gameId => ({
    gameId,
    firstHeardAt: time(),
    app: null,
    entries: [],
    configsAt: null,
    admissions: [],
    added: [],
    removed: [],
    left: [],
    startedAt: null,
    replay: { chunks: 0, endedAt: null },
    claimsAt: null,
    closedAt: null,
    closeReason: null,
    tickets: { amount: null, issued: [], lockedAt: null, book: null },
  });

  // Reads the journal of a match, lets "alter" change it, saves it.
  const journal = (gameId, alter) => inTurn(gameId, async () => {
    const match = (await read(gameId, 'match')) ?? blank(gameId);
    const answer = await alter(match);
    await save(gameId, 'match', match);
    return answer;
  });

  /** A client created the match or joined it: who Photon says it is, and what our authentication said of it. */
  const entered = (gameId, { how, userId, authCookie, app }) => journal(gameId, match => {
    if (app) match.app ??= app;
    match.entries.push({ at: time(), how, userId, authCookie });
  });

  /** The configs the first client sent, kept as received. */
  const configs = (gameId, request) => journal(gameId, async match => {
    if (match.configsAt) return;
    await save(gameId, 'configs', request);
    match.configsAt = time();
  });

  /**
   * With what a player was let into the simulation: the Genesis and the form it plays, what its
   * page had asked for, and the wallet address when the player is signed in. The match a wallet
   * was last let into is noted under its address, so that its page can ask about "my match".
   */
  const admitted = (gameId, admission) => journal(gameId, async match => {
    match.admissions.push({ at: time(), ...admission });
    if (admission.address) await store.set(`players/${admission.address}`, written({ gameId, at: time() }));
  });

  /** The match a wallet was last let into, while Photon has not closed it; else null. */
  async function currentOf(address) {
    const text = await store.get(`players/${address}`);
    const match = text === null ? null : await read(parseExact(text).gameId, 'match');
    return match && !match.closedAt ? match : null;
  }

  /** The last match this wallet entered, including one Photon has since closed. */
  async function latestOf(address) {
    const text = await store.get(`players/${address}`);
    return text === null ? null : read(parseExact(text).gameId, 'match');
  }

  /**
   * Photon added the player to the simulation. "player" is the number the simulation knows it
   * by: it is written on the admission of that client, which is how a re-simulation learns
   * which Genesis each of its players is.
   */
  const added = (gameId, { userId, actorNr, playerSlot, player }) => journal(gameId, match => {
    match.added.push({ at: time(), userId, actorNr, playerSlot, player });
    const same = admission => admission.userId === userId && admission.playerSlot === playerSlot;
    const admission = match.admissions.findLast(candidate => same(candidate) && candidate.actorNr === actorNr)
      ?? match.admissions.findLast(same);
    if (admission) admission.player = player;
  });

  const removed = (gameId, removal) => journal(gameId, match => {
    match.removed.push({ at: time(), ...removal });
  });

  const left = (gameId, leave) => journal(gameId, match => {
    match.left.push({ at: time(), ...leave });
  });

  const closed = (gameId, { closeReason }) => journal(gameId, match => {
    match.closedAt ??= time();
    match.closeReason ??= closeReason ?? null;
  });

  /** The simulation starts on Photon's server: its configs are kept as received, once. */
  const started = (gameId, request) => journal(gameId, async match => {
    if (match.startedAt) return { stored: false };
    await save(gameId, 'replay-start', request);
    match.startedAt = time();
    return { stored: true };
  });

  /**
   * One slice of the input history, kept as received under its number. A slice that comes a
   * second time is not kept again. "ended" is true once, when the last slice is first kept.
   */
  const chunk = (gameId, number, request) => journal(gameId, async match => {
    if ((await store.get(`${folderOf(gameId)}/${chunkName(number)}`)) !== null) return { stored: false, ended: false };
    await save(gameId, chunkName(number), request);
    match.replay.chunks += 1;
    const ended = request.IsLast === true && match.replay.endedAt === null;
    if (ended) match.replay.endedAt = time();
    return { stored: true, ended };
  });

  /** What the clients say happened, as received. The same request is not kept twice. */
  const claims = (gameId, request) => journal(gameId, async match => {
    const kept = (await read(gameId, 'claims')) ?? [];
    if (kept.some(earlier => JSON.stringify(earlier) === JSON.stringify(request))) return;
    await save(gameId, 'claims', [...kept, request]);
    match.claimsAt ??= time();
  });

  /**
   * Whether "address" may be given a ticket to stake "amount" on this match, and the note of it
   * when it may. Only an address the webhooks admitted, only until the stakes are locked; the
   * first ticket fixes what everyone stakes. "deadline" is in seconds, as the chain counts time.
   */
  const ticket = (gameId, { address, team, amount, deadline }) => inTurn(gameId, async () => {
    const match = await read(gameId, 'match');
    if (!match) return { refused: 'unknown_match' };
    const admitted = match.admissions.filter(admission => admission.address === address).at(-1);
    if (!admitted) return { refused: 'not_admitted' };
    // Stakes are for the players of a Genesis, held, rented or lent (23 October 2026, the owner: the NFT must be
    // essential; a recruit plays for practice).
    if (!(admitted.tokenId > 0)) return { refused: 'genesis_required' };
    // Stakes are for sieges, the ranked mode (the game names its Photon version "-siege" or "-coins").
    if (String(match.app?.appVersion ?? '').endsWith('-coins')) return { refused: 'siege_only' };
    if (match.tickets.lockedAt) return { refused: 'stakes_locked' };
    if (match.closedAt) return { refused: 'match_closed' };
    if (await read(gameId, 'verdict')) return { refused: 'match_judged' };
    if (match.tickets.amount !== null && match.tickets.amount !== amount) {
      return { refused: 'not_the_stake_of_the_match', amount: match.tickets.amount };
    }
    match.tickets.amount = amount;
    match.tickets.issued.push({ at: time(), address, team, deadline });
    await save(gameId, 'match', match);
    return { amount };
  });

  /**
   * Locks the stakes of a match: no ticket is handed out afterwards, and "stakers" (who has
   * staked on the chain, read by the caller just before) becomes the book the match is paid on.
   * It waits while a ticket that was handed out could still be used: its holder has not staked
   * and its deadline, plus "marginSeconds" for a chain whose clock is behind ours, has not
   * passed. So once the stakes are locked nobody can stake any more, whatever the match shows.
   */
  const lock = (gameId, { stakers, nowSeconds, marginSeconds = 0 }) => inTurn(gameId, async () => {
    const match = await read(gameId, 'match');
    if (!match) return { refused: 'unknown_match' };
    if (match.tickets.lockedAt) return { locked: true, lockedAt: match.tickets.lockedAt, book: match.tickets.book };
    if (match.closedAt) return { refused: 'match_closed' };
    const staked = new Set(stakers.map(staker => staker.address));
    const usableUntil = match.tickets.issued
      .filter(issued => !staked.has(issued.address))
      .map(issued => Number(issued.deadline) + marginSeconds);
    const last = Math.max(0, ...usableUntil);
    if (last >= nowSeconds) return { locked: false, waitSeconds: last - nowSeconds + 1 };
    match.tickets.lockedAt = time();
    match.tickets.book = stakers;
    await save(gameId, 'match', match);
    return { locked: true, lockedAt: match.tickets.lockedAt, book: match.tickets.book };
  });

  /**
   * What was decided once for a match under "name", for instance what the referee signs for its
   * stakes: the first value given is kept and answered ever after, so that a match never gets
   * two different decisions.
   */
  const decideOnce = (gameId, name, decide) => inTurn(gameId, async () => {
    const kept = await read(gameId, name);
    if (kept) return kept;
    const decided = { ...(await decide()), at: time() };
    await save(gameId, name, decided);
    return decided;
  });

  /** Everything recorded of a match, or null when Photon never spoke of it. */
  async function get(gameId) {
    const match = await read(gameId, 'match');
    if (!match) return null;
    return {
      ...match,
      configs: await read(gameId, 'configs'),
      claims: (await read(gameId, 'claims')) ?? [],
      verdict: await read(gameId, 'verdict'),
    };
  }

  /**
   * The input history of a match as Photon's server sent it: the start request, the slices in
   * the order of their numbers, and whether it is whole. It is whole when the start was
   * received, the last slice was received, and no number is missing from 0 up to it.
   */
  async function replayOf(gameId) {
    const start = await read(gameId, 'replay-start');
    const chunks = [];
    for (const key of await store.list(`${folderOf(gameId)}/replay-chunk-`)) chunks.push(parseExact(await store.get(key)));
    chunks.sort((a, b) => numberOf(a.ChunkNumber) - numberOf(b.ChunkNumber));
    const numbers = new Set(chunks.map(slice => numberOf(slice.ChunkNumber)));
    const highest = chunks.length > 0 ? numberOf(chunks.at(-1).ChunkNumber) : -1;
    const missing = [];
    for (let number = 0; number <= highest; number++) if (!numbers.has(number)) missing.push(number);
    const ended = chunks.length > 0 && chunks.at(-1).IsLast === true;
    return { gameId, start, chunks, ended, missing, complete: start !== null && ended && missing.length === 0 };
  }

  /**
   * The verdict of a match, given by whoever played it again: {winnerTeam: 0 | 1} for the team
   * that won, {winnerTeam: 2} for a draw, or {void: true} when the match cannot be judged; any
   * other field is kept with it. A match has one verdict: giving the same one again changes
   * nothing, giving another one throws, because two signed results for one match would let
   * whoever brings one to the chain choose.
   */
  const setVerdict = (gameId, verdict) => inTurn(gameId, async () => {
    const understood = verdict?.void === true
      ? (verdict.winnerTeam ?? null) === null
      : [0, 1, 2].includes(verdict?.winnerTeam);
    if (!understood) throw new Error('A verdict is {winnerTeam: 0, 1 or 2} or {void: true}');
    if (!(await read(gameId, 'match'))) throw new Error('No such match: a verdict is given on a match Photon told us about');
    const given = await read(gameId, 'verdict');
    if (given) {
      const same = verdict.void === true ? given.void === true : given.winnerTeam === verdict.winnerTeam;
      if (!same) throw new Error('This match already has another verdict');
      return given;
    }
    const kept = { ...verdict, at: time() };
    await save(gameId, 'verdict', kept);
    return kept;
  });

  /** The verdict of a match, or null while there is none. */
  const verdictOf = gameId => read(gameId, 'verdict');

  return {
    entered, configs, admitted, added, removed, left, closed, started, chunk, claims, ticket, lock, decideOnce,
    get, currentOf, latestOf, replayOf, setVerdict, verdictOf, folderOf,
  };
}
