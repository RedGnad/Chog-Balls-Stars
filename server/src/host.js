// The whole game behind one address: the WebGL build, the hero page, the portraits and the
// services, in one Node process. This is the entry a hosted web service starts.
//
//   node server/src/host.js
//
// Settings come from the environment:
//   PUBLIC_ORIGIN   the public address of the game, for example https://game.example. Required:
//                   it is written in the message a wallet signs, so it is never taken from a request.
//   PORT            default 10000
//   HOST            default 0.0.0.0
//   SITE_DIR        folder that holds game/ (the WebGL build) and genesis-art/256/ (the portraits);
//                   default site/ at the root of the repository
//   HERO_DIR        folder of the hero page; default web/hero
//   DATA_DIR        where the careers and the session secret are kept; it must survive a restart;
//                   default data/ at the root of the repository
//   SESSION_SECRET  optional, at least 32 characters; otherwise one is made once and kept in DATA_DIR
//   RPC_URLS        optional, Monad RPC endpoints separated by commas, tried in order
//   SHOP_ADDRESS    optional, the deployed shop contract (contracts/src/ChogShop.sol); without it
//                   nothing is sold and the game shows no purchase
//   RENTAL_ADDRESS  optional, the deployed rental contract (contracts/src/ChogRental.sol); without
//                   it nothing is rented and the game shows no rental
//   PHOTON_SECRET   optional, at least 32 characters: what Photon's dashboard sends with its
//                   requests (WebHookSecret, and the "key" parameter of the authentication);
//                   otherwise one is made once and kept in DATA_DIR (photon-secret)
//   PHOTON_AUTH     1 once Photon's dashboard asks these services who a player is: the game
//                   then connects to Photon with its session or its pass
//   REOWN_PROJECT_ID  optional, the Reown project id (32 hex digits) that lets a player on a
//                   browser without a wallet connect a wallet app; otherwise the text of the
//                   file reown-project-id in DATA_DIR, otherwise no such connection is offered
//   WAGER_ADDRESS   optional, the deployed wager contract (contracts/src/ChogWager.sol), with
//   REFEREE_KEY     the key of its referee, 64 hex digits: together or not at all. Without
//                   them no match is staked on
//   STAND_INS       optional, "0xCollection:tokenId=genesisTokenId[,...]": the holder of that NFT
//                   plays that Genesis without holding it, to film or test it (standin.js); it
//                   writes no career. Empty in the public game
//   PREVIEW_BUILDS  optional, "any" or builds separated by commas that may open rooms although they
//                   are not the build served: a local build is then played on Photon's cloud to be
//                   looked at; the judge calls its matches void (no career, no stake settled)
//   CAREERS_ADDRESS optional, the careers register on Monad mainnet (contracts/src/ChogCareers.sol);
//                   needs REFEREE_KEY, whose address writes it and pays its gas. Without it the
//                   careers are kept in a file of DATA_DIR, lost when a free host restarts
//   WAGER_NETWORK   "mainnet" (default) or "testnet": the chain the wager is on. The Genesis
//                   are read on mainnet whatever this says
//   WAGER_TOKEN     on the testnet, the test token the wager is paid in (contracts/src/testnet/
//                   TestChog.sol); on mainnet it is $CHOG and this stays empty
//   WAGER_RPC_URLS  optional, endpoints of the wager's chain separated by commas; default the
//                   public ones of the Monad documentation
//   VERIFIER_DLL    optional, the program that plays a match again (Chog.Quantum.Verifier.dll,
//                   built by tools/quantum/headless.py); default judge/ in SITE_DIR, with
//   QUANTUM_DB      the simulation assets of the build it judges; default quantum-db.json in
//                   SITE_DIR, and
//   QUANTUM_LUT     the tables the simulation reads; default judge/lut in SITE_DIR. All three
//                   are put in SITE_DIR by tools/release/assemble_site.py, so a site assembled
//                   on a machine with .NET needs none of these settings.
//   DOTNET          the .NET 8 runtime that runs the judge; default "dotnet" on the path.
//                   Without a judge, or without .NET, a match gets no verdict
//   UPSTASH_REDIS_REST_URL, UPSTASH_REDIS_REST_TOKEN
//                   the durable store for immutable player rank events, kept server-side.
//   RANK_ENABLED    1 only after that store and a real signed-in match have been verified.
//   SEASON_ID, SEASON_ENDS_AT  optional, together: the ladder season (1 to 40 letters, digits or -)
//                   and the ISO time it ends; SEASON_STARTS_AT optional. Once the end has passed the
//                   season is closed once: every wallet above Gold gets its reset event (seasons.js)
//                   Without both store settings, the player ladder stays disabled.

