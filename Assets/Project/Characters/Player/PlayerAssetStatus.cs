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

        private void Awake()
        {
            EnsureCompanies();
        }

        public int GetShares(Company company)
        {
            EnsureCompanies();
            return shares.TryGetValue(company, out int quantity) ? quantity : 0;
        }

        public void ApplyServerState(TradeResult result)
        {
            EnsureCompanies();
            cash = Mathf.Max(0, result.Cash);
            shares[result.Company] = Mathf.Max(0, result.Shares);
        }

        public void ApplyServerState(PortfolioSnapshot snapshot)
        {
            EnsureCompanies();
            if(snapshot==null){cash=reserved=0;foreach(Company company in Enum.GetValues(typeof(Company)))shares[company]=0;return;}
            cash = Mathf.Max(0, snapshot.Cash);
            reserved = Mathf.Max(0, snapshot.Reserved);

            foreach (Company company in Enum.GetValues(typeof(Company)))
                shares[company] = Mathf.Max(0, snapshot.GetShares(company));
        }

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
