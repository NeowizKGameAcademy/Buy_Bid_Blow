using System;
using System.Collections;
using System.Reflection;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Match;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    // A one-player host match in the real level, started the way the room host starts one.
    public sealed class HostedMatch
    {
        public GameNetworkManager Room { get; private set; }
        // The local body (scene Player): motor, seats, trading and the match HUD. PlayerNet is only its avatar.
        public static GameObject Body => UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>(FindObjectsInactive.Include).gameObject;
        readonly System.Collections.Generic.List<GameObject> detached = new System.Collections.Generic.List<GameObject>();

        // Takes a rig out of the player so the test can aim it still. Stop destroys it, or it would outlive the player.
        public void Detach(Transform rig)
        {
            rig.SetParent(null, true);
            detached.Add(rig.gameObject);
        }

        // roundSeconds 0 keeps the rules' round lengths; tweak edits a copy of the match rules first.
        public IEnumerator Start(int roundSeconds = 0, Action<MatchSettings> tweak = null, ushort port = 0)
        {
            SceneManager.LoadScene(GameNetworkManager.PlayScene);
            yield return null;
            Room = UnityEngine.Object.FindFirstObjectByType<GameNetworkManager>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(GameNetworkManager).GetField("minPlayers", flags).SetValue(Room, 1);
            typeof(GameNetworkManager).GetField("prepareSeconds", flags).SetValue(Room, .5f);
            typeof(GameNetworkManager).GetField("roundSecondsOverride", flags).SetValue(Room, roundSeconds);
            if (tweak != null)
            {
                var file = typeof(GameNetworkManager).GetField("matchSettingsFile", flags);
                var settings = JsonUtility.FromJson<MatchSettings>(((TextAsset)file.GetValue(Room)).text);
                tweak(settings);
                file.SetValue(Room, new TextAsset(JsonUtility.ToJson(settings)));
            }
            Room.LocalNickname = "Tester";
            if(port!=0)Room.transport.GetType().GetProperty("Port").SetValue(Room.transport,port);
            Room.StartHost();
            yield return Until(() => NetworkClient.localPlayer, "local player spawned");
            var net = NetworkClient.localPlayer.GetComponent<PlayerNet>();
            net.SetReady(true);
            yield return Until(() => net.ready, "ready");
            net.CmdStartMatch();
            yield return Until(() => MatchNet.Current && MatchNet.Current.phase == MatchNet.Phase.Playing, "round 1 open");
        }

        public IEnumerator Stop()
        {
            if (NetworkServer.active || NetworkClient.active) Room.StopHost();
            foreach (var rig in detached) if (rig) UnityEngine.Object.Destroy(rig);
            detached.Clear();
            yield return null;
            if (Room) UnityEngine.Object.Destroy(Room.gameObject);
            yield return null;
        }

        public static IEnumerator Until(Func<bool> condition, string what, float seconds = 15)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }
    }
}
