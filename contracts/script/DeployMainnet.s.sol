// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Script, console2} from "forge-std/Script.sol";
import {ChogWager} from "../src/ChogWager.sol";
import {ChogRental} from "../src/ChogRental.sol";
import {ChogShop} from "../src/ChogShop.sol";

/// Deploys, on Monad mainnet only, the three contracts paid in the real $CHOG: the wager on a match,
/// the rental of a Genesis and the shop of items for a Genesis (decided by the owner on 16 October
/// 2026: build on mainnet directly). The deploying wallet becomes the owner of the wager (it can only
/// replace the referee) and of the shop (it sets the prices); the rental has no owner. The referee is
/// the address of the key the game services sign with (REFEREE). BURNED_SHARE (in ten thousandths,
/// default 1000: a tenth) of what the losers of a match stake, of each rental and of each purchase is
/// burned. The shop's other $CHOG goes to TREASURY (default: the deploying wallet), and item 1, the
/// second form of a Genesis's attack, is put on sale at ITEM_PRICE whole $CHOG (default 100).
///
///   REFEREE=0x... [BURNED_SHARE=1000] [TREASURY=0x...] [ITEM_PRICE=100] forge script \
///       script/DeployMainnet.s.sol --rpc-url monad --account <keystore name> --broadcast
///
/// The addresses are printed and kept by forge in broadcast/DeployMainnet.s.sol/143/run-latest.json.
contract DeployMainnet is Script {
    uint256 private constant MONAD_MAINNET = 143;
    // The $CHOG token and the Chog Genesis collection on Monad mainnet.
    address private constant CHOG = 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777;
    address private constant GENESIS = 0xc96d31F8626c6D03Fae5dCD3d61e3FB9F4a73763;
    uint256 private constant SECOND_FORM = 1;

    function run() external {
        require(block.chainid == MONAD_MAINNET, "this script deploys on Monad mainnet (chain 143) only");
        address referee = vm.envAddress("REFEREE");
        require(referee != address(0), "REFEREE must be the address of the services' referee key");
        uint16 burnedShare = uint16(vm.envOr("BURNED_SHARE", uint256(1000)));
        require(burnedShare <= 10000, "BURNED_SHARE is in ten thousandths: 0 to 10000");
        uint256 itemPrice = vm.envOr("ITEM_PRICE", uint256(100));
        require(CHOG.code.length > 0 && GENESIS.code.length > 0, "the $CHOG token or the Genesis collection has no code here");

        vm.startBroadcast();
        (, address deployer,) = vm.readCallers();
        require(referee != deployer, "the referee and the deploying wallet must be two different keys");
        address treasury = vm.envOr("TREASURY", deployer);
        // (Version 2 of the wager takes a Genesis players' share, none here: script/DeployWager.s.sol deploys it alone.)
        ChogWager wager = new ChogWager(CHOG, referee, burnedShare, 0, deployer);
        ChogRental rental = new ChogRental(CHOG, GENESIS, burnedShare);
        ChogShop shop = new ChogShop(CHOG, GENESIS, treasury, burnedShare, deployer);
        shop.setItem(SECOND_FORM, uint128(itemPrice * 1 ether), true);
        vm.stopBroadcast();

        console2.log("WAGER_ADDRESS", address(wager));
        console2.log("RENTAL_ADDRESS", address(rental));
        console2.log("SHOP_ADDRESS", address(shop));
        console2.log("OWNER", deployer);
        console2.log("REFEREE", referee);
        console2.log("TREASURY", treasury);
        console2.log("BURNED_SHARE", uint256(burnedShare));
        console2.log("ITEM_PRICE_CHOG", itemPrice);
    }
}
