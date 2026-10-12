// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Test} from "forge-std/Test.sol";
import {ChogShop} from "../src/ChogShop.sol";

interface IRealChog {
    function name() external view returns (string memory);
    function balanceOf(address account) external view returns (uint256);
    function totalSupply() external view returns (uint256);
    function allowance(address owner, address spender) external view returns (uint256);
    function nonces(address owner) external view returns (uint256);
    function DOMAIN_SEPARATOR() external view returns (bytes32);
}

/// The shop against the real $CHOG token and the real Chog Genesis collection, on a local copy
/// of Monad mainnet. Nothing is sent to the chain. It answers the question the unit tests
/// cannot: does the real token accept the permit and the burn the shop relies on?
///
///   MONAD_FORK=1 forge test --match-contract ChogShopForkTest -vv
contract ChogShopForkTest is Test {
    address private constant CHOG = 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777;
    address private constant GENESIS = 0xc96d31F8626c6D03Fae5dCD3d61e3FB9F4a73763;
    bytes32 private constant PERMIT_TYPEHASH =
        keccak256("Permit(address owner,address spender,uint256 value,uint256 nonce,uint256 deadline)");

    uint256 private constant BUYER_KEY = 0xC0FFEE;
    uint128 private constant PRICE = 100 ether;

    IRealChog private chog = IRealChog(CHOG);
    ChogShop private shop;
    address private treasury = makeAddr("treasury");
    address private buyer;

    function setUp() public {
        if (!vm.envOr("MONAD_FORK", false)) {
            vm.skip(true);
        }
        vm.createSelectFork(vm.rpcUrl("monad"));
        assertEq(block.chainid, 143, "not Monad mainnet");
        assertEq(chog.name(), "Chog");

        // Half of every payment is burned.
        shop = new ChogShop(CHOG, GENESIS, treasury, 5000, address(this));
        shop.setItem(1, PRICE, true);

        // A test key is given $CHOG on the local copy only.
        buyer = vm.addr(BUYER_KEY);
        deal(CHOG, buyer, 1000 ether);
        assertEq(chog.balanceOf(buyer), 1000 ether);
    }

    function _permit(uint256 value, uint256 deadline) private view returns (uint8 v, bytes32 r, bytes32 s) {
        bytes32 structHash =
            keccak256(abi.encode(PERMIT_TYPEHASH, buyer, address(shop), value, chog.nonces(buyer), deadline));
        return vm.sign(BUYER_KEY, keccak256(abi.encodePacked("\x19\x01", chog.DOMAIN_SEPARATOR(), structHash)));
    }

    function test_TheRealTokenAcceptsThePermitThePaymentAndTheBurn() public {
        uint256 supplyBefore = chog.totalSupply();
        uint256 deadline = block.timestamp + 1 hours;
        (uint8 v, bytes32 r, bytes32 s) = _permit(PRICE, deadline);

        // Token 1795 is a real Genesis.
        vm.prank(buyer);
        shop.buyWithPermit(1795, 1, deadline, v, r, s);

        assertTrue(shop.owned(1795, 1));
        assertEq(chog.balanceOf(buyer), 900 ether, "the buyer paid exactly the price: no transfer fee");
        assertEq(chog.balanceOf(treasury), 50 ether, "half reached the treasury");
        assertEq(supplyBefore - chog.totalSupply(), 50 ether, "half was burned");
        assertEq(chog.balanceOf(address(shop)), 0);
        assertEq(chog.allowance(buyer, address(shop)), 0);
        assertEq(chog.nonces(buyer), 1);
    }

    function test_ATokenIdThatIsNotAGenesisIsRefusedByTheRealCollection() public {
        uint256 deadline = block.timestamp + 1 hours;
        (uint8 v, bytes32 r, bytes32 s) = _permit(PRICE, deadline);
        vm.prank(buyer);
        vm.expectRevert();
        shop.buyWithPermit(1970, 1, deadline, v, r, s);
        assertFalse(shop.owned(1970, 1));
        assertEq(chog.balanceOf(buyer), 1000 ether);
    }
}
