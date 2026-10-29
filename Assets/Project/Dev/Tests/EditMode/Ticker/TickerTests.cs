using System;
using System.IO;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Ticker;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace BuyBidBlow.Dev.Tests.EditMode.Ticker
{
    public sealed class TickerTests
    {
        MatchEngine Game()=>new MatchEngine(JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json")),1234,new[]{new PlayerId("a"),new PlayerId("b"),new PlayerId("c")},0,10);
        [Test] public void HistoryNeverExposesFutureAndIncludesFinalClose()
        {
            var game=Game();int initial=game.Price(Company.Straw);
            Assert.AreEqual(6,game.PublishedPriceCount);
            Assert.Throws<ArgumentOutOfRangeException>(()=>game.PublishedPrice(6,Company.Straw));
            game.Tick(10);
            Assert.AreEqual(7,game.PublishedPriceCount);
            Assert.AreEqual(initial,game.PublishedPrice(5,Company.Straw));
            Assert.AreEqual(game.Price(Company.Straw),game.PublishedPrice(6,Company.Straw));
            Assert.Throws<ArgumentOutOfRangeException>(()=>game.PublishedPrice(7,Company.Straw));
            game.Tick(10000);
            Assert.AreEqual(19,game.PublishedPriceCount);
            Assert.AreEqual(game.Price(Company.Straw),game.PublishedPrice(18,Company.Straw));
            Assert.Throws<ArgumentOutOfRangeException>(()=>game.PublishedPrice(19,Company.Straw));
        }
        [Test] public void ReplicatedHistoryContainsEveryRevealedSampleForLateClient()
        {
            var game=Game();game.Tick(35);
            var server=new SyncList<int>();
            for(int i=0;i<game.PublishedPriceCount;i++)for(int c=0;c<3;c++)server.Add(game.PublishedPrice(i,(Company)c));
            var writer=new NetworkWriter();server.OnSerializeAll(writer);
            var client=new SyncList<int>();client.OnDeserializeAll(new NetworkReader(writer.ToArraySegment()));
            CollectionAssert.AreEqual(server,client);
            Assert.AreEqual(27,client.Count);
        }
        [Test] public void HistoryIsReproducibleAndPreservesOriginalFuturePrices()
        {
            var settings=JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));
            for(int seed=0;seed<100;seed++)
            {
                var random=new System.Random(seed);
                var original=settings.RollStartingPrices(random);
                var history=PriceChangeGenerator.GenerateHistory(settings.PriceChange,original,settings.MinPrice,seed);
                var replay=PriceChangeGenerator.GenerateHistory(settings.PriceChange,original,settings.MinPrice,seed);
                var game=new MatchEngine(settings,seed,new[]{new PlayerId("a")},0,10);
                for(int i=0;i<6;i++)for(int c=0;c<3;c++)
                {
                    Assert.GreaterOrEqual(history[i][c],settings.MinPrice);
                    Assert.AreEqual(history[i][c],replay[i][c]);
                    Assert.AreEqual(history[i][c],game.PublishedPrice(i,(Company)c));
                }
                CollectionAssert.AreEqual(original,history[5]);
                for(int round=1;round<=settings.RoundCount;round++)
                {
                    foreach(var change in PriceChangeGenerator.Generate(settings.PriceChange,random))
                        original[(int)change.Company]=Math.Max(settings.MinPrice,original[(int)change.Company]+change.Amount);
                    game.Tick(round*10);
                    for(int c=0;c<3;c++)Assert.AreEqual(original[c],game.Price((Company)c));
                }
            }
        }
        [TestCase(30, "00:30")][TestCase(0.1,"00:01")][TestCase(-5,"00:00")][TestCase(240,"04:00")]
        public void CountdownDoesNotShowZeroEarly(double seconds,string expected)=>Assert.AreEqual(expected,TickerFormat.Clock(seconds));
        [Test] public void ChangesUsePreviousPriceAndFlatChartHasNonzeroRange()
        {
            Assert.AreEqual("+14.29%",TickerFormat.Percent(24,21));
            Assert.AreEqual("-5.41%",TickerFormat.Percent(35,37));
            Assert.AreEqual("0.00%",TickerFormat.Percent(46,46));
            TickerFormat.PriceRange(new[]{46f,46f},out float min,out float max);
            Assert.Less(min,46);Assert.Greater(max,46);
        }
    }
}
