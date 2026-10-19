using System;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.UI.Shared;
using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Result
{
    [Serializable] public struct ResultEntry
    {
        public int Rank, TotalAssets, Cash, StockValue, TotalShares;
        public string Nickname;
        public bool IsLocal;
    }
    public sealed class MatchResultView : ModalView
    {
        public GameObject[] Rows;
        public Text[] RankLabels, NameLabels, TotalLabels, CashLabels, StockLabels, SharesLabels;
        public Image[] RowBackgrounds;
        public Text Winner;
        // Rank/order are supplied by the authoritative result; this view never calculates game rules.
        public void ShowResults(IReadOnlyList<ResultEntry> entries, bool revealBreakdown, bool canReturn)
        {
            if (entries == null || entries.Count < 1 || entries.Count > Rows.Length) throw new ArgumentException("Expected 1 to 6 authoritative entries.");
            for (int i = 0; i < Rows.Length; i++)
            {
                Rows[i].SetActive(i < entries.Count); if (i >= entries.Count) continue;
                var e = entries[i]; RankLabels[i].text = e.Rank.ToString(); NameLabels[i].text = e.Nickname ?? "";
                TotalLabels[i].text = $"${e.TotalAssets:N0}";
                CashLabels[i].text = revealBreakdown || e.IsLocal ? $"${e.Cash:N0}" : "—";
                StockLabels[i].text = revealBreakdown || e.IsLocal ? $"${e.StockValue:N0}" : "—";
                SharesLabels[i].text = revealBreakdown || e.IsLocal ? $"{e.TotalShares}주" : "—";
                RowBackgrounds[i].color = e.IsLocal ? new Color(.75f,.52f,.16f,.32f) : new Color(0,0,0,.17f);
            }
            Winner.text = string.Join(" · ", entries.Where(e => e.Rank == 1).Select(e => e.Nickname));
            SetButtons("로비로 돌아가기"); EscapeEnabled = false;
            Show("최종 결과", "17 / 17 ROUND"); SetButtonEnabled(0, canReturn);
            Footer.text = canReturn ? "총자산 동점 → 보유 주식 수 비교 → 동일하면 공동 순위" : "방장의 로비 복귀를 기다리고 있습니다.";
        }
    }
}
