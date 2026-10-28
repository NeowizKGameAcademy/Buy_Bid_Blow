using System;
using System.Collections.Generic;
using System.IO;
using BuyBidBlow.Systems.Lottery;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;
using UnityEngine;

namespace BuyBidBlow.Dev.Tests.EditMode
{
    public sealed class LotteryTests
    {
        readonly PlayerId a=new PlayerId("a"),b=new PlayerId("b");
        Dictionary<PlayerId,PlayerPortfolio> Players()=>new Dictionary<PlayerId,PlayerPortfolio>{{a,new PlayerPortfolio(200,0)},{b,new PlayerPortfolio(200,0)}};
        MatchSettings Rules()=>JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));

        [Test] public void UnsoldNumbersCarryBaseAndSalesButNotTickets()
        {
            int seed=0;
            while(new System.Random(unchecked(seed^(int)0x39E1A64B)).Next(100)<2)seed++;
            var players=Players();var lottery=new LotteryGame(new LotterySettings(),new[]{2,6,10},players,seed);
            Assert.That(lottery.Buy(a,1,2),Does.Contain("구매 완료"));
            Assert.AreEqual(40,lottery.Prize);lottery.CloseRound(2);
            Assert.IsFalse(lottery.LastDraw.Won);Assert.AreEqual(40,lottery.Carried);
            Assert.AreEqual(50,lottery.BasePrize);Assert.AreEqual(90,lottery.Prize);
            Assert.AreEqual(0,lottery.Tickets(a));Assert.AreEqual(0,lottery.Sold);
            Assert.AreEqual(190,players[a].Cash);
            lottery.CloseRound(2);Assert.AreEqual(2,lottery.DrawIndex,"never redraw a closed round");
        }
        [Test] public void EntireFixedDomainIncludesUnsoldAndLastSoldNumber()
        {
            foreach(int target in new[]{1,2,3,100})
            {
                int seed=0;while(new System.Random(unchecked(seed^(int)0x39E1A64B)).Next(100)+1!=target)seed++;
                var players=Players();var lottery=new LotteryGame(new LotterySettings(),new[]{2},players,seed);
                lottery.Buy(a,1,1);lottery.Buy(b,1,1);lottery.CloseRound(2);
                Assert.AreEqual(target,lottery.LastDraw.Number);
                Assert.AreEqual(target<=2,lottery.LastDraw.Won);
                if(target<=2)Assert.AreEqual(target==1?a:b,lottery.LastDraw.Winner);
                Assert.AreEqual(390+(target<=2?40:0),players[a].Cash+players[b].Cash);
                int total=players[a].Cash+players[b].Cash;lottery.CloseRound(2);Assert.AreEqual(total,players[a].Cash+players[b].Cash);
            }
        }
        [Test] public void ReservationsLimitsAndStaleRequestsAreAtomic()
        {
            var players=Players();var lottery=new LotteryGame(new LotterySettings(),new[]{2,6,10},players,7);
            players[a].TryReserve(180);lottery.Buy(a,1,5);Assert.AreEqual(200,players[a].Cash);Assert.AreEqual(0,lottery.Sold);
            lottery.Buy(a,1,4);Assert.AreEqual(180,players[a].Cash);Assert.AreEqual(180,players[a].Reserved);
            players[a].Release(180);lottery.Buy(a,1,7);Assert.AreEqual(4,lottery.Sold);
            lottery.Buy(a,1,6);Assert.AreEqual(10,lottery.Tickets(a));
            int cash=players[a].Cash;
            foreach(int q in new[]{0,-1,int.MaxValue,1})lottery.Buy(a,1,q);
            Assert.AreEqual(cash,players[a].Cash);Assert.AreEqual(10,lottery.Sold);
            lottery.CloseRound(2);cash=players[a].Cash;lottery.Buy(a,1,1);Assert.AreEqual(cash,players[a].Cash);Assert.AreEqual(0,lottery.Sold);
        }
        [Test] public void GlobalCapacityAndGuaranteedSoldDraw()
        {
            var players=Players();var settings=new LotterySettings{TicketPrice=1,PerPlayerLimit=100};
            var lottery=new LotteryGame(settings,new[]{2,6},players,9);
            lottery.Buy(a,1,100);int cash=players[b].Cash;lottery.Buy(b,1,1);
            Assert.AreEqual(cash,players[b].Cash);Assert.AreEqual(100,lottery.Sold);
            lottery.CloseRound(2);Assert.IsTrue(lottery.LastDraw.Won);Assert.AreEqual(a,lottery.LastDraw.Winner);
            Assert.AreEqual(230,players[a].Cash);Assert.AreEqual(0,lottery.Carried);Assert.AreEqual(50,lottery.Prize);
        }
        [Test] public void EmptyDrawsAccumulateBasesAndFinalResidualCannotBePurchased()
        {
            var lottery=new LotteryGame(new LotterySettings(),new[]{2,6,10},Players(),5);
            lottery.CloseRound(2);Assert.AreEqual(80,lottery.Prize);
            lottery.CloseRound(6);Assert.AreEqual(150,lottery.Prize);
            lottery.CloseRound(10);Assert.IsTrue(lottery.Finished);Assert.AreEqual(150,lottery.Carried);
            Assert.AreEqual(0,lottery.Prize);Assert.AreEqual(0,lottery.BasePrize);lottery.Buy(a,4,1);Assert.AreEqual(0,lottery.Sold);
        }
        [Test] public void MatchDeadlineCatchupAndPriceRandomnessStayIndependent()
        {
            var rules=Rules();var baseline=new MatchEngine(rules,313,new[]{a,b},10);
            var game=new MatchEngine(rules,313,new[]{a,b},10);game.EnableLottery(new LotterySettings(),313,10);
            Assert.AreEqual(190,game.LotteryDeadline);
            game.BuyLottery(a,1,1,9);Assert.AreEqual(200,game.Snapshot(a).Cash);
            game.BuyLottery(a,1,1,10);Assert.AreEqual(195,game.Snapshot(a).Cash);
            game.BuyLottery(a,1,1,190);Assert.AreEqual(1,game.Lottery.Sold,"equality is closed even before Tick");
            game.Tick(10000);baseline.Tick(10000);Assert.IsTrue(game.Complete);Assert.IsTrue(game.Lottery.Finished);
            foreach(Company c in Enum.GetValues(typeof(Company)))Assert.AreEqual(baseline.Price(c),game.Price(c));
            Assert.AreEqual(game.Total(a),game.AssetHistory(a)[13]);
            var cash=game.Snapshot(a).Cash;game.Tick(10001);Assert.AreEqual(cash,game.Snapshot(a).Cash);
        }
        [Test] public void InvalidSettingsFailBeforePlay()
        {
            Assert.Throws<ArgumentException>(()=>new LotterySettings{TicketCount=0}.Validate(3));
            Assert.Throws<ArgumentException>(()=>new LotterySettings{PerPlayerLimit=101}.Validate(3));
            Assert.Throws<ArgumentException>(()=>new LotterySettings{InitialBasePrize=int.MaxValue}.Validate(3));
        }
    }
}
