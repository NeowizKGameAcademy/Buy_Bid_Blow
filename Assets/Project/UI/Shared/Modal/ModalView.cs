using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BuyBidBlow.UI.Shared
{
    // Presentation only. The integrating client decides what each action means.
    public class ModalView : MonoBehaviour
    {
        public Text Title, Body, Cause, Footer;
        public GameObject CauseArea;
        public Button[] Buttons;
        public event Action<int> ActionRequested;
        public event Action<bool> OpenChanged;
        private GameObject previousSelection;
        private int selected;
        private int openedFrame;
        public bool IsOpen => gameObject.activeSelf;
        public bool EscapeEnabled = true;
        public int EscapeAction = -1;
        public bool CloseOnAction = true;
        public bool VerticalButtons;

        // 이전 선택을 기억하고 제목·본문·원인 설정 후 모달 열기
        public virtual void Show(string title, string body, string cause = "")
        {
            if (!gameObject.activeSelf) previousSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            Title.text = title ?? ""; Body.text = body ?? "";
            if (Cause) Cause.text = cause ?? "";
            if (CauseArea) CauseArea.SetActive(!string.IsNullOrEmpty(cause));
            gameObject.SetActive(true); openedFrame = Time.frameCount; SelectFirst(); OpenChanged?.Invoke(true);
        }
        // 라벨 수만큼 버튼을 활성화·이름 지정하고 가로 정렬
        public void SetButtons(params string[] labels)
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                Buttons[i].gameObject.SetActive(i < labels.Length);
                if (i < labels.Length) Buttons[i].GetComponentInChildren<Text>().text = labels[i];
                if (i < labels.Length && !VerticalButtons)
                {
                    var rect = Buttons[i].GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2((i - (labels.Length - 1) / 2f) * (rect.sizeDelta.x + 26), rect.anchoredPosition.y);
                }
            }
        }
        // 버튼 활성 여부 설정 후 첫 선택 갱신
        public void SetButtonEnabled(int index, bool value) { Buttons[index].interactable = value; if (IsOpen) SelectFirst(); }
        // 모달을 닫고 이전 선택 복원
        public virtual void Hide()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            if (EventSystem.current && previousSelection && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null; OpenChanged?.Invoke(false);
        }
        // 유효한 버튼이면 모달을 닫고 해당 동작 요청 이벤트 발생
        public void Submit(int index)
        {
            if (index >= 0 && (index >= Buttons.Length || !Buttons[index].gameObject.activeSelf || !Buttons[index].interactable)) return;
            if (CloseOnAction) Hide();
            ActionRequested?.Invoke(index);
        }
        // Esc 허용 시 지정된 취소 동작 실행
        public void Cancel() { if (EscapeEnabled) Submit(EscapeAction); }
        // 방향에 따라 다음 선택 가능한 버튼으로 이동
        public void MoveSelection(int direction)
        {
            if (Buttons == null || Buttons.Length == 0) return;
            for (int n = 1; n <= Buttons.Length; n++)
            {
                int i = (selected + direction * n + Buttons.Length * 2) % Buttons.Length;
                if (Buttons[i].gameObject.activeSelf && Buttons[i].interactable) { selected = i; Focus(); return; }
            }
        }
        // 첫 번째 선택 가능한 버튼에 포커스
        private void SelectFirst()
        {
            selected = Array.FindIndex(Buttons, b => b && b.gameObject.activeSelf && b.interactable);
            if (selected >= 0) Focus(); else if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        // 현재 선택 버튼을 EventSystem 선택으로 지정
        private void Focus() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(Buttons[selected].gameObject); }
        // 키 입력으로 취소·선택 이동·제출 처리
        protected virtual void Update()
        {
            if (Time.frameCount == openedFrame) return;
            var k = Keyboard.current; if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame) { Cancel(); return; }
            if (k.leftArrowKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) MoveSelection(-1);
            else if (k.rightArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) MoveSelection(1);
            if ((k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) && selected >= 0) Submit(selected);
        }
    }
}
