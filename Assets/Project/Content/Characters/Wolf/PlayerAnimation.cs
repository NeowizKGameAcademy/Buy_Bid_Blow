using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Drives the local player's wolf animator from movement, seating and emotions,
    // and forwards each change to PlayerNet so other clients see the same pose.
    public sealed class PlayerAnimation : MonoBehaviour
    {
        // Radial menu order: menu number n plays Emotions[n - 1].
        public static readonly string[] Emotions =
        {
            "Defeat", "Flair", "Hadraise", "HipHopDance", "GolfDrive", "JumpingJacks", "LayingPose", "HurricaneKick"
        };

        public const string IdleState = "Player_Idle";
        public const string WalkingState = "Player_Walking";
        public const string SeatedState = "Seated";

        public static readonly int JumpTrigger = Animator.StringToHash("Jump");
        public static readonly int GroundedId = Animator.StringToHash("isGroud");
        public static readonly int WalkingId = Animator.StringToHash("isWalking");

        [SerializeField] private Animator animator;

        private PlayerMove playerMove;
        private PlayerSeating seating;

        public bool EmotionPlaying { get; private set; }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            TryGetComponent(out playerMove);
            TryGetComponent(out seating);
        }

        private void OnEnable()
        {
            if (playerMove != null)
            {
                playerMove.GroundedChanged += OnGroundedChanged;
                playerMove.Jumped += OnJumped;
                playerMove.MoveInputChanged += OnMoveInputChanged;
                OnGroundedChanged(playerMove.IsGrounded);
                OnMoveInputChanged(playerMove.MoveInput);
            }

            if (seating != null)
                seating.onSeatedChanged.AddListener(ShowSeated);
        }

        private void OnDisable()
        {
            EmotionPlaying = false;
            if (TryGetComponent(out PlayerCamera camera)) camera.SetEmotionView(false);

            if (playerMove != null)
            {
                playerMove.GroundedChanged -= OnGroundedChanged;
                playerMove.Jumped -= OnJumped;
                playerMove.MoveInputChanged -= OnMoveInputChanged;
            }

            if (seating != null)
                seating.onSeatedChanged.RemoveListener(ShowSeated);
        }

        public void PlayEmotion(int menuNumber)
        {
            if (menuNumber < 1 || menuNumber > Emotions.Length)
                return;

            EmotionPlaying = true;
            if (TryGetComponent(out PlayerCamera camera)) camera.SetEmotionView(true);
            SetTrigger(Emotions[menuNumber - 1]);
        }

        // Any movement, jump or interaction ends the emotion and returns to the matching pose.
        public void StopEmotion(bool walking)
        {
            if (!EmotionPlaying)
                return;

            EmotionPlaying = false;
            if (TryGetComponent(out PlayerCamera camera)) camera.SetEmotionView(false);
            ResetEmotions(animator);
            animator.Play(walking ? WalkingState : IdleState, 0, 0f);
            if (TryGetComponent(out PlayerNet net))
                net.CancelEmotion(walking);
        }

        public static void ResetEmotions(Animator target)
        {
            foreach (var emotion in Emotions)
                target.ResetTrigger(emotion);
        }

        private void OnGroundedChanged(bool isGrounded)
        {
            animator.SetBool(GroundedId, isGrounded);
            if (TryGetComponent(out PlayerNet net)) net.SetGroundedAnimation(isGrounded);
        }

        private void OnJumped()
        {
            animator.ResetTrigger(JumpTrigger);
            animator.SetTrigger(JumpTrigger);
            if (TryGetComponent(out PlayerNet net))
                net.PlayAnimationTrigger(JumpTrigger);
        }

        private void OnMoveInputChanged(Vector2 moveInput)
        {
            bool walking = moveInput != Vector2.zero;
            if (walking)
                StopEmotion(true);

            animator.SetBool(WalkingId, walking);
            if (TryGetComponent(out PlayerNet net))
                net.SetWalkingAnimation(walking);
        }

        private void ShowSeated(bool seated)
        {
            animator.SetBool(WalkingId, false);
            animator.Play(seated ? SeatedState : IdleState, 0, 0);
        }

        private void SetTrigger(string trigger)
        {
            animator.SetTrigger(trigger);
            if (TryGetComponent(out PlayerNet net))
                net.PlayAnimationTrigger(trigger);
        }
    }
}
