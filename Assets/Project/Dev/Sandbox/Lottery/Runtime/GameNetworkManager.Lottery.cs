using System;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Systems.Lottery;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Sandbox.Lottery;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Network
{
    public sealed partial class GameNetworkManager
    {
        [Header("Optional Lottery Sandbox (unset in production)")]
        [SerializeField] TextAsset lotterySandboxSettings;
        [SerializeField] Vector3 lotteryDevicePosition = new Vector3(0,0,5);
        int publishedLotteryVersion=-1;
        LotteryOutcome lastLotteryOutcome;
        public bool LotterySandboxEnabled => lotterySandboxSettings;
        public MatchEngine LotterySandboxEngine => NetworkServer.active ? engine : null;

        void RegisterLotteryServer()
        {
            NetworkServer.RegisterHandler<LotteryPurchase>(ReceiveLotteryPurchase);
            NetworkServer.RegisterHandler<LotteryStart>((conn,message)=>
            {
                if(!lotterySandboxSettings || MatchStarted || !CanStartMatch || !conn.identity ||
                    !IsRoomHost(conn.identity.GetComponent<PlayerNet>()))return;
                var original=matchSettingsFile;
                if(message.Quick)
                {
                    var settings=JsonUtility.FromJson<MatchSettings>(original.text);
                    settings.RoundSeconds=15;settings.AuctionRoundSeconds=30;
                    settings.AuctionSlots[0].DurationSeconds=10;
                    settings.AuctionSlots[1].StartsAtSeconds=10;settings.AuctionSlots[1].DurationSeconds=10;
                    matchSettingsFile=new TextAsset(JsonUtility.ToJson(settings));
                }
                bool started;
                try { started=BeginMatch(NetworkServer.connections.Values.Where(c=>c.identity).Select(PlayerIdOf).ToArray()); }
                finally { if(matchSettingsFile!=original)Destroy(matchSettingsFile);matchSettingsFile=original; }
                if(!started)return;
                MatchStarted=true;
                foreach(var peer in NetworkServer.connections.Values)
                    if(peer.identity && peer.identity.TryGetComponent<PlayerNet>(out var player))player.inMatch=true;
                SendPortfolios();NetworkServer.SendToAll(new MatchStarting());PublishLottery(true);
            });
        }
        void RegisterLotteryClient()
        {
            LotterySandboxClient.Reset();
            NetworkClient.RegisterHandler<LotteryState>(s=>LotterySandboxClient.State=s);
            NetworkClient.RegisterHandler<LotteryOutcome>(o=>LotterySandboxClient.Show(o));
            NetworkClient.RegisterHandler<LotteryReply>(r=>LotterySandboxClient.Reply=r.Message);
        }
        void InitializeSandboxLottery(int seed,double startsAt)
        {
            publishedLotteryVersion=-1;lastLotteryOutcome=default;
            if(!lotterySandboxSettings)return;
            engine.EnableLottery(JsonUtility.FromJson<LotterySettings>(lotterySandboxSettings.text),seed,startsAt);
            engine.Lottery.Drawn+=draw=>
            {
                lastLotteryOutcome=new LotteryOutcome{Draw=draw.Draw,Number=draw.Number,Prize=draw.Prize,
                    Sold=draw.Sold,Carried=draw.Carried,Won=draw.Won,
                    Winner=draw.Won?matchParticipants.FirstOrDefault(p=>p.Id.Equals(draw.Winner))?.Name??draw.Winner.ToString():"",
                    Final=engine.Lottery.Finished};
                foreach(var conn in NetworkServer.connections.Values)
                    if(conn.identity && conn.identity.GetComponent<PlayerNet>().inMatch)conn.Send(lastLotteryOutcome);
            };
        }
        void ReceiveLotteryPurchase(NetworkConnectionToClient conn,LotteryPurchase request)
        {
            if(!conn.identity || !conn.identity.TryGetComponent<PlayerNet>(out var player))return;
            if(!player.inMatch || !lotterySandboxSettings || engine?.Lottery==null || !matchNet ||
                matchNet.phase!=BuyBidBlow.Common.MatchNet.Phase.Playing)
            { conn.Send(new LotteryReply{Message="경기가 진행 중일 때만 구매할 수 있습니다."});return; }
            if(Vector3.Distance(player.transform.position,lotteryDevicePosition)>4.5f || player.IsSeated)
            { conn.Send(new LotteryReply{Message="복권 장치 가까이에서 구매하세요."});return; }
            conn.Send(new LotteryReply{Message=engine.BuyLottery(PlayerIdOf(conn),request.Draw,request.Quantity,NetworkTime.time)});
            SendPortfolio(conn,player);PublishLottery(true);
        }
        void PublishLottery(bool force=false)
        {
            var game=engine?.Lottery;
            if(game==null || (!force&&publishedLotteryVersion==game.Version))return;
            publishedLotteryVersion=game.Version;
            foreach(var conn in NetworkServer.connections.Values)
            {
                if(!conn.identity || !conn.identity.TryGetComponent<PlayerNet>(out var player) || !player.inMatch)continue;
                conn.Send(new LotteryState{Draw=game.DrawIndex,DrawRound=game.DrawRound,Capacity=game.Settings.TicketCount,
                    Price=game.Settings.TicketPrice,Limit=game.Settings.PerPlayerLimit,Sold=game.Sold,Own=game.Tickets(PlayerIdOf(conn)),
                    Base=game.BasePrize,Carry=game.Carried,Sales=game.SalesPrize,Prize=game.Prize,
                    Deadline=engine.LotteryDeadline,Finished=game.Finished});
            }
        }
    }
}
