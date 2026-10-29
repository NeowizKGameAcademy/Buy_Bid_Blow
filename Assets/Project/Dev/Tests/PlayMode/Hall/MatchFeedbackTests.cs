using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Market;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class MatchFeedbackTests
    {
        readonly HostedMatch match=new HostedMatch();
        readonly List<MatchNotice> notices=new List<MatchNotice>();
        readonly List<AuctionOutcome> outcomes=new List<AuctionOutcome>();
        [UnityTearDown] public IEnumerator Cleanup()
        {PlayerNet.LocalNotice-=notices.Add;PlayerNet.LocalAuctionOutcome-=outcomes.Add;yield return match.Stop();}

        [UnityTest] public IEnumerator MouseTradingBetsAuctionsResultsAndReturnUseServerEvents()
        {
            yield return match.Start(0,settings=>
            {
                settings.RoundCount=5;settings.RoundSeconds=5;settings.AuctionRoundSeconds=8;
                settings.AuctionRounds=new[]{2};settings.AuctionDurationSeconds=3;
                settings.AuctionSlots=new[]{new AuctionSlot{StartsAtSeconds=0,DurationSeconds=3},new AuctionSlot{StartsAtSeconds=3,DurationSeconds=3}};
            });
            PlayerNet.LocalNotice+=notices.Add;PlayerNet.LocalAuctionOutcome+=outcomes.Add;
            var feedback=Object.FindFirstObjectByType<NetworkMatchFeedback>();Assert.IsNotNull(feedback);
            Assert.IsTrue(feedback.notices.gameObject.activeInHierarchy,"notifications enabled");
            var cursor=Object.FindFirstObjectByType<CursorController>();
            var pointer=(Texture2D)typeof(CursorController).GetField("pointer",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(cursor);
            Assert.IsNotNull(pointer);Assert.LessOrEqual(pointer.width,64);Assert.IsTrue(pointer.isReadable);
            Assert.Less(pointer.GetPixel(0,0).a,.1f,"transparent cursor corners");
            var trader=Object.FindFirstObjectByType<LocalTradeController>();
            var panel=Object.FindFirstObjectByType<TradePanelController>();
            trader.OpenPanel(Company.Straw);yield return null;
            var active=Object.FindObjectsByType<TradingPanelView>(FindObjectsSortMode.None).Single(p=>p.gameObject.activeInHierarchy);
            Click(active.transform.Find("Quantity Control/Increase").GetComponent<Button>());
            Click(active.transform.Find("Payment Summary/Confirm Purchase").GetComponent<Button>());
            yield return HostedMatch.Until(()=>PlayerNet.LocalPortfolio.GetShares(Company.Straw)==2,"mouse buy confirmed");
            Assert.IsTrue(notices.Any(n=>n.Kind==MatchNoticeKind.Trade && n.Buy));
            Click(active.transform.Find("Trade Tabs/Sell").GetComponent<Button>());
            Click(active.transform.Find("Payment Summary/Confirm Purchase").GetComponent<Button>());
            yield return HostedMatch.Until(()=>PlayerNet.LocalPortfolio.GetShares(Company.Straw)==1,"mouse sell confirmed");
            Click(active.transform.Find("Trade Tabs/Downside Bet").GetComponent<Button>());
            Click(active.transform.Find("Payment Summary/Confirm Purchase").GetComponent<Button>());
            yield return HostedMatch.Until(()=>PlayerNet.LocalBet==Company.Straw,"bet confirmed");yield return null;
            Assert.IsTrue(feedback.betStatus.activeInHierarchy,"persistent active bet badge");
            Click(active.transform.Find("Close Hint/Keycap").GetComponent<Button>());Assert.IsFalse(panel.IsOpen);
            yield return HostedMatch.Until(()=>MatchNet.Current.round==2,"round two",8);
            yield return HostedMatch.Until(()=>notices.Any(n=>n.Kind==MatchNoticeKind.BetSettled),"server bet settlement notification");
            Assert.IsNull(PlayerNet.LocalBet);yield return null;Assert.IsFalse(feedback.betStatus.activeSelf);
            var seats=Object.FindFirstObjectByType<SeatController>();
            seats.RequestSit(Object.FindObjectsByType<BuyBidBlow.Props.Seat>(FindObjectsSortMode.None).First(s=>s.auction));
            yield return HostedMatch.Until(()=>seats.IsSeated,"auction seat");
            var local=NetworkClient.localPlayer.GetComponent<PlayerNet>();local.CmdBid(1,false);
            yield return HostedMatch.Until(()=>notices.Any(n=>n.Kind==MatchNoticeKind.Accepted),"bid accepted");
            yield return HostedMatch.Until(()=>outcomes.Any(o=>!o.Secret && o.Won),"public auction won",5);
            yield return HostedMatch.Until(()=>feedback.auctionResult.IsOpen,"public result modal");
            Click(feedback.auctionResult.Buttons[1]);
            local.CmdBid(2,false);
            yield return HostedMatch.Until(()=>PlayerNet.LocalSecretBid==2,"secret bid submitted");
            yield return HostedMatch.Until(()=>outcomes.Any(o=>o.Secret && o.Won),"secret auction won",5);
            Assert.IsTrue(string.IsNullOrEmpty(outcomes.First(o=>o.Secret).Winner),"secret result has no winner identity");
            yield return HostedMatch.Until(()=>feedback.auctionResult.IsOpen,"secret result modal");
            Click(feedback.auctionResult.Buttons[1]);
            Assert.AreEqual(4,PlayerNet.LocalInformation.Count,"both won lots delivered privately");
            yield return HostedMatch.Until(()=>PlayerNet.LocalResults!=null && feedback.results.IsOpen,"final results",25);
            Assert.AreEqual(1,PlayerNet.LocalResults[0].Rank);Assert.IsTrue(PlayerNet.LocalResults[0].Own);
            var snapshot=PlayerNet.LocalPortfolio;
            Assert.AreEqual(snapshot.Cash+snapshot.GetShares(Company.Straw)*MatchNet.Current.GetPrice(Company.Straw),PlayerNet.LocalResults[0].Total);
            Assert.IsTrue(feedback.results.Buttons[0].interactable,"room host can return");
            Click(feedback.results.GetComponentInChildren<BuyBidBlow.UI.Result.DetailedResultsView>().returnButton);
            yield return HostedMatch.Until(()=>!match.Room.ClientInMatch,"returned to lobby");
            var session=Object.FindFirstObjectByType<HallSession>();Assert.IsFalse(session.IsPlaying);Assert.IsTrue(session.lobbyPresentation.activeSelf);
            Assert.IsFalse(local.ready);Assert.IsNull(PlayerNet.LocalResults);
        }

        static void Click(Button button)
        {
            Assert.IsNotNull(button);Assert.IsTrue(button.interactable);
            Canvas.ForceUpdateCanvases();
            var eventSystem=EventSystem.current;Assert.IsNotNull(eventSystem);
            Vector2 position=RectTransformUtility.WorldToScreenPoint(null,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));
            var data=new PointerEventData(eventSystem){position=position,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();eventSystem.RaycastAll(data,hits);
            var hit=hits.FirstOrDefault(h=>h.gameObject.GetComponentInParent<Button>()==button);
            Assert.IsNotNull(hit.gameObject,"mouse raycast can reach "+button.name);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
    }
}
