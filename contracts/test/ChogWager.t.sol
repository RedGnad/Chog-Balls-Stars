// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Test} from "forge-std/Test.sol";
import {ChogWager} from "../src/ChogWager.sol";
import {TokenStandIn} from "./ChogShop.t.sol";
import {SilentToken} from "./ChogRental.t.sol";

/// The stand-in for $CHOG with the plain transfer a contract needs to pay out of its own balance.
contract WagerToken is TokenStandIn {
    function transfer(address to, uint256 amount) external returns (bool) {
        balanceOf[msg.sender] -= amount;
        balanceOf[to] += amount;
        return true;
    }
}

/// The token that answers nothing, with a transfer that answers nothing either.
contract SilentWagerToken is SilentToken {
    function transfer(address to, uint256 amount) external {
        balanceOf[msg.sender] -= amount;
        balanceOf[to] += amount;
    }
}

contract ChogWagerTest is Test {
    uint256 private constant REFEREE_KEY = 0xA11CE;
    uint256 private constant OTHER_KEY = 0xBAD;
    uint256 private constant STAKE = 100 ether;
    uint256 private constant CURVE_ORDER = 0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141;
    bytes32 private constant MATCH = keccak256("eu:quick-play-0001");
    bytes32 private constant OTHER_MATCH = keccak256("eu:quick-play-0002");

    WagerToken private chog;
    ChogWager private wager;
    address private admin = makeAddr("admin");
    address private referee;
    address private ana = makeAddr("ana");
    address private bo = makeAddr("bo");
    address private cy = makeAddr("cy");
    address private dee = makeAddr("dee");
    uint256 private deadline;

    event Staked(bytes32 indexed matchKey, address indexed player, uint8 team, uint256 amount);
    event Settled(bytes32 indexed matchKey, uint8 winnerTeam, uint256 paidToEachWinner, uint256 burned);
    event Refunded(bytes32 indexed matchKey);
    event RefereeChanged(address indexed referee);

    function setUp() public {
        // A realistic clock: forge starts at second 1.
        vm.warp(1_790_000_000);
        deadline = block.timestamp + 2 minutes;
        referee = vm.addr(REFEREE_KEY);
        chog = new WagerToken();
        // A tenth of what the losers staked is burned.
        wager = new ChogWager(address(chog), referee, 1000, admin);
        address[4] memory players = [ana, bo, cy, dee];
        for (uint256 index; index < players.length; ++index) {
            chog.mint(players[index], 1000 ether);
            vm.prank(players[index]);
            chog.approve(address(wager), type(uint256).max);
        }
    }

    // A digest signed the way a wallet signs a message, as r, s, v.
    function _sign(uint256 key, bytes32 digest) private pure returns (bytes memory) {
        (uint8 v, bytes32 r, bytes32 s) =
            vm.sign(key, keccak256(abi.encodePacked("\x19Ethereum Signed Message:\n32", digest)));
        return abi.encodePacked(r, s, v);
    }

    function _ticketFrom(uint256 key, ChogWager on, bytes32 matchKey, address player, uint8 team, uint256 amount, uint256 until)
        private
        view
        returns (bytes memory)
    {
        return _sign(key, keccak256(abi.encode(on.TICKET(), block.chainid, address(on), matchKey, player, team, amount, until)));
    }

    function _ticket(bytes32 matchKey, address player, uint8 team, uint256 amount) private view returns (bytes memory) {
        return _ticketFrom(REFEREE_KEY, wager, matchKey, player, team, amount, deadline);
    }

    function _resultFrom(uint256 key, ChogWager on, bytes32 matchKey, uint8 winnerTeam) private view returns (bytes memory) {
        return _sign(key, keccak256(abi.encode(on.RESULT(), block.chainid, address(on), matchKey, winnerTeam)));
    }

    function _result(bytes32 matchKey, uint8 winnerTeam) private view returns (bytes memory) {
        return _resultFrom(REFEREE_KEY, wager, matchKey, winnerTeam);
    }

    function _voidFrom(uint256 key, ChogWager on, bytes32 matchKey) private view returns (bytes memory) {
        return _sign(key, keccak256(abi.encode(on.VOID(), block.chainid, address(on), matchKey)));
    }

    function _stake(bytes32 matchKey, address player, uint8 team, uint256 amount) private {
        bytes memory ticket = _ticket(matchKey, player, team, amount);
        vm.prank(player);
        wager.stake(matchKey, team, amount, deadline, ticket);
    }

    // Ana and Bo on team 0, Cy and Dee on team 1.
    function _twoAgainstTwo() private {
        _stake(MATCH, ana, 0, STAKE);
        _stake(MATCH, cy, 1, STAKE);
        _stake(MATCH, bo, 0, STAKE);
        _stake(MATCH, dee, 1, STAKE);
    }

    function _state(bytes32 matchKey) private view returns (uint8 state) {
        (state,,,,) = wager.matchOf(matchKey);
    }

    function _split(bytes memory signature) private pure returns (bytes32 r, bytes32 s, uint8 v) {
        assembly ("memory-safe") {
            r := mload(add(signature, 32))
            s := mload(add(signature, 64))
            v := byte(0, mload(add(signature, 96)))
        }
    }

    function test_TwoTeamsStakeAndTheWinnersShareTheLosersStakesLessTheBurnedShare() public {
        bytes memory first = _ticket(MATCH, ana, 0, STAKE);
        vm.expectEmit();
        emit Staked(MATCH, ana, 0, STAKE);
        vm.prank(ana);
        wager.stake(MATCH, 0, STAKE, deadline, first);
        _stake(MATCH, cy, 1, STAKE);
        _stake(MATCH, bo, 0, STAKE);
        _stake(MATCH, dee, 1, STAKE);

        (uint8 state, uint256 stakeAmount, uint64 firstStakeAt, uint8 team0, uint8 team1) = wager.matchOf(MATCH);
        assertEq(state, 1, "open");
        assertEq(stakeAmount, STAKE);
        assertEq(firstStakeAt, block.timestamp);
        assertEq(team0, 2);
        assertEq(team1, 2);
        assertEq(chog.balanceOf(address(wager)), 400 ether);
        assertEq(chog.balanceOf(ana), 900 ether);

        // Team 0 wins: 200 were lost, 20 are burned, 180 are shared by the two winners.
        bytes memory result = _result(MATCH, 0);
        vm.expectEmit();
        emit Settled(MATCH, 0, 190 ether, 20 ether);
        vm.prank(makeAddr("anyone"));
        wager.settle(MATCH, 0, result);

        assertEq(_state(MATCH), 2, "settled");
        assertEq(chog.balanceOf(ana), 1090 ether);
        assertEq(chog.balanceOf(bo), 1090 ether);
        assertEq(chog.balanceOf(cy), 900 ether);
        assertEq(chog.balanceOf(dee), 900 ether);
        assertEq(chog.totalSupply(), 3980 ether, "a tenth of the lost stakes was burned");
        assertEq(chog.balanceOf(address(wager)), 0, "the contract keeps nothing");
    }

    function test_OneWinnerTakesWhatTwoLosersStaked() public {
        _stake(MATCH, ana, 0, STAKE);
        _stake(MATCH, cy, 1, STAKE);
        _stake(MATCH, dee, 1, STAKE);
        wager.settle(MATCH, 0, _result(MATCH, 0));
        assertEq(chog.balanceOf(ana), 1180 ether, "her stake back and 180 of the 200 lost");
        assertEq(chog.totalSupply(), 3980 ether);
        assertEq(chog.balanceOf(address(wager)), 0);
    }

    function test_WhatCannotBeSharedEquallyIsBurnedToo() public {
        // Three winners and one loser who staked 101 units: 10 are burned, 91 are left for three.
        _stake(MATCH, ana, 0, 101);
        _stake(MATCH, bo, 0, 101);
        _stake(MATCH, cy, 0, 101);
        _stake(MATCH, dee, 1, 101);
        uint256 supply = chog.totalSupply();

        bytes memory result = _result(MATCH, 0);
        vm.expectEmit();
        emit Settled(MATCH, 0, 131, 11);
        wager.settle(MATCH, 0, result);
        assertEq(chog.balanceOf(ana), 1000 ether + 30);
        assertEq(chog.balanceOf(dee), 1000 ether - 101);
        assertEq(supply - chog.totalSupply(), 11, "the tenth, and the unit three winners cannot share");
        assertEq(chog.balanceOf(address(wager)), 0);
    }

    function test_ATicketIsOnlyGoodForItsPlayerItsTeamItsAmountItsMatchAndItsTime() public {
        bytes memory ticket = _ticket(MATCH, ana, 0, STAKE);

        vm.prank(bo);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 0, STAKE, deadline, ticket);

        vm.startPrank(ana);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 1, STAKE, deadline, ticket);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 0, STAKE + 1, deadline, ticket);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(OTHER_MATCH, 0, STAKE, deadline, ticket);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 0, STAKE, deadline + 1 hours, ticket);

        // Signed by someone who is not the referee.
        bytes memory forged = _ticketFrom(OTHER_KEY, wager, MATCH, ana, 0, STAKE, deadline);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 0, STAKE, deadline, forged);

        // Good until its deadline, to the second, and not after.
        vm.warp(deadline + 1);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.TicketExpired.selector, deadline));
        wager.stake(MATCH, 0, STAKE, deadline, ticket);
        vm.warp(deadline);
        wager.stake(MATCH, 0, STAKE, deadline, ticket);
        vm.stopPrank();

        assertEq(chog.balanceOf(address(wager)), STAKE);
        (bool staked, uint8 team) = wager.stakeOf(MATCH, ana);
        assertTrue(staked);
        assertEq(team, 0);
    }

    function test_ATeamIsZeroOrOneAndAStakeIsNeverNothing() public {
        bytes memory ticket = _ticket(MATCH, ana, 2, STAKE);
        vm.prank(ana);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.BadTeam.selector, 2));
        wager.stake(MATCH, 2, STAKE, deadline, ticket);

        ticket = _ticket(MATCH, ana, 0, 0);
        vm.prank(ana);
        vm.expectRevert(ChogWager.AmountIsZero.selector);
        wager.stake(MATCH, 0, 0, deadline, ticket);
    }

    function test_ASignatureThatIsCutOrOfTheOtherHalfOfTheCurveIsRefused() public {
        bytes memory ticket = _ticket(MATCH, ana, 0, STAKE);
        (bytes32 r, bytes32 s, uint8 v) = _split(ticket);

        vm.startPrank(ana);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 0, STAKE, deadline, abi.encodePacked(r, s));
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 0, STAKE, deadline, abi.encodePacked(r, s, uint8(v - 27)));
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 0, STAKE, deadline, "");

        // The twin of a valid signature: same message, same signer for ecrecover, refused here.
        bytes memory twin = abi.encodePacked(r, bytes32(CURVE_ORDER - uint256(s)), uint8(v == 27 ? 28 : 27));
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(MATCH, 0, STAKE, deadline, twin);

        wager.stake(MATCH, 0, STAKE, deadline, ticket);
        vm.stopPrank();
    }

    function test_EveryStakeOfAMatchIsTheAmountOfTheFirstOne() public {
        _stake(MATCH, ana, 0, STAKE);
        bytes memory ticket = _ticket(MATCH, cy, 1, STAKE / 2);
        vm.prank(cy);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotTheStakeOfTheMatch.selector, STAKE));
        wager.stake(MATCH, 1, STAKE / 2, deadline, ticket);

        // Another match is free to have another stake.
        _stake(OTHER_MATCH, cy, 1, STAKE / 2);
        (, uint256 stakeAmount,,,) = wager.matchOf(OTHER_MATCH);
        assertEq(stakeAmount, STAKE / 2);
    }

    function test_APlayerStakesOnceAMatchOnOneTeam() public {
        _stake(MATCH, ana, 0, STAKE);
        bytes memory again = _ticket(MATCH, ana, 0, STAKE);
        bytes memory otherSide = _ticket(MATCH, ana, 1, STAKE);
        vm.startPrank(ana);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.AlreadyStaked.selector, MATCH));
        wager.stake(MATCH, 0, STAKE, deadline, again);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.AlreadyStaked.selector, MATCH));
        wager.stake(MATCH, 1, STAKE, deadline, otherSide);
        vm.stopPrank();
        assertEq(chog.balanceOf(ana), 900 ether);
    }

    function test_AMatchTakesSixteenStakesAndNoMore() public {
        for (uint256 index; index < 17; ++index) {
            address player = address(uint160(0xA000 + index));
            uint8 team = uint8(index % 2);
            chog.mint(player, STAKE);
            bytes memory ticket = _ticket(MATCH, player, team, STAKE);
            vm.startPrank(player);
            chog.approve(address(wager), STAKE);
            if (index == 16) vm.expectRevert(abi.encodeWithSelector(ChogWager.MatchIsFull.selector, MATCH));
            wager.stake(MATCH, team, STAKE, deadline, ticket);
            vm.stopPrank();
        }
        (,,, uint8 team0, uint8 team1) = wager.matchOf(MATCH);
        assertEq(team0, 8);
        assertEq(team1, 8);

        // Sixteen payments in one settlement go through.
        wager.settle(MATCH, 1, _result(MATCH, 1));
        assertEq(chog.balanceOf(address(wager)), 0);
        assertEq(chog.balanceOf(address(uint160(0xA001))), 190 ether);
    }

    function test_ADrawGivesEveryStakeBack() public {
        _twoAgainstTwo();
        uint256 supply = chog.totalSupply();
        bytes memory draw = _result(MATCH, 2);
        vm.expectEmit();
        emit Refunded(MATCH);
        wager.settle(MATCH, 2, draw);

        assertEq(_state(MATCH), 3, "refunded");
        assertEq(chog.balanceOf(ana), 1000 ether);
        assertEq(chog.balanceOf(dee), 1000 ether);
        assertEq(chog.totalSupply(), supply, "nothing is burned");
        assertEq(chog.balanceOf(address(wager)), 0);
    }

    function test_ASideWithoutAStakerGivesEveryStakeBack() public {
        // Only team 0 staked: whoever wins, there is nothing to win.
        _stake(MATCH, ana, 0, STAKE);
        _stake(MATCH, bo, 0, STAKE);
        wager.settle(MATCH, 0, _result(MATCH, 0));
        assertEq(_state(MATCH), 3);
        assertEq(chog.balanceOf(ana), 1000 ether);

        _stake(OTHER_MATCH, ana, 0, STAKE);
        _stake(OTHER_MATCH, bo, 0, STAKE);
        wager.settle(OTHER_MATCH, 1, _result(OTHER_MATCH, 1));
        assertEq(_state(OTHER_MATCH), 3);
        assertEq(chog.balanceOf(bo), 1000 ether);
        assertEq(chog.totalSupply(), 4000 ether);
    }

    function test_AResultSignedBySomeoneElseOrForAnotherWinnerIsRefused() public {
        _twoAgainstTwo();
        bytes memory forged = _resultFrom(OTHER_KEY, wager, MATCH, 1);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.settle(MATCH, 1, forged);

        // The referee said team 0: the same signature does not make team 1 win, nor a draw.
        bytes memory result = _result(MATCH, 0);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.settle(MATCH, 1, result);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.settle(MATCH, 2, result);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.BadTeam.selector, 3));
        wager.settle(MATCH, 3, result);

        // A ticket is not a result, even from the referee.
        bytes memory ticket = _ticket(MATCH, ana, 0, STAKE);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.settle(MATCH, 0, ticket);
        assertEq(chog.balanceOf(address(wager)), 400 ether);
    }

    function test_AResultIsUsedOnceOnItsOwnMatchAndItsOwnContract() public {
        _twoAgainstTwo();
        _stake(OTHER_MATCH, ana, 0, STAKE);
        _stake(OTHER_MATCH, cy, 1, STAKE);
        bytes memory result = _result(MATCH, 0);

        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.settle(OTHER_MATCH, 0, result);

        wager.settle(MATCH, 0, result);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotOpen.selector, MATCH));
        wager.settle(MATCH, 0, result);

        // The same referee, the same match name, another contract: its own signatures only.
        ChogWager other = new ChogWager(address(chog), referee, 1000, admin);
        bytes memory ticket = _ticketFrom(REFEREE_KEY, other, MATCH, ana, 0, STAKE, deadline);
        bytes memory otherTicket = _ticketFrom(REFEREE_KEY, other, MATCH, cy, 1, STAKE, deadline);
        bytes memory ticketOfTheFirst = _ticket(MATCH, ana, 0, STAKE);
        bytes memory otherResult = _resultFrom(REFEREE_KEY, other, MATCH, 0);
        vm.startPrank(ana);
        chog.approve(address(other), STAKE);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        other.stake(MATCH, 0, STAKE, deadline, ticketOfTheFirst);
        other.stake(MATCH, 0, STAKE, deadline, ticket);
        vm.stopPrank();
        vm.startPrank(cy);
        chog.approve(address(other), STAKE);
        other.stake(MATCH, 1, STAKE, deadline, otherTicket);
        vm.stopPrank();
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        other.settle(MATCH, 0, result);
        other.settle(MATCH, 0, otherResult);
        assertEq(chog.balanceOf(address(other)), 0);
    }

    function test_NothingIsStakedOnAMatchThatIsOverAndNothingIsSettledBeforeAStake() public {
        bytes memory early = _result(MATCH, 0);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotOpen.selector, MATCH));
        wager.settle(MATCH, 0, early);

        _stake(MATCH, ana, 0, STAKE);
        _stake(MATCH, cy, 1, STAKE);
        wager.settle(MATCH, 1, _result(MATCH, 1));

        bytes memory late = _ticket(MATCH, bo, 1, STAKE);
        vm.prank(bo);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.MatchClosed.selector, MATCH));
        wager.stake(MATCH, 1, STAKE, deadline, late);
        assertEq(chog.balanceOf(bo), 1000 ether);
    }

    function test_TheRefereeCanCallAMatchOff() public {
        _twoAgainstTwo();
        _stake(OTHER_MATCH, ana, 0, STAKE);

        bytes memory forged = _voidFrom(OTHER_KEY, wager, MATCH);
        bytes memory called = _voidFrom(REFEREE_KEY, wager, MATCH);
        bytes memory result = _result(MATCH, 0);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.voidMatch(MATCH, forged);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.voidMatch(OTHER_MATCH, called);
        // A result is not a way to call a match off, nor the other way round.
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.voidMatch(MATCH, result);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.settle(MATCH, 0, called);

        vm.expectEmit();
        emit Refunded(MATCH);
        vm.prank(makeAddr("anyone"));
        wager.voidMatch(MATCH, called);
        assertEq(_state(MATCH), 3);
        assertEq(chog.balanceOf(cy), 1000 ether);
        assertEq(chog.balanceOf(address(wager)), STAKE, "the other match is untouched");

        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotOpen.selector, MATCH));
        wager.voidMatch(MATCH, called);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotOpen.selector, MATCH));
        wager.settle(MATCH, 0, result);
    }

    function test_AfterAnHourWithoutAResultAnyoneGetsTheStakesBack() public {
        uint256 start = block.timestamp;
        bytes memory result = _result(MATCH, 0);
        _stake(MATCH, ana, 0, STAKE);
        vm.warp(start + 1 minutes);
        _stake(MATCH, cy, 1, STAKE);

        // The hour counts from the first stake.
        vm.warp(start + 1 hours - 1);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.TooEarly.selector, start + 1 hours));
        wager.refundExpired(MATCH);

        vm.warp(start + 1 hours);
        vm.expectEmit();
        emit Refunded(MATCH);
        vm.prank(makeAddr("anyone"));
        wager.refundExpired(MATCH);
        assertEq(_state(MATCH), 3);
        assertEq(chog.balanceOf(ana), 1000 ether);
        assertEq(chog.balanceOf(cy), 1000 ether);
        assertEq(chog.balanceOf(address(wager)), 0);

        // A result that comes afterwards pays nothing, and stakes are given back once.
        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotOpen.selector, MATCH));
        wager.settle(MATCH, 0, result);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotOpen.selector, MATCH));
        wager.refundExpired(MATCH);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotOpen.selector, OTHER_MATCH));
        wager.refundExpired(OTHER_MATCH);
    }

    function test_AChangedRefereeMakesOldSignaturesWorthlessAlsoForOpenMatches() public {
        _twoAgainstTwo();
        bytes memory oldResult = _result(MATCH, 0);
        bytes memory oldTicket = _ticket(OTHER_MATCH, ana, 0, STAKE);

        vm.expectEmit();
        emit RefereeChanged(vm.addr(OTHER_KEY));
        vm.prank(admin);
        wager.setReferee(vm.addr(OTHER_KEY));

        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.settle(MATCH, 0, oldResult);
        vm.prank(ana);
        vm.expectRevert(ChogWager.NotSignedByTheReferee.selector);
        wager.stake(OTHER_MATCH, 0, STAKE, deadline, oldTicket);

        // The new referee tells the result of the match that was open.
        wager.settle(MATCH, 1, _resultFrom(OTHER_KEY, wager, MATCH, 1));
        assertEq(chog.balanceOf(cy), 1090 ether);
    }

    function test_OnlyTheOwnerChangesTheRefereeAndHandsTheContractOver() public {
        vm.expectRevert(ChogWager.NotOwner.selector);
        wager.setReferee(address(this));
        vm.expectRevert(ChogWager.NotOwner.selector);
        wager.transferOwnership(address(this));
        vm.prank(referee);
        vm.expectRevert(ChogWager.NotOwner.selector);
        wager.setReferee(referee);

        vm.startPrank(admin);
        vm.expectRevert(ChogWager.ZeroAddress.selector);
        wager.setReferee(address(0));
        vm.expectRevert(ChogWager.ZeroAddress.selector);
        wager.transferOwnership(address(0));
        wager.transferOwnership(address(this));
        vm.stopPrank();

        assertEq(wager.owner(), address(this));
        wager.setReferee(bo);
        assertEq(wager.referee(), bo);
        vm.prank(admin);
        vm.expectRevert(ChogWager.NotOwner.selector);
        wager.setReferee(admin);
    }

    function test_TheWagerCannotBeDeployedHalfConfigured() public {
        vm.expectRevert(ChogWager.ZeroAddress.selector);
        new ChogWager(address(0), referee, 0, admin);
        vm.expectRevert(ChogWager.ZeroAddress.selector);
        new ChogWager(address(chog), address(0), 0, admin);
        vm.expectRevert(ChogWager.ZeroAddress.selector);
        new ChogWager(address(chog), referee, 0, address(0));
        vm.expectRevert(ChogWager.ShareTooLarge.selector);
        new ChogWager(address(chog), referee, 10_001, admin);
        // An address without code would let stakes "succeed" without a token moving.
        vm.expectRevert(ChogWager.NotAToken.selector);
        new ChogWager(makeAddr("not a token"), referee, 0, admin);
    }

    function test_WithoutApprovalOrFundsNothingIsStaked() public {
        address broke = makeAddr("broke");
        bytes memory ticket = _ticket(MATCH, broke, 0, STAKE);
        vm.startPrank(broke);
        vm.expectRevert(ChogWager.PaymentFailed.selector);
        wager.stake(MATCH, 0, STAKE, deadline, ticket);
        chog.approve(address(wager), STAKE);
        vm.expectRevert(ChogWager.PaymentFailed.selector);
        wager.stake(MATCH, 0, STAKE, deadline, ticket);
        vm.stopPrank();

        assertEq(_state(MATCH), 0, "a stake that was not paid opens nothing");
        (bool staked,) = wager.stakeOf(MATCH, broke);
        assertFalse(staked);
    }

    function test_ATokenThatAnswersNothingIsStakedPaidOutAndGivenBack() public {
        SilentWagerToken silent = new SilentWagerToken();
        ChogWager other = new ChogWager(address(silent), referee, 1000, admin);
        address[2] memory players = [ana, cy];
        for (uint8 index; index < 2; ++index) {
            silent.mint(players[index], 2 * STAKE);
            bytes memory first = _ticketFrom(REFEREE_KEY, other, MATCH, players[index], index, STAKE, deadline);
            bytes memory second = _ticketFrom(REFEREE_KEY, other, OTHER_MATCH, players[index], index, STAKE, deadline);
            vm.startPrank(players[index]);
            silent.approve(address(other), 2 * STAKE);
            other.stake(MATCH, index, STAKE, deadline, first);
            other.stake(OTHER_MATCH, index, STAKE, deadline, second);
            vm.stopPrank();
        }

        other.settle(MATCH, 0, _resultFrom(REFEREE_KEY, other, MATCH, 0));
        assertEq(silent.balanceOf(ana), 190 ether, "her stake and 90 of the 100 lost; her other stake is still held");
        other.voidMatch(OTHER_MATCH, _voidFrom(REFEREE_KEY, other, OTHER_MATCH));
        assertEq(silent.balanceOf(ana), 290 ether);
        assertEq(silent.balanceOf(cy), STAKE);
        assertEq(silent.balanceOf(address(other)), 0);
    }

    function test_OneReadListsWhoStakedOnWhat() public {
        (address[] memory players, uint8[] memory teams) = wager.stakersOf(MATCH);
        assertEq(players.length, 0);
        assertEq(teams.length, 0);

        _stake(MATCH, cy, 1, STAKE);
        _stake(MATCH, ana, 0, STAKE);
        _stake(MATCH, dee, 1, STAKE);
        (players, teams) = wager.stakersOf(MATCH);
        assertEq(players.length, 3);
        assertEq(players[0], cy);
        assertEq(teams[0], 1);
        assertEq(players[1], ana);
        assertEq(teams[1], 0);
        assertEq(players[2], dee);
        assertEq(teams[2], 1);

        (bool staked, uint8 team) = wager.stakeOf(MATCH, dee);
        assertTrue(staked);
        assertEq(team, 1);
        (staked, team) = wager.stakeOf(MATCH, bo);
        assertFalse(staked);
        assertEq(team, 0);
        (staked,) = wager.stakeOf(OTHER_MATCH, dee);
        assertFalse(staked);

        // The list stays readable once the match is paid.
        wager.settle(MATCH, 1, _result(MATCH, 1));
        (players,) = wager.stakersOf(MATCH);
        assertEq(players.length, 3);
    }

    /// Whatever the stake, the number of stakers on each team and the burned share, once the
    /// result is in the contract holds nothing and every unit paid in is with a winner or burned.
    function testFuzz_AfterAResultEveryUnitIsWithAWinnerOrBurned(
        uint256 stakeAmount,
        uint8 team0,
        uint8 team1,
        uint16 share,
        bool teamOneWins
    ) public {
        stakeAmount = bound(stakeAmount, 1, 1e30);
        team0 = uint8(bound(team0, 1, 8));
        team1 = uint8(bound(team1, 1, 8));
        share = uint16(bound(share, 0, 10_000));
        ChogWager other = new ChogWager(address(chog), referee, share, admin);
        uint256 count = uint256(team0) + team1;
        for (uint256 index; index < count; ++index) {
            _stakeOn(other, address(uint160(0xB000 + index)), index < team0 ? 0 : 1, stakeAmount);
        }
        assertEq(chog.balanceOf(address(other)), count * stakeAmount);

        uint256 supplyBefore = chog.totalSupply();
        uint8 winnerTeam = teamOneWins ? 1 : 0;
        other.settle(MATCH, winnerTeam, _resultFrom(REFEREE_KEY, other, MATCH, winnerTeam));

        uint256 winners = teamOneWins ? team1 : team0;
        uint256 lost = (count - winners) * stakeAmount;
        uint256 burned = supplyBefore - chog.totalSupply();
        uint256 toEachWinner = stakeAmount + (lost - lost * share / 10_000) / winners;
        for (uint256 index; index < count; ++index) {
            bool won = (index < team0) != teamOneWins;
            assertEq(chog.balanceOf(address(uint160(0xB000 + index))), won ? toEachWinner : 0, "winners are paid the same, losers nothing");
        }
        assertEq(chog.balanceOf(address(other)), 0, "the contract holds nothing");
        assertEq(winners * toEachWinner + burned, count * stakeAmount, "paid in is with a winner or burned");
        assertGe(burned, lost * share / 10_000, "at least the burned share");
        assertLt(burned - lost * share / 10_000, winners, "plus less than one unit a winner");
    }

    // A new player is given the stake and puts it on a team of MATCH, on any wager contract.
    function _stakeOn(ChogWager on, address player, uint8 team, uint256 amount) private {
        chog.mint(player, amount);
        bytes memory ticket = _ticketFrom(REFEREE_KEY, on, MATCH, player, team, amount, deadline);
        vm.startPrank(player);
        chog.approve(address(on), amount);
        on.stake(MATCH, team, amount, deadline, ticket);
        vm.stopPrank();
    }
}
