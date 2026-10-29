using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Room;
using BuyBidBlow.Systems.Shop;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Notebook;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Sandbox.Shop
{
    // 상점 Sandbox의 씬 진입점: 메시지 핸들러 연결, 내 가방·목줄 상태, 목줄 이동 제한, 임시 IMGUI(상점창·아이템창).
    // 원본 씬에는 이 컴포넌트가 없으므로 상점 기능이 꺼져 있다.
    public sealed class ShopSite : MonoBehaviour
    {
        public TextAsset settingsJson;
        public Font font;
        // 상점 판매대. 서버는 이 위치로 구매·판매 거리를 검증한다. Build Shop 메뉴가 연결한다.
        public Transform counter;
        // 판매대에서 이 거리(m) 안에서만 사고팔 수 있다. 벗어나면 상점창이 닫힌다.
        public float shopReach = 4f;

        public static ShopSite Instance { get; private set; }
        // 접속한 서버가 상점 기능을 지원한다고 알려왔는지. 연결이 끊기면 false.
        public static bool ServerReady { get; private set; }

        ShopSettings settings;
        public ShopSettings Settings => settings ??= ParseSettings();

        // ---- 내 상태(클라이언트) ----
        int[] bag;
        bool shopOpen, inventoryOpen;
        int armedSlot = -1;                                      // 목줄을 골라 대상 지정 중인 칸
        readonly Dictionary<uint, uint> leashes = new Dictionary<uint, uint>(); // 묶인 사람 netId → 채운 사람 netId
        MatchNet lastMatch;
        string message;
        float messageUntil;
        bool messageGood;

        // ---- 서버 ----
        bool serverHooked, clientHooked;
        readonly HashSet<int> announced = new HashSet<int>();
        ShopLeashState lastSent;

        readonly List<LineRenderer> ropes = new List<LineRenderer>();
        Material ropeMaterial;
        GUIStyle label, small, title, toastLabel, button, cell, soloButton;

        ShopSettings ParseSettings()
        {
            var parsed = settingsJson ? JsonUtility.FromJson<ShopSettings>(settingsJson.text) : new ShopSettings();
            parsed.Validate();
            return parsed;
        }

        void OnEnable()
        {
            Instance = this;
            NetworkNotebookController.SandboxBlocksInput = PanelOpen;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (NetworkNotebookController.SandboxBlocksInput == PanelOpen) NetworkNotebookController.SandboxBlocksInput = null;
            if (serverHooked && NetworkServer.active) NetworkServer.UnregisterHandler<ShopRequest>();
            if (clientHooked && NetworkClient.active)
            {
                NetworkClient.UnregisterHandler<ShopReply>();
                NetworkClient.UnregisterHandler<ShopAvailable>();
                NetworkClient.UnregisterHandler<ShopLeashState>();
            }
            serverHooked = clientHooked = false;
            ServerReady = false;
            ResetLocal();
            foreach (var rope in ropes) if (rope) Destroy(rope.gameObject);
            ropes.Clear();
        }

        bool PanelOpen() => shopOpen || inventoryOpen;

        // GameNetworkManager.cs를 고치지 않도록, 서버·클라이언트가 켜지는 것을 보고 핸들러를 붙인다.
        void Update()
        {
            ServerUpdate();

            if (NetworkClient.active)
            {
                if (!clientHooked)
                {
                    NetworkClient.RegisterHandler<ShopReply>(OnClientReply);
                    NetworkClient.RegisterHandler<ShopAvailable>(_ => ServerReady = true);
                    NetworkClient.RegisterHandler<ShopLeashState>(OnLeashState);
                    clientHooked = true;
                }
            }
            else if (clientHooked) { clientHooked = false; ServerReady = false; ResetLocal(); }

            var local = LocalPlayer;
            var match = MatchNet.Current;
            if (match != lastMatch) { lastMatch = match; ResetLocal(); }
            if (!local || !local.inMatch || !match || match.phase == MatchNet.Phase.Finished)
            {
                CloseAll();
                armedSlot = -1;
                if (!local || !local.inMatch) leashes.Clear();
                UpdateRopes();
                return;
            }

            HandleKeys(local);
            if (PanelOpen() && local.TryGetComponent(out PlayerMove move))
            {
                move.SetMoveInput(Vector2.zero);
                move.SetJumpHeld(false);
            }
            if (shopOpen && counter && Vector3.Distance(local.transform.position, counter.position) > shopReach + 1f) shopOpen = false;
            UpdateRopes();
        }

        void FixedUpdate() => ApplyLeash();
        void LateUpdate() => ApplyLeash();

        static PlayerNet LocalPlayer =>
            NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;

        // ---- Server ----
        void ServerUpdate()
        {
            if (!NetworkServer.active) { serverHooked = false; return; }
            if (!serverHooked) { NetworkServer.RegisterHandler<ShopRequest>(OnServerRequest); serverHooked = true; announced.Clear(); lastSent = default; }
            if (!(NetworkManager.singleton is GameNetworkManager manager)) return;

            manager.ShopServerTick(Settings);
            var state = manager.ShopLeashSnapshot();
            bool changed = !SameState(state, lastSent);
            if (changed) lastSent = state;

            foreach (var conn in NetworkServer.connections.Values)
            {
                if (!conn.identity || !conn.identity.TryGetComponent<PlayerNet>(out var player) || !player.inMatch) continue;
                bool fresh = announced.Add(conn.connectionId);
                if (fresh) conn.Send(new ShopAvailable());
                if (fresh || changed) conn.Send(state);
            }
            // 경기에서 빠진 연결은 다음 경기에 다시 알린다.
            announced.RemoveWhere(id => !NetworkServer.connections.TryGetValue(id, out var c) || !c.identity
                || !c.identity.TryGetComponent<PlayerNet>(out var p) || !p.inMatch);
        }

        static bool SameState(ShopLeashState a, ShopLeashState b) =>
            (a.Targets ?? new uint[0]).SequenceEqual(b.Targets ?? new uint[0])
            && (a.Holders ?? new uint[0]).SequenceEqual(b.Holders ?? new uint[0]);

        void OnServerRequest(NetworkConnectionToClient conn, ShopRequest request)
        {
            if (NetworkManager.singleton is GameNetworkManager manager)
                manager.HandleShopRequest(conn, request, Settings, counter ? counter.position : transform.position, shopReach);
        }

        // ---- Client ----
        void Request(ShopRequest request)
        {
            if (!NetworkClient.isConnected || !NetworkClient.ready) { Show("서버에 연결되어 있지 않습니다."); return; }
            if (!ServerReady) { Show(NotSupported); return; }
            NetworkClient.Send(request);
        }

        public const string NotSupported = "이 서버는 상점 기능이 없습니다. ShopSandbox에서 '이 PC에서 호스트 시작'으로 여세요.";

        void OnClientReply(ShopReply reply)
        {
            if (reply.Bag != null && reply.Bag.Length > 0) bag = reply.Bag;
            Show(reply.Message, reply.Good);
        }

        void OnLeashState(ShopLeashState state)
        {
            leashes.Clear();
            if (state.Targets == null || state.Holders == null) return;
            for (int i = 0; i < state.Targets.Length && i < state.Holders.Length; i++) leashes[state.Targets[i]] = state.Holders[i];
        }

        public void Show(string text, bool good = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            message = text;
            messageGood = good;
            messageUntil = Time.unscaledTime + 3f;
        }

        void ResetLocal()
        {
            bag = null;
            armedSlot = -1;
            leashes.Clear();
            CloseAll();
        }

        void CloseAll() => shopOpen = inventoryOpen = false;

        int[] Bag => bag != null && bag.Length == Settings.InventorySlots ? bag : bag = new int[Settings.InventorySlots];

        // 판매대에서 E (ShopCounter).
        public bool OpenShop()
        {
            var local = LocalPlayer;
            if (!local || !local.inMatch || !MatchNet.Current || MatchNet.Current.phase == MatchNet.Phase.Finished)
            {
                Show("경기 중에만 이용할 수 있습니다.");
                return false;
            }
            armedSlot = -1;
            inventoryOpen = false;
            shopOpen = true;
            return true;
        }

        void HandleKeys(PlayerNet local)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || BuyBidBlow.UI.Menu.GameMenuController.BlocksInput) return;

            if (keyboard.iKey.wasPressedThisFrame)
            {
                armedSlot = -1;
                if (inventoryOpen) inventoryOpen = false;
                else if (!shopOpen) inventoryOpen = true;
            }
            if (PanelOpen() && keyboard.qKey.wasPressedThisFrame) CloseAll();

            if (armedSlot < 0 || PanelOpen()) return;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) { armedSlot = -1; Show("목줄 사용 취소"); return; }
            if (!keyboard.eKey.wasPressedThisFrame) return;
            var target = FindLeashTarget(local);
            if (!target) { Show($"{Settings.LeashReach:0.#}m 안의 상대를 바라보고 E를 누르세요."); return; }
            Request(new ShopRequest { Action = ShopAction.Use, Slot = armedSlot, Target = target.netId });
            armedSlot = -1;
        }

        // 바라보는 방향에서 가장 가까운 각도의, 목줄 거리 안의 다른 플레이어.
        PlayerNet FindLeashTarget(PlayerNet local)
        {
            var view = local.TryGetComponent(out PlayerCamera playerCamera) ? playerCamera.CameraTransform
                : Camera.main ? Camera.main.transform : local.transform;
            PlayerNet best = null;
            float bestAngle = 40f;
            foreach (var identity in NetworkClient.spawned.Values)
            {
                if (!identity || identity == local.netIdentity || !identity.TryGetComponent<PlayerNet>(out var other) || !other.inMatch) continue;
                var chest = other.transform.position + Vector3.up;
                if (Vector3.Distance(local.transform.position, other.transform.position) > Settings.LeashReach) continue;
                float angle = Vector3.Angle(view.forward, chest - view.position);
                if (angle < bestAngle) { bestAngle = angle; best = other; }
            }
            return best;
        }

        void UseSlot(int slot)
        {
            var item = Settings.Find(Bag[slot]);
            if (item == null) return;
            switch (item.Effect)
            {
                case ShopEffect.Leash:
                    armedSlot = slot;
                    inventoryOpen = false;
                    Show($"목줄 준비 · 상대를 바라보고 E (우클릭 취소)", true);
                    break;
                case ShopEffect.Scissors:
                    Request(new ShopRequest { Action = ShopAction.Use, Slot = slot });
                    break;
                default:
                    Show($"{item.Name}은(는) 가지고 있기만 하면 효과가 있습니다.");
                    break;
            }
        }

        // ---- 목줄 ----
        // 묶인 내 플레이어를 채운 사람 반경 안으로 되돌린다. 내 위치는 클라이언트 권한이라 여기서 옮기면 서버로 동기화된다.
        void ApplyLeash()
        {
            var local = LocalPlayer;
            if (!local || !local.inMatch || !leashes.TryGetValue(local.netId, out var holderId)) return;
            if (!NetworkClient.spawned.TryGetValue(holderId, out var holder) || !holder) return;
            var anchor = holder.transform.position;
            var offset = local.transform.position - anchor;
            float radius = Settings.LeashRadius;
            if (offset.sqrMagnitude <= radius * radius) return;
            var outward = offset.normalized;
            var clamped = anchor + outward * radius;
            if (local.TryGetComponent(out Rigidbody body))
            {
                var velocity = body.linearVelocity;
                float away = Vector3.Dot(velocity, outward);
                if (away > 0) body.linearVelocity = velocity - outward * away;
                body.position = clamped;
            }
            local.transform.position = clamped;
        }

        void UpdateRopes()
        {
            int used = 0;
            foreach (var pair in leashes)
            {
                if (!NetworkClient.spawned.TryGetValue(pair.Key, out var target) || !target) continue;
                if (!NetworkClient.spawned.TryGetValue(pair.Value, out var holder) || !holder) continue;
                var rope = Rope(used++);
                rope.SetPosition(0, holder.transform.position + Vector3.up * 1.1f);
                rope.SetPosition(1, target.transform.position + Vector3.up * 1.3f);
            }
            for (int i = 0; i < ropes.Count; i++) ropes[i].gameObject.SetActive(i < used);
        }

        LineRenderer Rope(int index)
        {
            while (ropes.Count <= index)
            {
                var go = new GameObject("Shop Leash Rope");
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.widthMultiplier = .04f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (!ropeMaterial)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                    ropeMaterial = new Material(shader) { color = new Color(.75f, .15f, .15f) };
                }
                line.sharedMaterial = ropeMaterial;
                ropes.Add(line);
            }
            ropes[index].gameObject.SetActive(true);
            return ropes[index];
        }

        // ---- Temporary HUD ----
        void OnGUI()
        {
            if (!NetworkClient.active && !NetworkServer.active) { DrawSoloHost(); return; }
            var local = LocalPlayer;
            if (!local || !local.inMatch) return;
            EnsureStyles();

            DrawStatus(local);
            if (shopOpen) DrawShop();
            if (inventoryOpen) DrawInventory();
            if (Time.unscaledTime < messageUntil)
            {
                toastLabel.normal.textColor = messageGood ? new Color(1f, .85f, .3f) : Color.white;
                GUI.Label(new Rect(Screen.width / 2f - 320, Screen.height * .62f, 640, 44), message, toastLabel);
            }
        }

        void DrawStatus(PlayerNet local)
        {
            var lines = new List<string>();
            if (leashes.TryGetValue(local.netId, out var holderId))
            {
                string holderName = NetworkClient.spawned.TryGetValue(holderId, out var h) && h.TryGetComponent<PlayerNet>(out var hp) ? hp.nickname : "?";
                lines.Add($"<color=#ff8080><b>목줄</b></color> {holderName}님에게 묶여 있습니다 · 가위로 끊기");
            }
            if (armedSlot >= 0) lines.Add("<color=#ffd24d><b>목줄 준비</b></color> 상대를 바라보고 E · 우클릭 취소");
            if (!PanelOpen()) lines.Add("<size=13>I 아이템창</size>");
            GUI.Label(new Rect(16, Screen.height - 40 - 26 * lines.Count, 520, 26 * lines.Count + 10), string.Join("\n", lines), label);
        }

        void DrawShop()
        {
            int columns = Settings.ShopColumns, rows = Settings.ShopRows;
            const float cellW = 170, cellH = 150, gap = 10;
            float width = columns * cellW + (columns + 1) * gap;
            float height = 70 + rows * (cellH + gap) + 150;
            var area = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
            GUI.Box(area, GUIContent.none);
            GUI.Box(area, GUIContent.none);
            int cash = PlayerNet.LocalPortfolio != null ? PlayerNet.LocalPortfolio.Available : 0;
            GUI.Label(new Rect(area.x + 16, area.y + 10, width - 32, 34), $"<b>상점</b>   사용 가능 현금 <b>${cash}</b>", title);
            if (GUI.Button(new Rect(area.xMax - 96, area.y + 12, 80, 30), "닫기 Q", button)) shopOpen = false;

            for (int i = 0; i < columns * rows; i++)
            {
                var rect = new Rect(area.x + gap + (i % columns) * (cellW + gap), area.y + 56 + (i / columns) * (cellH + gap), cellW, cellH);
                GUI.Box(rect, GUIContent.none, cell);
                var item = i < Settings.Items.Length ? Settings.Items[i] : null;
                if (item == null) { GUI.Label(new Rect(rect.x, rect.y + rect.height / 2 - 12, rect.width, 24), "<color=#888>준비 중</color>", Centered(small)); continue; }
                GUI.Label(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, 26), $"<b>{item.Name}</b>", label);
                GUI.Label(new Rect(rect.x + 8, rect.y + 32, rect.width - 16, 72), item.Description, small);
                bool afford = cash >= item.Price;
                GUI.enabled = afford;
                if (GUI.Button(new Rect(rect.x + 8, rect.yMax - 38, rect.width - 16, 30), $"구매 ${item.Price}", button))
                    Request(new ShopRequest { Action = ShopAction.Buy, ItemId = item.Id });
                GUI.enabled = true;
            }

            float bagY = area.y + 56 + rows * (cellH + gap) + 4;
            GUI.Label(new Rect(area.x + 16, bagY, width - 32, 26), "<b>내 가방</b>  (되팔 수 있는 물건은 원가로 판매)", label);
            DrawBag(new Rect(area.x + gap, bagY + 30, width - 2 * gap, 100), selling: true);
        }

        void DrawInventory()
        {
            int slots = Settings.InventorySlots;
            const float size = 120, gap = 10;
            float width = slots * size + (slots + 1) * gap;
            var area = new Rect((Screen.width - width) / 2, Screen.height - size - 110, width, size + 56);
            GUI.Box(area, GUIContent.none);
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 12, area.y + 6, width - 24, 26), "<b>아이템</b>  클릭해서 사용 · I 닫기", label);
            DrawBag(new Rect(area.x + gap, area.y + 40, width - 2 * gap, size), selling: false);
        }

        void DrawBag(Rect area, bool selling)
        {
            int slots = Settings.InventorySlots;
            const float gap = 10;
            float size = (area.width - (slots - 1) * gap) / slots;
            for (int i = 0; i < slots; i++)
            {
                var rect = new Rect(area.x + i * (size + gap), area.y, size, area.height);
                var item = Settings.Find(Bag[i]);
                if (item == null) { GUI.Box(rect, "<color=#777>빈 칸</color>", cell); continue; }
                string text = $"<b>{item.Name}</b>\n<size=13>{Hint(item)}</size>";
                if (selling)
                {
                    GUI.Box(rect, GUIContent.none, cell);
                    GUI.Label(new Rect(rect.x + 6, rect.y + 6, rect.width - 12, 40), text, Centered(small));
                    if (item.SellPrice >= 0 && GUI.Button(new Rect(rect.x + 6, rect.yMax - 36, rect.width - 12, 30), $"팔기 ${item.SellPrice}", button))
                        Request(new ShopRequest { Action = ShopAction.Sell, Slot = i });
                }
                else if (GUI.Button(rect, new GUIContent(text, item.Description), cell)) UseSlot(i);
            }
        }

        static string Hint(ShopItemDef item) =>
            item.Effect == ShopEffect.Leash ? "클릭 → 상대에게 E" :
            item.Effect == ShopEffect.Scissors ? "클릭 → 목줄 끊기" :
            item.Effect == ShopEffect.Deed ? "보유 효과" : "";

        // 기본 로비는 전용 서버 참가만 지원한다. Sandbox에서는 이 PC가 서버를 겸하는 호스트로 혼자 시험한다.
        // 상점 처리는 서버 쪽에도 ShopSite가 있어야 하므로 전용 서버로는 시험할 수 없다.
        void DrawSoloHost()
        {
            // 연결이 끊겨 메뉴로 돌아가는 프레임에는 매니저가 이미 파괴되어 있을 수 있다.
            var room = NetworkManager.singleton as GameNetworkManager;
            if (!room || !room.TryGetComponent(out ConnectionController session) || !session.CanConnect) return;
            EnsureStyles();
            if (GUI.Button(new Rect(Screen.width / 2f - 200, 16, 400, 52), "혼자 시험하기 · 이 PC에서 호스트 시작", soloButton))
            {
                var nick = PlayerPrefs.GetString(Nickname.PreferenceKey, "");
                if (string.IsNullOrWhiteSpace(nick)) nick = "SHOPPER";
                session.Host(nick, DirectConnectionSettings.DefaultPort.ToString());
            }
        }

        static GUIStyle Centered(GUIStyle style) => new GUIStyle(style) { alignment = TextAnchor.UpperCenter };

        void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true, wordWrap = true };
            if (font) label.font = font;
            small = new GUIStyle(label) { fontSize = 14 };
            title = new GUIStyle(label) { fontSize = 22 };
            toastLabel = new GUIStyle(label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, richText = true };
            if (font) button.font = font;
            cell = new GUIStyle(GUI.skin.box) { fontSize = 16, richText = true, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            if (font) cell.font = font;
            soloButton = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            if (font) soloButton.font = font;
        }
    }
}
