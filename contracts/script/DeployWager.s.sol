// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

import {Script, console2} from "forge-std/Script.sol";
import {ChogWager} from "../src/ChogWager.sol";

/// Deploys version 2 of the wager alone on Monad mainnet (22 October 2026, decided by the owner): of
/// what the losers of a match stake, BURNED_SHARE (in ten thousandths, default 500) is burned and
/// GENESIS_SHARE (default 500) is split between the wallets that played a Chog Genesis in the match;
/// version 1 burned a tenth and paid no Genesis share. The winners' gain is the same. The deploying
/// wallet becomes the owner (it can only replace the referee); REFEREE is the address of the key the
/// game services sign with, the same as for version 1. Then set WAGER_ADDRESS of the services to the
/// printed address; matches staked on version 1 still settle there (their pages read it by address).
///
///   REFEREE=0x... [BURNED_SHARE=500] [GENESIS_SHARE=500] forge script script/DeployWager.s.sol \
///       --rpc-url monad --account <keystore name> --broadcast
contract DeployWager is Script {
    uint256 private constant MONAD_MAINNET = 143;
    // The $CHOG token on Monad mainnet.
    address private constant CHOG = 0x350035555E10d9AfAF1566AaebfCeD5BA6C27777;

    function run() external {
        require(block.chainid == MONAD_MAINNET, "this script deploys on Monad mainnet (chain 143) only");
        address referee = vm.envAddress("REFEREE");
        require(referee != address(0), "REFEREE must be the address of the services' referee key");
        uint16 burnedShare = uint16(vm.envOr("BURNED_SHARE", uint256(500)));
        uint16 genesisShare = uint16(vm.envOr("GENESIS_SHARE", uint256(500)));
        require(uint256(burnedShare) + genesisShare <= 10000, "BURNED_SHARE plus GENESIS_SHARE is at most 10000");
        require(CHOG.code.length > 0, "the $CHOG token has no code here");

        vm.startBroadcast();
        (, address deployer,) = vm.readCallers();
        require(referee != deployer, "the referee and the deploying wallet must be two different keys");
        ChogWager wager = new ChogWager(CHOG, referee, burnedShare, genesisShare, deployer);
        vm.stopBroadcast();

        console2.log("WAGER_ADDRESS", address(wager));
        console2.log("OWNER", deployer);
        console2.log("REFEREE", referee);
        console2.log("BURNED_SHARE", uint256(burnedShare));
        console2.log("GENESIS_SHARE", uint256(genesisShare));
    }
}
