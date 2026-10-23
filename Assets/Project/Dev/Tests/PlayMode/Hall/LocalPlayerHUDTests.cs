using System.Collections;
using BuyBidBlow.Characters;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Market;
using BuyBidBlow.UI.Notebook;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class LocalPlayerHUDTests
    {
        readonly HostedMatch match=new HostedMatch();
        [UnityTearDown]public IEnumerator Cleanup(){yield return match.Stop();}
        [UnityTest]public IEnumerator OnlyOwnedPlayerEnablesHUDAndUsesOwnComponents()
        {
            yield return match.Start();yield return null;
            var local=NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var hud=local.GetComponentInChildren<NetworkPlayerHUD>(true);
            Assert.IsNotNull(hud);Assert.IsTrue(hud.gameObject.activeInHierarchy);
            var session=Object.FindFirstObjectByType<HallSession>();Assert.IsFalse(session.gameplayPresentation.activeSelf);
            var notebook=hud.GetComponent<NetworkNotebookController>();var feedback=hud.GetComponent<NetworkMatchFeedback>();
            Assert.AreSame(local.GetComponent<PlayerMove>(),notebook.movement);Assert.AreSame(notebook.movement,feedback.movement);
            Assert.AreSame(local.GetComponentInChildren<TradePanelController>(true),notebook.trading);Assert.AreSame(notebook.trading,feedback.trading);
            Assert.IsNotNull(UnityEngine.EventSystems.EventSystem.current);
            var remote=Object.Instantiate(match.Room.playerPrefab);remote.name="Remote ownership verification";
            try
            {
                var net=remote.GetComponent<PlayerNet>();net.SetGameplayActive(true);
                yield return null;
                Assert.IsFalse(remote.GetComponentInChildren<NetworkPlayerHUD>(true).gameObject.activeInHierarchy);
                Assert.IsFalse(remote.GetComponent<BuyBidBlow.Common.LocalTradeController>().enabled);
                Assert.IsFalse(remote.GetComponent<SeatController>().enabled);
                Assert.IsTrue(hud.gameObject.activeInHierarchy,"remote state must not hide local HUD");
                local.SetGameplayActive(false);Assert.IsFalse(hud.gameObject.activeSelf);
                local.SetGameplayActive(true);Assert.IsTrue(hud.gameObject.activeInHierarchy);
                Assert.IsFalse(feedback.results.IsOpen,"re-enable must not open result modal");
            }
            finally{Object.Destroy(remote);}
        }
    }
}
