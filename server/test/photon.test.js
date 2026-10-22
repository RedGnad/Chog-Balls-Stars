import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createApi } from '../src/api.js';
import { createCareers } from '../src/career.js';
import { createJudge } from '../src/judge.js';
import { createMatches, createMemoryStore, parseExact } from '../src/matches.js';
import { ADMIN_ADDRESSES, createPhoton } from '../src/photon.js';
import { createSessions } from '../src/session.js';
import { createWallet } from './wallet.js';

const SECRET = 'a test secret that is long enough to be accepted';
const PHOTON_SECRET = 'what the dashboard of Photon sends with its requests';
const ORIGIN = 'https://game.example';
const HOLDER = createWallet();
const STRANGER = createWallet();
// Ids as the engine writes them: too large for a JavaScript number.
const AVATAR = '2430278665492933905';
const LOADOUT = '440543562436170603';

// Services where HOLDER holds the Genesis 1795 and rents 8, and the Genesis 1795 has its second form.
function services({ replay, onReplayEnded, build, previewBuilds } = {}) {
  const state = { holders: new Map([[1795, HOLDER.address], [408, '0x' + 'c3'.repeat(20)]]), rented: new Set([`8:${HOLDER.address}`]), bought: new Set([1795]) };
  const chain = {
    holds: async (address, tokenId) => ({ holds: state.holders.get(tokenId) === address.toLowerCase() }),
    ownedBy: async () => ({ tokens: [], block: 1 }),
  };
  const rental = { isRenting: async (tokenId, renter) => state.rented.has(`${tokenId}:${renter}`) };
  const shop = { ownedAmong: async tokens => tokens.filter(token => state.bought.has(token)), describe: async () => ({ enabled: false }) };
  let saved = null;
  const careers = createCareers({ load: async () => saved, save: async value => { saved = value; } });
  const sessions = createSessions({ secret: SECRET });
  const matches = createMatches({ store: createMemoryStore() });
  const judge = replay ? createJudge({ matches, replay, build }) : null;
  const photon = createPhoton({
    secret: PHOTON_SECRET, sessions, careers, chain, shop, rental, matches, build, previewBuilds,
    onReplayEnded: onReplayEnded ?? (judge ? gameId => judge.judge(gameId) : undefined),
  });
  const api = createApi({ chain, sessions, careers, shop, rental, photon, photonAuthentication: true, publicOrigin: ORIGIN, build });
  return { state, api, sessions, careers, matches, judge, photon };
}

async function sessionOf(api, wallet) {
  const post = (path, body) => api(new Request(ORIGIN + path, { method: 'POST', body: JSON.stringify(body) })).then(response => response.json());
  const { message, proof } = await post('/api/session/challenge', { address: wallet.address });
  return (await post('/api/session', { message, proof, signature: wallet.personalSign(message) })).session;
}

// One webhook as Photon sends it; the body is given as text so that large ids stay what they are.
async function hook(api, name, text, secret = PHOTON_SECRET) {
  const response = await api(new Request(`${ORIGIN}/api/photon/hook/${name}`, { method: 'POST', headers: { 'X-SecretKey': secret }, body: text }));
  return { status: response.status, text: await response.text() };
}

const runtimePlayer = fields => `{"$type":"Quantum.RuntimePlayer, Quantum.Simulation","PlayerAvatar":{"Id":{"Value":${AVATAR}}},"PlayerNickname":"red",`
  + `"ForceAI":false,"TeamIndex":0,"GenesisLoadout":{"Id":{"Value":0}},"GenesisTokenId":0,"GenesisForm":0${fields ? ',' + fields : ''}}`;

const addPlayer = (api, gameId, cookie, player, userId = 'someone') => hook(api, 'player/add',
  `{"AppId":"app","GameId":${JSON.stringify(gameId)},"UserId":"${userId}","ActorNr":1,"PlayerSlot":0,"RuntimePlayer":${player},"AuthCookie":${JSON.stringify(cookie)}}`);

