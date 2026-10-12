import assert from 'node:assert/strict';
import { mkdir, mkdtemp, rm, utimes, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { after, before, test } from 'node:test';
import { createApi } from '../src/api.js';
import { careerFile, createHttpServer, reownProjectId, sessionSecret } from '../src/node.js';
import { createStatic, describeFile } from '../src/static.js';

let folder;
let serve;

before(async () => {
  folder = await mkdtemp(join(tmpdir(), 'chog-host-test-'));
  await mkdir(join(folder, 'game/Build'), { recursive: true });
  await mkdir(join(folder, 'hero'), { recursive: true });
  await mkdir(join(folder, 'art/256'), { recursive: true });
  await writeFile(join(folder, 'game/index.html'), '<title>game</title>');
  await writeFile(join(folder, 'game/Build/webgl.wasm.gz'), 'compressed code');
  await writeFile(join(folder, 'hero/index.html'), '<title>hero</title>');
  await writeFile(join(folder, 'art/256/1287.jpg'), 'portrait');
  await writeFile(join(folder, 'secret.txt'), 'not for the web');
  serve = createStatic([
    { prefix: '/', root: join(folder, 'game') },
    { prefix: '/hero/', root: join(folder, 'hero') },
    { prefix: '/genesis-art/', root: join(folder, 'art'), cache: 'public, max-age=86400' },
  ]);
});

after(() => rm(folder, { recursive: true, force: true }));

const get = (path, init) => serve(new Request('http://game.example' + path, init));

test('the files of a Unity Web build are named for what they hold, not for their compression', () => {
  assert.deepEqual(describeFile('webgl.wasm.gz'), { type: 'application/wasm', encoding: 'gzip' });
  assert.deepEqual(describeFile('webgl.framework.js.gz'), { type: 'application/javascript; charset=utf-8', encoding: 'gzip' });
  assert.deepEqual(describeFile('webgl.data.gz'), { type: 'application/octet-stream', encoding: 'gzip' });
  assert.deepEqual(describeFile('webgl.data.br'), { type: 'application/octet-stream', encoding: 'br' });
  assert.deepEqual(describeFile('index.html'), { type: 'text/html; charset=utf-8', encoding: undefined });
  assert.deepEqual(describeFile('1287.JPG'), { type: 'image/jpeg', encoding: undefined });
});

test('the game, the hero page and the portraits each answer under their own address', async () => {
  const game = await get('/?token=1795');
  assert.equal(game.status, 200);
  assert.equal(await game.text(), '<title>game</title>');
  assert.equal(game.headers.get('cache-control'), 'no-cache');

  const code = await get('/Build/webgl.wasm.gz');
  assert.equal(code.headers.get('content-type'), 'application/wasm');
  assert.equal(code.headers.get('content-encoding'), 'gzip');
  assert.equal(code.headers.get('content-length'), '15');
  assert.equal(await code.text(), 'compressed code');

  assert.equal(await (await get('/hero/')).text(), '<title>hero</title>');
  const moved = await get('/hero?n=1287');
  assert.equal(moved.status, 308);
  assert.equal(moved.headers.get('location'), '/hero/?n=1287');

  const portrait = await get('/genesis-art/256/1287.jpg');
  assert.equal(portrait.headers.get('content-type'), 'image/jpeg');
  assert.equal(portrait.headers.get('cache-control'), 'public, max-age=86400');
});

test('nothing outside the served folders can be read, and nothing can be written', async () => {
  assert.equal((await get('/secret.txt')).status, 404);
  assert.equal((await get('/hero/%2e%2e%2fsecret.txt')).status, 404);
  assert.equal((await get('/hero/..%2f..%2fsecret.txt')).status, 404);
  assert.equal((await get('/genesis-art/%2e%2e/%2e%2e/secret.txt')).status, 404);
  assert.equal((await get('/Build/missing.js')).status, 404);
  assert.equal((await get('/%E0%A4%A')).status, 400);
  assert.equal((await get('/', { method: 'POST', body: 'x' })).status, 405);
});

test('a file that has not changed is not sent again, and HEAD sends no body', async () => {
  const old = new Date('2026-10-01T10:00:00Z');
  await utimes(join(folder, 'art/256/1287.jpg'), old, old);
  const first = await get('/genesis-art/256/1287.jpg');
  assert.equal(first.headers.get('last-modified'), old.toUTCString());
  assert.equal((await get('/genesis-art/256/1287.jpg', { headers: { 'If-Modified-Since': old.toUTCString() } })).status, 304);
  assert.equal((await get('/genesis-art/256/1287.jpg', { headers: { 'If-Modified-Since': 'Thu, 01 Oct 2026 09:00:00 GMT' } })).status, 200);

  const head = await get('/genesis-art/256/1287.jpg', { method: 'HEAD' });
  assert.equal(head.status, 200);
  assert.equal(head.body, null);
  assert.equal(head.headers.get('content-length'), '8');
});

test('over HTTP: answers are sent whole, a client cannot name the forwarded address, a huge body is refused', async () => {
  const echo = async request => new Response(JSON.stringify({
    path: new URL(request.url).pathname,
    forwarded: request.headers.get('x-forwarded-host'),
    body: request.method === 'POST' ? await request.text() : null,
  }), { status: 201, headers: { 'Content-Type': 'application/json' } });

  const listen = options => new Promise(resolve => {
    const server = createHttpServer(echo, { log: () => {}, ...options });
    server.listen(0, '127.0.0.1', () => resolve(server));
  });
  const open = await listen({});
  const trusting = await listen({ trustForwarded: true });
  try {
    const address = server => `http://127.0.0.1:${server.address().port}`;
    const sent = { headers: { 'X-Forwarded-Host': 'evil.example' } };

    const answer = await fetch(address(open) + '/api/health', sent);
    assert.equal(answer.status, 201);
    assert.deepEqual(await answer.json(), { path: '/api/health', forwarded: null, body: null });
    assert.equal((await (await fetch(address(trusting) + '/api/health', sent)).json()).forwarded, 'evil.example');

    const posted = await fetch(address(open) + '/api/session', { method: 'POST', body: '{"a":1}' });
    assert.equal((await posted.json()).body, '{"a":1}');

    const huge = await fetch(address(open) + '/api/session', { method: 'POST', body: 'x'.repeat(200_000) }).catch(() => null);
    assert.ok(huge === null || huge.status === 413, 'a 200 KB body must not reach the handler');
  } finally {
    open.close();
    trusting.close();
  }
});

test('careers and the session secret are kept in the data folder', async () => {
  const data = join(folder, 'data');
  const first = await sessionSecret(data);
  assert.equal(first.length, 64);
  assert.equal(await sessionSecret(data), first, 'the same secret after a restart');
  assert.equal(await sessionSecret(data, 'given by the environment, long enough'), 'given by the environment, long enough');

  const { load, save } = careerFile(data);
  assert.equal(await load(), null);
  await save({ tokens: { 1795: { matches: 1 } }, matches: {} });
  assert.deepEqual(await load(), { tokens: { 1795: { matches: 1 } }, matches: {} });
});

test('the wallet connector is offered only with a well-formed Reown project id, given or kept in the data folder', async () => {
  const data = join(folder, 'connector');
  await mkdir(data, { recursive: true });
  const id = '0123456789abcdef0123456789ABCDEF';
  const quiet = console.warn;
  console.warn = () => {};
  try {
    assert.equal(await reownProjectId(data), '', 'nothing given, no file');
    assert.equal(await reownProjectId(data, ' ' + id + '\n'), id.toLowerCase(), 'given by the environment');
    assert.equal(await reownProjectId(data, 'not an id'), '', 'a malformed one is ignored');
    await writeFile(join(data, 'reown-project-id'), id + '\n');
    assert.equal(await reownProjectId(data), id.toLowerCase(), 'read from the file');
    assert.equal(await reownProjectId(data, ''), id.toLowerCase(), 'an empty setting falls back on the file');
    await writeFile(join(data, 'reown-project-id'), 'pasted something else');
    assert.equal(await reownProjectId(data), '');
  } finally {
    console.warn = quiet;
  }

  const ask = api => api(new Request('https://game.example/api/wallet')).then(response => response.json());
  assert.deepEqual(await ask(createApi({ reownProjectId: id.toLowerCase() })), { reownProjectId: id.toLowerCase() });
  assert.deepEqual(await ask(createApi({})), { reownProjectId: '' });
});
