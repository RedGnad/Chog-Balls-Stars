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
//   VERIFIER_DLL    optional, the program that plays a match again (Chog.Quantum.Verifier.dll,
//                   built by tools/quantum/headless.py); default judge/ in SITE_DIR, with
//   QUANTUM_DB      the simulation assets of the build it judges; default quantum-db.json in
//                   SITE_DIR, and
//   QUANTUM_LUT     the tables the simulation reads; default judge/lut in SITE_DIR. All three
//                   are put in SITE_DIR by tools/release/assemble_site.py, so a site assembled
//                   on a machine with .NET needs none of these settings.
//   DOTNET          the .NET 8 runtime that runs the judge; default "dotnet" on the path.
//                   Without a judge, or without .NET, a match gets no verdict

import { mkdir, stat } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createApi } from './api.js';
import { createCareers } from './career.js';
import { CHAIN_ID, createChain, DEFAULT_RPC_URLS } from './chain.js';
import { createJudge } from './judge.js';
import { createMatches } from './matches.js';
import { careerFile, createHttpServer, dotnetReplay, matchFiles, photonSecret, reownProjectId, servedBuild, sessionSecret } from './node.js';
import { createPhoton } from './photon.js';
import { createReferee } from './referee.js';
import { createRental } from './rental.js';
import { createSessions } from './session.js';
import { createShop } from './shop.js';
import { createStatic } from './static.js';
import { createWager } from './wager.js';

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
const careers = createCareers(careerFile(dataFolder));
const sessions = createSessions({ secret: await sessionSecret(dataFolder, environment.SESSION_SECRET) });

const matchStore = matchFiles(join(dataFolder, 'matches'));
const matches = createMatches({ store: matchStore });
const wagerAddress = (environment.WAGER_ADDRESS ?? '').trim();
const refereeKey = (environment.REFEREE_KEY ?? '').trim();
if (Boolean(wagerAddress) !== Boolean(refereeKey)) {
  console.error('WAGER_ADDRESS and REFEREE_KEY go together: set both, or neither.');
  process.exit(1);
}
const referee = wagerAddress ? createReferee({ privateKey: refereeKey, chainId: CHAIN_ID, wager: wagerAddress }) : undefined;
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
console.log((await build())
  ? `Serving the build ${await build()}: a room or a match of another build is refused.`
  : 'The site folder names no build (no RELEASE.json): matches of any build are accepted and judged.');
const judge = verifier && (await exists(database))
  ? createJudge({
    matches,
    build,
    replay: dotnetReplay({
      dotnet: environment.DOTNET ?? 'dotnet', verifier: resolve(verifier), database, lut: tables, matchesRoot: matchStore.root,
    }),
  })
  : null;
// Stakes without a judge would only ever be given back after an hour: say so at the start.
if (referee && !judge) console.warn('WAGER_ADDRESS is set without VERIFIER_DLL: no match will get a verdict, stakes can only be refunded.');

const api = createApi({
  publicOrigin,
  chain,
  shop,
  rental,
  careers,
  sessions,
  photon: createPhoton({
    secret: await photonSecret(dataFolder, environment.PHOTON_SECRET),
    sessions, careers, chain, shop, rental, matches, build,
    onReplayEnded: judge ? gameId => judge.judge(gameId) : undefined,
  }),
  build,
  photonAuthentication: environment.PHOTON_AUTH === '1',
  wager: createWager({ chain, referee, matches }),
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
  return (await files(request)) ?? new Response('Not found', { status: 404 });
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
