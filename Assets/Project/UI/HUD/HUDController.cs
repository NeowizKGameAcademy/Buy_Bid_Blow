using System.Collections;
using System.Collections.Generic;
using BuyBidBlow.Characters;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using System;
using System.Linq;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Notebook;
using BuyBidBlow.UI.Result;
using Mirror;
using UnityEngine.InputSystem;

namespace BuyBidBlow.UI.HUD
{
    public class HUDController : MonoBehaviour
    {
        [Header("Components")]
        [FormerlySerializedAs("detect")]
        [SerializeField] private InteractionSensor sensor;
        [FormerlySerializedAs("wallet")]
        [SerializeField] private PlayerAssetStatus assets;
        [SerializeField] private TradePanelController tradePanel;

        [Header("UI")]
        [SerializeField] private GameObject cursorImage;
        [SerializeField] private Image cursor;
        // [0] nothing to use, [1] looking at something usable.
        [SerializeField] private Sprite[] cursorSprites = new Sprite[2];
        [SerializeField] private GameObject Prompt;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private TextMeshProUGUI InteractionText;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI PlayerMoney;
        // The notebook, match feedback and Esc menu (Network Player HUD, GameMenuController) hold the player still
        // and close trading while open; this includes the frame one of them closed.
        public bool ModalOpen => NetworkNotebookController.BlocksInput;
        private bool modalWasOpen;
        private string notice;
        private float noticeUntil;

        // 거래 패널 열림 이벤트를 구독하고 상호작용 UI 초기 갱신
        private void OnEnable()
        {
            PlayerNet.LocalReply += ShowReply;
            if (tradePanel != null)
                tradePanel.OpenStateChanged += RefreshInteractionUI;

            if (Time.unscaledTime >= noticeUntil) RefreshInteractionUI(tradePanel != null && tradePanel.IsOpen);
        }

        // 거래 패널 열림 이벤트 구독 해제
        private void OnDisable()
        {
            PlayerNet.LocalReply -= ShowReply;
            notice = null; noticeUntil = 0;
            if (tradePanel != null)
                tradePanel.OpenStateChanged -= RefreshInteractionUI;
        }

        // 매 프레임 상호작용 UI와 보유 현금 표시 갱신
        private void Update()
        {
            if (Time.unscaledTime >= noticeUntil) RefreshInteractionUI(tradePanel != null && tradePanel.IsOpen);
            if (assets != null)
                PlayerMoney.text = NetworkClient.active && !assets.HasServerState ? "—" : string.Format("$ {0:N0}", assets.Cash);
            if (ModalOpen != modalWasOpen) { modalWasOpen = ModalOpen; UpdateInputLock(); }
            if (Time.unscaledTime < noticeUntil)
            {
                Prompt.SetActive(true); keyText.text = ""; InteractionText.text = notice;
            }
        }

        private void UpdateInputLock()
        {
            if (ModalOpen)
            {
                if (tradePanel) tradePanel.Close();
                var radial = GetComponentInParent<PlayerSeating>()?.GetComponentInChildren<RadialMenuController>(true) ?? GetComponentInChildren<RadialMenuController>(true);
                if (radial) radial.EndSelection(false);
            }
        }

        private void ShowReply(RequestResult result)
        {
            // Trades, bets and bids answer through the Network Player HUD's notices (PlayerNet.LocalNotice).
            if (result.Kind == RequestKind.Seat || result.Kind == RequestKind.StartMatch || result.Kind == RequestKind.ReturnToLobby)
                ShowNotice(result.Message);
        }
        private void ShowNotice(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            notice = message; noticeUntil = Time.unscaledTime + 4;
        }

        // 거래 패널/착석/감지 상태에 따라 커서와 상호작용 안내 문구 표시
        private void RefreshInteractionUI(bool isTradeOpen)
        {
            if(BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput)
            {
                if(cursorImage)cursorImage.SetActive(false);
                if(Prompt)Prompt.SetActive(false);
                return;
            }
            if (cursorImage != null)
                cursorImage.SetActive(!isTradeOpen);

            var target = sensor != null ? sensor.Target : null;
            if (cursor != null && cursorSprites != null && cursorSprites.Length == 2)
                cursor.sprite = cursorSprites[target != null ? 1 : 0];

            var seatedActor = sensor != null ? sensor.GetComponentInParent<PlayerSeating>() : null;
            if (!isTradeOpen && seatedActor != null && seatedActor.IsSeated)
            {
                Prompt.SetActive(true);
                keyText.text = "Q";
                InteractionText.text = "Stand up";
                return;
            }
            if (!isTradeOpen && target != null)
            {
                Prompt.SetActive(true);
                keyText.text = "E";
                InteractionText.text = target.Prompt;
                return;
            }
            Prompt.SetActive(false);
        }
    }
}
