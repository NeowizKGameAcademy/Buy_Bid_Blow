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
        public static bool BlocksInput => (current && current.view && current.view.IsOpen) || closedFrame == Time.frameCount;
        void OnEnable()
        {
            current = this;
            if(!movement) movement = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);
            if(!trading) trading = FindFirstObjectByType<TradePanelController>(FindObjectsInactive.Include);
            PlayerNet.LocalInformationChanged += Refresh; Refresh();
        }
        void OnDisable()
        {
            PlayerNet.LocalInformationChanged -= Refresh;
            if(view) view.SetOpen(false,1);
            if(current == this) current = null;
        }
        void Update()
        {
            Refresh();
            if (!MatchNet.Current || PlayerNet.LocalPortfolio == null) { Close(); return; }
            var k = Keyboard.current;
            if(k == null) return;
            if(k.tabKey.wasPressedThisFrame)
            {
                if(view.IsOpen) Close();
                else if(!trading || !trading.IsOpen) view.SetOpen(true,MatchNet.Current.round);
            }
            else if(view.IsOpen && k.escapeKey.wasPressedThisFrame) Close();
            if(!view.IsOpen) return;
            if(movement) { movement.SetMoveInput(Vector2.zero); movement.SetJumpHeld(false); }
            if(k.upArrowKey.wasPressedThisFrame) view.Scroll(-1);
            if(k.downArrowKey.wasPressedThisFrame) view.Scroll(1);
        }
        void Close()
        {
            if(!view.IsOpen) return;
            view.SetOpen(false,MatchNet.Current ? MatchNet.Current.round : 1); closedFrame = Time.frameCount;
        }
        void Refresh()
        {
            var match = MatchNet.Current;
            view.Present(match ? Mathf.Max(1,match.round) : 1, match && match.totalRounds > 0 ? match.totalRounds : 17, PlayerNet.LocalInformation);
        }
    }
}
