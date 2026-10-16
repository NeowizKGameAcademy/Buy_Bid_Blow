using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

namespace BuyBidBlow
{
    public class HUDController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private DetectObject detect;
        [FormerlySerializedAs("wallet")]
        [SerializeField] private PlayerAssetStatus assets;
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
            if (assets != null)
                PlayerMoney.text = string.Format("$ {0:N0}", assets.Cash);
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
