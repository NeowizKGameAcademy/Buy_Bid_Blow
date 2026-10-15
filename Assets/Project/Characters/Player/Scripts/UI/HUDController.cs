using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BuyBidBlow
{
    public class HUDController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private DetectObject detect;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private TradePanelController tradePanel;

        [Header("UI")]
        [SerializeField] private GameObject cursorImage;
        [SerializeField] private GameObject Prompt;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private TextMeshProUGUI InteractionText;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI PlayerMoney;

        private void OnEnable()
        {
            if (tradePanel != null)
                tradePanel.OpenStateChanged += RefreshInteractionUI;

            RefreshInteractionUI(tradePanel != null && tradePanel.IsOpen);
        }

        private void OnDisable()
        {
            if (tradePanel != null)
                tradePanel.OpenStateChanged -= RefreshInteractionUI;
        }

        private void Update()
        {
            RefreshInteractionUI(tradePanel != null && tradePanel.IsOpen);
            PlayerMoney.text = string.Format("$ {0:N0}", wallet.Money);
        }

        private void RefreshInteractionUI(bool isTradeOpen)
        {
            if (cursorImage != null)
                cursorImage.SetActive(!isTradeOpen);

            if (!isTradeOpen && detect.isDetected)
            {
                Prompt.SetActive(true);

                switch (detect.detectingLayer)
                {
                    case 7:
                        keyText.text = "E";
                        InteractionText.text = "Trade";
                        break;
                }
            }
            else
            {
                Prompt.SetActive(false);
            }
        }
    }
}
