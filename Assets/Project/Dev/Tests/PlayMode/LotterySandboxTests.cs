using System.Collections;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Sandbox.Lottery;
using Mirror;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class LotterySandboxTests
    {
        GameNetworkManager room;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(room&&(NetworkServer.active||NetworkClient.active))room.StopHost();
            yield return null;if(room)Object.Destroy(room.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator RealHallServerPurchaseDrawAndNotification()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Project/Dev/Sandbox/Lottery/LotterySandbox.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            room=Object.FindFirstObjectByType<GameNetworkManager>();Assert.IsTrue(room.LotterySandboxEnabled);
            room.LocalNickname="LottoTester";room.StartHost();
            yield return HostedMatch.Until(()=>NetworkClient.localPlayer,"lottery player spawn");
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();player.SetReady(true);
            yield return HostedMatch.Until(()=>room.CanStartMatch,"lottery ready");
            NetworkClient.Send(new LotteryStart{Quick=true});
            yield return HostedMatch.Until(()=>MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing,"lottery match");
            var device=Object.FindFirstObjectByType<LotteryDevice>();Assert.IsNotNull(device);
            player.transform.SetPositionAndRotation(device.transform.position+Vector3.forward*2+Vector3.up*.04f,Quaternion.Euler(0,180,0));
            Physics.SyncTransforms();yield return null;
            yield return HostedMatch.Until(()=>!BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput,"round banner input release");
            if(!Application.isBatchMode){yield return new WaitForEndOfFrame();Capture("Library/LotteryDevicePreview.png");}
            var panel=Object.FindFirstObjectByType<LotterySandboxPanel>();
            player.GetComponent<PlayerInteraction>().Interact();yield return null;
            Assert.IsTrue(panel.IsOpen,"aimed device uses the production interaction path");
            Assert.AreEqual(100,LotterySandboxClient.State.Capacity);Assert.AreEqual(10,LotterySandboxClient.State.Limit);
            panel.Purchase(2);
            yield return HostedMatch.Until(()=>LotterySandboxClient.State.Own==2,"server accepted tickets");
            Assert.AreEqual(190,PlayerNet.LocalPortfolio.Cash);Assert.AreEqual(40,LotterySandboxClient.State.Prize);
            if(!Application.isBatchMode){yield return new WaitForEndOfFrame();Capture("Library/LotteryPanelPreview.png");}
            panel.Purchase(9);yield return null;yield return null;
            Assert.AreEqual(2,room.LotterySandboxEngine.Lottery.Tickets(GameNetworkManager.PlayerIdOf(NetworkServer.localConnection)));
            // Exercise the real round-close hook immediately; the UI timer normally waits 45 seconds in quick mode.
            room.LotterySandboxEngine.Tick(room.LotterySandboxEngine.LotteryDeadline);
            yield return HostedMatch.Until(()=>LotterySandboxClient.Outcome.Draw==1,"all-client draw notification");
            yield return HostedMatch.Until(()=>LotterySandboxClient.State.Draw==2,"next sale state");
            Assert.AreEqual(0,LotterySandboxClient.State.Own);
            Assert.AreEqual(LotterySandboxClient.Outcome.Won?50:90,LotterySandboxClient.State.Prize);
            var cash=room.LotterySandboxEngine.Snapshot(GameNetworkManager.PlayerIdOf(NetworkServer.localConnection)).Cash;
            room.LotterySandboxEngine.Tick(room.LotterySandboxEngine.Deadline-1);
            Assert.AreEqual(cash,room.LotterySandboxEngine.Snapshot(GameNetworkManager.PlayerIdOf(NetworkServer.localConnection)).Cash);
            panel.Close();Assert.IsFalse(panel.IsOpen);
        }
        static void Capture(string path)
        {
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes(path,image.EncodeToPNG());Object.Destroy(image);
        }
    }
}
