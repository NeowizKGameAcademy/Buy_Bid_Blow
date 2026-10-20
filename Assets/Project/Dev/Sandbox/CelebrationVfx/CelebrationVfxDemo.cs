using BuyBidBlow.Props;
using UnityEngine;

namespace BuyBidBlow.Dev
{
    public sealed class CelebrationVfxDemo : MonoBehaviour
    {
        [SerializeField] private CelebrationVfx[] effects;
        private float nextPlay;
        private bool repeat = true;

        public void Configure(CelebrationVfx[] value) => effects = value;

        private void Update()
        {
            if (!repeat || Time.unscaledTime < nextPlay) return;
            Replay();
        }

        private void Replay()
        {
            foreach (var effect in effects) effect.Play();
            nextPlay = Time.unscaledTime + 7f;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(20, 20, 290, 116), "BUY / BID / BLOW — CELEBRATIONS");
            if (GUI.Button(new Rect(35, 53, 125, 30), "Play all")) Replay();
            repeat = GUI.Toggle(new Rect(175, 58, 120, 25), repeat, "Auto replay");
            GUI.Label(new Rect(35, 95, 260, 25), "Fireworks     /     Fanfare     /     Money");
            for (int i = 0; i < effects.Length; i++)
                if (GUI.Button(new Rect(20 + i * 135, Screen.height - 55, 125, 32), effects[i].name))
                    effects[i].Play();
        }
    }
}
