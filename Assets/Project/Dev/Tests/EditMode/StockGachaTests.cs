using System;
using System.Collections.Generic;
using System.IO;
using BuyBidBlow.Systems.StockGacha;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;
using UnityEngine;

namespace BuyBidBlow.Dev.Tests.EditMode
{
    public sealed class StockGachaTests
    {
        readonly PlayerId a=new PlayerId("a"),b=new PlayerId("b");
        Dictionary<PlayerId,PlayerPortfolio> Players(int cash=2000)=>new Dictionary<PlayerId,PlayerPortfolio>
            {{a,new PlayerPortfolio(cash,0)},{b,new PlayerPortfolio(cash,0)}};
        MatchSettings Rules()=>JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));

        [Test] public void FiniteBagContainsExactlySeventyBlanksAndTenOfEachShare()
        {
            var players=Players();var game=new StockGachaGame(new StockGachaSettings(),players,7);
            int blanks=0;var awards=new int[3];
            for(int i=0;i<100;i++)
            {
                var result=game.Spin(a);Assert.IsTrue(result.Succeeded);
                if(result.Company<0)blanks++;else awards[result.Company]++;
                Assert.AreEqual(99-i,game.Remaining);
            }
            Assert.AreEqual(70,blanks);CollectionAssert.AreEqual(new[]{10,10,10},awards);
            Assert.AreEqual(1000,players[a].Cash);Assert.AreEqual(100,game.OwnSpins(a));Assert.IsTrue(game.Finished);
            foreach(Company c in Enum.GetValues(typeof(Company)))Assert.AreEqual(10,players[a].GetShares(c));
            Assert.IsFalse(game.Spin(a).Succeeded);Assert.AreEqual(1000,players[a].Cash);Assert.AreEqual(100,game.Version);
        }
        [Test] public void BlankConsumesCashAndOneBlankWithoutAward()
        {
            var players=Players();var game=new StockGachaGame(new StockGachaSettings{SharesPerCompany=0},players,7);
            var result=game.Spin(a);Assert.AreEqual(-1,result.Company);Assert.AreEqual(69,game.Blanks);
            Assert.AreEqual(1990,players[a].Cash);
            foreach(Company c in Enum.GetValues(typeof(Company)))Assert.AreEqual(0,players[a].GetShares(c));
        }
        [Test] public void LastSingleCompanyStillWorksAndInventoryIsShared()
        {
            var players=Players();var game=new StockGachaGame(new StockGachaSettings{Blanks=0,SharesPerCompany=1},players,7);
            var first=game.Spin(a);var second=game.Spin(b);
            Assert.AreNotEqual(first.Company,second.Company);Assert.AreEqual(1,game.Remaining);
            var last=game.Spin(b);Assert.IsTrue(last.Succeeded);
            Assert.AreNotEqual(first.Company,last.Company);Assert.AreNotEqual(second.Company,last.Company);
            Assert.IsTrue(game.Finished);Assert.AreEqual(1,game.OwnSpins(a));Assert.AreEqual(2,game.OwnSpins(b));
        }
        [Test] public void ReservationAndFailedRequestsNeverConsumeBagOrRandomness()
        {
            var players=Players(200);players[a].TryReserve(195);
            var game=new StockGachaGame(new StockGachaSettings(),players,55);
            Assert.IsFalse(game.Spin(a).Succeeded);
            Assert.IsFalse(game.Spin(new PlayerId("unknown")).Succeeded);
            Assert.AreEqual(100,game.Remaining);Assert.AreEqual(0,game.Version);Assert.AreEqual(200,players[a].Cash);
            players[a].Release(195);
            var baseline=new StockGachaGame(new StockGachaSettings(),Players(),55);
            Assert.AreEqual(baseline.Spin(a).Company,game.Spin(a).Company);
            Assert.AreEqual(190,players[a].Cash);
        }
        [Test] public void MarketStockUnaffectedByAwardsButSellingAddsToMarket()
        {
            var game=new MatchEngine(Rules(),123,new[]{a,b},0);
            game.EnableStockGacha(new StockGachaSettings{Blanks=0},123,0);
            var before=new int[3];foreach(Company c in Enum.GetValues(typeof(Company)))before[(int)c]=game.Stock(c);
            var result=game.SpinStockGacha(a,0);
            Assert.IsTrue(result.Succeeded);Assert.AreEqual(190,game.Snapshot(a).Cash);
            Assert.AreEqual(1,game.Snapshot(a).GetShares((Company)result.Company));
            foreach(Company c in Enum.GetValues(typeof(Company)))Assert.AreEqual(before[(int)c],game.Stock(c));
            game.Trade(a,(Company)result.Company,1,false,.1);
            Assert.AreEqual(before[result.Company]+1,game.Stock((Company)result.Company));
        }
        [Test] public void NoPerPlayerOrRoundLimitAndNoAuctionRefill()
        {
            var game=new MatchEngine(Rules(),313,new[]{a,b},0);
            game.EnableStockGacha(new StockGachaSettings{Price=1},313,0);
            for(int i=0;i<75;i++)Assert.IsTrue(game.SpinStockGacha(a,1).Succeeded);
            Assert.AreEqual(1,game.Round);Assert.AreEqual(75,game.StockGacha.OwnSpins(a));
            Assert.AreEqual(125,game.Snapshot(a).Cash);Assert.AreEqual(25,game.StockGacha.Remaining);
            game.Tick(180);Assert.AreEqual(3,game.Round);Assert.AreEqual(25,game.StockGacha.Remaining);
        }
        [Test] public void TimeBoundariesAndOriginalPricesStayIndependent()
        {
            var rules=Rules();var baseline=new MatchEngine(rules,313,new[]{a,b},10);
            var game=new MatchEngine(rules,313,new[]{a,b},10);game.EnableStockGacha(new StockGachaSettings(),313,10);
            Assert.IsFalse(game.SpinStockGacha(a,9).Succeeded);Assert.AreEqual(200,game.Snapshot(a).Cash);
            Assert.IsTrue(game.SpinStockGacha(a,10).Succeeded);
            Assert.IsFalse(game.SpinStockGacha(a,70).Succeeded,"round equality rejects before tick");
            game.Tick(10000);baseline.Tick(10000);Assert.IsFalse(game.SpinStockGacha(a,10001).Succeeded);
            foreach(Company c in Enum.GetValues(typeof(Company)))Assert.AreEqual(baseline.Price(c),game.Price(c));
            Assert.AreEqual(game.Total(a),game.AssetHistory(a)[13]);
        }
        [Test] public void ProbabilitiesFollowRemainingStockAndHandleEmptyBag()
        {
            var game=new StockGachaGame(new StockGachaSettings(),Players(),88);
            Assert.AreEqual(70,BuyBidBlow.Sandbox.StockGacha.StockGachaSandboxPanel.Chance(game.Blanks,game.Remaining));
            game.Spin(a);float sum=BuyBidBlow.Sandbox.StockGacha.StockGachaSandboxPanel.Chance(game.Blanks,game.Remaining);
            foreach(Company c in Enum.GetValues(typeof(Company)))sum+=BuyBidBlow.Sandbox.StockGacha.StockGachaSandboxPanel.Chance(game.Stock(c),game.Remaining);
            Assert.AreEqual(100,sum,.001f);
            Assert.AreEqual(0,BuyBidBlow.Sandbox.StockGacha.StockGachaSandboxPanel.Chance(0,0));
        }
        [Test] public void InvalidSettingsFailBeforePlay()
        {
            Assert.Throws<ArgumentException>(()=>new StockGachaSettings{Price=0}.Validate());
            Assert.Throws<ArgumentException>(()=>new StockGachaSettings{Blanks=-1}.Validate());
            Assert.Throws<ArgumentException>(()=>new StockGachaSettings{Blanks=0,SharesPerCompany=0}.Validate());
            Assert.Throws<ArgumentException>(()=>new StockGachaSettings{SharesPerCompany=int.MaxValue}.Validate());
        }
        [Test] public void DoubleScaleSandboxKeepsOriginalChangeAmountsAndChances()
        {
            var original=Rules();
            var doubled=JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Dev/Sandbox/StockGacha/MatchSettings.json"));doubled.Validate();
            Assert.AreEqual(original.StartingCash*2,doubled.StartingCash);
            Assert.AreEqual(original.MinPrice*2,doubled.MinPrice);
            Assert.AreEqual(JsonUtility.ToJson(original.PriceChange),JsonUtility.ToJson(doubled.PriceChange));
            foreach(int count in new[]{3,6})Assert.AreEqual(original.GetInitialCompanySupply(count)*2,doubled.GetInitialCompanySupply(count));
            foreach(int seed in new[]{1,15,313,12345})
            {
                var expected=original.RollStartingPrices(new System.Random(seed));var actual=doubled.RollStartingPrices(new System.Random(seed));
                for(int c=0;c<3;c++)Assert.AreEqual(expected[c]*2,actual[c]);
            }
            var gacha=JsonUtility.FromJson<StockGachaSettings>(File.ReadAllText("Assets/Project/Dev/Sandbox/StockGacha/StockGachaSettings.json"));
            Assert.AreEqual(20,gacha.Price);Assert.AreEqual(90,gacha.Blanks);Assert.AreEqual(20,gacha.SharesPerCompany);
            var players=Players(10000);var bag=new StockGachaGame(gacha,players,99);int blanks=0;
            for(int i=0;i<150;i++){var result=bag.Spin(a);Assert.IsTrue(result.Succeeded);if(result.Company<0)blanks++;}
            Assert.AreEqual(90,blanks);Assert.IsTrue(bag.Finished);Assert.AreEqual(7000,players[a].Cash);
            foreach(Company c in Enum.GetValues(typeof(Company)))Assert.AreEqual(20,players[a].GetShares(c));
        }
    }
}
