// A rehearsal of a staked match from end to end, on this computer only:
//
//   two wallets sign in, are let into a match with their Genesis, ask for a ticket and stake
//   $CHOG on their own team, the stakes are locked, the match is played, its record is played
//   again by the judge, the referee signs who won, and the contract pays.
//
// Three things are real: the game services (a process started for the rehearsal, with the
// referee's key and the judge), the wager contract (deployed on a LOCAL COPY of Monad mainnet,
// an anvil process), and the match (the record of one really played in the game, sent by a
// page opened with ?record=1). One thing is played by this script: Photon's cloud. It sends the
// services the requests Photon's manual describes, built from that record, because Photon can
// only call an address the internet reaches. So this proves everything except that Photon
// sends what its manual says.
//
//   anvil --fork-url https://rpc.monad.xyz --network monad --port 8545
//   sh tools/contracts/rehearse_wager.sh                 (prints the wager address)
//   node server/src/local.js --port 8091 --data .artifacts/services-wager --rpc http://127.0.0.1:8545 \
//        --wager <address> --referee-key-file tools/contracts/test-referee-key \
//        --verifier .artifacts/dotnet-build/bin/Chog.Quantum.Verifier/release/Chog.Quantum.Verifier.dll \
//        --quantum-db <build folder>/quantum-db.json --dotnet <dotnet>
//   node server/rehearsal/secure_match.mjs --record <replay.json> --wager <address> \
//        [--services http://127.0.0.1:8091] [--data .artifacts/services-wager] [--stake 25]
//        [--host-token 1795] [--guest-token 408] [--cheat none | late-stake | wrong-team | no-lock]
//
// The two players are anvil's second and third accounts, whose keys anvil publishes: public
// test values. Nothing here can reach the real chain: the script stops unless the endpoint is
// anvil on chain 143. It puts the copy back as it was when it ends.

