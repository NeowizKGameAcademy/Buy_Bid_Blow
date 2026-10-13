using System;
using UnityEngine;
using TMPro;

namespace BuyBidBlow
{
    public class TradePanelController : MonoBehaviour
    {
        [SerializeField] private GameObject tradePanel;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI unitPriceText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI logText;

        // int는 가격이 아니라 아이템 ID
        public event Action<int, int> BuyRequested;
        public event Action<int, int> SellRequested;
        public event Action<bool> OpenStateChanged;

        private int selectedItemId = -1;
        private int selectedQuantity = 1;

        public bool IsOpen => tradePanel.activeSelf;

        private void Awake()
        {
            Close();
        }

        public void Open(int itemId)
        {
            selectedItemId = itemId;
            selectedQuantity = 1;
            RefreshQuantity();
            SetLog(string.Empty);

            tradePanel.SetActive(true);
            OpenStateChanged?.Invoke(true);
        }

        // 외부 거래 대상에서 아이템 이름과 단가를 전달받습니다.
        public void Open(int itemId, string itemName, int unitPrice)
        {
            Open(itemId);
            itemNameText.text = itemName;
            unitPriceText.text = $"$ {Mathf.Max(0, unitPrice):N0}";
        }

        public void Close()
        {
            tradePanel.SetActive(false);
            selectedItemId = -1;
            OpenStateChanged?.Invoke(false);
        }

        public void SelectItem(int itemId)
        {
            selectedItemId = itemId;
            selectedQuantity = 1;
            RefreshQuantity();
            SetLog(string.Empty);
        }

        public void IncreaseQuantity()
        {
            if (!IsOpen || selectedQuantity == int.MaxValue)
                return;

            selectedQuantity++;
            RefreshQuantity();
        }

        public void DecreaseQuantity()
        {
            if (!IsOpen || selectedQuantity <= 1)
                return;

            selectedQuantity--;
            RefreshQuantity();
        }

        private void RefreshQuantity()
        {
            quantityText.text = selectedQuantity.ToString();
        }

        // 거래 처리 담당이 성공/실패 메시지를 전달할 때 호출합니다.
        public void SetLog(string message)
        {
            logText.text = message;
        }

        public void RequestBuy()
        {
            if (!IsOpen || selectedItemId < 0)
                return;

            BuyRequested?.Invoke(selectedItemId, selectedQuantity);
        }

        public void RequestSell()
        {
            if (!IsOpen || selectedItemId < 0)
                return;

            SellRequested?.Invoke(selectedItemId, selectedQuantity);
        }
    }
}
