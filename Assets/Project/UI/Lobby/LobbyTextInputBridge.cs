using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BuyBidBlow.UI.Lobby
{
    // uGUI InputField obtains IME information through BaseInput, even with InputSystemUIInputModule.
    public sealed class LobbyTextInputBridge : BaseInput
    {
        Keyboard keyboard;
        string composition="";
        Vector2 cursor;
        IMECompositionMode mode=IMECompositionMode.Auto;
        // 현재 키보드의 IME 조합 변경 이벤트 구독
        protected override void OnEnable(){base.OnEnable();keyboard=Keyboard.current;if(keyboard!=null)keyboard.onIMECompositionChange+=Changed;}
        // IME 이벤트 구독 해제 및 조합 문자열 초기화
        protected override void OnDisable(){if(keyboard!=null)keyboard.onIMECompositionChange-=Changed;keyboard=null;composition="";base.OnDisable();}
        // IME 조합 문자열 갱신
        void Changed(IMECompositionString value){composition=value.ToString();}
        public override string compositionString=>composition;
        public override IMECompositionMode imeCompositionMode {get=>mode;set{mode=value;keyboard?.SetIMEEnabled(value==IMECompositionMode.On);}}
        public override Vector2 compositionCursorPos {get=>cursor;set{cursor=value;keyboard?.SetIMECursorPosition(value);}}
        public override bool touchSupported=>false;
        public override int touchCount=>0;
        public override bool mousePresent=>Mouse.current!=null;
        public override Vector2 mousePosition=>Mouse.current!=null?Mouse.current.position.ReadValue():Vector2.zero;
    }
}
