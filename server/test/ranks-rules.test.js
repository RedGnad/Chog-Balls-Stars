// The ladder rules of 21 October 2026 (research/evidence/ladder-references-2026-10-21.md,
// section 5): bots raise a wallet up to Silver only, the top pays less and costs more, a
// season's end brings every wallet above Gold toward it, and every event keeps its own numbers.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createMatches, createMemoryStore } from '../src/matches.js';
import { BANDS, SEASON_RESET, SOLO_CAP, createRanks, deltaFor, resetFor, scoringFor } from '../src/ranks.js';
import { createSeasons, seasonFrom } from '../src/seasons.js';

const address = number => '0x' + number.toString(16).padStart(40, '0');
const players = Array.from({ length: 6 }, (_, index) => ({ player: index + 1, team: index < 3 ? 0 : 1, tokenId: index + 1 }));
const admission = player => ({ player: player.player, team: player.team, tokenId: player.tokenId, address: player.address ?? address(player.player), right: 'owner' });
// The seventh wallet, playing the first seat alone.
const seventh = { ...players[0], address: address(7) };
const app = { appVersion: 'sim-example-siege' };
const verdict = { winnerTeam: 0, mode: 'Siege', testOptions: false, players };

// A ladder whose clock advances one second per recorded match, so events keep their order.
function ladder(start = '2026-10-21T00:00:00Z') {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  let clock = Date.parse(start);
  const ranks = createRanks({ store, matches, now: () => clock });
  let counter = 0;
  /** Records one Siege: "lineup" are the human seats; team 0 wins unless "winnerTeam" says otherwise. */
  async function play(lineup, { winnerTeam = 0, guests = [] } = {}) {
    const id = `game-${++counter}`;
    clock += 1000;
    await matches.entered(id, { how: 'create', userId: address(1), app });
    for (const player of lineup) {
      await matches.admitted(id, guests.includes(player.player) ? { ...admission(player), address: null, right: 'guest' } : admission(player));
    }
    return ranks.record(id, { ...verdict, winnerTeam, players: lineup });
  }
  return { store, matches, ranks, play, tick: () => new Date(clock).toISOString(), advanceTo: iso => { clock = Date.parse(iso); } };
}

test('the frozen table: bands shrink the gain and grow the loss from Gold, more from Star', () => {
  const table = [];
  for (let humans = 1; humans <= 6; humans++) {
    const scoring = scoringFor(humans);
    table.push([50, 250, 500].map(before => [deltaFor(scoring, before, 'win'), deltaFor(scoring, before, 'loss')]));
  }
  assert.deepEqual(table, [
    [[3, -2], [0, 0], [0, 0]],
    [[6, -4], [5, -5], [3, -6]],
    [[10, -6], [8, -8], [5, -9]],
    [[13, -8], [10, -10], [7, -12]],
    [[17, -10], [13, -13], [9, -15]],
    [[20, -12], [15, -15], [10, -18]],
  ]);
  assert.deepEqual(BANDS, [{ from: 250, win: 75, loss: 125 }, { from: 500, win: 50, loss: 150 }]);
  assert.equal(deltaFor(scoringFor(6), 499, 'win'), 15);
  assert.equal(deltaFor(scoringFor(6), 249, 'loss'), -12);
  assert.equal(deltaFor(scoringFor(6), 10, 'loss'), -10);
  assert.equal(deltaFor(scoringFor(6), 0, 'loss'), 0);
  assert.equal(Object.is(deltaFor(scoringFor(1), 0, 'loss'), -0), false);
  assert.equal(deltaFor(scoringFor(6), 600, 'draw'), 0);
});

test('an event recorded before the rules keeps scoring as it did then', () => {
  const old = { win: 20, loss: -12 };
  assert.equal(deltaFor(old, 600, 'win'), 20);
  assert.equal(deltaFor(old, 600, 'loss'), -12);
  assert.equal(deltaFor(old, 5, 'loss'), -5);
  assert.equal(deltaFor(undefined, 0, 'win'), 25);
  assert.equal(deltaFor(undefined, 10, 'loss'), -10);
});

