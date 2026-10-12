using System;
using System.Globalization;
using System.Numerics;

namespace Chog.Heroes
{
    /// <summary>
    /// Binding supplied by a trusted chain/metadata reader. This class does not authenticate
    /// ownership or independently verify an RPC/IPFS response. Never trust a client's binding.
    /// </summary>
    public sealed class GenesisSnapshot
    {
        public int ChainId { get; private set; }
        public string Collection { get; private set; }
        public string TokenId { get; private set; }
        public int MetadataId { get; private set; }
        public string MetadataUri { get; private set; }
        public string MetadataHash { get; private set; }
        public string MetadataJson { get; private set; }
        public string IdentityKey { get { return ChainId.ToString(CultureInfo.InvariantCulture) + ":" + Collection + ":" + TokenId; } }

        public GenesisSnapshot(int chainId, string collection, string tokenId, int metadataId,
                               string metadataUri, string metadataJson, string expectedMetadataHash)
        {
            if (chainId <= 0 || !IsAddress(collection) || !IsTokenId(tokenId) || metadataId <= 0)
                throw new HeroCatalogException("INVALID_IDENTITY", "Invalid chain, collection or canonical token ID.");
            if (String.IsNullOrWhiteSpace(metadataUri) || String.IsNullOrWhiteSpace(metadataJson))
                throw new HeroCatalogException("INVALID_METADATA", "A metadata URI and JSON are required.");
            if (metadataJson.Length > 100000)
                throw new HeroCatalogException("INVALID_METADATA", "Metadata exceeds the accepted size.");
            var hash = JsonData.Hash(metadataJson);
            if (!String.Equals(hash, expectedMetadataHash, StringComparison.OrdinalIgnoreCase))
                throw new HeroCatalogException("METADATA_HASH_MISMATCH", "Metadata differs from the trusted snapshot.");
            ChainId = chainId;
            Collection = collection.ToLowerInvariant();
            TokenId = tokenId;
            MetadataId = metadataId;
            MetadataUri = metadataUri;
            MetadataJson = metadataJson;
            MetadataHash = hash;
        }

        internal static bool IsAddress(string value)
        {
            if (value == null || value.Length != 42 || !value.StartsWith("0x", StringComparison.Ordinal)) return false;
            for (int i = 2; i < value.Length; i++)
                if (!Uri.IsHexDigit(value[i])) return false;
            return true;
        }

        internal static bool IsTokenId(string value)
        {
            BigInteger number;
            return !String.IsNullOrEmpty(value) && value.Length <= 78 && value[0] != '0'
                && BigInteger.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number)
                && number > 0 && number < (BigInteger.One << 256);
        }
    }
}
