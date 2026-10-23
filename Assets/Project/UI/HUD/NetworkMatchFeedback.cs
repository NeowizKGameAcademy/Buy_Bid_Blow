using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.Market;
using BuyBidBlow.UI.Notebook;
using BuyBidBlow.UI.Result;
using BuyBidBlow.UI.Shared;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BuyBidBlow.UI.HUD
{
    [DefaultExecutionOrder(-250)]
    public sealed class NetworkMatchFeedback : MonoBehaviour
    {
        public MarketNoticeView notices;
        public AuctionResultView auctionResult;
        public MatchResultView results;
        public NetworkNotebookController notebook;
        public TradePanelController trading;
        public PlayerMove movement;
        public GameObject betStatus;
        public Text betLabel;
        public Image betIcon;
        public Sprite[] companyIcons;
        static NetworkMatchFeedback current;
        public static bool BlocksInput => current && ((current.auctionResult && current.auctionResult.IsOpen) || (current.results && current.results.IsOpen));
        readonly Queue<AuctionOutcome> outcomes = new Queue<AuctionOutcome>();
        bool canRead,shownResults,host;
        UnityEngine.Events.UnityAction[] auctionClicks,resultClicks;
        void OnEnable()
        {
            current=this;
            var owner=GetComponentInParent<PlayerNet>(true);
            if(owner)
            {
                movement=owner.GetComponent<PlayerMove>();
                trading=owner.GetComponentInChildren<TradePanelController>(true);
            }
            else
            {
                if(!movement)movement=FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
                if(!trading)trading=FindFirstObjectByType<TradePanelController>(FindObjectsInactive.Include);
            }
            PlayerNet.LocalNotice+=OnNotice;PlayerNet.LocalAuctionOutcome+=OnOutcome;PlayerNet.LocalResultsChanged+=ShowResults;
            auctionResult.ActionRequested+=OnAuctionAction;results.ActionRequested+=OnReturn;
            auctionResult.CloseOnAction=true;results.CloseOnAction=false;
            auctionClicks=BindButtons(auctionResult);resultClicks=BindButtons(results);EnsureEventSystem();
            auctionResult.Hide();results.Hide();shownResults=false;RefreshBet();
            if(PlayerNet.LocalResults!=null)ShowResults();
        }
        void OnDisable()
        {
            PlayerNet.LocalNotice-=OnNotice;PlayerNet.LocalAuctionOutcome-=OnOutcome;PlayerNet.LocalResultsChanged-=ShowResults;
            auctionResult.ActionRequested-=OnAuctionAction;results.ActionRequested-=OnReturn;
            UnbindButtons(auctionResult,auctionClicks);UnbindButtons(results,resultClicks);
            outcomes.Clear();auctionResult.Hide();results.Hide();notices.Stack.Clear();
            if(current==this)current=null;
        }
        void Update()
        {
            EnsureEventSystem();
            RefreshBet();
            if(BlocksInput && movement){movement.SetMoveInput(Vector2.zero);movement.SetJumpHeld(false);}
            if(!shownResults && !auctionResult.IsOpen && !notebook.view.IsOpen && outcomes.Count>0)PresentOutcome(outcomes.Dequeue());
            if(shownResults && host!=IsRoomHost())ShowResults();
        }
        void RefreshBet()
        {
            bool active=PlayerNet.LocalBet.HasValue && PlayerNet.LocalPortfolio!=null;
            betStatus.SetActive(active);
            if(!active)return;
            int stake=PlayerNet.LocalBetStake;betIcon.sprite=companyIcons[(int)PlayerNet.LocalBet.Value];
            betLabel.text=$"↓  {MatchNet.Current?.round ?? 0}R · ${stake} → ${stake*2}";
        }
        public void OnNotice(MatchNotice n)
        {
            string company=((Company)n.Company).ToString();
            switch(n.Kind)
            {
                case MatchNoticeKind.Trade:BuyBidBlow.Audio.GameAudio.Play("market.trade",this);notices.TradeCompleted(company,n.Quantity,n.Buy?-n.Amount:n.Amount,n.Buy);break;
                case MatchNoticeKind.Rejected:BuyBidBlow.Audio.GameAudio.Play("ui.error",this);notices.Rejected(n.Message);break;
                case MatchNoticeKind.BetSettled:notices.BetSettled(company,n.Amount,n.Payout,n.Won);break;
                case MatchNoticeKind.BetJoined:BuyBidBlow.Audio.GameAudio.Play("market.bet",this);notices.Stack.Push("하락 베팅 참여",$"{company} · 참가금 ${n.Amount} · 성공 지급 ${n.Amount*2}",NoticeTone.Success);break;
                case MatchNoticeKind.BetCancelled:notices.Stack.Push("하락 베팅 취소",$"참가금 ${n.Amount} 전액 환불");break;
                default:notices.Stack.Push(n.Message,"",NoticeTone.Success);break;
            }
        }
        public void OnOutcome(AuctionOutcome outcome){outcomes.Enqueue(outcome);}
        void PresentOutcome(AuctionOutcome outcome)
        {
            if(trading && trading.IsOpen)trading.Close();
            canRead=outcome.Won;
            if(outcome.Won)BuyBidBlow.Audio.GameAudio.Play("auction.won",this);
            if(outcome.Secret)auctionResult.ShowSecret(outcome.Won,outcome.Amount);
            else if(outcome.Sold)auctionResult.ShowPublic(outcome.Winner,outcome.Amount,outcome.Won);
            else auctionResult.ShowUnsold();
        }
        void OnAuctionAction(int action){if(action==0 && canRead)notebook.OpenInformation(true);}
        public void ShowResults()
        {
            var rows=PlayerNet.LocalResults;if(rows==null || rows.Length==0)return;
            if(trading && trading.IsOpen)trading.Close();
            notebook.Close();auctionResult.Hide();outcomes.Clear();shownResults=true;host=IsRoomHost();
            var entries=rows.Select(r=>new ResultEntry{Rank=r.Rank,Nickname=r.Nickname,TotalAssets=r.Total,Cash=r.Cash,StockValue=r.StockValue,TotalShares=r.Shares,IsLocal=r.Own,CompanyShares=r.CompanyShares,FinalPrices=r.FinalPrices,AssetHistory=r.AssetHistory}).ToArray();
            results.ShowResults(entries,true,host,PlayerNet.LocalResultRounds);
            EnsureEventSystem();
        }
        static bool IsRoomHost()
        {
            var local=NetworkClient.localPlayer?NetworkClient.localPlayer.GetComponent<PlayerNet>():null;
            return local && PlayerNet.All.Where(p=>p && p.inMatch).All(p=>p.seat>=local.seat);
        }
        void OnReturn(int action)
        {
            if(action!=0 || !host)return;
            var local=NetworkClient.localPlayer?NetworkClient.localPlayer.GetComponent<PlayerNet>():null;
            if(local)local.CmdReturnToLobby();
        }
        void EnsureEventSystem()
        {
            if(EventSystem.current)return;
            var events=new GameObject("Gameplay EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform,false);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        static UnityEngine.Events.UnityAction[] BindButtons(ModalView view)
        {
            var actions=new UnityEngine.Events.UnityAction[view.Buttons.Length];
            for(int i=0;i<actions.Length;i++){int index=i;actions[i]=()=>view.Submit(index);view.Buttons[i].onClick.AddListener(actions[i]);}
            return actions;
        }
        static void UnbindButtons(ModalView view,UnityEngine.Events.UnityAction[] actions)
        {if(actions==null)return;for(int i=0;i<actions.Length;i++)view.Buttons[i].onClick.RemoveListener(actions[i]);}
    }
}
