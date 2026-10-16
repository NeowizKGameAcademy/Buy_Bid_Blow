using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BuyBidBlow.LobbySandbox
{
    // uGUI InputField obtains IME information through BaseInput, even with InputSystemUIInputModule.
    public sealed class LobbyTextInputBridge : BaseInput
    {
        Keyboard keyboard;
        string composition="";
        Vector2 cursor;
        IMECompositionMode mode=IMECompositionMode.Auto;
        protected override void OnEnable(){base.OnEnable();keyboard=Keyboard.current;if(keyboard!=null)keyboard.onIMECompositionChange+=Changed;}
        protected override void OnDisable(){if(keyboard!=null)keyboard.onIMECompositionChange-=Changed;keyboard=null;composition="";base.OnDisable();}
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
