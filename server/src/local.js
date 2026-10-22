// Local stand-in for the hosted game services: the API alone, on the loopback address, behind
// the WebGL test server (tools/unity/serve_webgl.py), which forwards /api/ here so the game
// page and the services share one origin. For the whole game behind one address, see host.js.
//
//   node server/src/local.js [--port 8090] [--data .artifacts/services]
//                            [--pretend-holder 0xaddress=408,1795]
//                            [--shop 0xaddress] [--rental 0xaddress]
//                            [--rpc http://127.0.0.1:8545]
//                            [--wager 0xaddress --referee-key-file <file>
//                             [--wager-network testnet --wager-token 0xaddress] [--wager-rpc <url>]]
//                            [--verifier <Chog.Quantum.Verifier.dll> --quantum-db <quantum-db.json> [--dotnet <dotnet>]]
//                            [--photon-auth] [--photon-secret <text>]
//                            [--reown-project-id <32 hex digits>]
//                            [--season <id> --season-ends <ISO time> [--season-starts <ISO time>]]
//
// --pretend-holder is for tests only: that address is answered as the holder of those tokens,
// whatever the chain says, so the signed path can be run with a throwaway key. Never use it on
// a server that players reach.
// --shop is the deployed shop contract; without it nothing is sold. --rental is the deployed
// rental contract; without it nothing is rented. --rpc reads the chain from another endpoint,
// for instance a local copy of Monad mainnet on which the contracts were deployed for a test.
// --wager is the deployed wager contract and --referee-key-file a file that holds its
// referee's key (64 hex digits): without both, no match is staked on. The wager is on the chain
// the Genesis are read on, unless --wager-network testnet puts it on Monad's testnet, paid in
// the test token --wager-token; --wager-rpc reads it from another endpoint, for instance a
// local chain started as the testnet (anvil --chain-id 10143). --verifier is the program
// that plays a match again and --quantum-db the simulation assets of the build it judges:
// without them a match gets no verdict. --photon-auth tells the game to connect to Photon with
// its session. The secret Photon's requests must carry is --photon-secret, else one kept in
// the data folder (photon-secret). --reown-project-id, else the text of the file
// reown-project-id in the data folder, lets a page without a wallet connect a wallet app;
// without either, no such connection is offered. --season and --season-ends name the ladder
// season; once its end has passed it is closed once (seasons.js).

import { mkdir, readFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createApi } from './api.js';
import { createCareers } from './career.js';
import { createChain, isAddress } from './chain.js';
import { createJudge } from './judge.js';
import { createMatches } from './matches.js';
import { careerFile, createHttpServer, dotnetReplay, matchFiles, photonSecret, reownProjectId, sessionSecret } from './node.js';
import { createPhoton } from './photon.js';
import { createPrice } from './price.js';
import { createReferee } from './referee.js';
import { createRental } from './rental.js';
import { createRanks } from './ranks.js';
import { createSeasons, seasonFrom } from './seasons.js';
import { createSessions } from './session.js';
import { createShop } from './shop.js';
import { createWager, wagerNetwork } from './wager.js';

function option(name, fallback) {
  const position = process.argv.indexOf(name);
  return position > 0 ? process.argv[position + 1] : fallback;
}

const port = Number(option('--port', '8090'));
const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const dataFolder = resolve(option('--data', join(root, '.artifacts/services')));
await mkdir(dataFolder, { recursive: true });

