// Plays Photon's cloud for a test of the game's own staking screens on this computer, where
// Photon cannot call the game services. Two moments:
//
//   node server/rehearsal/photon_standin.mjs enter --address 0x... --token 1795 --player 0 [--game <name>]
//       tells the services that this wallet created a match (or joined the match named with
//       --game) and was let into it with that Genesis; prints the name of the match.
//   node server/rehearsal/photon_standin.mjs finish --game <name> --record <replay.json>
//       sends the record of the match (the one a page opened with ?record=1 sent to the test
//       server) as Photon's server would have, in slices, then closes the room. The judge of
//       the services then plays it again.
//
// Options for both: [--services http://127.0.0.1:8091] [--data .artifacts/services-wager].
// The secret Photon's requests carry is read from the data folder of those services. This is a
// test tool for services that listen on the loopback address: what it sends is what Photon's
// manual describes, nothing checks that Photon sends the same.

import { readFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { gunzipSync } from 'node:zlib';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');

function option(name, fallback) {
  const position = process.argv.indexOf(name);
  return position > 0 ? process.argv[position + 1] : fallback;
}

const ACTION = process.argv[2];
const SERVICES = option('--services', 'http://127.0.0.1:8091');
const DATA = resolve(option('--data', join(root, '.artifacts/services-wager')));
const secret = (await readFile(join(DATA, 'photon-secret'), 'utf8')).trim();

async function photon(name, request) {
  const response = await fetch(`${SERVICES}/api/photon/hook/${name}`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-SecretKey': secret, 'X-Origin': 'Photon' }, body: JSON.stringify(request),
  });
  const text = await response.text();
  if (response.status !== 200) throw new Error(`the services refused ${name}: ${response.status} ${text}`);
  return JSON.parse(text);
}

async function enter() {
  const address = (option('--address') ?? '').toLowerCase();
  const tokenId = Number(option('--token', '0'));
  const player = Number(option('--player', '0'));
  if (!/^0x[0-9a-f]{40}$/.test(address)) throw new Error('--address must be 0x followed by 40 hex digits');
  // What the authentication of the services would have put in the cookie of a signed-in wallet.
  const cookie = { address };
  let gameId = option('--game');
  if (gameId) {
    await photon('game/join', { AppId: 'standin', GameId: gameId, UserId: address, AuthCookie: cookie });
  } else {
    const room = `standin-${Date.now()}`;
    gameId = (await photon('game/create', {
      AppId: 'standin', AppVersion: '1', Region: 'eu', Cloud: '0', UserId: address, AuthCookie: cookie, RoomName: room, GameId: `0:eu:${room}`,
    })).GameId;
  }
  const answer = await photon('player/add', {
    AppId: 'standin', GameId: gameId, UserId: address, ActorNr: player + 1, PlayerSlot: 0, AuthCookie: cookie,
    RuntimePlayer: { $type: 'Quantum.RuntimePlayer, Quantum.Simulation', ForceAI: false, TeamIndex: 0, GenesisLoadout: { Id: { Value: 0 } }, GenesisTokenId: tokenId, GenesisForm: 0 },
  });
  if (answer.RuntimePlayer) console.error(`note: the services put this player right, it enters with Genesis ${answer.RuntimePlayer.GenesisTokenId}`);
  await photon('player/added', { AppId: 'standin', GameId: gameId, UserId: address, ActorNr: player + 1, PlayerSlot: 0, Player: player, AuthCookie: cookie });
  console.log(gameId);
}

async function finish() {
  const gameId = option('--game');
  const recordPath = option('--record');
  if (!gameId || !recordPath) throw new Error('finish needs --game and --record');
  const record = JSON.parse(await readFile(recordPath, 'utf8'));
  const input = gunzipSync(Buffer.from(record.InputHistoryDeltaCompressed.Base64, 'base64'));
  // The input history is one block for each tick, each led by its length on four bytes.
  const tickEnds = [];
  for (let at = 0; at + 4 <= input.length;) {
    at += 4 + input.readInt32LE(at);
    tickEnds.push(at);
  }
  if (tickEnds.at(-1) !== input.length) throw new Error('the input history of the record does not end where its last tick ends');
  const firstTick = record.LastTick - tickEnds.length + 1;

  await photon('replay/start', { AppId: 'standin', GameId: gameId, SessionConfig: record.DeterministicConfig, RuntimeConfig: record.RuntimeConfigData.Base64 });
  const SLICES = 4;
  let sent = 0;
  for (let number = 0; number < SLICES; number++) {
    const last = number === SLICES - 1;
    const untilTick = last ? tickEnds.length : Math.floor(tickEnds.length * (number + 1) / SLICES);
    await photon('replay/chunk', {
      AppId: 'standin', GameId: gameId, ChunkNumber: number, IsLast: last, LastTick: firstTick + untilTick - 1,
      TickCount: untilTick - sent, TickCountTotal: untilTick, IsCompressed: false,
      Input: input.subarray(sent === 0 ? 0 : tickEnds[sent - 1], tickEnds[untilTick - 1]).toString('base64'),
    });
    sent = untilTick;
  }
  await photon('game/close', { AppId: 'standin', GameId: gameId, CloseReason: 0 });
  console.log(`sent ${tickEnds.length} ticks of ${gameId} in ${SLICES} slices and closed the room`);
}

const actions = { enter, finish };
if (!actions[ACTION]) {
  console.error('usage: node server/rehearsal/photon_standin.mjs enter | finish [options, see the top of the file]');
  process.exit(2);
}
actions[ACTION]().catch(error => {
  console.error(error.message);
  process.exit(1);
});
