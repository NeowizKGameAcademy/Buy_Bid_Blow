using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Portfolio;
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
        private readonly double timeScale;
        private readonly IReadOnlyDictionary<PlayerId, PlayerPortfolio> players;
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

        // Single current-round lot. The absent auction is already finished and is never started.
        public AuctionRound(Lot lot, bool secret, double roundStart, AuctionSlot slot,
            int minBid, IReadOnlyDictionary<PlayerId, PlayerPortfolio> players, double timeScale = 1)
            : this(secret ? default : lot, secret ? lot : default, roundStart,
                new[] { slot, slot }, minBid, players, timeScale)
        {
            openClosed = secret;
            secretClosed = !secret;
        }

        // 두 물건·시작 시각·슬롯·최소 입찰가·플레이어로 경매 라운드를 구성
        // slots[0] = Open, slots[1] = Secret, in seconds from roundStart.
        public AuctionRound(Lot firstLot, Lot secondLot, double roundStart, IReadOnlyList<AuctionSlot> slots,
            int minBid, IReadOnlyDictionary<PlayerId, PlayerPortfolio> players, double timeScale = 1)
        {
            if (slots == null || slots.Count != 2)
                throw new ArgumentException("An auction round needs exactly two slots: Open, then Secret.", nameof(slots));

            this.firstLot = firstLot;
            this.secondLot = secondLot;
            this.roundStart = roundStart;
            this.slots = slots;
            this.minBid = minBid;
            this.players = players;
            if (timeScale <= 0 || double.IsNaN(timeScale) || double.IsInfinity(timeScale))
                throw new ArgumentOutOfRangeException(nameof(timeScale));
            this.timeScale = timeScale;
        }

        // 현재 시각에 따라 공개·비밀 경매를 차례로 시작하고 마감
        // Safe to call every frame. Handles big time jumps by running every step that is due, in order.
        public void Tick(double now)
        {
            if (!openClosed && Open == null && now >= SlotStart(0))
            {
                Open = new OpenAuction(firstLot, SlotEnd(0), minBid, players);
                OpenStarted?.Invoke(Open);
            }

            if (Open != null && !openClosed && now >= Open.Deadline)
            {
                openClosed = true;
                OpenClosed?.Invoke(Open.Close());
            }

            if (!secretClosed && Secret == null && now >= SlotStart(1))
            {
                Secret = new SecretAuction(secondLot, SlotEnd(1), minBid, players);
                SecretStarted?.Invoke(Secret);
            }

            if (Secret != null && !secretClosed && now >= Secret.Deadline)
            {
                secretClosed = true;
                SecretClosed?.Invoke(Secret.Close());
            }
        }

        // 공개 경매가 진행 중이면 입찰을 전달
        public AuctionError PlaceOpenBid(PlayerId player, int amount, double now)
            => Open == null || openClosed ? AuctionError.NotOpen : Open.PlaceBid(player, amount, now);

        // 비밀 경매가 진행 중이면 입찰 금액 제출을 전달
        public AuctionError SubmitSecret(PlayerId player, int amount, double now)
            => Secret == null || secretClosed ? AuctionError.NotOpen : Secret.Submit(player, amount, now);

        // 비밀 경매가 진행 중이면 제출 취소를 전달
        public AuctionError CancelSecret(PlayerId player, double now)
            => Secret == null || secretClosed ? AuctionError.NotOpen : Secret.Cancel(player, now);

        // 슬롯 시작 시각을 계산
        private double SlotStart(int i) => roundStart + slots[i].StartsAtSeconds * timeScale;
        // 슬롯 종료 시각을 계산
        private double SlotEnd(int i) => SlotStart(i) + slots[i].DurationSeconds * timeScale;
    }
}
