using System.Collections;
using System.Collections.Generic;

namespace BuyBidBlow
{
    public interface IWallet
    {
        int Money { get; }

        // Cash held by auction bids (Open high bid, Secret submission). Still part of Money,
        // but nothing else may spend it until the auction releases or charges it.
        int Reserved { get; }

        // Money - Reserved. Trading, bets and new bids are limited to this.
        int Available { get; }

        // Checks Available, not Money, so reserved cash is never spent twice.
        bool CanAfford(int amount);

        void AddMoney(int amount);
        bool TrySpendMoney(int amount);

        // Holds amount out of Available. False (and no change) when Available is short.
        bool TryReserve(int amount);

        // Returns reserved cash to Available: outbid, lost, cancelled or no sale.
        void Release(int amount);

        // Winning bid: drops the reservation and takes the same amount out of Money.
        void SpendReserved(int amount);
    }
}
