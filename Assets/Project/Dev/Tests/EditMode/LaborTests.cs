using System.IO;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;
using UnityEngine;
namespace BuyBidBlow.Dev.Tests.EditMode
{
    public sealed class LaborTests
    {
        [Test] public void WagesLimitsBombsAndNewRound()
        {
            var settings=JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Dev/Sandbox/StockGacha/MatchSettings.json"));
            var p=new PlayerId("worker");var game=new MatchEngine(settings,18,new[]{p},5);
            Assert.IsFalse(game.DeliverLabor(p,Company.Straw,false,4,out _));
            int initial=game.Snapshot(p).Cash,pay=game.Price(Company.Straw)/5;
            for(int i=0;i<5;i++){Assert.IsTrue(game.DeliverLabor(p,Company.Straw,false,6,out int wage));Assert.AreEqual(pay,wage);}
            Assert.AreEqual(initial+pay*5,game.Snapshot(p).Cash);
            Assert.IsFalse(game.DeliverLabor(p,Company.Brick,false,6,out _));
            Assert.IsTrue(game.DeliverLabor(p,Company.Straw,true,6,out int bombPay));Assert.AreEqual(0,bombPay);
            Assert.AreEqual(2,game.RoundPriceAdjustment(Company.Straw));
            Assert.IsFalse(game.DeliverLabor(p,Company.Timber,true,6,out _));
            Assert.IsFalse(game.DeliverLabor(p,Company.Straw,false,game.Deadline,out _));
            game.Tick(game.Deadline);Assert.AreEqual(0,game.LaborCount(p,false));Assert.AreEqual(0,game.RoundPriceAdjustment(Company.Straw));
            Assert.IsTrue(game.DeliverLabor(p,Company.Straw,false,game.Deadline-1,out _));
        }
    }
}
