// The hidden rating (rating.js): Elo from verified matches between authenticated humans, never
// moved by bots, guests or a season reset, answered as a band only, and written on every room
// Photon creates so that matchmaking can filter on it.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createMatches, createMemoryStore } from '../src/matches.js';
import { createRanks } from '../src/ranks.js';
import { BAND, RATING, UNRATED_BAND, applyEvent, bandFor, computeRatings, expectedScore, kFor, ratingEligibility } from '../src/rating.js';
import { RATING_BAND_KEY, createPhoton } from '../src/photon.js';
import { createApi } from '../src/api.js';
import { createSessions } from '../src/session.js';
import { createWallet } from './wallet.js';

const address = number => '0x' + number.toString(16).padStart(40, '0');
const players = Array.from({ length: 6 }, (_, index) => ({ player: index + 1, team: index < 3 ? 0 : 1, tokenId: index + 1 }));
const admission = player => ({ player: player.player, team: player.team, tokenId: player.tokenId, address: player.address ?? address(player.player), right: 'owner' });
const app = { appVersion: 'sim-example-siege' };
const verdict = { winnerTeam: 0, mode: 'Siege', testOptions: false, players };
const PHOTON_SECRET = 'a photon secret that is long enough for the tests';

function ladder() {
  const store = createMemoryStore();
  const matches = createMatches({ store });
  let clock = Date.parse('2026-10-21T00:00:00Z');
  const ranks = createRanks({ store, matches, now: () => clock });
  let counter = 0;
  async function play(lineup, { winnerTeam = 0, guests = [] } = {}) {
    const id = `game-${++counter}`;
    clock += 1000;
    await matches.entered(id, { how: 'create', userId: address(1), app });
    for (const player of lineup) {
      await matches.admitted(id, guests.includes(player.player) ? { ...admission(player), address: null, right: 'guest' } : admission(player));
    }
    return ranks.record(id, { ...verdict, winnerTeam, players: lineup });
  }
  return { store, matches, ranks, play, tick: () => new Date(clock).toISOString() };
}

const event = (winnerTeam, lineup = players) => ({ reason: null, winnerTeam, players: lineup.map(player => ({ address: address(player.player), team: player.team })) });

test('the Elo formula, FIDE K schedule and the bands are what the note says', () => {
  assert.equal(expectedScore(1500, 1500), 0.5);
  assert.equal(expectedScore(1500, 1700).toFixed(3), '0.240');
  assert.equal(expectedScore(1700, 1500).toFixed(3), '0.760');
  assert.equal(expectedScore(1500, 1900).toFixed(3), '0.091');
  assert.deepEqual([kFor(0), kFor(29), kFor(30), kFor(1000)], [40, 40, 20, 20]);
  assert.deepEqual(RATING, { start: 1500, provisionalMatches: 30, kProvisional: 40, kSettled: 20 });
  assert.deepEqual(BAND, { width: 150, centre: 1500, count: 7 });
  assert.deepEqual([0, 1124, 1125, 1274, 1275, 1424, 1425, 1500, 1574, 1575, 1724, 1725, 1874, 1875, 3000].map(bandFor),
    [0, 0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 5, 5, 6, 6]);
  assert.equal(UNRATED_BAND, 3);
});

test('only a verified match with authenticated humans on both teams rates anyone', () => {
  assert.equal(ratingEligibility(event(0)), null);
  assert.equal(ratingEligibility(event(2)), null);
  assert.equal(ratingEligibility(event(0, players.slice(0, 3))), 'one_side_only');
  assert.equal(ratingEligibility(event(0, [players[0], players[3]])), null);
  assert.equal(ratingEligibility({ ...event(0), reason: 'preview' }), 'preview');
  assert.equal(ratingEligibility({ ...event(0), winnerTeam: null }), 'unverified');
  assert.equal(ratingEligibility(null), 'unverified');
});

