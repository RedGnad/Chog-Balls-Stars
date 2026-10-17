// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Test} from "forge-std/Test.sol";
import {ChogCareers} from "../src/ChogCareers.sol";

contract ChogCareersTest is Test {
    bytes32 private constant MATCH = keccak256("eu:quick-play-0001");
    uint8 private constant HOLDER = 0;
    uint8 private constant RENTER = 1;
    uint8 private constant GUEST = 2;
    uint8 private constant DRAW = 2;

    ChogCareers private careers;
    address private admin = makeAddr("admin");
    address private referee = makeAddr("referee");
    uint256 private matchNumber;

    event MatchRecorded(bytes32 indexed matchKey, uint8 winnerTeam, uint256 entries);
    event Played(bytes32 indexed matchKey, uint256 indexed tokenId, uint8 team, uint8 playedBy, uint8 outcome);
    event RefereeChanged(address indexed referee);
    event OwnerChanged(address indexed previousOwner, address indexed newOwner);

    function setUp() public {
        vm.warp(1_790_000_000);
        careers = new ChogCareers(referee, admin);
    }

    // A match of one Genesis on team 0, played by its holder.
    function solo(uint16 tokenId) private pure returns (ChogCareers.Entry[] memory entries) {
        entries = new ChogCareers.Entry[](1);
        entries[0] = ChogCareers.Entry(tokenId, 0, HOLDER);
    }

    // The next match: a fresh key, a minute later.
    function play(uint8 winnerTeam, ChogCareers.Entry[] memory entries) private {
        vm.warp(block.timestamp + 1 minutes);
        vm.prank(referee);
        careers.record(keccak256(abi.encode("match", ++matchNumber)), winnerTeam, entries);
    }

    // A whole 3v3: Genesis 7, 1795 and 1969 on team 0, 1, 2 and 900 on team 1, every way of playing.
    function threeAgainstThree() private pure returns (ChogCareers.Entry[] memory entries) {
        entries = new ChogCareers.Entry[](6);
        entries[0] = ChogCareers.Entry(1, 1, HOLDER);
        entries[1] = ChogCareers.Entry(2, 1, RENTER);
        entries[2] = ChogCareers.Entry(7, 0, HOLDER);
        entries[3] = ChogCareers.Entry(900, 1, GUEST);
        entries[4] = ChogCareers.Entry(1795, 0, RENTER);
        entries[5] = ChogCareers.Entry(1969, 0, GUEST);
    }

    function test_OnlyTheRefereeRecords() public {
        vm.expectRevert(ChogCareers.NotReferee.selector);
        careers.record(MATCH, 0, solo(1795));
        vm.prank(admin);
        vm.expectRevert(ChogCareers.NotReferee.selector);
        careers.record(MATCH, 0, solo(1795));

        vm.prank(referee);
        careers.record(MATCH, 0, solo(1795));
        assertEq(careers.careerOf(1795).matches, 1);
    }

    function test_AMatchKeyIsRecordedOnce() public {
        vm.prank(referee);
        careers.record(MATCH, 1, solo(1795));
        vm.prank(referee);
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.AlreadyRecorded.selector, MATCH));
        careers.record(MATCH, 0, solo(1));

        (bool recorded, uint8 winnerTeam) = careers.resultOf(MATCH);
        assertTrue(recorded);
        assertEq(winnerTeam, 1);
        (recorded,) = careers.resultOf(keccak256("never played"));
        assertFalse(recorded);
        assertEq(careers.careerOf(1).matches, 0);
    }

    function test_AWholeMatchCreditsEveryGenesis() public {
        play(0, threeAgainstThree());
        ChogCareers.Career memory winner = careers.careerOf(1795);
        assertEq(winner.matches, 1);
        assertEq(winner.wins, 1);
        assertEq(winner.streak, 1);
        assertEq(winner.bestStreak, 1);
        assertEq(winner.rentedMatches, 1);
        assertEq(winner.guestMatches, 0);
        assertEq(winner.lastPlayedAt, block.timestamp);

        ChogCareers.Career memory loser = careers.careerOf(900);
        assertEq(loser.matches, 1);
        assertEq(loser.wins, 0);
        assertEq(loser.streak, 0);
        assertEq(loser.guestMatches, 1);
        assertEq(careers.careerOf(1969).guestMatches, 1);
        assertEq(careers.careerOf(2).rentedMatches, 1);
        assertEq(careers.careerOf(7).rentedMatches + careers.careerOf(7).guestMatches, 0);
    }

    function test_StreaksAcrossWinsLossesAndDraws() public {
        play(0, solo(1795)); // win
        play(0, solo(1795)); // win
        play(0, solo(1795)); // win
        ChogCareers.Career memory career = careers.careerOf(1795);
        assertEq(career.streak, 3);
        assertEq(career.bestStreak, 3);

        play(1, solo(1795)); // loss
        career = careers.careerOf(1795);
        assertEq(career.streak, 0);
        assertEq(career.bestStreak, 3);

        play(0, solo(1795)); // win
        play(DRAW, solo(1795)); // a draw ends the run too
        career = careers.careerOf(1795);
        assertEq(career.streak, 0);
        assertEq(career.draws, 1);
        assertEq(career.bestStreak, 3);

        play(0, solo(1795));
        play(0, solo(1795));
        play(0, solo(1795));
        play(0, solo(1795));
        career = careers.careerOf(1795);
        assertEq(career.matches, 10);
        assertEq(career.wins, 8);
        assertEq(career.draws, 1);
        assertEq(career.streak, 4);
        assertEq(career.bestStreak, 4);
    }

    function test_ADrawIsNeitherAWinNorALossForAnyone() public {
        play(DRAW, threeAgainstThree());
        for (uint256 tokenId = 1; tokenId <= 1969; tokenId += 1968) {
            ChogCareers.Career memory career = careers.careerOf(tokenId);
            assertEq(career.matches, 1);
            assertEq(career.wins, 0);
            assertEq(career.draws, 1);
        }
    }

    function test_RentedAndGuestMatchesAreCountedApart() public {
        ChogCareers.Entry[] memory entries = solo(1795);
        play(0, entries);
        entries[0].playedBy = RENTER;
        play(1, entries);
        play(0, entries);
        entries[0].playedBy = GUEST;
        play(0, entries);
        ChogCareers.Career memory career = careers.careerOf(1795);
        assertEq(career.matches, 4);
        assertEq(career.wins, 3);
        assertEq(career.rentedMatches, 2);
        assertEq(career.guestMatches, 1);
    }

    function test_TokenIdsOutsideTheCollectionAreRefused() public {
        vm.startPrank(referee);
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadToken.selector, 0));
        careers.record(MATCH, 0, solo(0));
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadToken.selector, 1970));
        careers.record(MATCH, 0, solo(1970));
        careers.record(MATCH, 0, solo(1969));
        vm.stopPrank();

        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadToken.selector, 0));
        careers.careerOf(0);
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadToken.selector, 1970));
        careers.careerOf(1970);
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadToken.selector, 1969));
        careers.packedCareers(1969, 2);
    }

    function test_AGenesisPlaysAMatchOnce() public {
        ChogCareers.Entry[] memory twice = new ChogCareers.Entry[](2);
        twice[0] = ChogCareers.Entry(1795, 0, HOLDER);
        twice[1] = ChogCareers.Entry(1795, 1, RENTER);
        vm.prank(referee);
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.TokensNotAscending.selector, 1795));
        careers.record(MATCH, 0, twice);

        // Out of order is refused too: the order is what makes a duplicate impossible.
        twice[1].tokenId = 7;
        vm.prank(referee);
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.TokensNotAscending.selector, 7));
        careers.record(MATCH, 0, twice);

        // Nothing of a refused match was kept, its key included.
        assertEq(careers.careerOf(1795).matches, 0);
        (bool recorded,) = careers.resultOf(MATCH);
        assertFalse(recorded);
    }

    function test_BadResultsAndEntriesAreRefused() public {
        vm.startPrank(referee);
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadTeam.selector, 3));
        careers.record(MATCH, 3, solo(1795));

        ChogCareers.Entry[] memory entries = solo(1795);
        entries[0].team = 2;
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadTeam.selector, 2));
        careers.record(MATCH, 0, entries);

        entries[0].team = 0;
        entries[0].playedBy = 3;
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadPlayedBy.selector, 3));
        careers.record(MATCH, 0, entries);

        vm.expectRevert(ChogCareers.NoEntries.selector);
        careers.record(MATCH, 0, new ChogCareers.Entry[](0));

        ChogCareers.Entry[] memory crowd = new ChogCareers.Entry[](17);
        for (uint256 index; index < 17; ++index) {
            crowd[index] = ChogCareers.Entry(uint16(index + 1), uint8(index % 2), HOLDER);
        }
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.TooManyEntries.selector, 17));
        careers.record(MATCH, 0, crowd);
        vm.stopPrank();
    }

    function test_EventsLetTheHistoryBeRebuilt() public {
        vm.expectEmit(true, true, false, true, address(careers));
        emit Played(MATCH, 1, 1, HOLDER, 0);
        vm.expectEmit(true, true, false, true, address(careers));
        emit Played(MATCH, 2, 1, RENTER, 0);
        vm.expectEmit(true, true, false, true, address(careers));
        emit Played(MATCH, 7, 0, HOLDER, 1);
        vm.expectEmit(true, true, false, true, address(careers));
        emit Played(MATCH, 900, 1, GUEST, 0);
        vm.expectEmit(true, true, false, true, address(careers));
        emit Played(MATCH, 1795, 0, RENTER, 1);
        vm.expectEmit(true, true, false, true, address(careers));
        emit Played(MATCH, 1969, 0, GUEST, 1);
        vm.expectEmit(true, false, false, true, address(careers));
        emit MatchRecorded(MATCH, 0, 6);
        vm.prank(referee);
        careers.record(MATCH, 0, threeAgainstThree());

        bytes32 draw = keccak256("eu:quick-play-0002");
        vm.expectEmit(true, true, false, true, address(careers));
        emit Played(draw, 1795, 0, HOLDER, 2);
        vm.prank(referee);
        careers.record(draw, DRAW, solo(1795));
    }

    function test_CareersOfEveryGenesisInOneCall() public {
        play(0, threeAgainstThree());
        play(1, threeAgainstThree());

        uint256[] memory all = new uint256[](1969);
        for (uint256 index; index < 1969; ++index) {
            all[index] = index + 1;
        }
        ChogCareers.Career[] memory read = careers.careersOf(all);
        assertEq(read.length, 1969);
        assertEq(read[1794].matches, 2);
        assertEq(read[1794].wins, 1);
        assertEq(read[899].wins, 1);
        assertEq(read[1968].rentedMatches, 0);
        assertEq(read[1968].guestMatches, 2);
        assertEq(read[99].matches, 0);

        uint256 words = gasleft();
        uint256[] memory packed = careers.packedCareers(1, 1969);
        words = words - gasleft();
        emit log_named_uint("gas of packedCareers(1, 1969) inside a call", words);
        assertEq(packed.length, 1969);
        for (uint256 index; index < 1969; ++index) {
            ChogCareers.Career memory career = read[index];
            uint256 word = packed[index];
            assertEq(uint32(word), career.matches);
            assertEq(uint32(word >> 32), career.wins);
            assertEq(uint32(word >> 64), career.draws);
            assertEq(uint32(word >> 96), career.rentedMatches);
            assertEq(uint32(word >> 128), career.guestMatches);
            assertEq(uint16(word >> 160), career.streak);
            assertEq(uint16(word >> 176), career.bestStreak);
            assertEq(uint64(word >> 192), career.lastPlayedAt);
        }

        uint256[] memory badList = new uint256[](1);
        vm.expectRevert(abi.encodeWithSelector(ChogCareers.BadToken.selector, 0));
        careers.careersOf(badList);
    }

    function test_OnlyTheOwnerReplacesTheReferee() public {
        address next = makeAddr("next referee");
        vm.expectRevert(ChogCareers.NotOwner.selector);
        careers.setReferee(next);

        vm.expectEmit(true, false, false, false, address(careers));
        emit RefereeChanged(next);
        vm.prank(admin);
        careers.setReferee(next);
        assertEq(careers.referee(), next);

        vm.prank(referee);
        vm.expectRevert(ChogCareers.NotReferee.selector);
        careers.record(MATCH, 0, solo(1795));
        vm.prank(next);
        careers.record(MATCH, 0, solo(1795));

        vm.prank(admin);
        vm.expectRevert(ChogCareers.ZeroAddress.selector);
        careers.setReferee(address(0));
    }

    function test_OwnershipIsHandedOver() public {
        address heir = makeAddr("heir");
        vm.expectRevert(ChogCareers.NotOwner.selector);
        careers.transferOwnership(heir);

        vm.expectEmit(true, true, false, false, address(careers));
        emit OwnerChanged(admin, heir);
        vm.prank(admin);
        careers.transferOwnership(heir);
        assertEq(careers.owner(), heir);

        vm.prank(admin);
        vm.expectRevert(ChogCareers.NotOwner.selector);
        careers.setReferee(heir);
        vm.prank(heir);
        careers.setReferee(heir);
        assertEq(careers.referee(), heir);
    }

    function test_TheConstructorRefusesZeroAddresses() public {
        vm.expectRevert(ChogCareers.ZeroAddress.selector);
        new ChogCareers(address(0), admin);
        vm.expectRevert(ChogCareers.ZeroAddress.selector);
        new ChogCareers(referee, address(0));
    }

    // Gas of record for a match of 1, 3 and 6 Genesis that never played (the first write of each
    // career) and that already played (the counters are only changed), for the gas report.
    function test_GasOfRecord() public {
        ChogCareers.Entry[] memory three = new ChogCareers.Entry[](3);
        three[0] = ChogCareers.Entry(10, 0, HOLDER);
        three[1] = ChogCareers.Entry(500, 1, RENTER);
        three[2] = ChogCareers.Entry(1500, 1, HOLDER);
        ChogCareers.Entry[][3] memory matches = [solo(1795), three, threeAgainstThree()];
        for (uint256 size; size < 3; ++size) {
            for (uint256 round; round < 2; ++round) {
                vm.warp(block.timestamp + 1 minutes);
                bytes32 key = keccak256(abi.encode("gas", size, round));
                vm.prank(referee);
                uint256 before = gasleft();
                careers.record(key, 0, matches[size]);
                uint256 used = before - gasleft();
                emit log_named_uint(
                    string.concat(
                        "record of ", vm.toString(matches[size].length), round == 0 ? " fresh careers" : " played careers"
                    ),
                    used
                );
            }
        }
    }

    function testFuzz_CountersStayConsistent(uint8[24] memory results) public {
        uint256 wins;
        uint256 draws;
        uint256 run;
        uint256 best;
        for (uint256 index; index < results.length; ++index) {
            uint8 winnerTeam = results[index] % 3;
            play(winnerTeam, solo(1795));
            if (winnerTeam == 0) {
                ++wins;
                ++run;
                if (run > best) best = run;
            } else {
                if (winnerTeam == DRAW) ++draws;
                run = 0;
            }
        }
        ChogCareers.Career memory career = careers.careerOf(1795);
        assertEq(career.matches, results.length);
        assertEq(career.wins, wins);
        assertEq(career.draws, draws);
        assertEq(career.streak, run);
        assertEq(career.bestStreak, best);
    }
}
