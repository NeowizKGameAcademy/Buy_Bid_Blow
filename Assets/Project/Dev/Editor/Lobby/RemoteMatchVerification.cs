using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Props;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Market;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // Real Unity client against a separately built server: hooks, owner state, UI and remote animation.
    // Start Builds/Server/BuyBidBlowServer.exe with -port 17778 -minPlayers 1 -roundSeconds 4 first.
    [InitializeOnLoad]
    public static class RemoteMatchVerification
    {
        private const string Key = "BBB.RemoteMatchVerification", Root = ".utmp/defect-fix/";
        [Serializable] private sealed class Report { public string Status; public List<string> Checks = new List<string>(); }
        private static readonly Report report = new Report();
        private static GameNetworkManager room;
        private static PlayerNet local, other;
        private static LoopbackLobbyPeer peer;
        private static HUDController hud;
        private static BBBWolfCharacter wolf;
        private static RequestResult? reply;
        private static int stage, price, cash, stock;
        private static double deadline, waitUntil;

        static RemoteMatchVerification()
        {
            if (!SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 230;
            EditorApplication.update += Tick;
        }

        public static void RunBatch()
        {
            EditorSceneManager.OpenScene("Assets/Project/Scenes/SC_ExchangeFurnished.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            report.Checks.Add(message);
        }
        private static void Go(int next, double delay = .05)
        {
            stage = next; waitUntil = EditorApplication.timeSinceStartup + delay;
            Debug.Log("REMOTE_QA stage=" + stage);
        }
        private static void OnReply(RequestResult value) => reply = value;

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Remote verification stage " + stage);
                peer?.Tick();
                if (EditorApplication.timeSinceStartup < waitUntil) return;
                if (stage == 0)
                {
                    room = Object.FindFirstObjectByType<GameNetworkManager>();
                    room.networkAddress = "127.0.0.1"; room.LocalNickname = "원격 검증";
                    ((kcp2k.KcpTransport)room.transport).Port = 17778;
                    PlayerNet.LocalReply += OnReply;
                    room.StartClient(); Go(1); return;
                }
                if (stage == 1)
                {
                    if (!NetworkClient.localPlayer) return;
                    local = NetworkClient.localPlayer.GetComponent<PlayerNet>();
                    Check(!NetworkServer.active && !local.isServer, "Unity client has no in-process server");
                    peer = new LoopbackLobbyPeer("상대 검증", port: 17778); Go(2); return;
                }
                if (stage == 2)
                {
                    other = PlayerNet.All.FirstOrDefault(p => p.netId == peer.OwnNetId);
                    if (!other) return;
                    Check(local.seat < other.seat, "Real Unity client is the room host");
                    local.SetReady(true); peer.Command(other, nameof(PlayerNet.SetReady), w => w.Write(true)); Go(3); return;
                }
                if (stage == 3)
                {
                    if (!local.ready || !other.ready) return;
                    local.CmdStartMatch(); Go(4); return;
                }
                var state = MatchNet.Current;
                if (stage == 4)
                {
                    if (!state || state.phase != MatchNet.Phase.Playing || PlayerNet.LocalPortfolio == null) return;
                    Check(Object.FindFirstObjectByType<HallSession>().IsPlaying, "Remote inMatch hook enters the existing hall");
                    hud = Object.FindFirstObjectByType<HUDController>();
                    price = state.GetPrice(Company.Straw); cash = PlayerNet.LocalPortfolio.Cash;
                    Object.FindFirstObjectByType<LocalTradeController>().OpenPanel(Company.Straw);
                    Object.FindFirstObjectByType<TradePanelController>().Confirm(); Go(5); return;
                }
                if (stage == 5)
                {
                    if (!reply.HasValue) return;
                    Check(reply.Value.Kind == RequestKind.Buy && reply.Value.Succeeded && PlayerNet.LocalPortfolio.Cash == cash - price,
                        "Successful remote trade reply observes the already-updated owner snapshot");
                    Check(Object.FindFirstObjectByType<PlayerAssetStatus>().Cash == cash - price, "Remote HUD assets follow owner state");
                    stock = state.GetStock(Company.Brick);
                    Object.FindFirstObjectByType<LocalTradeController>().OpenPanel(Company.Brick);
                    peer.Command(other, nameof(PlayerNet.CmdTrade), w => { w.Write(Company.Brick); w.Write(1); w.Write(true); });
                    wolf = other.GetComponentInChildren<BBBWolfCharacter>();
                    peer.Command(other, nameof(PlayerNet.CmdJump)); Go(6); return;
                }
                if (stage == 6)
                {
                    if (!wolf.animator.GetCurrentAnimatorStateInfo(0).IsName("Jump")) return;
                    var view = Object.FindFirstObjectByType<TradingPanelView>();
                    if (state.GetStock(Company.Brick) != stock - 1 || view.transform.Find("Stock Availability/Market Stock").GetComponent<UnityEngine.UI.Text>().text != (stock - 1) + "주") return;
                    Check(true, "Other player's remote trade refreshes stock in the already-open panel");
                    Object.FindFirstObjectByType<TradePanelController>().Close();
                    Check(true, "Remote jump Command/Rpc reaches the observer Animator");
                    peer.Command(other, nameof(PlayerNet.CmdEmote), w => w.Write(1)); Go(7); return;
                }
                if (stage == 7)
                {
                    if (!wolf.animator.GetCurrentAnimatorStateInfo(0).IsName("Mixamo_Defeat")) return;
                    Check(true, "Remote emote Command/Rpc reaches the observer Animator");
                    peer.Command(other, nameof(PlayerNet.CmdEmote), w => w.Write(0)); Go(8); return;
                }
                if (stage == 8)
                {
                    if (state.auctionStage != MatchNet.AuctionStage.Open) return;
                    var chairs = Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).Where(s => s.auction).ToArray();
                    local.CmdSit(chairs[0].Id, 0);
                    peer.Command(other, nameof(PlayerNet.CmdSit), w => { w.Write(chairs[1].Id); w.Write(0); }); Go(9); return;
                }
                if (stage == 9)
                {
                    if (!local.IsSeated || !other.IsSeated) return;
                    peer.Command(other, nameof(PlayerNet.CmdBid), w => { w.Write(1); w.Write(false); }); Go(10); return;
                }
                if (stage == 10)
                {
                    if (!wolf.paddle.activeSelf) return;
                    Check(true, "Public bid makes the remote wolf's paddle visible");
                    local.CmdBid(2, false); Go(11); return;
                }
                if (stage == 11)
                {
                    if (PlayerNet.LocalInformation.Count < 2) return;
                    Check(PlayerNet.LocalPortfolio.Reserved == 0 && PlayerNet.LocalPortfolio.Cash == cash - price - 2,
                        "Remote settlement releases reservation, charges once and retains earned information");
                    hud.Notebook.SetOpen(true, state.round); Go(12, .3); return;
                }
                if (stage == 12)
                {
                    Check(hud.Notebook.cellValues.Count(t => t.text != "") == 4, "Production notebook shows the two awarded information rows only");
                    Capture("notebook.png"); hud.Notebook.SetOpen(false, state.round); Go(13); return;
                }
                if (stage == 13)
                {
                    if (state.phase != MatchNet.Phase.Finished || !hud.Results.IsOpen) return;
                    Check(state.results.Count == 2 && hud.Results.Body.text == "17 / 17 ROUND", "Remote final results contain both authoritative standings");
                    Check(hud.Results.CashLabels.Count(t => t.text == "—") >= 1, "Results hide other players' private cash breakdown");
                    Capture("results.png"); hud.Results.Buttons[0].onClick.Invoke(); Go(14); return;
                }
                if (stage == 14)
                {
                    if (local.inMatch || MatchNet.Current || Object.FindFirstObjectByType<HallSession>().IsPlaying) return;
                    Check(PlayerNet.LocalPortfolio == null && PlayerNet.LocalInformation.Count == 0, "Remote lobby return clears private state and gameplay UI");
                    local.SetReady(true); peer.Command(other, nameof(PlayerNet.SetReady), w => w.Write(true)); Go(15); return;
                }
                if (stage == 15)
                {
                    if (!local.ready || !other.ready) return;
                    local.CmdStartMatch(); Go(16); return;
                }
                if (stage == 16 && local.inMatch && PlayerNet.LocalPortfolio != null)
                {
                    Check(PlayerNet.LocalPortfolio.Cash == 200 && PlayerNet.LocalInformation.Count == 0, "Second remote match starts with fresh private state");
                    Finish("PASS");
                }
            }
            catch (Exception error) { Debug.LogException(error); Finish("FAIL: " + error); }
        }

        private static void Capture(string name)
        {
            var camera = Object.FindFirstObjectByType<PlayerMove>().GetComponentInChildren<Camera>();
            var canvas = hud.GetComponentInParent<Canvas>();
            var mode = canvas.renderMode; var oldCamera = canvas.worldCamera; float distance = canvas.planeDistance;
            var oldTarget = camera.targetTexture; var active = RenderTexture.active;
            var target = new RenderTexture(1920, 1080, 24); var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply(); File.WriteAllBytes(Root + name, pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = mode; canvas.worldCamera = oldCamera; canvas.planeDistance = distance;
                camera.targetTexture = oldTarget; RenderTexture.active = active; Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            }
        }
        private static void Finish(string status)
        {
            report.Status = status; File.WriteAllText(Root + "remote.json", JsonUtility.ToJson(report, true));
            PlayerNet.LocalReply -= OnReply; peer?.Disconnect(); peer = null;
            if (room && NetworkClient.active) room.StopClient();
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick; EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(status == "PASS" ? 0 : 1);
        }
    }
}
