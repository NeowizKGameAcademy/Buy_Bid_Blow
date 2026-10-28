using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Client-side view of the asset state last confirmed by the server.
    public class PlayerAssetStatus : MonoBehaviour
    {
        [SerializeField] private int cash = 100;

        private readonly Dictionary<Company, int> shares = new Dictionary<Company, int>();

        // Cash held by auction bids; Cash - Reserved is what this player can spend.
        private int reserved;

        public int Cash => cash;
        public int Reserved => reserved;
        public int Available => cash - reserved;
        public bool HasServerState { get; private set; }

        private void OnEnable()
        {
            PlayerNet.LocalPortfolioChanged += ApplyServerState;
            if (Mirror.NetworkClient.active) ApplyServerState(PlayerNet.LocalPortfolio);
        }
        private void OnDisable() => PlayerNet.LocalPortfolioChanged -= ApplyServerState;

        // 회사별 보유 주식 항목을 초기화
        private void Awake()
        {
            EnsureCompanies();
        }

        // 해당 회사의 보유 주식 수를 반환
        public int GetShares(Company company)
        {
            EnsureCompanies();
            return shares.TryGetValue(company, out int quantity) ? quantity : 0;
        }

        // 거래 결과로 받은 현금과 해당 회사 주식 수를 반영
        public void ApplyServerState(TradeResult result)
        {
            EnsureCompanies();
            cash = Mathf.Max(0, result.Cash);
            shares[result.Company] = Mathf.Max(0, result.Shares);
        }

        // 서버 포트폴리오 스냅샷의 현금·예약금·전체 주식 수를 반영
        public void ApplyServerState(PortfolioSnapshot snapshot)
        {
            EnsureCompanies();
            HasServerState = snapshot != null;
            if (snapshot == null)
            {
                cash = reserved = 0;
                foreach (Company company in Enum.GetValues(typeof(Company))) shares[company] = 0;
                return;
            }
            cash = Mathf.Max(0, snapshot.Cash);
            reserved = Mathf.Max(0, snapshot.Reserved);

            foreach (Company company in Enum.GetValues(typeof(Company)))
                shares[company] = Mathf.Max(0, snapshot.GetShares(company));
        }

        // 모든 회사가 주식 목록에 0주로 등록되어 있도록 보장
        private void EnsureCompanies()
        {
            foreach (Company company in Enum.GetValues(typeof(Company)))
            {
                if (!shares.ContainsKey(company))
                    shares.Add(company, 0);
            }
        }
    }
}
