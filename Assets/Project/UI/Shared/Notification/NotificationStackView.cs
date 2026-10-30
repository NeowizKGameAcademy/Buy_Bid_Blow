using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Shared
{
    public enum NoticeTone { Neutral, Success, Failure }
    public sealed class NotificationStackView : MonoBehaviour
    {
        public Text[] Titles, Details;
        public Image[] Accents;
        public GameObject[] Cards;
        public int MaxQueued = 12;
        private sealed class Notice { public string Title, Detail; public NoticeTone Tone; public float Seconds; }
        private readonly Queue<Notice> waiting = new Queue<Notice>();
        private readonly List<Notice> active = new List<Notice>();
        public int ActiveCount => active.Count;
        public int QueuedCount => waiting.Count;
        // 알림을 대기열에 추가(초과 시 가장 오래된 것 제거)하고 표시 채움
        public void Push(string title, string detail, NoticeTone tone = NoticeTone.Neutral, float seconds = 4)
        {
            if (waiting.Count >= MaxQueued) waiting.Dequeue();
            waiting.Enqueue(new Notice { Title = title, Detail = detail, Tone = tone, Seconds = Mathf.Max(.1f,seconds) }); Fill();
        }
        // 대기·활성 알림을 모두 지우고 갱신
        public void Clear() { waiting.Clear(); active.Clear(); Refresh(); }
        // 비활성화 시 알림 모두 제거
        private void OnDisable() => Clear();
        // 활성 알림 시간을 줄여 만료된 것 제거 후 채움
        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--) { active[i].Seconds -= Time.unscaledDeltaTime; if (active[i].Seconds <= 0) active.RemoveAt(i); }
            Fill();
        }
        // 빈 카드 수만큼 대기 알림을 활성으로 옮기고 갱신
        private void Fill() { while (active.Count < Cards.Length && waiting.Count > 0) active.Add(waiting.Dequeue()); Refresh(); }
        // 활성 알림을 카드에 표시하고 톤별 강조 색 적용
        private void Refresh()
        {
            for (int i = 0; i < Cards.Length; i++)
            {
                Cards[i].SetActive(i < active.Count); if (i >= active.Count) continue;
                var n = active[i]; Titles[i].text = n.Title; Details[i].text = n.Detail;
                Accents[i].color = n.Tone == NoticeTone.Success ? new Color(.48f,.83f,.63f) : n.Tone == NoticeTone.Failure ? new Color(.94f,.48f,.40f) : new Color(.94f,.76f,.39f);
            }
        }
    }
}
