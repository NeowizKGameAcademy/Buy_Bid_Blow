using System.Collections;
using System.IO;
using System.Linq;
using BuyBidBlow.Common;
using BuyBidBlow.UI.Ticker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BuyBidBlow.Dev.Tests.PlayMode.Ticker
{
    // Not a check: records the hall boards through a round change for review. Run it by name; it is Explicit.
    // Frames go to Logs/TickerCapture (turn them into a clip with ffmpeg), key stills to Docs/Arts/Props/TickerBoard/Schedule.
    public sealed class TickerScheduleCapture
    {
        const string Frames="Logs/TickerCapture", Stills="Docs/Arts/Props/TickerBoard/Schedule";
        const float Fps=15;
        readonly HostedMatch match=new HostedMatch();
        [UnityTearDown]public IEnumerator Cleanup()
        {
            yield return match.Stop();
            if(CueAudio.CueAudioHost.Instance)Object.Destroy(CueAudio.CueAudioHost.Instance.gameObject);
            yield return null;
        }

        [UnityTest,Explicit("Writes review captures; run on demand.")]
        public IEnumerator RecordRoundChange()
        {
            if(Directory.Exists(Frames))Directory.Delete(Frames,true);
            Directory.CreateDirectory(Frames);Directory.CreateDirectory(Stills);
            yield return match.Start(30,settings=>{settings.RoundCount=3;settings.AuctionRounds=new int[0];});
            var center=Object.FindObjectsByType<TickerBoardDisplay>(FindObjectsSortMode.None).Single(b=>b.name=="TickerBoard_Center");
            var camera=new GameObject("Ticker capture camera").AddComponent<Camera>();
            camera.transform.position=center.transform.TransformPoint(new Vector3(0,.4f,23));
            camera.transform.rotation=center.transform.rotation*Quaternion.Euler(0,180,0);
            camera.fieldOfView=62;camera.enabled=false;
            var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            camera.targetTexture=rt;camera.aspect=16f/9;

            yield return HostedMatch.Until(()=>MatchNet.Current.SecondsLeft<13,"near the end of round 1",30);
            int frame=0;float next=0;
            var stills=new[]{"overview","timer","roundOver","pig","company:1"};
            double until=double.MaxValue;
            while(MatchNet.Current&&(MatchNet.Current.round<2||Mirror.NetworkTime.time<until))
            {
                if(MatchNet.Current.round==2&&until==double.MaxValue)until=Mirror.NetworkTime.time+18;
                yield return null;
                if(Time.realtimeSinceStartup<next)continue;
                next=Time.realtimeSinceStartup+1/Fps;
                camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();RenderTexture.active=null;
                File.WriteAllBytes($"{Frames}/frame_{frame++:0000}.jpg",image.EncodeToJPG(90));
                // One still per channel, a beat after it appears so videos have frames.
                string channel=center.CurrentChannel;
                int index=System.Array.IndexOf(stills,channel);
                if(index>=0&&!File.Exists($"{Stills}/{index+1:00}_{channel.Replace(':','_')}.png")&&HeldFor(center,1.2f))
                    File.WriteAllBytes($"{Stills}/{index+1:00}_{channel.Replace(':','_')}.png",image.EncodeToPNG());
            }
            Object.Destroy(camera.gameObject);Object.Destroy(rt);Object.Destroy(image);
            Debug.Log($"[Ticker] Captured {frame} frames");
            Assert.Greater(frame,0);
        }

        string heldChannel;float heldSince;
        bool HeldFor(TickerBoardDisplay board,float seconds)
        {
            if(board.CurrentChannel!=heldChannel){heldChannel=board.CurrentChannel;heldSince=Time.realtimeSinceStartup;}
            return Time.realtimeSinceStartup-heldSince>=seconds;
        }
    }
}
