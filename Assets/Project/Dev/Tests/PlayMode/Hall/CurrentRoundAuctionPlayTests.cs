using System.Collections;
using System.Linq;
using System.Reflection;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Props;
using BuyBidBlow.Systems.Round;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.Notebook;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class CurrentRoundAuctionPlayTests
    {
        private readonly HostedMatch match = new HostedMatch();
        [UnityTearDown] public IEnumerator Cleanup() => match.Stop();

        [UnityTest]
        public IEnumerator 품목은오른쪽상시표시_경매현황은착석시가운데표시한다()
        {
            yield return match.Start(12, s => { s.RoundCount = 1; s.AuctionRounds = new[] { 1 }; });
            var panel = HostedMatch.Body.GetComponentInChildren<AuctionPanelView>(true);
            var header = (RectTransform)panel.transform.Find("Auction Status - Top Center");
            var summary = (RectTransform)panel.transform.Find("Lot Summary");
            var controls = panel.transform.Find("Bid Controls - Bottom Center").gameObject;
            yield return null;
            Assert.IsFalse(header.gameObject.activeInHierarchy);
            Assert.IsTrue(summary.gameObject.activeInHierarchy);
            Assert.IsFalse(controls.activeSelf);
            Assert.AreEqual(new Vector2(.5f, 1), header.anchorMin);
            Assert.AreEqual(Vector2.one, summary.anchorMin);
            var position = header.anchoredPosition;
            var size = header.rect.size;
            System.IO.Directory.CreateDirectory("Logs/WoodAuctionReview");
            ScreenCapture.CaptureScreenshot("Logs/WoodAuctionReview/Standing.png");
            yield return null; yield return null;
            var local = NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var chair = Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).First(s => s.auction);
            local.CmdSit(chair.Id, 0);
            yield return HostedMatch.Until(() => local.IsSeated, "seated");
            yield return null;
            Assert.IsTrue(controls.activeSelf);
            Assert.IsTrue(header.gameObject.activeInHierarchy);
            Assert.AreEqual(position, header.anchoredPosition, "sitting does not move or duplicate the public status");
            Assert.AreEqual(size, header.rect.size);
            Assert.AreEqual(1, panel.GetComponentsInChildren<RectTransform>(true).Count(r => r.name == "Lot Summary"));
            ScreenCapture.CaptureScreenshot("Logs/WoodAuctionReview/Seated.png");
            yield return null; yield return null;
            local.CmdStand();
            yield return HostedMatch.Until(() => !local.IsSeated, "standing");
            yield return HostedMatch.Until(() => MatchNet.Current.auctionStage == MatchNet.AuctionStage.None, "auction ended", 8);
            yield return null;
            Assert.IsFalse(header.gameObject.activeInHierarchy, "auction status hides when standing");
            Assert.IsTrue(summary.gameObject.activeInHierarchy, "the public lot is retained until the round ends");
            Assert.IsFalse(controls.activeSelf);
            ScreenCapture.CaptureScreenshot("Logs/WoodAuctionReview/Closed.png");
            yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator 공개와비공개_현재정보표시와개인전달후거래를유지한다()
        {
            yield return match.Start(8, s => { s.RoundCount = 3; s.AuctionRounds = new[] { 1, 2, 3 }; });
            var local = NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var chair = Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).First(s => s.auction);
            local.CmdSit(chair.Id, 0);
            yield return HostedMatch.Until(() => local.IsSeated, "server approved seat");
            var panel = HostedMatch.Body.GetComponentInChildren<AuctionPanelView>(true);
            var rounds = (Text[])typeof(AuctionPanelView).GetField("lotRounds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
            var envelope = Object.FindFirstObjectByType<AuctionLotDisplayClient>();
            Assert.IsNotNull(envelope);
            Assert.IsNotNull(MatchTimeline.Current);
            for (int round = 1; round <= 2; round++)
            {
                int targetRound = round;
                var expectedStage = round == 1 ? MatchNet.AuctionStage.Open : MatchNet.AuctionStage.Secret;
                yield return HostedMatch.Until(() => MatchNet.Current.round == targetRound && MatchNet.Current.auctionStage == expectedStage, "auction stage");
                yield return null;
                Assert.AreEqual(round, MatchNet.Current.auctionLot.firstRound);
                Assert.AreEqual(0, MatchNet.Current.auctionLot.secondRound);
                Assert.IsTrue(rounds[0].gameObject.activeSelf);
                Assert.IsFalse(rounds[1].gameObject.activeSelf, "second information row is hidden");
                Assert.AreEqual(round + "R", rounds[0].text);
                Assert.IsTrue(envelope.display.gameObject.activeInHierarchy);
                Assert.AreEqual(round + "R", envelope.display.roundLabel.text);
                var span = MatchTimeline.Current.spans.Single(s => s.Round == targetRound &&
                    (s.Kind == TimelineSpanKind.OpenAuction || s.Kind == TimelineSpanKind.SecretAuction));
                Assert.AreEqual(MatchNet.Current.auctionClosesAt, span.EndsAt, .001);
                int bid = round;
                local.CmdBid(bid, false);
                yield return HostedMatch.Until(() => PlayerNet.LocalPortfolio.Reserved == bid, "reservation published");
                yield return HostedMatch.Until(() => PlayerNet.LocalInformation.Count == targetRound, "one owned information entry per lot", 6);
                yield return null;
                Assert.AreEqual(0, PlayerNet.LocalPortfolio.Reserved);
                Assert.AreEqual(MatchNet.AuctionStage.None, MatchNet.Current.auctionStage);
                Assert.IsFalse(envelope.display.gameObject.activeSelf);
                Assert.AreEqual(MatchNet.Phase.Playing, MatchNet.Current.phase);
                Assert.AreEqual(round, PlayerNet.LocalInformation[round - 1].Round);
                int cash = PlayerNet.LocalPortfolio.Cash;
                var company = BuyBidBlow.Systems.Types.Company.Timber;
                int price = MatchNet.Current.GetPrice(company);
                local.CmdTrade(company, 1, true);
                yield return HostedMatch.Until(() => PlayerNet.LocalPortfolio.Cash == cash - price, "trading after auction closes");
                local.CmdTrade(company, 1, false);
                yield return HostedMatch.Until(() => PlayerNet.LocalPortfolio.Cash == cash, "sell remains unchanged");
                var notebook = HostedMatch.Body.GetComponentInChildren<NetworkNotebookController>(true);
                notebook.Open();
                Assert.IsTrue(notebook.view.IsOpen);
                notebook.Close();
            }
            local.CmdStand();
            yield return HostedMatch.Until(() => !local.IsSeated, "standing restores seat");
        }
    }
}
