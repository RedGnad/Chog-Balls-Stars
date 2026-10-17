// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Test} from "forge-std/Test.sol";
import {ChogRental} from "../src/ChogRental.sol";

/// A collection where tokens 1 to 1969 exist, can change hands and can be burned.
contract MovableGenesis {
    error NoSuchToken(uint256 tokenId);

    mapping(uint256 => address) private _holders;
    mapping(uint256 => bool) private _burned;

    function give(uint256 tokenId, address to) external {
        _holders[tokenId] = to;
    }

    function burn(uint256 tokenId) external {
        _burned[tokenId] = true;
    }

    function ownerOf(uint256 tokenId) external view returns (address) {
        if (tokenId == 0 || tokenId > 1969 || _burned[tokenId]) revert NoSuchToken(tokenId);
        address holder = _holders[tokenId];
        return holder == address(0) ? address(uint160(0x1000 + tokenId)) : holder;
    }
}

/// An ERC-20 with burn that can refuse to pay some addresses, as a token with a blocklist would.
contract RentalToken {
    uint256 public totalSupply;
    mapping(address => uint256) public balanceOf;
    mapping(address => mapping(address => uint256)) public allowance;
    mapping(address => bool) public refused;

    function mint(address to, uint256 amount) external {
        balanceOf[to] += amount;
        totalSupply += amount;
    }

    function refuse(address account) external {
        refused[account] = true;
    }

    function approve(address spender, uint256 amount) external returns (bool) {
        allowance[msg.sender][spender] = amount;
        return true;
    }

    function transfer(address to, uint256 amount) external returns (bool) {
        require(!refused[to], "refused");
        balanceOf[msg.sender] -= amount;
        balanceOf[to] += amount;
        return true;
    }

    function transferFrom(address from, address to, uint256 amount) external returns (bool) {
        require(!refused[to], "refused");
        allowance[from][msg.sender] -= amount;
        balanceOf[from] -= amount;
        balanceOf[to] += amount;
        return true;
    }

    function burn(uint256 amount) external {
        balanceOf[msg.sender] -= amount;
        totalSupply -= amount;
    }
}

/// A token whose transferFrom answers nothing, as some older ERC-20 do (the wager's tests add a
/// transfer to it).
contract SilentToken {
    mapping(address => uint256) public balanceOf;
    mapping(address => mapping(address => uint256)) public allowance;

    function mint(address to, uint256 amount) external {
        balanceOf[to] += amount;
    }

    function approve(address spender, uint256 amount) external {
        allowance[msg.sender][spender] = amount;
    }

    function transferFrom(address from, address to, uint256 amount) external {
        allowance[from][msg.sender] -= amount;
        balanceOf[from] -= amount;
        balanceOf[to] += amount;
    }

    function burn(uint256 amount) external {
        balanceOf[msg.sender] -= amount;
    }
}

/// The silent token with a transfer that answers nothing either.
contract SilentRentalToken is SilentToken {
    function transfer(address to, uint256 amount) external {
        balanceOf[msg.sender] -= amount;
        balanceOf[to] += amount;
    }
}