import { mkdir, stat } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createApi } from './api.js';
import { createCareers } from './career.js';
import { createChainCareers } from './careers-onchain.js';
import { createChain, DEFAULT_RPC_URLS } from './chain.js';
import { createJudge } from './judge.js';
import { createMatches } from './matches.js';
import { careerFile, createHttpServer, dotnetReplay, matchFiles, photonSecret, reownProjectId, servedBuild, servedPhotonVersion, sessionSecret } from './node.js';
import { createPhoton } from './photon.js';
import { createReferee } from './referee.js';
import { createRental } from './rental.js';
import { createRanks } from './ranks.js';
import { createRemoteRankStore } from './rank-store.js';
import { createSeasons, seasonFrom } from './seasons.js';
import { createSender } from './sender.js';
import { createSessions } from './session.js';
import { createShop } from './shop.js';
import { createStandIns } from './standin.js';
import { createStatic, movedToPublic } from './static.js';
import { createWager, wagerNetwork } from './wager.js';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const environment = process.env;

const publicOrigin = (environment.PUBLIC_ORIGIN ?? '').replace(/\/+$/, '');
if (!/^https?:\/\/[^/]+$/.test(publicOrigin)) {
  console.error('PUBLIC_ORIGIN must be set to the public address of the game, for example https://game.example');
  process.exit(1);
}

const siteFolder = resolve(environment.SITE_DIR ?? join(root, 'site'));
const heroFolder = resolve(environment.HERO_DIR ?? join(root, 'web/hero'));
const dataFolder = resolve(environment.DATA_DIR ?? join(root, 'data'));
await mkdir(dataFolder, { recursive: true });

const gameFolder = join(siteFolder, 'game');
if (!(await stat(join(gameFolder, 'index.html')).catch(() => null))) {
  console.error(`No WebGL build at ${gameFolder}: put the build there (index.html, Build/, TemplateData/).`);
  process.exit(1);
}

const rpcUrls = (environment.RPC_URLS ?? '').split(',').map(url => url.trim()).filter(Boolean);
const chain = createChain({ rpcUrls: rpcUrls.length > 0 ? rpcUrls : DEFAULT_RPC_URLS });
const shop = createShop({ chain, address: (environment.SHOP_ADDRESS ?? '').trim() });
const rental = createRental({ chain, address: (environment.RENTAL_ADDRESS ?? '').trim() });
const standIns = createStandIns({ chain, setting: environment.STAND_INS ?? '' });
const sessions = createSessions({ secret: await sessionSecret(dataFolder, environment.SESSION_SECRET) });