const rpc = option('--rpc');
let chain = createChain(rpc ? { rpcUrls: [rpc] } : {});
// The shop and the rental read the chain as it is: a pretended holder changes who holds a
// Genesis, not what was bought or rented.
const shop = createShop({ chain, address: option('--shop', '') });
// The price of $CHOG in dollars is read from the same public sources as in production.
const price = createPrice();
const rental = createRental({ chain, address: option('--rental', ''), price });
const pretend = option('--pretend-holder');
if (pretend) {
  const [address, list] = pretend.split('=');
  const tokens = (list ?? '').split(',').map(Number).filter(Number.isInteger);
  if (!isAddress(address) || tokens.length === 0) throw new Error('--pretend-holder expects 0xaddress=token,token');
  const real = chain;
  const pretended = address.toLowerCase();
  chain = {
    ...real,
    ownedBy: async (asked, options) => (asked.toLowerCase() === pretended
      ? { tokens, block: (await real.owners(options)).block }
      : real.ownedBy(asked, options)),
    holds: async (asked, tokenId) => (asked.toLowerCase() === pretended
      ? { holds: tokens.includes(tokenId), block: 0 }
      : real.holds(asked, tokenId)),
  };
  console.warn(`TEST MODE: ${pretended} is answered as the holder of ${tokens.join(', ')}, whatever the chain says.`);
}

const careers = createCareers(careerFile(dataFolder));
const sessions = createSessions({ secret: await sessionSecret(dataFolder) });

// What Photon's cloud asks (photon.js). Here it can only be asked by a test that plays
// Photon's part, since this server is not reachable from outside this computer.
const matchStore = matchFiles(join(dataFolder, 'matches'));
const matches = createMatches({ store: matchStore });
const ranks = createRanks({ store: matchStore, matches });
const seasons = createSeasons({ ranks, store: matchStore, season: seasonFrom({ id: option('--season', ''), endsAt: option('--season-ends', ''), startsAt: option('--season-starts', '') }) });
seasons.watch();
const wagerAddress = option('--wager', '');
const refereeKeyFile = option('--referee-key-file');
const wagerRpc = option('--wager-rpc');
const wagerPlace = wagerNetwork({
  network: option('--wager-network', 'mainnet'),
  rpcUrls: wagerRpc ? [wagerRpc] : [],
  token: option('--wager-token', ''),
});
// On mainnet, without an endpoint of its own, the wager is read where the Genesis are.
const wagerChain = !wagerPlace.test && !wagerRpc ? chain : createChain({ rpcUrls: wagerPlace.rpcUrls });
const referee = wagerAddress && refereeKeyFile
  ? createReferee({ privateKey: (await readFile(refereeKeyFile, 'utf8')).trim(), chainId: wagerPlace.chainId, wager: wagerAddress })
  : undefined;
if (referee) console.log(`Wager ${wagerAddress} on ${wagerPlace.chainName} (chain ${wagerPlace.chainId}), paid in ${wagerPlace.token}`);
const verifier = option('--verifier');
const judge = verifier
  ? createJudge({
    matches,
    replay: dotnetReplay({
      dotnet: option('--dotnet', 'dotnet'), verifier: resolve(verifier), database: resolve(option('--quantum-db', '')),
      lut: resolve(option('--quantum-lut', join(root, 'game/Assets/Photon/Quantum/Resources/LUT'))), matchesRoot: matchStore.root,
    }),
  })
  : null;
const photon = createPhoton({
  secret: await photonSecret(dataFolder, option('--photon-secret')),
  sessions, careers, chain, shop, rental, matches, ratingBandOf: address => ranks.bandOf(address),
  onReplayEnded: judge ? async gameId => {
    const verdict = await judge.judge(gameId);
    if (verdict) await ranks.record(gameId, verdict);
  } : undefined,
});

const handle = createApi({
  chain,
  shop,
  rental,
  careers,
  ranks,
  seasons,
  matches,
  sessions,
  photon,
  photonAuthentication: process.argv.includes('--photon-auth'),
  wager: createWager({ chain: wagerChain, referee, matches, network: wagerPlace, store: matchStore, price }),
  price,
  reownProjectId: await reownProjectId(dataFolder, option('--reown-project-id')),
});

// Loopback only, behind our own test server: its forwarded address of the game page is trusted.
createHttpServer(handle, { trustForwarded: true })
  .listen(port, '127.0.0.1', () => console.log(`Game services: http://127.0.0.1:${port}/api/health (data in ${dataFolder})`));
