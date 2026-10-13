using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuyBidBlow
{
    public interface IWallet
    {
        int Money { get; }

        bool CanAfford(int amount);

        void AddMoney(int amount);
        bool TrySpendMoney(int amount);

        bool TryBuy(int price);
        void Sell(int price);
    }
}