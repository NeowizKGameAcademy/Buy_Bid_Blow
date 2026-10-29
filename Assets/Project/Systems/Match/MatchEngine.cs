using System;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Systems.Auction;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.Systems.Portfolio;
namespace BuyBidBlow.Systems.Match {
// Match rules authority: the dedicated server runs one per match, the PlayerUI sandbox one locally.
// UI receives snapshots and submits requests only.
public sealed class MatchEngine {
 public readonly MatchSettings Settings;
 readonly Dictionary<PlayerId,PlayerPortfolio> assets=new Dictionary<PlayerId,PlayerPortfolio>();
 readonly Dictionary<Company,int> inventory=new Dictionary<Company,int>();
 readonly Dictionary<PlayerId,Bet> bets=new Dictionary<PlayerId,Bet>();
 private readonly Dictionary<PlayerId,List<Info>> information=new Dictionary<PlayerId,List<Info>>();
 readonly List<int[]> prices=new List<int[]>();readonly LotGenerator lots;
 public readonly TradeService Trades;
 public AuctionRound Auction {get;private set;}
 public int Round {get;private set;}=1;
 public double Deadline {get;private set;}
 public bool Complete {get;private set;}
 public event Action Changed;
 public event Action<AuctionResult> InformationAwarded; public event Action<AuctionResult> PublicResolved; public event Action<SecretResult> SecretResolved;
 readonly int roundSecondsOverride;
 // 설정을 검증하고 자산·재고·전체 라운드 가격·경매 물건을 미리 만든 뒤 1라운드 시작
 // now: when round 1 opens. roundSecondsOverride > 0 makes every round that long (playtests).
 public MatchEngine(MatchSettings settings,int seed,IReadOnlyList<PlayerId> players,double now,int roundSecondsOverride=0){
 settings.Validate();Settings=settings;this.roundSecondsOverride=roundSecondsOverride;// Servers may start with fewer players for playtests (-minPlayers); they get the smallest table's supply.
 int supply=settings.GetInitialCompanySupply(Math.Max(players.Count,settings.MinPlayerCount));
 foreach(var p in players){if(assets.ContainsKey(p))throw new ArgumentException("Duplicate player");assets.Add(p,new PlayerPortfolio(settings.StartingCash,settings.StartingShares));information.Add(p,new List<Info>());}
 foreach(Company c in Enum.GetValues(typeof(Company)))inventory[c]=supply;
 var rng=new Random(seed);prices.Add(settings.RollStartingPrices(rng));
 for(int i=0;i<settings.RoundCount;i++){var next=(int[])prices[i].Clone();foreach(var change in PriceChangeGenerator.Generate(settings.PriceChange,rng))next[(int)change.Company]=Math.Max(settings.MinPrice,next[(int)change.Company]+change.Amount);prices.Add(next);}
 lots=new LotGenerator(settings.AuctionRounds,prices,rng);Trades=new TradeService(assets,Price,inventory);StartRound(now);
 }
 int PriceIndex=>Complete?Settings.RoundCount:Round-1;
 // 현재 공개된 회사 가격을 반환
 public int Price(Company c)=>prices[PriceIndex][(int)c];
 // 직전 라운드 대비 회사 가격 변동을 반환
 public int LastChange(Company c)=>PriceIndex==0?0:prices[PriceIndex][(int)c]-prices[PriceIndex-1][(int)c];
 // Public chart history stops at the currently revealed price, never the precomputed future.
 public int PublishedPriceCount=>PriceIndex+1;
 // 공개된 가격 기록에서 특정 시점의 회사 가격을 반환
 public int PublishedPrice(int sample,Company company){if(sample<0||sample>=PublishedPriceCount)throw new ArgumentOutOfRangeException(nameof(sample));return prices[sample][(int)company];}
 // 회사의 남은 시장 재고를 반환
 public int Stock(Company c)=>inventory[c];
 // 플레이어의 포트폴리오 스냅샷을 반환
 public PortfolioSnapshot Snapshot(PlayerId p)=>Trades.GetSnapshot(p);
 public IReadOnlyList<Info> InformationOf(PlayerId p)=>information[p];
 // 플레이어가 베팅 중인지 확인
 public bool HasBet(PlayerId p)=>bets.ContainsKey(p);
 // 플레이어가 해당 회사에 베팅 중인지 확인
 public bool HasBet(PlayerId p,Company c)=>bets.TryGetValue(p,out var bet)&&bet.Company==c;
 // 플레이어의 베팅 참가금을 반환 (없으면 0)
 public int BetStake(PlayerId p)=>bets.TryGetValue(p,out var b)?b.Stake:0;
 // 플레이어의 베팅 내용을 표시용 문자열로 반환
 public string BetDescription(PlayerId p)=>bets.TryGetValue(p,out var b)?$"↓ {b.Company} · ${b.Stake} → ${b.Stake*2}":"";
 // 매치 진행 중·마감 전·참가 플레이어인지 확인
 bool Accept(PlayerId p,double now)=>!Complete&&now<Deadline&&assets.ContainsKey(p);
 // 요청을 받을 수 있으면 매수/매도를 처리하고 결과 메시지를 반환
 public RequestResult Trade(PlayerId p,Company c,int quantity,bool buy,double now){var kind=buy?RequestKind.Buy:RequestKind.Sell;if(!Accept(p,now))return new RequestResult(kind,false,"거래 마감",c);if(!Enum.IsDefined(typeof(Company),c)||quantity<=0)return new RequestResult(kind,false,"잘못된 거래 요청입니다.",c);var result=buy?Trades.TryBuy(p,c,quantity):Trades.TrySell(p,c,quantity);if(result.Succeeded)Changed?.Invoke();return new RequestResult(kind,result.Succeeded,result.Message,c);}
 // 하락 베팅을 취소(환불)하거나 현재가만큼 지불해 새로 참여
 public RequestResult ToggleBet(PlayerId p,Company c,double now){
 if(!Accept(p,now))return new RequestResult(RequestKind.Bet,false,"베팅 마감",c);
 if(!Enum.IsDefined(typeof(Company),c))return new RequestResult(RequestKind.Bet,false,"잘못된 베팅 요청입니다.",c);
 if(bets.TryGetValue(p,out var old)){if(old.Company!=c)return new RequestResult(RequestKind.Bet,false,"기존 베팅을 먼저 취소하세요.",c);assets[p].Credit(old.Stake);bets.Remove(p);Changed?.Invoke();return new RequestResult(RequestKind.Bet,true,"베팅 취소 · 참가금 전액 환불",c);}
 int stake=Price(c);if(stake<=Settings.MinPrice)return new RequestResult(RequestKind.Bet,false,"하한가 · 베팅 불가",c);if(!assets[p].TrySpendAvailable(stake))return new RequestResult(RequestKind.Bet,false,"현금 부족",c);
 bets.Add(p,new Bet(c,stake));Changed?.Invoke();return new RequestResult(RequestKind.Bet,true,"하락 베팅 참여 완료",c);
 }
 // 진행 중인 경매 종류에 맞게 입찰·제출·취소를 처리하고 결과 메시지를 반환
 public RequestResult Bid(PlayerId p,int amount,bool cancel,double now){
 if(!Accept(p,now)||Auction==null)return new RequestResult(RequestKind.Bid,false,"진행 중인 경매 없음");
 AuctionError result;
 if(Auction.Secret!=null)result=cancel?Auction.CancelSecret(p,now):Auction.SubmitSecret(p,amount,now);
 else if(cancel)return new RequestResult(RequestKind.Bid,false,"공개 최고 입찰 취소 불가");
 else result=Auction.PlaceOpenBid(p,amount,now);
 if(result==AuctionError.None)Changed?.Invoke();return new RequestResult(RequestKind.Bid,result==AuctionError.None,result==AuctionError.None?(cancel?"입찰 취소 완료":"입찰 접수 완료"):BidErrorText(result));
 }
 static string BidErrorText(AuctionError error){switch(error){case AuctionError.NotOpen:case AuctionError.Closed:return "입찰이 마감되었습니다.";case AuctionError.TooLow:return "최소 입찰 금액보다 낮습니다.";case AuctionError.AlreadyHighest:return "현재 최고 입찰자입니다.";case AuctionError.NotEnoughCash:return "거래 가능한 현금이 부족합니다.";case AuctionError.NoSubmission:return "취소할 비공개 입찰이 없습니다.";default:return "입찰할 수 없습니다.";}}
 // 라운드 마감 시각을 정하고 경매 라운드면 경매를 생성해 결과 이벤트를 연결
 void StartRound(double now){Deadline=now+(roundSecondsOverride>0?roundSecondsOverride:Settings.GetRoundSeconds(Round));Auction=null;if(Settings.IsAuctionRound(Round)){
 // No agreed minimum yet: retain the existing local $1 fallback.
 // Fast playtests keep both auctions inside their shortened round, preserving slot order.
 double scale=roundSecondsOverride>0?roundSecondsOverride/(double)Settings.AuctionRoundSeconds:1;
 Auction=new AuctionRound(lots.Next(),lots.Next(),now,Settings.AuctionSlots,1,assets,scale);
 Auction.OpenClosed+=r=>{if(r.Sold)Award(r);PublicResolved?.Invoke(r);};Auction.SecretClosed+=r=>{if(r.Result.Sold)Award(r.Result);SecretResolved?.Invoke(r);};Auction.Tick(now);
 }}
 void Award(AuctionResult result){information[result.Winner].Add(result.Lot.First);information[result.Winner].Add(result.Lot.Second);InformationAwarded?.Invoke(result);}
 // 마감 시각이 지난 라운드를 닫아 베팅을 정산하고 다음 라운드로 진행하거나 매치 종료
 public void Tick(double now){if(Complete)return;bool changed=false;while(!Complete&&now>=Deadline){Auction?.Tick(Deadline);int closing=Round;foreach(var entry in bets){var b=entry.Value;if(prices[closing][(int)b.Company]<prices[closing-1][(int)b.Company])assets[entry.Key].Credit(b.Stake*2);}bets.Clear();double boundary=Deadline;if(Round==Settings.RoundCount){Complete=true;Auction=null;}else{Round++;StartRound(boundary);}changed=true;}Auction?.Tick(now);if(changed)Changed?.Invoke();}
 // 현금과 보유 주식 평가액을 합한 총자산을 계산
 public int Total(PlayerId p){var s=Snapshot(p);int total=s.Cash;foreach(Company c in Enum.GetValues(typeof(Company)))total=checked(total+s.GetShares(c)*Price(c));return total;}
 // 총자산·보유 주식 수로 순위를 매겨 문자열로 반환
 public MatchStanding[] GetStandings(){var order=assets.Keys.OrderByDescending(Total).ThenByDescending(p=>Enum.GetValues(typeof(Company)).Cast<Company>().Sum(c=>Snapshot(p).GetShares(c))).ToList();var rows=new MatchStanding[order.Count];int rank=0,previousTotal=-1,previousShares=-1;for(int i=0;i<order.Count;i++){var p=order[i];int total=Total(p),shares=Enum.GetValues(typeof(Company)).Cast<Company>().Sum(c=>Snapshot(p).GetShares(c));if(total!=previousTotal||shares!=previousShares)rank=i+1;rows[i]=new MatchStanding{PlayerId=p.Value,Rank=rank,TotalAssets=total};previousTotal=total;previousShares=shares;}return rows;}
 public string Standings()=>string.Join("\n",GetStandings().Select(r=>$"{r.Rank}위 {r.PlayerId} · ${r.TotalAssets}"));
 readonly struct Bet {public readonly Company Company;public readonly int Stake;public Bet(Company company,int stake){Company=company;Stake=stake;}}
}
}
