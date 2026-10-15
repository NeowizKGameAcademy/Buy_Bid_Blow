using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuyBidBlow
{
    public class PlayerWallet : MonoBehaviour, IWallet
    {
        [SerializeField] private int money = 100;

        // Held by auction bids. Part of money, but not spendable until released or charged.
        private int reserved;

        public int Money => money;
        public int Reserved => reserved;
        public int Available => money - reserved;

        // Reserved cash is excluded, so buying stock while a bid is live can't overspend.
        public bool CanAfford(int amount)
        {
            return Available >= amount;
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

        // Auction bid or Secret submission.
        public bool TryReserve(int amount)
        {
            if (amount <= 0 || !CanAfford(amount))
                return false;

            reserved += amount;
            return true;
        }

        // Outbid, lost, cancelled or no sale.
        public void Release(int amount)
        {
            if (amount <= 0)
                return;

            reserved -= Mathf.Min(amount, reserved);
        }

        // Winning bid: the reservation turns into an actual payment.
        public void SpendReserved(int amount)
        {
            if (amount <= 0)
                return;

            amount = Mathf.Min(amount, reserved);
            reserved -= amount;
            money -= amount;
        }
    }
}
