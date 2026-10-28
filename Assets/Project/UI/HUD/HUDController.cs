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
        [SerializeField] private InformationNotebookView notebookPrefab;
        [SerializeField] private MatchResultView resultsPrefab;
        public InformationNotebookView Notebook { get; private set; }
        public MatchResultView Results { get; private set; }
        public bool ModalOpen => (Notebook && Notebook.IsOpen) || (Results && Results.IsOpen);
        public int ModalClosedFrame { get; private set; } = -1;
        private PlayerController input;
        private MatchNet shownResults;
        private uint privateNoticeVersion, publicNoticeVersion, resultRevision;
        private bool lastCanReturn;
        private string notice;
        private float noticeUntil;

        private void Awake()
        {
            input = GetComponentInParent<PlayerController>();
            if (notebookPrefab)
            {
                Notebook = Instantiate(notebookPrefab, transform, false);
                Notebook.OpenChanged += open => { if (!open) ModalClosedFrame = Time.frameCount; UpdateInputLock(); };
            }
            if (resultsPrefab)
            {
                Results = Instantiate(resultsPrefab, transform, false);
                Results.CloseOnAction = false;
                Results.OpenChanged += _ => UpdateInputLock();
                Results.ActionRequested += _ => { if (NetworkClient.localPlayer) NetworkClient.localPlayer.GetComponent<PlayerNet>().CmdReturnToLobby(); };
                Results.Buttons[0].onClick.AddListener(() => Results.Submit(0));
            }
        }

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
            if (Notebook && Notebook.IsOpen) Notebook.SetOpen(false, 1);
            if (Results) Results.Hide();
            if (input) input.SetModalBlocked(false);
            shownResults = null; privateNoticeVersion = publicNoticeVersion = 0;
            notice = null; noticeUntil = 0; resultRevision = 0; ModalClosedFrame = -1;
            if (tradePanel != null)
                tradePanel.OpenStateChanged -= RefreshInteractionUI;
        }

        // 매 프레임 상호작용 UI와 보유 현금 표시 갱신
        private void Update()
        {
            if (Time.unscaledTime >= noticeUntil) RefreshInteractionUI(tradePanel != null && tradePanel.IsOpen);
            if (assets != null)
                PlayerMoney.text = NetworkClient.active && !assets.HasServerState ? "—" : string.Format("$ {0:N0}", assets.Cash);
            RefreshMatchUI();
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
                var radial = input ? input.GetComponentInChildren<RadialMenuController>(true) : GetComponentInChildren<RadialMenuController>(true);
                if (radial) radial.EndSelection(false);
            }
            if (input) input.SetModalBlocked(ModalOpen);
        }

        private void ShowReply(RequestResult result)
        {
            if (result.Kind == RequestKind.Bid || result.Kind == RequestKind.Seat ||
                result.Kind == RequestKind.StartMatch || result.Kind == RequestKind.ReturnToLobby)
                ShowNotice(result.Message);
        }
        private void ShowNotice(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            notice = message; noticeUntil = Time.unscaledTime + 4;
        }

        private void RefreshMatchUI()
        {
            var match = MatchNet.Current;
            if (!match) return;
            var local = NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            if (!local) return;
            var state = local.GetComponent<PlayerAssetsNet>();
            if (match.auctionResultVersion != publicNoticeVersion) { publicNoticeVersion = match.auctionResultVersion; ShowNotice(match.auctionResult); }
            if (state.NoticeVersion != privateNoticeVersion) { privateNoticeVersion = state.NoticeVersion; ShowNotice(state.Notice); }
            if (Notebook)
            {
                Notebook.Present(Math.Max(1, match.round), match.totalRounds, PlayerNet.LocalInformation);
                var keyboard = Keyboard.current;
                if (match.phase != MatchNet.Phase.Finished && keyboard != null && keyboard.iKey.wasPressedThisFrame)
                    Notebook.SetOpen(!Notebook.IsOpen, Math.Max(1, match.round));
                if (Notebook.IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Notebook.SetOpen(false, match.round);
                if (Notebook.IsOpen && Mouse.current != null)
                {
                    float scroll = Mouse.current.scroll.ReadValue().y;
                    if (scroll != 0) Notebook.Scroll(scroll > 0 ? -1 : 1);
                }
            }
            if (match.phase != MatchNet.Phase.Finished || !Results || match.results.Count == 0) return;
            bool canReturn = local.seat == PlayerNet.All.Where(p => p && p.inMatch).Select(p => p.seat).DefaultIfEmpty(local.seat).Min();
            if (shownResults == match && resultRevision == state.Revision && lastCanReturn == canReturn) return;
            if (Notebook && Notebook.IsOpen) Notebook.SetOpen(false, match.round);
            var entries = new List<ResultEntry>();
            foreach (var row in match.results)
            {
                bool own = row.PlayerId == local.playerId;
                int cash = own ? PlayerNet.LocalPortfolio?.Cash ?? 0 : 0;
                int shares = own && PlayerNet.LocalPortfolio != null ? Enumerable.Range(0, 3).Sum(i => PlayerNet.LocalPortfolio.GetShares((Company)i)) : 0;
                entries.Add(new ResultEntry { Rank = row.Rank, Nickname = row.Nickname, TotalAssets = row.TotalAssets,
                    IsLocal = own, Cash = cash, StockValue = own ? row.TotalAssets - cash : 0, TotalShares = shares });
            }
            Results.ShowResults(entries, false, canReturn, match.totalRounds);
            shownResults = match; resultRevision = state.Revision; lastCanReturn = canReturn;
        }

        // 거래 패널/착석/감지 상태에 따라 커서와 상호작용 안내 문구 표시
        private void RefreshInteractionUI(bool isTradeOpen)
        {
            if (cursorImage != null)
                cursorImage.SetActive(!isTradeOpen);

            var seatedActor = detect != null ? detect.GetComponentInParent<PlayerSeating>() : null;
            if (!isTradeOpen && seatedActor != null && seatedActor.IsSeated)
            {
                Prompt.SetActive(true);
                keyText.text = "Esc";
                InteractionText.text = "Stand up";
                return;
            }
            if (!isTradeOpen && detect != null && detect.isDetected)
            {
                Prompt.SetActive(true);

                var hallTarget = detect.DetectedCollider != null
                    ? detect.DetectedCollider.GetComponentInParent<BuyBidBlow.Common.IInteractable>() : null;
                if (hallTarget != null && hallTarget.CanInteract)
                {
                    keyText.text = "E";
                    InteractionText.text = hallTarget.Prompt;
                    return;
                }

                switch (detect.detectingLayer)
                {
                    case 7:
                        keyText.text = "E";
                        InteractionText.text = "Trade";
                        break;
                    default:
                        Prompt.SetActive(false);
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
