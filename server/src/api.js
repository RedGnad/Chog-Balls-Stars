// HTTP interface of the game services, written against the web-standard Request and Response
// so the same handler runs under the local Node server and under a hosted function runtime.

import { LAST_TOKEN_ID } from './career.js';
import { CHAIN_ID, GENESIS, isAddress } from './chain.js';
import { parseExact } from './matches.js';
import { MAX_LOAN_DAYS } from './session.js';
import { CHOG_TOKEN } from './shop.js';

const MAX_BODY_CHARACTERS = 20000;
const MAX_TOKENS_PER_READ = 64;
const WEBHOOKS = '/api/photon/hook/';
// Why a ticket or a lock was refused, in words a player can read.
const WAGER_REFUSALS = {
  no_wager: 'no match can be staked on here',
  no_relay: 'this wager cannot take a signed stake: stake with a transaction',
  unknown_permit_domain: 'the token signs permits in a way the game does not know',
  bad_request: 'the ticket or the permit is not well formed',
  reverted: 'the stake was refused on the chain',
  refused_by_chain: 'the chain refused the stake',
  genesis_required: 'stakes are for Genesis players: play a Genesis you hold, rent or were lent',
  siege_only: 'stakes are for sieges',
  too_soon: 'a stake of this wallet was just sent: wait a few seconds',
  relay_busy: 'too many stakes are being sent: try again in a moment',
  bad_team: 'team must be 0 or 1',
  bad_amount: 'amount must be a whole number of the smallest unit of $CHOG, as digits',
  not_in_a_match: 'this wallet is not in a match the game server knows',
  unknown_match: 'the game server does not know this match',
  not_admitted: 'this wallet was not let into this match',
  stakes_locked: 'the stakes of this match are locked',
  match_closed: 'this match is over',
  match_judged: 'this match is already judged',
  not_the_stake_of_the_match: 'this match is played for another stake',
};

