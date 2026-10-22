using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BuyBidBlow.Audio
{
    public sealed class UIAudioFeedback:MonoBehaviour,ISelectHandler,IPointerEnterHandler
    {
        Button button;
        public static void Bind(Transform root)
        {
            foreach(var button in root.GetComponentsInChildren<Button>(true))
                if(!button.GetComponent<UIAudioFeedback>())button.gameObject.AddComponent<UIAudioFeedback>();
        }
        void Awake(){button=GetComponent<Button>();if(button)button.onClick.AddListener(Click);}
        public void OnSelect(BaseEventData data){if(button&&button.interactable)GameAudio.Play("ui.navigate",this);}
        public void OnPointerEnter(PointerEventData data){if(button&&button.interactable)GameAudio.Play("ui.navigate",this);}
        void Click()
        {
            if(GetComponentInParent<BuyBidBlow.UI.Menu.HowToPlayPanel>())return;
            if(GetComponentInParent<BuyBidBlow.UI.Market.TradePanelController>() || GetComponentInParent<BuyBidBlow.UI.Shared.ModalView>())return;
            // Trading panels are sibling children of the HUD, not the trade controller.
            if(GetComponentInParent<BuyBidBlow.UI.Market.TradingPanelView>())return;
            string label=button.GetComponentInChildren<Text>()?.text ?? name;
            GameAudio.Play(label.Contains("닫기")||name.ToLowerInvariant().Contains("close")?"ui.close":"ui.confirm",this);
        }
    }
}