const matchStore = matchFiles(join(dataFolder, 'matches'));
const matches = createMatches({ store: matchStore });
const rankRequested = environment.RANK_ENABLED === '1';
const rankUrl = (environment.UPSTASH_REDIS_REST_URL ?? '').trim();
const rankToken = (environment.UPSTASH_REDIS_REST_TOKEN ?? '').trim();
let rankStore = null;
let rankStorageStatus = 'not_configured';
if (rankUrl && rankToken) {
  try {
    const candidate = createRemoteRankStore({ url: rankUrl, token: rankToken });
    if (!await candidate.ping() || !await candidate.probeWrite()) throw new Error('storage probe failed');
    rankStore = candidate;
    rankStorageStatus = 'writable';
  } catch {
    rankStorageStatus = 'unavailable';
    console.warn('Player rank storage is unavailable; no rank points will be credited.');
  }
}
if (rankRequested && !rankStore) console.warn('Player rank is disabled: persistent storage is not connected.');
const ranks = rankRequested && rankStore ? createRanks({ store: rankStore, matches }) : null;
const season = seasonFrom({ id: environment.SEASON_ID, endsAt: environment.SEASON_ENDS_AT, startsAt: environment.SEASON_STARTS_AT });
const seasons = ranks ? createSeasons({ ranks, store: rankStore, season }) : null;
if (season) console.log(`Ladder season ${season.id} ends at ${season.endsAt}${ranks ? '' : ' (player rank is disabled: it will not be closed here)'}`);
// Once a minute: a season whose end has passed is closed, once.
seasons?.watch();
const wagerAddress = (environment.WAGER_ADDRESS ?? '').trim();
const refereeKey = (environment.REFEREE_KEY ?? '').trim();
if (Boolean(wagerAddress) !== Boolean(refereeKey)) {
  console.error('WAGER_ADDRESS and REFEREE_KEY go together: set both, or neither.');
  process.exit(1);
}
const wagerRpcUrls = (environment.WAGER_RPC_URLS ?? '').split(',').map(url => url.trim()).filter(Boolean);
const wagerPlace = wagerNetwork({
  network: (environment.WAGER_NETWORK ?? '').trim() || 'mainnet',
  rpcUrls: wagerRpcUrls,
  token: (environment.WAGER_TOKEN ?? '').trim(),
});
// On mainnet, without endpoints of its own, the wager is read where the Genesis are.
const wagerChain = !wagerPlace.test && wagerRpcUrls.length === 0 ? chain : createChain({ rpcUrls: wagerPlace.rpcUrls });
const referee = wagerAddress ? createReferee({ privateKey: refereeKey, chainId: wagerPlace.chainId, wager: wagerAddress }) : undefined;
if (referee) console.log(`Wager ${wagerAddress} on ${wagerPlace.chainName} (chain ${wagerPlace.chainId}), paid in ${wagerPlace.token}`);
// What the services send themselves on Monad mainnet, with the referee's key: the careers after
// each judged match, a holder's stop to lending, the closing of a rental whose Genesis was sold.
const sender = refereeKey ? createSender({ rpcUrls: rpcUrls.length > 0 ? rpcUrls : DEFAULT_RPC_URLS, privateKey: refereeKey }) : undefined;
// The careers: on chain when CAREERS_ADDRESS names the register (and the referee can write it),
// otherwise in a file of the data folder (development, tests).
const careersAddress = (environment.CAREERS_ADDRESS ?? '').trim();
if (careersAddress && !sender) {
  console.error('CAREERS_ADDRESS needs REFEREE_KEY: the referee writes the careers.');
  process.exit(1);
}
const chainCareers = careersAddress ? createChainCareers({ chain, address: careersAddress, sender }) : undefined;
const careers = chainCareers ?? createCareers(careerFile(dataFolder));
console.log(chainCareers ? `Careers on chain at ${careersAddress}, written by ${sender.address}` : `Careers in ${dataFolder}`);
// The judge: the program that plays a match again. An assembled site carries it with the
// assets and the tables of its build (tools/release/assemble_site.py); the settings name
// another one. Without a program, or without the assets, no match gets a verdict.
const exists = async path => Boolean(await stat(path).catch(() => null));
const assembledJudge = join(siteFolder, 'judge/Chog.Quantum.Verifier.dll');
const verifier = (environment.VERIFIER_DLL ?? '').trim() || ((await exists(assembledJudge)) ? assembledJudge : '');
const database = resolve(environment.QUANTUM_DB ?? join(siteFolder, 'quantum-db.json'));
const assembledTables = join(siteFolder, 'judge/lut');
const tables = resolve(environment.QUANTUM_LUT
  ?? ((await exists(assembledTables)) ? assembledTables : join(root, 'game/Assets/Photon/Quantum/Resources/LUT')));
if (verifier && !(await exists(database))) {
  console.warn(`The judge has no simulation assets at ${database}: no match will get a verdict.`);
}
// The build served here, read each time it is needed: the site folder can be assembled again
// while this program runs, and the game and its judge change together there.
const build = () => servedBuild(siteFolder);
// What its pages tell Photon (Chog.Screens.BuildIdentity): rooms and matches are compared with that.
const photonVersion = () => servedPhotonVersion(siteFolder);
console.log(`Photon names this game ${(await photonVersion()) || '(any)'}.`);
console.log((await build())
  ? `Serving the build ${await build()}: a room or a match of another build is refused.`
  : 'The site folder names no build (no RELEASE.json): matches of any build are accepted and judged.');
const judge = verifier && (await exists(database))
  ? createJudge({
    matches,
    build: photonVersion,
    replay: dotnetReplay({
      dotnet: environment.DOTNET ?? 'dotnet', verifier: resolve(verifier), database, lut: tables, matchesRoot: matchStore.root,
    }),
  })
  : null;
// Stakes without a judge would only ever be given back after an hour: say so at the start.
if (referee && !judge) console.warn('WAGER_ADDRESS is set without VERIFIER_DLL: no match will get a verdict, stakes can only be refunded.');

