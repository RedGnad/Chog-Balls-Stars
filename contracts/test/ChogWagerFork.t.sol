// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Test} from "forge-std/Test.sol";
import {ChogWager} from "../src/ChogWager.sol";

interface IRealChog {
    function balanceOf(address account) external view returns (uint256);
    function totalSupply() external view returns (uint256);
    function nonces(address owner) external view returns (uint256);
    function DOMAIN_SEPARATOR() external view returns (bytes32);
}

/// The wager DEPLOYED on Monad mainnet (version 3) and the real $CHOG token, on a local copy of the chain. Nothing is
/// sent to the chain. Its owner, impersonated on the copy only, gives it a test referee; players stake the way the game
/// has them stake (a permit signed in the wallet, brought by the game's services), then the referee's result pays. It
/// answers what the unit tests cannot: does the contract that judges will use pay what the rules say, with the token
/// that is really staked? (23 October 2026, the owner: no live staked match will be played before the judges; audit.)
///
///   MONAD_FORK=1 forge test --match-contract ChogWagerForkTest -vv
contract ChogWagerForkTest is Test {
    address private constant CHOG = 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777;
    address private constant WAGER = 0xa9d1Faa2e09753f5419d768Cae05b1e2596b3E4f;
    bytes32 private constant PERMIT_TYPEHASH =
        keccak256("Permit(address owner,address spender,uint256 value,uint256 nonce,uint256 deadline)");
    uint256 private constant REFEREE_KEY = 0xA11CE;
    uint256 private constant ALICE_KEY = 0xA1;
    uint256 private constant BOB_KEY = 0xB0B;
    uint256 private constant CAROL_KEY = 0xCA401;
    uint256 private constant STAKE = 25 ether;

    IRealChog private chog = IRealChog(CHOG);
    ChogWager private wager = ChogWager(WAGER);
    address private relayer = makeAddr("services");
    address private alice;
    address private bob;
    address private carol;

    function setUp() public {
        if (!vm.envOr("MONAD_FORK", false)) {
            vm.skip(true);
        }
        vm.createSelectFork(vm.rpcUrl("monad"));
        assertEq(block.chainid, 143, "not Monad mainnet");
        assertEq(wager.burnedShare(), 500, "5% of lost stakes burned");
        assertEq(wager.genesisShare(), 500, "5% of lost stakes to the Genesis players");
        vm.prank(wager.owner());
        wager.setReferee(vm.addr(REFEREE_KEY));
        alice = vm.addr(ALICE_KEY);
        bob = vm.addr(BOB_KEY);
        carol = vm.addr(CAROL_KEY);
        deal(CHOG, alice, 1000 ether);
        deal(CHOG, bob, 1000 ether);
        deal(CHOG, carol, 1000 ether);
    }

    function _sign(uint256 key, bytes32 digest) private pure returns (bytes memory) {
        (uint8 v, bytes32 r, bytes32 s) = vm.sign(key, keccak256(abi.encodePacked("\x19Ethereum Signed Message:\n32", digest)));
        return abi.encodePacked(r, s, v);
    }

    // What the game does for one player: the referee's ticket, the player's permit, the services bring both.
    function _stakeWithPermit(bytes32 matchKey, uint256 key, uint8 team) private {
        address player = vm.addr(key);
        uint256 until = block.timestamp + 2 minutes;
        bytes memory ticket = _sign(
            REFEREE_KEY, keccak256(abi.encode(wager.TICKET(), block.chainid, WAGER, matchKey, player, team, STAKE, until))
        );
        bytes32 structHash = keccak256(abi.encode(PERMIT_TYPEHASH, player, WAGER, STAKE, chog.nonces(player), until));
        (uint8 v, bytes32 r, bytes32 s) = vm.sign(key, keccak256(abi.encodePacked("\x19\x01", chog.DOMAIN_SEPARATOR(), structHash)));
        vm.prank(relayer);
        wager.stakeWithPermit(player, matchKey, team, STAKE, until, ticket, until, v, r, s);
    }

    function _settle(bytes32 matchKey, uint8 winnerTeam, address[] memory genesisPlayers) private {
        bytes memory result =
            _sign(REFEREE_KEY, keccak256(abi.encode(wager.RESULT(), block.chainid, WAGER, matchKey, winnerTeam, genesisPlayers)));
        vm.prank(makeAddr("anyone"));
        wager.settle(matchKey, winnerTeam, genesisPlayers, result);
    }

    function test_TwoPlayersFacingEachOtherAreSettledAsTheRulesSay() public {
        bytes32 matchKey = keccak256("fork-honest");
        _stakeWithPermit(matchKey, ALICE_KEY, 0);
        _stakeWithPermit(matchKey, BOB_KEY, 1);
        assertEq(chog.balanceOf(WAGER), 50 ether, "the contract holds both stakes");
        uint256 supply = chog.totalSupply();

        // Bob's team wins. Alice played a Genesis and lost; Carol played a Genesis on Bob's team and did not stake.
        address[] memory genesisPlayers = new address[](2);
        genesisPlayers[0] = alice;
        genesisPlayers[1] = carol;
        _settle(matchKey, 1, genesisPlayers);

        assertEq(chog.balanceOf(bob), 1000 ether + 22.5 ether, "the winner: his stake back and 90% of the lost one");
        assertEq(chog.balanceOf(alice), 1000 ether - 25 ether + 0.625 ether, "the loser: half of the Genesis share");
        assertEq(chog.balanceOf(carol), 1000 ether + 0.625 ether, "a Genesis player who did not stake: the other half, no gain");
        assertEq(supply - chog.totalSupply(), 1.25 ether, "5% of the lost stake burned");
        assertEq(chog.balanceOf(WAGER), 0, "nothing left in the contract");
        (uint8 state,,,,) = wager.matchOf(matchKey);
        assertEq(state, 2, "settled");
    }

    function test_ALoneStakerGetsEverythingBackWithoutAFee() public {
        bytes32 matchKey = keccak256("fork-alone");
        _stakeWithPermit(matchKey, ALICE_KEY, 0);
        uint256 supply = chog.totalSupply();
        address[] memory genesisPlayers = new address[](1);
        genesisPlayers[0] = alice;
        _settle(matchKey, 0, genesisPlayers);
        assertEq(chog.balanceOf(alice), 1000 ether, "her whole stake back, she won alone");
        assertEq(chog.totalSupply(), supply, "nothing burned");
        (uint8 state,,,,) = wager.matchOf(matchKey);
        assertEq(state, 3, "given back");
    }

    function test_StakesOnOneSideOnlyComeBackEvenWhenThatSideLoses() public {
        bytes32 matchKey = keccak256("fork-one-side");
        _stakeWithPermit(matchKey, ALICE_KEY, 0);
        _stakeWithPermit(matchKey, BOB_KEY, 0);
        _settle(matchKey, 1, new address[](0));
        assertEq(chog.balanceOf(alice), 1000 ether);
        assertEq(chog.balanceOf(bob), 1000 ether);
    }

    function test_ADrawGivesBothStakesBack() public {
        bytes32 matchKey = keccak256("fork-draw");
        _stakeWithPermit(matchKey, ALICE_KEY, 0);
        _stakeWithPermit(matchKey, BOB_KEY, 1);
        _settle(matchKey, 2, new address[](0));
        assertEq(chog.balanceOf(alice), 1000 ether);
        assertEq(chog.balanceOf(bob), 1000 ether);
    }

    function test_WithoutAResultAnHourLaterAnyoneGivesTheStakesBack() public {
        bytes32 matchKey = keccak256("fork-expired");
        _stakeWithPermit(matchKey, ALICE_KEY, 0);
        _stakeWithPermit(matchKey, BOB_KEY, 1);
        vm.expectRevert();
        wager.refundExpired(matchKey);
        vm.warp(block.timestamp + 1 hours);
        vm.prank(makeAddr("anyone"));
        wager.refundExpired(matchKey);
        assertEq(chog.balanceOf(alice), 1000 ether);
        assertEq(chog.balanceOf(bob), 1000 ether);
    }

    function test_AStakeOfAnotherAmountOrASecondStakeIsRefused() public {
        bytes32 matchKey = keccak256("fork-rules");
        _stakeWithPermit(matchKey, ALICE_KEY, 0);
        uint256 until = block.timestamp + 2 minutes;
        bytes memory again = _sign(
            REFEREE_KEY, keccak256(abi.encode(wager.TICKET(), block.chainid, WAGER, matchKey, alice, uint8(1), STAKE, until))
        );
        vm.prank(alice);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.AlreadyStaked.selector, matchKey));
        wager.stake(matchKey, 1, STAKE, until, again);
        bytes memory dearer = _sign(
            REFEREE_KEY, keccak256(abi.encode(wager.TICKET(), block.chainid, WAGER, matchKey, bob, uint8(1), 50 ether, until))
        );
        vm.prank(bob);
        vm.expectRevert(abi.encodeWithSelector(ChogWager.NotTheStakeOfTheMatch.selector, STAKE));
        wager.stake(matchKey, 1, 50 ether, until, dearer);
    }
}
