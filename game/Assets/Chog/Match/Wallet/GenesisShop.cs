using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Chog.Match
{
    public enum ShopState
    {
        /// <summary>The game services were not asked yet, or did not answer.</summary>
        Unknown,
        /// <summary>Nothing is sold: no shop is set up, or its item is not on sale.</summary>
        Closed,
        /// <summary>The second form can be bought with $CHOG.</summary>
        Open
    }

    public enum PurchaseState
    {
        Idle,
        /// <summary>The wallet is asked to be on Monad.</summary>
        Switching,
        /// <summary>The wallet is asked to let the shop take the price, then that transaction is awaited.</summary>
        Approving,
        /// <summary>The wallet is asked to buy, then that transaction is awaited.</summary>
        Buying,
        /// <summary>The purchase is on the chain; the game services are asked to see it.</summary>
        Confirming,
        /// <summary>It did not go through; "Problem" says why.</summary>
        Failed
    }

    /// <summary>
    /// The one thing sold for $CHOG: the second form of a Genesis's attack, at once, without
    /// waiting for mastery. A purchase is two transactions the player's wallet sends on Monad:
    /// letting the shop contract take the price, then buying for one Genesis. It is recorded on
    /// the chain for that token id, so it stays with the Genesis when it is sold. The game reads
    /// what is sold and what is owned from the game services, which read the contract; while no
    /// shop contract is set up there, this class stays closed and nothing is offered.
    /// </summary>
    public static class GenesisShop
    {
        private const string BuySelector = "0xd6febde8"; // buy(uint256,uint256)

        // Monad charges the gas limit of a transaction, not what it uses. On a copy of mainnet,
        // on 10 October 2026, the first purchase cost 205,100.
        private const int BuyGasLimit = 300000;

        private const int SeenAttempts = 8;
        private const float SeenSeconds = 2f;

        private static readonly HashSet<int> Owned = new HashSet<int>();

        public static ShopState State { get; private set; } = ShopState.Unknown;

        /// <summary>The shop contract and the $CHOG token, as the services gave them.</summary>
        public static string Address { get; private set; } = "";
        public static string Token { get; private set; } = "";
        public static int ItemId { get; private set; }

        /// <summary>The price in the smallest unit of $CHOG, as decimal digits.</summary>
        public static string PriceWei { get; private set; } = "0";

        public static PurchaseState Purchase { get; private set; } = PurchaseState.Idle;

        /// <summary>The Genesis being bought for; 0 when idle.</summary>
        public static int PurchaseTokenId { get; private set; }

        /// <summary>Why the last purchase did not go through; empty otherwise.</summary>
        public static string Problem { get; private set; } = "";

        /// <summary>Raised after any of the values above changed, or what a Genesis owns.</summary>
        public static event Action Changed;

        // Field names are the JSON keys of the services.
        [Serializable]
        private sealed class ShopAnswer
        {
            public bool enabled;
            public string address;
            public string token;
            public int itemId;
            public string price;
        }

        [Serializable]
        private sealed class OwnedAnswer
        {
            public int[] tokens;
        }

        /// <summary>Whether the second form was bought for this Genesis, as far as the game has read.</summary>
        public static bool Owns(int tokenId) => Owned.Contains(tokenId);

        /// <summary>Whether a purchase is under way.</summary>
        public static bool IsBusy => Purchase != PurchaseState.Idle && Purchase != PurchaseState.Failed;

        /// <summary>Asks the game services what is sold. Called once when the page is up.</summary>
        public static void Begin()
        {
            PlayerWallet.Run(GameServices.Call("shop", null, null, (status, text) =>
            {
                var answer = status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<ShopAnswer>(text) : null;
                if (answer == null)
                {
                    // Services without this answer, or that could not read the chain: nothing is offered.
                    Debug.Log("CHOG_SHOP not read: " + status);
                    return;
                }

                State = answer.enabled && MonadTransactions.LooksLikeAnAddress(answer.address) && MonadTransactions.LooksLikeAnAddress(answer.token)
                    ? ShopState.Open : ShopState.Closed;
                Address = answer.address ?? "";
                Token = answer.token ?? "";
                ItemId = answer.itemId;
                PriceWei = MonadTransactions.IsDigits(answer.price) ? answer.price : "0";
                Debug.Log("CHOG_SHOP " + (State == ShopState.Open ? "open, " + MonadTransactions.PriceText(PriceWei) + " CHOG" : "closed"));
                Changed?.Invoke();
            }));
        }

        /// <summary>Asks which of these Genesis have the second form bought; "Changed" follows.</summary>
        public static void ReadOwned(int[] tokenIds)
        {
            if (State != ShopState.Open || tokenIds == null || tokenIds.Length == 0)
                return;

            var list = new StringBuilder();
            for (var i = 0; i < tokenIds.Length && i < GenesisCareers.MaxTokensPerRead; i++)
                list.Append(i == 0 ? "" : ",").Append(tokenIds[i]);

            PlayerWallet.Run(GameServices.Call("shop/owned?tokens=" + list, null, null, (status, text) =>
            {
                var answer = status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<OwnedAnswer>(text) : null;
                if (answer == null || answer.tokens == null)
                    return;

                var changed = false;
                foreach (var tokenId in answer.tokens)
                    changed |= Owned.Add(tokenId);
                if (changed)
                    Changed?.Invoke();
            }));
        }

        /// <summary>
        /// Buys the second form for a Genesis with the connected wallet. The wallet is asked up
        /// to three things: to be on Monad, to let the shop take the price (skipped when it
        /// already may), and to buy.
        /// </summary>
        public static void Buy(int tokenId)
        {
            if (State != ShopState.Open || IsBusy || tokenId <= 0 || Owns(tokenId) || PlayerWallet.State != WalletState.Ready)
                return;

            var buyer = PlayerWallet.Address;
            SetPurchase(PurchaseState.Switching, tokenId, "");
            MonadTransactions.SwitchToMonad(() =>
            {
                MonadTransactions.AllowPayment(buyer, Token, Address, PriceWei, "the second form costs",
                    () => SetPurchase(PurchaseState.Approving, tokenId, ""),
                    () =>
                    {
                        SetPurchase(PurchaseState.Buying, tokenId, "");
                        MonadTransactions.Send(buyer, Address, BuyData(tokenId, ItemId), BuyGasLimit, "the purchase", () =>
                        {
                            SetPurchase(PurchaseState.Confirming, tokenId, "");
                            PlayerWallet.Run(WaitUntilSeen(tokenId));
                        }, Fail);
                    }, Fail);
            }, Fail);
        }

        /// <summary>Puts a failed purchase away, so it can be tried again.</summary>
        public static void Dismiss()
        {
            if (Purchase == PurchaseState.Failed)
                SetPurchase(PurchaseState.Idle, 0, "");
        }

        // The services read the chain a moment after the wallet's own endpoint: ask until they see it.
        private static IEnumerator WaitUntilSeen(int tokenId)
        {
            for (var attempt = 0; attempt < SeenAttempts && !Owns(tokenId); attempt++)
            {
                ReadOwned(new[] { tokenId });
                yield return new WaitForSecondsRealtime(SeenSeconds);
            }

            if (Owns(tokenId))
            {
                Debug.Log("CHOG_SHOP bought for token " + tokenId);
                SetPurchase(PurchaseState.Idle, 0, "");
            }
            else
            {
                Fail(0, "the purchase is on the chain but the game does not see it yet; open the menu again in a moment");
            }
        }

        private static void Fail(int code, string problem)
        {
            Debug.LogWarning("CHOG_SHOP purchase stopped (" + code + "): " + problem);
            // A player who closes the wallet did not fail at anything.
            if (code == MonadTransactions.UserRefused)
                SetPurchase(PurchaseState.Idle, 0, "");
            else
                SetPurchase(PurchaseState.Failed, PurchaseTokenId, problem);
        }

        private static void SetPurchase(PurchaseState state, int tokenId, string problem)
        {
            Purchase = state;
            PurchaseTokenId = tokenId;
            Problem = problem;
            Changed?.Invoke();
        }

        /// <summary>The call that buys an item for a Genesis.</summary>
        public static string BuyData(int tokenId, int itemId)
        {
            return BuySelector + MonadTransactions.Word(tokenId) + MonadTransactions.Word(itemId);
        }
    }
}
