using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Props;
using BuyBidBlow.Systems.Auction;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Room;
using BuyBidBlow.Systems.Round;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Network
{
    // Keep the version handshake's original wire layout so older clients can receive a rejection.
    public struct LobbyJoinRequest : NetworkMessage { public int protocol; public string nickname; }
    public struct LobbyJoinResponse : NetworkMessage { public bool accepted; public string reason; public int minPlayers; }
    public struct LobbyResumeRequest : NetworkMessage { public int protocol; public string nickname, token; }
    public struct LobbyCredentials : NetworkMessage { public string token; public int maxPlayers; }
    public struct LobbyClosing : NetworkMessage { }
    public struct MatchStarting : NetworkMessage { } // Legacy identity; entry is now driven by PlayerNet.inMatch.

    public sealed class GameNetworkManager : NetworkManager
    {
        public const int ProtocolVersion = 8;
        public const string PlayScene = "SC_ExchangeFurnished";
        [Header("Match")]
        [SerializeField, Min(1)] private int minPlayers = 3;
        [SerializeField] private string menuScene = "SC_MainMenu";
        [SerializeField] private TextAsset matchSettingsFile;
        [SerializeField] private int randomSeed;
        [SerializeField] private GameObject matchNetPrefab;
        [SerializeField, Min(0)] private float prepareSeconds = 5;
        private int roundSecondsOverride;
        private bool clientInMatch;
        private int serverMinPlayers, serverMaxPlayers;
        private MatchSettings rules;
        private MatchEngine engine;
        private MatchNet matchNet;
        private readonly RoomRoster roster = new RoomRoster();
        private readonly Dictionary<int, Participant> admitted = new Dictionary<int, Participant>();
        private readonly Dictionary<string, Participant> credentials = new Dictionary<string, Participant>();
        private static readonly Company[] Companies = (Company[])Enum.GetValues(typeof(Company));

        private struct HallSeat { public string Id; public int Slots; public bool Auction; }
        private readonly Dictionary<string, HallSeat> seatsById = new Dictionary<string, HallSeat>();

        private sealed class Participant
        {
            public string Id, Token, Name;
            public int Seat, Top, Bottom, Tie;
            public bool InMatch;
            public NetworkConnectionToClient Connection;
        }

        public bool ClientInMatch => clientInMatch;
        public bool MatchStarted { get; private set; }
        public string LocalNickname { get; set; } = "";
        public int MinPlayers => !NetworkServer.active && serverMinPlayers > 0 ? serverMinPlayers : minPlayers;
        public int MaxPlayers => !NetworkServer.active && serverMaxPlayers > 0 ? serverMaxPlayers : Math.Min(RoomRoster.Capacity, rules?.MaxPlayerCount ?? RoomRoster.Capacity);
        public event Action ClientConnected, AdmissionApproved, ClientDisconnected, LocalPlayerReady, ClientMatchStarted, ClientMatchEnded;
        public event Action<string> AdmissionRejected, ClientError;

        private PlayerNet[] Players => NetworkServer.connections.Values.Where(c => c.identity)
            .Select(c => c.identity.GetComponent<PlayerNet>()).Where(p => p).ToArray();
        public bool CanStartMatch => NetworkServer.active && !MatchStarted && Players.Length >= minPlayers &&
            Players.Length == roster.Count && Players.All(p => p.ready);

        public override void Awake()
        {
            base.Awake();
            var args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            if (ushort.TryParse(Arg("-port"), out var port) && transport is kcp2k.KcpTransport kcp) kcp.Port = port;
            if (int.TryParse(Arg("-minPlayers"), out var min) && min > 0) minPlayers = min;
#if UNITY_EDITOR
            // Editor playtests can start alone; builds keep the scene's minimum unless -minPlayers says otherwise.
            else minPlayers = 1;
#endif
            if (int.TryParse(Arg("-roundSeconds"), out var seconds) && seconds > 0) roundSecondsOverride = seconds;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            roster.Clear(); admitted.Clear(); credentials.Clear(); EndMatch();
            if (!matchSettingsFile) throw new InvalidOperationException("GameNetworkManager needs MatchSettings JSON.");
            rules = JsonUtility.FromJson<MatchSettings>(matchSettingsFile.text); rules.Validate();
            if (minPlayers > MaxPlayers) throw new InvalidOperationException("Minimum start count exceeds the supported room capacity.");
            CaptureHallSeats();
            NetworkServer.RegisterHandler<LobbyJoinRequest>(ReceiveJoin);
            NetworkServer.RegisterHandler<LobbyResumeRequest>(ReceiveResume);
        }

        public override void OnStopServer()
        {
            EndMatch(); roster.Clear(); admitted.Clear(); credentials.Clear(); base.OnStopServer();
        }

        // The host reads the seats of the loaded hall.
        private void CaptureHallSeats()
        {
            var loaded = FindObjectsByType<Seat>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            seatsById.Clear();
            foreach (var seat in loaded.Select(s => new HallSeat { Id = s.Id, Slots = s.sittingPoints?.Length ?? 0, Auction = s.auction }))
            {
                if (string.IsNullOrEmpty(seat.Id) || seat.Slots < 1 || seatsById.ContainsKey(seat.Id))
                    throw new InvalidOperationException("The hall seat catalog has missing/duplicate IDs or no slots.");
                seatsById.Add(seat.Id, seat);
            }
            if (seatsById.Count == 0) throw new InvalidOperationException("The loaded hall has no seats.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.RegisterHandler<LobbyJoinResponse>(ReceiveResponse);
            NetworkClient.RegisterHandler<LobbyCredentials>(message =>
            {
                PlayerPrefs.SetString(CredentialKey, message.token); serverMaxPlayers = message.maxPlayers;
                PlayerPrefs.Save();
            });
            NetworkClient.RegisterHandler<LobbyClosing>(_ => AdmissionRejected?.Invoke("서버가 방을 닫았습니다."));
            if (matchNetPrefab) NetworkClient.RegisterPrefab(matchNetPrefab);
        }

        private string CredentialKey => "BuyBidBlow.Reconnect." + networkAddress + ":" +
            (transport is kcp2k.KcpTransport kcp ? kcp.Port : 0) + ":" + LocalNickname;

        public override void OnClientConnect()
        {
            ClientConnected?.Invoke();
            string token = PlayerPrefs.GetString(CredentialKey, "");
            if (string.IsNullOrEmpty(token)) NetworkClient.Send(new LobbyJoinRequest { protocol = ProtocolVersion, nickname = LocalNickname });
            else NetworkClient.Send(new LobbyResumeRequest { protocol = ProtocolVersion, nickname = LocalNickname, token = token });
        }

        public void SetClientMatchState(bool playing)
        {
            if (clientInMatch == playing) return;
            clientInMatch = playing;
            if (playing) ClientMatchStarted?.Invoke(); else ClientMatchEnded?.Invoke();
        }
        public void NotifyLocalPlayerReady() => LocalPlayerReady?.Invoke();

        [Server]
        public bool IsRoomHost(PlayerNet player)
        {
            var players = Players;
            return player && players.Length > 0 && player.seat == players.Min(p => p.seat);
        }

        [Server]
        public void RequestStartMatch(PlayerNet requester)
        {
            if (!IsRoomHost(requester) || !CanStartMatch)
            {
                requester.Reply(new RequestResult(RequestKind.StartMatch, false, "방장과 모든 참가자가 준비된 뒤 시작할 수 있습니다."));
                return;
            }
            var players = Players;
            if (!BeginMatch(players.Select(p => new PlayerId(p.playerId)).ToList()))
            {
                requester.Reply(new RequestResult(RequestKind.StartMatch, false, "경기 설정을 확인하세요."));
                return;
            }
            MatchStarted = true;
            foreach (var player in players)
            {
                admitted[player.connectionToClient.connectionId].InMatch = true;
                player.inMatch = true;
            }
            SendPortfolios();
        }

        private bool BeginMatch(IReadOnlyList<PlayerId> players)
        {
            if (!matchNetPrefab) { Debug.LogError("Missing MatchNet prefab.", this); return false; }
            rules = JsonUtility.FromJson<MatchSettings>(matchSettingsFile.text); rules.Validate();
            if (players.Count > rules.MaxPlayerCount) return false;
            Debug.Log($"[Server] Match rules: {rules.RoundCount} rounds, ordinary {rules.RoundSeconds}s, auction {rules.AuctionRoundSeconds}s, clock override {roundSecondsOverride}s");
            double startsAt = NetworkTime.time + prepareSeconds;
            engine = new MatchEngine(rules, randomSeed != 0 ? randomSeed : Environment.TickCount, players, startsAt, roundSecondsOverride);
            engine.PublicResolved += PublicResolved;
            engine.SecretResolved += SecretResolved;
            engine.BetSettled += BetSettled;
            var go = Instantiate(matchNetPrefab); matchNet = go.GetComponent<MatchNet>();
            matchNet.totalRounds = rules.RoundCount; matchNet.minPrice = rules.MinPrice;
            matchNet.phase = MatchNet.Phase.Preparing; matchNet.firstRoundStartsAt = startsAt; matchNet.roundClosesAt = engine.Deadline;
            go.GetComponent<MatchTimeline>().Publish(MatchSchedule.Build(rules, startsAt - prepareSeconds, startsAt, roundSecondsOverride));
            PublishMarket(); PublishAuction(NetworkTime.time);
            NetworkServer.Spawn(go);
            return true;
        }

        private void EndMatch()
        {
            if (engine != null) { engine.PublicResolved -= PublicResolved; engine.SecretResolved -= SecretResolved; engine.BetSettled -= BetSettled; }
            foreach (var player in Players)
            {
                player.StandUp(); player.ready = false; player.inMatch = false;
                player.GetComponent<PlayerAssetsNet>().ClearState();
                player.TargetClearResults();
            }
            foreach (var participant in credentials.Values) participant.InMatch = false;
            foreach (var token in credentials.Where(p => p.Value.Connection == null).Select(p => p.Key).ToArray()) credentials.Remove(token);
            if (matchNet && NetworkServer.active) NetworkServer.Destroy(matchNet.gameObject);
            matchNet = null; engine = null; MatchStarted = false;
        }

        [Server]
        public RequestResult ReturnToLobby(PlayerNet requester)
        {
            if (engine == null || !engine.Complete || !IsRoomHost(requester))
                return new RequestResult(RequestKind.ReturnToLobby, false, "경기 종료 후 방장이 로비로 돌아갈 수 있습니다.");
            EndMatch();
            return new RequestResult(RequestKind.ReturnToLobby, true, "로비로 돌아왔습니다.");
        }

        public static PlayerId PlayerIdOf(NetworkConnectionToClient conn)
        {
            if (conn != null && conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var player)) return new PlayerId(player.playerId);
            return new PlayerId("");
        }

        public override void Update()
        {
            base.Update();
            if (!NetworkServer.active || engine == null || !matchNet || matchNet.phase == MatchNet.Phase.Finished) return;
            double now = NetworkTime.time;
            if (now < matchNet.firstRoundStartsAt) return;
            if (matchNet.phase == MatchNet.Phase.Preparing) { matchNet.phase = MatchNet.Phase.Playing; matchNet.round = engine.Round; }
            int round = engine.Round;
            engine.Tick(now); PublishAuction(now);
            if (engine.Round == round && !engine.Complete) return;
            PublishMarket(); matchNet.lastChanges.Clear();
            foreach (var company in Companies) if (engine.LastChange(company) != 0) matchNet.lastChanges.Add(new PriceChange(company, engine.LastChange(company)));
            matchNet.lastClosedRound = engine.Complete ? rules.RoundCount : engine.Round - 1;
            if (engine.Complete) matchNet.phase = MatchNet.Phase.Finished;
            else { matchNet.roundClosesAt = engine.Deadline; matchNet.round = engine.Round; }
            SendPortfolios();
            if (engine.Complete) PublishResults();
        }

        // Each participant gets the final table with the full breakdown of every row.
        private void PublishResults()
        {
            var standings = engine.GetStandings();
            foreach (var recipient in Players.Where(p => p.inMatch))
                recipient.TargetResults(standings.Select(row =>
                {
                    var id = new PlayerId(row.PlayerId); var snapshot = engine.Snapshot(id);
                    return new FinalStanding { Rank = row.Rank, Total = row.TotalAssets, Nickname = NicknameOf(id), Own = row.PlayerId == recipient.playerId,
                        Cash = snapshot.Cash, StockValue = row.TotalAssets - snapshot.Cash, Shares = Companies.Sum(c => snapshot.GetShares(c)),
                        CompanyShares = Companies.Select(c => snapshot.GetShares(c)).ToArray(), FinalPrices = Companies.Select(c => engine.Price(c)).ToArray(),
                        AssetHistory = engine.AssetHistory(id) };
                }).ToArray(), rules.RoundCount);
        }

        private Participant ParticipantOf(PlayerId id) => credentials.Values.FirstOrDefault(p => p.Id == id.Value);
        private string NicknameOf(PlayerId id) => ParticipantOf(id)?.Name ?? "";
        private void SendPortfolio(PlayerId id)
        {
            var participant = ParticipantOf(id);
            if (participant?.Connection?.identity && participant.Connection.identity.TryGetComponent<PlayerNet>(out var player)) SendPortfolio(participant.Connection, player);
        }
        private PlayerNet NetOf(PlayerId id)
        {
            var participant = ParticipantOf(id);
            return participant?.Connection?.identity ? participant.Connection.identity.GetComponent<PlayerNet>() : null;
        }
        private void PublicResolved(AuctionResult result)
        {
            foreach (var player in Players.Where(p => p.inMatch))
                player.TargetAuctionOutcome(new AuctionOutcome { Sold = result.Sold, Won = result.Sold && result.Winner.Value == player.playerId,
                    Winner = result.Sold ? NicknameOf(result.Winner) : "", Amount = result.Price });
            if (result.Sold) SendPortfolio(result.Winner);
        }
        // Only a bidder learns their own outcome; losers never get the winner or the winning price.
        private void SecretResolved(SecretResult result)
        {
            foreach (var id in result.Players)
            {
                var player = NetOf(id);
                if (player) player.TargetAuctionOutcome(new AuctionOutcome { Secret = true, Sold = result.Result.Sold,
                    Won = result.Result.Sold && result.Result.Winner.Equals(id), Amount = engine.Auction.Secret.GetSubmitted(id) });
                SendPortfolio(id);
            }
        }
        private void BetSettled(PlayerId id, Company company, int stake, int payout)
        {
            var player = NetOf(id);
            if (player) player.TargetNotice(new MatchNotice { Kind = MatchNoticeKind.BetSettled, Company = (int)company, Amount = stake, Payout = payout, Won = payout > 0 });
        }

        private void PublishAuction(double now)
        {
            var auction = engine.Complete ? null : engine.Auction;
            bool secret = auction?.Secret != null && now < auction.Secret.Deadline;
            bool open = !secret && auction?.Open != null && now < auction.Open.Deadline;
            var stage = secret ? MatchNet.AuctionStage.Secret : open ? MatchNet.AuctionStage.Open : MatchNet.AuctionStage.None;
            if (stage != matchNet.auctionStage) SendPortfolios();
            matchNet.auctionStage = stage;
            if (secret || open)
            {
                var lot = secret ? auction.Secret.Lot : auction.Open.Lot;
                matchNet.auctionClosesAt = secret ? auction.Secret.Deadline : auction.Open.Deadline;
                matchNet.auctionLot = new MatchNet.PublicLot { firstRound = lot.First.Round, firstA = (int)lot.First.CompanyA, firstB = (int)lot.First.CompanyB,
                    secondRound = lot.Second.Round, secondA = (int)lot.Second.CompanyA, secondB = (int)lot.Second.CompanyB };
            }
            bool bid = open && auction.Open.HasBid;
            matchNet.highestBid = bid ? auction.Open.HighestBid : 0;
            matchNet.highestBidderId = bid ? auction.Open.HighestBidder.Value : "";
            matchNet.highestBidder = bid ? NicknameOf(auction.Open.HighestBidder) : "";
            matchNet.nextAuctionRound = rules.AuctionRounds.Where(r => r > engine.Round).DefaultIfEmpty(0).Min();
        }

        private void PublishMarket()
        {
            foreach (var company in Companies)
            {
                int i = (int)company;
                if (matchNet.prices.Count <= i) matchNet.prices.Add(engine.Price(company)); else matchNet.prices[i] = engine.Price(company);
                if (matchNet.stock.Count <= i) matchNet.stock.Add(engine.Stock(company)); else matchNet.stock[i] = engine.Stock(company);
            }
            for (int sample = matchNet.priceHistory.Count / Companies.Length; sample < engine.PublishedPriceCount; sample++)
                foreach (var company in Companies) matchNet.priceHistory.Add(engine.PublishedPrice(sample, company));
        }
        private void SendPortfolios()
        {
            if (engine == null) return;
            foreach (var player in Players) if (player.inMatch) SendPortfolio(player.connectionToClient, player);
        }
        private void SendPortfolio(NetworkConnectionToClient conn, PlayerNet player)
        {
            var id = PlayerIdOf(conn);
            int bet = Companies.Where(c => engine.HasBet(id, c)).Select(c => (int)c).DefaultIfEmpty(-1).First();
            player.SendPortfolio(engine.Snapshot(id), bet, engine.BetStake(id), engine.Auction?.Secret?.GetSubmitted(id) ?? 0, engine.InformationOf(id));
        }

        [Server]
        public RequestResult Trade(NetworkConnectionToClient conn, Company company, int quantity, bool buy)
        {
            int before = engine != null ? engine.Snapshot(PlayerIdOf(conn)).Cash : 0;
            var result = Request(conn, buy ? RequestKind.Buy : RequestKind.Sell, () => engine.Trade(PlayerIdOf(conn), company, quantity, buy, NetworkTime.time));
            result.Company = company;
            int after = result.Succeeded ? engine.Snapshot(PlayerIdOf(conn)).Cash : before;
            Notify(conn, new MatchNotice { Kind = result.Succeeded ? MatchNoticeKind.Trade : MatchNoticeKind.Rejected, Company = (int)company,
                Quantity = quantity, Amount = Math.Abs(after - before), Buy = buy, Message = result.Message });
            return result;
        }
        [Server]
        public RequestResult ToggleBet(NetworkConnectionToClient conn, Company company)
        {
            var id = PlayerIdOf(conn);
            bool before = engine != null && engine.HasBet(id); int stake = engine != null ? engine.BetStake(id) : 0;
            var result = Request(conn, RequestKind.Bet, () => engine.ToggleBet(id, company, NetworkTime.time));
            result.Company = company;
            bool after = engine != null && engine.HasBet(id);
            Notify(conn, new MatchNotice { Kind = before && !after ? MatchNoticeKind.BetCancelled : !before && after ? MatchNoticeKind.BetJoined : MatchNoticeKind.Rejected,
                Company = (int)company, Amount = after ? engine.BetStake(id) : stake, Message = result.Message });
            return result;
        }
        [Server]
        public RequestResult Bid(NetworkConnectionToClient conn, int amount, bool cancel)
        {
            if (conn?.identity == null || !conn.identity.TryGetComponent<PlayerNet>(out var player) || !player.IsSeated ||
                !seatsById.TryGetValue(player.hallSeat, out var seat) || !seat.Auction || player.hallSeatSlot < 0 || player.hallSeatSlot >= seat.Slots)
            {
                var refused = new RequestResult(RequestKind.Bid, false, "객석에 앉아야 입찰할 수 있습니다.");
                Notify(conn, new MatchNotice { Kind = MatchNoticeKind.Rejected, Message = refused.Message });
                return refused;
            }
            var previous = engine?.Auction?.Open;
            PlayerId outbid = previous != null && previous.HasBid ? previous.HighestBidder : new PlayerId("");
            var result = Request(conn, RequestKind.Bid, () => engine.Bid(PlayerIdOf(conn), amount, cancel, NetworkTime.time));
            if (result.Succeeded)
            {
                if (engine.Auction?.Secret == null)
                {
                    if (!string.IsNullOrEmpty(outbid.Value) && !outbid.Equals(PlayerIdOf(conn))) SendPortfolio(outbid);
                    player.ShowBid();
                }
                PublishAuction(NetworkTime.time);
            }
            Notify(conn, new MatchNotice { Kind = result.Succeeded ? MatchNoticeKind.Accepted : MatchNoticeKind.Rejected, Message = result.Message });
            return result;
        }
        private static void Notify(NetworkConnectionToClient conn, MatchNotice notice)
        {
            if (conn?.identity && conn.identity.TryGetComponent<PlayerNet>(out var player)) player.TargetNotice(notice);
        }
        private RequestResult Request(NetworkConnectionToClient conn, RequestKind kind, Func<RequestResult> apply)
        {
            if (engine == null || !matchNet || matchNet.phase != MatchNet.Phase.Playing || conn?.identity == null ||
                !admitted.TryGetValue(conn.connectionId, out var participant) || !participant.InMatch)
                return new RequestResult(kind, false, "지금은 요청을 처리할 수 없습니다.");
            var result = apply();
            if (result.Succeeded)
            {
                if (kind == RequestKind.Buy || kind == RequestKind.Sell) PublishMarket();
                SendPortfolio(conn, conn.identity.GetComponent<PlayerNet>());
            }
            return result;
        }
        [Server]
        public RequestResult Sit(PlayerNet player, string id, int slot)
        {
            if (!MatchStarted || engine == null || engine.Complete || !player.inMatch || string.IsNullOrEmpty(id) ||
                !seatsById.TryGetValue(id, out var seat) || slot < 0 || slot >= seat.Slots)
                return new RequestResult(RequestKind.Seat, false, "유효한 좌석이 아닙니다.");
            if (Players.Any(p => p != player && p.hallSeat == id && p.hallSeatSlot == slot))
                return new RequestResult(RequestKind.Seat, false, "다른 플레이어가 먼저 앉았습니다.");
            player.AssignSeat(id, slot);
            return new RequestResult(RequestKind.Seat, true, "");
        }

        public override void OnClientDisconnect()
        {
            bool wasPlaying = clientInMatch;
            ClientDisconnected?.Invoke(); base.OnClientDisconnect(); serverMinPlayers = serverMaxPlayers = 0;
            SetClientMatchState(false);
            if (wasPlaying) { SceneManager.LoadScene(menuScene); Destroy(gameObject); }
        }
        public override void OnClientError(TransportError error, string reason) =>
            ClientError?.Invoke(error == TransportError.Timeout ? "접속 시간이 초과되었습니다. 서버 상태를 확인하세요." : "네트워크 연결 오류가 발생했습니다. 주소·포트와 서버 상태를 확인하세요.");
        public override void OnServerConnect(NetworkConnectionToClient conn) { base.OnServerConnect(conn); StartCoroutine(AdmissionTimeout(conn)); }
        private IEnumerator AdmissionTimeout(NetworkConnectionToClient conn)
        {
            yield return new WaitForSecondsRealtime(10);
            if (NetworkServer.active && NetworkServer.connections.TryGetValue(conn.connectionId, out var current) && current == conn && !conn.identity)
            { roster.Release(conn.connectionId); Reject(conn, "입장 처리 시간이 초과되었습니다."); }
        }
        private void ReceiveJoin(NetworkConnectionToClient conn, LobbyJoinRequest message)
        {
            if (conn.identity || admitted.ContainsKey(conn.connectionId)) return;
            if (message.protocol != ProtocolVersion) { Reject(conn, "게임 버전이 다릅니다. 같은 버전으로 실행하세요."); return; }
            if (MatchStarted && engine != null && engine.Complete && admitted.Count == 0) EndMatch();
            if (MatchStarted) { Reject(conn, "이미 경기가 시작된 방입니다."); return; }
            if (roster.Count >= MaxPlayers) { Reject(conn, "방이 가득 찼습니다."); return; }
            if (!roster.TryReserve(conn.connectionId, message.nickname, out var error)) { Reject(conn, error); return; }
            roster.TryGetName(conn.connectionId, out var name);
            var used = admitted.Values.Select(p => p.Seat).ToArray();
            var participant = new Participant { Id = Guid.NewGuid().ToString("N"), Token = Guid.NewGuid().ToString("N"),
                Name = name, Seat = Enumerable.Range(0, MaxPlayers).First(i => !used.Contains(i)) };
            credentials.Add(participant.Token, participant); Approve(conn, participant);
        }
        private void ReceiveResume(NetworkConnectionToClient conn, LobbyResumeRequest message)
        {
            if (conn.identity || admitted.ContainsKey(conn.connectionId)) return;
            if (message.protocol != ProtocolVersion) { Reject(conn, "게임 버전이 다릅니다. 같은 버전으로 실행하세요."); return; }
            if (string.IsNullOrEmpty(message.token) || message.token.Length != 32 || !credentials.TryGetValue(message.token, out var participant))
            { ReceiveJoin(conn, new LobbyJoinRequest { protocol = message.protocol, nickname = message.nickname }); return; }
            if (participant.Connection != null) { Reject(conn, "이미 접속한 플레이어입니다."); return; }
            if (!roster.TryReserve(conn.connectionId, participant.Name, out var error)) { Reject(conn, error); return; }
            Approve(conn, participant);
        }
        private void Approve(NetworkConnectionToClient conn, Participant participant)
        {
            participant.Connection = conn; admitted.Add(conn.connectionId, participant);
            conn.Send(new LobbyJoinResponse { accepted = true, reason = "", minPlayers = minPlayers });
            conn.Send(new LobbyCredentials { token = participant.Token, maxPlayers = MaxPlayers });
        }
        private void ReceiveResponse(LobbyJoinResponse message)
        {
            if (!message.accepted) { AdmissionRejected?.Invoke(message.reason); return; }
            serverMinPlayers = message.minPlayers;
            if (NetworkClient.localPlayer || NetworkClient.ready) return;
            AdmissionApproved?.Invoke(); NetworkClient.Ready(); NetworkClient.AddPlayer();
        }
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            if (!roster.Contains(conn.connectionId) || !admitted.ContainsKey(conn.connectionId)) { Reject(conn, "입장 승인이 필요합니다."); return; }
            base.OnServerReady(conn);
        }
        private void Reject(NetworkConnectionToClient conn, string reason)
        {
            conn.Send(new LobbyJoinResponse { accepted = false, reason = reason }); StartCoroutine(DisconnectRejected(conn));
        }
        private IEnumerator DisconnectRejected(NetworkConnectionToClient conn)
        {
            yield return new WaitForSecondsRealtime(.25f);
            if (NetworkServer.active && NetworkServer.connections.TryGetValue(conn.connectionId, out var current) && current == conn) conn.Disconnect();
        }
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (!roster.Contains(conn.connectionId) || !admitted.TryGetValue(conn.connectionId, out var participant)) { Reject(conn, "입장 승인이 필요합니다."); return; }
            var go = Instantiate(playerPrefab); var player = go.GetComponent<PlayerNet>();
            player.playerId = participant.Id; player.nickname = participant.Name; player.seat = participant.Seat;
            player.top = participant.Top; player.bottom = participant.Bottom; player.tie = participant.Tie;
            player.inMatch = MatchStarted && participant.InMatch;
            NetworkServer.AddPlayerForConnection(conn, go);
            if (player.inMatch)
                SendPortfolio(conn, player);
        }
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (admitted.TryGetValue(conn.connectionId, out var participant))
            {
                if (conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var player))
                { participant.Top = player.top; participant.Bottom = player.bottom; participant.Tie = player.tie; }
                participant.Connection = null; admitted.Remove(conn.connectionId);
                if (!participant.InMatch) credentials.Remove(participant.Token);
            }
            roster.Release(conn.connectionId); base.OnServerDisconnect(conn);
            // A running match keeps participants, bids and earned information for authenticated resumption.
        }
        public void BroadcastClosing() { if (NetworkServer.active) NetworkServer.SendToAll(new LobbyClosing()); }
    }
}
