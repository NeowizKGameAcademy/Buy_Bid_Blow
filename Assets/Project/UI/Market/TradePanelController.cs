using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.UI.Market
{
    // Drives the desk trading panel: one designed panel per company, the selected tab and quantity.
    // It only raises requests; whoever owns the trade (LocalTradeController) answers with new data.
    public class TradePanelController : MonoBehaviour
    {
        public enum Tab { Buy, Sell, DownsideBet }

        // Indexed by Company.
        [SerializeField] private TradingPanelView[] companyPanels = new TradingPanelView[3];
        [SerializeField, Min(0)] private float feedbackSeconds = 3;

        public event Action<Company, int> BuyRequested;
        public event Action<Company, int> SellRequested;
        // Join, or cancel, a downside bet on the company.
        public event Action<Company> BetRequested;
        public event Action<bool> OpenStateChanged;

        private TradingPanelView active;
        private TradingPanelData data;
        private Company selectedCompany;
        private Tab tab;
        private int quantity = 1;
        private int openedFrame = -1;
        private int confirmedFrame = -1;
        private Text feedback;
        private float feedbackUntil;

        public bool IsOpen => active != null && active.gameObject.activeSelf;
        public Company SelectedCompany => selectedCompany;

        private void Awake()
        {
            BuyBidBlow.Audio.UIAudioFeedback.Bind(transform.parent?transform.parent:transform);
            for (int i = 0; i < companyPanels.Length; i++)
                if (companyPanels[i] != null)
                    BindButtons(companyPanels[i]);
            Close();
        }

        private void BindButtons(TradingPanelView panel)
        {
            void On(string path, Action action)
            {
                var target = panel.transform.Find(path);
                if (target && target.TryGetComponent<Button>(out var button))
                    button.onClick.AddListener(() => action());
            }
            On("Trade Tabs/Buy", () => SelectTab(Tab.Buy));
            On("Trade Tabs/Sell", () => SelectTab(Tab.Sell));
            On("Trade Tabs/Downside Bet", () => SelectTab(Tab.DownsideBet));
            On("Quantity Control/Increase", IncreaseQuantity);
            On("Quantity Control/Decrease", DecreaseQuantity);
            On("Payment Summary/Confirm Purchase", Confirm);
            On("Close Hint/Keycap", Close);
            var guide=panel.transform.Find("Keyboard Guide");
            if(guide && guide.TryGetComponent<Text>(out var guideText))
                guideText.text="W 오른쪽 탭 · S 왼쪽 탭 · A/D 수량 · E 확인 · Q 닫기";
        }

        public void Open(Company company, TradingPanelData panelData)
        {
            Hide();
            selectedCompany = company;
            active = companyPanels[(int)company];
            var feedbackLabel = active.transform.Find("Feedback");
            feedback = feedbackLabel ? feedbackLabel.GetComponent<Text>() : null;
            tab = Tab.Buy;
            quantity = 1;
            openedFrame = Time.frameCount;
            SetLog(string.Empty);
            active.gameObject.SetActive(true);
            Present(panelData);
            OpenStateChanged?.Invoke(true);
        }

        // New prices or assets while the panel stays open.
        public void Present(TradingPanelData panelData)
        {
            data = panelData;
            Refresh();
        }

        public void Close()
        {
            if(IsOpen)BuyBidBlow.Audio.GameAudio.Play("ui.close",this);
            Hide();
            active = null;
            OpenStateChanged?.Invoke(false);
        }

        private void Hide()
        {
            foreach (var panel in companyPanels)
                if (panel != null)
                    panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (feedback && feedbackUntil > 0 && Time.unscaledTime >= feedbackUntil)
                SetLog(string.Empty);

            var keyboard = Keyboard.current;
            // The E press that opened the panel must not also confirm a trade.
            if (keyboard == null || Time.frameCount == openedFrame)
                return;

            if (keyboard.dKey.wasPressedThisFrame) IncreaseQuantity();
            if (keyboard.aKey.wasPressedThisFrame) DecreaseQuantity();
            if (keyboard.wKey.wasPressedThisFrame) SelectTab((Tab)(((int)tab + 1) % 3));
            if (keyboard.sKey.wasPressedThisFrame) SelectTab((Tab)(((int)tab + 2) % 3));
            if (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) Confirm();
        }

        public void SelectTab(Tab value)
        {
            if (!IsOpen) return;
            if(tab==value)return;
            BuyBidBlow.Audio.GameAudio.Play("ui.navigate",this);
            tab = value;
            quantity = 1;
            Refresh();
        }

        public void IncreaseQuantity()
        {
            if (!IsOpen) return;
            int limit=Mathf.Max(1,TradingPanelView.QuantityLimit(data,(int)tab));
            int next=quantity>=limit?1:quantity+1;
            if(next==quantity)return;
            quantity=next;
            BuyBidBlow.Audio.GameAudio.Play("ui.quantity",this);
            Refresh();
        }

        public void DecreaseQuantity()
        {
            if (!IsOpen) return;
            int next=quantity>1?quantity-1:Mathf.Max(1,TradingPanelView.QuantityLimit(data,(int)tab));
            if(next==quantity)return;
            quantity=next;
            BuyBidBlow.Audio.GameAudio.Play("ui.quantity",this);
            Refresh();
        }

        public void Confirm()
        {
            // Enter may arrive through both the UI module and the keyboard shortcut.
            if (!IsOpen || !active.CanConfirm || confirmedFrame == Time.frameCount) return;
            confirmedFrame = Time.frameCount;
            BuyBidBlow.Audio.GameAudio.Play("ui.confirm",this);
            if (tab == Tab.Buy) RequestBuy();
            else if (tab == Tab.Sell) RequestSell();
            else BetRequested?.Invoke(selectedCompany);
        }

        public void RequestBuy()
        {
            if (IsOpen) BuyRequested?.Invoke(selectedCompany, quantity);
        }

        public void RequestSell()
        {
            if (IsOpen) SellRequested?.Invoke(selectedCompany, quantity);
        }

        public void SetLog(string message)
        {
            if (!feedback) return;
            feedback.text = message;
            feedbackUntil = string.IsNullOrEmpty(message) ? 0 : Time.unscaledTime + feedbackSeconds;
        }

        private void Refresh()
        {
            if (IsOpen)
                active.Present(data, (int)tab, ref quantity);
        }
    }
}
