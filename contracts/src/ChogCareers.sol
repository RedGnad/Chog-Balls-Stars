// SPDX-License-Identifier: MIT
pragma solidity 0.8.28;

/// @title ChogCareers
/// @notice The career of each Chog Genesis, kept on chain by token id, so it belongs to the Genesis
/// and follows it when the token changes hands. After each match the game's referee, a key of the
/// backend that writes only once it has played the match again from the inputs the game server
/// recorded, sends one transaction with the winner and the Genesis that played. Each Genesis
/// counts its matches, wins, draws, the matches a renter or a guest played it, its current and best
/// run of wins, and when it last played. Mastery and badges are read from these counters off
/// chain, by the rule of the game services (server/src/career.js).
/// Not upgradeable. The owner can only replace the referee and hand the contract over.
contract ChogCareers {
    /// @notice The counters of one Genesis, packed in one storage word.
    struct Career {
        uint32 matches;
        uint32 wins;
        uint32 draws;
        uint32 rentedMatches; // played by a renter, who paid the holder for it
        uint32 guestMatches; // played by a guest, with a pass the holder handed out
        uint16 streak; // wins in a row up to the last match
        uint16 bestStreak;
        uint64 lastPlayedAt; // seconds since 1970, 0 if it never played
    }

    /// @notice One Genesis in a match: its token id, its team (0 or 1), and who played it.
    struct Entry {
        uint16 tokenId;
        uint8 team;
        uint8 playedBy; // HOLDER, RENTER or GUEST
    }

    /// @notice The highest token id of the collection; token ids run from 1.
    uint256 public constant LAST_TOKEN_ID = 1969;
    /// @notice A match records no more Genesis than this.
    uint256 public constant MAX_ENTRIES = 16;
    /// @notice The result of a match nobody won.
    uint8 public constant DRAW = 2;
    uint8 public constant HOLDER = 0;
    uint8 public constant RENTER = 1;
    uint8 public constant GUEST = 2;
    // What a Genesis did in a match, in the event Played.
    uint8 private constant LOST = 0;
    uint8 private constant WON = 1;
    uint8 private constant DREW = 2;

    // Declared first so that the careers sit in consecutive storage slots, one word per token id
    // from slot 1: Monad prices cold storage per page of 128 consecutive slots (MIP-8), so a page
    // holds 128 careers and reading every career touches 16 pages. A mapping would give each token
    // its own page.
    Career[LAST_TOKEN_ID + 1] private _careers;

    /// @notice The address that alone records matches.
    address public referee;
    address public owner;

    // 0 for a match key never recorded, its winner team plus one otherwise.
    mapping(bytes32 matchKey => uint8) private _resultPlusOne;

    event MatchRecorded(bytes32 indexed matchKey, uint8 winnerTeam, uint256 entries);
    event Played(bytes32 indexed matchKey, uint256 indexed tokenId, uint8 team, uint8 playedBy, uint8 outcome);
    event RefereeChanged(address indexed referee);
    event OwnerChanged(address indexed previousOwner, address indexed newOwner);

    error NotOwner();
    error NotReferee();
    error ZeroAddress();
    error BadTeam(uint8 team);
    error BadPlayedBy(uint8 playedBy);
    error BadToken(uint256 tokenId);
    error NoEntries();
    error TooManyEntries(uint256 entries);
    error TokensNotAscending(uint256 tokenId);
    error AlreadyRecorded(bytes32 matchKey);

    modifier onlyOwner() {
        if (msg.sender != owner) revert NotOwner();
        _;
    }

    constructor(address referee_, address owner_) {
        if (referee_ == address(0) || owner_ == address(0)) revert ZeroAddress();
        referee = referee_;
        owner = owner_;
        emit RefereeChanged(referee_);
        emit OwnerChanged(address(0), owner_);
    }

    /// @notice Replaces the referee: from then on only the new one records matches.
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

    /// @notice Records one match: the team that won (0 or 1, 2 for a draw) and the Genesis that
    /// played it, in ascending order of token id, each once. A match key is recorded once; it is
    /// meant to be keccak256 of the name the game server gives the match, as for the wager.
    function record(bytes32 matchKey, uint8 winnerTeam, Entry[] calldata entries) external {
        if (msg.sender != referee) revert NotReferee();
        if (winnerTeam > DRAW) revert BadTeam(winnerTeam);
        if (_resultPlusOne[matchKey] != 0) revert AlreadyRecorded(matchKey);
        uint256 count = entries.length;
        if (count == 0) revert NoEntries();
        if (count > MAX_ENTRIES) revert TooManyEntries(count);

        _resultPlusOne[matchKey] = winnerTeam + 1;
        uint64 now_ = uint64(block.timestamp);
        uint256 previous;
        for (uint256 index; index < count; ++index) {
            Entry calldata entry = entries[index];
            uint256 tokenId = entry.tokenId;
            if (tokenId == 0 || tokenId > LAST_TOKEN_ID) revert BadToken(tokenId);
            // Strictly ascending: a token cannot appear twice.
            if (tokenId <= previous) revert TokensNotAscending(tokenId);
            previous = tokenId;
            if (entry.team > 1) revert BadTeam(entry.team);
            if (entry.playedBy > GUEST) revert BadPlayedBy(entry.playedBy);

            Career memory career = _careers[tokenId];
            uint8 outcome = winnerTeam == DRAW ? DREW : entry.team == winnerTeam ? WON : LOST;
            ++career.matches;
            if (outcome == WON) {
                ++career.wins;
                // A run that long stays at the top rather than stopping the records of this Genesis.
                if (career.streak != type(uint16).max) ++career.streak;
                if (career.streak > career.bestStreak) career.bestStreak = career.streak;
            } else {
                // A draw ends a run of wins as a loss does.
                if (outcome == DREW) ++career.draws;
                career.streak = 0;
            }
            if (entry.playedBy == RENTER) ++career.rentedMatches;
            else if (entry.playedBy == GUEST) ++career.guestMatches;
            career.lastPlayedAt = now_;
            _careers[tokenId] = career;
            emit Played(matchKey, tokenId, entry.team, entry.playedBy, outcome);
        }
        emit MatchRecorded(matchKey, winnerTeam, count);
    }

    /// @notice Whether a match was recorded, and its winner team (0, 1, or 2 for a draw).
    function resultOf(bytes32 matchKey) external view returns (bool recorded, uint8 winnerTeam) {
        uint8 resultPlusOne = _resultPlusOne[matchKey];
        return resultPlusOne == 0 ? (false, 0) : (true, resultPlusOne - 1);
    }

    /// @notice The career of one Genesis; zeros if it never played.
    function careerOf(uint256 tokenId) external view returns (Career memory) {
        if (tokenId == 0 || tokenId > LAST_TOKEN_ID) revert BadToken(tokenId);
        return _careers[tokenId];
    }

    /// @notice The careers of these Genesis, in the same order.
    function careersOf(uint256[] calldata tokenIds) external view returns (Career[] memory careers) {
        careers = new Career[](tokenIds.length);
        for (uint256 index; index < tokenIds.length; ++index) {
            uint256 tokenId = tokenIds[index];
            if (tokenId == 0 || tokenId > LAST_TOKEN_ID) revert BadToken(tokenId);
            careers[index] = _careers[tokenId];
        }
    }

    /// @notice The careers of `count` Genesis from `firstTokenId` on, each as its raw storage
    /// word, for a reader that ranks every Genesis in one call (count up to LAST_TOKEN_ID). From the
    /// lowest bits: matches (32 bits), wins (32), draws (32), rentedMatches (32), guestMatches (32),
    /// streak (16), bestStreak (16), lastPlayedAt (64).
    function packedCareers(uint256 firstTokenId, uint256 count) external view returns (uint256[] memory words) {
        if (firstTokenId == 0 || count == 0 || firstTokenId + count - 1 > LAST_TOKEN_ID) revert BadToken(firstTokenId);
        words = new uint256[](count);
        for (uint256 index; index < count; ++index) {
            uint256 slot = _slotOf(firstTokenId + index);
            uint256 word;
            assembly ("memory-safe") {
                word := sload(slot)
            }
            words[index] = word;
        }
    }

    function _slotOf(uint256 tokenId) private pure returns (uint256 slot) {
        assembly ("memory-safe") {
            slot := add(_careers.slot, tokenId)
        }
    }
}
