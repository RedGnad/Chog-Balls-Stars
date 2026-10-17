// What Photon's cloud asks the game services while a match runs, on the Public Cloud:
//
//   - who a connecting player is (Custom Authentication): the answer names the player and puts
//     in a cookie, which the player never sees, the wallet that signed in or the pass presented;
//   - eleven webhooks: a room is created, joined, left, closed, its configs arrive, a player
//     asks to enter the simulation, was added, was removed, the clients' own results, and the
//     input history of the match as Photon's server recorded it.
//
// The one that decides something is "player/add": the Genesis a page declares is admitted only
// when the wallet behind the cookie holds or rents it, or the pass lends it; the second form
// only when that Genesis has it. Anything else enters as a recruit. The rest is kept as
// received (matches.js) so that the match can be played again and judged from Photon's record,
// not from what a page says.
//
// Requests and answers are the ones of Photon's manual (quantum/current/manual/webhooks and
// realtime/current/reference/custom-authentication, read on 2026-10-11): JSON, 200 to accept,
// 400 with {Status, Error, Message} to refuse, 503 to be asked again.

import { isAddress, LAST_TOKEN_ID } from './chain.js';
import { numberOf } from './matches.js';
import { buildOf } from './modes.js';

// Mastery level from which a Genesis plays its second form (GenesisForms.UnlockLevel in the game).
const SECOND_FORM_MASTERY_LEVEL = 2;
const NO_ASSET = { Id: { Value: 0 } };

const encoder = new TextEncoder();

// Compared in constant time, so that the time an answer takes says nothing about the secret.
function sameSecret(given, expected) {
  if (typeof given !== 'string') return false;
  const a = encoder.encode(given);
  const b = encoder.encode(expected);
  let difference = a.length ^ b.length;
  for (let index = 0; index < b.length; index++) difference |= (a[index] ?? 0) ^ b[index];
  return difference === 0;
}

const randomName = prefix => prefix + Array.from(crypto.getRandomValues(new Uint8Array(9)), byte => byte.toString(16).padStart(2, '0')).join('');

const accepted = (body = {}) => ({ status: 200, body });
const refused = (error, message) => ({ status: 400, body: { Status: 400, Error: error, Message: message } });

/**
 * @param {object} options
 * @param {string} options.secret what Photon's dashboard sends with every request, at least 32
 *   characters: as the X-SecretKey header of a webhook, as the "key" parameter of an authentication
 * @param {ReturnType<import('./session.js').createSessions>} options.sessions
 * @param {ReturnType<import('./career.js').createCareers>} options.careers
 * @param {ReturnType<import('./chain.js').createChain>} options.chain
 * @param {ReturnType<import('./shop.js').createShop>} [options.shop]
 * @param {ReturnType<import('./rental.js').createRental>} [options.rental]
 * @param {ReturnType<import('./matches.js').createMatches>} options.matches
 * @param {(gameId: string) => Promise<void>} [options.onReplayEnded] called once when the last
 *   slice of a match's input history was kept; its failure is logged and changes no answer
 * @param {string | (() => Promise<string>)} [options.build] the build of the game these
 *   services serve and can judge, as that build names itself to Photon (its AppVersion), or a
 *   function that reads it (a site folder can be assembled again while the services run); a
 *   room of another build is refused. Empty when the services do not know what they serve:
 *   every room is then accepted
 */
