using BuyBidBlow.UI.Market;
namespace BuyBidBlow.PlayerUISandbox {
// Sandbox fixture; replace with an authoritative game-state provider later.
public static class TradePreviewSource {
 public static TradingPanelData ForCompany(int company){return new TradingPanelData{company=new[]{"Straw","Timber","Brick"}[company],price=new[]{24,35,46}[company],owned=new[]{2,2,0}[company],stock=8,available=115};}
}
}
