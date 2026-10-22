using System;
using System.Collections;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    // Hosts a one-player match in the real level, aims at each desk clerk and trades through the server.
    public sealed class DeskTradeTests
    {
        static readonly (string staff, Company company)[] Desks =
            { ("Staff_STRAW_Yellow", Company.Straw), ("Staff_WOOD_Green", Company.Timber), ("Staff_BRICK_Red", Company.Brick) };

        readonly HostedMatch match = new HostedMatch();

        [UnityTearDown]
        public IEnumerator TearDown() => match.Stop();

        [UnityTest]
        public IEnumerator EachDeskClerkOpensItsCompanyAndTradesThroughTheServer()
        {
            yield return match.Start(port: 27995);
            Assert.NotNull(PlayerNet.LocalPortfolio, "starting portfolio sent to the owner");

            var interaction = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
            var detect = UnityEngine.Object.FindFirstObjectByType<DetectObject>();
            var panel = UnityEngine.Object.FindFirstObjectByType<TradePanelController>(FindObjectsInactive.Include);
            var assets = interaction.GetComponentInParent<PlayerAssetStatus>(true);
            if (!assets) assets = UnityEngine.Object.FindFirstObjectByType<PlayerAssetStatus>();
            Assert.AreEqual(PlayerNet.LocalPortfolio.Cash, assets.Cash, "HUD assets match the server portfolio");

            // Aim from the customer side of the desk at eye height; the camera rig is detached so it holds still.
            float eye = detect.transform.position.y - interaction.transform.position.y;
            match.Detach(detect.transform);

            foreach (var (staffName, company) in Desks)
            {
                var staff = GameObject.Find(staffName);
                Assert.NotNull(staff, staffName);
                var body = staff.GetComponent<CapsuleCollider>();
                Assert.NotNull(body, staffName + " collider");
                var greeting = staff.GetComponent<DeskStaffAnimationClient>();
                Assert.NotNull(greeting); Assert.IsFalse(greeting.IsGreeting);
                Assert.IsFalse(greeting.greeting.isLooping);
                var originalPosition = staff.transform.position;
                Vector3 target = body.bounds.center;
                Vector3 eyePos = staff.transform.position + staff.transform.forward * 2.2f + Vector3.up * eye;
                detect.transform.SetPositionAndRotation(eyePos, Quaternion.LookRotation(target - eyePos));
                yield return null; yield return null;

                Assert.IsTrue(detect.isDetected, staffName + " detected");
                Assert.AreEqual(company, detect.DetectedCollider.GetComponentInParent<NPCInfo>().Company, staffName + " company");

                interaction.Interact();
                Assert.IsTrue(panel.IsOpen, staffName + " opens the trade panel");
                Assert.IsTrue(greeting.IsGreeting);

                int cash = assets.Cash, shares = assets.GetShares(company), price = MatchNet.Current.GetPrice(company);
                var view = UnityEngine.Object.FindObjectsByType<TradingPanelView>(FindObjectsSortMode.None).Single();
                Assert.AreEqual(company.ToString(), Label(view, "Company Header/Company Name"), "company panel shown");
                Assert.AreEqual("$" + price, Label(view, "Company Header/Current Price"), "published price shown");

                int stock = MatchNet.Current.GetStock(company);
                Assert.AreEqual(stock + "주", Label(view, "Stock Availability/Market Stock"), "server market stock shown");

                string reply = null;
                Action<string> listen = r => reply = r;
                PlayerNet.LocalReply += listen;
                try
                {
                    panel.Confirm();
                    yield return HostedMatch.Until(() => reply != null, company + " buy answered");
                    Assert.AreEqual(cash - price, assets.Cash, company + " cash after buy: " + reply);
                    Assert.AreEqual(shares + 1, assets.GetShares(company), company + " shares after buy");
                    Assert.AreEqual(shares + 1 + "주", Label(view, "Stock Availability/Owned"), "panel shows the new holding");
                    yield return HostedMatch.Until(() => MatchNet.Current.GetStock(company) == stock - 1, company + " market stock taken");

                    reply = null;
                    panel.SelectTab(TradePanelController.Tab.Sell);
                    panel.Confirm();
                    yield return HostedMatch.Until(() => reply != null, company + " sell answered");
                    Assert.AreEqual(cash, assets.Cash, company + " cash after sell: " + reply);
                    yield return HostedMatch.Until(() => MatchNet.Current.GetStock(company) == stock, company + " market stock returned");

                    // Downside bet: the stake is the current price, cancelling refunds it in full.
                    reply = null;
                    panel.SelectTab(TradePanelController.Tab.DownsideBet);
                    Assert.IsTrue(view.CanConfirm, company + " downside bet offered");
                    panel.Confirm();
                    yield return HostedMatch.Until(() => reply != null, company + " bet answered");
                    Assert.AreEqual(company, PlayerNet.LocalBet, company + " bet placed: " + reply);
                    Assert.AreEqual(cash - price, assets.Cash, company + " stake paid");

                    reply = null;
                    panel.Confirm();
                    yield return HostedMatch.Until(() => reply != null, company + " bet cancel answered");
                    Assert.IsNull(PlayerNet.LocalBet, company + " bet cancelled: " + reply);
                    Assert.AreEqual(cash, assets.Cash, company + " stake refunded");
                }
                finally { PlayerNet.LocalReply -= listen; }
                panel.Close();
                yield return HostedMatch.Until(() => !greeting.IsGreeting, staffName + " returns to idle", 8);
                Assert.Less(Vector3.Distance(originalPosition, staff.transform.position), .001f);
                interaction.Interact();
                Assert.IsTrue(panel.IsOpen); Assert.IsTrue(greeting.IsGreeting, "every visit replays locally");
                panel.Close();
            }
        }

        [UnityTest]
        public IEnumerator RoundsCloseOnTheEngineClockUntilTheMatchEnds()
        {
            yield return match.Start(roundSeconds: 1);
            int bells = 0;
            System.Action<MatchNet.RoundTurn> count = turn => { if (turn.Closed > 0) bells++; };
            MatchNet.RoundTurned += count;
            try { yield return HostedMatch.Until(() => MatchNet.Current.phase == MatchNet.Phase.Finished, "match finished", 40); }
            finally { MatchNet.RoundTurned -= count; }
            Assert.AreEqual(13, MatchNet.Current.lastClosedRound, "last Closing Bell is round 13");
            Assert.AreEqual(13, MatchNet.Current.round, "round stays at the last one");
            Assert.GreaterOrEqual(bells, 12, "a Closing Bell per round");
        }

        static string Label(TradingPanelView view, string path) => view.transform.Find(path).GetComponent<UnityEngine.UI.Text>().text;
    }
}

