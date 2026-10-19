using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Portfolio
{
    public sealed class PortfolioSnapshot
    {
        private readonly Dictionary<Company, int> shares;

        public PortfolioSnapshot(int cash, int reserved, IReadOnlyDictionary<Company, int> shares)
        {
            Cash = cash;
            Reserved = reserved;
            this.shares = new Dictionary<Company, int>(shares);
        }

        public int Cash { get; }

        // Held by auction bids; Cash - Reserved is what the player can spend.
        public int Reserved { get; }

        public int Available => Cash - Reserved;

        public int GetShares(Company company)
        {
            return shares.TryGetValue(company, out int quantity) ? quantity : 0;
        }
    }
}