test('equal teams move by K/2 on a win, nothing on a draw, and a stronger team wins less', () => {
  const ratings = new Map();
  const first = applyEvent(ratings, event(0));
  assert.equal(first.reason, null);
  assert.equal(first.changes.length, 6);
  assert.deepEqual(first.changes.map(change => change.after), [1520, 1520, 1520, 1480, 1480, 1480]);
  assert.deepEqual(ratings.get(address(1)), { rating: 1520, matches: 1 });
  const draw = applyEvent(new Map([[address(1), { rating: 1500, matches: 0 }]]), event(2));
  assert.deepEqual(draw.changes.map(change => change.after - change.before), [0, 0, 0, 0, 0, 0]);
  // Team 0 is rated 1700 on average: it is expected to win 76% of the time and gains 9.6.
  const uneven = () => new Map([[address(1), { rating: 1900, matches: 0 }], [address(2), { rating: 1600, matches: 0 }], [address(3), { rating: 1600, matches: 0 }]]);
  const favourite = applyEvent(uneven(), event(0));
  assert.equal(favourite.changes[0].expected.toFixed(3), '0.760');
  assert.equal((favourite.changes[0].after - favourite.changes[0].before).toFixed(1), '9.6');
  assert.equal((favourite.changes[3].after - favourite.changes[3].before).toFixed(1), '-9.6');
  const upset = applyEvent(uneven(), event(1));
  assert.equal((upset.changes[3].after - upset.changes[3].before).toFixed(1), '30.4');
  // A settled wallet moves at K 20.
  const settled = applyEvent(new Map([[address(1), { rating: 1500, matches: 30 }]]), event(0));
  assert.equal(settled.changes[0].after, 1510);
  assert.equal(settled.changes[1].after, 1520);
  const nothing = applyEvent(new Map(), { reason: 'preview', winnerTeam: 0, players: [] });
  assert.deepEqual(nothing, { reason: 'preview', changes: [] });
});

test('ratings come from the events in order, ignore bot and guest matches, and never reset', async () => {
  const { ranks, play, tick } = ladder();
  await play(players);
  let rated = await ranks.ratingOf(address(1));
  assert.deepEqual(rated, { rating: 1520, matches: 1, band: 3 });
  assert.deepEqual(await ranks.ratingOf(address(4)), { rating: 1480, matches: 1, band: 3 });
  assert.deepEqual(await ranks.ratingOf(address(9)), { rating: 1500, matches: 0, band: 3 });
  for (let number = 1; number <= 10; number++) await play([players[0]]);
  await play([players[0], players[1], players[3]], { guests: [4] });
  assert.deepEqual(await ranks.ratingOf(address(1)), rated, 'solo and one-sided matches rate nobody');
  for (let number = 1; number <= 6; number++) await play(players);
  rated = await ranks.ratingOf(address(1));
  assert.equal(rated.matches, 7);
  assert.equal(rated.band, 4);
  assert.ok(rated.rating > 1575 && rated.rating < 1620, `rating ${rated.rating}`);
  assert.equal((await ranks.ratingOf(address(4))).band, 2);
  assert.equal(await ranks.bandOf(address(1)), 4);
  assert.equal(await ranks.bandOf(address(4)), 2);
  assert.equal(await ranks.bandOf(null), UNRATED_BAND);
  assert.equal(await ranks.bandOf('guest-abc'), UNRATED_BAND);
  await ranks.resetSeason({ seasonId: 'one', at: tick() });
  assert.deepEqual(await ranks.ratingOf(address(1)), rated, 'the season reset touches points, not the rating');
  assert.deepEqual(computeRatings(await ranks.allEvents()).get(address(1)), { rating: rated.rating, matches: 7 });
});

