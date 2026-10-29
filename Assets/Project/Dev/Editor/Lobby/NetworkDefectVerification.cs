using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // Dedicated-server scene + independent UDP peers. Does not claim to test remote Unity rendering/hooks.
    [InitializeOnLoad]
    public static class NetworkDefectVerification
    {
        private const string Key = "BBB.NetworkDefectVerification";
        private const string ReportPath = ".utmp/defect-fix/network.json";
        [Serializable] private sealed class Report { public string Status; public List<string> Checks = new List<string>(); }
        private static readonly Report report = new Report();
        private static readonly List<LoopbackLobbyPeer> peers = new List<LoopbackLobbyPeer>();
        private static GameNetworkManager room;
        private static LoopbackLobbyPeer a, b, intruder;
        private static PlayerNet pa, pb;
        private static MatchEngine game;
        private static MatchNet state;
        private static string stableB, tokenB;
        private static int stage, publicUpdates, otherUpdates, stockBefore;
        private static uint departedNetId;
        private static double deadline, waitUntil, stageStarted;

        static NetworkDefectVerification()
        {
            if (!SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Tick;
        }

        public static void RunBatch()
        {
            EditorSceneManager.OpenScene("Assets/Project/Scenes/SC_Server.unity");
            Object.FindFirstObjectByType<GameNetworkManager>().headlessStartMode = HeadlessStartOptions.DoNothing;
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            report.Checks.Add(message);
        }
        private static void Set(string field, object value) => typeof(GameNetworkManager).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(room, value);
        private static PlayerNet Player(LoopbackLobbyPeer peer) => NetworkServer.spawned.TryGetValue(peer.OwnNetId, out var identity) ? identity.GetComponent<PlayerNet>() : null;
        private static void Go(int next, double delay = .15)
        {
            Debug.Log($"NETWORK_QA stage={next} time={NetworkTime.time:F2} round={state?.round} auction={state?.auctionStage} replyA={a?.LastReply?.Message}");
            stage = next; stageStarted = EditorApplication.timeSinceStartup; waitUntil = stageStarted + delay;
        }
        private static void Seat(LoopbackLobbyPeer peer, PlayerNet player, string id, int slot) => peer.Command(player, nameof(PlayerNet.CmdSit), writer => { writer.Write(id); writer.Write(slot); });
        private static void Bid(LoopbackLobbyPeer peer, PlayerNet player, int amount) => peer.Command(player, nameof(PlayerNet.CmdBid), writer => { writer.Write(amount); writer.Write(false); });

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Network verification timed out at stage " + stage);
                foreach (var peer in peers) peer.Tick();
                if (EditorApplication.timeSinceStartup < waitUntil) return;
                if (stage == 0)
                {
                    room = Object.FindFirstObjectByType<GameNetworkManager>();
                    ((kcp2k.KcpTransport)room.transport).Port = 17777;
                    var rules = JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));
                    rules.RoundCount = 4; rules.RoundSeconds = 2; rules.AuctionRounds = new[] { 1 };
                    rules.AuctionRoundSeconds = 20; rules.AuctionDurationSeconds = 8;
                    rules.AuctionSlots = new[] { new AuctionSlot { StartsAtSeconds = 0, DurationSeconds = 8 }, new AuctionSlot { StartsAtSeconds = 8, DurationSeconds = 8 } };
                    Set("minPlayers", 2); Set("prepareSeconds", .25f); Set("matchSettingsFile", new TextAsset(JsonUtility.ToJson(rules)));
                    room.StartServer();
                    Check(NetworkServer.active && !NetworkClient.active, "Dedicated server runs without a local client or hall");
                    a = new LoopbackLobbyPeer("같은이름"); b = new LoopbackLobbyPeer("같은이름"); intruder = new LoopbackLobbyPeer("미승인", skipApproval: true);
                    peers.Add(a); peers.Add(b); peers.Add(intruder); Go(1); return;
                }
                if (stage == 1)
                {
                    pa = Player(a); pb = Player(b);
                    if (!pa || !pb || string.IsNullOrEmpty(intruder.Rejection)) return;
                    if(pa.seat>pb.seat){var swap=a;a=b;b=swap;pa=Player(a);pb=Player(b);}
                    Check(intruder.SpawnMasks.Count == 0, "Unapproved Ready receives no spawned objects");
                    Check(pa.nickname == pb.nickname && pa.playerId != pb.playerId, "Duplicate nicknames have distinct stable IDs");
                    var privateIndex = pa.GetComponent<PlayerAssetsNet>().ComponentIndex;
                    if (!a.SpawnMasks.ContainsKey(pb.netId) || !b.SpawnMasks.ContainsKey(pa.netId)) return;
                    Check((a.SpawnMasks[pa.netId] & (1UL << privateIndex)) != 0, "Owner spawn contains its private state");
                    Check((a.SpawnMasks[pb.netId] & (1UL << privateIndex)) == 0 && (b.SpawnMasks[pa.netId] & (1UL << privateIndex)) == 0,
                        "Observer spawn omits the other player's cash, shares and information behaviour");
                    a.Command(pa, nameof(PlayerNet.SetReady), w => w.WriteBool(true));
                    b.Command(pb, nameof(PlayerNet.SetReady), w => w.WriteBool(true)); Go(2); return;
                }
                if (stage == 2)
                {
                    if (!room.CanStartMatch) return;
                    a.Command(pa, nameof(PlayerNet.CmdStartMatch)); Go(3); return;
                }
                if (stage == 3)
                {
                    state = Object.FindFirstObjectByType<MatchNet>();
                    if (!state || state.phase != MatchNet.Phase.Playing) return;
                    game = (MatchEngine)typeof(GameNetworkManager).GetField("engine", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(room);
                    Seat(a, pa, "nonexistent-seat", 0); Go(4); return;
                }
                var catalog = (GameNetworkManager.HallSeat[])typeof(GameNetworkManager).GetField("hallSeats", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(room);
                var chairs = catalog.Where(s => s.Auction).ToArray();
                if (stage == 4)
                {
                    Check(!pa.IsSeated, "Unknown seat is rejected over a real Command");
                    Seat(a, pa, chairs[0].Id, chairs[0].Slots); Go(5); return;
                }
                if (stage == 5)
                {
                    Check(!pa.IsSeated, "Out-of-range seat slot is rejected");
                    Seat(a, pa, chairs[0].Id, 0); Go(6); return;
                }
                if (stage == 6)
                {
                    if (!pa.IsSeated) return;
                    Seat(b, pb, chairs[0].Id, 0); Go(7); return;
                }
                if (stage == 7)
                {
                    Check(!pb.IsSeated, "Concurrent occupancy cannot claim the same slot");
                    Seat(b, pb, chairs[1].Id, 0); Go(8); return;
                }
                if (stage == 8)
                {
                    if (!pb.IsSeated) return;
                    Bid(a, pa, 10); Go(9); return;
                }
                if (stage == 9)
                {
                    if(a.LastReply?.Kind==RequestKind.Bid && !a.LastReply.Value.Succeeded)
                        throw new InvalidOperationException("First remote bid failed: "+a.LastReply.Value.Message+" phase="+state.phase+" auction="+state.auctionStage);
                    if (state.highestBid != 10) return;
                    Bid(b, pb, 11); Go(10); return;
                }
                if (stage == 10)
                {
                    if (state.highestBid != 11) return;
                    Check(state.highestBidderId == pb.playerId, "Public highest bidder is identified by ID despite matching nicknames");
                    Check(game.Snapshot(new PlayerId(pa.playerId)).Reserved == 0 && game.Snapshot(new PlayerId(pb.playerId)).Reserved == 11,
                        "Outbid player's reservation is released while the new bid is reserved");
                    Go(11); return;
                }
                if (stage == 11)
                {
                    if (state.auctionStage != MatchNet.AuctionStage.Secret) return;
                    Check(game.InformationOf(new PlayerId(pb.playerId)).Count == 2 && game.InformationOf(new PlayerId(pa.playerId)).Count == 0,
                        "Only the public winner receives the lot's private directions");
                    Go(12, .3); return;
                }
                if (stage == 12)
                {
                    publicUpdates = a.UpdateCount(state.netId); otherUpdates = a.UpdateCount(pb.netId);
                    Bid(b, pb, 15); Go(13, .3); return;
                }
                if (stage == 13)
                {
                    Check(game.Auction.Secret.GetSubmitted(new PlayerId(pb.playerId)) == 15, "Secret bid reaches the dedicated server");
                    Check(a.UpdateCount(state.netId) == publicUpdates && a.UpdateCount(pb.netId) == otherUpdates,
                        "Secret submission causes no public market or other-player state delta");
                    stableB = pb.playerId; tokenB = b.Token;
                    departedNetId = pb.netId;
                    Check(!string.IsNullOrEmpty(tokenB), "Reconnect credentials are returned only to the admitted connection");
                    b.Disconnect(); peers.Remove(b); Go(14); return;
                }
                if (stage == 14)
                {
                    if (NetworkServer.spawned.ContainsKey(departedNetId)) return;
                    b = new LoopbackLobbyPeer("같은이름", resumeToken: tokenB); peers.Add(b); Go(15); return;
                }
                if (stage == 15)
                {
                    pb = Player(b); if (!pb || !pb.inMatch) return;
                    Check(pb.playerId == stableB, "Reconnection restores the same participant ID with a new connection");
                    Check(game.Snapshot(new PlayerId(stableB)).Reserved == 15 && game.InformationOf(new PlayerId(stableB)).Count == 2,
                        "Reconnection retains cash reservation and earned information without refunding a live bid");
                    Go(16); return;
                }
                if (stage == 16)
                {
                    if (state.auctionStage != MatchNet.AuctionStage.None) return;
                    Check(game.Snapshot(new PlayerId(stableB)).Reserved == 0 && game.InformationOf(new PlayerId(stableB)).Count == 4,
                        "Resumed participant settles the secret auction once and receives its private lot");
                    stockBefore = state.stock[0];
                    a.Command(pa, nameof(PlayerNet.CmdTrade), w => { w.Write(Company.Straw); w.Write(1); w.Write(true); });
                    Go(17); return;
                }
                if (stage == 17)
                {
                    Check(game.Snapshot(new PlayerId(pa.playerId)).GetShares(Company.Straw) == 1 && state.stock[0] == stockBefore - 1,
                        "Remote trade changes the authoritative portfolio and public stock");
                    Go(18); return;
                }
                if (stage == 18)
                {
                    if (!game.Complete) return;
                    Check(state.results.Count == 2 && state.results.Any(r => r.PlayerId == stableB), "Final ranking includes the resumed participant");
                    a.Command(pa, nameof(PlayerNet.CmdReturnToLobby)); Go(19); return;
                }
                if (stage == 19)
                {
                    if (room.MatchStarted) return;
                    Check(!pa.inMatch && !pb.inMatch && !pa.ready && !pb.ready, "Returning to lobby resets participant match/ready state");
                    Check(pa.GetComponent<PlayerAssetsNet>().Information.Count == 0 && pb.GetComponent<PlayerAssetsNet>().Information.Count == 0,
                        "Returning to lobby clears private information before the next match");
                    a.Command(pa, nameof(PlayerNet.SetReady), w => w.WriteBool(true)); b.Command(pb, nameof(PlayerNet.SetReady), w => w.WriteBool(true));
                    Go(20); return;
                }
                if (stage == 20)
                {
                    if (!room.CanStartMatch) return;
                    a.Command(pa, nameof(PlayerNet.CmdStartMatch)); Go(21); return;
                }
                if (stage == 21)
                {
                    if (!room.MatchStarted) return;
                    Check(pa.inMatch && pb.inMatch, "The same connected participants start a second match");
                    Finish(true, "Dedicated server and two UDP peers passed");
                }
            }
            catch (Exception e) { Debug.LogException(e); Finish(false, e.ToString()); }
        }

        private static void Finish(bool success, string message)
        {
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
            foreach (var peer in peers) peer.Disconnect(); peers.Clear();
            if (room && NetworkServer.active) room.StopServer();
            report.Status = (success ? "PASS: " : "FAIL: ") + message;
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
