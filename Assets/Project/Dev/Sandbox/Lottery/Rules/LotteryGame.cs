using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Lottery
{
    public readonly struct LotteryDraw
    {
        public readonly int Draw, Round, Number, Prize, Sold, Carried;
        public readonly bool Won;
        public readonly PlayerId Winner;
        public LotteryDraw(int draw, int round, int number, int prize, int sold, bool won, PlayerId winner)
        { Draw=draw; Round=round; Number=number; Prize=prize; Sold=sold; Won=won; Winner=winner; Carried=won?0:prize; }
    }

    // 1..TicketCount is always the draw domain, including unsold tickets.
    // Tickets expire after each draw; only the prize carries forward.
    public sealed class LotteryGame
    {
        readonly IReadOnlyDictionary<PlayerId, PlayerPortfolio> players;
        readonly List<PlayerId> owners = new List<PlayerId>();
        readonly Dictionary<PlayerId,int> counts = new Dictionary<PlayerId,int>();
        readonly Random random;
        readonly int[] drawRounds;
        public LotterySettings Settings { get; }
        public int DrawIndex { get; private set; } = 1;
        public int Version { get; private set; }
        public int Carried { get; private set; }
        public int Sold => owners.Count;
        public bool Finished => DrawIndex > drawRounds.Length;
        public int DrawRound => Finished ? 0 : drawRounds[DrawIndex-1];
        public int BasePrize => Finished ? 0 : checked(Settings.InitialBasePrize+(DrawIndex-1)*Settings.BasePrizeIncrement);
        public int SalesPrize => checked(Sold*Settings.TicketPrice);
        public int Prize => Finished ? 0 : checked(Carried+BasePrize+SalesPrize);
        public LotteryDraw LastDraw { get; private set; }
        public event Action<LotteryDraw> Drawn;

        public LotteryGame(LotterySettings settings, IReadOnlyList<int> rounds,
            IReadOnlyDictionary<PlayerId,PlayerPortfolio> players, int seed)
        {
            settings.Validate(rounds.Count);
            // Freeze configuration for the life of this match.
            Settings=new LotterySettings{TicketCount=settings.TicketCount,TicketPrice=settings.TicketPrice,
                PerPlayerLimit=settings.PerPlayerLimit,InitialBasePrize=settings.InitialBasePrize,BasePrizeIncrement=settings.BasePrizeIncrement};
            this.players=players; drawRounds=new int[rounds.Count];
            for(int i=0;i<rounds.Count;i++)
            {
                if(rounds[i]<1 || (i>0&&rounds[i]<=rounds[i-1]))throw new ArgumentException("Lottery rounds must be increasing.");
                drawRounds[i]=rounds[i];
            }
            random=new Random(unchecked(seed ^ (int)0x39E1A64B));
        }
        public int Tickets(PlayerId player) => counts.TryGetValue(player,out int count)?count:0;
        public string Buy(PlayerId player,int expectedDraw,int quantity)
        {
            if(Finished || expectedDraw!=DrawIndex)return "판매가 끝났거나 지난 회차입니다.";
            if(!players.TryGetValue(player,out var portfolio))return "경기 참가자가 아닙니다.";
            if(quantity<=0 || quantity>Settings.TicketCount-Sold)return "수량 또는 남은 복권을 확인하세요.";
            int own=Tickets(player);
            if(quantity>Settings.PerPlayerLimit-own)return $"회차당 개인 한도는 {Settings.PerPlayerLimit}장입니다.";
            long cost=(long)quantity*Settings.TicketPrice;
            if(cost>int.MaxValue || (long)portfolio.Cash+Prize>int.MaxValue)return "금액 한도를 초과합니다.";
            if(!portfolio.TrySpendAvailable((int)cost))return "입찰 예약금을 제외한 현금이 부족합니다.";
            for(int i=0;i<quantity;i++)owners.Add(player);
            counts[player]=own+quantity;Version++;
            return $"복권 {quantity}장 구매 완료 · ${(int)cost}";
        }
        public void CloseRound(int round)
        {
            if(Finished || round!=DrawRound)return;
            int number=random.Next(Settings.TicketCount)+1;
            bool won=number<=owners.Count;
            var winner=won?owners[number-1]:default;
            int prize=Prize;
            if(won)players[winner].Credit(prize);
            LastDraw=new LotteryDraw(DrawIndex,round,number,prize,Sold,won,winner);
            Carried=won?0:prize;owners.Clear();counts.Clear();DrawIndex++;Version++;
            Drawn?.Invoke(LastDraw);
        }
    }
}
