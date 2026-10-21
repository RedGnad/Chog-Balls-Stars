import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createRemoteRankStore } from '../src/rank-store.js';
import { createMatches, createMemoryStore } from '../src/matches.js';
import { createRanks } from '../src/ranks.js';

function fakeRedis() {
  const values = new Map();
  const commands = [];
  const fetchImpl = async (_url, request) => {
    const [name, ...args] = JSON.parse(request.body);
    commands.push(name);
    const authorization = request.headers.Authorization;
    if (authorization !== 'Bearer test-token' && authorization !== 'Bearer read-only-token') {
      return Response.json({ error: 'unauthorized' }, { status: 401 });
    }
    if (authorization === 'Bearer read-only-token' && name === 'SET') {
      return Response.json({ error: 'command not allowed' }, { status: 400 });
    }
    let result;
    if (name === 'PING') result = 'PONG';
    else if (name === 'GET') result = values.get(args[0]) ?? null;
    else if (name === 'SET') {
      result = args[2] === 'NX' && values.has(args[0]) ? null : 'OK';
      if (result === 'OK') values.set(args[0], args[1]);
    } else if (name === 'SCAN') {
      const matched = [...values.keys()].filter(key => key.startsWith(args[2].slice(0, -1))).sort();
      const start = Number(args[0]);
      const end = Math.min(start + 2, matched.length);
      result = [end === matched.length ? '0' : String(end), matched.slice(start, end)];
    } else throw new Error(`unexpected command ${name}`);
    return Response.json({ result });
  };
  return { fetchImpl, commands };
}

test('remote rank events survive a new adapter and cannot be overwritten', async () => {
  const redis = fakeRedis();
  const options = { url: 'https://rank-store.example', token: 'test-token', fetchImpl: redis.fetchImpl };
  const first = createRemoteRankStore(options);
  assert.equal(await first.ping(), true);
  assert.equal(await first.probeWrite(), true);
  assert.equal(await first.setIfAbsent('ranked/game-1', '{"win":3}'), true);
  assert.equal(await first.setIfAbsent('ranked/game-1', '{"win":20}'), false);
  const second = createRemoteRankStore(options);
  assert.equal(await second.get('ranked/game-1'), '{"win":3}');
  assert.equal(await second.get('ranked/missing'), null);
});

test('remote rank listing walks every page and stays inside its namespace', async () => {
  const redis = fakeRedis();
  const store = createRemoteRankStore({ url: 'https://rank-store.example/', token: 'test-token', fetchImpl: redis.fetchImpl });
  for (const key of ['ranked/c', 'ranked/a', 'ranked/b', 'other/value']) {
    assert.equal(await store.setIfAbsent(key, key), true);
  }
  assert.deepEqual(await store.list('ranked/'), ['ranked/a', 'ranked/b', 'ranked/c']);
  assert.equal(redis.commands.filter(command => command === 'SCAN').length, 2);
});

test('a rank credited to remote storage survives a service restart and racing callbacks', async () => {
  const redis = fakeRedis();
  const options = { url: 'https://rank-store.example', token: 'test-token', fetchImpl: redis.fetchImpl };
  const matches = createMatches({ store: createMemoryStore() });
  const gameId = 'remote-rank-game';
  const address = '0x0000000000000000000000000000000000000001';
  await matches.entered(gameId, { how: 'create', userId: address, app: { appVersion: 'sim-example-siege' } });
  await matches.admitted(gameId, { player: 1, tokenId: 0, address, right: null });
  const players = [{ player: 1, team: 0, tokenId: 0 }];
  const ranks = createRanks({ store: createRemoteRankStore(options), matches });
  const win = { mode: 'Siege', testOptions: false, winnerTeam: 0, players };
  const [one, two] = await Promise.all([ranks.record(gameId, win), ranks.record(gameId, { ...win, winnerTeam: 1 })]);
  assert.deepEqual(one, two);
  const restarted = createRanks({ store: createRemoteRankStore(options), matches });
  const standing = await restarted.of(address);
  assert.equal(standing.matches, 1);
  assert.equal(standing.points, one.winnerTeam === 0 ? 3 : 0);
});

test('remote rank storage refuses missing credentials and failed commands', async () => {
  assert.throws(() => createRemoteRankStore({ url: 'http://rank-store.example', token: 'token' }));
  assert.throws(() => createRemoteRankStore({ url: 'https://rank-store.example', token: '' }));
  const store = createRemoteRankStore({
    url: 'https://rank-store.example', token: 'wrong-token', fetchImpl: fakeRedis().fetchImpl,
  });
  await assert.rejects(store.get('ranked/game-1'), /HTTP 401/);
  const readOnly = createRemoteRankStore({
    url: 'https://rank-store.example', token: 'read-only-token', fetchImpl: fakeRedis().fetchImpl,
  });
  assert.equal(await readOnly.ping(), true);
  await assert.rejects(readOnly.probeWrite(), /HTTP 400/);
});
