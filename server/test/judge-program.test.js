import assert from 'node:assert/strict';
import { EventEmitter } from 'node:events';
import { mkdtemp, readFile, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { PassThrough } from 'node:stream';
import { test } from 'node:test';
import { residentReplay } from '../src/node.js';

// A stand-in for the judge program kept running: it reads "<record>\t<report>" lines and answers "CHOG_JUDGED <code>", after
// writing a report that names the record. "behaviour" decides, for the nth line, what it does: 'judge', 'fail', 'die' or 'hang'.
function fakeProgram(behaviour) {
  const started = [];
  const spawnProgram = (command, args) => {
    const child = new EventEmitter();
    const index = started.length;
    child.args = args;
    child.stdin = new PassThrough();
    child.stdout = new PassThrough();
    child.stderr = new PassThrough();
    child.kill = () => setImmediate(() => child.emit('close', null, 'SIGKILL'));
    let lines = 0;
    child.stdin.setEncoding('utf8');
    child.stdin.on('data', async text => {
      for (const line of text.split('\n').filter(Boolean)) {
        const [record, report] = line.split('\t');
        const what = behaviour(index, lines++);
        if (what === 'judge') {
          await writeFile(report, JSON.stringify({ record, winnerTeam: 1 }));
          child.stdout.write('team 1 won\nCHOG_JUDGED 0\n');
        } else if (what === 'fail') {
          child.stderr.write('the record holds no commands\n');
          child.stdout.write('CHOG_JUDGED 1\n');
        } else if (what === 'die') {
          child.stderr.write('crashed\n');
          setTimeout(() => child.emit('close', 134, null), 5);
        }
      }
    });
    started.push(child);
    return child;
  };
  return { started, spawnProgram };
}

test('the judge kept running judges match after match, one at a time, with one start', async () => {
  const root = await mkdtemp(join(tmpdir(), 'chog-judge-'));
  const { started, spawnProgram } = fakeProgram(() => 'judge');
  const replay = residentReplay({ dotnet: 'dotnet', verifier: 'v.dll', database: 'db.json', lut: 'lut', matchesRoot: root, spawnProgram });
  const { mkdir } = await import('node:fs/promises');
  await mkdir(join(root, 'a'));
  await mkdir(join(root, 'b'));
  const [first, second] = await Promise.all([replay('a'), replay('b')]);
  assert.equal(first.record, join(root, 'a'));
  assert.equal(second.record, join(root, 'b'));
  assert.equal(started.length, 1, 'one program for both');
  assert.deepEqual(started[0].args, ['v.dll', '--serve', 'db.json', 'lut']);
  assert.equal(JSON.parse(await readFile(join(root, 'b', 'replay-report.json'), 'utf8')).record, join(root, 'b'));
});

test('a match the program refuses, a program that stops and one that hangs give no verdict, and the next match is judged', async () => {
  const root = await mkdtemp(join(tmpdir(), 'chog-judge-'));
  const { mkdir } = await import('node:fs/promises');
  for (const name of ['a', 'b', 'c', 'd']) await mkdir(join(root, name));
  // First program: refuses its first line, dies on its second. Second program: hangs. Third: judges.
  const { started, spawnProgram } = fakeProgram((program, line) => (program === 0 ? (line === 0 ? 'fail' : 'die') : program === 1 ? 'hang' : 'judge'));
  const replay = residentReplay({ dotnet: 'dotnet', verifier: 'v.dll', database: 'db.json', lut: 'lut', matchesRoot: root, spawnProgram, timeoutMs: 50 });
  await assert.rejects(replay('a'), /exit 1: the record holds no commands/);
  await assert.rejects(replay('b'), /the judge stopped: .*crashed/);
  await assert.rejects(replay('c'), /took too long/);
  const judged = await replay('d');
  assert.equal(judged.winnerTeam, 1);
  assert.equal(started.length, 3, 'started again after it stopped and after it hung');
});
