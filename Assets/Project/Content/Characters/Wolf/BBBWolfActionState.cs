using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Consume requests once and bind the paddle to actual Animator state.
    public sealed class BBBWolfActionState : StateMachineBehaviour
    {
        public bool showPaddle;

        // 상태 진입 시 액션 요청을 소비하고 패들 표시 여부 적용
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetInteger(BBBWolfCharacter.ActionParameter, -1);
            var character = animator.GetComponentInParent<BBBWolfCharacter>();
            if (character) character.SetPaddleVisible(showPaddle);
            var player = animator.GetComponentInParent<PlayerNet>();
            if (character && player && player.IsSeated && stateInfo.IsName("Locomotion"))
                character.SetSeated(true);
        }

    }
}
