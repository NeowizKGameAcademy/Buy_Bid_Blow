using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;
namespace BuyBidBlow.Dev.Tests.EditMode
{
    public sealed class MenuResultRulesTests
    {
        [Test] public void AssetHistoryUsesSettledRoundPricesAndNeverSkipsRounds()
        {
            var id=new PlayerId("test");var settings=UnityEngine.JsonUtility.FromJson<MatchSettings>(System.IO.File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));
            var engine=new MatchEngine(settings,12345,new[]{id},0,1);
            Assert.AreEqual(1,engine.AssetHistory(id).Length);
            engine.Trade(id,Company.Straw,1,true,.1);
            engine.ToggleBet(id,Company.Timber,.2);
            engine.Tick(1);
            Assert.AreEqual(engine.Total(id),engine.AssetHistory(id)[1]);
            engine.Tick(settings.RoundCount+1);
            Assert.IsTrue(engine.Complete);
            var history=engine.AssetHistory(id);Assert.AreEqual(settings.RoundCount+1,history.Length);
            Assert.AreEqual(engine.Total(id),history[history.Length-1]);
            engine.Tick(10000);Assert.AreEqual(history.Length,engine.AssetHistory(id).Length);
            history[0]=-1;Assert.GreaterOrEqual(engine.AssetHistory(id)[0],0,"records returned as a copy");
        }
    }
}