test('solo matches raise a wallet to Silver exactly and no further', async () => {
  const { ranks, play } = ladder();
  for (let number = 1; number <= 33; number++) await play([players[0]]);
  assert.equal((await ranks.of(address(1))).points, 99);
  const reaching = await play([players[0]]);
  assert.equal(reaching.scoring.soloCap, SOLO_CAP);
  let standing = await ranks.of(address(1));
  assert.equal(standing.points, 100);
  assert.equal(standing.tier, 'SILVER');
  assert.equal(standing.last.delta, 1);
  await play([players[0]]);
  standing = await ranks.of(address(1));
  assert.equal(standing.points, 100);
  assert.equal(standing.last.delta, 0);
  assert.equal(standing.matches, 35);
  assert.equal(standing.wins, 35);
  await play([players[0]], { winnerTeam: 1 });
  standing = await ranks.of(address(1));
  assert.equal(standing.points, 100, 'a solo loss at Silver changes nothing either');
  assert.equal(standing.last.delta, 0);
});

test('a solo match with guests only is still a solo match; a solo loss below Silver still costs', async () => {
  const { ranks, play } = ladder();
  const event = await play([players[0], players[1], players[3]], { guests: [2, 4] });
  assert.equal(event.authenticatedHumans, 1);
  assert.equal(event.scoring.soloCap, SOLO_CAP);
  assert.equal((await ranks.of(address(1))).points, 3);
  await play([players[0]], { winnerTeam: 1 });
  assert.equal((await ranks.of(address(1))).points, 1);
});

test('a Gold wallet earned against humans neither climbs nor falls against bots', async () => {
  const { ranks, play } = ladder();
  for (let number = 1; number <= 13; number++) await play(players);
  let standing = await ranks.of(address(1));
  assert.equal(standing.points, 260);
  assert.equal(standing.tier, 'GOLD');
  await play([players[0]]);
  await play([players[0]], { winnerTeam: 1 });
  standing = await ranks.of(address(1));
  assert.equal(standing.points, 260);
  assert.equal(standing.matches, 15);
  assert.equal(standing.last.delta, 0);
});

test('from Gold a six-human win pays 15 and a loss costs 15; from Star 10 and 18', async () => {
  const { ranks, play } = ladder();
  for (let number = 1; number <= 12; number++) await play(players);
  assert.equal((await ranks.of(address(1))).points, 240);
  await play(players);
  assert.equal((await ranks.of(address(1))).points, 260, '240 is below Gold: the full 20');
  await play(players);
  let standing = await ranks.of(address(1));
  assert.equal(standing.points, 275);
  assert.equal(standing.last.delta, 15);
  await play(players, { winnerTeam: 1 });
  standing = await ranks.of(address(1));
  assert.equal(standing.points, 260);
  assert.equal(standing.last.delta, -15);
  for (let number = 1; number <= 16; number++) await play(players);
  assert.equal((await ranks.of(address(1))).points, 500);
  await play(players);
  standing = await ranks.of(address(1));
  assert.equal(standing.points, 510);
  assert.equal(standing.last.delta, 10);
  await play(players, { winnerTeam: 1 });
  standing = await ranks.of(address(1));
  assert.equal(standing.points, 492);
  assert.equal(standing.last.delta, -18);
  assert.equal(standing.last.tierBefore, 'STAR');
  assert.equal(standing.last.tierAfter, 'GOLD');
  // The losing side of the same matches: the bands apply to each wallet's own points.
  const loser = await ranks.of(address(4));
  assert.equal(loser.points, 20);
  assert.equal(loser.wins, 2);
});

