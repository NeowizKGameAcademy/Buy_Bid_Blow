using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Labor;
using BuyBidBlow.Systems.Room;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Lobby;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Labor
{
    // 노동 Sandbox의 씬 진입점: 메시지 핸들러 연결, 내 운반 상태, 임시 IMGUI 표시.
    // 원본 씬에는 이 컴포넌트가 없으므로 노동 기능이 꺼져 있다.
    public sealed class LaborSite : MonoBehaviour
    {
        public TextAsset settingsJson;
        public Font font;
        // 일터에서 홀로 돌아올 지점. Set Selected Door As Entrance 메뉴가 연결한다.
        public Transform hallReturn;

        public static LaborSite Instance { get; private set; }
        // -1이면 빈손, 그 외에는 Company 값.
        public static int Carrying { get; private set; } = -1;
        public static int Earned { get; private set; }

        static string message;
        static float messageUntil;
        static bool messagePaid;

        LaborSettings settings;
        public LaborSettings Settings => settings ??= ParseSettings();
        bool serverHooked, clientHooked;
        GameObject carried;
        GUIStyle panelLabel, toastLabel, soloButton;

        LaborSettings ParseSettings()
        {
            var parsed = settingsJson ? JsonUtility.FromJson<LaborSettings>(settingsJson.text) : new LaborSettings();
            parsed.Validate();
            return parsed;
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (serverHooked && NetworkServer.active) NetworkServer.UnregisterHandler<LaborRequest>();
            if (clientHooked && NetworkClient.active) NetworkClient.UnregisterHandler<LaborReply>();
            serverHooked = clientHooked = false;
            ResetLocal();
        }

        // GameNetworkManager.cs를 고치지 않도록, 서버·클라이언트가 켜지는 것을 보고 핸들러를 붙인다.
        void Update()
        {
            if (NetworkServer.active)
            {
                if (!serverHooked) { NetworkServer.RegisterHandler<LaborRequest>(OnServerRequest); serverHooked = true; }
            }
            else serverHooked = false;

            if (NetworkClient.active)
            {
                if (!clientHooked) { NetworkClient.RegisterHandler<LaborReply>(OnClientReply); clientHooked = true; }
            }
            else if (clientHooked) { clientHooked = false; ResetLocal(); }

            var local = LocalPlayer;
            if (!local || !local.inMatch) { ResetLocal(); return; }
            // 일터 밖으로 떨어지면 도착 지점으로 되돌린다.
            if (local.transform.position.y < LaborLayout.Origin.y - 30)
                Teleport(local.gameObject, LaborLayout.Arrival, Vector3.forward);
        }

        static PlayerNet LocalPlayer =>
            NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;

        // ---- Server ----
        void OnServerRequest(NetworkConnectionToClient conn, LaborRequest request)
        {
            if (NetworkManager.singleton is GameNetworkManager manager)
                manager.HandleLaborRequest(conn, request, Settings);
        }

        // ---- Client ----
        public static void Request(LaborAction action, int material = -1, int floor = 0)
        {
            if (!NetworkClient.isConnected || !NetworkClient.ready) { Show("서버에 연결되어 있지 않습니다."); return; }
            NetworkClient.Send(new LaborRequest { Action = action, Material = material, Floor = floor });
        }

        void OnClientReply(LaborReply reply)
        {
            Earned = reply.Earned;
            SetCarrying(reply.Carrying);
            Show(reply.Message, reply.Pay > 0);
        }

        public static void Show(string text, bool paid = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            message = text;
            messagePaid = paid;
            messageUntil = Time.unscaledTime + 3f;
        }

        void ResetLocal()
        {
            if (Carrying < 0 && Earned == 0 && !carried) return;
            Earned = 0;
            SetCarrying(-1);
        }

        void SetCarrying(int material)
        {
            Carrying = material;
            var local = LocalPlayer;
            if (local && local.TryGetComponent(out PlayerMove move))
                move.SpeedMultiplier = material >= 0 ? Settings.CarrySpeed[material] : 1f;
            if (carried) Destroy(carried);
            carried = null;
            if (material < 0 || !local || !local.TryGetComponent(out PlayerCamera playerCamera)) return;
            carried = CreateLoadVisual((Company)material);
            carried.transform.SetParent(playerCamera.CameraTransform, false);
            carried.transform.localPosition = new Vector3(0, -.55f, .85f);
        }

        // 내 화면에만 보이는 임시 짐. 레이캐스트를 막지 않도록 콜라이더를 지운다.
        static GameObject CreateLoadVisual(Company material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Carried " + material;
            DestroyImmediate(go.GetComponent<Collider>());
            go.layer = 2; // Ignore Raycast
            go.transform.localScale = material == Company.Straw ? new Vector3(.7f, .4f, .45f)
                : material == Company.Timber ? new Vector3(.9f, .18f, .3f) : new Vector3(.5f, .3f, .3f);
            var renderer = go.GetComponent<Renderer>();
            renderer.material.color = LoadColor(material);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static Color LoadColor(Company material) =>
            material == Company.Straw ? new Color(.93f, .8f, .35f)
            : material == Company.Timber ? new Color(.55f, .35f, .18f) : new Color(.7f, .25f, .18f);

        // 내 플레이어는 이동을 직접 소유하므로(클라이언트 권한) 로컬에서 옮기면 서버로 동기화된다.
        public static void Teleport(GameObject player, Vector3 position, Vector3 forward)
        {
            forward.y = 0;
            var rotation = forward.sqrMagnitude > .001f ? Quaternion.LookRotation(forward, Vector3.up) : player.transform.rotation;
            if (player.TryGetComponent(out Rigidbody body))
            {
                body.linearVelocity = Vector3.zero;
                body.position = position;
                body.rotation = rotation;
            }
            player.transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
        }

        // ---- Temporary HUD ----
        void OnGUI()
        {
            if (!NetworkClient.active && !NetworkServer.active) { DrawSoloHost(); return; }
            var local = LocalPlayer;
            if (!local || !local.inMatch) return;
            bool atSite = LaborLayout.IsAtSite(local.transform.position);
            bool toast = Time.unscaledTime < messageUntil;
            if (!atSite && !toast) return;
            EnsureStyles();
            if (atSite)
            {
                string load = Carrying >= 0
                    ? $"운반 중: {LaborBoard.Name((Company)Carrying)} (속도 {Settings.CarrySpeed[Carrying] * 100:0}%)"
                    : "빈손 · 자재 더미에서 E";
                GUI.Box(new Rect(16, 120, 360, 100), GUIContent.none);
                GUI.Label(new Rect(28, 128, 340, 88),
                    $"<b>일터</b>   이번 경기 노동 수입 <b>${Earned}</b>\n{load}\n2~4층 하역장에서 E · 높은 층일수록 보상↑", panelLabel);
            }
            if (toast)
            {
                toastLabel.normal.textColor = messagePaid ? new Color(1f, .85f, .3f) : Color.white;
                GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height * .62f, 600, 44), message, toastLabel);
            }
        }

        // 기본 로비는 전용 서버 참가만 지원한다. Sandbox에서는 이 PC가 서버를 겸하는 호스트로 혼자 시험한다.
        // 노동 처리는 서버 쪽에도 LaborSite가 있어야 하므로 전용 서버로는 시험할 수 없다.
        void DrawSoloHost()
        {
            if (!(NetworkManager.singleton is GameNetworkManager room) || !room.TryGetComponent(out ConnectionController session) || !session.CanConnect) return;
            EnsureStyles();
            if (GUI.Button(new Rect(Screen.width / 2f - 200, 16, 400, 52), "혼자 시험하기 · 이 PC에서 호스트 시작", soloButton))
            {
                var nick = PlayerPrefs.GetString(Nickname.PreferenceKey, "");
                if (string.IsNullOrWhiteSpace(nick)) nick = "WORKER";
                session.Host(nick, DirectConnectionSettings.DefaultPort.ToString());
            }
        }

        void EnsureStyles()
        {
            if (panelLabel != null) return;
            panelLabel = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true, wordWrap = true };
            if (font) panelLabel.font = font;
            toastLabel = new GUIStyle(panelLabel) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            soloButton = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            if (font) soloButton.font = font;
        }
    }
}
