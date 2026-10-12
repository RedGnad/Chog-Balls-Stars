// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Test} from "forge-std/Test.sol";
import {ChogShop} from "../src/ChogShop.sol";

/// A plain ERC-20 with EIP-2612 permit and burn, standing in for $CHOG.
contract TokenStandIn {
    bytes32 public constant PERMIT_TYPEHASH =
        keccak256("Permit(address owner,address spender,uint256 value,uint256 nonce,uint256 deadline)");
    bytes32 public immutable DOMAIN_SEPARATOR;

    uint256 public totalSupply;
    mapping(address => uint256) public balanceOf;
    mapping(address => mapping(address => uint256)) public allowance;
    mapping(address => uint256) public nonces;

    constructor() {
        DOMAIN_SEPARATOR = keccak256(
            abi.encode(
                keccak256("EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)"),
                keccak256("Chog"),
                keccak256("1"),
                block.chainid,
                address(this)
            )
        );
    }

    function mint(address to, uint256 amount) external {
        balanceOf[to] += amount;
        totalSupply += amount;
    }

    function approve(address spender, uint256 amount) external returns (bool) {
        allowance[msg.sender][spender] = amount;
        return true;
    }

    function transferFrom(address from, address to, uint256 amount) external returns (bool) {
        allowance[from][msg.sender] -= amount;
        balanceOf[from] -= amount;
        balanceOf[to] += amount;
        return true;
    }

    function permit(address owner, address spender, uint256 value, uint256 deadline, uint8 v, bytes32 r, bytes32 s)
        external
    {
        require(block.timestamp <= deadline, "expired");
        bytes32 structHash = keccak256(abi.encode(PERMIT_TYPEHASH, owner, spender, value, nonces[owner]++, deadline));
        address signer = ecrecover(keccak256(abi.encodePacked("\x19\x01", DOMAIN_SEPARATOR, structHash)), v, r, s);
        require(signer != address(0) && signer == owner, "bad signature");
        allowance[owner][spender] = value;
    }

    function burn(uint256 amount) external {
        balanceOf[msg.sender] -= amount;
        totalSupply -= amount;
    }
}

/// A collection where tokens 1 to 1969 exist.
contract GenesisStandIn {
    error NoSuchToken(uint256 tokenId);

    function ownerOf(uint256 tokenId) external pure returns (address) {
        if (tokenId == 0 || tokenId > 1969) revert NoSuchToken(tokenId);
        return address(uint160(0x1000 + tokenId));
    }
}