test('the signed rank answer carries a band and never the rating number', async () => {
  const { matches, ranks, play } = ladder();
  const sessions = createSessions({ secret: 'a test secret that is long enough to be accepted' });
  const wallet = createWallet();
  const api = createApi({ sessions, matches, ranks, publicOrigin: 'https://game.example' });
  const challenge = await api(new Request('https://game.example/api/session/challenge', { method: 'POST', body: JSON.stringify({ address: wallet.address }) })).then(response => response.json());
  const signed = await api(new Request('https://game.example/api/session', { method: 'POST', body: JSON.stringify({ ...challenge, signature: wallet.personalSign(challenge.message) }) })).then(response => response.json());
  const read = () => api(new Request('https://game.example/api/rank/me', { headers: { Authorization: `Bearer ${signed.session}` } })).then(response => response.json());
  let answer = await read();
  assert.deepEqual(answer.rating, { band: 3, rated: false, matches: 0 });
  await play([{ ...players[0], address: wallet.address }, players[3]]);
  answer = await read();
  assert.deepEqual(answer.rating, { band: 3, rated: true, matches: 1 });
  assert.equal(JSON.stringify(answer).includes('1520'), false, 'the number stays on the server');
  assert.equal(answer.rank.points, 6);
});

test('every room Photon creates carries the creator band as C0, set by the services', async () => {
  const { matches, ranks, play } = ladder();
  for (let number = 1; number <= 7; number++) await play(players);
  const photon = createPhoton({ secret: PHOTON_SECRET, sessions: {}, careers: {}, chain: {}, matches, ratingBandOf: wallet => ranks.bandOf(wallet) });
  const create = (cookie, roomOptions = {}) => photon.hook('game/create', {
    AppId: 'app', AppVersion: 'sim-x-siege', Region: 'eu', Cloud: '0', UserId: 'u', RoomName: 'room', GameId: '0:eu:room',
    AuthCookie: cookie, EnterRoomParams: { RoomOptions: roomOptions },
  }, PHOTON_SECRET);
  const strong = await create({ address: address(1) });
  assert.equal(strong.status, 200);
  assert.equal(RATING_BAND_KEY, 'C0');
  assert.deepEqual(strong.body.EnterRoomParams, { RoomOptions: { CustomRoomProperties: { C0: 4 }, CustomRoomPropertiesForLobby: ['C0'] } });
  assert.equal((await matches.get(strong.body.GameId)).app.ratingBand, 4);
  const weak = await create({ address: address(4) });
  assert.equal(weak.body.EnterRoomParams.RoomOptions.CustomRoomProperties.C0, 2);
  const visitor = await create({ visitor: true });
  assert.equal(visitor.body.EnterRoomParams.RoomOptions.CustomRoomProperties.C0, UNRATED_BAND);
  const guest = await create({ passTokenId: 5, passLender: address(1), passIssuedAt: 1 });
  assert.equal(guest.body.EnterRoomParams.RoomOptions.CustomRoomProperties.C0, UNRATED_BAND);
  // A band the page claims is overwritten; what else it set is kept, and the lobby lists at most three keys.
  const claiming = await create({ address: address(4) }, { CustomRoomProperties: { C0: 6, mode: 'siege' }, CustomRoomPropertiesForLobby: ['mode', 'C0', 'x', 'y'] });
  assert.deepEqual(claiming.body.EnterRoomParams.RoomOptions, { CustomRoomProperties: { mode: 'siege', C0: 2 }, CustomRoomPropertiesForLobby: ['C0', 'mode', 'x'] });
  // Without the rating wired, the answer is what it was.
  const plain = createPhoton({ secret: PHOTON_SECRET, sessions: {}, careers: {}, chain: {}, matches });
  const unchanged = await plain.hook('game/create', { AppId: 'app', AppVersion: 'v', Region: 'eu', Cloud: '0', UserId: 'u', RoomName: 'r', GameId: '0:eu:r', AuthCookie: { address: address(1) } }, PHOTON_SECRET);
  assert.deepEqual(Object.keys(unchanged.body), ['GameId']);
  assert.equal((await matches.get(unchanged.body.GameId)).app.ratingBand, null);
});
