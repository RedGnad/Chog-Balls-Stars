// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

/// @notice What the rental needs from $CHOG: an ERC-20 with burn.
interface IRentalToken {
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
    function burn(uint256 amount) external;
}

/// @notice What the rental needs from the Chog Genesis collection: who holds a token.
interface IRentalGenesis {
    function ownerOf(uint256 tokenId) external view returns (address);
}

/// @title ChogRental
/// @notice A holder offers a Chog Genesis for rent at a price per day in $CHOG, and anyone
/// pays to play it for a number of days. The Genesis never moves and nothing is locked: a
/// rental is only a right the game reads. Each payment goes straight from the renter to the
/// holder, minus a share that is burned. Several players can rent the same Genesis at once.
/// A rental that was paid runs to its end even if the Genesis is sold meanwhile; an offer made
/// by a former holder cannot be rented any more. Not upgradeable, no owner: the token, the
/// collection and the burned share are fixed at deployment.
contract ChogRental {
    struct Offer {
        address lender; // who made the offer, and who is paid; no offer when zero
        uint128 pricePerDay; // in $CHOG, 18 decimals
    }

    uint256 private constant BASIS_POINTS = 10_000;
    /// @notice A renter is never paid up further than this from now.
    uint256 public constant MAX_DAYS = 30;

    IRentalToken public immutable chog;
    IRentalGenesis public immutable genesis;
    /// @notice Share of every payment that is burned, in basis points (10_000 is all of it).
    uint16 public immutable burnedShare;

    mapping(uint256 tokenId => Offer) public offers;
    /// @notice Until when an address may play a Genesis, in seconds since 1970; 0 if it never rented it.
    mapping(uint256 tokenId => mapping(address renter => uint64)) public rentedUntil;

    // What lets anyone list the offers and an address's rentals with one read, without an
    // indexer: the token ids that have an offer on record (and where each sits in that list,
    // plus one), and every Genesis an address ever rented.
    uint256[] private _offered;
    mapping(uint256 tokenId => uint256) private _offeredAt;
    mapping(address renter => uint256[]) private _rentedBy;

    event Offered(uint256 indexed tokenId, address indexed lender, uint256 pricePerDay);
    event Withdrawn(uint256 indexed tokenId);
    event Rented(
        uint256 indexed tokenId, address indexed renter, address indexed lender, uint256 numberOfDays, uint256 paid, uint64 until
    );

    error ZeroAddress();
    error ShareTooLarge();
    error NotTheHolder(uint256 tokenId);
    error PriceIsZero();
    error NotOffered(uint256 tokenId);
    error PriceWentUp(uint256 pricePerDay);
    error BadDuration(uint256 numberOfDays);
    error OwnGenesis(uint256 tokenId);
    error PaymentFailed();

    constructor(address chog_, address genesis_, uint16 burnedShare_) {
        if (chog_ == address(0) || genesis_ == address(0)) revert ZeroAddress();
        if (burnedShare_ > BASIS_POINTS) revert ShareTooLarge();
        chog = IRentalToken(chog_);
        genesis = IRentalGenesis(genesis_);
        burnedShare = burnedShare_;
    }

    /// @notice The holder of a Genesis offers it for rent, or changes its price.
    function offer(uint256 tokenId, uint128 pricePerDay) external {
        if (genesis.ownerOf(tokenId) != msg.sender) revert NotTheHolder(tokenId);
        if (pricePerDay == 0) revert PriceIsZero();
        offers[tokenId] = Offer(msg.sender, pricePerDay);
        if (_offeredAt[tokenId] == 0) {
            _offered.push(tokenId);
            _offeredAt[tokenId] = _offered.length;
        }
        emit Offered(tokenId, msg.sender, pricePerDay);
    }

    /// @notice Takes an offer back: its author, or whoever holds the Genesis now. Rentals
    /// already paid run to their end.
    function withdraw(uint256 tokenId) external {
        if (offers[tokenId].lender != msg.sender && genesis.ownerOf(tokenId) != msg.sender) revert NotTheHolder(tokenId);
        delete offers[tokenId];
        uint256 position = _offeredAt[tokenId];
        if (position != 0) {
            // The last of the list takes the freed place.
            uint256 last = _offered[_offered.length - 1];
            _offered[position - 1] = last;
            _offeredAt[last] = position;
            _offered.pop();
            delete _offeredAt[tokenId];
        }
        emit Withdrawn(tokenId);
    }

    /// @notice Every Genesis that can be rented now, with its price per day and who is paid.
    function standingOffers()
        external
        view
        returns (uint256[] memory tokenIds, uint128[] memory pricesPerDay, address[] memory lenders)
    {
        uint256 known = _offered.length;
        tokenIds = new uint256[](known);
        pricesPerDay = new uint128[](known);
        lenders = new address[](known);
        uint256 count;
        for (uint256 index; index < known; ++index) {
            (bool offered, uint128 pricePerDay, address lender) = offerOf(_offered[index]);
            if (!offered) continue;
            tokenIds[count] = _offered[index];
            pricesPerDay[count] = pricePerDay;
            lenders[count] = lender;
            ++count;
        }
        // Shorten the three lists to what stands.
        assembly ("memory-safe") {
            mstore(tokenIds, count)
            mstore(pricesPerDay, count)
            mstore(lenders, count)
        }
    }

    /// @notice The Genesis an address may play as their renter now, and until when.
    function rentalsOf(address renter) external view returns (uint256[] memory tokenIds, uint64[] memory until) {
        uint256[] storage rented = _rentedBy[renter];
        uint256 known = rented.length;
        tokenIds = new uint256[](known);
        until = new uint64[](known);
        uint256 count;
        for (uint256 index; index < known; ++index) {
            uint64 paidUntil = rentedUntil[rented[index]][renter];
            if (paidUntil <= block.timestamp) continue;
            tokenIds[count] = rented[index];
            until[count] = paidUntil;
            ++count;
        }
        assembly ("memory-safe") {
            mstore(tokenIds, count)
            mstore(until, count)
        }
    }

    /// @notice Whether a Genesis can be rented now, at what price, and who is paid.
    function offerOf(uint256 tokenId) public view returns (bool offered, uint128 pricePerDay, address lender) {
        Offer memory made = offers[tokenId];
        // An offer only stands while its author holds the Genesis.
        offered = made.lender != address(0) && genesis.ownerOf(tokenId) == made.lender;
        return (offered, made.pricePerDay, made.lender);
    }

    /// @notice Whether an address may play a Genesis as its renter at this moment.
    function isRenting(uint256 tokenId, address renter) external view returns (bool) {
        return rentedUntil[tokenId][renter] > block.timestamp;
    }

    /// @notice Rents a Genesis for a number of days, added after the days already paid. The
    /// renter must have approved this contract for the cost. `maxPricePerDay` is the price the
    /// renter saw: the rental is refused if the holder raised it meanwhile.
    function rent(uint256 tokenId, uint256 numberOfDays, uint128 maxPricePerDay) external {
        (bool offered, uint128 pricePerDay, address lender) = offerOf(tokenId);
        if (!offered) revert NotOffered(tokenId);
        if (pricePerDay > maxPricePerDay) revert PriceWentUp(pricePerDay);
        if (lender == msg.sender) revert OwnGenesis(tokenId);

        uint64 paidUntil = rentedUntil[tokenId][msg.sender];
        uint256 from = paidUntil > block.timestamp ? paidUntil : block.timestamp;
        uint256 until = from + numberOfDays * 1 days;
        if (numberOfDays == 0 || until > block.timestamp + MAX_DAYS * 1 days) revert BadDuration(numberOfDays);
        if (paidUntil == 0) _rentedBy[msg.sender].push(tokenId);
        rentedUntil[tokenId][msg.sender] = uint64(until);

        uint256 cost = uint256(pricePerDay) * numberOfDays;
        uint256 burned = cost * burnedShare / BASIS_POINTS;
        if (burned > 0) {
            _pull(msg.sender, address(this), burned);
            chog.burn(burned);
        }
        if (cost > burned) {
            _pull(msg.sender, lender, cost - burned);
        }

        emit Rented(tokenId, msg.sender, lender, numberOfDays, cost, uint64(until));
    }

    // transferFrom that accepts both tokens that return true and tokens that return nothing.
    function _pull(address from, address to, uint256 amount) private {
        (bool ok, bytes memory answer) =
            address(chog).call(abi.encodeCall(IRentalToken.transferFrom, (from, to, amount)));
        if (!ok || (answer.length != 0 && !abi.decode(answer, (bool)))) revert PaymentFailed();
    }
}
