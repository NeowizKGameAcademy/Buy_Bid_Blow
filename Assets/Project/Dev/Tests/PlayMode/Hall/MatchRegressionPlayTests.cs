using System.Collections;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Presentation;
using BuyBidBlow.Props;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Notebook;
using BuyBidBlow.UI.Result;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class MatchRegressionPlayTests
    {
        private readonly HostedMatch match = new HostedMatch();
        [UnityTearDown] public IEnumerator Stop() => match.Stop();

        [UnityTest]
        public IEnumerator MatchEntry_KeepsUIInputAndAssetUpdatesWithoutTradeController()
        {
            yield return match.Start();
            Assert.IsNotNull(EventSystem.current);
            Assert.IsTrue(EventSystem.current.isActiveAndEnabled, "lobby hiding must not disable gameplay UI input");
            var hud = Object.FindFirstObjectByType<HUDController>();
            var notebook = Object.FindFirstObjectByType<NetworkNotebookController>();
            Assert.IsNotNull(notebook, "the Network Player HUD is in the local Player");
            Assert.IsNotNull(Object.FindFirstObjectByType<NetworkMatchFeedback>());
            var local = NetworkClient.localPlayer.GetComponent<PlayerNet>();
            Assert.AreEqual(SyncMode.Owner, local.GetComponent<PlayerAssetsNet>().syncMode);
            var assets = Object.FindFirstObjectByType<PlayerAssetStatus>();
            var trade = Object.FindFirstObjectByType<LocalTradeController>();
            trade.enabled = false;
            int cash = assets.Cash, price = MatchNet.Current.GetPrice(Company.Straw);
            local.CmdTrade(Company.Straw, 1, true);
            yield return HostedMatch.Until(() => assets.Cash == cash - price, "asset update independent of trading UI");
            Assert.AreEqual(1, assets.GetShares(Company.Straw));
            notebook.Open();
            Assert.IsTrue(hud.ModalOpen);
            notebook.Close();
            yield return null;
            Assert.IsFalse(hud.ModalOpen);
        }

        [UnityTest]
        public IEnumerator QFromNotebook_DoesNotAlsoStandUp()
        {
            yield return match.Start();
            var notebook = Object.FindFirstObjectByType<NetworkNotebookController>();
            var local = NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var chair = Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).First(s => s.auction);
            local.CmdSit(chair.Id, 0);
            yield return HostedMatch.Until(() => local.IsSeated, "seated before reading information");
            notebook.Open();
            var background = InputSystem.settings.backgroundBehavior;
            var updateMode = InputSystem.settings.updateMode;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
                InputSystem.Update();
                Assert.IsTrue(keyboard.qKey.wasPressedThisFrame, "synthetic key reached the input system");
                // Batch mode has no focused Game view. Keep one input update alive for one game frame.
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return new WaitForSeconds(.1f);
                Assert.IsFalse(notebook.view.IsOpen);
                Assert.IsTrue(local.IsSeated, "one Q consumes the notebook action only");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.updateMode = updateMode;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
                InputSystem.settings.backgroundBehavior = background;
            }
        }

        [UnityTest]
        public IEnumerator Seats_RejectUnknownIdsAndSlotsAndReleaseDisabledLocalSeat()
        {
            yield return match.Start();
            var local = NetworkClient.localPlayer.GetComponent<PlayerNet>();
            RequestResult? reply = null;
            System.Action<RequestResult> listen = value => reply = value;
            PlayerNet.LocalReply += listen;
            try
            {
                local.CmdSit("not-a-seat", 0);
                yield return HostedMatch.Until(() => reply.HasValue, "unknown seat reply");
                Assert.IsFalse(reply.Value.Succeeded); Assert.IsFalse(local.IsSeated);
                var chair = Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).First(s => s.auction);
                reply = null; local.CmdSit(chair.Id, chair.sittingPoints.Length);
                yield return HostedMatch.Until(() => reply.HasValue, "invalid slot reply");
                Assert.IsFalse(reply.Value.Succeeded); Assert.IsFalse(local.IsSeated);
                var lounge = Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).First(s => !s.auction);
                local.CmdSit(lounge.Id, 0);
                yield return HostedMatch.Until(() => local.IsSeated, "lounge seated");
                reply = null; local.CmdBid(1, false);
                yield return HostedMatch.Until(() => reply.HasValue, "lounge bid reply");
                Assert.IsFalse(reply.Value.Succeeded);
                local.CmdStand();
                yield return HostedMatch.Until(() => !local.IsSeated, "lounge released");
                local.CmdSit(chair.Id, 0);
                yield return HostedMatch.Until(() => local.IsSeated, "auction chair seated");
                chair.gameObject.SetActive(false);
                yield return HostedMatch.Until(() => !local.IsSeated, "disabled chair released on server");
            }
            finally { PlayerNet.LocalReply -= listen; }
        }

        [UnityTest]
        public IEnumerator FinishedMatch_ShowsAuthoritativeResultsReturnsAndStartsAgain()
        {
            yield return match.Start(roundSeconds: 1, tweak: rules => { rules.RoundCount = 2; rules.AuctionRounds = new int[0]; });
            var results = Object.FindFirstObjectByType<NetworkMatchFeedback>().results;
            var local = NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var hall = Object.FindFirstObjectByType<HallSession>();
            yield return HostedMatch.Until(() => MatchNet.Current.phase == MatchNet.Phase.Finished && results.IsOpen, "final results shown");
            Assert.AreEqual(1, PlayerNet.LocalResults.Length);
            Assert.IsTrue(PlayerNet.LocalResults[0].Own);
            Assert.AreEqual(400, PlayerNet.LocalResults[0].Total);
            Assert.AreEqual("2 / 2 ROUND", results.Body.text);
            Assert.IsTrue(results.Buttons[0].interactable, "host can return through the result screen");
            results.GetComponentInChildren<DetailedResultsView>().returnButton.onClick.Invoke();
            yield return HostedMatch.Until(() => !local.inMatch && !hall.IsPlaying && !MatchNet.Current, "back in reusable lobby");
            Assert.IsFalse(local.ready);
            Assert.IsNull(PlayerNet.LocalPortfolio);
            Assert.AreEqual(0, PlayerNet.LocalInformation.Count);
            local.SetReady(true); local.CmdStartMatch();
            yield return HostedMatch.Until(() => local.inMatch && hall.IsPlaying && MatchNet.Current && PlayerNet.LocalPortfolio != null, "second match started");
            Assert.AreEqual(400, PlayerNet.LocalPortfolio.Cash);
        }
    }
}