test('two-human matches round the banded amounts to the nearest point', async () => {
  const { ranks, play } = ladder();
  const pair = [players[0], players[3]];
  for (let number = 1; number <= 42; number++) await play(pair);
  assert.equal((await ranks.of(address(1))).points, 252);
  await play(pair);
  assert.equal((await ranks.of(address(1))).last.delta, 5);
  await play(pair, { winnerTeam: 1 });
  assert.equal((await ranks.of(address(1))).last.delta, -5);
});

test('a season reset brings every wallet above Gold toward it, once, as one event per wallet', async () => {
  const { ranks, play, tick } = ladder();
  for (let number = 1; number <= 30; number++) await play(players);
  for (let number = 1; number <= 25; number++) await play([players[3], players[4], players[5]], { winnerTeam: 1 });
  for (let number = 1; number <= 10; number++) await play([seventh]);
  const before = { star: (await ranks.of(address(1))).points, gold: (await ranks.of(address(4))).points, low: (await ranks.of(address(7))).points, none: (await ranks.of(address(8))).points };
  assert.deepEqual(before, { star: 510, gold: 250, low: 30, none: 0 });
  assert.equal(resetFor(530), 390);
  assert.equal(resetFor(250), 250);
  assert.equal(resetFor(251), 251);
  assert.equal(resetFor(260, { floor: 250, keep: 50 }), 255);
  assert.deepEqual(SEASON_RESET, { floor: 250, keep: 50 });

  const endsAt = tick();
  const resets = await ranks.resetSeason({ seasonId: '2026-10', at: endsAt });
  assert.deepEqual(resets.map(reset => [reset.address, reset.before, reset.after]), [[address(1), 510, 380], [address(2), 510, 380], [address(3), 510, 380]]);
  assert.deepEqual(resets[0], { kind: 'reset', seasonId: '2026-10', address: address(1), at: endsAt, before: 510, after: 380, floor: 250, keep: 50 });
  const again = await ranks.resetSeason({ seasonId: '2026-10', at: endsAt });
  assert.deepEqual(again, resets, 'asking again writes nothing new');
  assert.equal((await ranks.allResets()).length, 3);

  const star = await ranks.of(address(1));
  assert.equal(star.points, 380);
  assert.equal(star.tier, 'GOLD');
  assert.equal(star.matches, 30);
  assert.deepEqual(star.lastReset, { seasonId: '2026-10', at: endsAt, before: 510, after: 380, tierBefore: 'STAR', tierAfter: 'GOLD' });
  assert.equal(star.last.gameId, 'game-30', 'the last match is still the last match');
  assert.equal((await ranks.of(address(4))).lastReset, null);
  assert.equal((await ranks.of(address(4))).points, 250);
  assert.equal((await ranks.of(address(7))).points, 30);

  // The next season starts from the reset value, with the Gold band.
  await play(players);
  const next = await ranks.of(address(1));
  assert.equal(next.points, 395);
  assert.equal(next.last.delta, 15);
  await assert.rejects(ranks.resetSeason({ seasonId: 'bad season', at: endsAt }), RangeError);
  await assert.rejects(ranks.resetSeason({ seasonId: 'ok', at: 'yesterday' }), RangeError);
});

test('a match judged after the season end is not in the reset and counts after it', async () => {
  const { ranks, play, tick, advanceTo } = ladder();
  for (let number = 1; number <= 30; number++) await play(players);
  const endsAt = tick();
  advanceTo('2026-11-01T00:00:00Z');
  await play(players);
  assert.equal((await ranks.of(address(1))).points, 520);
  const resets = await ranks.resetSeason({ seasonId: 'first', at: endsAt });
  assert.deepEqual(resets.map(reset => [reset.before, reset.after]), [[510, 380], [510, 380], [510, 380]]);
  const standing = await ranks.of(address(1));
  assert.equal(standing.points, 395, '380 after the reset, then the late match with the Gold band');
  assert.equal((await ranks.standingOf(address(1), { until: endsAt })).points, 380, 'the standing at the end includes the reset written at that instant');
});

