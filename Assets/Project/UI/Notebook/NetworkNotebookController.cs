using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.UI.Notebook
{
    [DefaultExecutionOrder(-200)]
    public sealed class NetworkNotebookController : MonoBehaviour
    {
        public InformationNotebookView view;
        public PlayerMove movement;
        public TradePanelController trading;
        static NetworkNotebookController current;
        static int closedFrame = -1;
        // The closing frame still blocks, so the Tab or Q that closed the notebook does not reach gameplay.
        public static bool BlocksInput => (current && current.view && current.view.IsOpen) || closedFrame == Time.frameCount;
        static bool IsBlocking() => BlocksInput;
        void OnEnable()
        {
            current = this;
            PlayerInputGate.AddBlocker(IsBlocking);
            var owner=GetComponentInParent<PlayerNet>(true);
            if(owner)
            {
                movement=owner.GetComponent<PlayerMove>();
                trading=owner.GetComponentInChildren<TradePanelController>(true);
            }
            else
            {
                if(!movement) movement = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
                if(!trading) trading = FindFirstObjectByType<TradePanelController>(FindObjectsInactive.Include);
            }
            PlayerNet.LocalInformationChanged += Refresh; Refresh();
        }
        void OnDisable()
        {
            PlayerNet.LocalInformationChanged -= Refresh;
            if(view) view.SetOpen(false,1);
            if(current == this) { current = null; PlayerInputGate.RemoveBlocker(IsBlocking); }
        }
        void Update()
        {
            Refresh();
            if(BuyBidBlow.UI.Menu.GameMenuController.BlocksInput)return;
            if(BuyBidBlow.UI.HUD.NetworkMatchFeedback.BlocksInput){Close();return;}
            if (!MatchNet.Current || PlayerNet.LocalPortfolio == null) { Close(); return; }
            var k = Keyboard.current;
            if(k == null) return;
            if(k.tabKey.wasPressedThisFrame)
            {
                if(view.IsOpen) Close();
                else if(!trading || !trading.IsOpen) view.SetOpen(true,MatchNet.Current.round);
            }
            else if(view.IsOpen && k.qKey.wasPressedThisFrame) Close();
            if(!view.IsOpen) return;
            if(movement) { movement.SetMoveInput(Vector2.zero); movement.SetJumpHeld(false); }
            if(k.wKey.wasPressedThisFrame) view.Scroll(-1);
            if(k.sKey.wasPressedThisFrame) view.Scroll(1);
        }
        public void Open()=>OpenInformation(false);
        public void OpenInformation(bool newlyAcquired)
        {
            if(!MatchNet.Current || PlayerNet.LocalPortfolio==null)return;
            if(trading && trading.IsOpen)trading.Close();
            view.SetOpen(true,MatchNet.Current.round,newlyAcquired?"information.reveal":"ui.notebook");
        }
        public void Close()
        {
            if(!view.IsOpen) return;
            view.SetOpen(false,MatchNet.Current ? MatchNet.Current.round : 1); closedFrame = Time.frameCount;
        }
        void Refresh()
        {
            var match = MatchNet.Current;
            view.Present(match ? Mathf.Max(1,match.round) : 1, match && match.totalRounds > 0 ? match.totalRounds : 13, PlayerNet.LocalInformation);
        }
    }
}
