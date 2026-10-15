using System.Collections;
using System.Collections.Generic;

namespace BuyBidBlow
{
    public interface IWallet
    {
        int Money { get; }

        bool CanAfford(int amount);

        void AddMoney(int amount);
        bool TrySpendMoney(int amount);
    }
}