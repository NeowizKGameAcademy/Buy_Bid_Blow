using System.Collections;
using System.Linq;
using System.Reflection;
using BuyBidBlow.UI.Ticker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BuyBidBlow.Dev.Tests.PlayMode.Ticker
{
    public sealed class WelcomeVideoPlayTests
    {
        TickerDirector director;
        GameObject[] boards;

        [UnityTest]
        public IEnumerator SharedVideoDecodesAcrossThreeBoardsAndCleansUpOnStopAndDisable()
        {
            const string root="Assets/Project/Content/Props/TickerBoard/";
            director=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(root+"TickerDirector.prefab")).GetComponent<TickerDirector>();
            Assert.Greater(director.WelcomeDuration,0,"direct clip reference survives moving out of Resources");
            boards=new[]{"Right","Center","Left"}.Select(side=>{
                var board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(root+"TickerBoard.prefab"));
                board.name="TickerBoard_"+side;return board;
            }).ToArray();
            director.BeginIntro();
            yield return null;
            Assert.IsTrue(director.IntroBlank);
            director.StartCoroutine(director.PlayWelcomeVideo(1));
            yield return HostedMatch.Until(()=>director.GetComponentInChildren<VideoPlayer>() is VideoPlayer player && player.frame>0,"decoded welcome frame",10);
            yield return null;
            Assert.IsTrue(director.TryGetIntroTexture(out var texture));
            Assert.AreEqual(1,director.GetComponentsInChildren<VideoPlayer>().Length);
            for(int i=0;i<boards.Length;i++)
            {
                var layer=(RawImage)typeof(TickerBoardDisplay).GetField("videoLayer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(boards[i].GetComponent<TickerBoardDisplay>());
                Assert.IsTrue(layer.enabled);Assert.AreSame(texture,layer.texture);
                Assert.AreEqual(new Rect(i/3f,0,1f/3f,1),layer.uvRect);
            }
            yield return HostedMatch.Until(()=>!director.IsIntroVideoPlaying,"welcome completion",8);
            yield return null;
            Assert.IsFalse(director.TryGetIntroTexture(out _));
            Assert.IsEmpty(director.GetComponentsInChildren<VideoPlayer>());
            Assert.IsTrue(director.IntroBlank,"boards stay blank until gameplay begins");
            director.EndIntro();
            Assert.IsFalse(director.IntroBlank);
            director.BeginIntro();director.StartCoroutine(director.PlayWelcomeVideo());
            yield return null;
            director.enabled=false;
            yield return null;
            Assert.IsFalse(director.IsIntroVideoPlaying);Assert.IsFalse(director.IntroBlank);
            Assert.IsFalse(director.TryGetIntroTexture(out _));
            Assert.IsEmpty(director.GetComponentsInChildren<VideoPlayer>());
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(director)Object.Destroy(director.gameObject);
            if(boards!=null)foreach(var board in boards)if(board)Object.Destroy(board);
            yield return null;
        }
    }
}
