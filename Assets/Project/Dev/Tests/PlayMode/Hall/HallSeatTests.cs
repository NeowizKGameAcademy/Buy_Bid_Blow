using System.Collections;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Props;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.Systems.Types;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    // A mezzanine chair in a hosted match: the server grants the seat, the player sits in the
    // seated pose without any motion, and standing up gives the seat back.
    public sealed class HallSeatTests
    {
        const string ChairName = "A41_Front_Seat_2";
        readonly HostedMatch match = new HostedMatch();

        [UnityTearDown]
        public IEnumerator TearDown() => match.Stop();

        [UnityTest]
        public IEnumerator MezzanineChairSeatsThePlayerThroughTheServer()
        {
            yield return match.Start();
            var net = NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var interaction = Object.FindFirstObjectByType<PlayerInteraction>();
            var seats = interaction.GetComponentInParent<SeatController>();
            var seating = seats.GetComponent<PlayerSeating>();
            var detect = Object.FindFirstObjectByType<InteractionSensor>();
            var model = seats.GetComponentsInChildren<Animator>().First(a => a.runtimeAnimatorController);
            var chair = Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).Single(s => s.name == ChairName);
            var anchor = chair.sittingPoints[0];

            // Aim at the chair from the aisle beside it; the camera rig is detached so it holds still.
            detect.transform.SetParent(null, true);
            var eye = anchor.position + anchor.right * 1.3f + Vector3.up * .6f;
            detect.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(anchor.position - eye));
            yield return null; yield return null;
            Assert.IsNotNull(detect.Target, "chair detected");
            Assert.AreEqual(chair, detect.Target, "aiming at the chair");
            Assert.IsTrue(chair.CanInteract, "free chair offers Sit");

            interaction.Interact();
            yield return HostedMatch.Until(() => seating.IsSeated, "seated");
            Assert.AreEqual(ChairName, net.hallSeat, "server granted this chair");
            Assert.IsFalse(chair.CanInteract, "an occupied chair no longer offers Sit");
            Assert.Less(Vector3.Distance(seating.transform.position, anchor.position - Vector3.up * seating.hipHeight), .01f, "snapped onto the chair");
            yield return null;
            Assert.IsTrue(model.GetCurrentAnimatorStateInfo(0).IsName("Seated"), "seated pose shown at once");

            seats.RequestStand();
            yield return HostedMatch.Until(() => !seating.IsSeated, "stood up");
            Assert.IsFalse(net.IsSeated, "server freed the chair");
            Assert.IsTrue(chair.CanInteract, "chair free again");
            yield return null;
            Assert.IsTrue(model.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), "back to standing");

            // Other players see the network avatar switch straight into the same pose.
            // The owner's own avatar is hidden in the level; show it as other clients do.
            var avatar = net.GetComponent<BBBWolfCharacter>();
            foreach (Transform child in net.transform) child.gameObject.SetActive(true);
            yield return null;
            avatar.SetSeated(true);
            yield return null;
            Assert.IsTrue(avatar.animator.GetCurrentAnimatorStateInfo(0).IsName("Seated"), "avatar seated pose");
        }

        [UnityTest]
        public IEnumerator AuctionPanelShowsWhileSeatedOnAMezzanineChair()
        {
            // 2-second rounds: round 2 is the first auction round and opens its public auction at once.
            yield return match.Start(roundSeconds: 2);
            var seats = Object.FindFirstObjectByType<SeatController>();
            var seating = seats.GetComponent<PlayerSeating>();
            var panel = Object.FindFirstObjectByType<AuctionPanelView>();
            var header = panel.transform.Find("Auction Status - Top Center").gameObject;
            var lot = panel.transform.Find("Live Lot Details").gameObject;
            var seatsByName = Object.FindObjectsByType<Seat>(FindObjectsSortMode.None);
            Assert.AreEqual(7, seatsByName.Count(s => s.auction), "the seven mezzanine chairs are auction seats");
            Assert.IsFalse(header.activeSelf, "no auction panel while standing");

            seats.RequestSit(seatsByName.Single(s => s.name == ChairName));
            yield return HostedMatch.Until(() => seating.IsSeated, "seated");
            yield return null;
            Assert.IsTrue(header.activeSelf, "auction panel shown when seated");
            Assert.AreEqual("진행 중인 경매 없음", Text(header, "Stage"), "round 1 has no auction");
            Assert.AreEqual("다음 경매 2R", Text(header, "Highest Bid"), "next auction round shown");

            yield return HostedMatch.Until(() => MatchNet.Current.round == 2, "auction round");
            yield return HostedMatch.Until(() => MatchNet.Current.auctionStage == MatchNet.AuctionStage.Open, "public auction published");
            yield return null;
            Assert.AreEqual("1차 공개 경매", Text(header, "Stage"), "public auction stage");
            Assert.IsTrue(lot.activeSelf, "lot shown");
            StringAssert.Contains("R · ", Text(lot, "Text"), "lot rounds and companies");

            seats.RequestStand();
            yield return HostedMatch.Until(() => !seating.IsSeated, "stood up");
            yield return null;
            Assert.IsTrue(header.activeSelf, "public auction remains visible after standing");
            Assert.IsFalse(panel.transform.Find("Bid Controls - Bottom Center").gameObject.activeSelf, "standing players cannot operate bids");
        }

        [UnityTest]
        public IEnumerator SeatedPlayerBidsInBothAuctions()
        {
            // Short rules: 3-second rounds, round 2's open auction for 3 s, then its secret auction for 3 s.
            yield return match.Start(tweak: s =>
            {
                s.RoundSeconds = 3; s.AuctionRoundSeconds = 8; s.AuctionDurationSeconds = 3;
                s.AuctionSlots = new[] { new AuctionSlot { StartsAtSeconds = 0, DurationSeconds = 3 }, new AuctionSlot { StartsAtSeconds = 3, DurationSeconds = 3 } };
            });
            var seats = Object.FindFirstObjectByType<SeatController>();
            var seating = seats.GetComponent<PlayerSeating>();
            var bidding = Object.FindFirstObjectByType<AuctionSeatPanel>();
            var panel = bidding.GetComponent<AuctionPanelView>();
            var assets = seats.GetComponentInParent<PlayerAssetStatus>(true);
            if (!assets) assets = Object.FindFirstObjectByType<PlayerAssetStatus>();
            var header = panel.transform.Find("Auction Status - Top Center").gameObject;
            var controls = panel.transform.Find("Bid Controls - Bottom Center").gameObject;
            string reply = null;
            System.Action<RequestResult> listen = r => reply = r.Message;
            PlayerNet.LocalReply += listen;
            try
            {
                seats.RequestSit(Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).Single(s => s.name == ChairName));
                yield return HostedMatch.Until(() => seating.IsSeated, "seated");
                int cash = assets.Cash;

                // Open auction: the bid becomes the public highest bid and reserves its cash.
                yield return HostedMatch.Until(() => MatchNet.Current.auctionStage == MatchNet.AuctionStage.Open, "open auction");
                yield return null;
                Assert.IsTrue(controls.activeSelf, "bid controls shown");
                bidding.AdjustBid(4);
                bidding.SubmitBid();
                yield return HostedMatch.Until(() => reply != null, "open bid answered");
                yield return HostedMatch.Until(() => MatchNet.Current.highestBid == 5, "public highest bid: " + reply);
                Assert.AreEqual("Tester", MatchNet.Current.highestBidder, "bidder shown by nickname");
                Assert.AreEqual(5, assets.Reserved, "bid cash reserved");
                yield return null;
                Assert.AreEqual("현재 최고 입찰 중", Text(header, "Leading Status"), "leading shown");

                // Secret auction: the submission is private, can be cancelled, and the open win was charged.
                yield return HostedMatch.Until(() => MatchNet.Current.auctionStage == MatchNet.AuctionStage.Secret, "secret auction");
                yield return HostedMatch.Until(() => assets.Cash == cash - 5 && assets.Reserved == 0, "open auction won and charged");
                Assert.AreEqual(2, PlayerNet.LocalInformation.Count, "winner receives both private information entries");
                Assert.AreEqual(0, MatchNet.Current.highestBid, "secret bids are not public");
                reply = null;
                bidding.AdjustBid(2);
                bidding.SubmitBid();
                yield return HostedMatch.Until(() => reply != null, "secret bid answered");
                Assert.AreEqual(3, PlayerNet.LocalSecretBid, "own secret submission known: " + reply);
                Assert.AreEqual(3, assets.Reserved, "secret bid cash reserved");
                yield return null;
                Assert.AreEqual("내 제출 $3", Text(header, "Highest Bid"), "own submission shown");

                reply = null;
                bidding.CancelSecret();
                yield return HostedMatch.Until(() => reply != null, "cancel answered");
                Assert.AreEqual(0, PlayerNet.LocalSecretBid, "submission cancelled: " + reply);
                Assert.AreEqual(0, assets.Reserved, "reserved cash released");
            }
            finally { PlayerNet.LocalReply -= listen; }
        }

        static string Text(GameObject parent, string path) => parent.transform.Find(path).GetComponent<UnityEngine.UI.Text>().text;
    }
}