contract ChogRentalTest is Test {
    uint256 private constant HERO = 1795;
    uint128 private constant PRICE = 10 ether;

    RentalToken private chog;
    MovableGenesis private genesis;
    ChogRental private rental;
    address private holder = makeAddr("holder");
    address private renter = makeAddr("renter");
    address private buyer = makeAddr("buyer");

    event Offered(uint256 indexed tokenId, address indexed lender, uint256 pricePerDay);
    event Withdrawn(uint256 indexed tokenId);
    event Rented(
        uint256 indexed tokenId, address indexed renter, address indexed lender, uint256 numberOfDays, uint256 paid, uint64 until
    );
    event Earned(uint256 indexed tokenId, address indexed lender, uint256 amount);
    event Ended(uint256 indexed tokenId, address indexed renter, address indexed lender, uint256 refunded);
    event Claimed(address indexed account, uint256 paid, uint256 burned);

    function setUp() public {
        // A realistic clock: forge starts at second 1.
        vm.warp(1_790_000_000);
        chog = new RentalToken();
        genesis = new MovableGenesis();
        // A tenth of what a holder earns is burned.
        rental = new ChogRental(address(chog), address(genesis), 1000);
        genesis.give(HERO, holder);
        chog.mint(renter, 1000 ether);
        vm.prank(renter);
        chog.approve(address(rental), type(uint256).max);
    }

    function _offer() private {
        vm.prank(holder);
        rental.offer(HERO, PRICE);
    }

    function _rent(uint256 numberOfDays) private {
        vm.prank(renter);
        rental.rent(HERO, numberOfDays, PRICE);
    }

    function _claim(address account) private {
        uint256[] memory tokenIds = new uint256[](1);
        tokenIds[0] = HERO;
        vm.prank(account);
        rental.claim(tokenIds);
    }

    function test_ARenterPaysIntoTheContractAndTheHolderEarnsBySecond() public {
        vm.expectEmit();
        emit Offered(HERO, holder, PRICE);
        _offer();

        (bool offered, uint128 pricePerDay, address lender) = rental.offerOf(HERO);
        assertTrue(offered);
        assertEq(pricePerDay, PRICE);
        assertEq(lender, holder);
        assertFalse(rental.isRenting(HERO, renter));

        uint64 until = uint64(block.timestamp + 3 days);
        vm.expectEmit();
        emit Rented(HERO, renter, holder, 3, 30 ether, until);
        _rent(3);

        assertTrue(rental.isRenting(HERO, renter));
        assertEq(rental.rentedUntil(HERO, renter), until);
        assertEq(chog.balanceOf(renter), 970 ether);
        assertEq(chog.balanceOf(address(rental)), 30 ether, "the payment is held");
        assertEq(chog.balanceOf(holder), 0);

        // One hour later the holder has earned one hour of the price.
        vm.warp(block.timestamp + 1 hours);
        (,,, uint256 escrow, uint256 earnedSince) = rental.rentalOf(HERO);
        assertEq(escrow, 30 ether);
        assertEq(earnedSince, uint256(PRICE) * 1 hours / 1 days);
    }

    function test_TheHolderClaimsWhatWasEarnedLessTheBurnedShare() public {
        _offer();
        _rent(3);
        vm.warp(block.timestamp + 1 days);

        uint256 earned = PRICE;
        vm.expectEmit();
        emit Earned(HERO, holder, earned);
        vm.expectEmit();
        emit Claimed(holder, 9 ether, 1 ether);
        _claim(holder);

        assertEq(chog.balanceOf(holder), 9 ether);
        assertEq(chog.totalSupply(), 999 ether, "a tenth of the earned part was burned");
        assertEq(chog.balanceOf(address(rental)), 20 ether, "the two days to come stay held");
        assertTrue(rental.isRenting(HERO, renter), "claiming does not end the rental");

        // Claiming again at once pays nothing more.
        _claim(holder);
        assertEq(chog.balanceOf(holder), 9 ether);

        // At the end the holder has earned everything.
        vm.warp(block.timestamp + 2 days);
        assertFalse(rental.isRenting(HERO, renter));
        _claim(holder);
        assertEq(chog.balanceOf(holder), 27 ether);
        assertEq(chog.totalSupply(), 997 ether);
        assertEq(chog.balanceOf(address(rental)), 0, "the contract keeps nothing");
    }

    function test_ARentalEndsWhenItsDaysAreOver() public {
        _offer();
        _rent(1);
        vm.warp(block.timestamp + 1 days - 1);
        assertTrue(rental.isRenting(HERO, renter));
        vm.warp(block.timestamp + 1);
        assertFalse(rental.isRenting(HERO, renter));

        // Closing it after its end gives the holder everything and the renter nothing back.
        vm.expectEmit();
        emit Ended(HERO, renter, holder, 0);
        rental.close(HERO);
        assertEq(rental.earnings(holder), PRICE);
        assertEq(rental.refunds(renter), 0);
        (address onRecord,,,,) = rental.rentalOf(HERO);
        assertEq(onRecord, address(0));
    }

    function test_ASaleEndsTheRightAtOnceAndAClosingRefundsTheTimeNotDelivered() public {
        _offer();
        _rent(4);
        vm.warp(block.timestamp + 1 days);

        genesis.give(HERO, buyer);
        assertFalse(rental.isRenting(HERO, renter), "no right once the holder who was paid no longer holds it");
        (uint256[] memory tokenIds,) = rental.rentalsOf(renter);
        assertEq(tokenIds.length, 0);
        (bool offered,,) = rental.offerOf(HERO);
        assertFalse(offered, "the former holder's offer no longer stands");

        vm.expectEmit();
        emit Ended(HERO, renter, holder, 30 ether);
        vm.prank(makeAddr("anyone"));
        rental.close(HERO);

        assertEq(rental.earnings(holder), 10 ether, "the seller earned the day delivered");
        assertEq(rental.refunds(renter), 30 ether);
        _claim(renter);
        assertEq(chog.balanceOf(renter), 990 ether, "the three days not delivered come back whole");
        _claim(holder);
        assertEq(chog.balanceOf(holder), 9 ether);
        assertEq(chog.balanceOf(address(rental)), 0);

        // The buyer offers it and is the one paid by the next renter.
        vm.prank(buyer);
        rental.offer(HERO, PRICE);
        _rent(1);
        assertTrue(rental.isRenting(HERO, renter));
        (, address lender,,,) = rental.rentalOf(HERO);
        assertEq(lender, buyer);
    }

    function test_ARenterClaimingAfterASaleClosesTheRentalThemselves() public {
        _offer();
        _rent(2);
        vm.warp(block.timestamp + 12 hours);
        genesis.give(HERO, buyer);
        _claim(renter);
        assertEq(chog.balanceOf(renter), 1000 ether - 5 ether, "half a day delivered, the rest back");
        assertEq(rental.earnings(holder), 5 ether);
    }

    function test_NobodyClosesARentalThatStillRuns() public {
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NoRental.selector, HERO));
        rental.close(HERO);
        _offer();
        _rent(2);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.StillRunning.selector, HERO));
        rental.close(HERO);
    }

    function test_ARentalOfABurnedGenesisCanBeClosed() public {
        _offer();
        _rent(2);
        vm.warp(block.timestamp + 1 days);
        genesis.burn(HERO);
        assertFalse(rental.isRenting(HERO, renter));
        rental.close(HERO);
        assertEq(rental.refunds(renter), PRICE);
        assertEq(rental.earnings(holder), PRICE);
    }

    function test_TheRenterEndsEarlyAndGetsTheRestBack() public {
        _offer();
        _rent(3);
        vm.warp(block.timestamp + 6 hours);

        vm.prank(makeAddr("someone else"));
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NotRenting.selector, HERO));
        rental.endRental(HERO);

        uint256 delivered = uint256(PRICE) * 6 hours / 1 days;
        vm.expectEmit();
        emit Ended(HERO, renter, holder, 30 ether - delivered);
        vm.prank(renter);
        rental.endRental(HERO);

        assertFalse(rental.isRenting(HERO, renter));
        assertEq(rental.earnings(holder), delivered);
        _claim(renter);
        assertEq(chog.balanceOf(renter), 1000 ether - delivered);

        vm.prank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NotRenting.selector, HERO));
        rental.endRental(HERO);
    }

    function test_ARentalThatRanOutCannotBeEndedByItsRenter() public {
        _offer();
        _rent(1);
        vm.warp(block.timestamp + 1 days);
        vm.prank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NotRenting.selector, HERO));
        rental.endRental(HERO);
    }

    function test_OnePlayerAtATimeAndTheGenesisIsFreeAgainAfterwards() public {
        _offer();
        address second = makeAddr("second");
        chog.mint(second, 100 ether);
        vm.prank(second);
        chog.approve(address(rental), type(uint256).max);

        uint64 until = uint64(block.timestamp + 1 days);
        _rent(1);
        vm.prank(second);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.AlreadyRented.selector, HERO, until));
        rental.rent(HERO, 2, PRICE);

        // Once the first rental is over, the next renter takes it; the first is settled.
        vm.warp(until);
        vm.prank(second);
        rental.rent(HERO, 2, PRICE);
        assertTrue(rental.isRenting(HERO, second));
        assertFalse(rental.isRenting(HERO, renter));
        assertEq(rental.earnings(holder), PRICE, "the first rental was earned whole");
        assertEq(rental.rentedUntil(HERO, renter), 0);
    }

    function test_RentingAgainAddsDaysAfterThoseAlreadyPaid() public {
        _offer();
        uint256 start = block.timestamp;
        _rent(2);
        vm.warp(start + 1 days);
        _rent(3);
        assertEq(rental.rentedUntil(HERO, renter), start + 5 days);
        assertEq(rental.earnings(holder), PRICE, "the day delivered was earned when days were added");
        (,,, uint256 escrow,) = rental.rentalOf(HERO);
        assertEq(escrow, 40 ether);

        // Once a rental is over, new days count from now.
        vm.warp(start + 9 days);
        _rent(1);
        assertEq(rental.rentedUntil(HERO, renter), start + 10 days);
        assertEq(rental.earnings(holder), 5 * uint256(PRICE));
    }

    function test_DaysAddedAtAnotherPriceAreSpreadOverTheTimeLeft() public {
        _offer();
        uint256 start = block.timestamp;
        _rent(2);
        vm.warp(start + 1 days);
        vm.prank(holder);
        rental.offer(HERO, PRICE / 2);
        _rent(2);
        // 10 left of the first payment and 10 of the second, over the three days left.
        vm.warp(start + 2 days);
        (,,,, uint256 earnedSince) = rental.rentalOf(HERO);
        assertEq(earnedSince, uint256(20 ether) / 3);
        vm.warp(start + 4 days);
        _claim(holder);
        assertEq(chog.balanceOf(holder), 27 ether, "30 paid in all, a tenth burned");
    }

    function test_NobodyIsPaidUpFurtherThanThirtyDays() public {
        _offer();
        vm.startPrank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.BadDuration.selector, 0));
        rental.rent(HERO, 0, PRICE);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.BadDuration.selector, 31));
        rental.rent(HERO, 31, PRICE);

        rental.rent(HERO, 30, PRICE);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.BadDuration.selector, 1));
        rental.rent(HERO, 1, PRICE);

        // A day later one more day fits again.
        vm.warp(block.timestamp + 1 days);
        rental.rent(HERO, 1, PRICE);
        vm.stopPrank();
        assertEq(chog.balanceOf(renter), 690 ether);
    }

    function test_OnlyTheHolderOffersAndNeverForNothing() public {
        vm.prank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NotTheHolder.selector, HERO));
        rental.offer(HERO, PRICE);

        vm.startPrank(holder);
        vm.expectRevert(ChogRental.PriceIsZero.selector);
        rental.offer(HERO, 0);
        vm.expectRevert(abi.encodeWithSelector(MovableGenesis.NoSuchToken.selector, 1970));
        rental.offer(1970, PRICE);
        vm.stopPrank();

        (bool offered,,) = rental.offerOf(HERO);
        assertFalse(offered);
    }

    function test_AGenesisThatIsNotOfferedCannotBeRented() public {
        vm.prank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NotOffered.selector, HERO));
        rental.rent(HERO, 1, PRICE);
        assertEq(chog.balanceOf(renter), 1000 ether);
    }

    function test_ARaisedPriceIsRefusedAndALoweredOneIsCharged() public {
        _offer();
        vm.prank(holder);
        rental.offer(HERO, 2 * PRICE);
        vm.prank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.PriceWentUp.selector, 2 * PRICE));
        rental.rent(HERO, 1, PRICE);

        vm.prank(holder);
        rental.offer(HERO, PRICE / 2);
        _rent(1);
        assertEq(chog.balanceOf(renter), 995 ether, "the lower price is the one paid");
    }

    function test_TheHolderDoesNotRentTheirOwnGenesis() public {
        _offer();
        chog.mint(holder, 100 ether);
        vm.startPrank(holder);
        chog.approve(address(rental), 100 ether);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.OwnGenesis.selector, HERO));
        rental.rent(HERO, 1, PRICE);
        vm.stopPrank();
    }

    function test_AWithdrawnOfferStopsNewRentalsAndKeepsTheRunningOne() public {
        _offer();
        _rent(5);

        vm.prank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NotTheHolder.selector, HERO));
        rental.withdraw(HERO);

        vm.expectEmit();
        emit Withdrawn(HERO);
        vm.prank(holder);
        rental.withdraw(HERO);

        (bool offered,,) = rental.offerOf(HERO);
        assertFalse(offered);
        assertTrue(rental.isRenting(HERO, renter), "what was paid still runs");
        vm.prank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NotOffered.selector, HERO));
        rental.rent(HERO, 1, PRICE);
    }

    function test_AFormerHolderMayStillTakeTheirOfferBack() public {
        _offer();
        genesis.give(HERO, buyer);
        vm.prank(holder);
        rental.withdraw(HERO);
        (, uint128 pricePerDay, address lender) = rental.offerOf(HERO);
        assertEq(pricePerDay, 0);
        assertEq(lender, address(0));
    }

    function test_WithoutApprovalOrFundsNothingIsRented() public {
        _offer();
        address broke = makeAddr("broke");
        vm.startPrank(broke);
        vm.expectRevert();
        rental.rent(HERO, 1, PRICE);
        chog.approve(address(rental), PRICE);
        vm.expectRevert();
        rental.rent(HERO, 1, PRICE);
        vm.stopPrank();
        assertFalse(rental.isRenting(HERO, broke));
    }

    function test_APaymentRefusedToOneDoesNotBlockTheOther() public {
        _offer();
        _rent(2);
        vm.warp(block.timestamp + 1 days);
        genesis.give(HERO, buyer);
        rental.close(HERO);

        // The token refuses to pay the former holder: only their own claim fails.
        chog.refuse(holder);
        vm.expectRevert(ChogRental.PaymentFailed.selector);
        _claim(holder);
        assertEq(rental.earnings(holder), PRICE, "kept for later");

        _claim(renter);
        assertEq(chog.balanceOf(renter), 1000 ether - PRICE, "the renter's day not delivered came back");
    }

    function test_ClaimingNothingDoesNothing() public {
        uint256[] memory none = new uint256[](0);
        vm.recordLogs();
        vm.prank(renter);
        rental.claim(none);
        assertEq(vm.getRecordedLogs().length, 0);
    }

    function test_ATokenThatAnswersNothingOnTransferStillPays() public {
        SilentRentalToken silent = new SilentRentalToken();
        ChogRental other = new ChogRental(address(silent), address(genesis), 1000);
        vm.prank(holder);
        other.offer(HERO, PRICE);
        silent.mint(renter, PRICE);
        vm.startPrank(renter);
        silent.approve(address(other), PRICE);
        other.rent(HERO, 1, PRICE);
        vm.stopPrank();
        assertTrue(other.isRenting(HERO, renter));

        vm.warp(block.timestamp + 1 days);
        uint256[] memory tokenIds = new uint256[](1);
        tokenIds[0] = HERO;
        vm.prank(holder);
        other.claim(tokenIds);
        assertEq(silent.balanceOf(holder), 9 ether);
    }

    function test_OneReadListsEveryOfferThatStandsAndIsFree() public {
        address other = makeAddr("other holder");
        genesis.give(8, other);
        genesis.give(408, other);
        _offer();
        vm.startPrank(other);
        rental.offer(8, 3 ether);
        rental.offer(408, 4 ether);
        // Offering again changes the price and does not list the Genesis twice.
        rental.offer(8, 5 ether);
        vm.stopPrank();

        (uint256[] memory tokenIds, uint128[] memory prices, address[] memory lenders) = rental.standingOffers();
        assertEq(tokenIds.length, 3);
        assertEq(tokenIds[0], HERO);
        assertEq(tokenIds[1], 8);
        assertEq(prices[1], 5 ether);
        assertEq(lenders[1], other);
        assertEq(tokenIds[2], 408);

        // A rented Genesis leaves the list while it is rented.
        _rent(1);
        (tokenIds,,) = rental.standingOffers();
        assertEq(tokenIds.length, 2);
        assertEq(tokenIds[0], 8, "the others keep their order");
        assertEq(tokenIds[1], 408);
        vm.warp(block.timestamp + 1 days);
        (tokenIds,,) = rental.standingOffers();
        assertEq(tokenIds.length, 3, "back once the rental is over");

        // A sold Genesis and a withdrawn offer leave the list; the last one takes the freed place.
        genesis.give(8, makeAddr("buyer of 8"));
        vm.prank(holder);
        rental.withdraw(HERO);
        (tokenIds, prices, lenders) = rental.standingOffers();
        assertEq(tokenIds.length, 1);
        assertEq(prices.length, 1);
        assertEq(lenders.length, 1);
        assertEq(tokenIds[0], 408);
        assertEq(prices[0], 4 ether);

        // Offered again after a withdrawal, a Genesis is listed again, once.
        _offer();
        vm.prank(other);
        rental.withdraw(408);
        (tokenIds,,) = rental.standingOffers();
        assertEq(tokenIds.length, 1);
        assertEq(tokenIds[0], HERO);

        // A burned Genesis does not stop the list from being read.
        genesis.burn(HERO);
        (tokenIds,,) = rental.standingOffers();
        assertEq(tokenIds.length, 0);
    }

    function test_OneReadListsTheRentalsOfAnAddressThatStillCount() public {
        address other = makeAddr("other holder");
        genesis.give(8, other);
        _offer();
        vm.prank(other);
        rental.offer(8, 1 ether);

        (uint256[] memory tokenIds, uint64[] memory until) = rental.rentalsOf(renter);
        assertEq(tokenIds.length, 0);

        uint256 start = block.timestamp;
        vm.startPrank(renter);
        rental.rent(HERO, 1, PRICE);
        rental.rent(8, 3, 1 ether);
        // More days on the same Genesis do not list it twice.
        rental.rent(8, 1, 1 ether);
        vm.stopPrank();

        (tokenIds, until) = rental.rentalsOf(renter);
        assertEq(tokenIds.length, 2);
        assertEq(tokenIds[0], HERO);
        assertEq(until[0], start + 1 days);
        assertEq(tokenIds[1], 8);
        assertEq(until[1], start + 4 days);

        vm.warp(start + 2 days);
        (tokenIds, until) = rental.rentalsOf(renter);
        assertEq(tokenIds.length, 1, "the rental that ran out is no longer listed");
        assertEq(until.length, 1);
        assertEq(tokenIds[0], 8);

        // Rented again after it ran out, it is listed again, once.
        _rent(2);
        (tokenIds,) = rental.rentalsOf(renter);
        assertEq(tokenIds.length, 2);

        // A sale takes it off the list at once.
        genesis.give(8, buyer);
        (tokenIds,) = rental.rentalsOf(renter);
        assertEq(tokenIds.length, 1);
        assertEq(tokenIds[0], HERO);
        (tokenIds,) = rental.rentalsOf(holder);
        assertEq(tokenIds.length, 0);
    }

    function test_TheRentalCannotBeDeployedHalfConfigured() public {
        vm.expectRevert(ChogRental.ZeroAddress.selector);
        new ChogRental(address(0), address(genesis), 0);
        vm.expectRevert(ChogRental.ZeroAddress.selector);
        new ChogRental(address(chog), address(0), 0);
        vm.expectRevert(ChogRental.ShareTooLarge.selector);
        new ChogRental(address(chog), address(genesis), 10_001);
    }

    /// Whatever the price, the days, the share, the days added, when the holder claims and when the
    /// Genesis is sold: every unit paid in is paid out, refunded or burned, none created or lost.
    function testFuzz_EveryUnitPaidInIsPaidOutRefundedOrBurned(
        uint128 price,
        uint8 numberOfDays,
        uint8 added,
        uint16 share,
        uint32 claimAfter,
        uint32 soldAfter
    ) public {
        price = uint128(bound(price, 1, type(uint128).max / 60));
        numberOfDays = uint8(bound(numberOfDays, 1, 15));
        added = uint8(bound(added, 0, 15));
        ChogRental other = new ChogRental(address(chog), address(genesis), uint16(bound(share, 0, 10_000)));
        vm.prank(holder);
        other.offer(HERO, price);

        uint256 paidIn = uint256(price) * (uint256(numberOfDays) + added);
        chog.mint(_payer, paidIn);
        uint256 supplyBefore = chog.totalSupply();
        vm.startPrank(_payer);
        chog.approve(address(other), paidIn);
        other.rent(HERO, numberOfDays, price);
        vm.stopPrank();

        vm.warp(block.timestamp + bound(claimAfter, 0, uint256(numberOfDays) * 1 days - 1));
        if (added > 0) {
            vm.prank(_payer);
            other.rent(HERO, added, price);
        }
        _claimOn(other, holder);

        vm.warp(block.timestamp + bound(soldAfter, 0, 40 days));
        genesis.give(HERO, buyer);
        _claimOn(other, _payer);
        _claimOn(other, holder);

        uint256 burned = supplyBefore - chog.totalSupply();
        assertEq(chog.balanceOf(holder) + chog.balanceOf(_payer) + burned, paidIn, "nothing created or lost");
        assertEq(chog.balanceOf(address(other)), 0, "the contract keeps nothing once all have claimed");
        assertFalse(other.isRenting(HERO, _payer));
    }

    address private _payer = makeAddr("payer");

    function _claimOn(ChogRental other, address account) private {
        uint256[] memory tokenIds = new uint256[](1);
        tokenIds[0] = HERO;
        vm.prank(account);
        other.claim(tokenIds);
    }
}

