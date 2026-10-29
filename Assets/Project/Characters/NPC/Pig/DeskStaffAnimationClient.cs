using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Personal greeting on every interaction. No private trading data or network state is changed.
    public sealed class DeskStaffAnimationClient : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip greeting;
        public bool IsGreeting { get; private set; }
        float endsAt;

        public void PlayGreeting()
        {
            if (!animator || !greeting) return;
            IsGreeting = true;
            endsAt = Time.time + greeting.length;
            animator.applyRootMotion = false;
            // Explicit time zero also restarts the greeting when reopening mid-animation.
            animator.Play("Base Layer.Greeting", 0, 0f);
        }

        void Update()
        {
            if (!IsGreeting || Time.time < endsAt) return;
            IsGreeting = false;
            animator.CrossFadeInFixedTime("Base Layer.Standing Idle", .12f, 0);
        }
    }
}

