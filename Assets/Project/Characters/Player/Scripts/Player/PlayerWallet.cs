using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuyBidBlow
{
    public class PlayerWallet : MonoBehaviour, IWallet
    {
        [SerializeField] private int money = 100;

        public int Money => money;

        public bool CanAfford(int amount)
        {
            return money >= amount;
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
                return;

            money += amount;
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0 || !CanAfford(amount))
                return false;

            money -= amount;
            return true;
        }
    }
}
