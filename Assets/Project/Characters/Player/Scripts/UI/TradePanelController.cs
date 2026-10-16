using System;
using UnityEngine;
using TMPro;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow
{
    public class TradePanelController : MonoBehaviour
    {
        [SerializeField] private GameObject tradePanel;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI unitPriceText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI logText;

        public event Action<Company, int> BuyRequested;
        public event Action<Company, int> SellRequested;
        public event Action<bool> OpenStateChanged;

        private Company selectedCompany;
        private bool hasSelectedCompany;
        private int selectedQuantity = 1;

        public bool IsOpen => tradePanel.activeSelf;

        private void Awake()
        {
            Close();
        }

        public void Open(Company company)
        {
            selectedCompany = company;
            hasSelectedCompany = true;
            selectedQuantity = 1;
            RefreshQuantity();
            SetLog(string.Empty);

            tradePanel.SetActive(true);
            OpenStateChanged?.Invoke(true);
        }

        // 외부 거래 대상에서 아이템 이름과 단가를 전달받습니다.
        public void Open(Company company, string itemName, int unitPrice)
        {
            Open(company);
            SetItemDetails(itemName, unitPrice);
        }

        // Can be called while the panel remains open after a round price change.
        public void SetItemDetails(string itemName, int unitPrice)
        {
            itemNameText.text = itemName;
            unitPriceText.text = $"$ {Mathf.Max(0, unitPrice):N0}";
        }

        public void Close()
        {
            tradePanel.SetActive(false);
            hasSelectedCompany = false;
            OpenStateChanged?.Invoke(false);
        }

        public void SelectItem(Company company)
        {
            selectedCompany = company;
            hasSelectedCompany = true;
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

        public void SetLog(string message)
        {
            logText.text = message;
        }

        public void RequestBuy()
        {
            if (!IsOpen || !hasSelectedCompany)
                return;

            BuyRequested?.Invoke(selectedCompany, selectedQuantity);
        }

        public void RequestSell()
        {
            if (!IsOpen || !hasSelectedCompany)
                return;

            SellRequested?.Invoke(selectedCompany, selectedQuantity);
        }
    }
}
