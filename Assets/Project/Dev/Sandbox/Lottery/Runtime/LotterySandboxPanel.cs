using System;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.UI.Notebook;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Sandbox.Lottery
{
    // Sandbox-only IMGUI: a real server purchase path, without modifying production UI prefabs.
    public sealed class LotterySandboxPanel : MonoBehaviour
    {
        public Font font;
        public Transform device;
        GameObject owner;
        UnityEngine.InputSystem.PlayerInput input;
        PlayerController controller;
        PlayerMove movement;
        NetworkNotebookController notebook;
        bool notebookWasEnabled;
        bool inputWasActive,controllerWasEnabled;
        CursorLockMode previousLock;
        bool previousVisible;
        int quantity=1,selectedDraw;
        float nextPurchase;
        string nickname="Tester",address="127.0.0.1";
        bool quick=true;
        GUIStyle title,body,small,button,field,toggle;
        public bool IsOpen => owner;

        public bool Open(GameObject player)
        {
            if(IsOpen || !MatchNet.Current || MatchNet.Current.phase!=MatchNet.Phase.Playing)return false;
            owner=player;input=owner.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            controller=owner.GetComponent<PlayerController>();movement=owner.GetComponent<PlayerMove>();
            notebook=owner.GetComponentInChildren<NetworkNotebookController>(true);
            notebookWasEnabled=notebook&&notebook.enabled;if(notebook)notebook.enabled=false;
            inputWasActive=input && input.inputIsActive;controllerWasEnabled=controller&&controller.enabled;
            if(movement){movement.SetMoveInput(Vector2.zero);movement.SetJumpHeld(false);}
            if(input)input.DeactivateInput();if(controller)controller.enabled=false;
            previousLock=Cursor.lockState;previousVisible=Cursor.visible;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            quantity=1;selectedDraw=LotterySandboxClient.State.Draw;LotterySandboxClient.Reply="";
            return true;
        }
        public void Close()
        {
            if(!owner)return;
            // Never reactivate gameplay after disconnect, return to lobby or final results.
            bool playing=NetworkClient.active&&owner.GetComponent<PlayerNet>().inMatch&&MatchNet.Current&&MatchNet.Current.phase==MatchNet.Phase.Playing;
            if(notebook)notebook.enabled=notebookWasEnabled;
            if(controller)controller.enabled=controllerWasEnabled&&playing;
            if(input && inputWasActive && playing)input.ActivateInput();
            Cursor.lockState=playing?previousLock:CursorLockMode.None;
            Cursor.visible=playing?previousVisible:true;owner=null;
        }
        void OnDisable()=>Close();
        void Start(){LotterySandboxClient.Reset();}
        void Update()
        {
            if(!IsOpen)return;
            if(!NetworkClient.active || !MatchNet.Current || MatchNet.Current.phase!=MatchNet.Phase.Playing ||
                NetworkNotebookController.BlocksInput || !device || Vector3.Distance(owner.transform.position,device.position)>4.5f)
            {Close();return;}
            if(Keyboard.current!=null && (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))Close();
            var state=LotterySandboxClient.State;
            if(selectedDraw!=state.Draw){selectedDraw=state.Draw;quantity=1;}
        }
        void Styles()
        {
            if(body!=null)return;
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=22,wordWrap=true};body.normal.textColor=new Color(.96f,.94f,.82f);
            title=new GUIStyle(body){fontSize=32,fontStyle=FontStyle.Bold};
            small=new GUIStyle(body){fontSize=17};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=21,fixedHeight=42};
            field=new GUIStyle(GUI.skin.textField){font=font,fontSize=21,fixedHeight=36};
            toggle=new GUIStyle(GUI.skin.toggle){font=font,fontSize=18,wordWrap=true};
        }
        void OnGUI()
        {
            Styles();GUI.depth=-100;
            var oldMatrix=GUI.matrix;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/1000f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-1000*scale)/2),Quaternion.identity,Vector3.one*scale);
            var room=NetworkManager.singleton as GameNetworkManager;
            if(!room || !room.LotterySandboxEnabled){GUI.matrix=oldMatrix;return;}
            bool playing=room.ClientInMatch&&MatchNet.Current;
            if(!playing)DrawConnection(room);
            else
            {
                var s=LotterySandboxClient.State;
                Box(new Rect(1080,115,500,160));
                GUI.Label(new Rect(1100,128,460,110),s.Finished?"복권 추첨 종료\n장치에서 마지막 결과 확인":$"복권 {s.Draw}회차 · {s.DrawRound}R 종료 추첨\n당첨금 ${s.Prize} · 판매 {s.Sold}/{s.Capacity}\n내 복권 {s.Own}장 · 확률 {(s.Capacity>0?100f*s.Own/s.Capacity:0):0.#}%",body);
                GUI.Label(new Rect(1100,238,460,28),"장치를 바라보고 E · 구매 후 취소 불가",small);
                if(IsOpen)DrawPurchase();
            }
            if(LotterySandboxClient.Outcome.Draw>0&&Time.unscaledTime<LotterySandboxClient.NoticeUntil)
            {
                Box(new Rect(350,25,900,80));
                GUI.Label(new Rect(370,32,860,66),OutcomeText(),body);
            }
            GUI.matrix=oldMatrix;
        }
        void DrawConnection(GameNetworkManager room)
        {
            Box(new Rect(35,35,640,560));
            GUILayout.BeginArea(new Rect(60,55,590,520));
            GUILayout.Label("복권 장치 · Sandbox",title);
            GUILayout.Label("원본 거래소 / 캐릭터 / 주식·경매 재사용\n호스트 시작 → 참가자 접속 → 경기 시작",body);
            if(!NetworkClient.active&&!NetworkServer.active)
            {
                GUILayout.Label("닉네임",small);nickname=GUILayout.TextField(nickname,12,field);
                GUILayout.Label("서버 주소 · 기본 KCP 7777",small);address=GUILayout.TextField(address,field);
                if(GUILayout.Button("로컬 호스트 시작",button)){room.LocalNickname=nickname;room.StartHost();}
                if(GUILayout.Button("서버에 참가",button)){room.LocalNickname=nickname;room.networkAddress=address;room.StartClient();}
            }
            else
            {
                var net=NetworkClient.localPlayer?NetworkClient.localPlayer.GetComponent<PlayerNet>():null;
                if(net)
                {
                    GUILayout.Label($"참가자 {PlayerNet.All.Count}명 · {net.nickname}",body);
                    if(!net.ready && GUILayout.Button("준비 완료",button))net.SetReady(true);
                    if(net.ready)GUILayout.Label("준비 완료 · 호스트가 시작할 때까지 대기",small);
                    if(NetworkServer.active)
                    {
                        quick=GUILayout.Toggle(quick,"빠른 시험: 일반 15초 / 경매 30초 (13R 유지)",toggle);
                        GUI.enabled=room.CanStartMatch;
                        if(GUILayout.Button("경기 시작 · 인트로 생략",button))NetworkClient.Send(new LotteryStart{Quick=quick});
                        GUI.enabled=true;
                    }
                }
                GUILayout.Label("시작 후에는 새 참가자가 들어올 수 없습니다.",small);
            }
            GUILayout.Label("회차 100장 · 개인 10장 · 장당 $5\n기본금 $30 → $50 → $70 / 미당첨 전액 이월",small);
            GUILayout.EndArea();
        }
        void DrawPurchase()
        {
            var s=LotterySandboxClient.State;var portfolio=PlayerNet.LocalPortfolio;
            int available=portfolio?.Available??0;
            int limit=s.Price>0?Math.Max(0,Math.Min(Math.Min(s.Capacity-s.Sold,s.Limit-s.Own),available/s.Price)):0;
            quantity=Mathf.Clamp(quantity,1,Math.Max(1,limit));
            Fill(new Rect(0,0,1600,1000),new Color(0,0,0,.65f));
            Box(new Rect(430,150,740,760));
            GUILayout.BeginArea(new Rect(465,175,670,710));
            GUILayout.Label(s.Finished?"복권 판매 종료":$"제{s.Draw}회 복권",title);
            GUILayout.Label($"현재 당첨금  ${s.Prize}\n기본금 ${s.Base} + 이전 이월 ${s.Carry} + 판매 적립 ${s.Sales}",body);
            GUILayout.Space(10);
            GUILayout.Label($"판매 {s.Sold}/{s.Capacity}장 · 미판매 번호 {s.Capacity-s.Sold}개\n내 복권 {s.Own}/{s.Limit}장 · 당첨 확률 {(s.Capacity>0?100f*s.Own/s.Capacity:0):0.#}%\n전체 당첨 확률 {(s.Capacity>0?100f*s.Sold/s.Capacity:0):0.#}%",body);
            GUILayout.Label(s.Finished?"마지막 미당첨 잔액은 경기 종료 시 소멸":$"{s.DrawRound}R 종료 추첨 · {Math.Max(0,s.Deadline-NetworkTime.time):0}초 남음",small);
            GUILayout.Label($"사용 가능 현금 ${available} · 장당 ${s.Price}",body);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("−",button))quantity=Math.Max(1,quantity-1);
            GUILayout.Label($"{quantity}장 / ${quantity*s.Price}",body);
            if(GUILayout.Button("+",button))quantity=Math.Min(Math.Max(1,limit),quantity+1);
            GUILayout.EndHorizontal();
            GUI.enabled=!s.Finished&&limit>0&&NetworkTime.time<s.Deadline&&Time.unscaledTime>=nextPurchase;
            if(GUILayout.Button("복권 구매",button))Purchase(quantity);
            GUI.enabled=true;
            GUILayout.Label(LotterySandboxClient.Reply,small);
            GUILayout.Label("100개 번호 중 하나 추첨. 미판매 번호면 당첨 없음.\n복권은 매회 소멸하고 기본금·판매금은 미당첨 시 이월됩니다.",small);
            if(LotterySandboxClient.Outcome.Draw>0)GUILayout.Label(OutcomeText(),small);
            if(GUILayout.Button("닫기 · Q",button))Close();
            GUILayout.EndArea();
        }
        public void Purchase(int count)
        {
            nextPurchase=Time.unscaledTime+.35f;
            NetworkClient.Send(new LotteryPurchase{Draw=LotterySandboxClient.State.Draw,Quantity=count});
        }
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
            var o=LotterySandboxClient.Outcome;
            return o.Won?$"제{o.Draw}회 · 당첨 번호 {o.Number:000}\n{o.Winner} 당첨! ${o.Prize} 지급":
                $"제{o.Draw}회 · 당첨 번호 {o.Number:000} · 미판매 번호\n"+(o.Final?$"당첨자 없음 · ${o.Carried} 최종 미지급 잔액":$"당첨자 없음 · ${o.Carried} 다음 회차 이월");
        }
    }
}
