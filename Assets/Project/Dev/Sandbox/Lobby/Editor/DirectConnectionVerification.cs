using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.LobbySandbox.Editor
{
    // Independent KCP sockets exercise actual UDP admission without a second NetworkClient singleton.
    sealed class LoopbackLobbyPeer
    {
        readonly kcp2k.KcpClient client;
        readonly Batcher batcher=new Batcher(1200);
        readonly Unbatcher unbatcher=new Unbatcher();
        public bool Accepted,Closing;
        public string Rejection="",Error="";
        public LoopbackLobbyPeer(string nickname,int protocol=LobbyRoom.ProtocolVersion)
        {
            client=new kcp2k.KcpClient(()=>Send(new LobbyJoinRequest{nickname=nickname,protocol=protocol}),Receive,()=>{},(e,r)=>Error=r,new kcp2k.KcpConfig());
            client.Connect("127.0.0.1",17777);
        }
        void Send<T>(T message) where T:struct,NetworkMessage
        {
            using(var writer=NetworkWriterPool.Get()) {NetworkMessages.Pack(message,writer);batcher.AddMessage(writer.ToArraySegment(),NetworkTime.localTime);}
            using(var writer=NetworkWriterPool.Get())while(batcher.GetBatch(writer)){client.Send(writer.ToArraySegment(),kcp2k.KcpChannel.Reliable);writer.Reset();}
        }
        void Receive(ArraySegment<byte> data,kcp2k.KcpChannel channel)
        {
            if(channel!=kcp2k.KcpChannel.Reliable)return;
            unbatcher.AddBatch(data);
            while(unbatcher.GetNextMessage(out var message,out _)) {
                using(var reader=NetworkReaderPool.Get(message)) {
                    if(!NetworkMessages.UnpackId(reader,out var id))continue;
                    if(id==NetworkMessages.GetId<LobbyJoinResponse>()) {
                        var response=reader.Read<LobbyJoinResponse>();Accepted=response.accepted;
                        if(Accepted){Send(new ReadyMessage());Send(new AddPlayerMessage());}else Rejection=response.reason;
                    }else if(id==NetworkMessages.GetId<LobbyClosing>())Closing=true;
                }
            }
        }
        public void Tick(){client.TickIncoming();client.TickOutgoing();}
        public void Disconnect(){client.Disconnect();batcher.Clear();}
    }

    [InitializeOnLoad] public static class DirectConnectionVerification
    {
        const string Request="Library/DirectConnection.request",StageKey="DirectConnection.Stage";
        static readonly List<LoopbackLobbyPeer> peers=new List<LoopbackLobbyPeer>();
        static double next,timeout;
        static int stage;
        static System.Net.Sockets.UdpClient blackhole;
        static string blackholePort;
        static readonly string[] PrefKeys={LobbyNickname.PreferenceKey,"BuyBidBlow.Lobby.Address","BuyBidBlow.Lobby.Port"};
        static DirectConnectionVerification(){EditorApplication.update+=Tick;}
        public static void RunBatch(){SessionState.SetInt(StageKey,0);File.WriteAllText(Request,"batch");}
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Go(int value,float delay=.3f){stage=value;SessionState.SetInt(StageKey,value);next=EditorApplication.timeSinceStartup+delay;timeout=EditorApplication.timeSinceStartup+12;}
        static void SavePreferences()
        {
            foreach(var key in PrefKeys){SessionState.SetBool(key+".testHad",PlayerPrefs.HasKey(key));SessionState.SetString(key+".testValue",key.EndsWith(".Port")?PlayerPrefs.GetInt(key).ToString():PlayerPrefs.GetString(key));}
        }
        static void RestorePreferences()
        {
            foreach(var key in PrefKeys)if(!SessionState.GetBool(key+".testHad",false))PlayerPrefs.DeleteKey(key);else if(key.EndsWith(".Port"))PlayerPrefs.SetInt(key,int.Parse(SessionState.GetString(key+".testValue","7777")));else PlayerPrefs.SetString(key,SessionState.GetString(key+".testValue",""));PlayerPrefs.Save();
        }
        static void ApplyScene()
        {
            var scene=SceneManager.GetSceneByPath(LobbyBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(LobbyBuilder.ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LobbyHud>(true)).Single();hud.UpgradeConnectionPanel();
            var session=hud.room.GetComponent<ConnectionController>();if(!session)session=hud.room.gameObject.AddComponent<ConnectionController>();
            hud.room.maxConnections=LobbyRoster.Capacity+4;
            var serialized=new SerializedObject(hud);serialized.FindProperty("session").objectReferenceValue=session;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(hud.room);EditorUtility.SetDirty(hud);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();LobbyNicknameUpgrade.RenderConnection(hud);
            Check(DirectConnectionSettings.TryAddress("192.168.0.10",out _)&&DirectConnectionSettings.TryAddress("localhost",out _)&&!DirectConnectionSettings.TryAddress("https://example.com",out _)&&!DirectConnectionSettings.TryAddress("999.1.1.1",out _),"Address validation failed.");
            Check(DirectConnectionSettings.TryPort("65535",out _)&&!DirectConnectionSettings.TryPort("0",out _)&&!DirectConnectionSettings.TryPort("65536",out _),"Port boundary validation failed.");
            var roster=new LobbyRoster();for(int i=0;i<6;i++)Check(roster.TryReserve(i,"이름"+i,out _),"Reservation failed.");Check(!roster.TryReserve(6,"초과",out _),"Capacity exceeded.");roster.Release(2);Check(roster.TryReserve(6,"대체",out _),"Seat reservation not returned.");
            File.WriteAllText("Library/DirectConnection.result","PASS: UI + controller saved; endpoint and capacity policy validated. UDP play tests running.\n");
        }
        static void Tick()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            stage=SessionState.GetInt(StageKey,0);
            try {
                if(stage==0) {
                    if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                    ApplyScene();SavePreferences();Go(1,3);EditorApplication.isPlaying=true;return;
                }
                if(!EditorApplication.isPlaying)return;
                foreach(var peer in peers.ToArray())peer.Tick();
                if(EditorApplication.timeSinceStartup<next)return;
                if(timeout==0)timeout=EditorApplication.timeSinceStartup+12;
                Check(EditorApplication.timeSinceStartup<timeout,"Timed out at test stage "+stage);
                var session=UnityEngine.Object.FindFirstObjectByType<ConnectionController>();Check(session!=null,"Connection controller missing.");
                var players=UnityEngine.Object.FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None).Where(p=>p.isServer).ToArray();
                if(stage==1) {Check(!session.Join("호스트","not-an-IP","17777"),"Invalid endpoint accepted.");Check(session.Host("호스트","17777"),"Host did not start.");Go(2);return;}
                if(stage==2) {
                    if(session.State!=ConnectionState.InRoom)return;Check(players.Length==1&&players[0].nickname=="호스트","Host spawn/name mismatch.");
                    peers.Add(new LoopbackLobbyPeer("참가자"));Go(3);return;
                }
                if(stage==3) {
                    if(players.Length!=2)return;Check(peers[0].Accepted&&players.Any(p=>p.nickname=="참가자"),"UDP client admission/name failed.");
                    for(int i=0;i<4;i++)peers.Add(new LoopbackLobbyPeer("추가"+i));Go(4);return;
                }
                if(stage==4) {
                    if(players.Length!=6)return;Check(players.Select(p=>p.seat).Distinct().Count()==6,"Duplicate seats.");
                    peers.Add(new LoopbackLobbyPeer("초과"));Go(5);return;
                }
                if(stage==5) {
                    if(string.IsNullOrEmpty(peers[5].Rejection))return;Check(peers[5].Rejection.Contains("가득"),"Capacity rejection reason missing.");
                    peers.Add(new LoopbackLobbyPeer("다른버전",999));Go(6);return;
                }
                if(stage==6) {
                    if(string.IsNullOrEmpty(peers[6].Rejection))return;Check(peers[6].Rejection.Contains("버전"),"Protocol rejection failed.");
                    peers[0].Disconnect();Go(7);return;
                }
                if(stage==7) {if(players.Length!=5)return;peers.Add(new LoopbackLobbyPeer("재입장"));Go(8);return;}
                if(stage==8) {
                    if(players.Length!=6||!players.Any(p=>p.nickname=="재입장"))return;
                    File.AppendAllText("Library/DirectConnection.result","PASS: independent UDP KCP peer admitted, nickname synchronized; 6 unique seats; seventh rejected as full; version mismatch rejected; disconnect/rejoin frees reservation.\n");
                    session.Leave();Go(9);return;
                }
                if(stage==9) {
                    if(session.State!=ConnectionState.Idle)return;Check(peers.Where((p,i)=>i>0&&i<5||i==7).All(p=>p.Closing),"Room-close message not delivered.");
                    foreach(var peer in peers)peer.Disconnect();peers.Clear();
                    typeof(ConnectionController).GetField("connectionTimeout",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(session,1f);
                    blackhole=new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,0));
                    blackholePort=((System.Net.IPEndPoint)blackhole.Client.LocalEndPoint).Port.ToString();
                    Check(session.Join("타임아웃","127.0.0.1",blackholePort),"Timeout connection not started.");Go(10,1.5f);return;
                }
                if(stage==10) {
                    if(session.State!=ConnectionState.Failed)return;Check(session.Message.Contains("초과"),"Timeout reason missing.");
                    Check(session.Join("취소","127.0.0.1",blackholePort),"Retry failed.");session.Cancel();Go(11);return;
                }
                if(stage==11) {
                    if(session.State!=ConnectionState.Idle)return;Check(!NetworkClient.active&&!NetworkServer.active,"Cancellation leaked network state.");
                    blackhole.Dispose();blackhole=null;
                    Check(session.Host("다시호스트","17777"),"Host restart failed.");Go(12);return;
                }
                if(stage==12) {
                    if(session.State!=ConnectionState.InRoom)return;Check(players.Single().nickname=="다시호스트","Reconnection used stale nickname.");session.Leave();Go(13);return;
                }
                if(stage==13) {
                    if(session.State!=ConnectionState.Idle)return;
                    File.AppendAllText("Library/DirectConnection.result","PASS: host close delivered to remote peers; timeout; cancel cleanup; host restart. Full second-client rendering and separate-PC LAN test remain manual.\n");
                    File.WriteAllText(LobbyBuilder.Root+"/UI/DirectConnectionVerification.txt",File.ReadAllText("Library/DirectConnection.result"));RestorePreferences();File.Delete(Request);SessionState.SetInt(StageKey,0);timeout=0;EditorApplication.isPlaying=false;AssetDatabase.Refresh();
                    if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                }
            }catch(Exception ex) {
                File.AppendAllText("Library/DirectConnection.result",ex+"\n");foreach(var peer in peers)peer.Disconnect();peers.Clear();
                blackhole?.Dispose();blackhole=null;
                if(stage>0)RestorePreferences();File.Delete(Request);SessionState.SetInt(StageKey,0);timeout=0;
                if(EditorApplication.isPlaying){var room=UnityEngine.Object.FindFirstObjectByType<LobbyRoom>();if(room){if(NetworkServer.active)room.StopHost();else if(NetworkClient.active)room.StopClient();}EditorApplication.isPlaying=false;}Debug.LogException(ex);
                if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(1);
            }
        }
    }
}
