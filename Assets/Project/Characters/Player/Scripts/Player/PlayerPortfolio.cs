using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow
{
    public class PlayerPortfolio : MonoBehaviour, IPortfolio
    {
        [SerializeField] private Dictionary<Company, int> ownedQuantity = new Dictionary<Company, int>();

        private static int CompanyCount => Enum.GetValues(typeof(Company)).Length;

        private void Awake()
        {
            EnsureStorage();
        }

        private void OnValidate()
        {
            EnsureStorage();
        }

        public int GetOwnedQuantity(Company company)
        {
            EnsureStorage();

            return IsValidCompany(company) ? ownedQuantity[company] : 0;
        }

        public void AddStock(Company company, int quantity)
        {
            if (quantity <= 0)
                return;

            EnsureStorage();
            if (!IsValidCompany(company))
                return;

            ownedQuantity[company] += quantity;
        }

        public bool TryRemoveStock(Company company, int quantity)
        {
            EnsureStorage();
            if (!IsValidCompany(company) || quantity <= 0 || quantity > ownedQuantity[company])
                return false;

            ownedQuantity[company] -= quantity;
            return true;
        }

        private void EnsureStorage()
        {
            if (ownedQuantity == null)
                ownedQuantity = new Dictionary<Company, int>();

            foreach (Company company in Enum.GetValues(typeof(Company)))
            {
                if (!ownedQuantity.ContainsKey(company))
                    ownedQuantity.Add(company, 0);
            }
        }

        private static bool IsValidCompany(Company company)
        {
            int index = (int)company;
            return index >= 0 && index < CompanyCount;
        }
    }
}
