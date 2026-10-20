using UnityEngine;

namespace BuyBidBlow.Props
{
    /// <summary>Reusable local celebration. Call Play again to restart, or Stop to return to a pool.</summary>
    [DisallowMultipleComponent]
    public sealed class CelebrationVfx : MonoBehaviour
    {
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool destroyWhenFinished;
        private ParticleSystem[] particles;
        private bool playing;

        public bool IsPlaying => playing;

        private void Awake() => particles = GetComponentsInChildren<ParticleSystem>(true);

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        [ContextMenu("Play")]
        public void Play()
        {
            if (particles == null) particles = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particle in particles)
            {
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play(false);
            }
            playing = true;
        }

        [ContextMenu("Stop")]
        public void Stop()
        {
            if (particles != null)
                foreach (var particle in particles)
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            playing = false;
        }

        private void OnDisable() => Stop();

        private void Update()
        {
            if (!playing) return;
            foreach (var particle in particles)
                if (particle.IsAlive(false)) return;
            playing = false;
            if (destroyWhenFinished) Destroy(gameObject);
        }
    }
}