/// What each step costs with every account cold, as in a transaction of its own: the execution
/// measured by `lastFrameGas` plus 21,000 and the calldata, less the refund a transaction gets.
/// Printed with `forge test -vv`.
contract ChogRentalGasTest is Test {
    uint256 private constant HERO = 1795;
    uint128 private constant PRICE = 10 ether;

    RentalToken private chog;
    MovableGenesis private genesis;
    ChogRental private rental;
    address private holder = makeAddr("holder");
    address private renter = makeAddr("renter");

    function setUp() public {
        vm.warp(1_790_000_000);
        chog = new RentalToken();
        genesis = new MovableGenesis();
        rental = new ChogRental(address(chog), address(genesis), 1000);
        genesis.give(HERO, holder);
        chog.mint(renter, 1000 ether);
        vm.prank(renter);
        chog.approve(address(rental), type(uint256).max);
    }

    function _measure(string memory step, address from, bytes memory data) private returns (uint256 total) {
        vm.cool(address(rental));
        vm.cool(address(chog));
        vm.cool(address(genesis));
        vm.prank(from);
        (bool ok,) = address(rental).call(data);
        assertTrue(ok, step);
        uint256 calldataGas;
        for (uint256 index; index < data.length; ++index) {
            calldataGas += data[index] == 0 ? 4 : 16;
        }
        // Read by hand: this forge answers five fields where forge-std's Vm.Gas now has six.
        (, bytes memory answer) = address(vm).staticcall(abi.encodeWithSignature("lastFrameGas()"));
        (, uint64 used,, int64 refund,) = abi.decode(answer, (uint64, uint64, uint64, int64, uint64));
        total = 21_000 + calldataGas + used;
        // A transaction gets back its refunds, up to a fifth of what it used (EIP-3529).
        uint256 refunded = refund > 0 ? uint256(int256(refund)) : 0;
        total -= refunded < total / 5 ? refunded : total / 5;
        emit log_named_uint(step, total);
    }

    function test_WhatEachStepCosts() public {
        uint256[] memory hero = new uint256[](1);
        hero[0] = HERO;
        _measure("first offer", holder, abi.encodeCall(ChogRental.offer, (HERO, PRICE)));
        _measure("new price", holder, abi.encodeCall(ChogRental.offer, (HERO, PRICE / 2)));
        _measure("first rental", renter, abi.encodeCall(ChogRental.rent, (HERO, 3, PRICE)));
        vm.warp(block.timestamp + 1 days);
        _measure("more days", renter, abi.encodeCall(ChogRental.rent, (HERO, 2, PRICE)));
        vm.warp(block.timestamp + 1 days);
        _measure("claim by the holder, one Genesis", holder, abi.encodeCall(ChogRental.claim, (hero)));
        _measure("end early by the renter", renter, abi.encodeCall(ChogRental.endRental, (HERO)));
        _measure("claim of a refund", renter, abi.encodeCall(ChogRental.claim, (new uint256[](0))));
        _measure("rent again", renter, abi.encodeCall(ChogRental.rent, (HERO, 2, PRICE)));
        vm.warp(block.timestamp + 12 hours);
        genesis.give(HERO, makeAddr("buyer"));
        _measure("close after a sale", makeAddr("anyone"), abi.encodeCall(ChogRental.close, (HERO)));
        _measure("withdraw an offer", holder, abi.encodeCall(ChogRental.withdraw, (HERO)));
    }
}