test('Photon is told who connects: a wallet by its address, a guest and a visitor by a name of their own', async () => {
  const { api, sessions } = services();
  const ask = query => api(new Request(`${ORIGIN}/api/photon/auth?${query}`)).then(async response => ({ status: response.status, body: await response.json() }));

  assert.equal((await ask('session=anything')).status, 403, 'without the key of the dashboard nobody is answered');
  assert.equal((await ask('key=wrong')).status, 403);

  const session = await sessionOf(api, HOLDER);
  const wallet = await ask(`key=${encodeURIComponent(PHOTON_SECRET)}&session=${encodeURIComponent(session)}`);
  assert.deepEqual(wallet.body, { ResultCode: 1, UserId: HOLDER.address, AuthCookie: { address: HOLDER.address } });

  const forged = await ask(`key=${encodeURIComponent(PHOTON_SECRET)}&session=${encodeURIComponent(session.slice(0, -2))}xx`);
  assert.equal(forged.body.ResultCode, 2, 'a session we did not issue is refused');

  const { pass } = await sessions.lend({ tokenId: 1795, lender: HOLDER.address });
  const guest = await ask(`key=${encodeURIComponent(PHOTON_SECRET)}&pass=${encodeURIComponent(pass)}`);
  assert.equal(guest.body.ResultCode, 1);
  assert.match(guest.body.UserId, /^guest-[0-9a-f]{18}$/);
  assert.equal(guest.body.AuthCookie.pass.tokenId, 1795);
  assert.equal(guest.body.AuthCookie.pass.lender, HOLDER.address);

  const visitor = await ask(`key=${encodeURIComponent(PHOTON_SECRET)}`);
  assert.equal(visitor.body.ResultCode, 1);
  assert.match(visitor.body.UserId, /^visitor-/);
  assert.deepEqual(visitor.body.AuthCookie, { visitor: true });

  assert.deepEqual(await api(new Request(`${ORIGIN}/api/photon`)).then(response => response.json()), { authentication: true, build: '' });
});

test('a webhook without the secret of the dashboard is refused and changes nothing', async () => {
  const { api, matches } = services();
  const refused = await hook(api, 'game/join', '{"GameId":"g","UserId":"u"}', 'not the secret');
  assert.equal(refused.status, 403);
  assert.equal(await matches.get('g'), null);
  assert.equal((await hook(api, 'no/such', '{"GameId":"g"}')).status, 404);
  assert.equal((await hook(api, 'game/join', 'not json')).status, 400);
  assert.equal((await hook(api, 'game/join', '{"UserId":"u"}')).status, 400, 'a webhook names its match');
});

test('a room is given a name no other match has, whatever its room name', async () => {
  const { api, matches } = services();
  const create = () => hook(api, 'game/create', '{"AppId":"app","AppVersion":"1","Region":"eu","Cloud":"0","UserId":"u","RoomName":"party","GameId":"0:eu:party"}');
  const first = JSON.parse((await create()).text).GameId;
  const second = JSON.parse((await create()).text).GameId;
  assert.match(first, /^0:eu:party~[0-9a-f]{18}$/);
  assert.notEqual(first, second);
  assert.equal((await matches.get(first)).app.roomName, 'party');
});

test('a holder enters with its Genesis as sent; a wallet that does not hold it enters as a recruit', async () => {
  const { api, matches } = services();
  const asHolder = await addPlayer(api, 'g1', { address: HOLDER.address }, runtimePlayer().replace('"GenesisTokenId":0', '"GenesisTokenId":1795'));
  assert.equal(asHolder.status, 200);
  assert.equal(asHolder.text, '{}', 'nothing to put right: the data of the page is accepted as sent');

  const asked = runtimePlayer().replace('"GenesisTokenId":0', '"GenesisTokenId":1795');
  const asStranger = await addPlayer(api, 'g1', { address: STRANGER.address }, asked, 'other');
  assert.equal(asStranger.status, 200);
  const put = parseExact(asStranger.text).RuntimePlayer;
  assert.equal(put.GenesisTokenId, 0, 'no Genesis without a right to it');
  assert.equal(put.PlayerNickname, 'red');
  assert.equal(put.$type, 'Quantum.RuntimePlayer, Quantum.Simulation');
  assert.ok(asStranger.text.includes(`"PlayerAvatar":{"Id":{"Value":${AVATAR}}}`), 'a 64-bit id goes back digit for digit');

  const visitor = await addPlayer(api, 'g1', { visitor: true }, asked, 'third');
  assert.equal(parseExact(visitor.text).RuntimePlayer.GenesisTokenId, 0);

  const kept = (await matches.get('g1')).admissions;
  assert.deepEqual(kept.map(admission => [admission.right, admission.tokenId, admission.askedToken, admission.address]),
    [['holder', 1795, 1795, HOLDER.address], [null, 0, 1795, STRANGER.address], [null, 0, 1795, null]]);
});

