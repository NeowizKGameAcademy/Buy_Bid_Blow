using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Portfolio
{
    public sealed class PlayerPortfolio
    {
        private readonly Dictionary<Company, int> shares = new Dictionary<Company, int>();

        // 시작 현금과 회사별 시작 주식 수를 설정
        public PlayerPortfolio(int startingCash, int startingShares)
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

        // 회사 보유 주식 수를 반환
        public int GetShares(Company company)
        {
            return shares.TryGetValue(company, out int quantity) ? quantity : 0;
        }

        // 현재 자산 상태의 스냅샷을 생성
        public PortfolioSnapshot CreateSnapshot()
        {
            return new PortfolioSnapshot(Cash, Reserved, shares);
        }

        // 사용 가능한 현금에서 금액을 예약
        // Auction bid or Secret submission. False (and no change) when Available is short.
        public bool TryReserve(int amount)
        {
            if (amount <= 0 || Available < amount)
                return false;

            Reserved += amount;
            return true;
        }

        // 예약된 현금을 해제
        // Returns reserved cash to Available: outbid, lost, cancelled or no sale.
        public void Release(int amount)
        {
            if (amount <= 0)
                return;

            Reserved -= Math.Min(amount, Reserved);
        }

        // 예약금을 실제로 지불해 현금에서 차감
        // Winning bid: the reservation turns into an actual payment.
        public void SpendReserved(int amount)
        {
            if (amount <= 0)
                return;

            amount = Math.Min(amount, Reserved);
            Reserved -= amount;
            Cash -= amount;
        }

        // 금액·현금·보유 한도를 확인해 주식을 매수
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

        // 금액·보유 수량·현금 한도를 확인해 주식을 매도
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

        // 사용 가능한 현금이 충분하면 금액을 차감
        public bool TrySpendAvailable(int amount) { if(amount <= 0 || Available < amount) return false; Cash -= amount; return true; }
        // 현금에 금액을 더함
        public void Credit(int amount) { if(amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); Cash = checked(Cash + amount); }

        // 거래 요청을 검사하고 총 거래 금액을 계산
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

        // 성공 거래 결과를 생성
        private TradeResult Succeeded(string action, Company company, int total)
        {
            return new TradeResult(true, $"{action}: $ {total:N0}", company, Cash, GetShares(company), total);
        }

        // 실패 거래 결과를 생성
        private TradeResult Failed(string message, Company company)
        {
            return new TradeResult(false, message, company, Cash, GetShares(company), 0);
        }
    }
}
