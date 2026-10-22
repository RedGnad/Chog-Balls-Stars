// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Script, console2} from "forge-std/Script.sol";
import {ChogWager} from "../src/ChogWager.sol";
import {TestChog} from "../src/testnet/TestChog.sol";

/// Deploys, on Monad's testnet only, the stand-in for $CHOG (src/testnet/TestChog.sol) and the
/// wager on it. The deploying wallet becomes the wager's owner; the referee is the address of
/// the key the game services sign with (REFEREE). A tenth of what the losers stake is burned,
/// as in the rehearsals. BENCH_WALLETS (addresses separated by commas) are the throwaway
/// wallets of the headless test players: each is sent BENCH_MON (default 0.25 MON) for its gas.
///
///   REFEREE=0x... BENCH_WALLETS=0x...,0x... forge script script/DeployTestnet.s.sol \
///       --rpc-url monad_testnet --account <keystore name> --broadcast
///
/// The addresses are printed and kept by forge in broadcast/DeployTestnet.s.sol/10143/run-latest.json.
contract DeployTestnet is Script {
    uint256 private constant MONAD_TESTNET = 10143;
    uint16 private constant BURNED_SHARE = 1000;

    function run() external {
        require(block.chainid == MONAD_TESTNET, "this script deploys on Monad testnet (chain 10143) only");
        address referee = vm.envAddress("REFEREE");
        require(referee != address(0), "REFEREE must be the address of the services' referee key");
        address[] memory bench = vm.envOr("BENCH_WALLETS", ",", new address[](0));
        uint256 benchMon = vm.envOr("BENCH_MON", uint256(0.25 ether));

        vm.startBroadcast();
        (, address deployer,) = vm.readCallers();
        require(referee != deployer, "the referee and the deploying wallet must be two different keys");
        TestChog chog = new TestChog();
        ChogWager wager = new ChogWager(address(chog), referee, BURNED_SHARE, 0, deployer);
        for (uint256 index; index < bench.length; ++index) {
            payable(bench[index]).transfer(benchMon);
        }
        vm.stopBroadcast();

        console2.log("TESTNET_CHOG", address(chog));
        console2.log("TESTNET_WAGER", address(wager));
        console2.log("WAGER_OWNER", deployer);
        console2.log("WAGER_REFEREE", referee);
        for (uint256 index; index < bench.length; ++index) {
            console2.log("BENCH_WALLET", bench[index]);
        }
    }
}