test('the owner\'s wallet enters with any Genesis, admitted like a stand-in, and no other wallet does', async () => {
  const { api, matches } = services();
  const asked = runtimePlayer().replace('"GenesisTokenId":0', '"GenesisTokenId":408');
  const asOwner = await addPlayer(api, 'g-admin', { address: ADMIN_ADDRESSES[0].toUpperCase().replace('0X', '0x') }, asked);
  assert.equal(asOwner.text, '{}', 'the Genesis it asked for is kept');
  const asStranger = await addPlayer(api, 'g-admin', { address: STRANGER.address }, asked, 'other');
  assert.equal(parseExact(asStranger.text).RuntimePlayer.GenesisTokenId, 0);
  const visitor = await addPlayer(api, 'g-admin', { visitor: true, address: 'not an address' }, asked, 'third');
  assert.equal(parseExact(visitor.text).RuntimePlayer.GenesisTokenId, 0);

  const [owner, stranger] = (await matches.get('g-admin')).admissions;
  assert.deepEqual([owner.right, owner.tokenId, owner.standIn], ['admin', 408, true], 'no career, rank or wager share follows');
  assert.deepEqual([stranger.right, stranger.tokenId, stranger.standIn], [null, 0, undefined]);
});

test('a renter and a guest enter with the Genesis they have a right to, and lose it with that right', async () => {
  const { api, state, careers } = services();
  const rented = runtimePlayer().replace('"GenesisTokenId":0', '"GenesisTokenId":8');
  assert.equal((await addPlayer(api, 'g2', { address: HOLDER.address }, rented)).text, '{}', 'a paid-up renter plays the Genesis');
  state.rented.clear();
  assert.equal(parseExact((await addPlayer(api, 'g2', { address: HOLDER.address }, rented)).text).RuntimePlayer.GenesisTokenId, 0);

  const lent = runtimePlayer().replace('"GenesisTokenId":0', '"GenesisTokenId":1795');
  const cookie = { pass: { tokenId: 1795, lender: HOLDER.address, issuedAt: Date.now() - 1000 } };
  assert.equal((await addPlayer(api, 'g2', cookie, lent, 'guest-1')).text, '{}', 'a guest plays the Genesis it was lent');
  const other = runtimePlayer().replace('"GenesisTokenId":0', '"GenesisTokenId":408');
  assert.equal(parseExact((await addPlayer(api, 'g2', cookie, other, 'guest-1')).text).RuntimePlayer.GenesisTokenId, 0, 'and no other');

  // However Photon hands the cookie back: flat, its numbers as text.
  const flat = { passTokenId: '1795', passLender: HOLDER.address, passIssuedAt: String(Date.now() - 1000) };
  assert.equal((await addPlayer(api, 'g2', flat, lent, 'guest-2')).text, '{}', 'a flat cookie lends the same');

  await careers.stopLending(1795);
  assert.equal(parseExact((await addPlayer(api, 'g2', cookie, lent, 'guest-1')).text).RuntimePlayer.GenesisTokenId, 0, 'not after the holder took it back');
});

