using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Loading
{
    public sealed class LoadingView : MonoBehaviour
    {
        public Text Title, Detail, ProgressLabel;
        public Image ProgressFill;
        private float? progress;
        // 제목·설명을 설정하고 로딩 화면 표시
        public void Show(string title, string detail, float? value = null)
        {
            Title.text = title; Detail.text = detail; gameObject.SetActive(true); SetProgress(value);
        }
        // 진행률을 설정해 퍼센트 텍스트와 게이지 갱신 (없으면 불확정 표시)
        public void SetProgress(float? value)
        {
            progress = value.HasValue ? Mathf.Clamp01(value.Value) : (float?)null;
            ProgressLabel.text = progress.HasValue ? $"{progress.Value:P0}" : "불러오는 중…";
            if (progress.HasValue) ProgressFill.fillAmount = progress.Value;
        }
        // 진행률이 없을 때 게이지를 왕복 애니메이션
        private void Update() { if (!progress.HasValue) ProgressFill.fillAmount = .15f + Mathf.PingPong(Time.unscaledTime * .25f, .7f); }
        // 로딩 화면 숨김
        public void Hide() => gameObject.SetActive(false);
    }
}
