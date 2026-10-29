using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Sandbox.Login;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuyBidBlow.Dev.Editor
{
    // 로그인 Sandbox 도구. 다른 Sandbox와 달리 거래소 씬을 복사하지 않고 UI 전용 새 씬을 만든다.
    // 1) Create and Open Sandbox: LoginSandbox.unity를 만들고 연다(원본 씬·Build Settings 미변경).
    // 2) Rebuild Login UI: 열린 Sandbox 씬의 로그인 UI를 다시 만든다.
    // 3) Open DB Settings: 저장소 밖의 LoginDb.json을 만들거나 연다.
    // 4) Test DB Connection: Play 없이 DB 접속·테이블 생성·계정 수를 확인한다.
    public static class LoginSandboxBuilder
    {
        public const string Root = "Assets/Project/Dev/Sandbox/Login";
        public const string ScenePath = Root + "/LoginSandbox.unity";
        const string MenuSprites = "Assets/Project/UI/Menu/Sprites/";
        const string ModalSprite = "Assets/Project/UI/Shared/Modal/Art/SP_ModalTeal.asset";
        const string FontRoot = "Assets/Font/public/static/alternative/";
        const string UiName = "Login UI";

        static readonly Color Ivory = new Color(1f, .95f, .77f), Gold = new Color(.95f, .79f, .45f), Brass = new Color(.76f, .58f, .3f), FieldTeal = new Color(.035f, .17f, .16f, .96f);
        static Font bold, regular;
        static Sprite[] states; // Normal, Focused, Pressed, Disabled

        [MenuItem("Tools/Buy Bid Blow/Login/Create and Open Sandbox")]
        public static void Build()
        {
            if (File.Exists(ScenePath))
            {
                if (!Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LOGIN SANDBOX CREATED: " + ScenePath);
        }

        [MenuItem("Tools/Buy Bid Blow/Login/Rebuild Login UI")]
        public static void Rebuild()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) { EditorUtility.DisplayDialog("Login Sandbox", "먼저 Create and Open Sandbox로 LoginSandbox 씬을 여세요.", "확인"); return; }
            var old = GameObject.Find(UiName);
            if (old) Undo.DestroyObjectImmediate(old);
            BuildScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("LOGIN UI REBUILT. 씬을 저장하세요.");
        }

        [MenuItem("Tools/Buy Bid Blow/Login/Open DB Settings")]
        public static void OpenSettings()
        {
            var path = LoginDbSettings.FilePath;
            if (!File.Exists(path)) { LoginDbSettings.WriteTemplate(path); Debug.Log("[Login] DB 설정 파일을 만들었습니다: " + path); }
            EditorUtility.OpenWithDefaultApp(path);
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Tools/Buy Bid Blow/Login/Test DB Connection")]
        public static void TestConnection()
        {
            if (!LoginDbSettings.TryLoad(out var settings, out var error)) { Debug.LogWarning("[Login] " + error); return; }
            try
            {
                EditorUtility.DisplayProgressBar("Login Sandbox", settings.Describe() + " 접속 중…", .5f);
                var result = new MariaDbAccountRepository(settings).Probe();
                Debug.Log($"LOGIN DB OK: {settings.Describe()} · {result}");
            }
            catch (Exception e) { Debug.LogError($"LOGIN DB FAILED: {settings.Describe()}\n{e.GetType().Name}: {e.Message}"); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        static void BuildScene()
        {
            LoadAssets();
            if (!UnityEngine.Object.FindFirstObjectByType<Camera>())
            {
                var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                Undo.RegisterCreatedObjectUndo(cameraObject, "Login Camera");
            }
            if (!UnityEngine.Object.FindFirstObjectByType<EventSystem>())
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "Login EventSystem");
            }

            var ui = new GameObject(UiName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(ui, "Build Login UI");
            ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var root = ui.transform;

            // 배경: 메인 메뉴와 같은 홀 그림 + 어둡게
            var background = Img("Background", root, MenuSprite("BG_MainHall"), 0, 0, 0, 0); Stretch(background.rectTransform);
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fitter.aspectRatio = 1.5f;
            var dim = Img("Dim", root, null, 0, 0, 0, 0); Stretch(dim.rectTransform); dim.color = new Color(0, 0, 0, .35f);
            var shade = Img("Soft Shade", root, MenuSprite("BG_MenuSoftShade"), 1250, 1350, 0, -60); shade.color = new Color(0, 0, 0, .5f);
            var logo = Img("Logo", root, MenuSprite("Logo_Main_v2"), 520, 300, 0, 372); logo.preserveAspect = true;

            // 패널: 공용 모달 액자(9-slice). 위쪽 기준이라 높이는 LoginPanel.Layout이 모드별로 정한다.
            var panelImage = Img("Panel", root, AssetDatabase.LoadAssetAtPath<Sprite>(ModalSprite), 780, 700, 0, 190);
            panelImage.type = Image.Type.Sliced; panelImage.pixelsPerUnitMultiplier = 1.4f; panelImage.raycastTarget = true;
            var panel = panelImage.rectTransform; panel.pivot = new Vector2(.5f, 1);

            var signInTab = Btn("Tab - Sign In", panel, "로그인", 260, 60, -140, -72, 26, out _);
            var signUpTab = Btn("Tab - Sign Up", panel, "회원가입", 260, 60, 140, -72, 26, out _);
            var idRow = Row("Row - Login Id", panel, "아이디", false, out var idField);
            var passwordRow = Row("Row - Password", panel, "비밀번호", true, out var passwordField);
            var confirmRow = Row("Row - Password Confirm", panel, "비밀번호 확인", true, out var confirmField);
            var nicknameRow = Row("Row - Nickname", panel, "닉네임", false, out var nicknameField);
            ((Text)confirmField.placeholder).text = "한 번 더 입력";
            ((Text)nicknameField.placeholder).text = "로비에서 쓸 이름 (최대 12글자)";
            var submit = Btn("Submit", panel, "로그인", 432, 96, 0, -520, 34, out var submitLabel);
            var status = Txt("Status", panel, "", regular, 22, 660, 54, 0, -630, Ivory); Top(status.rectTransform);
            var footer = Txt("Footer", root, "Login Sandbox", regular, 18, 900, 30, 0, 0, new Color(Ivory.r, Ivory.g, Ivory.b, .6f));
            footer.alignment = TextAnchor.LowerLeft;
            var footerRect = footer.rectTransform; footerRect.anchorMin = footerRect.anchorMax = footerRect.pivot = Vector2.zero; footerRect.anchoredPosition = new Vector2(24, 16);

            var login = ui.AddComponent<LoginPanel>();
            login.panel = panel;
            login.signInTab = signInTab; login.signUpTab = signUpTab; login.tabNormal = states[0]; login.tabActive = states[1];
            login.idRow = idRow; login.passwordRow = passwordRow; login.confirmRow = confirmRow; login.nicknameRow = nicknameRow;
            login.loginId = idField; login.password = passwordField; login.passwordConfirm = confirmField; login.nickname = nicknameField;
            login.submit = submit; login.submitLabel = submitLabel; login.status = status; login.footer = footer;
            ((Image)signInTab.targetGraphic).sprite = states[1];
            login.ApplySignInLayout();
            Selection.activeGameObject = ui;
        }

        static void LoadAssets()
        {
            bold = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + "Pretendard-Bold.ttf");
            regular = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + "Pretendard-Regular.ttf") ?? bold;
            var sheet = AssetDatabase.LoadAllAssetsAtPath(MenuSprites + "BTN_MainStates_v3.png").OfType<Sprite>().ToArray();
            states = new[] { "Normal", "Focused", "Pressed", "Disabled" }.Select(n => sheet.FirstOrDefault(s => s.name == n)).ToArray();
            if (states.Any(s => !s)) throw new Exception("BTN_MainStates_v3 스프라이트(Normal/Focused/Pressed/Disabled)를 찾지 못했습니다. Tools/BuyBidBlow/Main Menu 빌더로 임포트 상태를 확인하세요.");
        }

        static Sprite MenuSprite(string file) => AssetDatabase.LoadAllAssetsAtPath(MenuSprites + file + ".png").OfType<Sprite>().FirstOrDefault();

        // 한 줄 = 왼쪽 라벨 + 오른쪽 입력칸(640×70).
        static RectTransform Row(string name, RectTransform panel, string label, bool secret, out InputField field)
        {
            var row = Rect(name, panel, 640, 70, 0, 0); Top(row);
            var text = Txt("Label", row, label, bold, 24, 150, 70, -245, 0, Gold); text.alignment = TextAnchor.MiddleLeft;

            var box = Img("Input", row, null, 470, 64, 85, 0); box.color = FieldTeal; box.raycastTarget = true;
            var outline = box.gameObject.AddComponent<Outline>(); outline.effectColor = Brass; outline.effectDistance = new Vector2(2, -2);
            var value = Txt("Text", box.rectTransform, "", regular, 28, 0, 0, 0, 0, Ivory); Inset(value.rectTransform);
            value.alignment = TextAnchor.MiddleLeft; value.supportRichText = false; value.horizontalOverflow = HorizontalWrapMode.Overflow;
            var hint = Txt("Placeholder", box.rectTransform, label, regular, 24, 0, 0, 0, 0, new Color(Ivory.r, Ivory.g, Ivory.b, .38f)); Inset(hint.rectTransform);
            hint.alignment = TextAnchor.MiddleLeft; hint.fontStyle = FontStyle.Italic;

            field = box.gameObject.AddComponent<InputField>();
            field.targetGraphic = box; field.textComponent = value; field.placeholder = hint;
            field.lineType = InputField.LineType.SingleLine;
            field.contentType = secret ? InputField.ContentType.Password : InputField.ContentType.Standard;
            if (secret) field.asteriskChar = '•';
            field.customCaretColor = true; field.caretColor = Ivory; field.caretWidth = 2;
            field.selectionColor = new Color(Gold.r, Gold.g, Gold.b, .35f);
            var colors = field.colors; colors.normalColor = new Color(.86f, .86f, .86f); colors.highlightedColor = Color.white; colors.selectedColor = Color.white; colors.pressedColor = Color.white; colors.disabledColor = new Color(.55f, .55f, .55f); field.colors = colors;
            return row;
        }

        // 메인 메뉴와 같은 나무 액자 버튼(BTN_MainStates_v3 스프라이트 교체).
        static Button Btn(string name, RectTransform parent, string label, float w, float h, float x, float y, int size, out Text text)
        {
            var image = Img(name, parent, states[0], w, h, x, y); image.raycastTarget = true; Top(image.rectTransform);
            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap; button.targetGraphic = image;
            button.spriteState = new SpriteState { highlightedSprite = states[1], selectedSprite = states[1], pressedSprite = states[2], disabledSprite = states[3] };
            text = Txt("Label", image.rectTransform, label, bold, size, 0, 0, 0, 0, Ivory); Stretch(text.rectTransform);
            return button;
        }

        static RectTransform Rect(string name, Transform parent, float w, float h, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        static Image Img(string name, Transform parent, Sprite sprite, float w, float h, float x, float y)
        {
            var image = Rect(name, parent, w, h, x, y).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false;
            return image;
        }

        static Text Txt(string name, Transform parent, string value, Font font, int size, float w, float h, float x, float y, Color color)
        {
            var text = Rect(name, parent, w, h, x, y).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static void Top(RectTransform rect) { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1); }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero; }
        static void Inset(RectTransform rect) { Stretch(rect); rect.offsetMin = new Vector2(20, 4); rect.offsetMax = new Vector2(-20, -4); }
    }
}