test('what a page may not decide is put right: the second form, a loadout named directly, a character left to the machine', async () => {
  const { api, state } = services();
  const withForm = token => runtimePlayer().replace('"GenesisTokenId":0', `"GenesisTokenId":${token}`).replace('"GenesisForm":0', '"GenesisForm":1');
  assert.equal((await addPlayer(api, 'g3', { address: HOLDER.address }, withForm(1795))).text, '{}', 'the second form of a Genesis that has it');
  assert.equal(parseExact((await addPlayer(api, 'g3', { address: HOLDER.address }, withForm(8))).text).RuntimePlayer.GenesisForm, 0, 'not of one that has not');
  state.bought.clear();
  assert.equal(parseExact((await addPlayer(api, 'g3', { address: HOLDER.address }, withForm(1795))).text).RuntimePlayer.GenesisForm, 0);

  const named = await addPlayer(api, 'g3', { visitor: true }, runtimePlayer().replace('"GenesisLoadout":{"Id":{"Value":0}}', `"GenesisLoadout":{"Id":{"Value":${LOADOUT}}}`));
  assert.ok(named.text.includes('"GenesisLoadout":{"Id":{"Value":0}}'), 'a loadout is never taken from the page');

  const machine = await addPlayer(api, 'g3', { visitor: true }, runtimePlayer().replace('"ForceAI":false', '"ForceAI":true'));
  assert.equal(parseExact(machine.text).RuntimePlayer.ForceAI, false);

  assert.equal((await addPlayer(api, 'g3', { visitor: true }, 'null')).text, '{}', 'a player without data enters without a Genesis');
});

test('when the chain cannot be read Photon is told to ask again', async () => {
  const failing = createPhoton({
    secret: PHOTON_SECRET, sessions: {}, careers: {}, matches: createMatches({ store: createMemoryStore() }),
    chain: { holds: async () => { throw new Error('rpc down'); } },
  });
  const quiet = console.error;
  console.error = () => {};
  try {
    const answer = await failing.hook('player/add', parseExact(`{"GameId":"g","UserId":"u","RuntimePlayer":${runtimePlayer().replace('"GenesisTokenId":0', '"GenesisTokenId":1795')},"AuthCookie":{"address":"${HOLDER.address}"}}`), PHOTON_SECRET);
    assert.equal(answer.status, 503);
  } finally {
    console.error = quiet;
  }
});

const startText = '{"AppId":"app","GameId":"g4","SessionConfig":{"PlayerCount":6,"UpdateFPS":30},"RuntimeConfig":"H4sIAAAA"}';
const chunkText = (number, isLast) => `{"AppId":"app","GameId":"g4","ChunkNumber":${number},"IsLast":${isLast},"LastTick":${300 * (number + 1)},"TickCount":240,"TickCountTotal":${240 * (number + 1)},"IsCompressed":false,"Input":"AAAA"}`;

test('the record of a match is kept as Photon sent it, and the match is judged once, when its last slice arrives', async () => {
  const judged = [];
  const { api, matches } = services({ onReplayEnded: async gameId => { judged.push(gameId); } });
  assert.deepEqual(JSON.parse((await hook(api, 'replay/start', startText)).text), { Skip: false });
  assert.equal((await hook(api, 'replay/chunk', chunkText(0, false))).status, 200);
  assert.equal((await hook(api, 'replay/chunk', chunkText(0, false))).status, 200, 'a slice sent twice is not an error');
  let record = await matches.replayOf('g4');
  assert.equal(record.chunks.length, 1);
  assert.equal(record.complete, false);
  assert.deepEqual(judged, []);

  await hook(api, 'replay/chunk', chunkText(2, true));
  record = await matches.replayOf('g4');
  assert.deepEqual(record.missing, [1], 'a hole in the record is seen');
  assert.equal(record.complete, false);

  await hook(api, 'replay/chunk', chunkText(1, false));
  await hook(api, 'replay/chunk', chunkText(2, true));
  await new Promise(resolve => setImmediate(resolve));
  record = await matches.replayOf('g4');
  assert.equal(record.complete, true);
  assert.equal(record.start.RuntimeConfig, 'H4sIAAAA');
  assert.deepEqual(judged, ['g4'], 'judged when the last slice was first kept, and only then');

  await hook(api, 'game/result', '{"AppId":"app","GameId":"g4","Results":[{"Result":{"Winner":0},"Clients":[{"UserId":"u"}],"IsServerResult":false}]}');
  await hook(api, 'game/close', '{"AppId":"app","GameId":"g4","CloseReason":0}');
  const kept = await matches.get('g4');
  assert.equal(kept.claims.length, 1, 'what the pages claim is kept');
  assert.equal(kept.verdict, null, 'and decides nothing');
  assert.ok(kept.closedAt);

  // The record can be read back from outside, to replay it elsewhere; the AppId is not repeated.
  const reader = createApi({ matches, photon: null, publicOrigin: ORIGIN });
  const read = await reader(new Request(`${ORIGIN}/api/match/record?gameId=g4`)).then(response => response.json());
  assert.equal(read.ended, true);
  assert.deepEqual(read.chunks.map(slice => slice.ChunkNumber), [0, 1, 2]);
  assert.equal(read.start.RuntimeConfig, 'H4sIAAAA');
  assert.equal(read.start.AppId, undefined);
  assert.equal(read.chunks[0].AppId, undefined);
  assert.equal((await reader(new Request(`${ORIGIN}/api/match/record?gameId=nothing`))).status, 404);
});