// A judged match goes on chain once: the careers of the Genesis that played it. What happened to
// the last matches is kept in memory and shown at /api/careers/recent, since the host's own log
// is not readable from outside: the verdict, and the transaction or why there is none.
const recorded = new Set();
const recent = [];
function remember(entry) {
  recent.unshift({ at: new Date().toISOString(), ...entry });
  recent.length = Math.min(recent.length, 30);
}
async function judgeAndRecord(gameId) {
  let verdict;
  try {
    verdict = await judge.judge(gameId);
  } catch (error) {
    remember({ gameId, judged: false, why: error.message });
    throw error;
  }
  if (!verdict) {
    remember({ gameId, judged: false, why: 'the record of the match is not whole yet' });
    return verdict;
  }
  const summary = { gameId, judged: true, void: Boolean(verdict.void), why: verdict.why ?? null, winnerTeam: verdict.winnerTeam ?? null, players: verdict.players ?? null };
  if (ranks) {
    try {
      const rank = await ranks.record(gameId, verdict);
      console.log(`Rank of ${gameId}: ${rank.reason ?? 'recorded'}`);
    } catch (error) {
      console.warn(`Rank of ${gameId} not recorded: ${error.message}`);
    }
  }
  if (!chainCareers || verdict.void || recorded.has(gameId)) {
    remember({ ...summary, recorded: false, reason: !chainCareers ? 'careers kept in a file' : verdict.void ? 'void' : 'already sent' });
    return verdict;
  }
  recorded.add(gameId);
  const match = await matches.get(gameId);
  const admissions = (match?.admissions ?? []).map(({ player, tokenId, right, standIn }) => ({ player, tokenId, right, ...(standIn ? { standIn } : {}) }));
  chainCareers.recordMatch(gameId, verdict, match?.admissions ?? [])
    .then(result => {
      remember({ ...summary, admissions, recorded: result.recorded, reason: result.reason ?? null, hash: result.hash ?? null, entries: result.entries ?? null });
      console.log(`Careers of ${gameId}: ${result.recorded ? 'recorded ' + result.hash : 'nothing recorded (' + result.reason + ')'}`);
    })
    .catch(error => {
      remember({ ...summary, admissions, recorded: false, reason: error.message });
      console.warn(`Careers of ${gameId} not recorded: ${error.message}`);
    });
  return verdict;
}

// A rental whose Genesis was sold is closed soon after, so its renter is paid back the time not
// delivered: one read a minute, a transaction only when there is one to close.
if (rental.address && sender) {
  setInterval(() => {
    rental.closeSold(sender).catch(error => console.warn(`Sold rentals not closed: ${error.message}`));
  }, 60000).unref();
}

const api = createApi({
  publicOrigin,
  chain,
  shop,
  rental,
  standIns,
  careers,
  ranks,
  rankStorageStatus,
  matches,
  recentRecords: () => recent,
  sessions,
  photon: createPhoton({
    secret: await photonSecret(dataFolder, environment.PHOTON_SECRET),
    sessions, careers, chain, shop, rental, standIns, matches, build: photonVersion,
    ratingBandOf: ranks ? address => ranks.bandOf(address) : undefined,
    previewBuilds: (environment.PREVIEW_BUILDS ?? '').trim(),
    onReplayEnded: judge ? gameId => judgeAndRecord(gameId) : undefined,
  }),
  build,
  photonAuthentication: environment.PHOTON_AUTH === '1',
  wager: createWager({ chain: wagerChain, referee, matches, network: wagerPlace }),
  reownProjectId: await reownProjectId(dataFolder, environment.REOWN_PROJECT_ID),
});

const files = createStatic([
  // A portrait never changes; the game and the page are read again when they do.
  { prefix: '/genesis-art/', root: join(siteFolder, 'genesis-art'), cache: 'public, max-age=86400' },
  { prefix: '/hero/', root: heroFolder },
  { prefix: '/', root: gameFolder },
]);

async function handle(request) {
  const path = new URL(request.url).pathname;
  if (path.startsWith('/api/')) return api(request);
  return movedToPublic(request, publicOrigin) ?? (await files(request)) ?? new Response('Not found', { status: 404 });
}

const port = Number(environment.PORT ?? 10000);
const host = environment.HOST ?? '0.0.0.0';
createHttpServer(handle).listen(port, host, () => {
  console.log(`Game: ${publicOrigin}/ (listening on ${host}:${port})`);
  console.log(`Build ${gameFolder}, hero page ${heroFolder}, data ${dataFolder}`);
  console.log(judge
    ? `Judge ${verifier}, run with ${environment.DOTNET ?? 'dotnet'}`
    : 'No judge: a match gets no verdict (see VERIFIER_DLL at the top of host.js)');
});
