// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

/// @title TestChog
/// @notice A stand-in for $CHOG on Monad's testnet, where the real token does not exist. It has
/// what the game's contracts use of $CHOG (an ERC-20 with 18 decimals and a burn of the caller's
/// own tokens) and a tap: any address may take CLAIM_AMOUNT once a day, so a player can try a
/// staked match without being sent anything. It is worth nothing and must never be deployed on
/// mainnet: the deployment script refuses any chain but the testnet.
contract TestChog {
    string public constant name = "Test CHOG";
    string public constant symbol = "tCHOG";
    uint8 public constant decimals = 18;

    uint256 public constant CLAIM_AMOUNT = 1000 ether;
    uint256 public constant CLAIM_EVERY = 1 days;

    uint256 public totalSupply;
    mapping(address => uint256) public balanceOf;
    mapping(address => mapping(address => uint256)) public allowance;
    /// When each address last used the tap.
    mapping(address => uint256) public claimedAt;

    event Transfer(address indexed from, address indexed to, uint256 amount);
    event Approval(address indexed owner, address indexed spender, uint256 amount);

    error ClaimTooSoon(uint256 nextClaimAt);
    error InsufficientBalance();
    error InsufficientAllowance();

    /// Gives the caller CLAIM_AMOUNT, at most once every CLAIM_EVERY.
    function claim() external {
        uint256 last = claimedAt[msg.sender];
        if (last != 0 && block.timestamp < last + CLAIM_EVERY) revert ClaimTooSoon(last + CLAIM_EVERY);
        claimedAt[msg.sender] = block.timestamp;
        totalSupply += CLAIM_AMOUNT;
        balanceOf[msg.sender] += CLAIM_AMOUNT;
        emit Transfer(address(0), msg.sender, CLAIM_AMOUNT);
    }

    function approve(address spender, uint256 amount) external returns (bool) {
        allowance[msg.sender][spender] = amount;
        emit Approval(msg.sender, spender, amount);
        return true;
    }

    function transfer(address to, uint256 amount) external returns (bool) {
        _move(msg.sender, to, amount);
        return true;
    }

    function transferFrom(address from, address to, uint256 amount) external returns (bool) {
        uint256 allowed = allowance[from][msg.sender];
        if (allowed != type(uint256).max) {
            if (allowed < amount) revert InsufficientAllowance();
            allowance[from][msg.sender] = allowed - amount;
        }
        _move(from, to, amount);
        return true;
    }

    /// Destroys some of the caller's own tokens, as $CHOG's burn does.
    function burn(uint256 amount) external {
        if (balanceOf[msg.sender] < amount) revert InsufficientBalance();
        balanceOf[msg.sender] -= amount;
        totalSupply -= amount;
        emit Transfer(msg.sender, address(0), amount);
    }

    function _move(address from, address to, uint256 amount) private {
        if (balanceOf[from] < amount) revert InsufficientBalance();
        balanceOf[from] -= amount;
        balanceOf[to] += amount;
        emit Transfer(from, to, amount);
    }
}
