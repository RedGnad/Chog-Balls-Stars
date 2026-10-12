namespace Quantum
{
    public partial class RuntimePlayer
    {
        // Serialized by Quantum's asset serializer, like PlayerAvatar and PlayerNickname.
        // These fields carry the admission profile; they do not prove NFT ownership.
        public AssetRef<GenesisCombatLoadout> GenesisLoadout;
        public string GenesisIdentityKey;
        public int GenesisMetadataId;
        public string GenesisMetadataHash;
        public string GenesisCatalogHash;
        // Canonical token id of the Genesis this player enters the match with; 0 means none.
        // A declaration until the admission backend checks ownership.
        public int GenesisTokenId;
        // Which form of its signature that Genesis plays: 0 the first one, 1 the second form.
        // A declaration like the token id: what unlocks a form is checked outside the match.
        public int GenesisForm;
    }

    public partial class RuntimeConfig
    {
        // Default false keeps the original sample usable during qualification.
        public bool RequireGenesisLoadout;
        public string GenesisCatalogHash;
        // When set, every bot is a Genesis of this roster and a player can enter as one.
        public AssetRef<GenesisRoster> GenesisRoster;
    }
}
