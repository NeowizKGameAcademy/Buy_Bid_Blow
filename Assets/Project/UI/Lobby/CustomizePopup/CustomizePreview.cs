using BuyBidBlow.Characters;
using UnityEngine;

namespace BuyBidBlow.UI.Lobby
{
    // 커스터마이징 팝업 왼쪽의 미리보기. 로비 방 한쪽(메인 카메라 밖)에 배치한 네트워크와 무관한 늑대를
    // 전용 카메라가 RT_CustomizePreview에 그리고, 팝업의 RawImage가 그 텍스처를 표시한다.
    // 늑대·카메라는 씬의 Wolf Point·Camera Point 아래에 배치돼 있으며 위치는 그 둘로 조정한다.
    public sealed class CustomizePreview : MonoBehaviour
    {
        [SerializeField] private Transform wolf;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private float rotateSpeed = 120;

        private BBBWolfAppearance appearance;

        // 외형 컴포넌트를 찾고, 팝업이 열리기 전까지 모델과 카메라를 꺼 둠
        private void Awake()
        {
            if (wolf) appearance = wolf.GetComponentInChildren<BBBWolfAppearance>(true);
            Hide();
        }

        // 미리보기를 켜고 회전을 정면으로 초기화, 로비 늑대와 같은 대기 동작으로 맞춤
        public void Show()
        {
            if (!wolf || !previewCamera) return;
            wolf.localRotation = Quaternion.identity;
            wolf.gameObject.SetActive(true);
            // Same setup PlayerNet gives the lobby wolves: no root motion, standing idle.
            foreach (var animator in wolf.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
            var character = wolf.GetComponentInChildren<BBBWolfCharacter>(true);
            if (character)
            {
                character.StopAction();
                character.SetMovementSpeed(0);
            }
            previewCamera.enabled = true;
        }

        // 팝업이 닫히면 카메라와 모델을 꺼서 렌더 비용 제거
        public void Hide()
        {
            if (previewCamera) previewCamera.enabled = false;
            if (wolf) wolf.gameObject.SetActive(false);
        }

        // 고른 상의·하의·넥타이 색을 미리보기 모델에 적용
        public void Apply(int top, int bottom, Color tieColor)
        {
            if (!appearance) return;
            appearance.SetOutfit(top, bottom);
            appearance.SetTieColor(tieColor);
        }

        // 방향(-1 왼쪽, 1 오른쪽)으로 프레임 시간만큼 모델 회전
        public void Rotate(float direction)
        {
            if (wolf) wolf.Rotate(0, direction * rotateSpeed * Time.unscaledDeltaTime, 0, Space.Self);
        }
    }
}
