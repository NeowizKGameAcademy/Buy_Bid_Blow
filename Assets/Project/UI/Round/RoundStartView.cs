using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Round
{
    public sealed class RoundStartView : MonoBehaviour
    {
        public Text RoundLabel, Detail;
        private float remaining;
        // 라운드 시작 안내를 표시하고 자동 숨김 타이머 설정
        public void Show(int round, int total, bool auction, float seconds = 3)
        {
            RoundLabel.text = $"{round} / {total} ROUND"; Detail.text = auction ? "경매 라운드 · 공개 경매가 시작됩니다" : "거래를 시작하세요";
            remaining = Mathf.Max(.1f, seconds); gameObject.SetActive(true);
        }
        // 안내 숨김
        public void Hide() => gameObject.SetActive(false);
        // 남은 시간이 지나면 안내 숨김
        private void Update() { remaining -= Time.unscaledDeltaTime; if (remaining <= 0) Hide(); }
    }
}
