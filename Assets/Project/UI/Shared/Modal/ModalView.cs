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

        public virtual void Show(string title, string body, string cause = "")
        {
            BuyBidBlow.Audio.UIAudioFeedback.Bind(transform);
            if (!gameObject.activeSelf) previousSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            Title.text = title ?? ""; Body.text = body ?? "";
            if (Cause) Cause.text = cause ?? "";
            if (CauseArea) CauseArea.SetActive(!string.IsNullOrEmpty(cause));
            gameObject.SetActive(true); openedFrame = Time.frameCount; SelectFirst(); OpenChanged?.Invoke(true);
        }
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
        public void SetButtonEnabled(int index, bool value) { Buttons[index].interactable = value; if (IsOpen) SelectFirst(); }
        public virtual void Hide()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            if (EventSystem.current && previousSelection && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null; OpenChanged?.Invoke(false);
        }
        public void Submit(int index)
        {
            if (index >= 0 && (index >= Buttons.Length || !Buttons[index].gameObject.activeSelf || !Buttons[index].interactable)) return;
            BuyBidBlow.Audio.GameAudio.Play(index<0?"ui.close":"ui.confirm",this);
            if (CloseOnAction) Hide();
            ActionRequested?.Invoke(index);
        }
        public void Cancel() { if (EscapeEnabled) Submit(EscapeAction); }
        public void MoveSelection(int direction)
        {
            if (Buttons == null || Buttons.Length == 0) return;
            for (int n = 1; n <= Buttons.Length; n++)
            {
                int i = (selected + direction * n + Buttons.Length * 2) % Buttons.Length;
                if (Buttons[i].gameObject.activeSelf && Buttons[i].interactable) { selected = i; Focus(); return; }
            }
        }
        private void SelectFirst()
        {
            selected = Array.FindIndex(Buttons, b => b && b.gameObject.activeSelf && b.interactable);
            if (selected >= 0) Focus(); else if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        private void Focus() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(Buttons[selected].gameObject); }
        protected virtual void Update()
        {
            if(BuyBidBlow.UI.Menu.GameMenuController.BlocksInput)return;
            if (Time.frameCount == openedFrame) return;
            var k = Keyboard.current; if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame) { Cancel(); return; }
            if (k.leftArrowKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) MoveSelection(-1);
            else if (k.rightArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) MoveSelection(1);
            if ((k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) && selected >= 0) Submit(selected);
        }
    }
}
