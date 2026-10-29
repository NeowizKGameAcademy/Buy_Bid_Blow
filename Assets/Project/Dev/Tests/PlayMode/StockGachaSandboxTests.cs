using System.Collections;
using System.Reflection;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Sandbox.StockGacha;
using BuyBidBlow.Systems.Types;
using Mirror;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class StockGachaSandboxTests
    {
        GameNetworkManager room;
        Transform detachedCamera;
        Keyboard keyboard;InputSettings previousInputSettings;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(previousInputSettings)InputSystem.settings=previousInputSettings;
            if(room&&(NetworkServer.active||NetworkClient.active))room.StopHost();
            if(detachedCamera)Object.Destroy(detachedCamera.gameObject);
            yield return null;if(room)Object.Destroy(room.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator RealHallCursorKeyboardStockAwardAndDuplicateProtection()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Project/Dev/Sandbox/StockGacha/StockGachaSandbox.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            room=Object.FindFirstObjectByType<GameNetworkManager>();Assert.IsTrue(room.StockGachaSandboxEnabled);
            int seed=1;while(new System.Random(unchecked(seed^(int)0x4B8E1593)).Next(150)<90)seed++;
            typeof(GameNetworkManager).GetField("randomSeed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(room,seed);
            room.LocalNickname="GachaTester";room.StartHost();
            yield return HostedMatch.Until(()=>NetworkClient.localPlayer,"gacha player spawn");
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();player.SetReady(true);
            yield return HostedMatch.Until(()=>room.CanStartMatch,"gacha ready");
            NetworkClient.Send(new StockGachaStart());
            yield return HostedMatch.Until(()=>MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing,"gacha match");
            Assert.AreEqual(90,room.StockGachaSandboxEngine.Settings.RoundSeconds);
            Assert.AreEqual(150,room.StockGachaSandboxEngine.Settings.AuctionRoundSeconds);
            Assert.AreEqual(13,room.StockGachaSandboxEngine.Settings.RoundCount);
            Assert.AreEqual(60,room.StockGachaSandboxEngine.Settings.AuctionSlots[0].DurationSeconds);
            Assert.AreEqual(30,room.StockGachaSandboxEngine.Settings.AuctionSlots[1].DurationSeconds);
            var device=Object.FindFirstObjectByType<StockGachaDevice>();Assert.IsNotNull(device);
            player.transform.SetPositionAndRotation(device.transform.position+Vector3.forward*2+Vector3.up*.04f,Quaternion.Euler(0,180,0));
            Physics.SyncTransforms();yield return null;
            yield return HostedMatch.Until(()=>!BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput,"round input release");
            var panel=Object.FindFirstObjectByType<StockGachaSandboxPanel>();
            player.GetComponent<PlayerInteraction>().Interact();yield return null;yield return null;
            Assert.IsTrue(panel.IsOpen);
            Assert.AreEqual(CursorLockMode.None,Cursor.lockState);Assert.IsTrue(Cursor.visible);
            Assert.AreEqual(150,StockGachaSandboxClient.State.Remaining);Assert.AreEqual(20,StockGachaSandboxClient.State.Price);
            Assert.AreEqual(400,PlayerNet.LocalPortfolio.Cash);
            CollectionAssert.AreEqual(new[]{.35f,.5f,.75f},MatchNet.Current.riseChances.ToArray());
            Assert.AreEqual(10,MatchNet.Current.minPrice);
            previousInputSettings=InputSystem.settings;var inputSettings=Object.Instantiate(previousInputSettings);
            inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=inputSettings;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            yield return Press(Key.Enter);
            yield return HostedMatch.Until(()=>StockGachaSandboxClient.State.OwnSpins==1&&!StockGachaSandboxClient.Pending,"server gacha result");
            Assert.AreEqual(380,PlayerNet.LocalPortfolio.Cash);
            Assert.GreaterOrEqual(StockGachaSandboxClient.Outcome.Company,0,"seed chosen for a share award");
            var awarded=(Company)StockGachaSandboxClient.Outcome.Company;
            Assert.AreEqual(1,PlayerNet.LocalPortfolio.GetShares(awarded));
            Assert.AreEqual(20,room.StockGachaSandboxEngine.Stock(awarded),"doubled market stock not used");
            Assert.AreEqual(149,StockGachaSandboxClient.State.Remaining);
            if(!Application.isBatchMode){yield return new WaitForEndOfFrame();Capture("Library/StockGachaPanelPreview.png");}
            NetworkClient.Send(new StockGachaPurchase{Session=StockGachaSandboxClient.State.Session,Request=1});
            yield return null;yield return null;Assert.AreEqual(380,PlayerNet.LocalPortfolio.Cash);
            Assert.AreEqual(149,room.StockGachaSandboxEngine.StockGacha.Remaining);
            NetworkClient.Send(new StockGachaPurchase{Session="previous-match",Request=2});
            yield return null;yield return null;Assert.AreEqual(380,PlayerNet.LocalPortfolio.Cash);
            yield return Press(Key.Enter);
            yield return HostedMatch.Until(()=>StockGachaSandboxClient.State.OwnSpins==2&&!StockGachaSandboxClient.Pending,"second draw same round");
            Assert.AreEqual(360,PlayerNet.LocalPortfolio.Cash);
            yield return Press(Key.Q);Assert.IsFalse(panel.IsOpen);
            player.GetComponent<LocalTradeController>().OpenPanel(awarded);yield return null;
            var trading=player.GetComponentInChildren<BuyBidBlow.UI.Market.TradePanelController>(true);
            var visible=player.GetComponentsInChildren<BuyBidBlow.UI.Market.TradingPanelView>(true).First(p=>p.gameObject.activeInHierarchy);
            var chance=visible.GetComponentsInChildren<UnityEngine.UI.Text>().Single(t=>t.name=="Rise Chance");
            Assert.AreEqual($"상승 확률 {MatchNet.Current.riseChances[(int)awarded]*100:0.#}%",chance.text);
            if(!Application.isBatchMode){yield return new WaitForSecondsRealtime(3.5f);yield return new WaitForEndOfFrame();Capture("Library/StockGachaTradePreview.png");}
            trading.Close();
        }
        [UnityTest] public IEnumerator AuctionChairEInteractionAndQStandUseServerState()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Project/Dev/Sandbox/StockGacha/StockGachaSandbox.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;room=Object.FindFirstObjectByType<GameNetworkManager>();
            room.LocalNickname="SeatTester";room.StartHost();
            yield return HostedMatch.Until(()=>NetworkClient.localPlayer,"seat player spawn");
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();player.SetReady(true);
            yield return HostedMatch.Until(()=>room.CanStartMatch,"seat ready");
            NetworkClient.Send(new StockGachaStart());
            yield return HostedMatch.Until(()=>MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing,"seat match");
            yield return HostedMatch.Until(()=>!BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput,"seat input release");
            previousInputSettings=InputSystem.settings;var inputSettings=Object.Instantiate(previousInputSettings);
            inputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=inputSettings;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            var chair=Object.FindObjectsByType<BuyBidBlow.Props.Seat>(FindObjectsSortMode.None).Single(s=>s.name=="A41_Front_Seat_2");
            var seats=player.GetComponent<SeatController>();var seating=player.GetComponent<PlayerSeating>();
            var detect=player.GetComponentInChildren<BuyBidBlow.UI.HUD.DetectObject>();
            detachedCamera=detect.transform;detachedCamera.SetParent(null,true);
            var anchor=chair.sittingPoints[0];var eye=anchor.position+anchor.right*1.3f+Vector3.up*.6f;
            detect.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(anchor.position-eye));
            yield return null;yield return null;
            Assert.IsTrue(detect.isDetected);Assert.AreEqual(chair,detect.DetectedCollider.GetComponentInParent<BuyBidBlow.Props.Seat>());
            player.GetComponent<PlayerInteraction>().Interact();
            yield return HostedMatch.Until(()=>seating.IsSeated&&player.IsSeated,"server-authorized chair seat");
            Assert.AreEqual(chair.Id,player.hallSeat);
            yield return Press(Key.Q);
            yield return HostedMatch.Until(()=>!seating.IsSeated&&!player.IsSeated,"Q frees local and server seat");
            Assert.IsTrue(chair.CanInteract);Assert.IsFalse(player.GetComponent<Rigidbody>().isKinematic);
            // An already-broken local-only seat must also be recoverable without a SyncVar change.
            Assert.IsTrue(chair.Interact(player.gameObject));Assert.IsFalse(player.IsSeated);Assert.IsTrue(seating.IsSeated);
            yield return Press(Key.Q);
            yield return HostedMatch.Until(()=>!seating.IsSeated,"recover legacy local-only seating");
            Assert.IsTrue(chair.CanInteract);
        }
        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        }
        static void Capture(string path)
        {
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes(path,image.EncodeToPNG());Object.Destroy(image);
        }
    }
}
