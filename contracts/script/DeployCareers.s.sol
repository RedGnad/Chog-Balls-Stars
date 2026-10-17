// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Script, console2} from "forge-std/Script.sol";
import {ChogCareers} from "../src/ChogCareers.sol";

/// Deploys, on Monad mainnet only, the register of the Genesis careers. The deploying wallet becomes
/// its owner (it can only replace the referee and hand the contract over). The referee is the
/// address of the key the game services send the records with (REFEREE); it pays the gas of every
/// record, so it needs MON.
///
///   REFEREE=0x... forge script script/DeployCareers.s.sol --rpc-url monad --account <keystore name> --broadcast
///
/// The address is printed and kept by forge in broadcast/DeployCareers.s.sol/143/run-latest.json.
contract DeployCareers is Script {
    uint256 private constant MONAD_MAINNET = 143;

    function run() external {
        require(block.chainid == MONAD_MAINNET, "this script deploys on Monad mainnet (chain 143) only");
        address referee = vm.envAddress("REFEREE");
        require(referee != address(0), "REFEREE must be the address of the services' referee key");

        vm.startBroadcast();
        (, address deployer,) = vm.readCallers();
        require(referee != deployer, "the referee and the deploying wallet must be two different keys");
        ChogCareers careers = new ChogCareers(referee, deployer);
        vm.stopBroadcast();

        console2.log("CAREERS_ADDRESS", address(careers));
        console2.log("OWNER", deployer);
        console2.log("REFEREE", referee);
    }
}
