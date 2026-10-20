using System;
using System.IO;
using BuyBidBlow.Systems.Match;
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
            Assert.AreEqual(1,game.PublishedPriceCount);
            Assert.Throws<ArgumentOutOfRangeException>(()=>game.PublishedPrice(1,Company.Straw));
            game.Tick(10);
            Assert.AreEqual(2,game.PublishedPriceCount);
            Assert.AreEqual(initial,game.PublishedPrice(0,Company.Straw));
            Assert.AreEqual(game.Price(Company.Straw),game.PublishedPrice(1,Company.Straw));
            Assert.Throws<ArgumentOutOfRangeException>(()=>game.PublishedPrice(2,Company.Straw));
            game.Tick(10000);
            Assert.AreEqual(18,game.PublishedPriceCount);
            Assert.AreEqual(game.Price(Company.Straw),game.PublishedPrice(17,Company.Straw));
            Assert.Throws<ArgumentOutOfRangeException>(()=>game.PublishedPrice(18,Company.Straw));
        }
        [Test] public void ReplicatedHistoryContainsEveryRevealedSampleForLateClient()
        {
            var game=Game();game.Tick(35);
            var server=new SyncList<int>();
            for(int i=0;i<game.PublishedPriceCount;i++)for(int c=0;c<3;c++)server.Add(game.PublishedPrice(i,(Company)c));
            var writer=new NetworkWriter();server.OnSerializeAll(writer);
            var client=new SyncList<int>();client.OnDeserializeAll(new NetworkReader(writer.ToArraySegment()));
            CollectionAssert.AreEqual(server,client);
            Assert.AreEqual(12,client.Count);
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
