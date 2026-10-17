// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

/// @notice What the rental needs from $CHOG: an ERC-20 with burn.
interface IRentalToken {
    function transfer(address to, uint256 amount) external returns (bool);
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
    function burn(uint256 amount) external;
}

/// @notice What the rental needs from the Chog Genesis collection: who holds a token.
interface IRentalGenesis {
    function ownerOf(uint256 tokenId) external view returns (address);
}

/// @title ChogRental
/// @notice A holder offers a Chog Genesis for rent at a price per day in $CHOG, and one player
/// at a time pays to play it for a number of days. The Genesis never moves and nothing is
/// locked: a rental is only a right the game reads, and it lasts only while the holder who was
/// paid still holds the Genesis, as the reference implementation of ERC-4907 and delegate.xyz's
/// guidance do (docs/PRODUCT-REVIEW.md). The payment is held here and earned by the holder by
/// the second (reNFT's formula: seconds x price / 86400); once the holder no longer holds the
/// Genesis anyone may close the rental, and the renter gets back the time not delivered. The
/// renter may also end a rental early and get the rest back. Every party takes its own money
/// out (`claim`), so a transfer refused to one never blocks another. A share of what the holder
/// earns is burned when it is claimed; refunds are whole. Not upgradeable, no owner: the token,
/// the collection and the burned share are fixed at deployment.
contract ChogRental {
    struct Offer {
        address lender; // who made the offer, and who is paid; no offer when zero
        uint128 pricePerDay; // in $CHOG, 18 decimals
    }

    /// @notice The running rental of a Genesis; no rental when `renter` is zero.
    struct Rental {
        address renter;
        uint64 until; // the second the paid time ends
        address lender; // the holder who is paid; the right lasts while they hold the Genesis
        uint64 settledAt; // the second up to which the holder's part was moved to their earnings
        uint256 escrow; // paid and not yet earned: spread evenly from settledAt to until
    }

    uint256 private constant BASIS_POINTS = 10_000;
    /// @notice A renter is never paid up further than this from now.
    uint256 public constant MAX_DAYS = 30;

    IRentalToken public immutable chog;
    IRentalGenesis public immutable genesis;
    /// @notice Share of what a holder earns that is burned when claimed, in basis points.
    uint16 public immutable burnedShare;

    mapping(uint256 tokenId => Offer) public offers;
    mapping(uint256 tokenId => Rental) private _rentals;
    /// @notice What holders earned and have not claimed yet, before the burned share.
    mapping(address lender => uint256) public earnings;
    /// @notice What renters are owed back and have not claimed yet.
    mapping(address renter => uint256) public refunds;

    // What lets anyone list the offers and an address's rentals with one read, without an
    // indexer: the token ids that have an offer on record (and where each sits in that list,
    // plus one), and every Genesis an address ever rented.
    uint256[] private _offered;
    mapping(uint256 tokenId => uint256) private _offeredAt;
    mapping(address renter => uint256[]) private _rentedBy;
    mapping(address renter => mapping(uint256 tokenId => bool)) private _listedFor;

    event Offered(uint256 indexed tokenId, address indexed lender, uint256 pricePerDay);
    event Withdrawn(uint256 indexed tokenId);
    event Rented(
        uint256 indexed tokenId, address indexed renter, address indexed lender, uint256 numberOfDays, uint256 paid, uint64 until
    );
    /// @notice Part of a rental's payment became the holder's.
    event Earned(uint256 indexed tokenId, address indexed lender, uint256 amount);
    /// @notice A rental ended: run out, ended by its renter, or closed after a sale. `refunded`
    /// is what its renter gets back.
    event Ended(uint256 indexed tokenId, address indexed renter, address indexed lender, uint256 refunded);
    /// @notice An address took out its money: `paid` sent to it, `burned` burned from its earnings.
    event Claimed(address indexed account, uint256 paid, uint256 burned);

    error ZeroAddress();
    error ShareTooLarge();
    error NotTheHolder(uint256 tokenId);
    error PriceIsZero();
    error NotOffered(uint256 tokenId);
    error PriceWentUp(uint256 pricePerDay);
    error BadDuration(uint256 numberOfDays);
    error OwnGenesis(uint256 tokenId);
    error AlreadyRented(uint256 tokenId, uint64 until);
    error NotRenting(uint256 tokenId);
    error NoRental(uint256 tokenId);
    error StillRunning(uint256 tokenId);
    error PaymentFailed();

    constructor(address chog_, address genesis_, uint16 burnedShare_) {
        if (chog_ == address(0) || genesis_ == address(0)) revert ZeroAddress();
        if (burnedShare_ > BASIS_POINTS) revert ShareTooLarge();
        chog = IRentalToken(chog_);
        genesis = IRentalGenesis(genesis_);
        burnedShare = burnedShare_;
    }

    /// @notice The holder of a Genesis offers it for rent, or changes its price. A new price is
    /// paid by the next rental or the next days added; what was paid does not change.
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

    /// @notice Takes an offer back: its author, or whoever holds the Genesis now. A rental that
    /// is running goes on to its end.
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

    /// @notice Every Genesis that can be rented now, with its price per day and who is paid: the
    /// offers whose author still holds the Genesis, less those another player rents now.
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
            uint256 tokenId = _offered[index];
            Offer memory made = offers[tokenId];
            address holder = _holderOf(tokenId);
            if (made.lender == address(0) || holder != made.lender) continue;
            if (_counts(_rentals[tokenId], holder)) continue;
            tokenIds[count] = tokenId;
            pricesPerDay[count] = made.pricePerDay;
            lenders[count] = made.lender;
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
            uint256 tokenId = rented[index];
            Rental memory running = _rentals[tokenId];
            if (running.renter != renter || !_counts(running, _holderOf(tokenId))) continue;
            tokenIds[count] = tokenId;
            until[count] = running.until;
            ++count;
        }
        assembly ("memory-safe") {
            mstore(tokenIds, count)
            mstore(until, count)
        }
    }

    /// @notice Whether an offer for a Genesis stands (its author still holds it), at what price,
    /// and who is paid. It answers the same while the Genesis is rented: its renter adds days at
    /// this price.
    function offerOf(uint256 tokenId) public view returns (bool offered, uint128 pricePerDay, address lender) {
        Offer memory made = offers[tokenId];
        offered = made.lender != address(0) && _holderOf(tokenId) == made.lender;
        return (offered, made.pricePerDay, made.lender);
    }

    /// @notice Whether an address may play a Genesis as its renter at this moment: its paid time
    /// runs and the holder it paid still holds the Genesis.
    function isRenting(uint256 tokenId, address renter) external view returns (bool) {
        Rental memory running = _rentals[tokenId];
        return running.renter == renter && renter != address(0) && _counts(running, _holderOf(tokenId));
    }

    /// @notice The second the paid time of an address on a Genesis ends; 0 when it has no rental
    /// of it on record. It does not say whether the holder still holds it (see isRenting).
    function rentedUntil(uint256 tokenId, address renter) external view returns (uint64) {
        Rental memory running = _rentals[tokenId];
        return running.renter == renter && renter != address(0) ? running.until : 0;
    }

    /// @notice The rental on record for a Genesis, running or not yet closed: who rents it, who is
    /// paid, until when, the payment not yet earned, and the part of it earned since the last
    /// settlement (still in `escrow`).
    function rentalOf(uint256 tokenId)
        external
        view
        returns (address renter, address lender, uint64 until, uint256 escrow, uint256 earnedSinceSettled)
    {
        Rental memory running = _rentals[tokenId];
        return (running.renter, running.lender, running.until, running.escrow, _earnedBy(running, block.timestamp));
    }

    /// @notice Rents a Genesis for a number of days. A renter whose rental runs adds days after
    /// those already paid; nobody else may rent it until it ends. The renter must have approved
    /// this contract for price x days, which is held here and earned by the holder by the second.
    /// `maxPricePerDay` is the price the renter saw: the rental is refused if the holder raised it.
    function rent(uint256 tokenId, uint256 numberOfDays, uint128 maxPricePerDay) external {
        (bool offered, uint128 pricePerDay, address lender) = offerOf(tokenId);
        if (!offered) revert NotOffered(tokenId);
        if (pricePerDay > maxPricePerDay) revert PriceWentUp(pricePerDay);
        if (lender == msg.sender) revert OwnGenesis(tokenId);
        if (numberOfDays == 0) revert BadDuration(numberOfDays);

        Rental storage running = _rentals[tokenId];
        uint256 from = block.timestamp;
        if (running.renter != address(0)) {
            if (_counts(running, lender)) {
                if (running.renter != msg.sender) revert AlreadyRented(tokenId, running.until);
                // Days added: what was earned so far is the holder's, and the payment left plus
                // the new one is spread from now to the new end.
                _settle(tokenId, running);
                running.settledAt = uint64(block.timestamp);
                from = running.until;
            } else {
                // Run out, or the holder it paid sold the Genesis: settled before anyone rents again.
                _end(tokenId, running);
            }
        }
        uint256 until = from + numberOfDays * 1 days;
        if (until > block.timestamp + MAX_DAYS * 1 days) revert BadDuration(numberOfDays);
        uint256 cost = uint256(pricePerDay) * numberOfDays;

        if (running.renter == address(0)) {
            running.renter = msg.sender;
            running.lender = lender;
            running.settledAt = uint64(block.timestamp);
        }
        running.until = uint64(until);
        running.escrow += cost;
        if (!_listedFor[msg.sender][tokenId]) {
            _listedFor[msg.sender][tokenId] = true;
            _rentedBy[msg.sender].push(tokenId);
        }

        _pull(msg.sender, cost);
        emit Rented(tokenId, msg.sender, lender, numberOfDays, cost, uint64(until));
    }

    /// @notice The renter ends their rental now: the time not delivered is refunded to them
    /// (taken out with `claim`).
    function endRental(uint256 tokenId) external {
        Rental storage running = _rentals[tokenId];
        if (running.renter != msg.sender || running.renter == address(0) || running.until <= block.timestamp) {
            revert NotRenting(tokenId);
        }
        _end(tokenId, running);
    }

    /// @notice Anyone closes a rental once the holder it paid no longer holds the Genesis, or once
    /// its time is over: the holder earned the time up to now, the renter gets the rest back. The
    /// contract cannot see when a sale happened, so the game services close a rental as soon as
    /// they see the Genesis change hands.
    function close(uint256 tokenId) external {
        Rental storage running = _rentals[tokenId];
        if (running.renter == address(0)) revert NoRental(tokenId);
        if (_counts(running, _holderOf(tokenId))) revert StillRunning(tokenId);
        _end(tokenId, running);
    }

    /// @notice Takes out the caller's money: what they earned as a holder, less the burned share,
    /// and what they are owed back as a renter. The rentals of `tokenIds` are settled first (any
    /// of them, by anyone): their holders' earnings up to now are counted, and those whose holder
    /// sold the Genesis or whose time is over are closed.
    function claim(uint256[] calldata tokenIds) external {
        for (uint256 index; index < tokenIds.length; ++index) {
            uint256 tokenId = tokenIds[index];
            Rental storage running = _rentals[tokenId];
            if (running.renter == address(0)) continue;
            if (_counts(running, _holderOf(tokenId))) _settle(tokenId, running);
            else _end(tokenId, running);
        }

        uint256 earned = earnings[msg.sender];
        uint256 owedBack = refunds[msg.sender];
        if (earned == 0 && owedBack == 0) return;
        earnings[msg.sender] = 0;
        refunds[msg.sender] = 0;
        uint256 burned = earned * burnedShare / BASIS_POINTS;
        uint256 paid = earned - burned + owedBack;

        emit Claimed(msg.sender, paid, burned);
        if (burned > 0) chog.burn(burned);
        if (paid > 0) _send(msg.sender, paid);
    }

    // Whether a rental counts now: someone rents it, its time runs, and the holder it pays is the
    // one given (the present holder).
    function _counts(Rental memory running, address holder) private view returns (bool) {
        return running.renter != address(0) && running.until > block.timestamp && running.lender == holder;
    }

    // The part of the escrow earned between the last settlement and `moment` (never past the end).
    function _earnedBy(Rental memory running, uint256 moment) private pure returns (uint256) {
        if (running.renter == address(0)) return 0;
        uint256 upTo = moment < running.until ? moment : running.until;
        if (upTo <= running.settledAt) return 0;
        if (upTo == running.until) return running.escrow;
        return running.escrow * (upTo - running.settledAt) / (running.until - running.settledAt);
    }

    // Moves the part earned up to now from the escrow to the holder's earnings. When that part
    // rounds down to nothing the settlement time stays, so no fraction is ever dropped.
    function _settle(uint256 tokenId, Rental storage running) private {
        uint256 earned = _earnedBy(running, block.timestamp);
        if (earned == 0) return;
        running.settledAt = uint64(block.timestamp < running.until ? block.timestamp : running.until);
        running.escrow -= earned;
        earnings[running.lender] += earned;
        emit Earned(tokenId, running.lender, earned);
    }

    // Settles a rental up to now, gives its renter what is left of the escrow, and clears it.
    function _end(uint256 tokenId, Rental storage running) private {
        _settle(tokenId, running);
        address renter = running.renter;
        address lender = running.lender;
        uint256 refunded = running.escrow;
        if (refunded > 0) refunds[renter] += refunded;
        delete _rentals[tokenId];
        emit Ended(tokenId, renter, lender, refunded);
    }

    // Who holds a Genesis, or zero when the collection does not answer (a burned token).
    function _holderOf(uint256 tokenId) private view returns (address) {
        try genesis.ownerOf(tokenId) returns (address holder) {
            return holder;
        } catch {
            return address(0);
        }
    }

    // transferFrom and transfer that accept both tokens that return true and tokens that return nothing.
    function _pull(address from, uint256 amount) private {
        (bool ok, bytes memory answer) =
            address(chog).call(abi.encodeCall(IRentalToken.transferFrom, (from, address(this), amount)));
        if (!ok || (answer.length != 0 && !abi.decode(answer, (bool)))) revert PaymentFailed();
    }

    function _send(address to, uint256 amount) private {
        (bool ok, bytes memory answer) = address(chog).call(abi.encodeCall(IRentalToken.transfer, (to, amount)));
        if (!ok || (answer.length != 0 && !abi.decode(answer, (bool)))) revert PaymentFailed();
    }
}
