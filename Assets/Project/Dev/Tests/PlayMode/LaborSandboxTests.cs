using System.Collections;
using System.Reflection;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Sandbox.Labor;
using BuyBidBlow.Sandbox.StockGacha;
using BuyBidBlow.Systems.Types;
using Mirror;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class LaborSandboxTests
    {
        GameNetworkManager manager;PlayerNet player;LaborRoom room;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(manager&&(NetworkServer.active||NetworkClient.active))manager.StopHost();yield return null;
            if(manager)Object.Destroy(manager.gameObject);yield return null;
        }
        void Move(Vector3 pos){player.transform.position=pos;var body=player.GetComponent<Rigidbody>();body.position=pos;body.linearVelocity=Vector3.zero;Physics.SyncTransforms();}
        IEnumerator Send(int target){NetworkClient.Send(new LaborRequest{Session=LaborRoom.State.Session,Target=target});yield return new WaitForSecondsRealtime(.3f);}
        IEnumerator InteractAt(Vector3 target,int expected)
        {
            var detector=player.GetComponentInChildren<BuyBidBlow.UI.HUD.DetectObject>();
            detector.transform.rotation=Quaternion.LookRotation(target-detector.transform.position);Physics.SyncTransforms();
            typeof(BuyBidBlow.UI.HUD.DetectObject).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(detector,null);
            Physics.Raycast(detector.transform.position,detector.transform.forward,out var hit,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            Assert.IsTrue(detector.isDetected,$"real interaction raycast: origin={detector.transform.position}, target={target}, hit={(hit.collider?hit.collider.name:"none")}, distance={hit.distance}");
            Assert.AreEqual(expected,detector.DetectedCollider.GetComponentInParent<LaborTarget>().id);
            player.GetComponent<PlayerInteraction>().Interact();yield return new WaitForSecondsRealtime(.3f);
        }
        [UnityTest] public IEnumerator DoorCarryWrongBinWageBombAndReturnAreServerAuthoritative()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Project/Dev/Sandbox/Labor/LaborSandbox.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            manager=Object.FindFirstObjectByType<GameNetworkManager>();room=LaborRoom.Current;
            Assert.IsNotNull(room);manager.LocalNickname="Worker";manager.StartHost();
            yield return HostedMatch.Until(()=>NetworkClient.localPlayer,"labor player spawn");
            player=NetworkClient.localPlayer.GetComponent<PlayerNet>();player.SetReady(true);
            yield return HostedMatch.Until(()=>manager.CanStartMatch,"labor ready");NetworkClient.Send(new StockGachaStart());
            yield return HostedMatch.Until(()=>MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing&&!string.IsNullOrEmpty(LaborRoom.State.Session),"labor round");
            yield return HostedMatch.Until(()=>!BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput,"labor input release");
            Move(new Vector3(room.doorPosition.x-2,room.hallExit.y,room.doorPosition.z));
            var detector=player.GetComponentInChildren<BuyBidBlow.UI.HUD.DetectObject>();
            detector.transform.rotation=Quaternion.LookRotation(room.doorPosition-detector.transform.position);
            Physics.SyncTransforms();
            typeof(BuyBidBlow.UI.HUD.DetectObject).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(detector,null);
            Assert.IsTrue(detector.isDetected,"real doorway raycast");
            Assert.AreEqual(100,detector.DetectedCollider.GetComponentInParent<LaborTarget>().id);
            yield return null;
            var hud=player.GetComponentInChildren<BuyBidBlow.UI.HUD.HUDController>();
            var prompt=(TMPro.TextMeshProUGUI)typeof(BuyBidBlow.UI.HUD.HUDController).GetField("InteractionText",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
            Assert.AreEqual("작업장으로 이동",prompt.text);
            Assert.AreEqual("Pretendard 400 SDF",prompt.font.name);
            Assert.IsTrue(prompt.font.HasCharacters("작업장으로 이동 거래소로 돌아가기 짚 목재 벽돌 투입 블록 들기 폭탄 무급",out uint[] missing,true,true),"Korean prompt glyphs must exist");
            prompt.ForceMeshUpdate();
            foreach(var character in prompt.textInfo.characterInfo)
                if(character.isVisible)Assert.AreEqual((uint)character.character,character.textElement.unicode,"no replacement squares");
            if(!Application.isBatchMode){yield return new WaitForEndOfFrame();var preview=ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes("Library/LaborKoreanPromptPreview.png",preview.EncodeToPNG());Object.Destroy(preview);}
            player.GetComponent<PlayerInteraction>().Interact();yield return new WaitForSecondsRealtime(.3f);
            Assert.Less(Vector3.Distance(player.transform.position,room.Entry),2);
            int cash=PlayerNet.LocalPortfolio.Cash,wage=manager.StockGachaSandboxEngine.Price(Company.Straw)/5;
            for(int c=0;c<3;c++)
            {
                int count=0;for(int i=0;i<LaborRoom.BlocksPerCompany;i++)if(LaborRoom.State.Available[c*LaborRoom.BlocksPerCompany+i])count++;
                Assert.That(count,Is.InRange(1,5));
            }
            int bombs=0;for(int i=LaborRoom.BombStart;i<LaborRoom.CargoCount;i++)if(LaborRoom.State.Available[i])bombs++;
            Assert.That(bombs,Is.InRange(1,2));
            // Remote pickup must fail.
            Move(room.Entry);yield return Send(8);Assert.AreEqual(0,LaborRoom.State.Owners[8]);
            Move(new Vector3(room.spawns[0].x,.2f,room.spawns[0].z-1.5f));yield return InteractAt(room.spawns[0],0);Assert.AreEqual(player.netId,LaborRoom.State.Owners[0]);
            yield return Send(1);Assert.AreEqual(0,LaborRoom.State.Owners[1]);
            Move(room.Bin(1)-Vector3.forward*2);yield return Send(201);Assert.AreEqual(cash,PlayerNet.LocalPortfolio.Cash);Assert.AreEqual(player.netId,LaborRoom.State.Owners[0]);
            Move(room.Bin(0)-Vector3.forward*2);yield return InteractAt(room.Bin(0),200);
            Assert.AreEqual(cash+wage,PlayerNet.LocalPortfolio.Cash);Assert.AreEqual(1,LaborRoom.State.Paid);Assert.AreEqual(1,LaborRoom.State.Adjustments[0]);
            yield return Send(200);Assert.AreEqual(cash+wage,PlayerNet.LocalPortfolio.Cash,"duplicate delivery gives no salary");
            Move(room.spawns[LaborRoom.BombStart]);yield return Send(LaborRoom.BombStart);Move(room.Bin(0)-Vector3.forward*2);yield return InteractAt(room.Bin(0),200);
            Assert.AreEqual(cash+wage,PlayerNet.LocalPortfolio.Cash);Assert.AreEqual(-2,LaborRoom.State.Adjustments[0]);Assert.AreEqual(1,LaborRoom.State.Bombs);
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.IsFalse(LaborRoom.State.Available[0],"delivered block stays consumed");
            Assert.IsFalse(LaborRoom.State.Available[LaborRoom.BombStart],"delivered bomb stays consumed");
            Move(room.spawns[0]);yield return Send(0);Assert.AreEqual(0,LaborRoom.State.Owners[0]);
            Move(room.origin+new Vector3(0,.3f,0));player.transform.rotation=Quaternion.identity;
            if(!Application.isBatchMode){yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes("Library/LaborWorkshopPreview.png",shot.EncodeToPNG());Object.Destroy(shot);}
            Move(room.origin+new Vector3(0,.2f,-7));yield return InteractAt(room.origin+new Vector3(0,1.5f,-9),101);Assert.Less(Vector3.Distance(player.transform.position,room.hallExit),2);
            string oldSession=LaborRoom.State.Session;
            manager.StockGachaSandboxEngine.Tick(manager.StockGachaSandboxEngine.Deadline);
            yield return HostedMatch.Until(()=>LaborRoom.State.Session!=oldSession,"new round replenishes cargo");
            Assert.IsTrue(LaborRoom.State.Available[0]);Assert.IsTrue(LaborRoom.State.Available[LaborRoom.BombStart]);
            Assert.AreEqual(0,LaborRoom.State.Paid);Assert.AreEqual(0,LaborRoom.State.Bombs);
        }
    }
}
