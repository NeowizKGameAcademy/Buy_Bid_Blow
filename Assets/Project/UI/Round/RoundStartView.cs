using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Round
{
    public sealed class RoundStartView : MonoBehaviour
    {
        public Text RoundLabel, Detail;
        private float remaining;
        public void Show(int round, int total, bool auction, float seconds = 3)
        {
            RoundLabel.text = $"{round} / {total} ROUND"; Detail.text = auction ? "경매 라운드 · 공개 경매가 시작됩니다" : "거래를 시작하세요";
            remaining = Mathf.Max(.1f, seconds); gameObject.SetActive(true);
        }
        public void Hide() => gameObject.SetActive(false);
        private void Update() { remaining -= Time.unscaledDeltaTime; if (remaining <= 0) Hide(); }
    }
}
