using System.Collections;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Menu;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class HowToPlayReviewTests
    {
        const string Root="Assets/Project/UI/Menu/HowToPlay";
        readonly HostedMatch match=new HostedMatch();
        Keyboard keyboard;InputSettings originalInput;
        [UnityTearDown]public IEnumerator Cleanup(){if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(originalInput)InputSystem.settings=originalInput;yield return match.Stop();}
        [UnityTest]public IEnumerator CaptureAuctionChairInteraction()
        {
            yield return match.Start(0,s=>s.RoundSeconds=60,27996);
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var detect=player.GetComponentInChildren<DetectObject>();
            match.Detach(detect.transform);
            var camera=detect.GetComponent<Camera>();
            if(!camera)camera=detect.GetComponentInChildren<Camera>();
            var seats=Object.FindObjectsByType<BuyBidBlow.Props.Seat>(FindObjectsSortMode.None).Where(s=>s.auction).ToArray();
            bool found=false;
            foreach(var seat in seats){
                var collider=seat.GetComponentsInChildren<Collider>().FirstOrDefault(c=>c.enabled&&!c.isTrigger);if(!collider)continue;
                var target=collider.bounds.center;
                foreach(var side in new[]{seat.transform.forward,-seat.transform.forward,seat.transform.right,-seat.transform.right}){
                    var origin=target+side*1.6f+Vector3.up*.65f;
                    detect.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(target-origin));
                    yield return null;yield return null;
                    if(detect.isDetected&&detect.DetectedCollider.GetComponentInParent<BuyBidBlow.Props.Seat>()==seat){found=true;break;}
                }
                if(found)break;
            }
            Assert.IsTrue(found,"camera aims at a real auction chair");
            player.GetComponentInChildren<NetworkPlayerHUD>(true).roundStart.Hide();
            yield return null;yield return null;
            Capture(camera,"AuctionChairInteraction",false);
        }
        [UnityTest]public IEnumerator CaptureCurrentGameExamples()
        {
            if(!File.Exists("Library/HowToPlayCapture.enabled"))Assert.Ignore("Opt-in capture generation");
            yield return match.Start(0,s=>s.RoundSeconds=4,27995);
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();
            var camera=player.GetComponentsInChildren<Camera>().First(c=>c.enabled);
            var hud=player.GetComponentInChildren<NetworkPlayerHUD>(true);hud.roundStart.Hide();
            Capture(camera,"CurrentExplore",false);
            yield return HostedMatch.Until(()=>MatchNet.Current.round==2,"auction round",10);
            var seats=player.GetComponent<SeatController>();
            seats.RequestSit(Object.FindObjectsByType<BuyBidBlow.Props.Seat>(FindObjectsSortMode.None).First(s=>s.auction));
            yield return HostedMatch.Until(()=>seats.IsSeated,"auction chair");yield return null;yield return null;
            hud.roundStart.Hide();
            var lot=Object.FindFirstObjectByType<BuyBidBlow.Props.AuctionLotDisplayClient>();
            if(lot && lot.display)camera.transform.LookAt(lot.display.transform.position+Vector3.up*.25f);
            Capture(camera,"CurrentAuction",false);
            // Explicitly authored demonstration information, delivered only to this preview player.
            player.TargetAwardInformation(new Info(2,Company.Straw,Direction.Rise,Company.Timber,Direction.Fall),new Info(5,Company.Timber,Direction.Rise,Company.Brick,Direction.Flat));
            yield return null;yield return null;
            player.GetComponentInChildren<BuyBidBlow.UI.Notebook.NetworkNotebookController>(true).Open();
            yield return null;yield return null;Capture(camera,"CurrentNotebook",true);
        }
        static void Capture(Camera camera,string name,bool notebook)
        {
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var previousCameras=canvases.Select(c=>c.worldCamera).ToArray();var planes=canvases.Select(c=>c.planeDistance).ToArray();
            var target=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(1920,1080,24);var texture=new Texture2D(notebook?1100:1920,notebook?1000:1080,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.aspect=16f/9;
                foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
                texture.ReadPixels(notebook?new Rect(520,40,1100,1000):new Rect(0,0,1920,1080),0,0);texture.Apply();
                Directory.CreateDirectory(Root+"/Examples");File.WriteAllBytes(Root+"/Examples/"+name+".png",texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=target;camera.aspect=aspect;RenderTexture.active=active;
                for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=previousCameras[i];canvases[i].planeDistance=planes[i];}
                Object.Destroy(rt);Object.Destroy(texture);
            }
        }
        [UnityTest]public IEnumerator MainMenuPagesMatchCurrentRules()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("SC_MainMenu");yield return null;yield return null;
            var menu=Object.FindFirstObjectByType<MainMenuActions>();menu.HowToPlay();yield return null;
            var panel=menu.howToPlayPanel.GetComponent<HowToPlayPanel>();Assert.AreEqual(7,panel.pages.Length);
            string TextAt(int page)=>string.Join("\n",panel.pages[page].GetComponentsInChildren<Text>(true).Select(t=>t.text));
            Assert.IsTrue(TextAt(0).Contains("120초"));Assert.IsTrue(TextAt(0).Contains("16분"));
            Assert.IsTrue(TextAt(1).Contains("W/S"));Assert.IsFalse(TextAt(4).Contains("I 열기"));
            Assert.IsTrue(TextAt(2).Contains("× 5 + 5"));Assert.IsFalse(TextAt(2).Contains("× 4주"));
            Assert.IsTrue(TextAt(3).Contains("비공개 30초"));Assert.IsTrue(TextAt(3).Contains("거래 시간 30초"));
            for(int page=0;page<7;page++){panel.Show(page);yield return null;Assert.AreEqual(1,panel.pages.Count(p=>p.activeSelf));Assert.AreEqual($"{page+1:00} / 07",panel.counter.text);}
            panel.Turn(1);Assert.AreEqual(6,panel.PageIndex);panel.Turn(-99);Assert.AreEqual(0,panel.PageIndex);
            originalInput=InputSystem.settings;var input=Object.Instantiate(originalInput);
            input.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=input;
            keyboard=InputSystem.AddDevice<Keyboard>();
            for(int page=0;page<7;page++){
                panel.Show(page);yield return null;Assert.IsFalse(panel.DetailsOpen);
                Assert.AreEqual(3,panel.pages[page].GetComponentsInChildren<Text>().Count(t=>t.name.StartsWith("Step Title ")));
                yield return Press(Key.E);Assert.IsTrue(panel.DetailsOpen);
                Assert.IsFalse(panel.pages[page].transform.Find("Steps").gameObject.activeSelf);
                yield return Press(Key.Escape);Assert.IsFalse(panel.DetailsOpen);Assert.IsTrue(menu.howToPlayPanel.activeSelf);
                Click(panel.pages[page].transform.Find("Details Toggle").GetComponent<Button>());Assert.IsTrue(panel.DetailsOpen);
                panel.Show(page);Assert.IsFalse(panel.DetailsOpen);
            }
            panel.Show(0);
            yield return Press(Key.D);Assert.AreEqual(1,panel.PageIndex);
            yield return Press(Key.S);Assert.AreEqual(2,panel.PageIndex);
            yield return Press(Key.A);Assert.AreEqual(1,panel.PageIndex);
            yield return Press(Key.W);Assert.AreEqual(0,panel.PageIndex);
            yield return Press(Key.RightArrow);Assert.AreEqual(0,panel.PageIndex,"old arrow input removed");
            Click(panel.nextHint.GetComponent<Button>());Assert.AreEqual(1,panel.PageIndex);
            Click(panel.previousHint.GetComponent<Button>());Assert.AreEqual(0,panel.PageIndex);
            Assert.IsFalse(panel.previousHint.GetComponent<Button>().interactable);
            Click(panel.tabs[6].GetComponent<Button>());Assert.AreEqual(6,panel.PageIndex);
            Assert.IsFalse(panel.nextHint.GetComponent<Button>().interactable);
            Click(menu.howClose);Assert.IsFalse(menu.howToPlayPanel.activeSelf);
            menu.HowToPlay();yield return null;Click(panel.nextHint.GetComponent<Button>());Assert.AreEqual(1,panel.PageIndex,"no duplicate handlers after reopening");
            yield return Press(Key.Escape);Assert.IsFalse(menu.howToPlayPanel.activeSelf);
        }
        IEnumerator Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
        static void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();var events=EventSystem.current;Assert.IsNotNull(events);
            var rect=button.GetComponent<RectTransform>();var canvas=button.GetComponentInParent<Canvas>();
            var position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            var pointer=new PointerEventData(events){position=position,button=PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<RaycastResult>();events.RaycastAll(pointer,hits);
            Assert.IsTrue(hits.Count>0);Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<Button>(),"mouse target reachable");
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
    }
}
