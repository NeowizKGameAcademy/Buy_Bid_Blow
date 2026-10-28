using System;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using BuyBidBlow.UI.Lobby;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace BuyBidBlow.Dev
{
    // Test player for one-person playtests. Started only by the -bot argument, usually headless:
    //   BuyBidBlow.exe -batchmode -nographics -bot -name Bot1 -address 127.0.0.1 -port 7777
    // Joins the lobby, readies up, starts the match when it is the room host and everyone is ready,
    // then wanders around the level.
    public sealed class TestBot : MonoBehaviour
    {
        string nickname, address, port;
        float nextJoin, nextTurn;
        PlayerMove body;
        Vector3 lastTurnPosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-bot")) return;
            string Arg(string name, string fallback) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
            var bot = new GameObject("Test Bot").AddComponent<TestBot>();
            bot.nickname = Arg("-name", "Bot" + Random.Range(1, 100));
            bot.address = Arg("-address", "127.0.0.1");
            bot.port = Arg("-port", "7777");
            DontDestroyOnLoad(bot.gameObject);
        }

        void Update()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene == "SC_MainMenu") { SceneManager.LoadScene(GameNetworkManager.PlayScene); return; }
            if (!(NetworkManager.singleton is GameNetworkManager room)) return;

            var local = NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            if (!local)
            {
                // Retry until the server is up; the controller times out failed attempts on its own.
                var session = room.GetComponent<ConnectionController>();
                if (session && session.CanConnect && Time.time >= nextJoin) { session.Join(nickname, address, port); nextJoin = Time.time + 3; }
                return;
            }
            if (!local.inMatch) { Lobby(local, room); return; }
            Wander();
        }

        static void Lobby(PlayerNet local, GameNetworkManager room)
        {
            if (!local.ready) { local.SetReady(true); return; }
            var players = FindObjectsByType<PlayerNet>(FindObjectsSortMode.None);
            bool roomHost = players.All(p => p.seat >= local.seat);
            if (roomHost && players.Length >= room.MinPlayers && players.All(p => p.ready) && Time.frameCount % 60 == 0) local.CmdStartMatch();
        }

        void Wander()
        {
            if (!body) { body = FindFirstObjectByType<PlayerMove>(); if (!body) return; }
            if (Time.time < nextTurn) return;
            nextTurn = Time.time + Random.Range(1f, 3f);
            // Blocked by a wall or furniture since the last turn: turn around instead of a random turn.
            bool stuck = Vector3.Distance(body.transform.position, lastTurnPosition) < .3f;
            lastTurnPosition = body.transform.position;
            var stop = !stuck && Random.value < .1f;
            body.SetMoveInput(stop ? Vector2.zero : new Vector2(Random.Range(-.3f, .3f), 1));
            body.Rotate(new Vector2(stuck ? 3600f : Random.Range(-1500f, 1500f), 0));
        }
    }
}
