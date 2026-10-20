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
 readonly List<int[]> prices=new List<int[]>();readonly LotGenerator lots;
 public readonly TradeService Trades;
 public AuctionRound Auction {get;private set;}
 public int Round {get;private set;}=1;
 public double Deadline {get;private set;}
 public bool Complete {get;private set;}
 public event Action Changed;
 public event Action<AuctionResult> InformationAwarded; public event Action<AuctionResult> PublicResolved; public event Action<SecretResult> SecretResolved;
 readonly int roundSecondsOverride;
 // now: when round 1 opens. roundSecondsOverride > 0 makes every round that long (playtests).
 public MatchEngine(MatchSettings settings,int seed,IReadOnlyList<PlayerId> players,double now,int roundSecondsOverride=0){
 settings.Validate();Settings=settings;this.roundSecondsOverride=roundSecondsOverride;// Servers may start with fewer players for playtests (-minPlayers); they get the smallest table's supply.
 int supply=settings.GetInitialCompanySupply(Math.Max(players.Count,settings.MinPlayerCount));
 foreach(var p in players){if(assets.ContainsKey(p))throw new ArgumentException("Duplicate player");assets.Add(p,new PlayerPortfolio(settings.StartingCash,settings.StartingShares));}
 foreach(Company c in Enum.GetValues(typeof(Company)))inventory[c]=supply;
 var rng=new Random(seed);prices.Add(settings.RollStartingPrices(rng));
 for(int i=0;i<settings.RoundCount;i++){var next=(int[])prices[i].Clone();foreach(var change in PriceChangeGenerator.Generate(settings.PriceChange,rng))next[(int)change.Company]=Math.Max(settings.MinPrice,next[(int)change.Company]+change.Amount);prices.Add(next);}
 lots=new LotGenerator(settings.AuctionRounds,prices,rng);Trades=new TradeService(assets,Price,inventory);StartRound(now);
 }
 int PriceIndex=>Complete?Settings.RoundCount:Round-1;
 public int Price(Company c)=>prices[PriceIndex][(int)c];
 public int LastChange(Company c)=>PriceIndex==0?0:prices[PriceIndex][(int)c]-prices[PriceIndex-1][(int)c];
 // Public chart history stops at the currently revealed price, never the precomputed future.
 public int PublishedPriceCount=>PriceIndex+1;
 public int PublishedPrice(int sample,Company company){if(sample<0||sample>=PublishedPriceCount)throw new ArgumentOutOfRangeException(nameof(sample));return prices[sample][(int)company];}
 public int Stock(Company c)=>inventory[c];
 public PortfolioSnapshot Snapshot(PlayerId p)=>Trades.GetSnapshot(p);
 public bool HasBet(PlayerId p)=>bets.ContainsKey(p);
 public bool HasBet(PlayerId p,Company c)=>bets.TryGetValue(p,out var bet)&&bet.Company==c;
 public int BetStake(PlayerId p)=>bets.TryGetValue(p,out var b)?b.Stake:0;
 public string BetDescription(PlayerId p)=>bets.TryGetValue(p,out var b)?$"↓ {b.Company} · ${b.Stake} → ${b.Stake*2}":"";
 bool Accept(PlayerId p,double now)=>!Complete&&now<Deadline&&assets.ContainsKey(p);
 public string Trade(PlayerId p,Company c,int quantity,bool buy,double now){if(!Accept(p,now))return "거래 마감";var result=buy?Trades.TryBuy(p,c,quantity):Trades.TrySell(p,c,quantity);Changed?.Invoke();return result.Message;}
 public string ToggleBet(PlayerId p,Company c,double now){
 if(!Accept(p,now))return "베팅 마감";
 if(bets.TryGetValue(p,out var old)){if(old.Company!=c)return "기존 베팅을 먼저 취소하세요.";assets[p].Credit(old.Stake);bets.Remove(p);Changed?.Invoke();return "베팅 취소 · 참가금 전액 환불";}
 int stake=Price(c);if(stake<=Settings.MinPrice)return "하한가 · 베팅 불가";if(!assets[p].TrySpendAvailable(stake))return "현금 부족";
 bets.Add(p,new Bet(c,stake));Changed?.Invoke();return "하락 베팅 참여 완료";
 }
 public string Bid(PlayerId p,int amount,bool cancel,double now){
 if(!Accept(p,now)||Auction==null)return "진행 중인 경매 없음";
 AuctionError result;
 if(Auction.Secret!=null)result=cancel?Auction.CancelSecret(p,now):Auction.SubmitSecret(p,amount,now);
 else if(cancel)return "공개 최고 입찰 취소 불가";
 else result=Auction.PlaceOpenBid(p,amount,now);
 Changed?.Invoke();return result==AuctionError.None?(cancel?"입찰 취소 완료":"입찰 접수 완료"):"입찰 실패: "+result;
 }
 void StartRound(double now){Deadline=now+(roundSecondsOverride>0?roundSecondsOverride:Settings.GetRoundSeconds(Round));Auction=null;if(Settings.IsAuctionRound(Round)){
 // No agreed minimum yet: retain the existing local $1 fallback.
 Auction=new AuctionRound(lots.Next(),lots.Next(),now,Settings.AuctionSlots,1,assets);
 Auction.OpenClosed+=r=>{PublicResolved?.Invoke(r);if(r.Sold)InformationAwarded?.Invoke(r);};Auction.SecretClosed+=r=>{SecretResolved?.Invoke(r);if(r.Result.Sold)InformationAwarded?.Invoke(r.Result);};Auction.Tick(now);
 }}
 public void Tick(double now){if(Complete)return;bool changed=false;while(!Complete&&now>=Deadline){Auction?.Tick(Deadline);int closing=Round;foreach(var entry in bets){var b=entry.Value;if(prices[closing][(int)b.Company]<prices[closing-1][(int)b.Company])assets[entry.Key].Credit(b.Stake*2);}bets.Clear();double boundary=Deadline;if(Round==Settings.RoundCount){Complete=true;Auction=null;}else{Round++;StartRound(boundary);}changed=true;}Auction?.Tick(now);if(changed)Changed?.Invoke();}
 public int Total(PlayerId p){var s=Snapshot(p);int total=s.Cash;foreach(Company c in Enum.GetValues(typeof(Company)))total=checked(total+s.GetShares(c)*Price(c));return total;}
 public string Standings(){var order=assets.Keys.OrderByDescending(Total).ThenByDescending(p=>Enum.GetValues(typeof(Company)).Cast<Company>().Sum(c=>Snapshot(p).GetShares(c))).ToList();var rows=new List<string>();int rank=0,previousTotal=-1,previousShares=-1;for(int i=0;i<order.Count;i++){var p=order[i];int total=Total(p),shares=Enum.GetValues(typeof(Company)).Cast<Company>().Sum(c=>Snapshot(p).GetShares(c));if(total!=previousTotal||shares!=previousShares)rank=i+1;rows.Add($"{rank}위 {p} · ${total} · {shares}주");previousTotal=total;previousShares=shares;}return string.Join("\n",rows);}
 readonly struct Bet {public readonly Company Company;public readonly int Stake;public Bet(Company company,int stake){Company=company;Stake=stake;}}
}
}
