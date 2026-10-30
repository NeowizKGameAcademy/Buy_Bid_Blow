using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.Props;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.PlayerUISandbox {
// Bridges one local player's inputs/views to the local authority. No network transport here.
[DefaultExecutionOrder(-100)] public sealed class MatchUIBridge:MonoBehaviour {
 public TextAsset settingsFile;public PlayerUIController controller;public Text privateInfo,betStatus,marketBoard;public AuctionPanelView auctionPanel;
 public Transform exchangeBoard; public AuctionEnvelopeDisplay envelope;public int seed=20261016;[Min(.01f)] public float clockSpeed=1;
 public MatchEngine Game {get;private set;}
 public PlayerId Player {get;}=new PlayerId("나");
 public int BidAmount {get;private set;}=1;
 public double Now {get;private set;}
 readonly List<Info> information=new List<Info>();public IReadOnlyList<Info> OwnedInformation=>information;bool showInformation;string feedback="";float feedbackUntil;int lastAuctionRound;bool lastSecret;
 void Start(){var settings=JsonUtility.FromJson<MatchSettings>(settingsFile.text);var players=new List<PlayerId>{Player};for(int i=1;i<settings.DefaultPlayerCount;i++)players.Add(new PlayerId("대기 참가자 "+i));Game=new MatchEngine(settings,seed,players,0);Game.InformationAwarded+=r=>{if(r.Winner.Equals(Player)){information.Add(r.Lot.First);information.Add(r.Lot.Second);}};Game.PublicResolved+=r=>Message(r.Sold?$"공개 낙찰 · {r.Winner} · ${r.Price}":"공개 경매 유찰");Game.SecretResolved+=r=>{foreach(var bidder in r.Players)if(bidder.Equals(Player))Message(r.Result.Sold&&r.Result.Winner.Equals(Player)?"비공개 경매 낙찰 성공":"비공개 경매 낙찰 실패");};Refresh();}
 void Update(){if(Game==null)return;AdvanceClock(Time.deltaTime*clockSpeed);}
 public void AdvanceClock(double seconds){if(Game==null||seconds<0)return;int oldRound=Game.Round;Now+=seconds;Game.Tick(Now);if((Game.Round!=oldRound||Game.Complete)&&controller.CurrentMode==PlayerUIController.Mode.Trade){controller.Close();Message("라운드 마감 · 새 주가를 확인하세요.");}Refresh();}
 public TradingPanelData TradeData(int company){var c=(Company)company;var s=Game.Snapshot(Player);return new TradingPanelData{company=c.DisplayName(),price=Game.Price(c),owned=s.GetShares(c),stock=Game.Stock(c),available=s.Available,change=Game.LastChange(c),hasBet=Game.HasBet(Player,c),otherBet=Game.HasBet(Player)&&!Game.HasBet(Player,c),betStake=Game.BetStake(Player),closed=Game.Complete};}
 public void ConfirmTrade(int company,int tab,int quantity){if(Game==null)return;Message((tab==2?Game.ToggleBet(Player,(Company)company,Now):Game.Trade(Player,(Company)company,quantity,tab==0,Now)).Message);Refresh();}
 public void AdjustBid(int delta){BidAmount=Mathf.Max(MinimumBid(),BidAmount+delta);}
 int MinimumBid()=>Game?.Auction?.Secret!=null?1:Game?.Auction?.Open?.HasBid==true?Game.Auction.Open.HighestBid+1:1;
 public void SubmitBid(bool cancel=false){if(Game==null)return;Message(Game.Bid(Player,BidAmount,cancel,Now).Message);Refresh();}
 public void ToggleInformation(){if(Game==null||Game.Complete)return;showInformation=!showInformation;if(controller.notebook)controller.notebook.SetOpen(showInformation,Game.Round);Refresh();}
 void Message(string message){feedback=message;feedbackUntil=Time.unscaledTime+3;}
 static string Describe(Info info)=>$"{info.Round}R · {info.CompanyA} {DirectionText(info.DirectionA)} · {info.CompanyB} {DirectionText(info.DirectionB)}";
 static string DirectionText(Direction d)=>d==Direction.Rise?"상승":d==Direction.Fall?"하락":"보합";
 static void Set(Transform parent,string name,string value){var t=parent.Find(name);if(t)t.GetComponent<Text>().text=value;}
 static string TimeText(double seconds){int n=Mathf.Max(0,(int)Math.Ceiling(seconds));return $"{n/60:00}:{n%60:00}";}
 public void Refresh(){if(Game==null)return;if(controller.CurrentMode==PlayerUIController.Mode.Trade)controller.RefreshTrade();var hud=controller.hud.transform;var s=Game.Snapshot(Player);var assets=hud.Find("Assets - Top Left");Set(assets,"Cash","$"+s.Cash);Set(assets,"Total Value","$"+Game.Total(Player));Set(assets,"Reserved Cash",$"입찰 묶임 ${s.Reserved} · 거래 가능 ${s.Available}");for(int i=0;i<3;i++)Set(assets,"Holding Quantity "+i,s.GetShares((Company)i).ToString());var timer=hud.Find("Round Timer - Top Right");Set(timer,"Round",$"{Game.Round} / {Game.Settings.RoundCount} ROUND");Set(timer,"Time",TimeText(Game.Deadline-Now));Set(timer,"Phase",Game.Complete?"경기 종료":Game.Settings.IsAuctionRound(Game.Round)?"경매 라운드":"일반 거래");
 if(betStatus){betStatus.text=Game.BetDescription(Player);betStatus.transform.parent.gameObject.SetActive(Game.HasBet(Player));}
 if(privateInfo){privateInfo.transform.parent.gameObject.SetActive(Game.Complete);if(Game.Complete)privateInfo.text="최종 순위\n"+Game.Standings();}if(controller.notebook){if(Game.Complete&&controller.notebook.IsOpen){showInformation=false;controller.notebook.SetOpen(false,Game.Round);}controller.notebook.Present(Game.Round,Game.Settings.RoundCount,information);}
 if(marketBoard){var lines=new List<string>();foreach(Company c in Enum.GetValues(typeof(Company))){int change=Game.LastChange(c);lines.Add($"{c.DisplayName()}   ${Game.Price(c)}   "+(change==0?"보합":change>0?"+$"+change:"−$"+(-change)));}marketBoard.text=string.Join("\n",lines);}
 if(exchangeBoard)for(int i=0;i<3;i++){var company=(Company)i;int change=Game.LastChange(company);Set(exchangeBoard,"Price "+i,"$"+Game.Price(company));Set(exchangeBoard,"Change "+i,change==0?"보합":change>0?"+$"+change:"−$"+(-change));var delta=exchangeBoard.Find("Change "+i);if(delta)delta.GetComponent<Text>().color=change<0?new Color(.2f,.75f,1):change>0?new Color(1,.81f,.35f):Color.white;}
 bool seated=controller.CurrentMode==PlayerUIController.Mode.Auction;var a=Game.Auction;bool secret=a?.Secret!=null;bool live=a!=null&&!a.IsFinished&&!Game.Complete;var panel=new AuctionPanelData{seated=seated,inMatch=!Game.Complete,live=live,secret=secret,complete=Game.Complete,idleNote="다음 경매 2 · 6 · 10 · 14R"};
 if(live){if(lastAuctionRound!=Game.Round||lastSecret!=secret){lastAuctionRound=Game.Round;lastSecret=secret;BidAmount=1;}BidAmount=Mathf.Max(BidAmount,MinimumBid());Lot lot=secret?a.Secret.Lot:a.Open.Lot;if(envelope)envelope.SetRounds(lot.First.Round,lot.Second.Round);panel.lot=lot;panel.secondsLeft=(secret?a.Secret.Deadline:a.Open.Deadline)-Now;panel.hasBid=!secret&&a.Open.HasBid;panel.highestBid=panel.hasBid?a.Open.HighestBid:0;panel.highestBidder=panel.hasBid?a.Open.HighestBidder.ToString():"";panel.ownSubmitted=secret?a.Secret.GetSubmitted(Player):0;panel.ownHighest=panel.hasBid&&a.Open.HighestBidder.Equals(Player);panel.bidAmount=BidAmount;panel.available=s.Available;
 }
 if(auctionPanel)auctionPanel.Present(panel);
 if(envelope)envelope.gameObject.SetActive(live);
 if(Time.unscaledTime<feedbackUntil)controller.prompt.text=feedback;else if(seated)controller.prompt.text="←→ 금액 · E 입찰 · C 비공개 취소 · I 내 정보 · Esc 퇴석";
 }
}
}