const json = (status, body) => new Response(JSON.stringify(body), {
  status,
  headers: { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' },
});

const failure = (status, code, message) => json(status, { error: code, message });

async function bodyOf(request) {
  try {
    const text = await request.text();
    const body = text.length <= MAX_BODY_CHARACTERS ? JSON.parse(text) : null;
    return body && typeof body === 'object' ? body : null;
  } catch {
    return null;
  }
}

/**
 * @param {object} services
 * @param {ReturnType<import('./chain.js').createChain>} services.chain
 * @param {ReturnType<import('./session.js').createSessions>} services.sessions
 * @param {ReturnType<import('./career.js').createCareers>} services.careers
 * @param {ReturnType<import('./shop.js').createShop>} [services.shop] absent when nothing is sold
 * @param {ReturnType<import('./rental.js').createRental>} [services.rental] absent when nothing is rented
 * @param {ReturnType<import('./photon.js').createPhoton>} [services.photon] absent when Photon's
 *   cloud is not set up to ask these services anything
 * @param {boolean} [services.photonAuthentication] whether Photon's dashboard is set up to ask
 *   these services who a player is: the game then connects with its session or its pass
 * @param {string} [services.rankStorageStatus] public, credential-free startup probe result
 * @param {ReturnType<import('./seasons.js').createSeasons>} [services.seasons] the ladder season, when one is configured
 * @param {ReturnType<import('./wager.js').createWager>} [services.wager] absent when no match is staked on
 * @param {ReturnType<import('./price.js').createPrice>} [services.price] the price of $CHOG in dollars;
 *   absent when the game shows amounts in $CHOG only
 * @param {ReturnType<import('./rental-ledger.js').createRentalLedger>} [services.ledger] what each
 *   Genesis earned by being rented, from the rental's events; absent when that is not read
 * @param {string} [services.publicOrigin] origin of the game page, for example "https://game.example";
 *   when absent it is taken from the request, which is only right behind a proxy we control
 * @param {string} [services.reownProjectId] the Reown project id of this deployment; without it
 *   the page offers no connection to a wallet app and keeps to the wallet of the browser
 * @param {string | (() => Promise<string>)} [services.build] the build of the game served
 *   beside these services, or a function that reads it; empty when they do not know
 * @returns {(request: Request) => Promise<Response>}
 */
export function createApi({ chain, sessions, careers, ranks, seasons, rankStorageStatus = 'not_configured', matches, recentRecords, shop, rental, standIns, photon, photonAuthentication = false, wager, price, ledger, publicOrigin, reownProjectId = '', build = '' }) {
  // The page a wallet is asked to trust: it is written in the message the player signs.
  function pageOf(request, url) {
    const origin = publicOrigin
      ?? `${request.headers.get('x-forwarded-proto') ?? url.protocol.replace(':', '')}://${request.headers.get('x-forwarded-host') ?? url.host}`;
    return { uri: origin, domain: origin.replace(/^[a-z]+:\/\//, '') };
  }

  const authorization = request => request.headers.get('authorization') ?? '';
  const signedIn = request => sessions.addressOf(authorization(request).replace(/^Bearer /, ''));

  // The loan behind the pass a request presents, when it still counts: we issued the pass, it
  // has not run out, its lender has not stopped lending since and still holds the Genesis.
  async function loanFrom(request) {
    const loan = await sessions.loanOf(authorization(request).replace(/^Pass /, ''));
    if (!loan) return { refusal: failure(403, 'bad_pass', 'this link was not made by the game') };
    if (loan.expired) return { refusal: failure(403, 'pass_expired', 'this link has run out') };
    if (loan.issuedAt <= await careers.lendingStoppedAt(loan.tokenId)) {
      return { refusal: failure(403, 'lending_stopped', 'the holder stopped lending this Genesis') };
    }
    if (!(await chain.holds(loan.lender, loan.tokenId)).holds) {
      return { refusal: failure(403, 'lender_not_the_holder', 'the wallet that lent this Genesis no longer holds it') };
    }
    return { loan };
  }

  const routes = {
    // Answers without reading the chain, so a host can ask it every few seconds.
    'GET /api/health': async () => json(200, {
      ok: true, chainId: CHAIN_ID, contract: GENESIS, lastOwnerReadBlock: chain.lastBlock?.() ?? null,
      rankStorage: rankStorageStatus, rankEnabled: Boolean(ranks),
    }),

    // The Genesis an address holds. A public read: it proves nothing about who is asking.
    'GET /api/genesis/owned': async (request, url) => {
      const address = url.searchParams.get('address');
      if (!isAddress(address)) return failure(400, 'bad_address', 'address must be 0x followed by 40 hex digits');
      const owned = await chain.ownedBy(address);
      // A Genesis this address plays through a stand-in it holds (standin.js) is listed with its own.
      const standingIn = standIns ? await standIns.genesisOf(address) : [];
      const tokens = [...new Set([...owned.tokens, ...standingIn])].sort((a, b) => a - b);
      return json(200, {
        address: address.toLowerCase(), chainId: CHAIN_ID, contract: GENESIS,
        tokens, block: owned.block, ...(standingIn.length > 0 ? { standIns: standingIn } : {}),
      });
    },

    // Step 1 of the sign-in: the message the wallet of that address must sign.
    'POST /api/session/challenge': async (request, url) => {
      const body = await bodyOf(request);
      if (!body || !isAddress(body.address)) return failure(400, 'bad_address', 'address must be 0x followed by 40 hex digits');
      return json(200, await sessions.challenge(body.address, pageOf(request, url)));
    },

    // Step 2: the signed message against a session token.
    'POST /api/session': async request => {
      const body = await bodyOf(request);
      if (!body) return failure(400, 'bad_request', 'expected message, signature and proof');
      const result = await sessions.signIn(body);
      if (!result.ok) return failure(401, result.reason, 'the signature does not prove this address');
      return json(200, { address: result.address, session: result.session, expiresAt: result.expiresAt });
    },

    // Careers are public: anyone may read what a Genesis has done.
    'GET /api/career': async (request, url) => {
      const tokens = (url.searchParams.get('tokens') ?? '').split(',').filter(Boolean).map(Number);
      if (tokens.length === 0 || tokens.length > MAX_TOKENS_PER_READ
        || tokens.some(token => !Number.isInteger(token) || token < 1 || token > LAST_TOKEN_ID)) {
        return failure(400, 'bad_tokens', `tokens must be 1 to ${MAX_TOKENS_PER_READ} token ids separated by commas`);
      }
      return json(200, { careers: await careers.get(tokens) });
    },

    // The Genesis that won the most.
    // The last players Photon asked to add: the cookie it handed back and the right decided.
    'GET /api/admissions/recent': async () => json(200, { admissions: photon?.recentAdmissions ? photon.recentAdmissions() : [] }),

    // What happened to the last judged matches: verdict, and the career transaction or why none.
    'GET /api/careers/recent': async () => json(200, { matches: recentRecords ? recentRecords() : [] }),

    // The record of one match as Photon's server sent it (its configs and its slices of input, nothing of a player), so that a
    // verdict can be replayed again elsewhere and compared with the record a test page kept of the same match.
    'GET /api/match/record': async (request, url) => {
      const gameId = url.searchParams.get('gameId') ?? '';
      if (!matches?.replayOf || gameId.length === 0) return failure(400, 'bad_game', 'gameId is needed');
      const record = await matches.replayOf(gameId);
      if (!record.start && record.chunks.length === 0) return failure(404, 'no_record', 'no record of that match here');
      const { AppId, ...start } = record.start ?? {};
      return json(200, {
        gameId, ended: record.ended, missing: record.missing, start: record.start ? start : null,
        chunks: record.chunks.map(({ AppId: ignored, ...slice }) => slice),
      });
    },

    'GET /api/career/top': async (request, url) => {
      const limit = Number(url.searchParams.get('limit') ?? 10);
      if (!Number.isInteger(limit) || limit < 1 || limit > MAX_TOKENS_PER_READ) {
        return failure(400, 'bad_limit', `limit must be 1 to ${MAX_TOKENS_PER_READ}`);
      }
      return json(200, { careers: await careers.top(limit) });
    },

    // A player's ladder is separate from the career of any Genesis they use. A replayed,
    // complete Siege with at least one signed-in human can change it; bots lower the award.
    'GET /api/rank/me': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      const rank = ranks ? await ranks.of(address) : null;
      const latest = matches?.latestOf ? await matches.latestOf(address) : null;
      const event = latest && ranks ? await ranks.eventOf(latest.gameId) : null;
      // The hidden rating is answered as a band only, never as a number (rating.js).
      const rated = ranks ? await ranks.ratingOf(address) : null;
      const season = ranks && seasons ? await seasons.current() : null;
      return json(200, {
        enabled: Boolean(ranks), rank,
        rating: rated ? { band: rated.band, rated: rated.matches > 0, matches: rated.matches } : null,
        season: season ? { ...season, summary: await seasons.summaryOf(address) } : null,
        latestMatch: latest ? {
          gameId: latest.gameId,
          status: event ? (event.reason ?? 'ranked') : latest.startedAt ? 'verifying' : 'preparing',
        } : null,
      });
    },

    // The ladder season: its id and times, whether it ended and was closed; {enabled: false} without one.
    'GET /api/season': async () => {
      const season = ranks && seasons ? await seasons.current() : null;
      return json(200, season ? { enabled: true, ...season } : { enabled: false });
    },

    // The reward list of a closed season: every wallet that played in it with its final tier.
    // Public like the events behind it; the $CHOG payment is made elsewhere from this list.
    'GET /api/season/rewards': async (request, url) => {
      const seasonId = url.searchParams.get('season') ?? '';
      const rewards = ranks && seasons ? await seasons.rewardsOf(seasonId) : null;
      return rewards ? json(200, rewards) : failure(404, 'no_such_closed_season', 'no season of that id was closed here');
    },

    // One finished match, reported by the game page of a signed-in player for a Genesis that
    // player holds or rents at this moment, or by the page of a guest who plays it with a pass.
    // A guest's match counts under the holder's address: a holder and their own guest do not
    // confirm each other. A renter's match counts under the renter's own address.
    'POST /api/career/result': async request => {
      const guest = authorization(request).startsWith('Pass ');
      const { loan, refusal } = guest ? await loanFrom(request) : {};
      if (refusal) return refusal;
      const address = guest ? loan.lender : await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      const body = await bodyOf(request);
      if (!body || !Number.isInteger(body.tokenId)) return failure(400, 'bad_request', 'expected tokenId, matchId, team and winnerTeam');
      if (guest && loan.tokenId !== body.tokenId) return failure(403, 'not_the_lent_genesis', 'this link lends another Genesis');
      const renter = !guest && !(await chain.holds(address, body.tokenId)).holds;
      if (renter && !(rental && await rental.isRenting(body.tokenId, address))) {
        return failure(403, 'not_the_holder', 'this wallet neither holds nor rents that Genesis');
      }
      const result = await careers.record({
        tokenId: body.tokenId, address, matchId: body.matchId, team: body.team, winnerTeam: body.winnerTeam, guest, renter,
      });
      if (!result.credited && !result.career) return failure(400, result.reason, 'the result was not understood');
      return json(200, result);
    },

    // The rental of Genesis for $CHOG: the contract to pay and its fixed terms, or
    // {enabled: false} when no rental contract is set up.
    'GET /api/rental': async () => json(200, rental ? await rental.describe() : { enabled: false }),

    // Every Genesis its holder offers for rent now, with the price a day, read on the chain.
    'GET /api/rental/offers': async () => json(200, { offers: rental ? await rental.offers() : [] }),

    // The cheapest Genesis for rent now (three unless "limit" says otherwise), each with the
    // dollars of a day when the price is known: what a player without a Genesis is shown first.
    'GET /api/rental/cheapest': async (request, url) => {
      const limit = Number(url.searchParams.get('limit') ?? 3);
      if (!Number.isInteger(limit) || limit < 1 || limit > MAX_TOKENS_PER_READ) {
        return failure(400, 'bad_limit', `limit must be 1 to ${MAX_TOKENS_PER_READ}`);
      }
      return json(200, rental ? await rental.cheapest(limit) : { offers: [], offered: 0 });
    },

    // What these Genesis earned their holders by being rented, and how often, from the rental's
    // events as read so far (rental-ledger.js): public, like the chain. {enabled: false} when
    // the events are not read.
    'GET /api/rental/earned': async (request, url) => {
      const tokens = (url.searchParams.get('tokens') ?? '').split(',').filter(Boolean).map(Number);
      if (tokens.length === 0 || tokens.length > MAX_TOKENS_PER_READ
        || tokens.some(token => !Number.isInteger(token) || token < 1 || token > LAST_TOKEN_ID)) {
        return failure(400, 'bad_tokens', `tokens must be 1 to ${MAX_TOKENS_PER_READ} token ids separated by commas`);
      }
      return json(200, ledger ? ledger.earnedOf(tokens) : { enabled: false });
    },

    // The Genesis an address rents now. A public read, like what an address holds.
    'GET /api/rental/rented': async (request, url) => {
      const address = url.searchParams.get('address');
      if (!isAddress(address)) return failure(400, 'bad_address', 'address must be 0x followed by 40 hex digits');
      return json(200, { address: address.toLowerCase(), rentals: rental ? await rental.rentedBy(address) : [] });
    },

    // The money of an address in the rental: what it earned as a holder and may claim, what it is
    // owed back as a renter, its rentals both ways and the token ids a claim must name. Public,
    // as everything on the chain is.
    'GET /api/rental/account': async (request, url) => {
      const address = url.searchParams.get('address');
      if (!isAddress(address)) return failure(400, 'bad_address', 'address must be 0x followed by 40 hex digits');
      return json(200, rental && rental.account ? await rental.account(address) : { enabled: false });
    },

    // What is sold for $CHOG: the contract to pay, the token, the item and its price, read on
    // the chain; {enabled: false} when no shop is set up or the item is not on sale.
    'GET /api/shop': async () => json(200, shop ? await shop.describe() : { enabled: false }),

    // Which of these Genesis have the item, read on the chain. A purchase belongs to the
    // Genesis, so this is public like a career.
    'GET /api/shop/owned': async (request, url) => {
      const tokens = (url.searchParams.get('tokens') ?? '').split(',').filter(Boolean).map(Number);
      if (tokens.length === 0 || tokens.length > MAX_TOKENS_PER_READ
        || tokens.some(token => !Number.isInteger(token) || token < 1 || token > LAST_TOKEN_ID)) {
        return failure(400, 'bad_tokens', `tokens must be 1 to ${MAX_TOKENS_PER_READ} token ids separated by commas`);
      }
      return json(200, { tokens: shop ? await shop.ownedAmong(tokens) : [] });
    },

    // A signed-in holder lends one Genesis: the answer carries a pass, and whoever opens the
    // game with it plays that Genesis without a wallet until the pass stops counting.
    'POST /api/loan': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      const body = await bodyOf(request);
      const days = body?.days ?? MAX_LOAN_DAYS;
      if (!body || !Number.isInteger(body.tokenId) || !Number.isInteger(days) || days < 1 || days > MAX_LOAN_DAYS) {
        return failure(400, 'bad_request', `expected tokenId and, optionally, days from 1 to ${MAX_LOAN_DAYS}`);
      }
      if (!(await chain.holds(address, body.tokenId)).holds) {
        return failure(403, 'not_the_holder', 'this wallet does not hold that Genesis');
      }
      const { pass, tokenId, lender, expiresAt } = await sessions.lend({ tokenId: body.tokenId, lender: address, days });
      return json(200, { pass, tokenId, lender, expiresAt });
    },

    // What the pass in "Authorization: Pass ..." lends, if it still counts.
    'GET /api/loan': async request => {
      const { loan, refusal } = await loanFrom(request);
      return refusal ?? json(200, { tokenId: loan.tokenId, lender: loan.lender, expiresAt: loan.expiresAt });
    },

    // The holder takes the Genesis back: every pass handed out for it so far stops counting.
    'POST /api/loan/stop': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      const body = await bodyOf(request);
      if (!body || !Number.isInteger(body.tokenId)) return failure(400, 'bad_request', 'expected tokenId');
      if (!(await chain.holds(address, body.tokenId)).holds) {
        return failure(403, 'not_the_holder', 'this wallet does not hold that Genesis');
      }
      return json(200, await careers.stopLending(body.tokenId));
    },

    // What the game page needs to connect a wallet app when the browser has no wallet of its
    // own: the Reown project id of this deployment, or an empty text when none is set.
    'GET /api/wallet': async () => json(200, { reownProjectId }),

    // Whether the game must tell Photon who its player is when it connects: true once Photon's
    // dashboard asks these services (see docs/DEPLOY.md). The page reads this before it connects.
    // "build" is the build of the game served here, as it names itself to Photon, or an empty
    // text when these services do not know: a page that runs another one loads itself again.
    'GET /api/photon': async () => json(200, {
      authentication: Boolean(photon) && photonAuthentication,
      build: typeof build === 'function' ? await build() : build,
    }),

    // Asked by Photon's cloud when a player connects, never by a page: who is this player.
    'GET /api/photon/auth': async (request, url) => {
      if (!photon) return failure(404, 'not_found', 'no such endpoint');
      const { status, body } = await photon.authenticate(url.searchParams);
      return json(status, body);
    },

    // The price of $CHOG in US dollars, read from public sources and kept a few minutes (price.js):
    // usd is null while no source has answered. Public, and never a failure: a page without
    // a price shows amounts in $CHOG only.
    'GET /api/price': async () => json(200, price ? await price.describe()
      : { token: CHOG_TOKEN.toLowerCase(), usd: null, source: null, readAt: null, stale: true }),

    // Staking $CHOG on a match: the contract, the token and the terms, or {enabled: false}.
    'GET /api/wager': async () => json(200, wager ? await wager.describe() : { enabled: false }),

    // The stakes of the match the signed-in wallet is in, as the chain has them.
    'GET /api/wager/mine': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      return json(200, wager ? await wager.mine({ address }) : { enabled: false });
    },

    // What the signed-in wallet won and lost on its stakes of the last seven days, on the
    // referee's decisions, with the dollars of it when the price is known.
    'GET /api/wager/week': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      return json(200, wager ? await wager.weekOf({ address }) : { enabled: false });
    },

    // The referee's ticket to stake on the match the signed-in wallet is in.
    // Version 3: what the wallet signs (the permit of the token) and the stake these services bring with it.
    'GET /api/wager/permit': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      const answer = wager ? await wager.permit({ address }) : { refused: 'no_wager' };
      if (answer.refused) return json(409, { error: answer.refused, message: WAGER_REFUSALS[answer.refused] ?? 'refused' });
      return json(200, answer);
    },
    'POST /api/wager/relay': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      const body = await bodyOf(request);
      if (!body) return failure(400, 'bad_request', 'expected the ticket and the permit');
      const answer = wager ? await wager.relay({ ...body, address }) : { refused: 'no_wager' };
      if (answer.refused) {
        return json(409, { error: answer.refused, message: answer.message ?? WAGER_REFUSALS[answer.refused] ?? 'refused', hash: answer.hash });
      }
      return json(200, answer);
    },
    'POST /api/wager/ticket': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      const body = await bodyOf(request);
      if (!body) return failure(400, 'bad_request', 'expected team and amount');
      const ticket = wager ? await wager.ticket({ address, team: body.team, amount: body.amount }) : { refused: 'no_wager' };
      if (ticket.refused) return json(409, { error: ticket.refused, message: WAGER_REFUSALS[ticket.refused] ?? 'refused', amount: ticket.amount });
      return json(200, ticket);
    },

    // Locks the stakes of the match the signed-in wallet staked on, before the match begins.
    'POST /api/wager/lock': async request => {
      const address = await signedIn(request);
      if (!address) return failure(401, 'not_signed_in', 'sign in with the wallet first');
      const locked = wager ? await wager.lock({ address }) : { refused: 'no_wager' };
      if (locked.refused) return failure(409, locked.refused, WAGER_REFUSALS[locked.refused] ?? 'refused');
      return json(200, locked);
    },

    // What anyone may bring to the contract once a match is judged, with where its stakes
    // stand on the chain: public, like the chain.
    'GET /api/wager/result': async (request, url) => {
      const outcome = wager ? await wager.outcome(url.searchParams.get('gameId') ?? '') : null;
      return outcome ? json(200, outcome) : failure(404, 'unknown_match', 'the game server does not know this match');
    },
  };

  // One of Photon's webhooks. Its body is read without losing a digit: it carries 64-bit ids,
  // and what is sent back to Photon must be what came.
  async function webhook(request, name) {
    if (!photon) return failure(404, 'not_found', 'no such endpoint');
    let body;
    try {
      body = parseExact(await request.text());
    } catch {
      return json(400, { Status: 400, Error: 'BadRequest', Message: 'the body is not JSON' });
    }
    const answer = await photon.hook(name, body, request.headers.get('x-secretkey'));
    return json(answer.status, answer.body);
  }

  return async function handle(request) {
    const url = new URL(request.url);
    if (request.method === 'POST' && url.pathname.startsWith(WEBHOOKS)) return webhook(request, url.pathname.slice(WEBHOOKS.length));
    const route = routes[`${request.method} ${url.pathname}`];
    if (!route) return failure(404, 'not_found', 'no such endpoint');
    try {
      return await route(request, url);
    } catch (error) {
      console.error(`${request.method} ${url.pathname} failed:`, error);
      return failure(502, 'upstream_failed', 'the chain could not be read, try again');
    }
  };
}
