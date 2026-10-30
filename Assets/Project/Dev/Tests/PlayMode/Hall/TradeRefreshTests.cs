using System.Collections;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Market;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class TradeRefreshTests
    {
        readonly HostedMatch match=new HostedMatch();
        [UnityTearDown]public IEnumerator Cleanup(){yield return match.Stop();}
        [UnityTest]public IEnumerator OpenPanelTracksPublicStockPrivateAssetsAndQuantityWrap()
        {
            yield return match.Start(port:27997);
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var trader=HostedMatch.Body.GetComponent<LocalTradeController>();
            var panel=HostedMatch.Body.GetComponentInChildren<TradePanelController>(true);
            trader.OpenPanel(Company.Straw);yield return null;
            var view=panel.GetComponentsInChildren<TradingPanelView>(true).Single(v=>v.gameObject.activeSelf);
            string Label(string name)=>view.GetComponentsInChildren<Text>(true).First(t=>t.name==name).text;
            void Click(string path)=>view.transform.Find(path).GetComponent<Button>().onClick.Invoke();
            int originalStock=MatchNet.Current.GetStock(Company.Straw);
            int maximum=int.Parse(Label("Maximum").Replace("주",""));
            Assert.Greater(maximum,1);
            Click("Quantity Control/Decrease");Assert.AreEqual(maximum.ToString(),Label("Quantity"));
            Click("Quantity Control/Increase");Assert.AreEqual("1",Label("Quantity"));
            Click("Payment Summary/Confirm Purchase");
            yield return HostedMatch.Until(()=>PlayerNet.LocalPortfolio.GetShares(Company.Straw)==1,"buy confirmed");
            yield return null;yield return null;
            Assert.AreEqual((originalStock-1)+"주",Label("Market Stock"));Assert.AreEqual("1주",Label("Owned"));
            panel.SelectTab(TradePanelController.Tab.Sell);yield return null;panel.Confirm();
            yield return HostedMatch.Until(()=>PlayerNet.LocalPortfolio.GetShares(Company.Straw)==0,"sell confirmed");
            yield return null;yield return null;
            Assert.AreEqual(originalStock+"주",Label("Market Stock"));Assert.AreEqual("0주",Label("Owned"));
            panel.SelectTab(TradePanelController.Tab.DownsideBet);yield return null;panel.Confirm();
            yield return HostedMatch.Until(()=>PlayerNet.LocalBet==Company.Straw,"bet entered");
            yield return null;yield return null;Assert.AreEqual("베팅 취소",Label("Action"));
            panel.Confirm();yield return HostedMatch.Until(()=>!PlayerNet.LocalBet.HasValue,"bet cancelled");
            yield return null;yield return null;Assert.AreEqual("베팅 확인",Label("Action"));
            panel.SelectTab(TradePanelController.Tab.Buy);
            // A public market update without a local portfolio RPC (another participant's trade).
            MatchNet.Current.stock[0]=2;yield return null;yield return null;
            Assert.AreEqual("2주",Label("Market Stock"));Assert.AreEqual("2주",Label("Maximum"));
            panel.DecreaseQuantity();Assert.AreEqual("2",Label("Quantity"));
            MatchNet.Current.stock[0]=1;yield return null;yield return null;
            Assert.AreEqual("1",Label("Quantity"));
            MatchNet.Current.stock[0]=0;yield return null;yield return null;
            Assert.AreEqual("0주",Label("Maximum"));Assert.IsFalse(view.CanConfirm);
            MatchNet.Current.stock[0]=originalStock;yield return null;yield return null;
            Assert.IsTrue(view.CanConfirm);
            MatchNet.Current.phase=MatchNet.Phase.Finished;yield return null;yield return null;
            Assert.IsFalse(view.CanConfirm,"phase changes disable an open trading panel");
        }
    }
}
