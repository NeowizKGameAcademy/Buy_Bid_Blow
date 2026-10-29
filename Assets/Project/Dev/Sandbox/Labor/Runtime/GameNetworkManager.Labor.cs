using System;
using System.Collections.Generic;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Sandbox.Labor;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;
namespace BuyBidBlow.Network
{
    public sealed partial class GameNetworkManager
    {
        readonly uint[] laborOwners=new uint[LaborRoom.CargoCount];
        readonly bool[] laborAvailable=new bool[LaborRoom.CargoCount];
        System.Random laborRandom;
        readonly double[] laborPickedAt=new double[LaborRoom.CargoCount];
        readonly Dictionary<int,double> laborRequestAt=new Dictionary<int,double>();
        MatchEngine laborEngine;int laborRound;string laborSession;double laborPublishAt;
        void RegisterLaborServer()=>NetworkServer.RegisterHandler<LaborRequest>(ReceiveLabor);
        void RegisterLaborClient()=>NetworkClient.RegisterHandler<LaborState>(state=>{if(LaborRoom.Current)LaborRoom.Current.Receive(state);});
        void TickLabor()
        {
            if(!LaborRoom.Current||engine==null)return;
            if(laborEngine!=engine||laborRound!=engine.Round)
            {
                if(laborEngine!=engine)laborRandom=new System.Random(Guid.NewGuid().GetHashCode());
                laborEngine=engine;laborRound=engine.Round;laborSession=Guid.NewGuid().ToString("N");
                Array.Clear(laborOwners,0,laborOwners.Length);Array.Clear(laborAvailable,0,laborAvailable.Length);laborRequestAt.Clear();laborPublishAt=0;
                for(int company=0;company<3;company++)
                    for(int i=0,count=laborRandom.Next(1,LaborRoom.BlocksPerCompany+1);i<count;i++)laborAvailable[company*LaborRoom.BlocksPerCompany+i]=true;
                for(int i=0,count=laborRandom.Next(1,3);i<count;i++)laborAvailable[LaborRoom.BombStart+i]=true;
            }
            for(int i=0;i<laborOwners.Length;i++)
                if(laborOwners[i]!=0&&(!NetworkServer.spawned.ContainsKey(laborOwners[i])||NetworkTime.time-laborPickedAt[i]>45||engine.Complete))
                {laborOwners[i]=0;}
            if(NetworkTime.time>=laborPublishAt){laborPublishAt=NetworkTime.time+.2;PublishLabor();}
        }
        void PublishLabor(NetworkConnectionToClient only=null,string message=null,bool move=false,Vector3 destination=default)
        {
            if(engine==null)return;
            foreach(var conn in NetworkServer.connections.Values)
            {
                if(only!=null&&only!=conn)continue;
                if(!conn.identity||!conn.identity.TryGetComponent<PlayerNet>(out var player)||!player.inMatch)continue;
                var available=(bool[])laborAvailable.Clone();
                conn.Send(new LaborState{Session=laborSession,Owners=(uint[])laborOwners.Clone(),Available=available,
                    Adjustments=new[]{engine.RoundPriceAdjustment(Company.Straw),engine.RoundPriceAdjustment(Company.Timber),engine.RoundPriceAdjustment(Company.Brick)},
                    Paid=engine.LaborCount(PlayerIdOf(conn),false),Bombs=engine.LaborCount(PlayerIdOf(conn),true),Message=message,Move=move,Teleport=destination});
            }
        }
        void ReceiveLabor(NetworkConnectionToClient conn,LaborRequest request)
        {
            var room=LaborRoom.Current;
            if(!room||engine==null||!matchNet||matchNet.phase!=MatchNet.Phase.Playing||engine.Complete||!conn.identity||
                !conn.identity.TryGetComponent<PlayerNet>(out var player)||!player.inMatch||player.IsSeated)return;
            TickLabor();
            if(request.Session!=laborSession)return;
            double now=NetworkTime.time;
            if(laborRequestAt.TryGetValue(conn.connectionId,out double last)&&now-last<.2)return;
            laborRequestAt[conn.connectionId]=now;
            int held=Array.IndexOf(laborOwners,conn.identity.netId);int id=request.Target;
            bool Near(Vector3 target,float distance=3.5f)=>Vector3.Distance(player.transform.position,target)<=distance;
            void Reply(string text)=>PublishLabor(conn,text);
            if(id==100||id==101)
            {
                if(!Near(id==100?room.doorPosition:room.origin+new Vector3(0,1.5f,-9),5))return;
                if(held>=0){laborOwners[held]=0;}
                var destination=id==100?room.Entry:room.hallExit;
                player.transform.position=destination;
                PublishLabor(conn,"",true,destination);PublishLabor();return;
            }
            if(id==300){if(held>=0){laborOwners[held]=0;PublishLabor();}return;}
            if(id>=0&&id<laborOwners.Length)
            {
                if(held>=0){Reply("한 번에 하나만 들 수 있습니다. G로 반납하세요.");return;}
                if(!Near(room.spawns[id])||laborOwners[id]!=0||!laborAvailable[id])return;
                if(engine.LaborCount(PlayerIdOf(conn),id>=LaborRoom.BombStart)>=(id>=LaborRoom.BombStart?1:5)){Reply("이번 라운드 운반 횟수를 모두 사용했습니다.");return;}
                laborOwners[id]=conn.identity.netId;laborPickedAt[id]=now;PublishLabor();return;
            }
            if(id<200||id>202||held<0||!Near(room.Bin(id-200)))return;
            int company=id-200;bool bomb=held>=LaborRoom.BombStart;
            if(!bomb&&held/LaborRoom.BlocksPerCompany!=company){Reply("블록과 같은 회사의 통에 넣으세요.");return;}
            if(!engine.DeliverLabor(PlayerIdOf(conn),(Company)company,bomb,now,out int wage)){Reply("운반 한도 또는 라운드 마감으로 투입할 수 없습니다.");return;}
            laborOwners[held]=0;laborAvailable[held]=false;
            SendPortfolio(conn,player);PublishLabor();Reply(bomb?"폭탄 투입 · 급여 $0 / 주가 보정 -$3":$"운반 완료 · 급여 +${wage} / 주가 보정 +$1");
        }
    }
}