export function createPhoton({ secret, sessions, careers, chain, shop, rental, standIns, matches, onReplayEnded, build = '' }) {
  if (typeof secret !== 'string' || secret.length < 32) throw new Error('The Photon secret must be at least 32 characters');
  const servedNow = typeof build === 'function' ? build : async () => build;

  /**
   * The answer to Photon's question "who is this": {ResultCode: 1, UserId, AuthCookie} lets the
   * player in. A signed-in wallet is named by its address, so one wallet is in a room once; a
   * guest and a visitor get a name of their own. Nothing here checks a Genesis: that is done
   * when the player enters a simulation, with the cookie this answer sets.
   */
  async function authenticate(parameters) {
    if (!sameSecret(parameters.get('key'), secret)) return { status: 403, body: { ResultCode: 3, Message: 'not asked by the game server' } };
    const session = parameters.get('session');
    const pass = parameters.get('pass');
    if (session) {
      const address = await sessions.addressOf(session);
      if (!address) return { status: 200, body: { ResultCode: 2, Message: 'the wallet session has run out: sign in again' } };
      return { status: 200, body: { ResultCode: 1, UserId: address, AuthCookie: { address } } };
    }
    if (pass) {
      const loan = await sessions.loanOf(pass);
      if (!loan || loan.expired) return { status: 200, body: { ResultCode: 2, Message: 'this link does not lend a Genesis any more' } };
      const { tokenId, lender, issuedAt } = loan;
      return { status: 200, body: { ResultCode: 1, UserId: randomName('guest-'), AuthCookie: { pass: { tokenId, lender, issuedAt } } } };
    }
    return { status: 200, body: { ResultCode: 1, UserId: randomName('visitor-'), AuthCookie: { visitor: true } } };
  }

  // How the player behind a cookie may play that Genesis now: 'holder', 'renter', 'guest',
  // 'stand-in' (standin.js: never counted for a career), or null.
  async function rightTo(tokenId, cookie) {
    if (!Number.isInteger(tokenId) || tokenId < 1 || tokenId > LAST_TOKEN_ID) return null;
    if (isAddress(cookie?.address)) {
      if ((await chain.holds(cookie.address, tokenId)).holds) return 'holder';
      if (rental && await rental.isRenting(tokenId, cookie.address)) return 'renter';
      return standIns && await standIns.standsIn(cookie.address, tokenId) ? 'stand-in' : null;
    }
    const pass = cookie?.pass;
    if (pass && pass.tokenId === tokenId && isAddress(pass.lender)) {
      // A pass counts while its holder has not taken the Genesis back and still holds it.
      const stopped = pass.issuedAt <= await careers.lendingStoppedAt(tokenId);
      return !stopped && (await chain.holds(pass.lender, tokenId)).holds ? 'guest' : null;
    }
    return null;
  }

  async function secondFormOf(tokenId) {
    if (shop && (await shop.ownedAmong([tokenId])).includes(tokenId)) return true;
    const [career] = await careers.get([tokenId]);
    return (career?.mastery?.level ?? 1) >= SECOND_FORM_MASTERY_LEVEL;
  }

  /**
   * A player asks to enter the simulation with the RuntimePlayer its page built. What the page
   * may not decide is put right: the Genesis (none unless the player has a right to it), its
   * form, a loadout named directly, and a character left to the machine. The answer replaces
   * the player's data only when something had to be put right, and then with every field the
   * page sent: Photon takes the answer whole.
   */
  async function addPlayer(request) {
    const cookie = request.AuthCookie ?? {};
    const asked = request.RuntimePlayer;
    const askedToken = numberOf(asked?.GenesisTokenId) ?? 0;
    const askedForm = numberOf(asked?.GenesisForm) ?? 0;
    const right = askedToken > 0 ? await rightTo(askedToken, cookie) : null;
    const tokenId = right ? askedToken : 0;
    const form = tokenId > 0 && askedForm === 1 && await secondFormOf(tokenId) ? 1 : 0;

    await matches.admitted(request.GameId, {
      userId: request.UserId, actorNr: numberOf(request.ActorNr), playerSlot: numberOf(request.PlayerSlot),
      address: isAddress(cookie.address) ? cookie.address : null, lender: right === 'guest' ? cookie.pass.lender : null,
      right, tokenId, form, askedToken, askedForm, ...(right === 'stand-in' ? { standIn: true } : {}),
    });

    if (!asked || typeof asked !== 'object') return accepted();
    const namedLoadout = (numberOf(asked.GenesisLoadout?.Id?.Value) ?? 0) !== 0;
    const asIs = tokenId === askedToken && form === askedForm && !namedLoadout && asked.ForceAI !== true;
    if (asIs) return accepted();
    return accepted({ RuntimePlayer: { ...asked, GenesisTokenId: tokenId, GenesisForm: form, GenesisLoadout: NO_ASSET, ForceAI: false } });
  }

  const hooks = {
    // A room name can be used again another day, and a match must share its name with no
    // other: neither in what is kept of it nor on the chain, where its stakes are held under
    // that name. Photon uses the name given back here in every later request for this room.
    'game/create': async request => {
      // A page that kept an older build would play a match nobody here can judge: the judge
      // only replays right what was played with its own simulation.
      const served = await servedNow();
      if (served && buildOf(request.AppVersion) !== served) {
        return refused('AnotherBuild', 'this page runs another version of the game than the one served here: load it again');
      }
      const gameId = `${request.GameId}~${randomName('')}`;
      await matches.entered(gameId, {
        how: 'create', userId: request.UserId, authCookie: request.AuthCookie ?? null,
        app: { appId: request.AppId, appVersion: request.AppVersion, region: request.Region, cloud: request.Cloud, roomName: request.RoomName },
      });
      return accepted({ GameId: gameId });
    },
    'game/join': async request => {
      await matches.entered(request.GameId, { how: 'join', userId: request.UserId, authCookie: request.AuthCookie ?? null });
      return accepted();
    },
    'game/leave': async request => {
      await matches.left(request.GameId, { userId: request.UserId, actorNr: numberOf(request.ActorNr), isInactive: request.IsInactive === true });
      return accepted();
    },
    'game/close': async request => {
      await matches.closed(request.GameId, { closeReason: numberOf(request.CloseReason) ?? null });
      return accepted();
    },
    // Kept as received and accepted as sent: what a match was played with is read again when
    // it is judged.
    'game/configs': async request => {
      await matches.configs(request.GameId, request);
      return accepted();
    },
    'player/add': addPlayer,
    'player/added': async request => {
      await matches.added(request.GameId, {
        userId: request.UserId, actorNr: numberOf(request.ActorNr), playerSlot: numberOf(request.PlayerSlot), player: numberOf(request.Player),
      });
      return accepted();
    },
    'player/removed': async request => {
      await matches.removed(request.GameId, {
        userId: request.UserId, actorNr: numberOf(request.ActorNr), player: numberOf(request.Player), reason: numberOf(request.Reason) ?? null,
      });
      return accepted();
    },
    // What the players' own pages say happened: kept, and deciding nothing.
    'game/result': async request => {
      await matches.claims(request.GameId, request);
      return accepted();
    },
    'replay/start': async request => {
      await matches.started(request.GameId, request);
      return accepted({ Skip: false });
    },
    'replay/chunk': async request => {
      const number = numberOf(request.ChunkNumber);
      if (!Number.isInteger(number) || number < 0) return refused('BadChunk', 'ChunkNumber must be a whole number');
      const { ended } = await matches.chunk(request.GameId, number, request);
      // The match is judged after the answer: Photon is not kept waiting for it.
      if (ended && onReplayEnded) {
        Promise.resolve().then(() => onReplayEnded(request.GameId)).catch(error => console.error(`Judging ${request.GameId} failed:`, error));
      }
      return accepted();
    },
  };

  /**
   * One webhook: "name" is what follows the base address ("player/add"), "request" its JSON
   * body read without losing a digit, "secretKey" the X-SecretKey header. Answers {status, body}.
   */
  async function hook(name, request, secretKey) {
    if (!sameSecret(secretKey, secret)) return { status: 403, body: { Status: 403, Error: 'NotPhoton', Message: 'the secret key is missing or wrong' } };
    const handler = hooks[name];
    if (!handler) return { status: 404, body: { Status: 404, Error: 'NoSuchHook', Message: name } };
    if (!request || typeof request !== 'object' || typeof request.GameId !== 'string' || request.GameId === '') {
      return refused('BadRequest', 'a webhook carries a GameId');
    }
    try {
      return await handler(request);
    } catch (error) {
      // Photon asks again after a 503, three times: a chain that did not answer gets its chance.
      console.error(`Webhook ${name} of ${request.GameId} failed:`, error);
      return { status: 503, body: { Status: 503, Error: 'TryAgain', Message: 'the game services could not answer' } };
    }
  }

  return { authenticate, hook, hookNames: Object.keys(hooks) };
}
