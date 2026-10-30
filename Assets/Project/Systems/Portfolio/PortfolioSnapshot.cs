using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Portfolio
{
    public sealed class PortfolioSnapshot
    {
        private readonly Dictionary<Company, int> shares;

        // 현금·예약금·보유 주식을 복사해 저장
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

        // 회사 보유 주식 수를 반환
        public int GetShares(Company company)
        {
            return shares.TryGetValue(company, out int quantity) ? quantity : 0;
        }
    }
}
