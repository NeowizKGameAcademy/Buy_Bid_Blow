using System;
using System.Collections;
using System.Collections.Generic;
using BuyBidBlow.Systems.Account;
using BuyBidBlow.UI.Lobby;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuyBidBlow.Sandbox.Login
{
    // 로그인·회원가입 패널. 참조는 LoginSandboxBuilder가 연결하고, 줄 배치는 모드에 따라 여기서 정한다.
    // 계정 처리는 IAccountService만 거친다(구현 선택은 AccountBackend).
    public sealed class LoginPanel : MonoBehaviour
    {
        enum Mode { SignIn, SignUp }

        public RectTransform panel;
        public Button signInTab, signUpTab;
        public Sprite tabNormal, tabActive;
        public RectTransform idRow, passwordRow, confirmRow, nicknameRow;
        public InputField loginId, password, passwordConfirm, nickname;
        public Button submit;
        public Text submitLabel, status, footer;
        public string nextScene = "SC_MainMenu";
        public float nextSceneDelay = .8f;

        static readonly Color InfoColor = new Color(1f, .95f, .77f), ErrorColor = new Color(1f, .56f, .46f), SuccessColor = new Color(.62f, 1f, .66f);
        const float TabsBottom = -132, RowGap = 14, SectionGap = 14, BottomPadding = 70;

        Mode mode;
        bool busy;
        IAccountService service;

        void Awake()
        {
            signInTab.onClick.AddListener(() => SetMode(Mode.SignIn));
            signUpTab.onClick.AddListener(() => SetMode(Mode.SignUp));
            submit.onClick.AddListener(Submit);
            loginId.characterLimit = AccountRules.LoginIdMax;
            loginId.onValidateInput += (text, index, c) => AccountRules.IsLoginIdChar(c) ? c : '\0';
            password.characterLimit = passwordConfirm.characterLimit = AccountRules.PasswordMax;
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            BindTextInput();
            SetMode(Mode.SignIn);
            Connect();
            Focus(loginId);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || busy) return;
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Submit();
            else if (keyboard.tabKey.wasPressedThisFrame) MoveFocus(keyboard.shiftKey.isPressed ? -1 : 1);
        }

        void Connect()
        {
            service = AccountBackend.Create(out var target, out var error);
            if (footer) footer.text = service != null ? $"Login Sandbox · DB {target}" : "Login Sandbox · DB 설정 필요";
            if (service == null) { Show(error, ErrorColor); Debug.LogWarning("[Login] " + error); }
        }

        void SetMode(Mode value)
        {
            if (busy) return;
            mode = value;
            bool signUp = mode == Mode.SignUp;
            confirmRow.gameObject.SetActive(signUp);
            nicknameRow.gameObject.SetActive(signUp);
            ((Image)signInTab.targetGraphic).sprite = signUp ? tabNormal : tabActive;
            ((Image)signUpTab.targetGraphic).sprite = signUp ? tabActive : tabNormal;
            submitLabel.text = signUp ? "가입하기" : "로그인";
            Placeholder(loginId, signUp ? $"영문·숫자·_ {AccountRules.LoginIdMin}~{AccountRules.LoginIdMax}자" : "아이디");
            Placeholder(password, signUp ? $"{AccountRules.PasswordMin}자 이상" : "비밀번호");
            password.text = passwordConfirm.text = "";
            if (service != null) Show("", InfoColor);
            Layout();
        }

        // 빌더가 씬에 저장할 초기 모습(로그인 모드)을 만들 때 쓴다.
        public void ApplySignInLayout()
        {
            confirmRow.gameObject.SetActive(false); nicknameRow.gameObject.SetActive(false);
            Layout();
        }

        // 보이는 줄을 위에서부터 쌓고 패널 높이를 맞춘다(패널·자식 모두 위쪽 기준).
        void Layout()
        {
            float y = TabsBottom - 18;
            foreach (var row in new[] { idRow, passwordRow, confirmRow, nicknameRow })
            {
                if (!row.gameObject.activeSelf) continue;
                row.anchoredPosition = new Vector2(0, y);
                y -= row.sizeDelta.y + RowGap;
            }
            y -= SectionGap;
            var submitRect = (RectTransform)submit.transform;
            submitRect.anchoredPosition = new Vector2(0, y);
            y -= submitRect.sizeDelta.y + 12;
            status.rectTransform.anchoredPosition = new Vector2(0, y);
            y -= status.rectTransform.sizeDelta.y;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, -y + BottomPadding);
        }

        async void Submit()
        {
            if (busy) return;
            if (service == null) { Connect(); if (service == null) return; }
            bool signUp = mode == Mode.SignUp;
            if (signUp && password.text != passwordConfirm.text) { Show("비밀번호 확인이 일치하지 않습니다.", ErrorColor); Focus(passwordConfirm); return; }

            SetBusy(true);
            Show(signUp ? "가입 처리 중…" : "확인 중…", InfoColor);
            AccountResult result;
            try
            {
                result = signUp
                    ? await service.SignUpAsync(loginId.text, password.text, nickname.text)
                    : await service.SignInAsync(loginId.text, password.text);
            }
            catch (Exception e) { result = AccountResult.Fail(AccountError.Unavailable, "처리 중 오류가 발생했습니다.", e.ToString()); }
            if (!this) return; // 기다리는 동안 씬이 바뀜

            if (!string.IsNullOrEmpty(result.Detail)) Debug.LogWarning("[Login] " + result.Detail);
            if (!result.Success)
            {
                SetBusy(false); Show(result.Message, ErrorColor);
                Focus(result.Error == AccountError.WrongCredentials ? password : loginId);
                return;
            }

            var profile = result.Profile;
            if (signUp)
            {
                Debug.Log($"[Login] SIGN UP OK: {profile.LoginId} (#{profile.AccountId}, {profile.Nickname})");
                SetBusy(false); SetMode(Mode.SignIn);
                loginId.text = profile.LoginId;
                Show(result.Message, SuccessColor); Focus(password);
                return;
            }

            Debug.Log($"[Login] SIGN IN OK: {profile.LoginId} (#{profile.AccountId}, {profile.Nickname})");
            LoginSession.Begin(profile);
            Show(result.Message, SuccessColor);
            StartCoroutine(LoadNext());
        }

        IEnumerator LoadNext()
        {
            yield return new WaitForSecondsRealtime(nextSceneDelay);
            if (Application.CanStreamedLevelBeLoaded(nextScene)) { SceneManager.LoadScene(nextScene); yield break; }
            SetBusy(false);
            Show($"'{nextScene}' 씬이 빌드 씬 목록에 없어 이동하지 못했습니다.", ErrorColor);
        }

        void SetBusy(bool value)
        {
            busy = value;
            foreach (var selectable in new Selectable[] { signInTab, signUpTab, loginId, password, passwordConfirm, nickname, submit })
                selectable.interactable = !value;
        }

        void Show(string message, Color color) { status.text = message ?? ""; status.color = color; }

        static void Placeholder(InputField field, string text) { if (field.placeholder is Text label) label.text = text; }

        void MoveFocus(int step)
        {
            var fields = new List<InputField>();
            foreach (var field in new[] { loginId, password, passwordConfirm, nickname }) if (field.gameObject.activeInHierarchy) fields.Add(field);
            var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            int index = fields.FindIndex(f => f.gameObject == current);
            Focus(fields[((index < 0 ? -step : index) + step + fields.Count) % fields.Count]);
        }

        static void Focus(InputField field)
        {
            if (!field || !field.gameObject.activeInHierarchy) return;
            field.Select(); field.ActivateInputField();
        }

        // 한글 IME 조합을 InputField에 전달(로비와 같은 방식).
        static void BindTextInput()
        {
            var eventSystem = EventSystem.current ? EventSystem.current : FindFirstObjectByType<EventSystem>();
            if (!eventSystem) return;
            var bridge = eventSystem.GetComponent<LobbyTextInputBridge>();
            if (!bridge) bridge = eventSystem.gameObject.AddComponent<LobbyTextInputBridge>();
            foreach (var module in eventSystem.GetComponents<BaseInputModule>()) module.inputOverride = bridge;
        }
    }
}