// A match of two admitted players whose record is whole.
async function playedMatch(api, gameId = 'g5') {
  await hook(api, 'replay/start', startText.replace('g4', gameId));
  await addPlayer(api, gameId, { address: HOLDER.address }, runtimePlayer().replace('"GenesisTokenId":0', '"GenesisTokenId":1795'), HOLDER.address);
  await hook(api, 'player/added', `{"GameId":"${gameId}","UserId":"${HOLDER.address}","ActorNr":1,"PlayerSlot":0,"Player":0}`);
  await addPlayer(api, gameId, { address: STRANGER.address }, runtimePlayer(), STRANGER.address);
  await hook(api, 'player/added', `{"GameId":"${gameId}","UserId":"${STRANGER.address}","ActorNr":1,"PlayerSlot":0,"Player":1}`);
  await hook(api, 'replay/chunk', chunkText(0, true).replace('g4', gameId));
  await new Promise(resolve => setImmediate(resolve));
}

const report = changes => ({
  winnerTeam: 1, scores: [3, 11], endedAtTick: 3235, selectionEndedAtTick: 387, firstTick: 60, lastTick: 3239, updateRate: 30,
  players: [{ player: 0, team: 0, tokenId: 1795 }, { player: 1, team: 1, tokenId: 0 }], simulation: 'abc', ...changes,
});

test('a build being previewed opens rooms of its own, and its matches are void', async () => {
  const served = '20261012T035911141Z';
  const { api, matches } = services({ build: async () => served, replay: async () => report(), previewBuilds: 'any' });
  const created = await hook(api, 'game/create',
    `{"AppId":"app","AppVersion":"20261017T120000000Z","Region":"eu","Cloud":"0","UserId":"u","RoomName":"party","GameId":"0:eu:party"}`);
  assert.equal(created.status, 200, 'a previewed build opens a room');
  const gameId = JSON.parse(created.text).GameId;
  await playedMatch(api, gameId);
  assert.equal((await matches.get(gameId)).verdict.void, true, 'judged with another simulation, its match counts for nothing');
});

