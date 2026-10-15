using System;
using System.Collections.Generic;
using UnityEngine;
using BuyBidBlow.Systems;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow
{
    // Client-side view of the asset state last confirmed by the server.
    public class PlayerAssetStatus : MonoBehaviour
    {
        [SerializeField] private int cash = 100;

        private readonly Dictionary<Company, int> shares = new Dictionary<Company, int>();

        public int Cash => cash;

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

        public void ApplyServerState(PlayerAssetSnapshot snapshot)
        {
            EnsureCompanies();
            cash = Mathf.Max(0, snapshot.Cash);

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
