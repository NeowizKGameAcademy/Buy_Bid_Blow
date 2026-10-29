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
        private Text feedback;
        private float feedbackUntil;

        public bool IsOpen => active != null && active.gameObject.activeSelf;
        public Company SelectedCompany => selectedCompany;

        // 회사별 패널 버튼을 바인딩하고 패널을 닫은 상태로 시작
        private void Awake()
        {
            for (int i = 0; i < companyPanels.Length; i++)
                if (companyPanels[i] != null)
                    BindButtons(companyPanels[i]);
            Close();
        }

        // 패널의 탭·수량·확인·닫기 버튼에 동작 연결
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
        }

        // 선택한 회사 패널을 매수 탭·수량 1로 열고 열림 이벤트 발생
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

        // 패널 데이터를 교체하고 다시 그리기
        // New prices or assets while the panel stays open.
        public void Present(TradingPanelData panelData)
        {
            data = panelData;
            Refresh();
        }

        // 모든 패널을 숨기고 닫힘 이벤트 발생
        public void Close()
        {
            Hide();
            active = null;
            OpenStateChanged?.Invoke(false);
        }

        // 모든 회사 패널 비활성화
        private void Hide()
        {
            foreach (var panel in companyPanels)
                if (panel != null)
                    panel.gameObject.SetActive(false);
        }

        // 피드백 만료 처리 및 키 입력으로 수량·탭 변경과 확인 처리
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

            if (keyboard.rightArrowKey.wasPressedThisFrame) IncreaseQuantity();
            if (keyboard.leftArrowKey.wasPressedThisFrame) DecreaseQuantity();
            if (keyboard.upArrowKey.wasPressedThisFrame) SelectTab((Tab)(((int)tab + 2) % 3));
            if (keyboard.downArrowKey.wasPressedThisFrame) SelectTab((Tab)(((int)tab + 1) % 3));
            if (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) Confirm();
        }

        // 탭을 전환하고 수량을 1로 초기화
        public void SelectTab(Tab value)
        {
            if (!IsOpen) return;
            tab = value;
            quantity = 1;
            Refresh();
        }

        // 수량을 1 증가
        public void IncreaseQuantity()
        {
            if (!IsOpen || quantity == int.MaxValue) return;
            quantity++;
            Refresh();
        }

        // 수량을 1 감소(최소 1)
        public void DecreaseQuantity()
        {
            if (!IsOpen || quantity <= 1) return;
            quantity--;
            Refresh();
        }

        // 확인 가능하면 현재 탭에 따라 매수/매도/베팅 요청
        public void Confirm()
        {
            if (!IsOpen || !active.CanConfirm) return;
            if (tab == Tab.Buy) RequestBuy();
            else if (tab == Tab.Sell) RequestSell();
            else BetRequested?.Invoke(selectedCompany);
        }

        // 선택 회사·수량으로 매수 요청 이벤트 발생
        public void RequestBuy()
        {
            if (IsOpen) BuyRequested?.Invoke(selectedCompany, quantity);
        }

        // 선택 회사·수량으로 매도 요청 이벤트 발생
        public void RequestSell()
        {
            if (IsOpen) SellRequested?.Invoke(selectedCompany, quantity);
        }

        // 피드백 문구를 표시하고 일정 시간 후 지워지도록 만료 시각 설정
        public void SetLog(string message)
        {
            if (!feedback) return;
            feedback.text = message;
            feedbackUntil = string.IsNullOrEmpty(message) ? 0 : Time.unscaledTime + feedbackSeconds;
        }

        // 열려 있으면 현재 데이터·탭·수량으로 패널 다시 그리기
        private void Refresh()
        {
            if (IsOpen)
                active.Present(data, (int)tab, ref quantity);
        }
    }
}
