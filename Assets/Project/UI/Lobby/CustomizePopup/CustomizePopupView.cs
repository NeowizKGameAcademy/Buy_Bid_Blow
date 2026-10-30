using System;
using System.Collections.Generic;
using BuyBidBlow.Characters;
using BuyBidBlow.UI.Menu;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.UI.Lobby
{
    // 캐릭터 꾸미기 팝업. 고르는 동안에는 미리보기 늑대에만 적용하고, 저장할 때 한 번만 서버에 보낸다.
    // 아이템 순서는 BBBWolfAppearance의 상·하의 순서와 PlayerNet.Colors의 넥타이 색 순서를 그대로 따른다.
    public sealed class CustomizePopupView : MonoBehaviour
    {
        public enum Category { Hat, Top, Bottom, Tie, Shoes, Accessory }

        [Serializable] public class Item
        {
            public string name;
            public Sprite icon;
        }

        [Serializable] public class Tab
        {
            public Category category;
            public Button button;
            public Text label;
        }

        private sealed class Slot
        {
            public Button button;
            public Image icon;
            public GameObject check;
            public GameObject locked;
        }

        private const int Top = 0, Bottom = 1, Tie = 2;
        private static readonly Color TabText = new Color(1, .94f, .77f);
        private static readonly Color TabSelectedText = new Color(.16f, .11f, .04f);
        private static readonly Color LockedIcon = new Color(1, 1, 1, .35f);

        [SerializeField] private GameObject window;
        [SerializeField] private Tab[] tabs;
        [SerializeField] private Sprite tabNormal, tabSelected, slotNormal, slotSelected;
        [SerializeField] private Vector2 tabSize = new Vector2(132, 58), tabSelectedSize = new Vector2(156, 79);
        // 프리팹에 미리 배치한 슬롯. 탭의 아이템 수만큼 앞에서부터 켜서 쓴다.
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private float slotSize = 132, slotSelectedSize = 149;
        [SerializeField] private Text itemLabel, nameLabel;
        [SerializeField] private CustomizeRotateButton rotateLeft, rotateRight;
        [SerializeField] private Button closeButton, cancelButton, saveButton, randomButton, resetButton;
        [SerializeField] private Item[] tops, bottoms, ties;
        [SerializeField] private CustomizePreview preview;

        private readonly List<Slot> slots = new List<Slot>();
        private readonly int[] opened = new int[3];
        private readonly int[] draft = new int[3];
        private Category category = Category.Top;

        public bool IsOpen => window && window.activeSelf;

        // 버튼 연결, 아이템이 없는 탭은 눌리지 않게 처리
        private void Awake()
        {
            if (!preview) preview = FindFirstObjectByType<CustomizePreview>();
            foreach (var tab in tabs)
            {
                var target = tab.category;
                tab.button.onClick.AddListener(() => SelectCategory(target));
                tab.button.interactable = ItemsOf(target).Length > 0;
            }
            closeButton.onClick.AddListener(Close);
            cancelButton.onClick.AddListener(Close);
            saveButton.onClick.AddListener(Save);
            randomButton.onClick.AddListener(Randomize);
            resetButton.onClick.AddListener(ResetDraft);
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                var button = slotButtons[i];
                button.onClick.AddListener(() => Pick(index));
                slots.Add(new Slot
                {
                    button = button,
                    icon = button.transform.Find("Icon").GetComponent<Image>(),
                    check = button.transform.Find("Check").gameObject,
                    locked = button.transform.Find("Lock").gameObject,
                });
            }
            window.SetActive(false);
        }

        // 팝업 열기: 준비 상태를 풀고 현재 외형을 임시 선택값으로 복사
        public void Open()
        {
            var local = Local();
            if (!local) return;
            if (local.ready) local.SetReady(false);
            opened[Top] = local.top;
            opened[Bottom] = local.bottom;
            opened[Tie] = local.tie;
            Array.Copy(opened, draft, draft.Length);
            category = Category.Top;
            nameLabel.text = string.IsNullOrEmpty(local.nickname) ? "WOLF " + (local.seat + 1).ToString("00") : local.nickname;
            window.SetActive(true);
            if (preview) preview.Show();
            Render();
        }

        // 변경을 버리고 닫기 (취소·X 버튼)
        public void Close()
        {
            window.SetActive(false);
            if (preview) preview.Hide();
        }

        // 임시 선택값을 서버에 한 번 보내고 닫기. 반영은 PlayerNet SyncVar로 로비 늑대에 적용된다.
        private void Save()
        {
            var local = Local();
            if (local && NetworkClient.ready) local.Customize(draft[Top], draft[Bottom], draft[Tie]);
            Close();
        }

        // 상의·하의·넥타이를 보유한 아이템 중에서 무작위로 고르기
        private void Randomize()
        {
            foreach (var target in new[] { Category.Top, Category.Bottom, Category.Tie })
            {
                var owned = new List<int>();
                for (int i = 0; i < ItemsOf(target).Length; i++) if (IsOwned(target, i)) owned.Add(i);
                if (owned.Count > 0) draft[Part(target)] = owned[UnityEngine.Random.Range(0, owned.Count)];
            }
            Render();
        }

        // 팝업을 열었을 때의 외형으로 되돌리기
        private void ResetDraft()
        {
            Array.Copy(opened, draft, draft.Length);
            Render();
        }

        // 탭 선택 (아이템이 없는 탭은 무시)
        private void SelectCategory(Category target)
        {
            if (ItemsOf(target).Length == 0) return;
            category = target;
            Render();
        }

        // 현재 탭에서 아이템 선택
        private void Pick(int index)
        {
            draft[Part(category)] = index;
            Render();
        }

        // 보유 아이템 연동 전까지 모두 보유로 처리한다. 상점·계정 데이터가 생기면 이 메서드만 바꾼다.
        private bool IsOwned(Category target, int index)
        {
            return true;
        }

        // 버튼을 누르고 있는 동안 미리보기 모델 회전
        private void Update()
        {
            if (!IsOpen || !preview) return;
            float direction = (rotateRight && rotateRight.Held ? 1 : 0) - (rotateLeft && rotateLeft.Held ? 1 : 0);
            if (direction != 0) preview.Rotate(direction);
        }

        // 연결이 끊기면 닫고, 팝업이 열린 동안 Esc로 열린 게임 메뉴는 바로 닫아 키 입력을 무시
        private void LateUpdate()
        {
            if (!IsOpen) return;
            if (!Local()) { Close(); return; }
            // GameMenuController runs its Update late (order 10000) but before any LateUpdate, so the menu never draws.
            if (GameMenuController.BlocksInput) GameMenuController.Ensure()?.Close();
        }

        // 탭·슬롯·설명·미리보기를 임시 선택값에 맞게 갱신
        private void Render()
        {
            foreach (var tab in tabs)
            {
                bool on = tab.category == category;
                tab.button.image.sprite = on ? tabSelected : tabNormal;
                ((RectTransform)tab.button.transform).sizeDelta = on ? tabSelectedSize : tabSize;
                if (tab.label) tab.label.color = on ? TabSelectedText : TabText;
            }

            var local = Local();
            var items = ItemsOf(category);
            int part = Part(category);
            int worn = local ? Worn(local, part) : -1;
            if (items.Length > slots.Count) Debug.LogWarning("CustomizePopupView: " + category + " has more items than placed slots.", this);
            Slot picked = null;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                bool used = i < items.Length;
                slot.button.gameObject.SetActive(used);
                if (!used) continue;
                bool selected = draft[part] == i;
                bool owned = IsOwned(category, i);
                var rect = (RectTransform)slot.button.transform;
                rect.sizeDelta = Vector2.one * (selected ? slotSelectedSize : slotSize);
                slot.button.image.sprite = selected ? slotSelected : slotNormal;
                slot.button.interactable = owned;
                slot.icon.sprite = items[i].icon;
                slot.icon.color = owned ? Color.white : LockedIcon;
                slot.check.SetActive(i == worn);
                slot.locked.SetActive(!owned);
                if (selected) picked = slot;
            }
            // The selected frame is larger than the grid cell, so draw it above its neighbours.
            if (picked != null) picked.button.transform.SetAsLastSibling();

            int index = draft[part];
            itemLabel.text = index >= 0 && index < items.Length
                ? "<color=#F2C14E>" + items[index].name + "</color>  ·  " + (IsOwned(category, index) ? "보유중" : "미보유")
                : "";
            if (preview) preview.Apply(draft[Top], draft[Bottom], PlayerNet.Colors[Mathf.Clamp(draft[Tie], 0, PlayerNet.Colors.Length - 1)]);
        }

        // 카테고리별 아이템 목록 (모자·신발·장신구는 아직 없음)
        private Item[] ItemsOf(Category target)
        {
            switch (target)
            {
                case Category.Top: return tops ?? Array.Empty<Item>();
                case Category.Bottom: return bottoms ?? Array.Empty<Item>();
                case Category.Tie: return ties ?? Array.Empty<Item>();
                default: return Array.Empty<Item>();
            }
        }

        // 카테고리를 임시 선택값 배열 인덱스로 변환
        private static int Part(Category target)
        {
            return target == Category.Bottom ? Bottom : target == Category.Tie ? Tie : Top;
        }

        // 서버에 반영된 현재 착용 인덱스
        private static int Worn(PlayerNet local, int part)
        {
            return part == Top ? local.top : part == Bottom ? local.bottom : local.tie;
        }

        // 로컬 플레이어 검색
        private static PlayerNet Local()
        {
            return NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
        }
    }
}
