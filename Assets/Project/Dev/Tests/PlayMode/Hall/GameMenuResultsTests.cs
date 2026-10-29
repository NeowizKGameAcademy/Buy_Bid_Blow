using System.Collections;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.UI.Menu;
using BuyBidBlow.UI.Result;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public sealed class GameMenuResultsTests
    {
        readonly HostedMatch match=new HostedMatch();
        [UnityTearDown] public IEnumerator Cleanup(){var menu=Object.FindFirstObjectByType<GameMenuController>();if(menu)Object.Destroy(menu.gameObject);yield return match.Stop();}
        [UnityTest]public IEnumerator SettingsAndFinalResultsUseRealGameData()
        {
            yield return match.Start(1,null,27997);
            var menu=GameMenuController.Ensure();Assert.IsNotNull(menu);
            Capture("HudGear");
            menu.OpenMenu();yield return null;Assert.IsTrue(GameMenuController.BlocksInput);Assert.IsTrue(menu.launcher.gameObject.activeSelf);Capture("GameMenu");
            menu.controls.onClick.Invoke();Capture("Controls");menu.closeControls.onClick.Invoke();
            menu.RequestLeave();Capture("LeaveConfirmation");menu.cancelLeave.onClick.Invoke();
            int before=MatchNet.Current.round;menu.OpenSettings(false);yield return null;
            Assert.IsTrue(menu.settingsPanel.activeSelf);
            var saved=GameSettings.Current;menu.sliders[0].value=35;menu.sliders[1].value=25;menu.sliders[2].value=45;menu.apply.onClick.Invoke();
            Assert.AreEqual(.35f,AudioListener.volume,.001f);Assert.AreEqual(.25f,BuyBidBlow.Audio.GameAudio.EnsureHost().Engine.GetBusVolume("music"),.001f);
            Assert.AreEqual(.45f,BuyBidBlow.Audio.GameAudio.EnsureHost().Engine.GetBusVolume("sfx"),.001f);GameSettings.Load();Assert.AreEqual(35,GameSettings.Current.Master);GameSettings.Apply(saved);
            menu.OpenSettings(false);Capture("Settings");
            yield return new WaitForSeconds(1.2f);Assert.Greater(MatchNet.Current.round,before,"menu does not pause network match");
            menu.Close();Assert.IsFalse(GameMenuController.BlocksInput);
            Assert.IsNull(PlayerNet.LocalResults,"history and other portfolios stay private until finish");
            yield return HostedMatch.Until(()=>PlayerNet.LocalResults!=null,"final result data",25);
            var row=PlayerNet.LocalResults[0];Assert.AreEqual(14,row.AssetHistory.Length);Assert.AreEqual(row.Total,row.AssetHistory.Last());
            Assert.AreEqual(3,row.CompanyShares.Length);Assert.AreEqual(3,row.FinalPrices.Length);
            Assert.AreEqual(row.Cash+row.StockValue,row.Total);
            var details=Object.FindFirstObjectByType<DetailedResultsView>();Assert.IsNotNull(details);
            Assert.IsTrue(details.summaryPanel.activeSelf);Capture("Results");
            details.graphTab.onClick.Invoke();yield return null;Assert.IsTrue(details.graphPanel.activeSelf);Assert.AreEqual(14,details.graph.Entries[0].AssetHistory.Length);Capture("Graph");
            var examples=Enumerable.Range(0,6).Select(i=>new ResultEntry{Rank=i+1,Nickname="플레이어 "+(i+1),IsLocal=i==0,Cash=252-i*32,StockValue=150,TotalAssets=402-i*32,CompanyShares=new[]{2,1,1},FinalPrices=new[]{30,40,50},AssetHistory=Enumerable.Range(0,14).Select(r=>200+r*(15-i*3)+(r%3)*(i+1)*7).ToArray()}).ToArray();
            details.Present(details.GetComponentInParent<MatchResultView>(),examples,true,13);details.graphTab.onClick.Invoke();Capture("GraphSixPlayers");details.playerButtons[4].onClick.Invoke();Assert.AreEqual(4,details.graph.Selected);
            menu.RequestLeave();Assert.IsTrue(menu.leavePanel.activeSelf);menu.cancelLeave.onClick.Invoke();Assert.IsTrue(menu.menuPanel.activeSelf);menu.Close();
            details.returnButton.onClick.Invoke();yield return HostedMatch.Until(()=>!match.Room.ClientInMatch,"result returns to lobby");
            Assert.IsNull(PlayerNet.LocalResults);
            menu.RequestLeave();yield return menu.LeaveToMain();yield return null;
            Assert.IsFalse(NetworkClient.active);Assert.AreEqual("SC_MainMenu",UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            var mainMenu=GameMenuController.Ensure();mainMenu.OpenSettings();Assert.IsTrue(mainMenu.settingsPanel.activeSelf);Capture("MainSettings");mainMenu.Close();
        }
        [UnityTest]public IEnumerator HighestBidderCanRaiseAgainThroughTheServer()
        {
            yield return match.Start(0,s=>{s.RoundCount=5;s.RoundSeconds=2;s.AuctionRounds=new[]{2};s.AuctionRoundSeconds=16;s.AuctionDurationSeconds=7;s.AuctionSlots=new[]{new BuyBidBlow.Systems.Match.AuctionSlot{StartsAtSeconds=0,DurationSeconds=7},new BuyBidBlow.Systems.Match.AuctionSlot{StartsAtSeconds=7,DurationSeconds=7}};},27998);
            yield return HostedMatch.Until(()=>MatchNet.Current.auctionStage==MatchNet.AuctionStage.Open,"open auction",5);
            var player=NetworkClient.localPlayer.GetComponent<PlayerNet>();var seats=HostedMatch.Body.GetComponent<SeatController>();seats.RequestSit(Object.FindObjectsByType<BuyBidBlow.Props.Seat>(FindObjectsSortMode.None).First(s=>s.auction));
            yield return HostedMatch.Until(()=>seats.IsSeated,"auction chair");player.CmdBid(30,false);
            yield return HostedMatch.Until(()=>PlayerNet.LocalPortfolio.Reserved==30,"first reservation");player.CmdBid(200,false);
            yield return HostedMatch.Until(()=>MatchNet.Current.highestBid==200,"own increase to all cash");yield return HostedMatch.Until(()=>PlayerNet.LocalPortfolio.Reserved==200,"difference reserved");
            Assert.AreEqual(0,PlayerNet.LocalPortfolio.Available);
            player.CmdBid(201,false);yield return null;yield return null;Assert.AreEqual(200,MatchNet.Current.highestBid);Assert.AreEqual(200,PlayerNet.LocalPortfolio.Reserved);
        }
        static void Capture(string name)
        {
            Directory.CreateDirectory("Logs/MenuResults");
            var camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.isActiveAndEnabled);
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var previous=canvases.Select(c=>c.worldCamera).ToArray();var planes=canvases.Select(c=>c.planeDistance).ToArray();var old=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
            var rt=new RenderTexture(1920,1080,24);var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.aspect=16f/9;foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;}Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes("Logs/MenuResults/"+name+".png",texture.EncodeToPNG());}
            finally{camera.targetTexture=old;camera.aspect=aspect;RenderTexture.active=active;for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=previous[i];canvases[i].planeDistance=planes[i];}Object.Destroy(rt);Object.Destroy(texture);}        }
    }
}
