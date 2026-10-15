using System;
using UnityEngine;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow
{
    public class PlayerPortfolio : MonoBehaviour
    {
        [SerializeField] private int[] ownedQuantity = new int[CompanyCount];

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
            return IsValidCompany(company) ? ownedQuantity[(int)company] : 0;
        }

        public void AddStock(Company company, int quantity)
        {
            if (quantity <= 0)
                return;

            EnsureStorage();
            if (!IsValidCompany(company))
                return;

            ownedQuantity[(int)company] += quantity;
        }

        public bool TryRemoveStock(Company company, int quantity)
        {
            EnsureStorage();
            if (!IsValidCompany(company) || quantity <= 0 || quantity > ownedQuantity[(int)company])
                return false;

            ownedQuantity[(int)company] -= quantity;
            return true;
        }

        private void EnsureStorage()
        {
            if (ownedQuantity == null)
                ownedQuantity = new int[CompanyCount];
            else if (ownedQuantity.Length != CompanyCount)
                Array.Resize(ref ownedQuantity, CompanyCount);
        }

        private static bool IsValidCompany(Company company)
        {
            int index = (int)company;
            return index >= 0 && index < CompanyCount;
        }
    }
}
