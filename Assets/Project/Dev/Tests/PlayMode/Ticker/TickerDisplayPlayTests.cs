using System.Collections;
using BuyBidBlow.Common;
using BuyBidBlow.UI.Ticker;
using Mirror;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode.Ticker
{
    public sealed class TickerDisplayPlayTests
    {
        GameObject board;
        MatchNet match;
        TMP_Text Text(string path)=>board.transform.Find("Ticker UI/"+path).GetComponent<TMP_Text>();
        MatchNet NewMatch()
        {
            var go=new GameObject("Ticker test match",typeof(NetworkIdentity),typeof(MatchNet));
            var value=go.GetComponent<MatchNet>();value.OnStartClient();return value;
        }
        static void ReceiveHistory(MatchNet client,int[] values)
        {
            var server=new SyncList<int>();foreach(int value in values)server.Add(value);
            var writer=new NetworkWriter();server.OnSerializeAll(writer);
            client.priceHistory.OnDeserializeAll(new NetworkReader(writer.ToArraySegment()));
        }
        [UnityTest]
        public IEnumerator DisplayConsumesPublicHistoryAndClearsWhenMatchChanges()
        {
            board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Content/Props/TickerBoard/TickerBoard.prefab"));
            board.name="TickerBoard_Right";
            var display=board.GetComponent<TickerBoardDisplay>();display.ApplyMode();
            yield return null;
            Assert.AreEqual("—",Text("Company detail/Price").text);
            Assert.IsTrue(board.transform.Find("Ticker UI/Company detail").gameObject.activeSelf);
            Assert.IsFalse(board.transform.Find("Ticker UI/Market overview").gameObject.activeSelf);
            match=NewMatch();
            match.phase=MatchNet.Phase.Playing;match.firstRoundStartsAt=NetworkTime.time;
            match.roundClosesAt=NetworkTime.time+30;
            ReceiveHistory(match,new[]{30,38,40,28,39,41,26,40,42,25,39,43,23,38,44,18,40,42,22,36,48,21,37,46,24,35,46});
            yield return null;
            Assert.AreEqual("$24",Text("Company detail/Price").text);
            Assert.AreEqual("+3 (+14.29%)",Text("Company detail/Change").text);
            Assert.AreEqual("$35",Text("Market overview/Price 1").text);
            var plot=board.transform.Find("Ticker UI/Company detail/Price chart").GetComponent<TickerPlot>();
            CollectionAssert.AreEqual(new[]{30f,28f,26f,25f,23f,18f,22f,21f,24f},plot.values);
            Assert.AreEqual("R-5",Text("Company detail/Round tick 0").text);
            Assert.AreEqual("R3",Text("Company detail/Round tick 4").text);
            StringAssert.Contains("00:30",Text("Timer/Countdown").text);
            match.firstRoundStartsAt=NetworkTime.time-8.1;
            yield return null;
            Assert.AreEqual("$35",Text("Company detail/Price").text);
            Assert.AreEqual("-2 (-5.41%)",Text("Company detail/Change").text);
            match.phase=MatchNet.Phase.Finished;
            yield return null;
            StringAssert.Contains("00:00",Text("Timer/Countdown").text);
            match.OnStopClient();Object.Destroy(match.gameObject);match=null;
            yield return null;
            Assert.AreEqual("—",Text("Company detail/Price").text);
            Assert.AreEqual(0,plot.values.Length);
            match=NewMatch();ReceiveHistory(match,new[]{50,60,70});
            match.firstRoundStartsAt=NetworkTime.time;
            yield return null;
            Assert.AreEqual("$50",Text("Company detail/Price").text);
            Assert.AreEqual(1,plot.values.Length);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(match){match.OnStopClient();Object.Destroy(match.gameObject);}
            if(board)Object.Destroy(board);
            yield return null;
        }
    }
}
