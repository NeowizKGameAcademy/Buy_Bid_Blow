using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Match;
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
    public struct MatchReturning : NetworkMessage {}
    public sealed class GameNetworkManager : NetworkManager
    {
        public const int ProtocolVersion=6;
        public const string PlayScene="SC_ExchangeFurnished";
        [Header("Match")]
        // Lower this in the Inspector to test with fewer players (e.g. one ParrelSync clone).
        [SerializeField,Min(1)] int minPlayers=3;
        [SerializeField] string menuScene="SC_MainMenu";
        bool clientInMatch;
        public bool ClientInMatch=>clientInMatch;
        readonly RoomRoster roster=new RoomRoster();
        public event Action ClientConnected,AdmissionApproved,ClientDisconnected,LocalPlayerReady,ClientMatchStarted;
        public event Action ClientMatchReturned;
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
            NetworkClient.RegisterHandler<MatchReturning>(_=>{clientInMatch=false;ClientMatchReturned?.Invoke();});
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
            if(!BeginMatch(participants.Select(PlayerIdOf).ToList()))return;
            MatchStarted=true;
            foreach(var conn in participants)conn.identity.GetComponent<PlayerNet>().inMatch=true;
            SendPortfolios();
            NetworkServer.SendToAll(new MatchStarting());
            Debug.Log($"[Server] Match started with {participants.Length} player(s)");
        }

        // ---- Match controller (server only) ----
        // MatchEngine owns the rules (prices, stock, portfolios, bets, auctions, standings).
        // This only drives its clock, forwards participants' requests and publishes the results.
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
        MatchNet matchNet;
        MatchEngine engine;
        sealed class Participant { public PlayerId Id; public string Name; }
        readonly List<Participant> matchParticipants = new List<Participant>();

        // Players are identified by their connection for the life of the match.
        public static PlayerId PlayerIdOf(NetworkConnectionToClient conn)=>new PlayerId("conn-"+conn.connectionId);

        bool BeginMatch(IReadOnlyList<PlayerId> players)
        {
            if(!matchSettingsFile){Debug.LogError("[Server] GameNetworkManager needs the MatchSettings JSON file.",this);return false;}
            if(!matchNetPrefab){Debug.LogError("[Server] GameNetworkManager needs the MatchNet prefab.",this);return false;}
            var settings=JsonUtility.FromJson<MatchSettings>(matchSettingsFile.text);
            Debug.Log($"[Server] Match rules: {settings.RoundCount} rounds, ordinary {settings.RoundSeconds}s, auction {settings.AuctionRoundSeconds}s, clock override {roundSecondsOverride}s");
            double startsAt=NetworkTime.time+prepareSeconds;
            engine=new MatchEngine(settings,randomSeed!=0?randomSeed:Environment.TickCount,players,startsAt,roundSecondsOverride);
            matchParticipants.Clear();
            foreach(var conn in NetworkServer.connections.Values)
                if(conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var p) && players.Contains(PlayerIdOf(conn)))
                    matchParticipants.Add(new Participant{Id=PlayerIdOf(conn),Name=p.nickname});
            engine.BetSettled += (id,company,stake,payout)=>
            {
                var owner=FindParticipant(id);
                if(owner)owner.TargetNotice(new MatchNotice{Kind=MatchNoticeKind.BetSettled,Company=(int)company,Amount=stake,Payout=payout,Won=payout>0});
            };
            engine.PublicResolved += outcome=>
            {
                foreach(var conn in NetworkServer.connections.Values)
                    if(conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var p) && p.inMatch)
                        p.TargetAuctionOutcome(new AuctionOutcome{Sold=outcome.Sold,Won=outcome.Sold&&PlayerIdOf(conn).Equals(outcome.Winner),Winner=outcome.Sold?NicknameOf(outcome.Winner):"",Amount=outcome.Price});
            };
            engine.SecretResolved += outcome=>
            {
                // Only a bidder receives their own outcome; never send the winner or winning price to losers.
                foreach(var id in outcome.Players)
                {
                    var owner=FindParticipant(id);if(!owner)continue;
                    owner.TargetAuctionOutcome(new AuctionOutcome{Secret=true,Sold=outcome.Result.Sold,Won=outcome.Result.Sold&&id.Equals(outcome.Result.Winner),Amount=engine.Auction.Secret.GetSubmitted(id)});
                }
            };
            foreach(var conn in NetworkServer.connections.Values)
                if(conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var participant)) participant.TargetResetInformation();
            engine.InformationAwarded += award =>
            {
                foreach(var conn in NetworkServer.connections.Values)
                    if(PlayerIdOf(conn).Equals(award.Winner) && conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var winner))
                    { winner.TargetAwardInformation(award.Lot.First, award.Lot.Second); break; }
            };

            var go=Instantiate(matchNetPrefab);matchNet=go.GetComponent<MatchNet>();
            matchNet.totalRounds=settings.RoundCount;
            matchNet.phase=MatchNet.Phase.Preparing;matchNet.firstRoundStartsAt=startsAt;matchNet.roundClosesAt=engine.Deadline;
            go.GetComponent<MatchTimeline>().Publish(MatchSchedule.Build(settings,startsAt-prepareSeconds,startsAt,roundSecondsOverride));
            PublishMarket();PublishAuction(NetworkTime.time);
            NetworkServer.Spawn(go);
            Debug.Log($"[Server] Match prices {string.Join(", ",Companies.Select(c=>c+" $"+engine.Price(c)))} · {players.Count} player(s)");
            return true;
        }

        void EndMatch()
        {
            if(matchNet&&NetworkServer.active)NetworkServer.Destroy(matchNet.gameObject);
            foreach(var conn in NetworkServer.connections.Values)if(conn.identity&&conn.identity.TryGetComponent<PlayerNet>(out var player))player.StandUp();
            matchNet=null;engine=null;MatchStarted=false;
        }

        static readonly Company[] Companies=(Company[])Enum.GetValues(typeof(Company));

        // Opens round 1 after the countdown, then lets the engine close rounds on its own deadlines.
        public override void Update()
        {
            base.Update();
            if(!NetworkServer.active||engine==null||!matchNet||matchNet.phase==MatchNet.Phase.Finished)return;
            double now=NetworkTime.time;
            if(matchNet.phase==MatchNet.Phase.Preparing)
            {
                if(now<matchNet.firstRoundStartsAt)return;
                matchNet.phase=MatchNet.Phase.Playing;matchNet.round=engine.Round;
                Debug.Log($"[Server] Round {engine.Round}/{engine.Settings.RoundCount} open");
            }
            int round=engine.Round;
            engine.Tick(now);
            PublishAuction(now);
            if(engine.Round==round&&!engine.Complete)return;
            // Closing Bell: publish the new prices and changes, then the next round (or the end).
            PublishMarket();
            matchNet.lastChanges.Clear();
            foreach(var company in Companies)if(engine.LastChange(company)!=0)matchNet.lastChanges.Add(new PriceChange(company,engine.LastChange(company)));
            matchNet.lastClosedRound=engine.Complete?engine.Settings.RoundCount:engine.Round-1;
            if(engine.Complete){matchNet.phase=MatchNet.Phase.Finished;Debug.Log("[Server] Match finished\n"+engine.Standings());}
            else{matchNet.roundClosesAt=engine.Deadline;matchNet.round=engine.Round;Debug.Log($"[Server] Round {engine.Round}/{engine.Settings.RoundCount} open");}
            // Bets settle at the bell, so every portfolio may have changed.
            SendPortfolios();
            if(engine.Complete)PublishResults();
        }

        PlayerNet FindParticipant(PlayerId id)
        {
            foreach(var conn in NetworkServer.connections.Values)
                if(PlayerIdOf(conn).Equals(id) && conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var p))return p;
            return null;
        }

        void PublishResults()
        {
            var order=matchParticipants.OrderByDescending(p=>engine.Total(p.Id)).ThenByDescending(p=>Companies.Sum(c=>engine.Snapshot(p.Id).GetShares(c))).ToArray();
            foreach(var conn in NetworkServer.connections.Values)
            {
                if(!conn.identity || !conn.identity.TryGetComponent<PlayerNet>(out var recipient) || !recipient.inMatch)continue;
                var rows=new FinalStanding[order.Length];int rank=0,previousTotal=-1,previousShares=-1;
                for(int i=0;i<order.Length;i++)
                {
                    var p=order[i];var snapshot=engine.Snapshot(p.Id);int total=engine.Total(p.Id),shares=Companies.Sum(c=>snapshot.GetShares(c));
                    if(total!=previousTotal || shares!=previousShares)rank=i+1;
                    bool own=PlayerIdOf(conn).Equals(p.Id);
                    rows[i]=new FinalStanding{Rank=rank,Total=total,Nickname=p.Name,Own=own,Cash=own?snapshot.Cash:0,StockValue=own?total-snapshot.Cash:0,Shares=own?shares:0};
                    previousTotal=total;previousShares=shares;
                }
                recipient.TargetResults(rows,engine.Settings.RoundCount);
            }
        }

        [Server] public void RequestReturnToLobby(PlayerNet requester)
        {
            if(!IsRoomHost(requester) || engine==null || !engine.Complete)return;
            foreach(var conn in NetworkServer.connections.Values)
                if(conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var p)){p.inMatch=false;p.ready=false;p.TargetResetInformation();}
            EndMatch();NetworkServer.SendToAll(new MatchReturning());
        }

        // The running auction slot as everyone may see it. Mirror only sends values that changed.
        void PublishAuction(double now)
        {
            var auction=engine.Complete?null:engine.Auction;
            bool secret=auction?.Secret!=null&&now<auction.Secret.Deadline;
            bool open=!secret&&auction?.Open!=null&&now<auction.Open.Deadline;
            var stage=secret?MatchNet.AuctionStage.Secret:open?MatchNet.AuctionStage.Open:MatchNet.AuctionStage.None;
            // An auction slot opened or closed: settlement moved cash and released reservations.
            if(stage!=matchNet.auctionStage)SendPortfolios();
            matchNet.auctionStage=stage;
            if(secret||open)
            {
                var lot=secret?auction.Secret.Lot:auction.Open.Lot;
                matchNet.auctionClosesAt=secret?auction.Secret.Deadline:auction.Open.Deadline;
                matchNet.auctionLot=new MatchNet.PublicLot{firstRound=lot.First.Round,firstA=(int)lot.First.CompanyA,firstB=(int)lot.First.CompanyB,
                    secondRound=lot.Second.Round,secondA=(int)lot.Second.CompanyA,secondB=(int)lot.Second.CompanyB};
            }
            bool bid=open&&auction.Open.HasBid;
            matchNet.highestBid=bid?auction.Open.HighestBid:0;
            matchNet.highestBidder=bid?NicknameOf(auction.Open.HighestBidder):"";
            matchNet.nextAuctionRound=engine.Settings.AuctionRounds.Where(r=>r>engine.Round).DefaultIfEmpty(0).Min();
        }

        string NicknameOf(PlayerId id)
        {
            foreach(var conn in NetworkServer.connections.Values)
                if(PlayerIdOf(conn).Equals(id)&&conn.identity&&conn.identity.TryGetComponent<PlayerNet>(out var player))return player.nickname;
            return "";
        }

        // Prices and remaining market stock, indexed by Company.
        void PublishMarket()
        {
            matchNet.prices.Clear();matchNet.stock.Clear();
            foreach(var company in Companies){matchNet.prices.Add(engine.Price(company));matchNet.stock.Add(engine.Stock(company));}
            for(int sample=matchNet.priceHistory.Count/Companies.Length;sample<engine.PublishedPriceCount;sample++)
                foreach(var company in Companies)matchNet.priceHistory.Add(engine.PublishedPrice(sample,company));
        }

        void SendPortfolios()
        {
            foreach(var conn in NetworkServer.connections.Values)
                if(conn.identity&&conn.identity.TryGetComponent<PlayerNet>(out var player)&&player.inMatch)SendPortfolio(conn,player);
        }

        void SendPortfolio(NetworkConnectionToClient conn,PlayerNet player)
        {
            var id=PlayerIdOf(conn);
            var bet=Companies.Where(c=>engine.HasBet(id,c)).Select(c=>(int)c).DefaultIfEmpty(-1).First();
            player.SendPortfolio(engine.Snapshot(id),bet,engine.BetStake(id),engine.Auction?.Secret?.GetSubmitted(id)??0);
        }

        // A participant's request; the engine answers with the message to show. Rounds must be open.
        [Server]
        public string Trade(NetworkConnectionToClient conn,Company company,int quantity,bool buy)
        {
            bool valid=engine!=null&&Enum.IsDefined(typeof(Company),company);
            int before=valid?engine.Snapshot(PlayerIdOf(conn)).Cash:0;
            string message=Request(conn,()=>quantity<=0||!valid?"잘못된 거래 요청입니다.":engine.Trade(PlayerIdOf(conn),company,quantity,buy,NetworkTime.time));
            int after=valid?engine.Snapshot(PlayerIdOf(conn)).Cash:before;
            conn.identity.GetComponent<PlayerNet>().TargetNotice(new MatchNotice{Kind=before!=after?MatchNoticeKind.Trade:MatchNoticeKind.Rejected,Company=(int)company,Quantity=quantity,Amount=Math.Abs(after-before),Buy=buy,Message=message});
            return message;
        }

        [Server]
        public string ToggleBet(NetworkConnectionToClient conn,Company company)
        {
            bool before=engine!=null&&engine.HasBet(PlayerIdOf(conn));int stake=engine!=null?engine.BetStake(PlayerIdOf(conn)):0;
            string message=Request(conn,()=>!Enum.IsDefined(typeof(Company),company)?"잘못된 베팅 요청입니다.":engine.ToggleBet(PlayerIdOf(conn),company,NetworkTime.time));
            bool after=engine!=null&&engine.HasBet(PlayerIdOf(conn));
            conn.identity.GetComponent<PlayerNet>().TargetNotice(new MatchNotice{Kind=before&&!after?MatchNoticeKind.BetCancelled:!before&&after?MatchNoticeKind.BetJoined:MatchNoticeKind.Rejected,Company=(int)company,Amount=after?engine.BetStake(PlayerIdOf(conn)):stake,Message=message});
            return message;
        }

        // Only seated players bid. The server has no hall, so which chairs are auction chairs is the client's call.
        [Server]
        public string Bid(NetworkConnectionToClient conn,int amount,bool cancel)
        {
            if(!conn.identity.TryGetComponent<PlayerNet>(out var player))return "객석에 앉아야 입찰할 수 있습니다.";
            if(!player.IsSeated){const string error="객석에 앉아야 입찰할 수 있습니다.";player.TargetNotice(new MatchNotice{Kind=MatchNoticeKind.Rejected,Message=error});return error;}
            string message=Request(conn,()=>engine.Bid(PlayerIdOf(conn),amount,cancel,NetworkTime.time));
            player.TargetNotice(new MatchNotice{Kind=message=="입찰 접수 완료"||message=="입찰 취소 완료"?MatchNoticeKind.Accepted:MatchNoticeKind.Rejected,Message=message});
            // An open bid releases the outbid player's reserved cash.
            SendPortfolios();
            return message;
        }

        string Request(NetworkConnectionToClient conn,Func<string> apply)
        {
            if(engine==null||!matchNet||matchNet.phase!=MatchNet.Phase.Playing)return "지금은 거래할 수 없습니다.";
            string message=apply();
            PublishMarket();SendPortfolio(conn,conn.identity.GetComponent<PlayerNet>());
            return message;
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