test('the season configuration names a season whole or not at all', () => {
  assert.equal(seasonFrom({}), null);
  assert.equal(seasonFrom({ id: '', endsAt: '' }), null);
  assert.deepEqual(seasonFrom({ id: '2026-10', endsAt: '2026-10-31T00:00:00Z' }), { id: '2026-10', startsAt: null, endsAt: '2026-10-31T00:00:00.000Z' });
  assert.deepEqual(seasonFrom({ id: 's1', startsAt: '2026-10-01', endsAt: '2026-10-31' }).startsAt, '2026-10-01T00:00:00.000Z');
  assert.throws(() => seasonFrom({ id: '2026-10' }), RangeError);
  assert.throws(() => seasonFrom({ endsAt: '2026-10-31T00:00:00Z' }), RangeError);
  assert.throws(() => seasonFrom({ id: 'has space', endsAt: '2026-10-31T00:00:00Z' }), RangeError);
  assert.throws(() => seasonFrom({ id: 'has_underscore', endsAt: '2026-10-31T00:00:00Z' }), RangeError);
  assert.throws(() => seasonFrom({ id: 'ok', endsAt: 'soon' }), RangeError);
  assert.throws(() => seasonFrom({ id: 'ok', startsAt: '2026-11-01', endsAt: '2026-10-31' }), RangeError);
});

test('the season closes once its end has passed, resets and remembers it, and calls the hook once', async () => {
  const { store, ranks, play, advanceTo } = ladder();
  for (let number = 1; number <= 30; number++) await play(players);
  const closings = [];
  const season = seasonFrom({ id: 'oct', endsAt: '2026-10-31T15:00:00Z' });
  let clock = Date.parse('2026-10-31T14:59:59Z');
  const seasons = createSeasons({ ranks, store, season, now: () => clock, onClosed: closing => { closings.push(closing); } });
  assert.deepEqual(await seasons.current(), { id: 'oct', startsAt: null, endsAt: '2026-10-31T15:00:00.000Z', ended: false, closed: false });
  assert.equal(await seasons.closeIfEnded(), null);
  assert.equal((await ranks.of(address(1))).points, 510);
  clock = Date.parse('2026-10-31T15:00:00Z');
  const closing = await seasons.closeIfEnded();
  assert.equal(closing.seasonId, 'oct');
  assert.equal(closing.resets.length, 3, 'the three winners were above Gold');
  assert.equal((await ranks.of(address(1))).points, 380);
  assert.equal((await ranks.of(address(4))).points, 0);
  assert.deepEqual(await seasons.current(), { id: 'oct', startsAt: null, endsAt: '2026-10-31T15:00:00.000Z', ended: true, closed: true });
  assert.equal(await seasons.closeIfEnded(), null);
  assert.equal(closings.length, 1);
  // Another instance with the same store sees the season closed and does nothing.
  const other = createSeasons({ ranks, store, season, now: () => clock, onClosed: closing => { closings.push(closing); } });
  assert.equal(await other.closeIfEnded(), null);
  assert.equal(closings.length, 1);
  assert.equal((await ranks.allResets()).length, 3);
  assert.deepEqual((await store.list('seasons/')).filter(key => key.endsWith('closed')), ['seasons/oct_closed']);
  // Without a season nothing happens.
  const none = createSeasons({ ranks, store, season: null, now: () => clock });
  assert.equal(await none.current(), null);
  assert.equal(await none.closeIfEnded(), null);
  // The watcher looks at once and then on its interval.
  const calls = [];
  advanceTo('2026-11-01T00:00:00Z');
  const timer = seasons.watch({ everyMs: 5, setIntervalImpl: (fn, ms) => { calls.push(ms); return { unref() { calls.push('unref'); } }; } });
  assert.deepEqual(calls, [5, 'unref']);
  assert.ok(timer);
});
