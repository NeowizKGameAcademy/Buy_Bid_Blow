using UnityEngine;
using UnityEngine.EventSystems;

namespace BuyBidBlow.UI.Lobby
{
    // 누르고 있는 동안 Held가 true인 버튼. 팝업이 매 프레임 읽어 미리보기 모델을 회전한다.
    public sealed class CustomizeRotateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }

        // 눌림 시작
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Held = true;
        }

        // 손을 떼면 회전 중지
        public void OnPointerUp(PointerEventData eventData)
        {
            Held = false;
        }

        // 버튼 밖으로 나가면 회전 중지
        public void OnPointerExit(PointerEventData eventData)
        {
            Held = false;
        }

        // 팝업이 닫히는 등 비활성화되면 눌림 해제
        private void OnDisable()
        {
            Held = false;
        }
    }
}
