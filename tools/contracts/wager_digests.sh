#!/bin/sh
# Prints the three digests the referee of ChogWager signs, computed by cast for fixed values:
# the reference the services' own computation is tested against (server/test/referee.test.js).
# Run it again and compare if the contract's digests ever change.
set -e
CAST="${CAST:-$HOME/.foundry/bin/cast}"
WAGER=0x00000000000000000000000000000000000000aa
PLAYER=0x00000000000000000000000000000000000000bb
MATCH_KEY=$("$CAST" keccak "0:eu:room~1234")
echo "matchKey $MATCH_KEY"
echo "ticket   $("$CAST" keccak "$("$CAST" abi-encode "f(bytes32,uint256,address,bytes32,address,uint8,uint256,uint256)" \
  "$("$CAST" keccak CHOG_WAGER_TICKET)" 143 $WAGER "$MATCH_KEY" $PLAYER 1 25000000000000000000 1790000120)")"
echo "result   $("$CAST" keccak "$("$CAST" abi-encode "f(bytes32,uint256,address,bytes32,uint8)" \
  "$("$CAST" keccak CHOG_WAGER_RESULT)" 143 $WAGER "$MATCH_KEY" 1)")"
echo "void     $("$CAST" keccak "$("$CAST" abi-encode "f(bytes32,uint256,address,bytes32)" \
  "$("$CAST" keccak CHOG_WAGER_VOID)" 143 $WAGER "$MATCH_KEY")")"
