using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Loading
{
    public sealed class LoadingView : MonoBehaviour
    {
        public Text Title, Detail, ProgressLabel;
        public Image ProgressFill;
        private float? progress;
        public void Show(string title, string detail, float? value = null)
        {
            Title.text = title; Detail.text = detail; gameObject.SetActive(true); SetProgress(value);
        }
        public void SetProgress(float? value)
        {
            progress = value.HasValue ? Mathf.Clamp01(value.Value) : (float?)null;
            ProgressLabel.text = progress.HasValue ? $"{progress.Value:P0}" : "불러오는 중…";
            if (progress.HasValue) ProgressFill.fillAmount = progress.Value;
        }
        private void Update() { if (!progress.HasValue) ProgressFill.fillAmount = .15f + Mathf.PingPong(Time.unscaledTime * .25f, .7f); }
        public void Hide() => gameObject.SetActive(false);
    }
}
