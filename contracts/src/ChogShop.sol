// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

/// @notice What the shop needs from $CHOG: an ERC-20 with EIP-2612 permit and burn.
interface IChogToken {
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
    function permit(address owner, address spender, uint256 value, uint256 deadline, uint8 v, bytes32 r, bytes32 s)
        external;
    function burn(uint256 amount) external;
}

/// @notice What the shop needs from the Chog Genesis collection: whether a token exists.
interface IGenesis {
    function ownerOf(uint256 tokenId) external view returns (address);
}

/// @title ChogShop
/// @notice Sells items for $CHOG. An item is bought for one Chog Genesis and stays with that
/// token id: it follows the Genesis when it is sold, and anyone may buy it for any Genesis.
/// The game reads `owned` to know what a Genesis has. Not upgradeable; the token, the
/// collection, the treasury and the burned share are fixed at deployment.
contract ChogShop {
    struct Item {
        uint128 price; // in $CHOG, 18 decimals
        bool forSale;
    }

    uint256 private constant BASIS_POINTS = 10_000;

    IChogToken public immutable chog;
    IGenesis public immutable genesis;
    /// @notice Receives what is not burned.
    address public immutable treasury;
    /// @notice Share of every payment that is burned, in basis points (10_000 is all of it).
    uint16 public immutable burnedShare;

    /// @notice May list items and set prices; nothing else.
    address public owner;

    mapping(uint256 itemId => Item) public items;
    mapping(uint256 tokenId => mapping(uint256 itemId => bool)) public owned;

    event ItemSet(uint256 indexed itemId, uint256 price, bool forSale);
    event Purchased(address indexed buyer, uint256 indexed tokenId, uint256 indexed itemId, uint256 price);
    event OwnerChanged(address indexed previousOwner, address indexed newOwner);

    error NotOwner();
    error ZeroAddress();
    error ShareTooLarge();
    error NotForSale(uint256 itemId);
    error AlreadyOwned(uint256 tokenId, uint256 itemId);
    error PaymentFailed();

    modifier onlyOwner() {
        if (msg.sender != owner) revert NotOwner();
        _;
    }

    constructor(address chog_, address genesis_, address treasury_, uint16 burnedShare_, address owner_) {
        if (chog_ == address(0) || genesis_ == address(0) || treasury_ == address(0) || owner_ == address(0)) {
            revert ZeroAddress();
        }
        if (burnedShare_ > BASIS_POINTS) revert ShareTooLarge();
        chog = IChogToken(chog_);
        genesis = IGenesis(genesis_);
        treasury = treasury_;
        burnedShare = burnedShare_;
        owner = owner_;
        emit OwnerChanged(address(0), owner_);
    }

    /// @notice Lists an item, changes its price, or takes it off sale. What was bought stays bought.
    function setItem(uint256 itemId, uint128 price, bool forSale) external onlyOwner {
        items[itemId] = Item(price, forSale);
        emit ItemSet(itemId, price, forSale);
    }

    function transferOwnership(address newOwner) external onlyOwner {
        if (newOwner == address(0)) revert ZeroAddress();
        emit OwnerChanged(owner, newOwner);
        owner = newOwner;
    }

    /// @notice Buys an item for a Genesis. The buyer must have approved the shop for the price.
    function buy(uint256 tokenId, uint256 itemId) external {
        _buy(msg.sender, tokenId, itemId);
    }

    /// @notice Buys an item for a Genesis with a signed EIP-2612 permit for exactly the price,
    /// so a key-pair wallet pays in one transaction.
    function buyWithPermit(uint256 tokenId, uint256 itemId, uint256 deadline, uint8 v, bytes32 r, bytes32 s) external {
        // Someone may have submitted this permit first: what matters is the allowance, which
        // the payment itself checks.
        try chog.permit(msg.sender, address(this), items[itemId].price, deadline, v, r, s) {} catch {}
        _buy(msg.sender, tokenId, itemId);
    }

    /// @notice Which of these items a Genesis has, in one call.
    function ownedMany(uint256 tokenId, uint256[] calldata itemIds) external view returns (bool[] memory has) {
        has = new bool[](itemIds.length);
        for (uint256 i = 0; i < itemIds.length; ++i) {
            has[i] = owned[tokenId][itemIds[i]];
        }
    }

    function _buy(address buyer, uint256 tokenId, uint256 itemId) private {
        Item memory item = items[itemId];
        if (!item.forSale) revert NotForSale(itemId);
        if (owned[tokenId][itemId]) revert AlreadyOwned(tokenId, itemId);
        // Reverts for a token id that is not a Genesis.
        genesis.ownerOf(tokenId);

        owned[tokenId][itemId] = true;

        uint256 burned = uint256(item.price) * burnedShare / BASIS_POINTS;
        if (burned > 0) {
            _pull(buyer, address(this), burned);
            chog.burn(burned);
        }
        if (item.price > burned) {
            _pull(buyer, treasury, item.price - burned);
        }

        emit Purchased(buyer, tokenId, itemId, item.price);
    }

    // transferFrom that accepts both tokens that return true and tokens that return nothing.
    function _pull(address from, address to, uint256 amount) private {
        (bool ok, bytes memory answer) =
            address(chog).call(abi.encodeCall(IChogToken.transferFrom, (from, to, amount)));
        if (!ok || (answer.length != 0 && !abi.decode(answer, (bool)))) revert PaymentFailed();
    }
}