contract ChogShopTest is Test {
    uint256 private constant GOLD_RING = 1;
    uint128 private constant PRICE = 100 ether;
    uint256 private constant BUYER_KEY = 0xB0B;

    TokenStandIn private chog;
    GenesisStandIn private genesis;
    ChogShop private shop;
    address private admin = makeAddr("admin");
    address private treasury = makeAddr("treasury");
    address private buyer;

    event Purchased(address indexed buyer, uint256 indexed tokenId, uint256 indexed itemId, uint256 price);

    function setUp() public {
        chog = new TokenStandIn();
        genesis = new GenesisStandIn();
        // A quarter of every payment is burned.
        shop = new ChogShop(address(chog), address(genesis), treasury, 2500, admin);
        buyer = vm.addr(BUYER_KEY);
        chog.mint(buyer, 1000 ether);
        vm.prank(admin);
        shop.setItem(GOLD_RING, PRICE, true);
    }

    function _permit(uint256 key, uint256 value, uint256 deadline) private view returns (uint8 v, bytes32 r, bytes32 s) {
        address signer = vm.addr(key);
        bytes32 structHash =
            keccak256(abi.encode(chog.PERMIT_TYPEHASH(), signer, address(shop), value, chog.nonces(signer), deadline));
        return vm.sign(key, keccak256(abi.encodePacked("\x19\x01", chog.DOMAIN_SEPARATOR(), structHash)));
    }

    function test_AnApprovedBuyerGetsTheItemForTheGenesisAndThePaymentIsSplit() public {
        vm.prank(buyer);
        chog.approve(address(shop), PRICE);

        vm.expectEmit();
        emit Purchased(buyer, 1795, GOLD_RING, PRICE);
        vm.prank(buyer);
        shop.buy(1795, GOLD_RING);

        assertTrue(shop.owned(1795, GOLD_RING));
        assertFalse(shop.owned(1796, GOLD_RING), "only that Genesis has it");
        assertEq(chog.balanceOf(buyer), 900 ether);
        assertEq(chog.balanceOf(treasury), 75 ether);
        assertEq(chog.totalSupply(), 975 ether, "a quarter was burned");
        assertEq(chog.balanceOf(address(shop)), 0, "the shop keeps nothing");
    }

    function test_AKeyPairWalletPaysInOneTransactionWithAPermit() public {
        (uint8 v, bytes32 r, bytes32 s) = _permit(BUYER_KEY, PRICE, block.timestamp + 1 hours);
        vm.prank(buyer);
        shop.buyWithPermit(8, GOLD_RING, block.timestamp + 1 hours, v, r, s);

        assertTrue(shop.owned(8, GOLD_RING));
        assertEq(chog.balanceOf(treasury), 75 ether);
        assertEq(chog.allowance(buyer, address(shop)), 0, "the permit was for exactly the price");
    }

    function test_APermitSomeoneElseSubmittedFirstStillBuys() public {
        uint256 deadline = block.timestamp + 1 hours;
        (uint8 v, bytes32 r, bytes32 s) = _permit(BUYER_KEY, PRICE, deadline);
        // A watcher copies the permit from the pending transaction and submits it alone.
        chog.permit(buyer, address(shop), PRICE, deadline, v, r, s);

        vm.prank(buyer);
        shop.buyWithPermit(8, GOLD_RING, deadline, v, r, s);
        assertTrue(shop.owned(8, GOLD_RING));
    }

    function test_APermitSignedByAnotherKeyPaysNothing() public {
        (uint8 v, bytes32 r, bytes32 s) = _permit(0xBAD, PRICE, block.timestamp + 1 hours);
        vm.prank(buyer);
        vm.expectRevert(ChogShop.PaymentFailed.selector);
        shop.buyWithPermit(8, GOLD_RING, block.timestamp + 1 hours, v, r, s);
        assertFalse(shop.owned(8, GOLD_RING));
        assertEq(chog.balanceOf(buyer), 1000 ether);
    }

    function test_AnyoneMayBuyForAGenesisTheyDoNotHold() public {
        address friend = makeAddr("friend");
        chog.mint(friend, PRICE);
        vm.startPrank(friend);
        chog.approve(address(shop), PRICE);
        shop.buy(1795, GOLD_RING);
        vm.stopPrank();
        assertTrue(shop.owned(1795, GOLD_RING));
    }

    function test_AnItemIsBoughtOncePerGenesis() public {
        vm.startPrank(buyer);
        chog.approve(address(shop), 2 * PRICE);
        shop.buy(1795, GOLD_RING);
        vm.expectRevert(abi.encodeWithSelector(ChogShop.AlreadyOwned.selector, 1795, GOLD_RING));
        shop.buy(1795, GOLD_RING);
        shop.buy(1796, GOLD_RING);
        vm.stopPrank();
        assertEq(chog.balanceOf(buyer), 800 ether);
    }

    function test_NothingIsSoldForATokenThatIsNotAGenesisOrAnItemNotForSale() public {
        vm.startPrank(buyer);
        chog.approve(address(shop), 10 * PRICE);
        vm.expectRevert(abi.encodeWithSelector(GenesisStandIn.NoSuchToken.selector, 1970));
        shop.buy(1970, GOLD_RING);
        vm.expectRevert(abi.encodeWithSelector(ChogShop.NotForSale.selector, 2));
        shop.buy(1795, 2);
        vm.stopPrank();
        assertEq(chog.balanceOf(buyer), 1000 ether);
    }

    function test_WithoutApprovalOrFundsThePaymentFailsAndNothingIsGiven() public {
        vm.prank(buyer);
        vm.expectRevert(ChogShop.PaymentFailed.selector);
        shop.buy(1795, GOLD_RING);
        assertFalse(shop.owned(1795, GOLD_RING));

        address broke = makeAddr("broke");
        vm.startPrank(broke);
        chog.approve(address(shop), PRICE);
        vm.expectRevert(ChogShop.PaymentFailed.selector);
        shop.buy(1795, GOLD_RING);
        vm.stopPrank();
    }

    function test_TakingAnItemOffSaleKeepsWhatWasBought() public {
        vm.startPrank(buyer);
        chog.approve(address(shop), 2 * PRICE);
        shop.buy(1795, GOLD_RING);
        vm.stopPrank();

        vm.prank(admin);
        shop.setItem(GOLD_RING, PRICE, false);
        assertTrue(shop.owned(1795, GOLD_RING));
        vm.prank(buyer);
        vm.expectRevert(abi.encodeWithSelector(ChogShop.NotForSale.selector, GOLD_RING));
        shop.buy(1796, GOLD_RING);
    }

    function test_OnlyTheOwnerListsItemsAndHandsTheShopOver() public {
        vm.expectRevert(ChogShop.NotOwner.selector);
        shop.setItem(2, 1 ether, true);
        vm.expectRevert(ChogShop.NotOwner.selector);
        shop.transferOwnership(address(this));

        vm.startPrank(admin);
        vm.expectRevert(ChogShop.ZeroAddress.selector);
        shop.transferOwnership(address(0));
        shop.transferOwnership(address(this));
        vm.stopPrank();

        shop.setItem(2, 1 ether, true);
        (uint128 price, bool forSale) = shop.items(2);
        assertEq(price, 1 ether);
        assertTrue(forSale);
    }

    function test_TheShopCannotBeDeployedHalfConfigured() public {
        vm.expectRevert(ChogShop.ZeroAddress.selector);
        new ChogShop(address(0), address(genesis), treasury, 0, admin);
        vm.expectRevert(ChogShop.ZeroAddress.selector);
        new ChogShop(address(chog), address(genesis), address(0), 0, admin);
        vm.expectRevert(ChogShop.ZeroAddress.selector);
        new ChogShop(address(chog), address(genesis), treasury, 0, address(0));
        vm.expectRevert(ChogShop.ShareTooLarge.selector);
        new ChogShop(address(chog), address(genesis), treasury, 10_001, admin);
    }

    function test_OneCallTellsWhatAGenesisHas() public {
        vm.prank(admin);
        shop.setItem(7, 1 ether, true);
        vm.startPrank(buyer);
        chog.approve(address(shop), PRICE + 1 ether);
        shop.buy(1795, 7);
        vm.stopPrank();

        uint256[] memory asked = new uint256[](3);
        asked[0] = GOLD_RING;
        asked[1] = 7;
        asked[2] = 99;
        bool[] memory has = shop.ownedMany(1795, asked);
        assertFalse(has[0]);
        assertTrue(has[1]);
        assertFalse(has[2]);
    }

    /// Whatever the price and the burned share, every unit paid is either burned or in the treasury.
    function testFuzz_EveryUnitPaidIsBurnedOrInTheTreasury(uint128 price, uint16 share) public {
        share = uint16(bound(share, 0, 10_000));
        address vault = makeAddr("vault");
        ChogShop other = new ChogShop(address(chog), address(genesis), vault, share, admin);
        vm.prank(admin);
        other.setItem(1, price, true);

        address payer = makeAddr("payer");
        chog.mint(payer, price);
        uint256 supplyBefore = chog.totalSupply();
        vm.startPrank(payer);
        chog.approve(address(other), price);
        other.buy(42, 1);
        vm.stopPrank();

        uint256 burned = supplyBefore - chog.totalSupply();
        assertEq(burned, uint256(price) * share / 10_000);
        assertEq(chog.balanceOf(vault) + burned, price);
        assertEq(chog.balanceOf(payer), 0);
        assertEq(chog.balanceOf(address(other)), 0);
    }
}
