// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Test} from "forge-std/Test.sol";
import {ChogRental} from "../src/ChogRental.sol";
import {TokenStandIn} from "./ChogShop.t.sol";

/// A collection where tokens 1 to 1969 exist and can change hands.
contract MovableGenesis {
    error NoSuchToken(uint256 tokenId);

    mapping(uint256 => address) private _holders;

    function give(uint256 tokenId, address to) external {
        _holders[tokenId] = to;
    }

    function ownerOf(uint256 tokenId) external view returns (address) {
        if (tokenId == 0 || tokenId > 1969) revert NoSuchToken(tokenId);
        address holder = _holders[tokenId];
        return holder == address(0) ? address(uint160(0x1000 + tokenId)) : holder;
    }
}

/// A token whose transferFrom answers nothing, as some older ERC-20 do.
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

contract ChogRentalTest is Test {
    uint256 private constant HERO = 1795;
    uint128 private constant PRICE = 10 ether;

    TokenStandIn private chog;
    MovableGenesis private genesis;
    ChogRental private rental;
    address private holder = makeAddr("holder");
    address private renter = makeAddr("renter");

    event Offered(uint256 indexed tokenId, address indexed lender, uint256 pricePerDay);
    event Withdrawn(uint256 indexed tokenId);
    event Rented(
        uint256 indexed tokenId, address indexed renter, address indexed lender, uint256 numberOfDays, uint256 paid, uint64 until
    );

    function setUp() public {
        // A realistic clock: forge starts at second 1.
        vm.warp(1_790_000_000);
        chog = new TokenStandIn();
        genesis = new MovableGenesis();
        // A tenth of every payment is burned.
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

    function test_TheHolderOffersAndARenterPaysTheHolderMinusTheBurnedShare() public {
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
        vm.prank(renter);
        rental.rent(HERO, 3, PRICE);

        assertTrue(rental.isRenting(HERO, renter));
        assertEq(rental.rentedUntil(HERO, renter), until);
        assertEq(chog.balanceOf(renter), 970 ether);
        assertEq(chog.balanceOf(holder), 27 ether);
        assertEq(chog.totalSupply(), 997 ether, "a tenth was burned");
        assertEq(chog.balanceOf(address(rental)), 0, "the contract keeps nothing");
    }

    function test_ARentalEndsWhenItsDaysAreOver() public {
        _offer();
        vm.prank(renter);
        rental.rent(HERO, 1, PRICE);

        vm.warp(block.timestamp + 1 days - 1);
        assertTrue(rental.isRenting(HERO, renter));
        vm.warp(block.timestamp + 1);
        assertFalse(rental.isRenting(HERO, renter));
    }

    function test_RentingAgainAddsDaysAfterThoseAlreadyPaid() public {
        _offer();
        uint256 start = block.timestamp;
        vm.startPrank(renter);
        rental.rent(HERO, 2, PRICE);
        vm.warp(start + 1 days);
        rental.rent(HERO, 3, PRICE);
        vm.stopPrank();
        assertEq(rental.rentedUntil(HERO, renter), start + 5 days);

        // Once a rental is over, new days count from now.
        vm.warp(start + 9 days);
        vm.prank(renter);
        rental.rent(HERO, 1, PRICE);
        assertEq(rental.rentedUntil(HERO, renter), start + 10 days);
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
        vm.stopPrank();
        assertEq(chog.balanceOf(renter), 700 ether);
    }

    function test_SeveralPlayersRentTheSameGenesisAtOnce() public {
        _offer();
        address second = makeAddr("second");
        chog.mint(second, 100 ether);
        vm.prank(renter);
        rental.rent(HERO, 1, PRICE);
        vm.startPrank(second);
        chog.approve(address(rental), 100 ether);
        rental.rent(HERO, 2, PRICE);
        vm.stopPrank();

        assertTrue(rental.isRenting(HERO, renter));
        assertTrue(rental.isRenting(HERO, second));
        assertEq(chog.balanceOf(holder), 27 ether);
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
        vm.prank(renter);
        rental.rent(HERO, 1, PRICE);
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

    function test_AWithdrawnOfferStopsNewRentalsAndKeepsThePaidOnes() public {
        _offer();
        vm.prank(renter);
        rental.rent(HERO, 5, PRICE);

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

    function test_ASoldGenesisKeepsItsPaidRentalsAndLosesItsOffer() public {
        _offer();
        vm.prank(renter);
        rental.rent(HERO, 5, PRICE);

        address newHolder = makeAddr("new holder");
        genesis.give(HERO, newHolder);

        assertTrue(rental.isRenting(HERO, renter), "what was paid still runs");
        (bool offered,,) = rental.offerOf(HERO);
        assertFalse(offered, "the former holder's offer no longer stands");
        vm.prank(renter);
        vm.expectRevert(abi.encodeWithSelector(ChogRental.NotOffered.selector, HERO));
        rental.rent(HERO, 1, PRICE);
        assertEq(chog.balanceOf(holder), 45 ether, "the former holder is paid nothing more");

        // The new holder clears the old offer, or makes their own and is the one paid.
        vm.prank(newHolder);
        rental.withdraw(HERO);
        vm.prank(newHolder);
        rental.offer(HERO, PRICE);
        vm.prank(renter);
        rental.rent(HERO, 1, PRICE);
        assertEq(chog.balanceOf(newHolder), 9 ether);
        assertEq(chog.balanceOf(holder), 45 ether);
    }

    function test_AFormerHolderMayStillTakeTheirOfferBack() public {
        _offer();
        genesis.give(HERO, makeAddr("new holder"));
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
        vm.expectRevert(ChogRental.PaymentFailed.selector);
        rental.rent(HERO, 1, PRICE);
        chog.approve(address(rental), PRICE);
        vm.expectRevert(ChogRental.PaymentFailed.selector);
        rental.rent(HERO, 1, PRICE);
        vm.stopPrank();
        assertFalse(rental.isRenting(HERO, broke));
    }

    function test_ATokenThatAnswersNothingOnTransferStillPays() public {
        SilentToken silent = new SilentToken();
        ChogRental other = new ChogRental(address(silent), address(genesis), 1000);
        vm.prank(holder);
        other.offer(HERO, PRICE);
        silent.mint(renter, PRICE);
        vm.startPrank(renter);
        silent.approve(address(other), PRICE);
        other.rent(HERO, 1, PRICE);
        vm.stopPrank();
        assertEq(silent.balanceOf(holder), 9 ether);
        assertTrue(other.isRenting(HERO, renter));
    }

    function test_OneReadListsEveryOfferThatStands() public {
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
    }

    function test_OneReadListsTheRentalsOfAnAddressThatStillRun() public {
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
        vm.prank(renter);
        rental.rent(HERO, 2, PRICE);
        (tokenIds,) = rental.rentalsOf(renter);
        assertEq(tokenIds.length, 2);
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

    /// Whatever the price, the days and the burned share, every unit paid is burned or with the holder.
    function testFuzz_EveryUnitPaidIsBurnedOrWithTheHolder(uint128 price, uint8 numberOfDays, uint16 share) public {
        price = uint128(bound(price, 1, type(uint128).max));
        numberOfDays = uint8(bound(numberOfDays, 1, 30));
        share = uint16(bound(share, 0, 10_000));
        ChogRental other = new ChogRental(address(chog), address(genesis), share);
        vm.prank(holder);
        other.offer(HERO, price);

        address payer = makeAddr("payer");
        uint256 cost = uint256(price) * numberOfDays;
        chog.mint(payer, cost);
        uint256 supplyBefore = chog.totalSupply();
        vm.startPrank(payer);
        chog.approve(address(other), cost);
        other.rent(HERO, numberOfDays, price);
        vm.stopPrank();

        uint256 burned = supplyBefore - chog.totalSupply();
        assertEq(burned, cost * share / 10_000);
        assertEq(chog.balanceOf(holder) + burned, cost);
        assertEq(chog.balanceOf(payer), 0);
        assertEq(chog.balanceOf(address(other)), 0);
        assertEq(other.rentedUntil(HERO, payer), block.timestamp + uint256(numberOfDays) * 1 days);
    }
}
