using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Menu
{
    // Used by the editor assembler; every resulting element is a normal editable uGUI object.
    public static class MenuWidgets
    {
        public static Font Font;
        public static readonly Color Cream=new Color(.99f,.95f,.82f), Brass=new Color(.55f,.39f,.17f), Teal=new Color(.025f,.12f,.115f,.98f);
        public static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
        public static Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
        {var r=Rect(name,parent,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.color=color;return i;}
        public static Text Text(string name,Transform parent,string value,float x,float y,float w,float h,int size=28)
        {var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=Font;t.text=value;t.fontSize=size;t.color=Cream;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
        public static Button Button(string name,Transform parent,string value,float x,float y,float w=260,float h=58)
        {var bg=Panel(name,parent,x,y,w,h,new Color(.09f,.22f,.20f));var outline=bg.gameObject.AddComponent<Outline>();outline.effectColor=Brass;outline.effectDistance=new Vector2(2,-2);var b=bg.gameObject.AddComponent<Button>();b.targetGraphic=bg;b.navigation=new Navigation{mode=Navigation.Mode.None};var colors=b.colors;colors.highlightedColor=new Color(1,.82f,.42f);colors.selectedColor=colors.highlightedColor;b.colors=colors;Text("Label",bg.transform,value,0,0,w-20,h,26).alignment=TextAnchor.MiddleCenter;return b;}
        public static void Frame(Image panel){var o=panel.gameObject.AddComponent<Outline>();o.effectColor=Brass;o.effectDistance=new Vector2(3,-3);}
    }
}
