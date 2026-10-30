using System;
using BuyBidBlow.Systems.Types;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Result
{
    public sealed class DetailedResultsView : MonoBehaviour
    {
        public Button[] playerButtons;public Text[] playerLabels,stockRows,yAxis;
        public Text title,total,cash,stock,legend,readout,roundsLabel;
        public GameObject summaryPanel,graphPanel;public Button summaryTab,graphTab,returnButton,menuButton;
        public AssetHistoryGraph graph;ResultEntry[] entries=Array.Empty<ResultEntry>();int selected,round;bool graphMode;MatchResultView owner;bool bound;
        public void Present(MatchResultView source,ResultEntry[] rows,bool canReturn,int rounds)
        {
            owner=source;entries=rows;selected=Mathf.Max(0,Array.FindIndex(rows,r=>r.IsLocal));round=rounds;
            if(!bound){bound=true;for(int i=0;i<playerButtons.Length;i++){int index=i;playerButtons[i].onClick.AddListener(()=>{selected=index;Render();});}summaryTab.onClick.AddListener(()=>SetTab(false));graphTab.onClick.AddListener(()=>SetTab(true));returnButton.onClick.AddListener(()=>owner.Submit(0));menuButton.onClick.AddListener(()=>BuyBidBlow.UI.Menu.GameMenuController.Ensure()?.RequestLeave());graph.RoundSelected+=r=>{round=r;Render();};}
            returnButton.interactable=canReturn;
            returnButton.GetComponentInChildren<Text>().text=canReturn?"E 로비로 돌아가기":"방장의 로비 복귀 대기";
            roundsLabel.text=$"{rounds} / {rounds} ROUND · 최종 결과";
            if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            SetTab(false);
        }
        void SetTab(bool value){graphMode=value;summaryPanel.SetActive(!value);graphPanel.SetActive(value);Render();}
        void Update(){if(entries.Length==0||BuyBidBlow.UI.Menu.GameMenuController.BlocksInput)return;var k=Keyboard.current;if(k==null)return;if(k.wKey.wasPressedThisFrame)selected=(selected+entries.Length-1)%entries.Length;if(k.sKey.wasPressedThisFrame)selected=(selected+1)%entries.Length;if(k.aKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame)SetTab(!graphMode);if(k.eKey.wasPressedThisFrame&&returnButton.interactable)owner.Submit(0);Render();}
        void Render()
        {
            if(entries.Length==0)return;selected=Mathf.Clamp(selected,0,entries.Length-1);var e=entries[selected];title.text=e.Nickname+(e.IsLocal?" · 나":"");total.text=$"총자산  ${e.TotalAssets:N0}";cash.text=$"현금  ${e.Cash:N0}";stock.text=$"최종 주식 가치  ${e.StockValue:N0}";
            for(int i=0;i<playerButtons.Length;i++){playerButtons[i].gameObject.SetActive(i<entries.Length);if(i>=entries.Length)continue;var row=entries[i];playerLabels[i].text=$"{row.Rank}위  {row.Nickname}\n${row.TotalAssets:N0}";playerLabels[i].color=AssetHistoryGraph.Palette[i];playerButtons[i].GetComponent<Image>().color=i==selected?Color.white:new Color(.72f,.78f,.75f);}
            for(int i=0;i<3;i++){int shares=e.CompanyShares!=null&&i<e.CompanyShares.Length?e.CompanyShares[i]:0;int price=e.FinalPrices!=null&&i<e.FinalPrices.Length?e.FinalPrices[i]:0;stockRows[i].text=$"{((Company)i).DisplayName()}     {shares}주 × ${price}     = ${(long)shares*price:N0}";}
            graph.Present(entries,selected,round);for(int i=0;i<yAxis.Length;i++)yAxis[i].text="$"+(graph.Maximum*(4-i)/4).ToString("N0");
            var points=e.AssetHistory;int sample=points==null||points.Length==0?0:Mathf.Clamp(round,0,points.Length-1);readout.text=points==null||points.Length==0?"라운드 기록 없음":$"{(sample==0?"시작":sample+"R 정산 후")} · {e.Nickname}  ${points[sample]:N0}";
            legend.text="모든 플레이어 표시 · 선택한 플레이어의 선을 강조\n그래프 클릭: 해당 라운드 자산 확인";
        }
    }
}
