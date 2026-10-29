using System;
using System.Collections.Generic;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.UI.Notebook;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Sandbox.StockGacha
{
    public sealed class StockGachaSandboxPanel : MonoBehaviour
    {
        public Font font;
        public Transform device;
        GameObject owner;
        UnityEngine.InputSystem.PlayerInput input;
        PlayerController controller;
        PlayerMove movement;
        NetworkNotebookController notebook;
        readonly List<BuyBidBlow.UI.HUD.CursorController> suspendedCursors=new List<BuyBidBlow.UI.HUD.CursorController>();
        bool notebookWasEnabled,inputWasActive,controllerWasEnabled;
        CursorLockMode previousLock;
        bool previousVisible;
        int openedFrame;
        string nickname="Tester",address="127.0.0.1";
        GUIStyle title,body,small,button,field;
        public bool IsOpen=>owner;
        public bool CanSpin
        {
            get
            {
                var s=StockGachaSandboxClient.State;
                return IsOpen&&NetworkClient.isConnected&&MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing&&
                    s.Remaining>0&&s.Price>0&&!StockGachaSandboxClient.Pending&&(PlayerNet.LocalPortfolio?.Available??0)>=s.Price;
            }
        }
        public bool Open(GameObject player)
        {
            if(IsOpen||!MatchNet.Current||MatchNet.Current.phase!=MatchNet.Phase.Playing)return false;
            owner=player;input=owner.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            controller=owner.GetComponent<PlayerController>();movement=owner.GetComponent<PlayerMove>();
            notebook=owner.GetComponentInChildren<NetworkNotebookController>(true);
            notebookWasEnabled=notebook&&notebook.enabled;if(notebook)notebook.enabled=false;
            inputWasActive=input&&input.inputIsActive;controllerWasEnabled=controller&&controller.enabled;
            if(movement){movement.SetMoveInput(Vector2.zero);movement.SetJumpHeld(false);}
            if(input)input.DeactivateInput();if(controller)controller.enabled=false;
            previousLock=Cursor.lockState;previousVisible=Cursor.visible;
            suspendedCursors.Clear();
            foreach(var cursor in FindObjectsByType<BuyBidBlow.UI.HUD.CursorController>(FindObjectsSortMode.None))
                if(cursor.isActiveAndEnabled){suspendedCursors.Add(cursor);cursor.enabled=false;}
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            openedFrame=Time.frameCount;StockGachaSandboxClient.Reply="";return true;
        }
        public void Close()
        {
            bool playing=owner&&NetworkClient.active&&owner.GetComponent<PlayerNet>().inMatch&&MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing;
            if(notebook)notebook.enabled=notebookWasEnabled;
            if(controller)controller.enabled=controllerWasEnabled&&playing;
            if(input&&inputWasActive&&playing)input.ActivateInput();
            if(owner){Cursor.lockState=playing?previousLock:CursorLockMode.None;Cursor.visible=playing?previousVisible:true;}
            owner=null;
            foreach(var cursor in suspendedCursors)if(cursor)cursor.enabled=true;
            suspendedCursors.Clear();
        }
        void OnDisable()=>Close();
        void Start(){StockGachaSandboxClient.Reset();}
        void Update()
        {
            if(!IsOpen)return;
            if(!NetworkClient.active||!MatchNet.Current||MatchNet.Current.phase!=MatchNet.Phase.Playing||
                NetworkNotebookController.BlocksInput||!device||Vector3.Distance(owner.transform.position,device.position)>4.5f)
            {Close();return;}
            var keyboard=Keyboard.current;
            if(keyboard==null||Time.frameCount==openedFrame)return;
            if(keyboard.qKey.wasPressedThisFrame||keyboard.escapeKey.wasPressedThisFrame){Close();return;}
            if((keyboard.eKey.wasPressedThisFrame||keyboard.enterKey.wasPressedThisFrame)&&CanSpin)Spin();
        }
        public void Spin()
        {
            if(!CanSpin)return;
            StockGachaSandboxClient.Pending=true;StockGachaSandboxClient.Reply="뽑는 중…";
            NetworkClient.Send(new StockGachaPurchase{Session=StockGachaSandboxClient.State.Session,Request=StockGachaSandboxClient.NextRequest()});
        }
        void Styles()
        {
            if(body!=null)return;
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=22,wordWrap=true};
            body.normal.textColor=new Color(.96f,.94f,.82f);
            title=new GUIStyle(body){fontSize=32,fontStyle=FontStyle.Bold};
            small=new GUIStyle(body){fontSize=17};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=21,fixedHeight=46};
            field=new GUIStyle(GUI.skin.textField){font=font,fontSize=21,fixedHeight=36};
        }
        void OnGUI()
        {
            Styles();GUI.depth=-100;
            var oldMatrix=GUI.matrix;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/1000f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-1000*scale)/2),Quaternion.identity,Vector3.one*scale);
            var room=NetworkManager.singleton as GameNetworkManager;
            if(!room||!room.StockGachaSandboxEnabled){GUI.matrix=oldMatrix;return;}
            bool playing=room.ClientInMatch&&MatchNet.Current;
            if(!playing)DrawConnection(room);
            else
            {
                var s=StockGachaSandboxClient.State;
                var trade=NetworkClient.localPlayer?NetworkClient.localPlayer.GetComponentInChildren<BuyBidBlow.UI.Market.TradePanelController>(true):null;
                if(!trade||!trade.IsOpen)
                {
                    Box(new Rect(1080,115,500,150));
                    GUI.Label(new Rect(1100,128,460,110),$"주식 뽑기머신 · 1회 ${s.Price}\n남은 캡슐 {s.Remaining}/{s.Initial}\n주식 당첨 확률 {Chance(s.Straw+s.Timber+s.Brick,s.Remaining):0.#}%",body);
                }
                if(IsOpen)DrawMachine();
            }
            if(StockGachaSandboxClient.Outcome.Spin>0&&Time.unscaledTime<StockGachaSandboxClient.NoticeUntil)
            {
                Box(new Rect(350,25,900,80));
                GUI.Label(new Rect(370,32,860,66),OutcomeText(),body);
            }
            GUI.matrix=oldMatrix;
        }
        void DrawConnection(GameNetworkManager room)
        {
            Box(new Rect(35,35,660,610));GUILayout.BeginArea(new Rect(60,55,610,570));
            GUILayout.Label("주식 뽑기머신 · Sandbox",title);
            GUILayout.Label("기존 거래소 / 캐릭터 / 주식·경매 재사용\n호스트 시작 → 참가자 접속 → 준비 → 시작",body);
            if(!NetworkClient.active&&!NetworkServer.active)
            {
                GUILayout.Label("닉네임",small);nickname=GUILayout.TextField(nickname,12,field);
                GUILayout.Label("서버 주소 · 기본 KCP 7777",small);address=GUILayout.TextField(address,field);
                if(GUILayout.Button("로컬 호스트 시작",button))Connect(room,true);
                if(GUILayout.Button("서버에 참가",button))Connect(room,false);
            }
            else
            {
                var net=NetworkClient.localPlayer?NetworkClient.localPlayer.GetComponent<PlayerNet>():null;
                if(net)
                {
                    GUILayout.Label($"참가자 {PlayerNet.All.Count}명 · {net.nickname}",body);
                    if(!net.ready&&GUILayout.Button("준비 완료",button))net.SetReady(true);
                    if(net.ready)GUILayout.Label("준비 완료 · 호스트의 시작 대기",small);
                    if(NetworkServer.active)
                    {
                        GUILayout.Label("원래 경기 시간: 일반 90초 / 경매 150초 · 13R",small);
                        GUI.enabled=room.CanStartMatch;
                        if(GUILayout.Button("경기 시작 · 인트로 생략",button))NetworkClient.Send(new StockGachaStart());
                        GUI.enabled=true;
                    }
                }
                GUILayout.Label("경기가 시작된 후에는 신규 참가 불가",small);
            }
            GUILayout.Label("1회 $20 · 꽝 90개 + 회사별 주식 20주\n전체 공용 150개 · 시작 현금 $400 · 초기 주가·시장 재고 2배",small);
            GUILayout.Label(StockGachaSandboxClient.Reply,small);GUILayout.EndArea();
        }
        void Connect(GameNetworkManager room,bool host)
        {
            if(!BuyBidBlow.Systems.Room.Nickname.TryNormalize(nickname,out var normalized))
            {StockGachaSandboxClient.Reply="닉네임을 1~12글자로 입력하세요.";return;}
            room.LocalNickname=normalized;
            if(host)room.StartHost();else{room.networkAddress=address.Trim();room.StartClient();}
        }
        void DrawMachine()
        {
            var s=StockGachaSandboxClient.State;int available=PlayerNet.LocalPortfolio?.Available??0;
            Fill(new Rect(0,0,1600,1000),new Color(0,0,0,.65f));Box(new Rect(430,130,740,780));
            GUILayout.BeginArea(new Rect(465,155,670,730));
            GUILayout.Label(s.Finished?"주식 뽑기머신 · 소진":"주식 뽑기머신",title);
            GUILayout.Label($"1회 ${s.Price}  /  남은 캡슐 {s.Remaining}/{s.Initial}",body);
            GUILayout.BeginHorizontal();
            GUILayout.Label("상품",small,GUILayout.Width(250));
            GUILayout.Label("남은 수량",small,GUILayout.Width(180));
            GUILayout.Label("현재 확률",small);GUILayout.EndHorizontal();
            GUILayout.Space(8);
            Row("꽝",s.Blanks,s.Remaining);
            Row("Straw 1주",s.Straw,s.Remaining);
            Row("Timber 1주",s.Timber,s.Remaining);
            Row("Brick 1주",s.Brick,s.Remaining);
            GUILayout.Space(12);
            GUILayout.Label($"사용 가능 현금 ${available}\n내 이용 횟수 {s.OwnSpins}회",body);
            GUILayout.Label("뽑힌 항목은 제거됩니다. 꽝도 소모됩니다.\n시장과 별도 재고 · 경기 중 재충전 없음",small);
            GUILayout.Space(10);
            GUI.enabled=CanSpin;
            if(GUILayout.Button(StockGachaSandboxClient.Pending?"서버 결과 대기 중…":$"한 번 뽑기 · ${s.Price} · E / Enter",button))Spin();
            GUI.enabled=true;
            GUILayout.Label(StockGachaSandboxClient.Reply,body);
            if(s.Finished)GUILayout.Label("모든 캡슐이 소진되었습니다. 다음 경기에서 다시 채워집니다.",small);
            else if(available<s.Price)GUILayout.Label("입찰 예약금을 제외한 현금이 부족합니다.",small);
            GUILayout.Label("마우스로 클릭 또는 E/Enter 뽑기 · Q 닫기",small);
            if(GUILayout.Button("닫기 · Q",button))Close();
            GUILayout.EndArea();
        }
        void Row(string name,int count,int total)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name,body,GUILayout.Width(250));
            GUILayout.Label(count.ToString(),body,GUILayout.Width(180));
            GUILayout.Label($"{Chance(count,total):0.#}%",body);
            GUILayout.EndHorizontal();
        }
        public static float Chance(int count,int total)=>total>0?100f*count/total:0;
        static void Fill(Rect rect,Color color)
        {
            var previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;
        }
        static void Box(Rect rect)
        {
            Fill(rect,new Color(.65f,.48f,.18f));
            Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),new Color(.025f,.095f,.075f));
        }
        static string OutcomeText()
        {
            var o=StockGachaSandboxClient.Outcome;
            return o.Company<0?$"{o.Player} · 꽝\n남은 캡슐 {o.Remaining}개":
                $"{o.Player} · {(BuyBidBlow.Systems.Types.Company)o.Company} 1주 획득!\n남은 캡슐 {o.Remaining}개";
        }
    }
}
