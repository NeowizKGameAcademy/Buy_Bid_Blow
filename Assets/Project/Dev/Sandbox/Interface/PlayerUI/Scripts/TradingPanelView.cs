using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.PlayerUISandbox {
// Presentation only: never changes cash, inventory, or bets.
public sealed class TradingPanelView : MonoBehaviour {
 readonly Dictionary<string,Text> labels=new Dictionary<string,Text>();
 public bool CanConfirm {get;private set;}
 public string Status {get;private set;}
 void Bind(){if(labels.Count>0)return;foreach(var t in GetComponentsInChildren<Text>(true))if(!labels.ContainsKey(t.name))labels.Add(t.name,t);}
 void Set(string name,string value){Bind();if(labels.TryGetValue(name,out var t))t.text=value;}
 public void Present(TradePreviewData data,int tab,ref int quantity){
 int limit=tab==0?Mathf.Min(data.stock,data.available/data.price):tab==1?data.owned:1;
 quantity=Mathf.Clamp(quantity,1,Mathf.Max(1,limit));
 CanConfirm=limit>0&&(tab!=2||(data.price>5&&data.available>=data.price));
 Status=CanConfirm?"":tab==1?"보유 주식 없음":tab==0&&data.stock==0?"시장 재고 없음":tab==2&&data.price<=5?"하한가 · 베팅 불가":"현금 부족";
 int cost=quantity*data.price;
 Set("Company Name",data.company);Set("Current Price","$"+data.price);Set("Market Stock",data.stock+"주");Set("Owned",data.owned+"주");Set("Maximum",Mathf.Min(data.stock,data.available/data.price)+"주");
 Set("Quantity",tab==2?"$"+data.price:quantity.ToString());Set("Unit",tab==2?"":"주");
 Set("Total Label",tab==2?"성공 지급액":tab==1?"매도 금액":"합계");Set("Total Value","$"+(tab==2?data.price*2:cost));
 Set("Cash After Label","거래 가능 $"+data.available+"  →  거래 후");Set("Cash After","$"+(CanConfirm?(tab==1?data.available+cost:data.available-cost):data.available));
 Set("Action",CanConfirm?new[]{"매수 확인","매도 확인","베팅 확인"}[tab]:Status);
 var q=transform.Find("Quantity Control/Label");if(q)q.GetComponent<Text>().text=new[]{"매수 수량","매도 수량","베팅 참가금"}[tab];
 var names=new[]{"Buy","Sell","Downside Bet"};for(int i=0;i<3;i++){var t=transform.Find("Trade Tabs/"+names[i]);t.Find("Label").GetComponent<Text>().color=i==tab?new Color(1,.81f,.35f):new Color(.62f,.7f,.69f);t.GetComponent<Image>().color=new Color(1,1,1,i==tab?.08f:.01f);}
 var indicator=(RectTransform)transform.Find("Trade Tabs/Selected Tab Indicator");indicator.anchoredPosition=new Vector2(22+tab*265,indicator.anchoredPosition.y);
 var button=transform.Find("Payment Summary/Confirm Purchase").GetComponent<Button>();button.interactable=CanConfirm;
 }
}
public struct TradePreviewData {public string company;public int price,owned,stock,available;}
// Sandbox fixture; replace with an authoritative game-state provider later.
public static class TradePreviewSource {
 public static TradePreviewData ForCompany(int company){return new TradePreviewData{company=new[]{"Straw","Timber","Brick"}[company],price=new[]{24,35,46}[company],owned=new[]{2,2,0}[company],stock=8,available=115};}
}
}