using System.Collections;
using System.Collections.Generic;
using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using BuyBidBlow.UI.Ticker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BuyBidBlow.Presentation
{
    /// <summary>
    /// Client-only guided view of the exchange hall.  The five shot transforms live below this object,
    /// so designers can adjust the route directly in the Scene view without touching this script.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapIntroCutscene : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f)] float exchangeDuration = 5f;
        [SerializeField, Min(0.1f)] float auctionDuration = 5f;
        [Tooltip("0이면 영상 전체 길이를 재생합니다.")]
        [SerializeField, Min(0f)] float monitorVideoDuration = 0f;
        [Tooltip("Monitor Video Duration이 0일 때 사용할 영상 길이입니다.")]
        [SerializeField, Min(.1f)] float fullWelcomeVideoDuration = 15f;
        [SerializeField, Min(0f)] float transitionFadeDuration = .3f;
        [SerializeField, Min(0f)] float letterboxHeight = 120f;

        [Header("Copy")]
        [SerializeField] TMP_FontAsset captionFont;
        [TextArea] [SerializeField] string exchangeDescription = "주식을 사고팔며 가격 변동을 확인할 수 있습니다.";
        [TextArea] [SerializeField] string auctionDescription = "원하는 정보와 아이템에 입찰할 수 있습니다.";

        Transform exchangeStart;
        Transform exchangeEnd;
        Transform auctionStart;
        Transform auctionEnd;
        Transform monitor;
        CanvasGroup overlay;
        GameObject overlayRoot;
        TMP_Text title;
        TMP_Text description;
        GameNetworkManager room;
        Coroutine intro;
        readonly List<GameObject> hiddenScreenUi = new List<GameObject>();
        VideoPlayer welcomePlayer;
        RenderTexture welcomeTexture;
        bool welcomeFinished;

        public float SequenceDuration => exchangeDuration + auctionDuration
            + (monitorVideoDuration > 0 ? monitorVideoDuration : fullWelcomeVideoDuration)
            + transitionFadeDuration * 8f + .1f;

        void Start()
        {
            exchangeStart = transform.Find("ExchangeShot_Start");
            exchangeEnd = transform.Find("ExchangeShot_End");
            auctionStart = transform.Find("AuctionShot_Start");
            auctionEnd = transform.Find("AuctionShot_End");
            monitor = transform.Find("Monitor Shot");
            if (!HasAllShots())
            {
                Debug.LogError("Map intro needs ExchangeShot_Start, ExchangeShot_End, AuctionShot_Start, AuctionShot_End, and Monitor Shot.", this);
                enabled = false;
                return;
            }
            room = Mirror.NetworkManager.singleton as GameNetworkManager;
            if (!room) { Debug.LogError("Map intro needs GameNetworkManager.", this); enabled = false; return; }
            room.ClientMatchIntroStarted += BeginIntro;
            room.ClientMatchReturned += RestoreScreenUi;
            room.ClientMatchStarted += RestoreGameplay;
            PlayerNet.LocalMatchIntroStarted += BeginIntro;
            if (room.ClientInIntro) BeginIntro();
        }

        void OnDestroy()
        {
            if (room) { room.ClientMatchIntroStarted -= BeginIntro; room.ClientMatchReturned -= RestoreScreenUi; room.ClientMatchStarted -= RestoreGameplay; }
            PlayerNet.LocalMatchIntroStarted -= BeginIntro;
            TickerBoardDisplay.IntroTexture = null;
            RestoreScreenUi();
        }

        void BeginIntro()
        {
            if (intro == null) intro = StartCoroutine(PlayWhenLocalCameraIsReady());
        }

        bool HasAllShots() => exchangeStart && exchangeEnd && auctionStart && auctionEnd && monitor;

        IEnumerator PlayWhenLocalCameraIsReady()
        {
            var view = Camera.main;
            if (!view) { Debug.LogError("Map intro needs the Lobby Fixed Camera.", this); yield break; }
            var cameraTransform = view.transform;
            var savedPosition = cameraTransform.position;
            var savedRotation = cameraTransform.rotation;
            TickerBoardDisplay.IntroBlank = true;
            HideScreenUi();
            CreateOverlay();
            yield return Fade(0f, 1f, transitionFadeDuration);

            cameraTransform.SetPositionAndRotation(exchangeStart.position, exchangeStart.rotation);
            SetCaption("주식 거래소", exchangeDescription);
            yield return Fade(1f, 0f, transitionFadeDuration);
            yield return MoveCamera(cameraTransform, exchangeStart, exchangeEnd, exchangeDuration);

            yield return Fade(0f, 1f, transitionFadeDuration);
            cameraTransform.SetPositionAndRotation(auctionStart.position, auctionStart.rotation);
            SetCaption("경매장", auctionDescription);
            yield return Fade(1f, 0f, transitionFadeDuration);
            yield return MoveCamera(cameraTransform, auctionStart, auctionEnd, auctionDuration);

            yield return Fade(0f, 1f, transitionFadeDuration);
            cameraTransform.SetPositionAndRotation(monitor.position, monitor.rotation);
            SetCaption(string.Empty, string.Empty);
            yield return Fade(1f, 0f, transitionFadeDuration);
            yield return PlayWelcomeVideo();

            yield return Fade(0f, 1f, transitionFadeDuration);
            cameraTransform.SetPositionAndRotation(savedPosition, savedRotation);
            yield return Fade(1f, 0f, transitionFadeDuration);
            Destroy(overlayRoot);
            var local = Mirror.NetworkClient.localPlayer ? Mirror.NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            if (local) local.CmdFinishMatchIntro();
            intro = null;
        }

        IEnumerator PlayWelcomeVideo()
        {
            var clip = Resources.Load<VideoClip>("Video/Welcome");
            if (!clip) { Debug.LogError("Missing Resources/Video/Welcome.mp4.", this); yield return new WaitForSecondsRealtime(15f); yield break; }
            var playerObject = new GameObject("Map intro Welcome player");
            welcomePlayer = playerObject.AddComponent<VideoPlayer>();
            welcomeTexture = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "Welcome intro video" };
            var previousTarget = RenderTexture.active;
            RenderTexture.active = welcomeTexture; GL.Clear(false, true, Color.black);
            RenderTexture.active = previousTarget;
            welcomePlayer.playOnAwake = false; welcomePlayer.isLooping = false; welcomePlayer.skipOnDrop = true;
            welcomePlayer.renderMode = VideoRenderMode.RenderTexture; welcomePlayer.targetTexture = welcomeTexture;
            welcomePlayer.audioOutputMode = VideoAudioOutputMode.None; welcomePlayer.clip = clip;
            welcomePlayer.loopPointReached += _ => welcomeFinished = true;
            TickerBoardDisplay.IntroTexture = welcomeTexture;
            welcomePlayer.Prepare();
            for (float waited = 0; !welcomePlayer.isPrepared && waited < 5f; waited += Time.unscaledDeltaTime) yield return null;
            if (welcomePlayer.isPrepared)
            {
                welcomeFinished = false;
                welcomePlayer.Play();
                float duration = monitorVideoDuration > 0 ? monitorVideoDuration : (float)clip.length;
                for (float elapsed = 0; !welcomeFinished && elapsed < duration; elapsed += Time.unscaledDeltaTime) yield return null;
            }
            else yield return new WaitForSecondsRealtime(15f);
            TickerBoardDisplay.IntroTexture = null;
            welcomePlayer.Stop(); Destroy(playerObject); welcomeTexture.Release(); Destroy(welcomeTexture); welcomePlayer = null; welcomeTexture = null;
        }

        void HideScreenUi()
        {
            hiddenScreenUi.Clear();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (canvas.renderMode != RenderMode.WorldSpace && canvas.gameObject.activeInHierarchy)
                { hiddenScreenUi.Add(canvas.gameObject); canvas.gameObject.SetActive(false); }
        }

        void RestoreScreenUi()
        {
            foreach (var ui in hiddenScreenUi) if (ui) ui.SetActive(true);
            hiddenScreenUi.Clear();
        }

        // HallSession also enables these components.  Doing it here as well makes
        // the handoff safe if the match-start network message arrives before its UI.
        void RestoreGameplay()
        {
            TickerBoardDisplay.IntroBlank = false;
            RestoreScreenUi();
            var local = Mirror.NetworkClient.localPlayer ? Mirror.NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            if (!local) return;
            local.SetGameplayActive(true);
            if (local.TryGetComponent<UnityEngine.InputSystem.PlayerInput>(out var input)) input.enabled = true;
        }

        IEnumerator MoveCamera(Transform cameraTransform, Transform from, Transform to, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cameraTransform.SetPositionAndRotation(Vector3.Lerp(from.position, to.position, t), Quaternion.Slerp(from.rotation, to.rotation, t));
                yield return null;
            }
            cameraTransform.SetPositionAndRotation(to.position, to.rotation);
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f) { overlay.alpha = to; yield break; }
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                overlay.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            overlay.alpha = to;
        }

        void SetCaption(string heading, string body)
        {
            title.text = string.IsNullOrEmpty(heading) ? string.Empty : heading + "  -  " + body;
            description.text = string.Empty;
        }

        void CreateOverlay()
        {
            var root = overlayRoot = new GameObject("Map Intro UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            var fadeNode = UiNode(root.transform, "Fade", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fade = fadeNode.gameObject.AddComponent<Image>();
            fade.color = Color.black;
            overlay = fadeNode.gameObject.AddComponent<CanvasGroup>();
            overlay.blocksRaycasts = false;

            Letterbox(root.transform, "Letterbox Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -letterboxHeight * .5f));
            Letterbox(root.transform, "Letterbox Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, letterboxHeight * .5f));

            var panel = UiNode(root.transform, "Caption", new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, letterboxHeight * .5f), new Vector2(1500f, 50f));
            title = Text(panel, "Title", Vector2.zero, 24, FontStyles.Bold);
            description = Text(panel, "Description", Vector2.zero, 1, FontStyles.Normal);
            description.gameObject.SetActive(false);
        }

        static RectTransform UiNode(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            node.SetParent(parent, false);
            node.anchorMin = anchorMin; node.anchorMax = anchorMax; node.anchoredPosition = position; node.sizeDelta = size;
            return node;
        }

        void Letterbox(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
        {
            var bar = UiNode(parent, name, anchorMin, anchorMax, position, new Vector2(0f, letterboxHeight));
            bar.gameObject.AddComponent<Image>().color = Color.black;
        }

        TMP_Text Text(RectTransform parent, string name, Vector2 position, float size, FontStyles style)
        {
            var node = UiNode(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(1500f, 55f));
            var text = node.gameObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.font = captionFont ? captionFont : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            return text;
        }
    }
}
