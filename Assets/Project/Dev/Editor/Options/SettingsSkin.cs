using BuyBidBlow.UI.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BuyBidBlow.UI.Menu.MenuWidgets;

namespace BuyBidBlow.Dev.Editor
{
    // Art is sliced; labels, values, hit areas and slider mechanics remain editable uGUI.
    static class SettingsSkin
    {
        const string Folder = "Assets/Project/UI/Menu/Options/Sprites/";
        public static void Import()
        {
            AssetDatabase.Refresh();
            foreach (var file in System.IO.Directory.GetFiles(Folder, "*.png"))
            {
                var path = file.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.spritePixelsPerUnit = 100;
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                importer.spriteBorder = name == "SettingsPanel" ? new Vector4(110,110,110,110) :
                    name.Contains("Button") || name.StartsWith("Primary") ? new Vector4(40,30,40,30) :
                    name == "WoodTitle" ? new Vector4(65,40,65,40) : Vector4.zero;
                importer.SaveAndReimport();
            }
        }
        public static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + name + ".png");
        static Image Picture(string name, Transform p, string art, float x, float y, float w, float h, bool sliced = false)
        {
            var image = Panel(name,p,x,y,w,h,Color.white);
            image.sprite = Art(art); image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false; return image;
        }
        public static Button StyledButton(string name, Transform p, string label,float x,float y,float w=260,float h=58,bool primary=false)
        {
            var image = Picture(name,p,primary?"PrimaryNormal":"ButtonNormal",x,y,w,h,true);
            image.raycastTarget = true;
            var b=image.gameObject.AddComponent<Button>(); b.targetGraphic=image;
            b.navigation=new Navigation{mode=Navigation.Mode.None}; b.transition=Selectable.Transition.SpriteSwap;
            b.spriteState=new SpriteState{highlightedSprite=Art(primary?"PrimarySelected":"ButtonSelected"),selectedSprite=Art(primary?"PrimarySelected":"ButtonSelected"),pressedSprite=Art(primary?"PrimaryPressed":"ButtonPressed"),disabledSprite=Art("ButtonDisabled")};
            var t=Text("Label",image.transform,label,0,0,w-24,h-12,32);
            t.alignment=TextAnchor.MiddleCenter;
            if(primary)t.color=new Color(.13f,.08f,.025f);
            return b;
        }
        public static Image Window(string name,Transform p,float w,float h,string title)
        {
            var panel=Picture(name,p,"SettingsPanel",0,0,w,h,true);panel.raycastTarget=true;
            var plaque=Picture("Wood Title",panel.transform,"WoodTitle",0,h/2-65,Mathf.Min(w-90,660),100,true);
            string emblem=name=="Detailed Results"?"ResultsIcon":name=="Controls"?"ControlsIcon":name=="Leave Confirmation"?"HomeIcon":"ResumeIcon";
            Picture("Header Emblem",plaque.transform,emblem,-plaque.rectTransform.sizeDelta.x/2+60,0,45,45);
            var text=Text("Title",plaque.transform,title,24,0,Mathf.Min(w-205,510),70,40);
            text.alignment=TextAnchor.MiddleCenter;text.resizeTextForBestFit=true;text.resizeTextMinSize=28;text.resizeTextMaxSize=40;
            Picture("Brass Divider",panel.transform,"Divider",0,h/2-128,w-110,6).color=new Color(1,1,1,.4f);
            return panel;
        }
        public static Button Launcher(Transform parent)
        {
            var image=Picture("Options Gear Button",parent,"GearNormal",0,0,64,64);
            image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
            b.navigation=new Navigation{mode=Navigation.Mode.None};b.transition=Selectable.Transition.SpriteSwap;
            b.spriteState=new SpriteState{highlightedSprite=Art("GearSelected"),selectedSprite=Art("GearSelected"),pressedSprite=Art("GearPressed"),disabledSprite=Art("GearDisabled")};
            image.rectTransform.anchorMin=image.rectTransform.anchorMax=new Vector2(1,1);
            image.rectTransform.pivot=new Vector2(1,1);image.rectTransform.anchoredPosition=new Vector2(-26,-35);
            return b;
        }
        public static void AlignHudTimer()
        {
            const string path="Assets/Project/UI/HUD/PF_NetworkPlayerHUD.prefab";
            var hud=PrefabUtility.LoadPrefabContents(path);
            try{var timer=(RectTransform)hud.transform.Find("Round Timer - Top Right");timer.anchoredPosition=new Vector2(-110,-24);PrefabUtility.SaveAsPrefabAsset(hud,path);}
            finally{PrefabUtility.UnloadPrefabContents(hud);}
        }
        public static void Assemble(GameMenuController view,Transform parent)
        {
            var panel=Picture("Settings",parent,"SettingsPanel",0,-20,1120,850,true);
            panel.raycastTarget=true; view.settingsPanel=panel.gameObject;
            var title=Picture("Wood Title",panel.transform,"WoodTitle",0,421,390,120,true);
            Text("Title",title.transform,"설정",0,-4,280,80,52).alignment=TextAnchor.MiddleCenter;
            Picture("Gear",panel.transform,"GearMedallion",0,486,78,78);
            view.sliders=new Slider[4]; view.values=new Text[4]; view.settingHighlights=new Image[5];
            var round=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/UI/Market/TradingPanel/Sprites/BG_RoundedDim.png");
            for(int i=0;i<5;i++)
            {
                float y=i<4?260-i*108:-185;
                var hi=Picture("Selection "+i,panel.transform,"ButtonSelected",0,y,994,87,true);
                hi.color=new Color(1,.8f,.4f,.18f);view.settingHighlights[i]=hi;
                string label=new[]{"전체 음량","배경음","효과음","마우스 감도","화면 모드"}[i];
                Text("Label "+label,panel.transform,label,-330,y,260,64,33);
                if(i==4)break;
                var root=Rect("Slider "+label,panel.transform,95,y,430,66);
                var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
                var slider=root.gameObject.AddComponent<Slider>();slider.minValue=i==3?1:0;slider.maxValue=100;slider.wholeNumbers=true;
                slider.navigation=new Navigation{mode=Navigation.Mode.None};
                var track=Panel("Recessed Track",root,0,0,430,22,new Color(.015f,.045f,.04f));track.sprite=round;track.type=Image.Type.Sliced;track.raycastTarget=false;
                var area=Rect("Fill Area",root,0,0,410,12);
                var fill=Panel("Brass Fill",area,0,0,410,12,new Color(.76f,.52f,.22f));fill.sprite=round;fill.type=Image.Type.Sliced;fill.raycastTarget=false;
                fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
                var handles=Rect("Handle Area",root,0,0,410,0);
                var handle=Picture("Brass Handle",handles,"SliderHandle",0,0,29,52);
                slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
                slider.colors=new ColorBlock{normalColor=Color.white,highlightedColor=new Color(1,.9f,.65f),selectedColor=new Color(1,.9f,.65f),pressedColor=new Color(.78f,.65f,.42f),disabledColor=Color.gray,colorMultiplier=1,fadeDuration=.08f};
                view.sliders[i]=slider;
                view.values[i]=Text("Value "+label,panel.transform,"80%",445,y,110,64,34);
                view.values[i].alignment=TextAnchor.MiddleRight;
                Picture("Divider "+i,panel.transform,"Divider",0,y-55,980,6).color=new Color(1,1,1,.28f);
            }
            view.display=StyledButton("Display Mode",panel.transform,"창 모드",95,-185,330,65);
            view.displayValue=view.display.GetComponentInChildren<Text>();view.displayValue.fontSize=28;
            view.displayPrevious=StyledButton("Previous Display",panel.transform,"◀",-112,-185,64,65);
            view.displayNext=StyledButton("Next Display",panel.transform,"▶",302,-185,64,65);
            Picture("Footer Divider",panel.transform,"Divider",0,-241,980,6).color=new Color(1,1,1,.3f);
            view.defaults=StyledButton("Defaults",panel.transform,"기본값",-335,-305,290);
            view.apply=StyledButton("Apply",panel.transform,"적용·저장",0,-305,320,78,true);
            view.closeSettings=StyledButton("Close Settings",panel.transform,"닫기",335,-305,290);
            view.status=Text("Status",panel.transform,"W/S 항목 · A/D 조절 · E 실행 · Esc 닫기",0,-385,980,42,23);
            view.status.alignment=TextAnchor.MiddleCenter;
        }
    }
}
