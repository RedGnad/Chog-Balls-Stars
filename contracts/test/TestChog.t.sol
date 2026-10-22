// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Test} from "forge-std/Test.sol";
import {ChogWager} from "../src/ChogWager.sol";
import {TestChog} from "../src/testnet/TestChog.sol";

contract TestChogTest is Test {
    uint256 private constant REFEREE_KEY = 0xA11CE;
    bytes32 private constant MATCH = keccak256("eu:testnet-0001");

    TestChog private chog;
    address private ana = makeAddr("ana");
    address private bo = makeAddr("bo");

    function setUp() public {
        vm.warp(1_790_000_000);
        chog = new TestChog();
    }

    function test_TheTapGivesAThousandOnceADay() public {
        vm.prank(ana);
        chog.claim();
        assertEq(chog.balanceOf(ana), 1000 ether);
        assertEq(chog.totalSupply(), 1000 ether);

        vm.warp(block.timestamp + 1 days - 1);
        vm.prank(ana);
        vm.expectRevert(abi.encodeWithSelector(TestChog.ClaimTooSoon.selector, block.timestamp + 1));
        chog.claim();

        vm.warp(block.timestamp + 1);
        vm.prank(ana);
        chog.claim();
        assertEq(chog.balanceOf(ana), 2000 ether);
    }

    function test_TransfersNeedTheBalanceAndTheAllowance() public {
        vm.prank(ana);
        chog.claim();

        vm.prank(bo);
        vm.expectRevert(TestChog.InsufficientAllowance.selector);
        chog.transferFrom(ana, bo, 1 ether);

        vm.prank(ana);
        chog.approve(bo, 10 ether);
        vm.prank(bo);
        chog.transferFrom(ana, bo, 4 ether);
        assertEq(chog.allowance(ana, bo), 6 ether);
        assertEq(chog.balanceOf(bo), 4 ether);

        vm.prank(bo);
        vm.expectRevert(TestChog.InsufficientBalance.selector);
        chog.transfer(ana, 5 ether);
    }

    function test_BurnDestroysTheCallersOwnTokens() public {
        vm.prank(ana);
        chog.claim();
        vm.prank(ana);
        chog.burn(250 ether);
        assertEq(chog.balanceOf(ana), 750 ether);
        assertEq(chog.totalSupply(), 750 ether);
    }

    // The wager runs on the stand-in as on $CHOG: two players stake 100 each, team 0 wins, the
    // winner gets 190 and 10 are burned.
    function test_AStakedMatchPaysAndBurnsWithTheStandIn() public {
        ChogWager wager = new ChogWager(address(chog), vm.addr(REFEREE_KEY), 1000, 0, address(this));
        _claimAndStake(wager, ana, 0);
        _claimAndStake(wager, bo, 1);

        wager.settle(MATCH, 0, new address[](0), _sign(keccak256(abi.encode(wager.RESULT(), block.chainid, address(wager), MATCH, uint8(0), new address[](0)))));

        assertEq(chog.balanceOf(ana), 1090 ether);
        assertEq(chog.balanceOf(bo), 900 ether);
        assertEq(chog.balanceOf(address(wager)), 0);
        assertEq(chog.totalSupply(), 1990 ether);
    }

    function _claimAndStake(ChogWager wager, address player, uint8 team) private {
        uint256 until = block.timestamp + 2 minutes;
        bytes memory ticket =
            _sign(keccak256(abi.encode(wager.TICKET(), block.chainid, address(wager), MATCH, player, team, 100 ether, until)));
        vm.startPrank(player);
        chog.claim();
        chog.approve(address(wager), type(uint256).max);
        wager.stake(MATCH, team, 100 ether, until, ticket);
        vm.stopPrank();
    }

    function _sign(bytes32 digest) private pure returns (bytes memory) {
        (uint8 v, bytes32 r, bytes32 s) =
            vm.sign(REFEREE_KEY, keccak256(abi.encodePacked("\x19Ethereum Signed Message:\n32", digest)));
        return abi.encodePacked(r, s, v);
    }
}
