#!/bin/sh
# Prints the three calls the game sends to ChogWager, encoded by cast for fixed values: the
# reference the game's own encoding is tested against (game/Assets/Chog/Match/Tests).
set -e
CAST="${CAST:-$HOME/.foundry/bin/cast}"
KEY=0x3ed1720486303eb00d7fa1f4b59ffac849dc5553fe292b20555ae44e3387317a
SIGNATURE=0x$(printf 'ab%.0s' $(seq 1 64))1b
echo "stake  $("$CAST" calldata "stake(bytes32,uint8,uint256,uint256,bytes)" $KEY 1 25000000000000000000 1790000120 "$SIGNATURE")"
echo "settle $("$CAST" calldata "settle(bytes32,uint8,bytes)" $KEY 1 "$SIGNATURE")"
echo "void   $("$CAST" calldata "voidMatch(bytes32,bytes)" $KEY "$SIGNATURE")"
