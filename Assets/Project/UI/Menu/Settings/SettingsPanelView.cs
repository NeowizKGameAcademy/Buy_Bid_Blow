using System;
using BuyBidBlow.UI.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Menu
{
    [Serializable] public struct SettingsValues
    {
        public int Master, Music, Effects, Sensitivity, DisplayMode;
        public static SettingsValues Defaults => new SettingsValues { Master=80, Music=40, Effects=70, Sensitivity=50, DisplayMode=0 };
    }
    public sealed class SettingsPanelView : ModalView
    {
        public Text[] ValueLabels;
        public Image[] Fills, RowHighlights;
        public event Action<SettingsValues> ApplyRequested;
        public SettingsValues Draft { get; private set; }
        private SettingsValues applied;
        private int row;
        public void ShowSettings(SettingsValues current)
        {
            applied = current; Draft = current; row = 0;
            SetButtons("기본값", "적용", "닫기"); Show("설정", "사운드 · 조작 · 화면"); Render();
        }
        public void Adjust(int direction)
        {
            var v = Draft;
            switch(row) { case 0: v.Master=Mathf.Clamp(v.Master+direction*5,0,100);break;case 1:v.Music=Mathf.Clamp(v.Music+direction*5,0,100);break;case 2:v.Effects=Mathf.Clamp(v.Effects+direction*5,0,100);break;case 3:v.Sensitivity=Mathf.Clamp(v.Sensitivity+direction*5,1,100);break;case 4:v.DisplayMode=(v.DisplayMode+direction+3)%3;break; }
            Draft=v; Render();
        }
        public void ResetDraft() { Draft=SettingsValues.Defaults; Render(); }
        public void ApplyDraft() { applied=Draft; ApplyRequested?.Invoke(applied); Footer.text="적용 요청 완료 · ↑↓ 항목 / ←→ 조절 / Esc 닫기"; }
        public void DiscardAndClose() { Draft=applied; Hide(); }
        private void Render()
        {
            var v=Draft; int[] values={v.Master,v.Music,v.Effects,v.Sensitivity};
            for(int i=0;i<4;i++){ValueLabels[i].text=values[i].ToString();Fills[i].fillAmount=values[i]/100f;}
            ValueLabels[4].text=new[]{"창 모드","전체 화면","테두리 없는 창"}[Mathf.Clamp(v.DisplayMode,0,2)];
            for(int i=0;i<5;i++)RowHighlights[i].color=i==row?new Color(.85f,.64f,.26f,.18f):Color.clear;
            if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(row>=5?Buttons[row-5].gameObject:null);
        }
        protected override void Update()
        {
            var k=Keyboard.current;if(k==null)return;
            if(k.escapeKey.wasPressedThisFrame){DiscardAndClose();return;}
            if(k.upArrowKey.wasPressedThisFrame){row=(row+7)%8;Render();}
            else if(k.downArrowKey.wasPressedThisFrame){row=(row+1)%8;Render();}
            if(k.leftArrowKey.wasPressedThisFrame)Adjust(-1);else if(k.rightArrowKey.wasPressedThisFrame)Adjust(1);
            if(k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame){if(row==5)ResetDraft();else if(row==6)ApplyDraft();else if(row==7)DiscardAndClose();}
        }
    }
}
