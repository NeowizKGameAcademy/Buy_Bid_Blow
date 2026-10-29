using System;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Systems.StockGacha;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Sandbox.StockGacha;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Network
{
    public sealed partial class GameNetworkManager
    {
        [Header("Optional Stock Gacha Sandbox (unset in production)")]
        [SerializeField] TextAsset stockGachaSandboxSettings;
        [SerializeField] Vector3 stockGachaDevicePosition=new Vector3(0,0,5);
        int publishedStockGachaVersion=-1;
        string stockGachaSession;
        readonly Dictionary<int,uint> stockGachaRequests=new Dictionary<int,uint>();
        public bool StockGachaSandboxEnabled=>stockGachaSandboxSettings;
        public MatchEngine StockGachaSandboxEngine=>NetworkServer.active?engine:null;

        void RegisterStockGachaServer()
        {
            NetworkServer.RegisterHandler<StockGachaPurchase>(ReceiveStockGachaPurchase);
            NetworkServer.RegisterHandler<StockGachaStart>((conn,message)=>
            {
                if(!stockGachaSandboxSettings || MatchStarted || !CanStartMatch || !conn.identity ||
                    !IsRoomHost(conn.identity.GetComponent<PlayerNet>()))return;
                if(!BeginMatch(NetworkServer.connections.Values.Where(c=>c.identity).Select(PlayerIdOf).ToArray()))return;
                MatchStarted=true;
                foreach(var peer in NetworkServer.connections.Values)
                    if(peer.identity&&peer.identity.TryGetComponent<PlayerNet>(out var p))p.inMatch=true;
                SendPortfolios();NetworkServer.SendToAll(new MatchStarting());PublishStockGacha(true);
            });
        }
        void RegisterStockGachaClient()
        {
            StockGachaSandboxClient.Reset();
            NetworkClient.RegisterHandler<StockGachaState>(s=>
            {
                if(StockGachaSandboxClient.State.Session!=s.Session)StockGachaSandboxClient.Reset();
                StockGachaSandboxClient.State=s;
            });
            NetworkClient.RegisterHandler<StockGachaOutcome>(o=>StockGachaSandboxClient.Show(o));
            NetworkClient.RegisterHandler<StockGachaReply>(r=>
            {StockGachaSandboxClient.Reply=r.Message;StockGachaSandboxClient.Pending=false;});
        }
        void InitializeSandboxStockGacha(int seed,double startsAt)
        {
            publishedStockGachaVersion=-1;stockGachaRequests.Clear();stockGachaSession=Guid.NewGuid().ToString("N");
            if(!stockGachaSandboxSettings)return;
            engine.EnableStockGacha(JsonUtility.FromJson<StockGachaSettings>(stockGachaSandboxSettings.text),seed,startsAt);
            engine.StockGacha.Spun+=result=>
            {
                var outcome=new StockGachaOutcome{Spin=result.Spin,Company=result.Company,Cost=result.Cost,Remaining=result.Remaining,
                    Player=matchParticipants.FirstOrDefault(p=>p.Id.Equals(result.Player))?.Name??result.Player.ToString()};
                foreach(var conn in NetworkServer.connections.Values)
                    if(conn.identity&&conn.identity.GetComponent<PlayerNet>().inMatch)conn.Send(outcome);
            };
        }
        void ReceiveStockGachaPurchase(NetworkConnectionToClient conn,StockGachaPurchase request)
        {
            if(!conn.identity||!conn.identity.TryGetComponent<PlayerNet>(out var player))return;
            void Reply(string text)=>conn.Send(new StockGachaReply{Message=text,Request=request.Request});
            if(!player.inMatch||!stockGachaSandboxSettings||engine?.StockGacha==null||!matchNet||
                matchNet.phase!=BuyBidBlow.Common.MatchNet.Phase.Playing)
            {Reply("경기가 진행 중일 때만 이용할 수 있습니다.");return;}
            if(request.Session!=stockGachaSession||request.Request==0)
            {Reply("지난 경기 요청입니다. 패널을 다시 확인하세요.");return;}
            if(stockGachaRequests.TryGetValue(conn.connectionId,out uint previous)&&request.Request<=previous)
            {Reply("이미 처리한 뽑기 요청입니다.");return;}
            stockGachaRequests[conn.connectionId]=request.Request;
            if(Vector3.Distance(player.transform.position,stockGachaDevicePosition)>4.5f||player.IsSeated)
            {Reply("뽑기머신 가까이에서 이용하세요.");return;}
            var result=engine.SpinStockGacha(PlayerIdOf(conn),NetworkTime.time);
            SendPortfolio(conn,player);PublishStockGacha(true);Reply(result.Message);
        }
        void PublishStockGacha(bool force=false)
        {
            var game=engine?.StockGacha;
            if(game==null||(!force&&publishedStockGachaVersion==game.Version))return;
            publishedStockGachaVersion=game.Version;
            foreach(var conn in NetworkServer.connections.Values)
            {
                if(!conn.identity||!conn.identity.TryGetComponent<PlayerNet>(out var p)||!p.inMatch)continue;
                conn.Send(new StockGachaState{Session=stockGachaSession,Initial=game.InitialCount,Remaining=game.Remaining,
                    Price=game.Settings.Price,Blanks=game.Blanks,Straw=game.Stock(BuyBidBlow.Systems.Types.Company.Straw),
                    Timber=game.Stock(BuyBidBlow.Systems.Types.Company.Timber),Brick=game.Stock(BuyBidBlow.Systems.Types.Company.Brick),
                    OwnSpins=game.OwnSpins(PlayerIdOf(conn)),Version=game.Version,Finished=game.Finished});
            }
        }
    }
}
