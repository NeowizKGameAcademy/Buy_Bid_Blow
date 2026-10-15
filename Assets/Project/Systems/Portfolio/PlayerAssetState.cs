using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems
{
    public sealed class PlayerAssetState
    {
        private readonly Dictionary<Company, int> shares = new Dictionary<Company, int>();

        public PlayerAssetState(int startingCash, int startingShares)
        {
            Cash = Math.Max(0, startingCash);

            foreach (Company company in Enum.GetValues(typeof(Company)))
                shares[company] = Math.Max(0, startingShares);
        }

        public int Cash { get; private set; }

        public int GetShares(Company company)
        {
            return shares.TryGetValue(company, out int quantity) ? quantity : 0;
        }

        public PlayerAssetSnapshot CreateSnapshot()
        {
            return new PlayerAssetSnapshot(Cash, shares);
        }

        public TradeResult TryBuy(Company company, int quantity, int unitPrice)
        {
            if (!TryGetTotal(company, quantity, unitPrice, out int total, out string error))
                return Failed(error, company);

            if (Cash < total)
                return Failed("돈이 부족합니다.", company);

            if ((long)GetShares(company) + quantity > int.MaxValue)
                return Failed("보유 수량 한도를 초과합니다.", company);

            Cash -= total;
            shares[company] += quantity;
            return Succeeded("구매 완료", company, total);
        }

        public TradeResult TrySell(Company company, int quantity, int unitPrice)
        {
            if (!TryGetTotal(company, quantity, unitPrice, out int total, out string error))
                return Failed(error, company);

            if (GetShares(company) < quantity)
                return Failed("보유 수량이 부족합니다.", company);

            if ((long)Cash + total > int.MaxValue)
                return Failed("보유 현금 한도를 초과합니다.", company);

            shares[company] -= quantity;
            Cash += total;
            return Succeeded("판매 완료", company, total);
        }

        private bool TryGetTotal(Company company, int quantity, int unitPrice, out int total, out string error)
        {
            total = 0;
            error = null;

            if (!shares.ContainsKey(company) || quantity <= 0 || unitPrice <= 0)
            {
                error = "잘못된 거래 요청입니다.";
                return false;
            }

            long value = (long)quantity * unitPrice;
            if (value > int.MaxValue)
            {
                error = "거래 금액이 너무 큽니다.";
                return false;
            }

            total = (int)value;
            return true;
        }

        private TradeResult Succeeded(string action, Company company, int total)
        {
            return new TradeResult(true, $"{action}: $ {total:N0}", company, Cash, GetShares(company), total);
        }

        private TradeResult Failed(string message, Company company)
        {
            return new TradeResult(false, message, company, Cash, GetShares(company), 0);
        }
    }
}
