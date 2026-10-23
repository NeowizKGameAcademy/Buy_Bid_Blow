using System;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Common;
using Mirror;
using UnityEngine;
using UnityEngine.Video;

namespace BuyBidBlow.UI.Ticker
{
    // Programs the hall's ticker boards from TickerSchedule.json and the match timeline. Boards ask for their shot
    // by name; videos play once here and every board showing one samples the same texture, so slices stay in sync.
    [DisallowMultipleComponent]
    public sealed class TickerDirector : MonoBehaviour
    {
        [Serializable] public struct VideoEntry { public string id; public VideoClip clip; }
        public TextAsset schedule;
        public VideoEntry[] videos;
        public static TickerDirector Active { get; private set; }
        public TickerSchedule Schedule { get; private set; }

        sealed class Feed { public VideoPlayer Player; public RenderTexture Texture; public double Since = double.NaN; public float SeekedAt; public bool Used; }
        readonly Dictionary<string, Feed> feeds = new Dictionary<string, Feed>(StringComparer.Ordinal);
        TickerShot[] shots = Array.Empty<TickerShot>();
        bool resolved;
        int resolvedFrame = -1;
        VideoPlayer introPlayer;
        RenderTexture introTexture;
        bool introPlaying;

        public bool IsIntroVideoPlaying => introPlaying;

        void Awake()
        {
            if (!schedule) { Debug.LogError("TickerDirector needs its schedule JSON.", this); enabled = false; return; }
            Schedule = TickerSchedule.FromJson(schedule.text);
            videos ??= Array.Empty<VideoEntry>();
            var errors = Schedule.Validate(videos.Where(v => v.clip).Select(v => v.id).ToList());
            if (errors.Count > 0) { Debug.LogError("Ticker schedule:\n" + string.Join("\n", errors), this); enabled = false; return; }
            shots = new TickerShot[Schedule.BoardCount];
        }
        void OnEnable() { if (Schedule != null) Active = this; }
        void OnDisable() { if (Active == this) Active = null; foreach (var feed in feeds.Values) feed.Player.Stop(); if (introPlayer) introPlayer.Stop(); }
        void OnDestroy()
        {
            foreach (var feed in feeds.Values) if (feed.Texture) { feed.Texture.Release(); Destroy(feed.Texture); }
            if (introTexture) { introTexture.Release(); Destroy(introTexture); }
        }

        // Local-only welcome video used by the map introduction.  The board UI asks
        // TryGetIntroTexture each frame, so it replaces the normal ticker briefly.
        public void PlayWelcomeVideo()
        {
#if UNITY_SERVER
            return;
#else
            var clip = Resources.Load<VideoClip>("Video/Welcome");
            if (!clip) { Debug.LogError("Missing Resources/Video/Welcome.mp4 for the map intro.", this); return; }
            if (!introPlayer)
            {
                var go = new GameObject("Map intro welcome video");
                go.transform.SetParent(transform, false);
                introPlayer = go.AddComponent<VideoPlayer>();
                introPlayer.playOnAwake = false; introPlayer.isLooping = false; introPlayer.skipOnDrop = true;
                introPlayer.renderMode = VideoRenderMode.RenderTexture;
                introPlayer.audioOutputMode = VideoAudioOutputMode.None;
                introPlayer.loopPointReached += _ => introPlaying = false;
            }
            if (!introTexture || introTexture.width != (int)clip.width || introTexture.height != (int)clip.height)
            {
                if (introTexture) { introTexture.Release(); Destroy(introTexture); }
                introTexture = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "Map intro welcome video" };
                Clear(introTexture);
                introPlayer.targetTexture = introTexture;
            }
            introPlayer.Stop(); introPlayer.clip = clip; introPlaying = true; introPlayer.Prepare();
            Debug.Log("[Ticker] Playing Welcome.mp4 for the map introduction.", this);
#endif
        }

        public bool TryGetIntroTexture(out Texture texture)
        {
            texture = null;
#if !UNITY_SERVER
            if (!introPlaying || !introPlayer) return false;
            if (introPlayer.isPrepared && !introPlayer.isPlaying) introPlayer.Play();
            texture = introTexture;
            return texture;
#else
            return false;
#endif
        }

        // False while no match timeline exists; the board then keeps its own fixed screen.
        public bool TryGetShot(string board, out TickerShot shot)
        {
            Resolve();
            int index = Schedule.IndexOf(board);
            shot = resolved && index >= 0 ? shots[index] : default;
            return shot.IsSet;
        }

        // Created on first request, so a board switching to video shows black rather than its old screen.
        public Texture VideoTexture(string id)
        {
#if UNITY_SERVER
            return null;
#else
            return FeedFor(id).Texture;
#endif
        }

        void Resolve()
        {
            if (resolvedFrame == Time.frameCount) return;
            resolvedFrame = Time.frameCount;
            var timeline = MatchTimeline.Current;
            resolved = timeline && Schedule.Resolve(timeline.spans, NetworkTime.time, shots);
        }

        void LateUpdate()
        {
            Resolve();
#if !UNITY_SERVER
            if (introPlaying && introPlayer && introPlayer.isPrepared && !introPlayer.isPlaying) introPlayer.Play();
            foreach (var feed in feeds.Values) feed.Used = false;
            if (resolved)
                foreach (var shot in shots)
                    if (shot.IsSet && !string.IsNullOrEmpty(shot.Channel.video)) Drive(shot.Channel.video, shot.Since);
            foreach (var feed in feeds.Values)
                if (!feed.Used && !double.IsNaN(feed.Since)) { feed.Player.Stop(); feed.Since = double.NaN; }
#endif
        }

        // Keeps the clip at NetworkTime - since, so late joiners and every board see the same frame.
        void Drive(string id, double since)
        {
            var feed = FeedFor(id);
            feed.Used = true;
            var video = feed.Player;
            if (feed.Since != since)
            {
                feed.Since = since;
                Clear(feed.Texture);
                video.Stop(); video.Prepare();
            }
            if (!video.isPrepared) return;
            double target = NetworkTime.time - since;
            if (target >= video.length) { if (video.isPlaying) video.Pause(); return; }
            if (!video.isPlaying) { video.time = Math.Max(0, target); video.Play(); feed.SeekedAt = Time.unscaledTime; }
            // Correct drift only while frames flow and not right after a seek: seeking every frame stalls the decoder.
            else if (video.frame >= 0 && Time.unscaledTime - feed.SeekedAt > 1 && Math.Abs(video.time - target) > .2)
            { video.time = target; feed.SeekedAt = Time.unscaledTime; }
        }

        Feed FeedFor(string id)
        {
            if (feeds.TryGetValue(id, out var feed)) return feed;
            var clip = videos.First(v => v.id == id).clip;
            var texture = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "Ticker video " + id };
            Clear(texture);
            var go = new GameObject("Ticker video " + id);
            go.transform.SetParent(transform, false);
            var player = go.AddComponent<VideoPlayer>();
            player.playOnAwake = false; player.isLooping = false; player.skipOnDrop = true;
            player.renderMode = VideoRenderMode.RenderTexture; player.targetTexture = texture;
            player.audioOutputMode = VideoAudioOutputMode.None; player.clip = clip;
            return feeds[id] = new Feed { Player = player, Texture = texture };
        }

        static void Clear(RenderTexture texture)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = texture; GL.Clear(false, true, Color.black);
            RenderTexture.active = previous;
        }
    }
}
