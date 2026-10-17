using System;
using System.Collections;
using System.Globalization;
using System.Numerics;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// What every payment in $CHOG has in common, whatever is paid for: asking the connected
    /// wallet to be on Monad, reading the chain through it, checking that the payer holds enough
    /// and letting a contract take the amount, sending a transaction and waiting until it is in
    /// a block. It also turns values into the bytes a contract call is made of, and back. Each
    /// step that can fail reports a wallet code (4001 when the player refused) and a sentence.
    /// </summary>
    public static class MonadTransactions
    {
        public const int UserRefused = 4001;

        private const string ApproveSelector = "0x095ea7b3";   // approve(address,uint256)
        private const string AllowanceSelector = "0xdd62ed3e"; // allowance(address,address)
        private const string BalanceSelector = "0x70a08231";   // balanceOf(address)

        // Monad charges the gas limit of a transaction, not what it uses: each one is sent with a
        // limit close above its cost. On a copy of mainnet, on 10 October 2026, the approval of
        // the real token cost 62,228.
        private const int ApproveGasLimit = 100000;

        private const int ReceiptAttempts = 90;
        private const float ReceiptSeconds = 1.5f;
        private const int UnknownChain = 4902;

        /// <summary>A chain the wallet is asked to be on: its id, its name, a public endpoint.</summary>
        public sealed class ChainSpec
        {
            public readonly string HexId;
            public readonly string Name;
            public readonly string RpcUrl;

            internal ChainSpec(int chainId, string name, string rpcUrl)
            {
                HexId = "0x" + chainId.ToString("x", CultureInfo.InvariantCulture);
                Name = name;
                RpcUrl = rpcUrl;
            }
        }

        /// <summary>Monad mainnet, with the public endpoint the Monad documentation lists.</summary>
        public static readonly ChainSpec Mainnet = new ChainSpec(0x8f, "Monad", "https://rpc.monad.xyz");

        /// <summary>
        /// The chain the game services name (the wager can be on Monad's testnet), or null when
        /// what they gave cannot be right: the name and the endpoint go into what the wallet is
        /// asked, so only plain names and https addresses without quotes are taken.
        /// </summary>
        public static ChainSpec ChainOf(int chainId, string name, string rpcUrl)
        {
            if (chainId <= 0 || string.IsNullOrEmpty(name) || name.Length > 40 || string.IsNullOrEmpty(rpcUrl) || rpcUrl.Length > 200
                || !rpcUrl.StartsWith("https://", StringComparison.Ordinal))
                return null;
            foreach (var letter in name)
                if (!char.IsLetterOrDigit(letter) && letter != ' ')
                    return null;
            foreach (var letter in rpcUrl)
                if (letter == '"' || letter == '\\' || letter <= ' ')
                    return null;
            return new ChainSpec(chainId, name, rpcUrl);
        }

        /// <summary>What the wallet is asked to switch to that chain.</summary>
        public static string SwitchChainParameters(ChainSpec chain) => "[{\"chainId\":\"" + chain.HexId + "\"}]";

        /// <summary>What a wallet that does not know that chain is asked to add it.</summary>
        public static string AddChainParameters(ChainSpec chain) =>
            "[{\"chainId\":\"" + chain.HexId + "\",\"chainName\":\"" + chain.Name + "\",\"rpcUrls\":[\"" + chain.RpcUrl + "\"]," +
            "\"nativeCurrency\":{\"name\":\"Monad\",\"symbol\":\"MON\",\"decimals\":18}}]";

        /// <summary>The wallet must be on Monad mainnet before anything is read or sent there.</summary>
        public static void SwitchToMonad(Action then, Action<int, string> fail) => SwitchTo(Mainnet, then, fail);

        /// <summary>
        /// The wallet must be on the chain before anything is read or sent. A wallet that does
        /// not know the chain (its code for that is 4902) is offered it, with its public endpoint.
        /// </summary>
        public static void SwitchTo(ChainSpec chain, Action then, Action<int, string> fail)
        {
            PlayerWallet.Request("wallet_switchEthereumChain", SwitchChainParameters(chain), (switched, error, code) =>
            {
                if (error == null)
                {
                    then();
                    return;
                }

                if (code != UnknownChain)
                {
                    fail(code, "the wallet is not on " + chain.Name + ": " + error);
                    return;
                }

                PlayerWallet.Request("wallet_addEthereumChain", AddChainParameters(chain), (added, addError, addCode) =>
                {
                    if (addError != null)
                        fail(addCode, chain.Name + " could not be added to the wallet: " + addError);
                    else
                        then();
                });
            });
        }

        /// <summary>Reads a contract through the wallet; "then" receives the hex it answered.</summary>
        public static void Read(string to, string data, Action<string> then, Action<int, string> fail)
        {
            PlayerWallet.Request("eth_call", "[{\"to\":\"" + to + "\",\"data\":\"" + data + "\"},\"latest\"]", (answer, error, code) =>
            {
                if (error != null)
                    fail(code, "the wallet could not read the chain: " + error);
                else
                    then(Unquoted(answer));
            });
        }

        /// <summary>
        /// Makes sure "payer" can pay "amountWei" of a token to a contract: a wallet that holds
        /// less is told so before it is asked to sign anything ("whatCosts" names what is paid
        /// for, as in "the second form costs"), and the contract is allowed to take the amount
        /// unless it already may. "approving" is called when the wallet is asked for that.
        /// "currency" names the token in what the player reads.
        /// </summary>
        public static void AllowPayment(string payer, string token, string spender, string amountWei, string whatCosts,
                                        Action approving, Action then, Action<int, string> fail, string currency = "$CHOG")
        {
            Read(token, BalanceData(payer), balance =>
            {
                if (!IsAtLeast(balance, amountWei))
                {
                    fail(0, "this wallet holds " + PriceText(HexToDecimal(balance)) + " " + currency + " and " + whatCosts + " "
                            + PriceText(amountWei));
                    return;
                }

                Read(token, AllowanceData(payer, spender), allowance =>
                {
                    if (IsAtLeast(allowance, amountWei))
                    {
                        then();
                        return;
                    }

                    approving?.Invoke();
                    Send(payer, token, ApproveData(spender, amountWei), ApproveGasLimit, "the approval", then, fail);
                }, fail);
            }, fail);
        }

        /// <summary>Sends one transaction from an address, waits until it is in a block, then goes on.</summary>
        public static void Send(string from, string to, string data, int gasLimit, string what, Action then, Action<int, string> fail)
        {
            var transaction = "[{\"from\":\"" + from + "\",\"to\":\"" + to + "\",\"data\":\"" + data + "\",\"gas\":\"0x" +
                              gasLimit.ToString("x", CultureInfo.InvariantCulture) + "\"}]";
            PlayerWallet.Request("eth_sendTransaction", transaction, (hash, error, code) =>
            {
                if (error != null)
                {
                    fail(code, what + " was not sent: " + error);
                    return;
                }

                PlayerWallet.Run(WaitForReceipt(Unquoted(hash), what, then, fail));
            });
        }

        private static IEnumerator WaitForReceipt(string hash, string what, Action then, Action<int, string> fail)
        {
            for (var attempt = 0; attempt < ReceiptAttempts; attempt++)
            {
                yield return new WaitForSecondsRealtime(ReceiptSeconds);
                string receipt = null;
                var answered = false;
                PlayerWallet.Request("eth_getTransactionReceipt", "[\"" + hash + "\"]", (result, error, code) =>
                {
                    receipt = result;
                    answered = true;
                });
                while (!answered)
                    yield return null;

                var succeeded = ReceiptSucceeded(receipt);
                if (succeeded == true)
                {
                    then();
                    yield break;
                }

                if (succeeded == false)
                {
                    fail(0, what + " was refused by the chain");
                    yield break;
                }
            }

            fail(0, what + " is taking too long; look at your wallet, then open the menu again");
        }

        /// <summary>The call that lets "spender" take up to "amountWei" of the caller's tokens.</summary>
        public static string ApproveData(string spender, string amountWei)
        {
            return ApproveSelector + AddressWord(spender) + Word(BigInteger.Parse(amountWei, CultureInfo.InvariantCulture));
        }

        /// <summary>The call that reads how much "spender" may take from "owner".</summary>
        public static string AllowanceData(string owner, string spender)
        {
            return AllowanceSelector + AddressWord(owner) + AddressWord(spender);
        }

        /// <summary>The call that reads how many tokens "owner" holds.</summary>
        public static string BalanceData(string owner)
        {
            return BalanceSelector + AddressWord(owner);
        }

        /// <summary>A hex number ("0x...") as decimal digits; "0" for anything else.</summary>
        public static string HexToDecimal(string hex)
        {
            return !string.IsNullOrEmpty(hex) && hex.StartsWith("0x", StringComparison.Ordinal)
                && BigInteger.TryParse("0" + hex.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value)
                ? value.ToString(CultureInfo.InvariantCulture) : "0";
        }

        /// <summary>An amount of $CHOG in its smallest unit as whole $CHOG with at most two decimals: "500", "12.5".</summary>
        public static string PriceText(string wei)
        {
            if (!IsDigits(wei))
                return "?";

            var amount = BigInteger.Parse(wei, CultureInfo.InvariantCulture);
            // Below a hundredth, which a rental earned by the second often is: neither nothing nor a false zero.
            if (amount > 0 && amount < BigInteger.Pow(10, 16))
                return "<0.01";
            var hundredths = amount / BigInteger.Pow(10, 16);
            var whole = BigInteger.Divide(hundredths, 100).ToString(CultureInfo.InvariantCulture);
            var rest = (int)(hundredths % 100);
            return rest == 0 ? whole : whole + "." + (rest % 10 == 0 ? (rest / 10).ToString(CultureInfo.InvariantCulture) : rest.ToString("00", CultureInfo.InvariantCulture));
        }

        /// <summary>A whole number of $CHOG in its smallest unit, as decimal digits.</summary>
        public static string WholeToWei(int wholeChog)
        {
            return (new BigInteger(Math.Max(0, wholeChog)) * BigInteger.Pow(10, 18)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>An amount in the smallest unit, as decimal digits, times a whole number; "0" for anything else.</summary>
        public static string Times(string wei, int count)
        {
            return IsDigits(wei) && count > 0
                ? (BigInteger.Parse(wei, CultureInfo.InvariantCulture) * count).ToString(CultureInfo.InvariantCulture)
                : "0";
        }

        /// <summary>
        /// What a receipt, as the wallet returned it, says: true for a transaction that went
        /// through, false for one the chain refused, null while there is no receipt yet.
        /// </summary>
        public static bool? ReceiptSucceeded(string receiptJson)
        {
            if (string.IsNullOrEmpty(receiptJson) || receiptJson == "null")
                return null;

            var compact = receiptJson.Replace(" ", "");
            if (compact.Contains("\"status\":\"0x1\""))
                return true;
            if (compact.Contains("\"status\":\"0x0\""))
                return false;
            return null;
        }

        /// <summary>Whether a hex number ("0x...") is at least a decimal one.</summary>
        public static bool IsAtLeast(string hex, string decimalDigits)
        {
            if (string.IsNullOrEmpty(hex) || !hex.StartsWith("0x", StringComparison.Ordinal) || !IsDigits(decimalDigits))
                return false;

            // A leading zero keeps the number positive whatever its first digit.
            return BigInteger.TryParse("0" + hex.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value)
                && value >= BigInteger.Parse(decimalDigits, CultureInfo.InvariantCulture);
        }

        /// <summary>One 32-byte word of call data, as 64 hex digits.</summary>
        public static string Word(BigInteger value)
        {
            return value.ToString("x", CultureInfo.InvariantCulture).TrimStart('0').PadLeft(64, '0');
        }

        /// <summary>An amount given as decimal digits, as one word of call data.</summary>
        public static string WordOf(string decimalDigits)
        {
            return Word(IsDigits(decimalDigits) ? BigInteger.Parse(decimalDigits, CultureInfo.InvariantCulture) : BigInteger.Zero);
        }

        /// <summary>An address as one word of call data.</summary>
        public static string AddressWord(string address)
        {
            return (address ?? "").Replace("0x", "").ToLowerInvariant().PadLeft(64, '0');
        }

        public static bool LooksLikeAnAddress(string text)
        {
            if (text == null || text.Length != 42 || !text.StartsWith("0x", StringComparison.Ordinal))
                return false;

            for (var i = 2; i < text.Length; i++)
                if (!Uri.IsHexDigit(text[i]))
                    return false;
            return true;
        }

        public static bool IsDigits(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 80)
                return false;

            foreach (var letter in text)
                if (letter < '0' || letter > '9')
                    return false;
            return true;
        }

        // The wallet's answers are JSON text: a hex value comes between quotes.
        private static string Unquoted(string json)
        {
            return string.IsNullOrEmpty(json) ? "" : json.Trim().Trim('"');
        }
    }
}
