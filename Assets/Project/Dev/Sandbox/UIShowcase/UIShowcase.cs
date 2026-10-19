using System;
using BuyBidBlow.UI.Shared;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Menu;
using BuyBidBlow.UI.Loading;
using BuyBidBlow.UI.Result;
using BuyBidBlow.UI.Round;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BuyBidBlow.Dev
{
    // UI specimen inputs only; not a fake game/service and never included in a player build.
    public sealed class UIShowcase : MonoBehaviour
    {
        public ModalView Error, LeaveConfirm, Controls;
        public ConnectionPanelView Connection;
        public PauseMenuView Pause;
        public SettingsPanelView Settings;
        public LoadingView Loading;
        public MatchResultView Results;
        public AuctionResultView Auction;
        public RoundStartView Round;
        public NotificationStackView MarketStack, LobbyStack;
        public MarketNoticeView Market;
        public LobbyNoticeView Lobby;
        public Text SpecimenLabel, Status;
        public GameObject PreviewHud;
        public int Selected { get; private set; }
        public int Count => Labels.Length;
        public static readonly string[] Labels={"공통 오류 · 원인 영역","접속 중","접속 실패","연결 끊김","호스트 종료","게임 메뉴","나가기 확인","로딩 · 진행률 미확정","로딩 · 진행률 전달","결과 · 3명","결과 · 6명 / 공동 1위","라운드 시작","공개 경매 낙찰","비공개 낙찰 성공","비공개 미낙찰","유찰","거래·베팅 로그","로비 입장·퇴장 로그","설정","조작 안내","오류 · 긴 원인 문구"};
        private ModalView[] modals;
        private void Start()
        {
            modals=new ModalView[]{Error,LeaveConfirm,Controls,Connection,Pause,Settings,Results,Auction};
            foreach(var modal in modals){var m=modal;m.ActionRequested+=i=>Status.text=$"표시 이벤트: {m.name} / 버튼 {i} · 실제 게임 동작 연결 없음";}
            Pause.ActionRequested+=i=>{if(i==1)ShowCase(18);else if(i==2)ShowCase(19);else if(i==3)ShowCase(6);};
            Settings.ApplyRequested+=v=>Status.text=$"설정 적용 요청: Master {v.Master}, Music {v.Music}, Effects {v.Effects} · 기기 설정은 변경하지 않음";
            ShowCase(0);
        }
        private void Update()
        {
            var k=Keyboard.current;if(k==null)return;
            if(k.pageDownKey.wasPressedThisFrame)ShowCase((Selected+1)%Count);
            else if(k.pageUpKey.wasPressedThisFrame)ShowCase((Selected+Count-1)%Count);
            else if(k.homeKey.wasPressedThisFrame)ShowCase(0);
            else if(k.f5Key.wasPressedThisFrame)ShowCase(Selected);
            if(k.f6Key.wasPressedThisFrame)Market.Rejected("시장 재고가 부족합니다. 요청 전체가 취소되었습니다.");
            if(k.f7Key.wasPressedThisFrame)Lobby.Joined("긴닉네임테스트123");
        }
        public void ShowCase(int index)
        {
            Selected=Mathf.Clamp(index,0,Count-1);
            foreach(var m in new ModalView[]{Error,LeaveConfirm,Controls,Connection,Pause,Settings,Results,Auction})m.Hide();
            Loading.Hide();Round.Hide();MarketStack.Clear();LobbyStack.Clear();
            PreviewHud.SetActive(index==11||index>=12&&index<=17);
            SpecimenLabel.text=$"UI SHOWCASE  {Selected+1:00} / {Count:00}  ·  {Labels[Selected]}";
            Status.text="PgUp/PgDn 화면 전환 · F5 다시 열기 · F6 거래 로그 추가 · F7 입장 로그 추가 · Home 처음";
            switch(Selected)
            {
                case 0:Error.EscapeAction=0;Error.SetButtons("확인");Error.Show("요청을 처리할 수 없습니다","아래 원인을 확인해주세요.","방의 최대 인원 6명이 모두 접속 중입니다.");break;
                case 1:Connection.ShowState(ConnectionPanelState.Connecting,"","localhost:7777");break;
                case 2:Connection.ShowState(ConnectionPanelState.Failed,"서버가 응답하지 않습니다. 주소와 포트를 확인해주세요.");break;
                case 3:Connection.ShowState(ConnectionPanelState.Disconnected,"서버에서 응답을 받지 못했습니다.");break;
                case 4:Connection.ShowState(ConnectionPanelState.HostClosed,"방이 종료되었습니다.");break;
                case 5:Pause.OpenMenu();break;
                case 6:LeaveConfirm.SetButtons("취소","나가기");LeaveConfirm.EscapeAction=0;LeaveConfirm.Show("방을 나가시겠습니까?","진행 중인 경기를 떠납니다.");break;
                case 7:Loading.Show("경매장으로 이동 중","연결된 화면을 준비하고 있습니다.");break;
                case 8:Loading.Show("경매장으로 이동 중","진행률은 연결 담당자가 전달합니다.",.62f);break;
                case 9:Results.ShowResults(Entries(3,false),true,true);break;
                case 10:Results.ShowResults(Entries(6,true),false,false);break;
                case 11:Round.Show(6,17,true,4);break;
                case 12:Auction.ShowPublic("루나",35,true);break;
                case 13:Auction.ShowSecret(true,40);break;
                case 14:Auction.ShowSecret(false,25);break;
                case 15:Auction.ShowUnsold();break;
                case 16:Market.TradeCompleted("Straw",2,-32,true);Market.BetSettled("Timber",40,80,true);Market.BetSettled("Brick",35,0,false);break;
                case 17:Lobby.Joined("루나");Lobby.Left("로이");break;
                case 18:Settings.ShowSettings(SettingsValues.Defaults);break;
                case 19:Controls.SetButtons("닫기");Controls.Show("조작 안내","WASD  이동    /    마우스  시점\nSpace  점프    /    E  상호작용·확인\nI  정보 수첩    /    Esc  닫기·메뉴\n←→  수량·입찰금    /    ↑↓  거래 메뉴\nC  비공개 입찰 취소");break;
                case 20:Error.SetButtons("다시 시도","돌아가기");Error.EscapeAction=1;Error.Show("접속할 수 없습니다","입장 요청이 완료되지 않았습니다.","서버가 제한 시간 안에 응답하지 않았습니다. 입력한 주소와 포트가 맞는지 확인하고, 서버가 실행 중인지 확인해주세요. 같은 문제가 계속되면 방의 접속 정보를 다시 확인해주세요.");break;
            }
        }
        public static ResultEntry[] Entries(int count,bool tie)
        {
            var a=new ResultEntry[count];string[] names={"루나","모카","로이","긴닉네임테스트123","브릭","팀버"};
            for(int i=0;i<count;i++){int total=620-i*45;if(tie&&i==1)total=620;int cash=200-i*10;a[i]=new ResultEntry{Rank=tie&&i==1?1:i+1,Nickname=names[i],Cash=cash,StockValue=total-cash,TotalAssets=total,TotalShares=tie&&i<2?12:12-i,IsLocal=i==1};}return a;
        }
    }
}
