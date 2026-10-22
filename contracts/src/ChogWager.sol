// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

/// @notice What the wager needs from $CHOG: an ERC-20 with burn.
interface IWagerToken {
    function transfer(address to, uint256 amount) external returns (bool);
    function transferFrom(address from, address to, uint256 amount) external returns (bool);
    function burn(uint256 amount) external;
}

/// @title ChogWager
/// @notice The players of one match each stake the same amount of $CHOG on their own team.
/// The contract holds the stakes until the game's referee says who won: the winners then get
/// their stake back plus an equal share of what the losers staked, less a share that is burned and
/// a share paid to the players who played a Chog Genesis in the match, whether they staked or not
/// (version 2, 22 October 2026: the Genesis carries the game, so its players earn from every staked
/// match they play; the winners' gain is unchanged, the share comes out of what version 1 burned).
/// The referee is a key of the game's backend, which signs a result only after it has played the
/// match again from the inputs the game server recorded. It also signs, before the match, the
/// ticket that lets a player stake on the team that player is really in. Anyone may bring a
/// signed result to the contract, with the list of the Genesis players the referee signed with it. If no result comes within an hour of the first stake, anyone
/// can have every stake given back: the stakes never depend on the backend staying alive.
/// Not upgradeable. The owner can only replace the referee and hand the contract over.
contract ChogWager {
    struct Match {
        uint8 state; // NONE, OPEN, SETTLED or REFUNDED
        uint64 firstStakeAt; // seconds since 1970
        uint8 stakersTeam0;
        uint8 stakersTeam1;
        uint256 stakeAmount; // what each player stakes, fixed by the first stake
        address[] players; // in the order they staked
    }

    uint256 private constant BASIS_POINTS = 10_000;
    uint8 private constant NONE = 0;
    uint8 private constant OPEN = 1;
    uint8 private constant SETTLED = 2;
    uint8 private constant REFUNDED = 3;
    // The result of a match nobody won.
    uint8 private constant DRAW = 2;
    // Half the order of the curve: a signature whose s is above it has a twin below, which is the one accepted.
    uint256 private constant HALF_CURVE_ORDER = 0x7FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF5D576E7357A4501DDFE92F46681B20A0;

    /// @notice A match takes no more stakes than this.
    uint256 public constant MAX_STAKERS = 16;
    /// @notice A result names no more Genesis players than a match has players (3 against 3).
    uint256 public constant MAX_GENESIS_PLAYERS = 6;
    /// @notice How long after its first stake a match without a result can be refunded by anyone.
    uint256 public constant REFUND_AFTER = 1 hours;
    /// @notice What the three kinds of referee signature start with, so one is never taken for another.
    bytes32 public constant TICKET = keccak256("CHOG_WAGER_TICKET");
    bytes32 public constant RESULT = keccak256("CHOG_WAGER_RESULT");
    bytes32 public constant VOID = keccak256("CHOG_WAGER_VOID");

    IWagerToken public immutable chog;
    /// @notice Share of what the losers staked that is burned, in basis points (10_000 is all of it).
    uint16 public immutable burnedShare;
    /// @notice Share of what the losers staked that is split equally between the Genesis players of
    /// the match, in basis points; burned instead when the match had none.
    uint16 public immutable genesisShare;
    /// @notice The key whose signature admits a stake and tells the winner.
    address public referee;
    address public owner;

    mapping(bytes32 matchKey => Match) private _matches;
    // 0 for an address that has not staked on the match, its team plus one otherwise.
    mapping(bytes32 matchKey => mapping(address player => uint8)) private _teamPlusOne;

    event Staked(bytes32 indexed matchKey, address indexed player, uint8 team, uint256 amount);
    event Settled(bytes32 indexed matchKey, uint8 winnerTeam, uint256 paidToEachWinner, uint256 burned);
    event GenesisPlayerPaid(bytes32 indexed matchKey, address indexed player, uint256 amount);
    event Refunded(bytes32 indexed matchKey);
    event RefereeChanged(address indexed referee);
    event OwnerChanged(address indexed previousOwner, address indexed newOwner);

    error NotOwner();
    error ZeroAddress();
    error NotAToken();
    error ShareTooLarge();
    error BadTeam(uint8 team);
    error AmountIsZero();
    error TicketExpired(uint256 deadline);
    error NotSignedByTheReferee();
    error MatchClosed(bytes32 matchKey);
    error NotTheStakeOfTheMatch(uint256 stakeAmount);
    error AlreadyStaked(bytes32 matchKey);
    error MatchIsFull(bytes32 matchKey);
    error NotOpen(bytes32 matchKey);
    error TooEarly(uint256 refundableAt);
    error PaymentFailed();
    error TooManyGenesisPlayers(uint256 count);

    modifier onlyOwner() {
        if (msg.sender != owner) revert NotOwner();
        _;
    }

    constructor(address chog_, address referee_, uint16 burnedShare_, uint16 genesisShare_, address owner_) {
        if (chog_ == address(0) || referee_ == address(0) || owner_ == address(0)) revert ZeroAddress();
        // Stakes are held here: an address without code would take them for paid without moving anything.
        if (chog_.code.length == 0) revert NotAToken();
        if (uint256(burnedShare_) + genesisShare_ > BASIS_POINTS) revert ShareTooLarge();
        chog = IWagerToken(chog_);
        burnedShare = burnedShare_;
        genesisShare = genesisShare_;
        referee = referee_;
        owner = owner_;
        emit RefereeChanged(referee_);
        emit OwnerChanged(address(0), owner_);
    }

    /// @notice Replaces the referee. Signatures of the former one are worth nothing from then on,
    /// also for the matches that are open.
    function setReferee(address referee_) external onlyOwner {
        if (referee_ == address(0)) revert ZeroAddress();
        referee = referee_;
        emit RefereeChanged(referee_);
    }

    function transferOwnership(address newOwner) external onlyOwner {
        if (newOwner == address(0)) revert ZeroAddress();
        emit OwnerChanged(owner, newOwner);
        owner = newOwner;
    }

    /// @notice Stakes on one's own team in a match, with the ticket the referee signed for this
    /// player, this team, this amount and this match, good until `deadline`. The player must have
    /// approved this contract for the amount. The first stake of a match fixes what everyone stakes.
    function stake(bytes32 matchKey, uint8 team, uint256 amount, uint256 deadline, bytes calldata signature) external {
        if (team > 1) revert BadTeam(team);
        if (amount == 0) revert AmountIsZero();
        if (block.timestamp > deadline) revert TicketExpired(deadline);
        _requireReferee(
            keccak256(abi.encode(TICKET, block.chainid, address(this), matchKey, msg.sender, team, amount, deadline)),
            signature
        );

        Match storage played = _matches[matchKey];
        if (played.state == NONE) {
            played.state = OPEN;
            played.stakeAmount = amount;
            played.firstStakeAt = uint64(block.timestamp);
        } else if (played.state != OPEN) {
            revert MatchClosed(matchKey);
        } else if (amount != played.stakeAmount) {
            revert NotTheStakeOfTheMatch(played.stakeAmount);
        }
        if (_teamPlusOne[matchKey][msg.sender] != 0) revert AlreadyStaked(matchKey);
        if (played.players.length == MAX_STAKERS) revert MatchIsFull(matchKey);

        _teamPlusOne[matchKey][msg.sender] = team + 1;
        played.players.push(msg.sender);
        if (team == 0) ++played.stakersTeam0;
        else ++played.stakersTeam1;

        _pull(msg.sender, amount);
        emit Staked(matchKey, msg.sender, team, amount);
    }

    /// @notice Pays a match out on the result the referee signed: 0 or 1 for the team that won,
    /// 2 for a draw, and the wallets that played a Genesis in it. Anyone may bring the signature.
    /// Each winner gets their stake back and an equal share of what the losers staked, less the
    /// burned share and the Genesis share; each Genesis player gets an equal part of the Genesis
    /// share; what cannot be shared equally is burned too. A draw, or a side on which nobody
    /// staked, gives every stake back and pays no Genesis share.
    function settle(bytes32 matchKey, uint8 winnerTeam, address[] calldata genesisPlayers, bytes calldata signature)
        external
    {
        if (winnerTeam > DRAW) revert BadTeam(winnerTeam);
        if (genesisPlayers.length > MAX_GENESIS_PLAYERS) revert TooManyGenesisPlayers(genesisPlayers.length);
        Match storage played = _matches[matchKey];
        if (played.state != OPEN) revert NotOpen(matchKey);
        _requireReferee(
            keccak256(abi.encode(RESULT, block.chainid, address(this), matchKey, winnerTeam, genesisPlayers)), signature
        );

        uint256 winners = winnerTeam == 0 ? played.stakersTeam0 : winnerTeam == 1 ? played.stakersTeam1 : 0;
        uint256 losers = played.players.length - winners;
        if (winners == 0 || losers == 0) {
            _refund(matchKey, played);
            return;
        }

        played.state = SETTLED;
        uint256 lost = losers * played.stakeAmount;
        uint256 genesisPot = lost * genesisShare / BASIS_POINTS;
        uint256 gain = (lost - lost * burnedShare / BASIS_POINTS - genesisPot) / winners;
        uint256 paid = played.stakeAmount + gain;
        _payWinners(matchKey, played.players, winnerTeam, paid);
        uint256 toGenesis = _payGenesisPlayers(matchKey, genesisPlayers, genesisPot);
        uint256 burned = lost - gain * winners - toGenesis;
        if (burned > 0) chog.burn(burned);
        emit Settled(matchKey, winnerTeam, paid, burned);
    }

    /// @notice Calls a match off on the referee's signature, for instance one that never started:
    /// every stake is given back. Anyone may bring the signature.
    function voidMatch(bytes32 matchKey, bytes calldata signature) external {
        Match storage played = _matches[matchKey];
        if (played.state != OPEN) revert NotOpen(matchKey);
        _requireReferee(keccak256(abi.encode(VOID, block.chainid, address(this), matchKey)), signature);
        _refund(matchKey, played);
    }

    /// @notice Gives every stake of a match back once an hour has passed since its first stake
    /// without a result. Anyone may call it and no signature is needed: this is what protects the
    /// players if the referee disappears.
    function refundExpired(bytes32 matchKey) external {
        Match storage played = _matches[matchKey];
        if (played.state != OPEN) revert NotOpen(matchKey);
        uint256 refundableAt = played.firstStakeAt + REFUND_AFTER;
        if (block.timestamp < refundableAt) revert TooEarly(refundableAt);
        _refund(matchKey, played);
    }

    /// @notice Where a match stands (0 none, 1 open, 2 settled, 3 refunded), what each player
    /// stakes, when the first stake was made and how many staked on each team.
    function matchOf(bytes32 matchKey)
        external
        view
        returns (uint8 state, uint256 stakeAmount, uint64 firstStakeAt, uint8 stakersTeam0, uint8 stakersTeam1)
    {
        Match storage played = _matches[matchKey];
        return (played.state, played.stakeAmount, played.firstStakeAt, played.stakersTeam0, played.stakersTeam1);
    }

    /// @notice Whether an address staked on a match, and on which team.
    function stakeOf(bytes32 matchKey, address player) external view returns (bool staked, uint8 team) {
        uint8 teamPlusOne = _teamPlusOne[matchKey][player];
        return teamPlusOne == 0 ? (false, 0) : (true, teamPlusOne - 1);
    }

    /// @notice Everyone who staked on a match, in the order they did, with their team.
    function stakersOf(bytes32 matchKey) external view returns (address[] memory players, uint8[] memory teams) {
        players = _matches[matchKey].players;
        teams = new uint8[](players.length);
        for (uint256 index; index < players.length; ++index) {
            teams[index] = _teamPlusOne[matchKey][players[index]] - 1;
        }
    }

    function _payWinners(bytes32 matchKey, address[] storage players, uint8 winnerTeam, uint256 paid) private {
        for (uint256 index; index < players.length; ++index) {
            if (_teamPlusOne[matchKey][players[index]] == winnerTeam + 1) _pay(players[index], paid);
        }
    }

    // Splits the pot equally between the Genesis players and returns what was paid; the rest of the
    // pot (all of it when the match had no Genesis player) is left to be burned.
    function _payGenesisPlayers(bytes32 matchKey, address[] calldata genesisPlayers, uint256 pot)
        private
        returns (uint256 paid)
    {
        if (genesisPlayers.length == 0) return 0;
        uint256 each = pot / genesisPlayers.length;
        if (each == 0) return 0;
        for (uint256 index; index < genesisPlayers.length; ++index) {
            if (genesisPlayers[index] == address(0)) revert ZeroAddress();
            _pay(genesisPlayers[index], each);
            emit GenesisPlayerPaid(matchKey, genesisPlayers[index], each);
        }
        return each * genesisPlayers.length;
    }

    function _refund(bytes32 matchKey, Match storage played) private {
        played.state = REFUNDED;
        uint256 stakeAmount = played.stakeAmount;
        address[] storage players = played.players;
        for (uint256 index; index < players.length; ++index) {
            _pay(players[index], stakeAmount);
        }
        emit Refunded(matchKey);
    }

    // The referee signs a digest the way a wallet signs a message: under the Ethereum header for
    // 32 bytes. The signature is r, s and v (27 or 28), 65 bytes.
    function _requireReferee(bytes32 digest, bytes calldata signature) private view {
        if (signature.length != 65) revert NotSignedByTheReferee();
        bytes32 r = bytes32(signature[0:32]);
        bytes32 s = bytes32(signature[32:64]);
        uint8 v = uint8(signature[64]);
        if (uint256(s) > HALF_CURVE_ORDER || (v != 27 && v != 28)) revert NotSignedByTheReferee();
        address signer = ecrecover(keccak256(abi.encodePacked("\x19Ethereum Signed Message:\n32", digest)), v, r, s);
        if (signer == address(0) || signer != referee) revert NotSignedByTheReferee();
    }

    // transferFrom and transfer that accept both tokens that return true and tokens that return nothing.
    function _pull(address from, uint256 amount) private {
        _send(abi.encodeCall(IWagerToken.transferFrom, (from, address(this), amount)));
    }

    function _pay(address to, uint256 amount) private {
        _send(abi.encodeCall(IWagerToken.transfer, (to, amount)));
    }

    function _send(bytes memory transferCall) private {
        (bool ok, bytes memory answer) = address(chog).call(transferCall);
        if (!ok || (answer.length != 0 && !abi.decode(answer, (bool)))) revert PaymentFailed();
    }
}
