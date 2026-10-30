using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Animation API. Clothing and materials belong to BBBWolfAppearance.
    public class BBBWolfCharacter : MonoBehaviour
    {
        public enum Face { Happy, Suspicious, Shocked, BlankBluff }
        public enum Action
        {
            Locomotion, Walk, RaiseArms, Wave, Sit, Bid,
            MixamoWalking, Defeat, HandRaising, Flair, HipHopDancing, Jump,
            GolfDrive, JumpingJacks, LayingPose, HurricaneKick
        }

        public const string ActionParameter = "Action";
        public const string ExpressionParameter = "Expression";
        public const string MovementParameter = "MoveSpeed";
        private static readonly int ActionId = Animator.StringToHash(ActionParameter);
        private static readonly int ExpressionId = Animator.StringToHash(ExpressionParameter);
        private static readonly int MovementId = Animator.StringToHash(MovementParameter);
        // The controller enters actions from Any State on "Action == n". Left at n, an action would restart
        // as soon as it ends (Jump even every frame), and Action == 0 pulls any pose back to Locomotion.
        // Once an action has started the parameter is parked on Hold, which no transition matches, so the
        // action plays out and its own exit returns to Locomotion.
        private const int Hold = -1;
        private bool holdPending;
        // The Expression layer overrides at full weight with Write Defaults off, so the Animator keeps the last
        // bone positions an action set (Flair lowers the Armature, LayingPose the Hips) and writes them again
        // in the next action and after it. Once an action ends, or before another replaces it, the body goes
        // back to its bind pose and the Animator is rebound, which drops those held values.
        private Transform[] bones;
        private Vector3[] bindPositions;
        private Quaternion[] bindRotations;
        // An action has played since the last rebind.
        private bool posed;
        private bool seated;

        public Transform model;
        public Animator animator;
        public BBBWolfAppearance appearance;
        public GameObject paddle;
        public Face expression = Face.BlankBluff;
        [Min(0)] public float speed = 1;

        // 표정을 설정하고 애니메이터 파라미터에 반영
        public void SetExpression(Face value)
        {
            expression = value;
            if (animator) animator.SetInteger(ExpressionId, (int)value);
        }

        // 이동 블렌드 값을 0~1로 설정
        // Normalized speed: 0 = idle, 1 = walk. Gameplay owns actual movement.
        public void SetMovementSpeed(float value)
        {
            if (animator) animator.SetFloat(MovementId, Mathf.Clamp01(value));
        }

        // The action last asked for (Locomotion while seated).
        public Action Current { get; private set; }

        // 재생 속도를 설정하고 지정 액션 재생 요청
        public void PlayAction(Action value)
        {
            if (!animator) return;
            if (value != Action.Locomotion)
            {
                // Switching straight from one action to another: clear what the first one left.
                if (posed) ResetBody();
                posed = true;
            }
            Current = value;
            animator.speed = speed;
            animator.SetInteger(ActionId, (int)value);
            holdPending = value != Action.Locomotion;
        }

        // 기본 이동 상태로 액션 되돌리기
        public void StopAction() => PlayAction(Action.Locomotion);

        // 앉은 자세 또는 기본 이동 상태로 즉시 전환
        // Chair-seated pose, switched instantly with no sit-down or stand-up motion.
        public void SetSeated(bool seated)
        {
            this.seated = seated;
            if (!animator) return;
            holdPending = false; Current = Action.Locomotion;
            animator.SetInteger(ActionId, seated ? Hold : (int)Action.Locomotion);
            animator.Play(seated ? "Seated" : "Locomotion", 0, 0);
        }

        // 애니메이션 재생 일시정지 또는 재개
        public void SetPlaybackPaused(bool paused)
        {
            if (animator) animator.speed = paused ? 0 : speed;
        }

        // 현재 애니메이션 상태를 지정한 정규화 시간으로 이동
        // Sandbox scrubbing also uses Animator, never SampleAnimation.
        public void Seek(float normalizedTime)
        {
            if (!animator || !animator.isInitialized) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Mathf.Clamp01(normalizedTime));
            animator.Update(0);
        }

        // 패들 오브젝트 표시 여부 설정
        public void SetPaddleVisible(bool visible)
        {
            if (paddle) paddle.SetActive(visible);
        }

        // 애니메이터·외형 참조를 찾고 패들을 숨기며 재생 속도와 표정 초기화
        private void Awake()
        {
            if (!animator && model) animator = model.GetComponent<Animator>();
            if (!appearance) appearance = GetComponent<BBBWolfAppearance>();
            SetPaddleVisible(false);
            if (animator)
            {
                animator.speed = speed;
                SetExpression(expression);
                bones = animator.GetComponentsInChildren<Transform>(true);
                bindPositions = new Vector3[bones.Length];
                bindRotations = new Quaternion[bones.Length];
                for (int i = 0; i < bones.Length; i++) { bindPositions[i] = bones[i].localPosition; bindRotations[i] = bones[i].localRotation; }
            }
        }

        // 요청한 액션으로 전환이 시작되면 파라미터를 Hold로 돌려 재시작을 막음
        private void Update()
        {
            if (holdPending && animator && animator.isInitialized && animator.IsInTransition(0))
            {
                animator.SetInteger(ActionId, Hold);
                holdPending = false;
            }
        }

        // 액션이 끝나 기본 이동·앉은 자세로 돌아오면 애니메이터를 다시 바인드하고, 그 자세에서는 뼈 위치를 원래대로 유지
        // Locomotion and Seated animate no bone position other than the bind one, so the overwrite loses nothing.
        private void LateUpdate()
        {
            if (!animator || bones == null || !animator.isInitialized || animator.IsInTransition(0)) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Locomotion") && !state.IsName("Seated")) return;
            if (posed) { ResetBody(); return; }
            for (int i = 0; i < bones.Length; i++) if (bones[i]) bones[i].localPosition = bindPositions[i];
        }

        // 뼈를 원래 자세로 되돌린 뒤 애니메이터를 다시 바인드해 이전 액션이 남긴 값을 지우고, 표정·착석 상태를 다시 적용
        // Rebind takes the current transforms as defaults, so the bind pose is put back first.
        // It also resets parameters and states: the expression is set again and a seated body sits again.
        private void ResetBody()
        {
            posed = false;
            if (bones == null || !animator.isActiveAndEnabled) return;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i]) { bones[i].localPosition = bindPositions[i]; bones[i].localRotation = bindRotations[i]; }
            animator.Rebind();
            animator.speed = speed;
            animator.SetInteger(ExpressionId, (int)expression);
            if (seated) { animator.SetInteger(ActionId, Hold); animator.Play("Seated", 0, 0); }
        }

        // 비활성화 시 패들 숨김
        private void OnDisable() => SetPaddleVisible(false);
    }
}
