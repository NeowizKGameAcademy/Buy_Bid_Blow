using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Auction
{
    // One auction round: the 1st lot as Open, then the 2nd lot as Secret, on the AuctionSlots timetable.
    // The caller feeds time through Tick and requests through the methods below; it never reads the clock itself.
    public sealed class AuctionRound
    {
        private readonly Lot firstLot;
        private readonly Lot secondLot;
        private readonly double roundStart;
        private readonly IReadOnlyList<AuctionSlot> slots;
        private readonly int minBid;
        private readonly IReadOnlyDictionary<int, IWallet> wallets;
        private bool openClosed;
        private bool secretClosed;

        public OpenAuction Open { get; private set; }      // null until its slot starts
        public SecretAuction Secret { get; private set; }  // null until its slot starts
        public bool IsFinished => openClosed && secretClosed;

        // Public: show the lot's rounds and companies (never Direction) to everyone.
        public event Action<OpenAuction> OpenStarted;
        // Public: winner and price. Send the lot itself to the winner only.
        public event Action<AuctionResult> OpenClosed;
        // Public: show the lot's rounds and companies (never Direction) to everyone.
        public event Action<SecretAuction> SecretStarted;
        // Private: tell each of Players won or lost; send the lot to the winner only.
        public event Action<SecretResult> SecretClosed;

        // slots[0] = Open, slots[1] = Secret, in seconds from roundStart.
        public AuctionRound(Lot firstLot, Lot secondLot, double roundStart, IReadOnlyList<AuctionSlot> slots,
            int minBid, IReadOnlyDictionary<int, IWallet> wallets)
        {
            if (slots == null || slots.Count != 2)
                throw new ArgumentException("An auction round needs exactly two slots: Open, then Secret.", nameof(slots));

            this.firstLot = firstLot;
            this.secondLot = secondLot;
            this.roundStart = roundStart;
            this.slots = slots;
            this.minBid = minBid;
            this.wallets = wallets;
        }

        // Safe to call every frame. Handles big time jumps by running every step that is due, in order.
        public void Tick(double now)
        {
            if (Open == null && now >= SlotStart(0))
            {
                Open = new OpenAuction(firstLot, SlotEnd(0), minBid, wallets);
                OpenStarted?.Invoke(Open);
            }

            if (Open != null && !openClosed && now >= Open.Deadline)
            {
                openClosed = true;
                OpenClosed?.Invoke(Open.Close());
            }

            if (Secret == null && now >= SlotStart(1))
            {
                Secret = new SecretAuction(secondLot, SlotEnd(1), minBid, wallets);
                SecretStarted?.Invoke(Secret);
            }

            if (Secret != null && !secretClosed && now >= Secret.Deadline)
            {
                secretClosed = true;
                SecretClosed?.Invoke(Secret.Close());
            }
        }

        public AuctionError PlaceOpenBid(int player, int amount, double now)
            => Open == null || openClosed ? AuctionError.NotOpen : Open.PlaceBid(player, amount, now);

        public AuctionError SubmitSecret(int player, int amount, double now)
            => Secret == null || secretClosed ? AuctionError.NotOpen : Secret.Submit(player, amount, now);

        public AuctionError CancelSecret(int player, double now)
            => Secret == null || secretClosed ? AuctionError.NotOpen : Secret.Cancel(player, now);

        private double SlotStart(int i) => roundStart + slots[i].StartsAtSeconds;
        private double SlotEnd(int i) => SlotStart(i) + slots[i].DurationSeconds;
    }
}
