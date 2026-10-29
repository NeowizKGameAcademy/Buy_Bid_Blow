using System;
using System.IO;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;
using UnityEngine;

namespace BuyBidBlow.Dev.Tests.EditMode
{
    public sealed class ContributionPriceTests
    {
        readonly PlayerId player=new PlayerId("worker");
        MatchSettings Rules()=>JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Dev/Sandbox/StockGacha/MatchSettings.json"));
        [TestCase(6,1,27)][TestCase(-6,1,15)][TestCase(6,-3,23)][TestCase(-6,-3,11)]
        [TestCase(2,-5,21)][TestCase(-2,5,19)][TestCase(0,5,20)]
        public void AdjustmentNeverReversesDirection(int change,int adjustment,int expected)
            =>Assert.AreEqual(expected,DirectionLockedPricePlan.Resolve(20,change,adjustment,10));
        [Test] public void LowerBoundLeavesSpaceForFuturePromisedFalls()
        {
            var prices=new[]{new[]{20},new[]{12},new[]{10}};
            var plan=new DirectionLockedPricePlan(prices,10);plan.Apply(prices,1,0,-5);
            Assert.AreEqual(11,prices[1][0]);Assert.AreEqual(10,prices[2][0]);
            var rising=new[]{new[]{10},new[]{16},new[]{13},new[]{10}};
            var other=new DirectionLockedPricePlan(rising,10);other.Apply(rising,1,0,-5);
            Assert.AreEqual(12,rising[1][0]);Assert.AreEqual(11,rising[2][0]);Assert.AreEqual(10,rising[3][0]);
        }
        [Test] public void AllPrecomputedInformationDirectionsSurviveEveryRound()
        {
            for(int seed=1;seed<=100;seed++)
            {
                var rules=Rules();var baseline=new MatchEngine(rules,seed,new[]{player},0);
                var game=new MatchEngine(rules,seed,new[]{player},0);double now=0;
                for(int round=1;round<=rules.RoundCount;round++)
                {
                    foreach(Company c in Enum.GetValues(typeof(Company)))
                    {
                        int before=game.Price(c);
                        for(int i=0;i<10;i++)Assert.IsTrue(game.AddRoundPriceAdjustment(player,c,(round+(int)c+seed)%2==0?1:-3,now));
                        Assert.AreEqual(before,game.Price(c),"published price stays fixed during work");
                    }
                    double closes=game.Deadline;baseline.Tick(baseline.Deadline);game.Tick(closes);
                    foreach(Company c in Enum.GetValues(typeof(Company)))
                    {Assert.AreEqual(Math.Sign(baseline.LastChange(c)),Math.Sign(game.LastChange(c)));Assert.GreaterOrEqual(game.Price(c),rules.MinPrice);}
                    now=closes;
                    if(!game.Complete)foreach(Company c in Enum.GetValues(typeof(Company)))Assert.AreEqual(0,game.RoundPriceAdjustment(c));
                }
            }
        }
        [Test] public void ThirtyExtraSecondsAndExpiredWorkRequests()
        {
            var rules=Rules();Assert.AreEqual(90,rules.RoundSeconds);Assert.AreEqual(150,rules.AuctionRoundSeconds);
            var game=new MatchEngine(rules,12,new[]{player},10);
            Assert.IsFalse(game.AddRoundPriceAdjustment(player,Company.Straw,1,9));
            Assert.IsFalse(game.AddRoundPriceAdjustment(player,Company.Straw,1,game.Deadline));
            Assert.IsFalse(game.AddRoundPriceAdjustment(player,Company.Straw,2,10));
            game.Tick(20000);Assert.IsTrue(game.Complete);
            Assert.IsFalse(game.AddRoundPriceAdjustment(player,Company.Straw,1,20001));
        }
    }
}