import { execFile } from 'node:child_process';
import { readFile } from 'node:fs/promises';
import { homedir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { gunzipSync } from 'node:zlib';
import { createWallet, hexToBytes } from '../test/wallet.js';

const run = promisify(execFile);
const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');

function option(name, fallback) {
  const position = process.argv.indexOf(name);
  return position > 0 ? process.argv[position + 1] : fallback;
}

const RPC = option('--rpc', 'http://127.0.0.1:8545');
const SERVICES = option('--services', 'http://127.0.0.1:8091');
const DATA = resolve(option('--data', join(root, '.artifacts/services-wager')));
const WAGER = option('--wager');
const RECORD = option('--record');
const STAKE = BigInt(option('--stake', '25')) * 10n ** 18n;
const CHEAT = option('--cheat', 'none');
const CAST = option('--cast', join(homedir(), '.foundry/bin/cast'));
const CHOG = '0x350035555E10d9AfAF1566AaebfCeD5BA6C27777';
const GENESIS = '0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763';
// The keys of the accounts every anvil starts with, as anvil prints them: public test values.
const THIRD_PARTY = '0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266';
const HOST = { name: 'host', key: '0x59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d', tokenId: Number(option('--host-token', '1795')), player: 0, team: 0 };
const GUEST = { name: 'guest', key: '0x5de4111afa1a4b94908f83103eb1f1706367c2e68ca870fc3fb9a804cdab365a', tokenId: Number(option('--guest-token', '408')), player: 1, team: 1 };

if (!WAGER || !RECORD) {
  console.error('usage: node server/rehearsal/secure_match.mjs --record <replay.json> --wager <address> [options, see the top of the file]');
  process.exit(2);
}

const cast = async (...args) => (await run(CAST, [...args, '--rpc-url', RPC])).stdout.trim();
const sendAs = (account, ...args) => cast('send', ...args, '--unlocked', '--from', account);
const chogOf = async address => BigInt((await cast('call', CHOG, 'balanceOf(address)(uint256)', address)).split(' ')[0]);
const whole = wei => (Number(wei / 10n ** 14n) / 10000).toString();
const step = text => console.log(`\n== ${text}`);

async function services(method, path, { session, body } = {}) {
  const response = await fetch(SERVICES + path, {
    method, headers: session ? { Authorization: 'Bearer ' + session } : {}, body: body ? JSON.stringify(body) : undefined,
  });
  return { status: response.status, body: await response.json() };
}

// Photon's part: one webhook, with the secret its dashboard would send.
let photonSecret;
async function photon(name, request) {
  const response = await fetch(`${SERVICES}/api/photon/hook/${name}`, {
    method: 'POST', headers: { 'Content-Type': 'application/json', 'X-SecretKey': photonSecret, 'X-Origin': 'Photon' }, body: JSON.stringify(request),
  });
  const text = await response.text();
  if (response.status !== 200) throw new Error(`the services refused ${name}: ${response.status} ${text}`);
  return JSON.parse(text);
}

// The input history is one block for each tick, each led by its length on four bytes.
function ticksOf(input) {
  const ends = [];
  let at = 0;
  while (at + 4 <= input.length) {
    at += 4 + input.readInt32LE(at);
    ends.push(at);
  }
  if (at !== input.length) throw new Error('the input history of the record does not end where its last tick ends');
  return ends;
}

async function main() {
  if (await cast('chain-id') !== '143') throw new Error(`the endpoint at ${RPC} is not a copy of Monad mainnet`);
  await cast('rpc', 'anvil_nodeInfo').catch(() => { throw new Error(`the endpoint at ${RPC} is not anvil: this script only acts on a local copy`); });
  photonSecret = (await readFile(join(DATA, 'photon-secret'), 'utf8')).trim();
  const record = JSON.parse(await readFile(RECORD, 'utf8'));
  const input = gunzipSync(Buffer.from(record.InputHistoryDeltaCompressed.Base64, 'base64'));
  const tickEnds = ticksOf(input);
  const firstTick = record.LastTick - tickEnds.length + 1;
  console.log(`record: ${tickEnds.length} ticks (${firstTick} to ${record.LastTick}), ${input.length} bytes of input`);

  const described = (await services('GET', '/api/wager')).body;
  if (!described.enabled || described.address !== WAGER.toLowerCase()) throw new Error(`the services do not stake on ${WAGER}: ${JSON.stringify(described)}`);
  const snapshot = (await cast('rpc', 'evm_snapshot')).replaceAll('"', '');
  const supplyBefore = BigInt((await cast('call', CHOG, 'totalSupply()(uint256)')).split(' ')[0]);

  try {
    step('the two wallets are given, on the copy only, their Genesis and some $CHOG');
    for (const player of [HOST, GUEST]) {
      player.wallet = createWallet(hexToBytes(player.key));
      player.address = player.wallet.address;
      const holder = await cast('call', GENESIS, 'ownerOf(uint256)(address)', String(player.tokenId));
      if (holder.toLowerCase() !== player.address) {
        await cast('rpc', 'anvil_setBalance', holder, '0x56BC75E2D63100000');
        await cast('rpc', 'anvil_impersonateAccount', holder);
        await sendAs(holder, GENESIS, 'transferFrom(address,address,uint256)', holder, player.address, String(player.tokenId));
        await cast('rpc', 'anvil_stopImpersonatingAccount', holder);
      }
      player.before = await chogOf(player.address);
      console.log(`${player.name} ${player.address}: Genesis ${player.tokenId}, ${whole(player.before)} $CHOG`);
      if (player.before < STAKE) throw new Error(`${player.name} has less than the stake: run tools/contracts/rehearse_wager.sh first`);
    }

    step('each signs in with its wallet, and Photon asks the services who connects');
    for (const player of [HOST, GUEST]) {
      const { message, proof } = (await services('POST', '/api/session/challenge', { body: { address: player.address } })).body;
      player.session = (await services('POST', '/api/session', { body: { message, proof, signature: player.wallet.personalSign(message) } })).body.session;
      const asked = await fetch(`${SERVICES}/api/photon/auth?key=${encodeURIComponent(photonSecret)}&session=${encodeURIComponent(player.session)}`).then(response => response.json());
      if (asked.ResultCode !== 1) throw new Error(`the services did not name ${player.name}: ${JSON.stringify(asked)}`);
      player.userId = asked.UserId;
      player.cookie = asked.AuthCookie;
      console.log(`${player.name} is named ${player.userId}`);
    }

    step('Photon: the room is created and joined, the players enter the simulation');
    const room = `rehearsal-${Date.now()}`;
    const gameId = (await photon('game/create', {
      AppId: 'rehearsal', AppVersion: '1', Region: 'eu', Cloud: '0', UserId: HOST.userId, AuthCookie: HOST.cookie,
      RoomName: room, GameId: `0:eu:${room}`, EnterRoomParams: { RoomOptions: { IsVisible: true, IsOpen: true } },
    })).GameId;
    await photon('game/join', { AppId: 'rehearsal', GameId: gameId, UserId: GUEST.userId, AuthCookie: GUEST.cookie });
    console.log(`the match is named ${gameId}`);
    for (const player of [HOST, GUEST]) {
      const actorNr = player.player + 1;
      const answer = await photon('player/add', {
        AppId: 'rehearsal', GameId: gameId, UserId: player.userId, ActorNr: actorNr, PlayerSlot: 0, AuthCookie: player.cookie,
        RuntimePlayer: { $type: 'Quantum.RuntimePlayer, Quantum.Simulation', PlayerNickname: player.name, ForceAI: false, TeamIndex: 0,
          GenesisLoadout: { Id: { Value: 0 } }, GenesisTokenId: player.tokenId, GenesisForm: 0 },
      });
      if (answer.RuntimePlayer) throw new Error(`${player.name} was not let in with its Genesis: ${JSON.stringify(answer.RuntimePlayer)}`);
      await photon('player/added', { AppId: 'rehearsal', GameId: gameId, UserId: player.userId, ActorNr: actorNr, PlayerSlot: 0, Player: player.player, AuthCookie: player.cookie });
    }

    step(`each asks the referee for a ticket and stakes ${whole(STAKE)} $CHOG${CHEAT === 'none' ? ' on its own team' : `; the guest cheats (${CHEAT})`}`);
    const stake = async (player, team) => {
      const ticket = await services('POST', '/api/wager/ticket', { session: player.session, body: { team, amount: STAKE.toString() } });
      if (ticket.status !== 200) throw new Error(`no ticket for ${player.name}: ${JSON.stringify(ticket.body)}`);
      const { matchKey, amount, deadline, signature } = ticket.body;
      await sendAs(player.address, CHOG, 'approve(address,uint256)', WAGER, amount);
      await sendAs(player.address, WAGER, 'stake(bytes32,uint8,uint256,uint256,bytes)', matchKey, String(team), amount, String(deadline), signature);
      console.log(`${player.name} staked on team ${team}`);
      return ticket.body;
    };
    const { matchKey } = await stake(HOST, HOST.team);
    // A guest who cheats either keeps its ticket to stake once it sees how the match goes, or
    // stakes on the team it does not play in.
    let lateTicket = null;
    if (CHEAT === 'late-stake') {
      lateTicket = (await services('POST', '/api/wager/ticket', { session: GUEST.session, body: { team: GUEST.team, amount: STAKE.toString() } })).body;
      console.log('guest keeps its ticket for later');
    } else {
      await stake(GUEST, CHEAT === 'wrong-team' ? HOST.team : GUEST.team);
    }
    console.log(`held by the contract: ${whole(await chogOf(WAGER))} $CHOG`);

    step('Photon: the simulation starts; the stakes are locked before the match begins');
    await photon('game/configs', { AppId: 'rehearsal', GameId: gameId, UserId: HOST.userId, ActorNr: 1, SessionConfig: record.DeterministicConfig, AuthCookie: HOST.cookie });
    await photon('replay/start', {
      AppId: 'rehearsal', AppVersion: '1', Region: 'eu', Cloud: '0', RoomName: room, GameId: gameId,
      SessionConfig: record.DeterministicConfig, RuntimeConfig: record.RuntimeConfigData.Base64,
    });
    if (CHEAT === 'no-lock') {
      console.log('nobody locks the stakes');
    } else {
      const locked = (await services('POST', '/api/wager/lock', { session: HOST.session })).body;
      if (CHEAT === 'late-stake') {
        if (locked.locked !== false) throw new Error('the stakes were locked while a ticket could still be used');
        console.log(`the stakes cannot be locked for ${locked.waitSeconds} s, while guest's ticket can still be used; the match begins without a lock`);
      } else if (locked.locked !== true) {
        throw new Error(`the stakes were not locked: ${JSON.stringify(locked)}`);
      } else {
        console.log(`locked at ${locked.lockedAt}: ${locked.book.map(staker => `${staker.address.slice(0, 8)} on team ${staker.team}`).join(', ')}`);
      }
    }

    step('Photon: the input of every tick, in slices, as its server recorded them');
    const SLICES = 5;
    let sent = 0;
    for (let number = 0; number < SLICES; number++) {
      const last = number === SLICES - 1;
      const untilTick = last ? tickEnds.length : Math.floor(tickEnds.length * (number + 1) / SLICES);
      const from = sent === 0 ? 0 : tickEnds[sent - 1];
      await photon('replay/chunk', {
        AppId: 'rehearsal', GameId: gameId, ChunkNumber: number, IsLast: last, LastTick: firstTick + untilTick - 1,
        TickCount: untilTick - sent, TickCountTotal: untilTick, IsCompressed: false,
        Input: input.subarray(from, tickEnds[untilTick - 1]).toString('base64'),
      });
      sent = untilTick;
    }
    // What the players' own pages would claim: a lie, to see that it decides nothing.
    await photon('game/result', { AppId: 'rehearsal', GameId: gameId, Results: [{ Result: { $type: 'Quantum.GameResult, Quantum.Simulation', Winner: 0 }, Clients: [{ UserId: HOST.userId }], IsServerResult: false }] });
    await photon('game/close', { AppId: 'rehearsal', GameId: gameId, CloseReason: 0 });

    step('the judge plays the match again; the referee signs what came out');
    let decision;
    for (let attempt = 0; attempt < 60; attempt++) {
      decision = (await services('GET', `/api/wager/result?gameId=${encodeURIComponent(gameId)}`)).body;
      if (!decision.pending) break;
      await new Promise(resolvePause => setTimeout(resolvePause, 1000));
    }
    if (decision.pending) throw new Error('the match got no verdict in a minute: is the judge set up (--verifier, --quantum-db)?');
    const verdict = JSON.parse(await readFile(join(DATA, 'matches', await folderOf(gameId), 'verdict.json'), 'utf8'));
    console.log(`verdict: ${verdict.void ? 'void, ' + verdict.why : `team ${verdict.winnerTeam} won ${verdict.scores.join(' to ')}`}; players ${JSON.stringify(verdict.players)}`);
    console.log(decision.void ? `the referee calls the match off: ${decision.why}` : `the referee signs: team ${decision.winnerTeam} won`);

    step('a third party brings the signature to the contract');
    if (decision.void) await sendAs(THIRD_PARTY, WAGER, 'voidMatch(bytes32,bytes)', matchKey, decision.signature);
    else await sendAs(THIRD_PARTY, WAGER, 'settle(bytes32,uint8,bytes)', matchKey, String(decision.winnerTeam), decision.signature);
    if (lateTicket) {
      const late = await sendAs(GUEST.address, WAGER, 'stake(bytes32,uint8,uint256,uint256,bytes)', lateTicket.matchKey, String(GUEST.team), lateTicket.amount, String(lateTicket.deadline), lateTicket.signature)
        .then(() => 'accepted', () => 'refused by the contract');
      console.log(`guest tries its kept ticket after the end: ${late}`);
    }
    const burned = supplyBefore - BigInt((await cast('call', CHOG, 'totalSupply()(uint256)')).split(' ')[0]);
    for (const player of [HOST, GUEST]) {
      const after = await chogOf(player.address);
      const change = after - player.before;
      console.log(`${player.name} (team ${player.team}): ${whole(player.before)} -> ${whole(after)} $CHOG (${change >= 0n ? '+' : '-'}${whole(change >= 0n ? change : -change)})`);
    }
    const state = (await cast('call', WAGER, 'matchOf(bytes32)(uint8,uint256,uint64,uint8,uint8)', matchKey)).split('\n')[0];
    console.log(`burned ${whole(burned)} $CHOG; left in the contract ${whole(await chogOf(WAGER))}; state of the match ${state} (2 settled, 3 refunded)`);
  } finally {
    await cast('rpc', 'evm_revert', snapshot);
    console.log('\nthe copy is back as it was');
  }
}

async function folderOf(gameId) {
  return (await import('../src/matches.js')).folderOf(gameId);
}

main().catch(error => {
  console.error(error.message);
  process.exit(1);
});
