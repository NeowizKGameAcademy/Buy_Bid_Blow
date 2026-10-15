using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems
{
    public sealed class PlayerAssetSnapshot
    {
        private readonly Dictionary<Company, int> shares;

        public PlayerAssetSnapshot(int cash, IReadOnlyDictionary<Company, int> shares)
        {
            Cash = cash;
            this.shares = new Dictionary<Company, int>(shares);
        }

        public int Cash { get; }

        public int GetShares(Company company)
        {
            return shares.TryGetValue(company, out int quantity) ? quantity : 0;
        }
    }
}
