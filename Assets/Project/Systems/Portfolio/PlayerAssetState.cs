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

        // Cash held by auction bids (Open high bid, Secret submission). Still part of Cash,
        // but nothing else may spend it until the auction releases or charges it.
        public int Reserved { get; private set; }

        // Cash - Reserved. Buying stock, bets and new bids are limited to this.
        public int Available => Cash - Reserved;

        public int GetShares(Company company)
        {
            return shares.TryGetValue(company, out int quantity) ? quantity : 0;
        }

        public PlayerAssetSnapshot CreateSnapshot()
        {
            return new PlayerAssetSnapshot(Cash, Reserved, shares);
        }

        // Auction bid or Secret submission. False (and no change) when Available is short.
        public bool TryReserve(int amount)
        {
            if (amount <= 0 || Available < amount)
                return false;

            Reserved += amount;
            return true;
        }

        // Returns reserved cash to Available: outbid, lost, cancelled or no sale.
        public void Release(int amount)
        {
            if (amount <= 0)
                return;

            Reserved -= Math.Min(amount, Reserved);
        }

        // Winning bid: the reservation turns into an actual payment.
        public void SpendReserved(int amount)
        {
            if (amount <= 0)
                return;

            amount = Math.Min(amount, Reserved);
            Reserved -= amount;
            Cash -= amount;
        }

        public TradeResult TryBuy(Company company, int quantity, int unitPrice)
        {
            if (!TryGetTotal(company, quantity, unitPrice, out int total, out string error))
                return Failed(error, company);

            // Reserved cash is excluded, so buying stock while a bid is live can't overspend.
            if (Available < total)
                return Failed(Reserved > 0 ? "입찰 예약금을 제외한 돈이 부족합니다." : "돈이 부족합니다.", company);

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
