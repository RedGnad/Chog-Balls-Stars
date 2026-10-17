// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Script, console2} from "forge-std/Script.sol";
import {ChogRental} from "../src/ChogRental.sol";
import {ChogCareers} from "../src/ChogCareers.sol";

/// Deploys, on Monad mainnet only and in one run, the second version of the rental (one renter at a
/// time, the payment held and earned by the second, the right ending with the holding) and the
/// register of the Genesis careers. The wager and the shop of 16 October stay as they are. The
/// deploying wallet owns the careers (it can only replace the referee); the rental has no owner.
/// The referee is the address of the key the game services send the records with (REFEREE); it
/// pays the gas of every record, so it needs MON.
///
///   REFEREE=0x... [BURNED_SHARE=1000] forge script script/DeployRentalAndCareers.s.sol \
///     --rpc-url monad --account <keystore name> --broadcast
///
/// The addresses are printed and kept by forge in broadcast/DeployRentalAndCareers.s.sol/143/run-latest.json.
contract DeployRentalAndCareers is Script {
    uint256 private constant MONAD_MAINNET = 143;
    // The $CHOG token and the Chog Genesis collection on Monad mainnet.
    address private constant CHOG = 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777;
    address private constant GENESIS = 0xc96d31F8626c6D03Fae5dCD3d61e3FB9F4a73763;

    function run() external {
        require(block.chainid == MONAD_MAINNET, "this script deploys on Monad mainnet (chain 143) only");
        address referee = vm.envAddress("REFEREE");
        require(referee != address(0), "REFEREE must be the address of the services' referee key");
        uint16 burnedShare = uint16(vm.envOr("BURNED_SHARE", uint256(1000)));
        require(burnedShare <= 10000, "BURNED_SHARE is in ten thousandths: 0 to 10000");
        require(CHOG.code.length > 0 && GENESIS.code.length > 0, "the $CHOG token or the Genesis collection has no code here");

        vm.startBroadcast();
        (, address deployer,) = vm.readCallers();
        require(referee != deployer, "the referee and the deploying wallet must be two different keys");
        ChogRental rental = new ChogRental(CHOG, GENESIS, burnedShare);
        ChogCareers careers = new ChogCareers(referee, deployer);
        vm.stopBroadcast();

        console2.log("RENTAL_ADDRESS", address(rental));
        console2.log("CAREERS_ADDRESS", address(careers));
        console2.log("OWNER", deployer);
        console2.log("REFEREE", referee);
        console2.log("BURNED_SHARE", uint256(burnedShare));
    }
}
