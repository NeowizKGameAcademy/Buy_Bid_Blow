using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Auction;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Room;
using BuyBidBlow.Systems.Round;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Network
{
    public struct LobbyJoinRequest : NetworkMessage {public int protocol;public string nickname;}
    // minPlayers: the server's start threshold (e.g. -minPlayers 1), so clients judge "all ready" the same way.
    public struct LobbyJoinResponse : NetworkMessage {public bool accepted;public string reason;public int minPlayers;}
    public struct LobbyClosing : NetworkMessage {}
    public struct MatchStarting : NetworkMessage {}
    public sealed class GameNetworkManager : NetworkManager
    {
        public const int ProtocolVersion=3;
        public const string PlayScene="SC_ExchangeFurnished";
        [Header("Match")]
        // Lower this in the Inspector to test with fewer players (e.g. one ParrelSync clone).
        [SerializeField,Min(1)] int minPlayers=3;
        [SerializeField] string menuScene="SC_MainMenu";
        bool clientInMatch;
        public bool ClientInMatch=>clientInMatch;
        readonly RoomRoster roster=new RoomRoster();
        public event Action ClientConnected,AdmissionApproved,ClientDisconnected,LocalPlayerReady,ClientMatchStarted;
        public event Action<string> AdmissionRejected,ClientError;
        // Captured before connecting; the owned player submits it once after spawning.
        public string LocalNickname {get;set;}=string.Empty;
        // Server: its own setting. Client: the value the server sent on admission (falls back to this scene's).
        public int MinPlayers=>!NetworkServer.active&&serverMinPlayers>0?serverMinPlayers:minPlayers;
        int serverMinPlayers;
        public bool MatchStarted {get;private set;}
        public bool CanStartMatch
        {
            get {
                if(!NetworkServer.active||MatchStarted)return false;
                var players=NetworkServer.connections.Values.Where(c=>c.identity).Select(c=>c.identity.GetComponent<PlayerNet>()).Where(p=>p).ToArray();
                return players.Length>=minPlayers&&players.Length==roster.Count&&players.All(p=>p.ready);
            }
        }
        // Dedicated server options: BuyBidBlowServer.exe -port 7777 -minPlayers 3 [-roundSeconds 5]
        public override void Awake()
        {
            base.Awake();
            var args=Environment.GetCommandLineArgs();
            string Arg(string name){int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
            if(ushort.TryParse(Arg("-port"),out var port)&&transport is kcp2k.KcpTransport kcp)kcp.Port=port;
            if(int.TryParse(Arg("-minPlayers"),out var min)&&min>0)minPlayers=min;
            if(int.TryParse(Arg("-roundSeconds"),out var seconds)&&seconds>0)roundSecondsOverride=seconds;
        }
        public override void OnStartServer()
        {
            base.OnStartServer();roster.Clear();EndMatch();NetworkServer.RegisterHandler<LobbyJoinRequest>(ReceiveJoin);
            Debug.Log($"[Server] Listening on UDP {(transport as kcp2k.KcpTransport)?.Port} · min players {minPlayers}");
        }
        public override void OnStopServer(){roster.Clear();EndMatch();base.OnStopServer();}
        public override void OnStartClient()
        {
            base.OnStartClient();NetworkClient.RegisterHandler<LobbyJoinResponse>(ReceiveResponse);
            NetworkClient.RegisterHandler<LobbyClosing>(_=>AdmissionRejected?.Invoke("서버가 방을 닫았습니다."));
            NetworkClient.RegisterHandler<MatchStarting>(ReceiveMatchStarting);
            if(matchNetPrefab)NetworkClient.RegisterPrefab(matchNetPrefab);
        }
        // The room host (방장) is the player in the lowest occupied seat; there is no server host player.
        public bool IsRoomHost(PlayerNet player)
        {
            var seats=NetworkServer.connections.Values.Where(c=>c.identity).Select(c=>c.identity.GetComponent<PlayerNet>()).Where(p=>p).Select(p=>p.seat).ToArray();
            return seats.Length>0&&player.seat==seats.Min();
        }
        void ReceiveMatchStarting(MatchStarting message)
        {
            if(clientInMatch)return;
            clientInMatch=true;
            ClientMatchStarted?.Invoke();
        }
        // The hall is already loaded during lobby setup. Starting only changes the play state.
        [Server]
        public void RequestStartMatch(PlayerNet requester)
        {
            if(!IsRoomHost(requester)||!CanStartMatch)return;
            var participants=NetworkServer.connections.Values.Where(c=>c.identity&&c.identity.GetComponent<PlayerNet>()).ToArray();
            if(!BeginMatch(participants.Select(PlayerIdOf)))return;
            SpawnMatchNet();
            MatchStarted=true;
            foreach(var conn in participants)conn.identity.GetComponent<PlayerNet>().inMatch=true;
            NetworkServer.SendToAll(new MatchStarting());
            Debug.Log($"[Server] Match started with {participants.Length} player(s)");
        }

        // ---- Match controller (server only) ----
        // Rolls the price schedule and lots for the whole match and owns every participant's portfolio.
        // Trading and auctions share those portfolios, so cash reserved by a bid can't be spent on stock.
        [Header("Match rules")]
        [SerializeField] TextAsset matchSettingsFile;
        // 0 picks a new seed each match; set it to replay the same prices and lots.
        [SerializeField] int randomSeed;
        // Public match state for clients (Common/MatchNet), spawned per match.
        [SerializeField] GameObject matchNetPrefab;
        // Short synchronized countdown before round 1; no level loading happens here.
        [SerializeField,Min(0)] float prepareSeconds=5;
        // -roundSeconds: every round this long instead of the rules (quick playtests). 0 = rules.
        int roundSecondsOverride;
        RoundClock clock;
        MatchNet matchNet;
        // Minimum bid is still undecided in the rules; $1 until it is.
        const int MinBid=1;
        MatchSettings settings;
        LotGenerator lotGenerator;
        readonly Dictionary<Company,int> prices=new Dictionary<Company,int>();
        readonly List<PriceChange[]> scheduledPriceChanges=new List<PriceChange[]>();
        readonly List<int[]> scheduledPrices=new List<int[]>();
        readonly Dictionary<PlayerId,PlayerPortfolio> portfolios=new Dictionary<PlayerId,PlayerPortfolio>();
        public int CurrentRound {get;private set;}
        public bool IsMatchComplete=>settings!=null&&CurrentRound>=settings.RoundCount;
        public TradeService TradeService {get;private set;}
        public event Action<int,IReadOnlyList<PriceChange>> RoundAdvanced;
        // Round open for requests (0 while preparing) and its deadline; requests at or after the deadline are late.
        public int PlayingRound=>matchNet?matchNet.round:0;
        public double RoundClosesAt=>matchNet?matchNet.roundClosesAt:0;

        // Players are identified by their connection for the life of the match.
        public static PlayerId PlayerIdOf(NetworkConnectionToClient conn)=>new PlayerId("conn-"+conn.connectionId);

        bool BeginMatch(IEnumerable<PlayerId> players)
        {
            if(!matchSettingsFile){Debug.LogError("[Server] GameNetworkManager needs the MatchSettings JSON file.",this);return false;}
            if(!matchNetPrefab){Debug.LogError("[Server] GameNetworkManager needs the MatchNet prefab.",this);return false;}
            settings=JsonUtility.FromJson<MatchSettings>(matchSettingsFile.text);
            settings.Validate();
            var random=new System.Random(randomSeed!=0?randomSeed:Environment.TickCount);

            int[] startingPrices=settings.RollStartingPrices(random);
            scheduledPriceChanges.Clear();scheduledPrices.Clear();
            scheduledPrices.Add((int[])startingPrices.Clone());
            var nextPrices=(int[])startingPrices.Clone();
            for(int round=0;round<settings.RoundCount;round++)
            {
                PriceChange[] changes=PriceChangeGenerator.Generate(settings.PriceChange,random);
                scheduledPriceChanges.Add(changes);
                foreach(PriceChange change in changes)
                    nextPrices[(int)change.Company]=Math.Max(settings.MinPrice,nextPrices[(int)change.Company]+change.Amount);
                scheduledPrices.Add((int[])nextPrices.Clone());
            }
            lotGenerator=new LotGenerator(settings.AuctionRounds,scheduledPrices,random);

            CurrentRound=0;ApplyPrices(scheduledPrices[0]);
            portfolios.Clear();
            foreach(var player in players)AddPlayer(player);
            TradeService=new TradeService(portfolios,GetPrice);
            Debug.Log($"[Server] Match prices {string.Join(", ",prices.Select(p=>p.Key+" $"+p.Value))} · {portfolios.Count} portfolio(s)");
            return true;
        }

        void EndMatch()
        {
            if(matchNet&&NetworkServer.active)NetworkServer.Destroy(matchNet.gameObject);
            matchNet=null;clock=null;
            MatchStarted=false;settings=null;lotGenerator=null;TradeService=null;CurrentRound=0;
            prices.Clear();scheduledPriceChanges.Clear();scheduledPrices.Clear();portfolios.Clear();
        }

        public int GetPrice(Company company)
        {
            if(!prices.TryGetValue(company,out int price))throw new InvalidOperationException($"No price has been initialized for {company}.");
            return price;
        }

        public int GetPriceForRound(Company company,int round)
        {
            if(round<0||round>=scheduledPrices.Count)throw new ArgumentOutOfRangeException(nameof(round));
            return scheduledPrices[round][(int)company];
        }

        public IReadOnlyList<PriceChange> GetChangesForRound(int round)
        {
            if(round<1||round>scheduledPriceChanges.Count)throw new ArgumentOutOfRangeException(nameof(round));
            return scheduledPriceChanges[round-1];
        }

        // Registers a participant with the starting assets. Calling again for the same player keeps their assets.
        public void AddPlayer(PlayerId playerId)
        {
            if(settings==null)throw new InvalidOperationException("No match is running.");
            if(!portfolios.ContainsKey(playerId))portfolios.Add(playerId,new PlayerPortfolio(settings.StartingCash,settings.StartingShares));
        }

        // Lots come out in auction order: 1st (Open) then 2nd (Secret) of each auction round.
        public Lot NextLot()=>lotGenerator.Next();

        // Call at the start of an auction round, then feed it Tick(now) and player requests.
        public AuctionRound CreateAuctionRound(double roundStart)
        {
            if(settings==null)throw new InvalidOperationException("No match is running.");
            Lot first=NextLot();Lot second=NextLot();
            return new AuctionRound(first,second,roundStart,settings.AuctionSlots,MinBid,portfolios);
        }

        public void AdvanceRound()
        {
            if(settings==null||IsMatchComplete)return;
            CurrentRound++;
            PriceChange[] changes=scheduledPriceChanges[CurrentRound-1];
            ApplyPrices(scheduledPrices[CurrentRound]);
            RoundAdvanced?.Invoke(CurrentRound,changes);
        }

        void SpawnMatchNet()
        {
            var go=Instantiate(matchNetPrefab);matchNet=go.GetComponent<MatchNet>();
            double startsAt=NetworkTime.time+prepareSeconds;
            clock=new RoundClock(settings,startsAt,roundSecondsOverride);
            matchNet.phase=MatchNet.Phase.Preparing;matchNet.firstRoundStartsAt=startsAt;
            PublishPrices();
            NetworkServer.Spawn(go);
        }

        // Round timer: opens rounds on schedule and rings the Closing Bell when each one closes.
        public override void Update()
        {
            base.Update();
            if(!NetworkServer.active||clock==null||!matchNet||matchNet.phase==MatchNet.Phase.Finished)return;
            int due=clock.RoundAt(NetworkTime.time);
            while(matchNet.round<due)
            {
                int closing=matchNet.round;
                if(closing>=1)CloseRound(closing);
                if(closing+1>clock.RoundCount){FinishMatch();return;}
                OpenRound(closing+1);
            }
        }

        void OpenRound(int round)
        {
            matchNet.roundClosesAt=clock.ClosesAt(round);
            matchNet.phase=MatchNet.Phase.Playing;
            matchNet.round=round;
            // Auction rounds also run their two auctions; hooked up with the auction step (CreateAuctionRound).
            Debug.Log($"[Server] Round {round}/{clock.RoundCount} open for {clock.ClosesAt(round)-clock.StartsAt(round):F0}s{(settings.IsAuctionRound(round)?" · auction round":"")}");
        }

        // Closing Bell: prices were fixed during the round; apply this round's change now.
        void CloseRound(int round)
        {
            var changes=GetChangesForRound(round).ToArray();
            AdvanceRound();
            PublishPrices();
            matchNet.lastChanges.Clear();
            foreach(var change in changes)matchNet.lastChanges.Add(change);
            matchNet.lastClosedRound=round;
            Debug.Log($"[Server] Closing Bell R{round}: {string.Join(", ",prices.Select(p=>p.Key+" $"+p.Value))}");
        }

        void FinishMatch()
        {
            matchNet.phase=MatchNet.Phase.Finished;
            Debug.Log("[Server] Match finished");
        }

        void PublishPrices()
        {
            matchNet.prices.Clear();
            foreach(Company company in Enum.GetValues(typeof(Company)))matchNet.prices.Add(GetPrice(company));
        }

        void ApplyPrices(int[] roundPrices)
        {
            var companies=(Company[])Enum.GetValues(typeof(Company));
            for(int i=0;i<companies.Length;i++)prices[companies[i]]=roundPrices[i];
        }
        public override void OnClientConnect()
        {
            // Defer Ready/AddPlayer until server capacity and nickname checks have passed.
            ClientConnected?.Invoke();NetworkClient.Send(new LobbyJoinRequest{protocol=ProtocolVersion,nickname=LocalNickname});
        }
        public override void OnClientDisconnect()
        {
            ClientDisconnected?.Invoke();base.OnClientDisconnect();serverMinPlayers=0;
            if(!clientInMatch)return;
            // Lost the server during a match: back to the start screen. The lobby scene brings a fresh manager.
            clientInMatch=false;SceneManager.LoadScene(menuScene);Destroy(gameObject);
        }
        public override void OnClientError(TransportError error,string reason)
        {
            ClientError?.Invoke(error==TransportError.Timeout?"접속 시간이 초과되었습니다. 서버 상태를 확인하세요.":"네트워크 연결 오류가 발생했습니다. 주소·포트와 호스트 상태를 확인하세요.");
        }
        public override void OnServerConnect(NetworkConnectionToClient conn){base.OnServerConnect(conn);StartCoroutine(AdmissionTimeout(conn));}
        IEnumerator AdmissionTimeout(NetworkConnectionToClient conn)
        {
            yield return new WaitForSecondsRealtime(10);
            if(NetworkServer.active&&NetworkServer.connections.TryGetValue(conn.connectionId,out var current)&&current==conn&&!conn.identity) {
                roster.Release(conn.connectionId);Reject(conn,"입장 처리 시간이 초과되었습니다.");
            }
        }
        void ReceiveJoin(NetworkConnectionToClient conn,LobbyJoinRequest message)
        {
            if(conn.identity||roster.Contains(conn.connectionId))return;
            if(message.protocol!=ProtocolVersion){Reject(conn,"게임 버전이 다릅니다. 같은 버전으로 실행하세요.");return;}
            if(MatchStarted){Reject(conn,"이미 경기가 시작된 방입니다.");return;}
            if(!roster.TryReserve(conn.connectionId,message.nickname,out var error)){Reject(conn,error);return;}
            conn.Send(new LobbyJoinResponse{accepted=true,reason="",minPlayers=minPlayers});
        }
        void ReceiveResponse(LobbyJoinResponse message)
        {
            if(!message.accepted){AdmissionRejected?.Invoke(message.reason);return;}
            serverMinPlayers=message.minPlayers;
            if(NetworkClient.localPlayer||NetworkClient.ready)return;
            AdmissionApproved?.Invoke();NetworkClient.Ready();NetworkClient.AddPlayer();
        }
        void Reject(NetworkConnectionToClient conn,string reason)
        {
            conn.Send(new LobbyJoinResponse{accepted=false,reason=reason});StartCoroutine(DisconnectRejected(conn));
        }
        IEnumerator DisconnectRejected(NetworkConnectionToClient conn){yield return new WaitForSecondsRealtime(.25f);if(NetworkServer.active&&NetworkServer.connections.TryGetValue(conn.connectionId,out var current)&&current==conn)conn.Disconnect();}
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            roster.Release(conn.connectionId);base.OnServerDisconnect(conn);
            // The dedicated server keeps running; reopen the room once every player has left the match.
            if(MatchStarted&&!NetworkServer.connections.Values.Any(c=>c!=conn&&c.identity))EndMatch();
        }
        public void NotifyLocalPlayerReady()=>LocalPlayerReady?.Invoke();
        public void BroadcastClosing(){if(NetworkServer.active)NetworkServer.SendToAll(new LobbyClosing());}
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if(!roster.TryGetName(conn.connectionId,out var name)){Reject(conn,"입장 승인이 필요합니다.");return;}
            var used = NetworkServer.connections.Values.Where(c => c.identity).Select(c => c.identity.GetComponent<PlayerNet>().seat).ToArray();
            int seat = Enumerable.Range(0,6).FirstOrDefault(i => !used.Contains(i));
            if (used.Length >= 6) { conn.Disconnect(); return; }
            var player = Instantiate(playerPrefab);
            var lobbyPlayer=player.GetComponent<PlayerNet>();lobbyPlayer.seat = seat;
            lobbyPlayer.nickname=name;
            NetworkServer.AddPlayerForConnection(conn,player);
        }
    }
}
