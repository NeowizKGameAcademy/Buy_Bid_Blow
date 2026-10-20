using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Market {
// Presentation only: never changes cash, inventory, or bets.
public sealed class TradingPanelView : MonoBehaviour {
 readonly Dictionary<string,Text> labels=new Dictionary<string,Text>();
 public bool CanConfirm {get;private set;}
 public string Status {get;private set;}
 void Bind(){if(labels.Count>0)return;foreach(var t in GetComponentsInChildren<Text>(true))if(!labels.ContainsKey(t.name))labels.Add(t.name,t);}
 void Set(string name,string value){Bind();if(labels.TryGetValue(name,out var t))t.text=value;}
 public void Present(TradingPanelData data,int tab,ref int quantity){
 int affordable=data.available/data.price,buyable=data.stock<0?affordable:Mathf.Min(data.stock,affordable);
 int limit=tab==0?buyable:tab==1?data.owned:1;
 quantity=Mathf.Clamp(quantity,1,Mathf.Max(1,limit));
 CanConfirm=!data.closed&&limit>0&&(tab!=2||!data.betUnavailable&&(data.hasBet||(!data.otherBet&&data.price>5&&data.available>=data.price)));
 Status=CanConfirm?"":data.closed?"거래 마감":tab==2&&data.betUnavailable?"하락 베팅 준비 중":tab==2&&data.otherBet?"기존 베팅 취소 필요":tab==1?"보유 주식 없음":tab==0&&data.stock==0?"시장 재고 없음":tab==2&&data.price<=5?"하한가 · 베팅 불가":"현금 부족";
 int cost=quantity*data.price;
 Set("Company Name",data.company);Set("Current Price","$"+data.price);Set("Market Stock",data.stock<0?"제한 없음":data.stock+"주");Set("Owned",data.owned+"주");Set("Maximum",buyable+"주");
 Set("Last Change",data.change==0?"보합":data.change>0?"+$"+data.change:"−$"+(-data.change));labels["Last Change"].color=data.change<0?new Color(.2f,.75f,1):new Color(1,.81f,.35f);
 Set("Quantity",tab==2?"$"+data.price:quantity.ToString());Set("Unit",tab==2?"":"주");
 Set("Total Label",tab==2?"성공 지급액":tab==1?"매도 금액":"합계");Set("Total Value","$"+(tab==2?data.price*2:cost));
 Set("Cash After Label","거래 가능 $"+data.available+"  →  거래 후");Set("Cash After","$"+(CanConfirm?(tab==2&&data.hasBet?data.available+data.betStake:tab==1?data.available+cost:data.available-cost):data.available));
 Set("Action",CanConfirm?(tab==2&&data.hasBet?"베팅 취소":new[]{"매수 확인","매도 확인","베팅 확인"}[tab]):Status);
 var q=transform.Find("Quantity Control/Label");if(q)q.GetComponent<Text>().text=new[]{"매수 수량","매도 수량","베팅 참가금"}[tab];
 var names=new[]{"Buy","Sell","Downside Bet"};for(int i=0;i<3;i++){var t=transform.Find("Trade Tabs/"+names[i]);t.Find("Label").GetComponent<Text>().color=i==tab?new Color(1,.81f,.35f):new Color(.62f,.7f,.69f);t.GetComponent<Image>().color=new Color(1,1,1,i==tab?.08f:.01f);}
 var indicator=(RectTransform)transform.Find("Trade Tabs/Selected Tab Indicator");indicator.anchoredPosition=new Vector2(22+tab*265,indicator.anchoredPosition.y);
 var button=transform.Find("Payment Summary/Confirm Purchase").GetComponent<Button>();button.interactable=CanConfirm;
 }
}
// stock < 0: the market has no supply limit. betUnavailable: downside bets are not offered here yet.
public struct TradingPanelData {public string company;public int price,owned,stock,available,change,betStake;public bool hasBet,otherBet,closed,betUnavailable;}
}
