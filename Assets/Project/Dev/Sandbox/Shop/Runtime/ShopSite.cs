using System.Collections.Generic;
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
    // 상점 Sandbox의 씬 진입점: 메시지 핸들러 연결, 내 가방·사용 간격, 파티 효과 재생, 임시 IMGUI(상점창·아이템창).
    // 원본 씬에는 이 컴포넌트가 없으므로 상점 기능이 꺼져 있다.
    public sealed class ShopSite : MonoBehaviour
    {
        public TextAsset settingsJson;
        public Font font;
        // 상점 판매대. 서버는 이 위치로 구매 거리를 검증한다. Build Shop 메뉴가 연결한다.
        public Transform counter;
        // 판매대에서 이 거리(m) 안에서만 살 수 있다. 벗어나면 상점창이 닫힌다.
        public float shopReach = 4f;

        public static ShopSite Instance { get; private set; }
        // 접속한 서버가 상점 기능을 지원한다고 알려왔는지. 연결이 끊기면 false.
        public static bool ServerReady { get; private set; }

        ShopSettings settings;
        public ShopSettings Settings => settings ??= ParseSettings();

        // ---- 내 상태(클라이언트) ----
        int[] bag;
        bool shopOpen, inventoryOpen;
        int emoteSlot = -1;          // 이모티콘을 고르는 중인 칸
        float cooldownUntil;         // Time.unscaledTime 기준
        MatchNet lastMatch;
        string message;
        float messageUntil;
        bool messageGood;

        // ---- 서버 ----
        bool serverHooked, clientHooked;
        readonly HashSet<int> announced = new HashSet<int>();

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
                NetworkClient.UnregisterHandler<ShopEffectPlay>();
            }
            serverHooked = clientHooked = false;
            ServerReady = false;
            ResetLocal();
            ShopPartyEffect.ClearAll();
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
                    NetworkClient.RegisterHandler<ShopEffectPlay>(OnEffectPlay);
                    clientHooked = true;
                }
            }
            else if (clientHooked) { clientHooked = false; ServerReady = false; ResetLocal(); }

            var local = LocalPlayer;
            var match = MatchNet.Current;
            // 새 경기면 가방을 비운다(서버도 경기마다 새 ShopBoard를 만든다).
            if (match != lastMatch) { lastMatch = match; ResetLocal(); }
            if (!local || !local.inMatch || !match || match.phase == MatchNet.Phase.Finished)
            {
                CloseAll();
                return;
            }

            HandleKeys();
            if (PanelOpen() && local.TryGetComponent(out PlayerMove move))
            {
                move.SetMoveInput(Vector2.zero);
                move.SetJumpHeld(false);
            }
            if (shopOpen && counter && Vector3.Distance(local.transform.position, counter.position) > shopReach + 1f) shopOpen = false;
        }

        static PlayerNet LocalPlayer =>
            NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;

        // ---- Server ----
        void ServerUpdate()
        {
            if (!NetworkServer.active) { serverHooked = false; return; }
            if (!serverHooked) { NetworkServer.RegisterHandler<ShopRequest>(OnServerRequest); serverHooked = true; announced.Clear(); }
            if (!(NetworkManager.singleton is GameNetworkManager manager)) return;

            manager.ShopServerTick(Settings);
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (!conn.identity || !conn.identity.TryGetComponent<PlayerNet>(out var player) || !player.inMatch) continue;
                if (announced.Add(conn.connectionId)) conn.Send(new ShopAvailable());
            }
            // 경기에서 빠진 연결은 다음 경기에 다시 알린다.
            announced.RemoveWhere(id => !NetworkServer.connections.TryGetValue(id, out var c) || !c.identity
                || !c.identity.TryGetComponent<PlayerNet>(out var p) || !p.inMatch);
        }

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
            if (reply.Bag != null && reply.Bag.Length > 0)
            {
                bag = reply.Bag;
                cooldownUntil = Time.unscaledTime + Mathf.Max(0, reply.Cooldown);
            }
            Show(reply.Message, reply.Good);
        }

        void OnEffectPlay(ShopEffectPlay play)
        {
            var item = Settings.Find(play.ItemId);
            if (item == null || !NetworkClient.spawned.TryGetValue(play.Player, out var owner) || !owner) return;
            string emote = play.Emote >= 0 && play.Emote < Settings.Emotes.Length ? Settings.Emotes[play.Emote] : "";
            bool mine = owner.isLocalPlayer;
            ShopPartyEffect.Play(owner.transform, item.Effect, emote, Mathf.Min(play.Seconds, ShopSettings.MaxEffectSeconds), font, mine);
            // 1인칭이라 내 머리 위 이모티콘은 내게 보이지 않으므로 글자로 알려 준다.
            if (mine && item.Effect == ShopEffect.Emote) Show($"머리 위에 '{emote}' 표시 중", true);
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
            emoteSlot = -1;
            cooldownUntil = 0;
            CloseAll();
        }

        void CloseAll()
        {
            shopOpen = inventoryOpen = false;
            emoteSlot = -1;
        }

        int[] Bag => bag != null && bag.Length == Settings.InventorySlots ? bag : bag = new int[Settings.InventorySlots];

        float CooldownLeft => Mathf.Max(0, cooldownUntil - Time.unscaledTime);

        // 판매대에서 E (ShopCounter).
        public bool OpenShop()
        {
            var local = LocalPlayer;
            if (!local || !local.inMatch || !MatchNet.Current || MatchNet.Current.phase == MatchNet.Phase.Finished)
            {
                Show("경기 중에만 이용할 수 있습니다.");
                return false;
            }
            inventoryOpen = false;
            emoteSlot = -1;
            shopOpen = true;
            return true;
        }

        void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || BuyBidBlow.UI.Menu.GameMenuController.BlocksInput) return;

            if (keyboard.iKey.wasPressedThisFrame)
            {
                emoteSlot = -1;
                if (inventoryOpen) inventoryOpen = false;
                else if (!shopOpen) inventoryOpen = true;
            }
            if (PanelOpen() && keyboard.qKey.wasPressedThisFrame) CloseAll();
        }

        void UseSlot(int slot)
        {
            var item = Settings.Find(Bag[slot]);
            if (item == null) return;
            if (CooldownLeft > 0) { Show($"{Mathf.CeilToInt(CooldownLeft)}초 뒤에 다시 쓸 수 있습니다."); return; }
            if (item.Effect == ShopEffect.Emote) { emoteSlot = slot; return; }
            Request(new ShopRequest { Action = ShopAction.Use, Slot = slot });
        }

        void UseEmote(int emote)
        {
            if (emoteSlot < 0) return;
            Request(new ShopRequest { Action = ShopAction.Use, Slot = emoteSlot, Emote = emote });
            emoteSlot = -1;
        }

        // ---- Temporary HUD ----
        void OnGUI()
        {
            if (!NetworkClient.active && !NetworkServer.active) { DrawSoloHost(); return; }
            var local = LocalPlayer;
            if (!local || !local.inMatch) return;
            EnsureStyles();

            DrawStatus();
            if (shopOpen) DrawShop();
            if (inventoryOpen) DrawInventory();
            if (Time.unscaledTime < messageUntil)
            {
                toastLabel.normal.textColor = messageGood ? new Color(1f, .85f, .3f) : Color.white;
                GUI.Label(new Rect(Screen.width / 2f - 320, Screen.height * .62f, 640, 44), message, toastLabel);
            }
        }

        void DrawStatus()
        {
            if (PanelOpen()) return;
            string text = CooldownLeft > 0 ? $"<size=13>I 아이템창 · 다음 사용까지 {Mathf.CeilToInt(CooldownLeft)}초</size>" : "<size=13>I 아이템창</size>";
            GUI.Label(new Rect(16, Screen.height - 50, 520, 36), text, label);
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
            GUI.Label(new Rect(area.x + 16, area.y + 10, width - 32, 34), $"<b>파티 상점</b>   사용 가능 현금 <b>${cash}</b>", title);
            if (GUI.Button(new Rect(area.xMax - 96, area.y + 12, 80, 30), "닫기 Q", button)) shopOpen = false;

            bool full = System.Array.IndexOf(Bag, 0) < 0;
            for (int i = 0; i < columns * rows; i++)
            {
                var rect = new Rect(area.x + gap + (i % columns) * (cellW + gap), area.y + 56 + (i / columns) * (cellH + gap), cellW, cellH);
                GUI.Box(rect, GUIContent.none, cell);
                var item = i < Settings.Items.Length ? Settings.Items[i] : null;
                if (item == null) { GUI.Label(new Rect(rect.x, rect.y + rect.height / 2 - 12, rect.width, 24), "<color=#888>준비 중</color>", Centered(small)); continue; }
                GUI.Label(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, 26), $"<b>{item.Name}</b>", label);
                GUI.Label(new Rect(rect.x + 8, rect.y + 32, rect.width - 16, 72), $"{item.Description}\n<color=#aaa>{item.Seconds:0.#}초</color>", small);
                GUI.enabled = cash >= item.Price && !full;
                if (GUI.Button(new Rect(rect.x + 8, rect.yMax - 38, rect.width - 16, 30), $"구매 ${item.Price}", button))
                    Request(new ShopRequest { Action = ShopAction.Buy, ItemId = item.Id });
                GUI.enabled = true;
            }

            float bagY = area.y + 56 + rows * (cellH + gap) + 4;
            GUI.Label(new Rect(area.x + 16, bagY, width - 32, 26),
                $"<b>내 가방</b>  (최대 {Settings.InventorySlots}개 · 일회용 · 사용 간격 {Settings.UseCooldown:0.#}초 · 경기가 끝나면 사라짐)", label);
            DrawBag(new Rect(area.x + gap, bagY + 30, width - 2 * gap, 100), usable: false);
        }

        void DrawInventory()
        {
            int slots = Settings.InventorySlots;
            const float size = 120, gap = 10;
            float width = Mathf.Max(slots * size + (slots + 1) * gap, 420);
            var area = new Rect((Screen.width - width) / 2, Screen.height - size - 110, width, size + 56);
            GUI.Box(area, GUIContent.none);
            GUI.Box(area, GUIContent.none);
            string cooldown = CooldownLeft > 0 ? $"  <color=#ffb0b0>다음 사용까지 {Mathf.CeilToInt(CooldownLeft)}초</color>" : "";
            GUI.Label(new Rect(area.x + 12, area.y + 6, width - 24, 26), $"<b>아이템</b>  클릭해서 사용 · I 닫기{cooldown}", label);
            DrawBag(new Rect(area.x + gap, area.y + 40, width - 2 * gap, size), usable: true);
            if (emoteSlot >= 0) DrawEmotePicker(new Rect(area.x, area.y - 70, width, 62));
        }

        void DrawEmotePicker(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUI.Box(area, GUIContent.none);
            var emotes = Settings.Emotes;
            const float gap = 8;
            float size = (area.width - gap * (emotes.Length + 1)) / emotes.Length;
            for (int i = 0; i < emotes.Length; i++)
                if (GUI.Button(new Rect(area.x + gap + i * (size + gap), area.y + 8, size, area.height - 16), emotes[i], button)) UseEmote(i);
        }

        void DrawBag(Rect area, bool usable)
        {
            int slots = Settings.InventorySlots;
            const float gap = 10;
            float size = (area.width - (slots - 1) * gap) / slots;
            for (int i = 0; i < slots; i++)
            {
                var rect = new Rect(area.x + i * (size + gap), area.y, size, area.height);
                var item = Settings.Find(Bag[i]);
                if (item == null) { GUI.Box(rect, "<color=#777>빈 칸</color>", cell); continue; }
                string text = $"<b>{item.Name}</b>\n<size=13>{(usable ? Hint(item) : "I 아이템창에서 사용")}</size>";
                if (!usable) GUI.Box(rect, text, cell);
                else if (GUI.Button(rect, new GUIContent(text, item.Description), cell)) UseSlot(i);
            }
        }

        static string Hint(ShopItemDef item) =>
            item.Effect == ShopEffect.Emote ? "클릭 → 고르기" : "클릭해서 사용";

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
