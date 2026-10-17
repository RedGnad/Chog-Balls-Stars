import assert from 'node:assert/strict';
import { test } from 'node:test';
import { entriesOf, recordData, unpackCareer } from '../src/careers-onchain.js';
import { createRental } from '../src/rental.js';
import { addressOf, signTransaction } from '../src/sender.js';

const word = value => BigInt(value).toString(16).padStart(64, '0');

test('a transaction signed for the referee is the one Foundry decodes', () => {
  // The key 1, whose address is well known; checked with `cast decode-transaction` on 17 October 2026.
  const key = new Uint8Array(32);
  key[31] = 1;
  assert.equal(addressOf(key), '0x7e5f4552091a69125d5dfcb7b8c2659029395bdf');
  const raw = signTransaction({
    chainId: 143, nonce: 5, maxPriorityFeePerGas: 2000000000n, maxFeePerGas: 202000000000n, gas: 100000,
    to: '0x335bf7bc610645546e2b00de6a9fd0c4709d6111', data: '0x1234',
  }, key);
  assert.equal(raw, '0x02f86f818f058477359400852f08236400830186a094335bf7bc610645546e2b00de6a9fd0c4709d611180821234c080a085d7755419f8c8585fc770dae71d5acfbe6199b5660ffaeb59d148fea1552c94a07eb837a7cab1a6d0e10fbe8ddec1e862597f5f81e847a9ea35f455d016b1b8df');
});

test('the record of a match is the calldata Foundry encodes', () => {
  // cast calldata "record(bytes32,uint8,(uint16,uint8,uint8)[])" 0x..07 1 "[(16,0,0),(1795,1,1)]"
  const expected = '0x071e705f' + word(7) + word(1) + word(96) + word(2) + word(16) + word(0) + word(0) + word(1795) + word(1) + word(1);
  assert.equal(recordData('0x' + word(7), 1, [{ tokenId: 16, team: 0, playedBy: 0 }, { tokenId: 1795, team: 1, playedBy: 1 }]), expected);
});

test('a packed career reads from its lowest bits', () => {
  const packed = (BigInt(1_790_000_000) << 192n) | (2n << 176n) | (1n << 160n) | (4n << 128n) | (3n << 96n) | (1n << 64n) | (5n << 32n) | 9n;
  assert.deepEqual(unpackCareer(packed.toString(16)), {
    matches: 9, wins: 5, draws: 1, rentedMatches: 3, guestMatches: 4, streak: 1, bestStreak: 2, lastPlayedAt: 1_790_000_000,
  });
  assert.equal(unpackCareer('0').matches, 0);
});

test('a judged match records each Genesis played with a right, once, by token id', () => {
  const verdict = {
    winnerTeam: 1,
    players: [
      { player: 0, team: 0, tokenId: 1795 },
      { player: 1, team: 1, tokenId: 16 },
      { player: 2, team: 1, tokenId: 0 },
      { player: 3, team: 0, tokenId: 500 },
      { player: 4, team: 1, tokenId: 1795 },
      { player: 5, team: 0, tokenId: 77 },
    ],
  };
  const admissions = [
    { player: 0, tokenId: 1795, right: 'holder' },
    { player: 1, tokenId: 16, right: 'guest' },
    { player: 2, tokenId: 0, right: null },
    // Asked for 500 without a right: admitted as a recruit, so its replay shows no Genesis... or a stand-in.
    { player: 3, tokenId: 500, right: 'holder', standIn: true },
    { player: 4, tokenId: 1795, right: 'renter' },
  ];
  assert.deepEqual(entriesOf(verdict, admissions), [
    { tokenId: 16, team: 1, playedBy: 2 },
    { tokenId: 1795, team: 0, playedBy: 0 },
  ]);
  assert.equal(entriesOf({ void: true }, admissions), null);
  assert.equal(entriesOf({ winnerTeam: 0, players: [{ player: 2, team: 1, tokenId: 0 }] }, admissions), null);
  assert.deepEqual(entriesOf({ winnerTeam: 2, players: [{ player: 4, team: 1, tokenId: 1795 }] }, admissions), [{ tokenId: 1795, team: 1, playedBy: 1 }]);
});

test('the money of an address in the rental is read from the records on chain', async () => {
  const holder = '0x' + 'a'.repeat(40);
  const renter = '0x' + 'b'.repeat(40);
  const record = (tokenId, who, paid, until, escrow, earned, counting) =>
    word(tokenId) + word('0x' + who) + word('0x' + paid) + word(until) + word(escrow) + word(earned) + word(counting ? 1 : 0);
  const records = '0x' + word(32) + word(2)
    + record(1795, 'b'.repeat(40), 'a'.repeat(40), 2_000_000_000, 30n * 10n ** 18n, 10n * 10n ** 18n, true)
    + record(16, 'b'.repeat(40), 'c'.repeat(40), 2_000_000_000, 20n * 10n ** 18n, 5n * 10n ** 18n, false);
  const answers = {
    rentalsOnRecord: records,
    earnings: '0x' + word(4n * 10n ** 18n),
    refunds: '0x' + word(0),
    offerOf: '0x' + word(1) + word(10n ** 19n) + word('0x' + 'a'.repeat(40)),
  };
  const chain = {
    // earnings(address), refunds(address) and offerOf(uint256) by their selectors; the one read without arguments is rentalsOnRecord().
    call: async (to, data) => answers[{ '0x543fd313': 'earnings', '0xbc3da535': 'refunds', '0xa3cb2f44': 'offerOf' }[data.slice(0, 10)] ?? 'rentalsOnRecord'],
  };
  const rental = createRental({ chain, address: '0x' + '9'.repeat(40) });
  const asHolder = await rental.account(holder);
  assert.equal(asHolder.earned, (14n * 10n ** 18n).toString(), 'earned before plus earned since on its running rental');
  assert.equal(asHolder.owedBack, '0');
  assert.deepEqual(asHolder.lent.map(rented => rented.tokenId), [1795]);
  assert.deepEqual(asHolder.toSettle, [1795]);

  const asRenter = await rental.account(renter);
  assert.equal(asRenter.owedBack, (15n * 10n ** 18n).toString(), 'the sold one gives back what was not earned');
  assert.deepEqual(asRenter.rented.map(rented => [rented.tokenId, rented.counting, rented.pricePerDay]), [
    [1795, true, (10n ** 19n).toString()], [16, false, (10n ** 19n).toString()],
  ]);
  assert.deepEqual(asRenter.toSettle, [16, 1795]);
});

test('a stand-in plays its Genesis only while its NFT is held, and a setting must be well formed', async () => {
  const { createStandIns, parseStandIns } = await import('../src/standin.js');
  assert.deepEqual(parseStandIns(''), []);
  assert.throws(() => parseStandIns('0x123:1=1795'));
  assert.throws(() => parseStandIns('0x' + 'c'.repeat(40) + ':1=1970'));

  const keeper = '0x' + 'd'.repeat(40);
  let holder = keeper;
  const chain = { call: async () => '0x' + word(holder) };
  const standIns = createStandIns({ chain, setting: '0x' + 'c'.repeat(40) + ':42=1795' });
  assert.deepEqual(await standIns.genesisOf(keeper), [1795]);
  assert.equal(await standIns.standsIn(keeper, 1795), true);
  assert.equal(await standIns.standsIn(keeper, 16), false);
  holder = '0x' + 'e'.repeat(40);
  assert.deepEqual(await standIns.genesisOf(keeper), [], 'sold, the stand-in no longer plays');
  assert.deepEqual(await createStandIns({ chain, setting: '' }).genesisOf(keeper), []);
});