test('a page that runs another build opens no room here, and a match played with another build is void', async () => {
  let served = '20261012T035911141Z';
  let replays = 0;
  const { api, matches } = services({ build: async () => served, replay: async () => { replays++; return report(); } });
  const create = version => hook(api, 'game/create',
    `{"AppId":"app","AppVersion":"${version}","Region":"eu","Cloud":"0","UserId":"u","RoomName":"party","GameId":"0:eu:party"}`);

  const stale = await create('20261011T120000000Z');
  assert.equal(stale.status, 400, 'a page that kept an older build opens no room');
  assert.equal(JSON.parse(stale.text).Error, 'AnotherBuild');
  assert.deepEqual(await api(new Request(`${ORIGIN}/api/photon`)).then(response => response.json()), { authentication: true, build: served },
    'the page is told which build is served, to load itself again when it runs another');

  const first = JSON.parse((await create(served)).text).GameId;
  await playedMatch(api, first);
  assert.equal((await matches.get(first)).verdict.winnerTeam, 1, 'a match of the served build is judged');
  assert.equal(replays, 1);

  // A page names the mode its player chose after its build: same build, so it opens and is judged.
  const siege = JSON.parse((await create(`${served}-siege`)).text).GameId;
  await playedMatch(api, siege);
  assert.equal((await matches.get(siege)).verdict.winnerTeam, 1, 'a siege of the served build is judged');
  assert.equal(replays, 2);
  const staleSiege = await create('20261011T120000000Z-siege');
  assert.equal(JSON.parse(staleSiege.text).Error, 'AnotherBuild', 'a mode does not let another build in');
  const unknownMode = await create(`${served}-golf`);
  assert.equal(JSON.parse(unknownMode.text).Error, 'AnotherBuild', 'only the modes of the game are read after a build');

  // The site is assembled again while a match of the build before is being played.
  const second = JSON.parse((await create(served)).text).GameId;
  served = '20261013T101500000Z';
  await playedMatch(api, second);
  const { at, ...verdict } = (await matches.get(second)).verdict;
  assert.deepEqual(verdict, {
    void: true, why: 'the match was played with another build than the one judged here', playedWith: '20261012T035911141Z', build: served,
  });
  assert.equal(replays, 2, 'the program of another build was not asked: it would have played another match');
});

test('the verdict of a match is what its replay says, never what a page claims', async () => {
  const asked = [];
  const { api, matches } = services({ replay: async (folder, gameId) => { asked.push([folder, gameId]); return report(); } });
  await playedMatch(api);
  const verdict = await matches.verdictOf('g5');
  assert.equal(verdict.winnerTeam, 1);
  assert.deepEqual(verdict.scores, [3, 11]);
  assert.equal(verdict.selectionEndedAtTick, 387);
  assert.deepEqual(asked, [[matches.folderOf('g5'), 'g5']]);
  const admissions = (await matches.get('g5')).admissions;
  assert.deepEqual(admissions.map(admission => admission.player), [0, 1], 'each admission knows the player the simulation gave it');
});

test('an even score is a draw, and a match that cannot be judged is void', async () => {
  const judgedWith = async changes => {
    const { api, matches } = services({ replay: async () => report(changes) });
    await playedMatch(api);
    return matches.verdictOf('g5');
  };
  assert.equal((await judgedWith({ scores: [7, 7] })).winnerTeam, 2, 'the game gives an even score to the second team; with stakes it is a draw');
  // A destroyed core decides the match whatever the scores: an even score is then no draw.
  const byCore = await judgedWith({ scores: [7, 7], winnerTeam: 0, coreLostBy: 1 });
  assert.equal(byCore.winnerTeam, 0);
  assert.equal(byCore.coreLostBy, 1);
  assert.equal((await judgedWith({ scores: [2, 9], winnerTeam: 0, coreLostBy: 1 })).winnerTeam, 0, 'the core counts before the score');
  const unfinished = await judgedWith({ winnerTeam: null });
  assert.equal(unfinished.void, true);
  assert.match(unfinished.why, /did not reach its end/);
  const other = await judgedWith({ players: [{ player: 0, team: 0, tokenId: 408 }, { player: 1, team: 1, tokenId: 0 }] });
  assert.equal(other.void, true);
  assert.match(other.why, /Genesis it was admitted with/);
});

test('a record with a hole is void; a replay that fails leaves the match to be judged again', async () => {
  let fail = true;
  const { api, matches, judge } = services({ replay: async () => { if (fail) throw new Error('no dotnet'); return report(); } });
  const quiet = console.error;
  console.error = () => {};
  try {
    await playedMatch(api);
  } finally {
    console.error = quiet;
  }
  assert.equal(await matches.verdictOf('g5'), null, 'a failure of our own tool is not a verdict');
  fail = false;
  assert.equal((await judge.judge('g5')).winnerTeam, 1);
  await assert.rejects(matches.setVerdict('g5', { winnerTeam: 0 }), /already has another verdict/);

  await hook(api, 'replay/start', startText.replace('g4', 'g6'));
  await hook(api, 'replay/chunk', chunkText(1, true).replace('g4', 'g6'));
  await new Promise(resolve => setImmediate(resolve));
  assert.equal((await matches.verdictOf('g6')).void, true);
});
