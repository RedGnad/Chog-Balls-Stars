#!/bin/sh
# What the rehearsal scripts share: acting on a LOCAL COPY of Monad mainnet (an anvil process on
# this computer). Sourced, not run. Nothing here can reach the real chain: `require_copy` stops
# the script unless the endpoint is anvil on chain 143, and every account that acts is one anvil
# unlocks or impersonates.
RPC=${RPC:-http://127.0.0.1:8545}
CHOG=0x350035555E10d9AfAF1566AaebfCeD5BA6C27777
GENESIS=0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763
# The first two accounts every anvil starts with.
FIRST_ACCOUNT=0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266
SECOND_ACCOUNT=0x70997970C51812dc3A010C7d01b50e0d17dc79C8
PLENTY_OF_MON=0x56BC75E2D63100000

require_copy() {
  if [ "$(cast chain-id --rpc-url "$RPC")" != "143" ]; then
    echo "the endpoint at $RPC is not a copy of Monad mainnet" >&2
    exit 1
  fi
  if ! cast rpc anvil_nodeInfo --rpc-url "$RPC" >/dev/null 2>&1; then
    echo "the endpoint at $RPC is not anvil: this script only acts on a local copy" >&2
    exit 1
  fi
}

lower() { echo "$1" | tr 'A-F' 'a-f'; }

give_mon() { # give_mon <address>
  cast rpc anvil_setBalance "$1" "$PLENTY_OF_MON" --rpc-url "$RPC" >/dev/null
}

# $CHOG: write the balance where the token keeps it. The token is an upgradeable ERC-20: its
# balances are a mapping at OpenZeppelin's namespaced slot, or at a plain low slot.
give_chog() { # give_chog <address> <whole $CHOG>
  wanted=$(cast to-wei "$2")
  for base in 0x52c63247e1f47db19d5ce0460030c497f067ca4cebf71ba98eeadabe20bace00 0 1 2 3 4 5; do
    slot=$(cast index address "$1" "$base")
    before=$(cast storage "$CHOG" "$slot" --rpc-url "$RPC")
    cast rpc anvil_setStorageAt "$CHOG" "$slot" "$(cast to-uint256 "$wanted")" --rpc-url "$RPC" >/dev/null
    if [ "$(cast call "$CHOG" "balanceOf(address)(uint256)" "$1" --rpc-url "$RPC" | cut -d' ' -f1)" = "$wanted" ]; then
      return 0
    fi
    cast rpc anvil_setStorageAt "$CHOG" "$slot" "$before" --rpc-url "$RPC" >/dev/null
  done
  echo "could not give \$CHOG to $1 on the copy" >&2
  exit 1
}

holder_of() { # holder_of <token id>
  cast call "$GENESIS" "ownerOf(uint256)(address)" "$1" --rpc-url "$RPC"
}

# Sends one transaction as any account, which anvil is told to impersonate for its duration.
send_as() { # send_as <account> <contract> <signature> [arguments...]
  account=$1
  shift
  give_mon "$account"
  cast rpc anvil_impersonateAccount "$account" --rpc-url "$RPC" >/dev/null
  cast send "$@" --rpc-url "$RPC" --unlocked --from "$account" >/dev/null
  cast rpc anvil_stopImpersonatingAccount "$account" --rpc-url "$RPC" >/dev/null
}

# The Genesis: its real holder hands it over, on the copy only.
give_genesis() { # give_genesis <address> <token id>
  holder=$(holder_of "$2")
  if [ "$(lower "$holder")" != "$(lower "$1")" ]; then
    send_as "$holder" "$GENESIS" "transferFrom(address,address,uint256)" "$holder" "$1" "$2"
  fi
}
