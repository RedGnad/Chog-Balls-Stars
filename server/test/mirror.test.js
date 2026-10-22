import assert from 'node:assert/strict';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { matchFiles, mirroredMatchStore } from '../src/node.js';

function memoryRemote() {
  const kept = new Map();
  return { kept, get: async key => kept.get(key) ?? null, set: async (key, value) => { kept.set(key, value); return true; } };
}

test('a new container finds the match records in the mirror, but not the replay', async () => {
  const remote = memoryRemote();
  const first = mirroredMatchStore(matchFiles(await mkdtemp(join(tmpdir(), 'chog-mirror-'))), remote);
  await first.set('match-abc/match', '{"gameId":"abc"}');
  await first.set('match-abc/replay-chunk-000001', '{"chunk":1}');
  await first.set('players/0xabc', '{"gameId":"abc"}');
  assert.equal(remote.kept.has('matches/match-abc/replay-chunk-000001'), false, 'the replay stays on the disk');

  // Another container: an empty disk, the same remote store.
  const second = mirroredMatchStore(matchFiles(await mkdtemp(join(tmpdir(), 'chog-mirror-'))), remote);
  assert.equal(await second.get('match-abc/match'), '{"gameId":"abc"}');
  assert.equal(await second.get('players/0xabc'), '{"gameId":"abc"}');
  assert.equal(await second.get('match-abc/replay-chunk-000001'), null);
});

test('a mirror that fails does not stop a match', async () => {
  const broken = { get: async () => { throw new Error('down'); }, set: async () => { throw new Error('down'); } };
  const store = mirroredMatchStore(matchFiles(await mkdtemp(join(tmpdir(), 'chog-mirror-'))), broken);
  await store.set('match-abc/match', '{}');
  assert.equal(await store.get('match-abc/match'), '{}');
  assert.equal(await store.get('match-abc/verdict'), null);
});
