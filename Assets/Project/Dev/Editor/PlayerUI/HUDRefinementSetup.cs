using System;
using System.IO;
using System.Linq;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.Notebook;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad]
    public static class HUDRefinementSetup
    {
        static HUDRefinementSetup() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if(!File.Exists("Library/HUDRefinement.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying) return;
            File.Delete("Library/HUDRefinement.request");
            try { Apply(); File.WriteAllText("Library/HUDRefinement.result","PASS"); }
            catch(Exception e) { File.WriteAllText("Library/HUDRefinement.result",e.ToString()); Debug.LogException(e); }
        }
        public static void Apply()
        {
            const string trading = "Assets/Project/UI/Market/TradingPanel/";
            foreach(var name in new[]{"PF_TradingPanel","PF_TradingPanel_Straw","PF_TradingPanel_Timber","PF_TradingPanel_Brick"})
                Edit(trading+name+".prefab", root=>
                {
                    var summary=root.transform.Find("Payment Summary");
                    var label=summary.Find("Cash After Label").GetComponent<Text>();
                    var value=summary.Find("Cash After").GetComponent<Text>();
                    Place(label.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(300,-163),new Vector2(240,43));
                    Place(value.rectTransform,new Vector2(1,1),new Vector2(1,1),new Vector2(-30,-163),new Vector2(200,43));
                    label.text="거래 후 현금"; label.fontSize=24; label.alignment=TextAnchor.MiddleLeft;
                    value.fontSize=30; value.alignment=TextAnchor.MiddleRight;
                    label.resizeTextForBestFit=value.resizeTextForBestFit=true;
                    label.resizeTextMinSize=18;label.resizeTextMaxSize=24;value.resizeTextMinSize=20;value.resizeTextMaxSize=30;
                });
            Edit("Assets/Project/UI/Auction/Panel/PF_AuctionPanel.prefab", root=>
            {
                var view=root.GetComponent<AuctionPanelView>(); var so=new SerializedObject(view);
                var old=so.FindProperty("lotDetails").objectReferenceValue as Text;
                if(old) old.transform.parent.gameObject.SetActive(false);
                var previous=root.transform.Find("Lot Summary");if(previous) Object.DestroyImmediate(previous.gameObject);
                var panel=Rect("Lot Summary",root.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-180),new Vector2(270,110));
                panel.gameObject.AddComponent<Image>().color=new Color(.015f,.06f,.05f,.72f);
                var rounds=new Text[2];var icons=new Image[4];
                var companyIcons=new[]{"Straw","Timber","Brick"}.Select(c=>AssetDatabase.LoadAssetAtPath<Sprite>(trading+"Sprites/IC_"+c+".png")).ToArray();
                for(int row=0;row<2;row++)
                {
                    var text=Rect("Round "+row,panel,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-10-row*48),new Vector2(78,42)).gameObject.AddComponent<Text>();
                    text.font=old.font;text.fontSize=28;text.alignment=TextAnchor.MiddleLeft;text.color=new Color(1,.96f,.85f);text.text=(2+row)+"R";rounds[row]=text;
                    for(int col=0;col<2;col++)
                    {
                        int i=row*2+col;var image=Rect("Company "+i,panel,new Vector2(0,1),new Vector2(0,1),new Vector2(120+col*62,-10-row*48),new Vector2(42,42)).gameObject.AddComponent<Image>();
                        image.sprite=companyIcons[(row+col)%3]; image.preserveAspect=true;icons[i]=image;
                    }
                }
                so.FindProperty("lotSummary").objectReferenceValue=panel.gameObject;
                Assign(so,"lotRounds",rounds);Assign(so,"lotCompanies",icons);Assign(so,"companyIcons",companyIcons);so.ApplyModifiedPropertiesWithoutUndo();
                panel.gameObject.SetActive(false);
                foreach(var graphic in panel.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            });
            Edit(NetworkHUDSetup.PrefabPath,root=>
            {
                var existing=root.GetComponent<NetworkNotebookController>();
                if(existing && existing.view) Object.DestroyImmediate(existing.view.gameObject);
                var notebook=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/UI/Notebook/PF_InformationNotebook.prefab"),root.transform);
                var controller=existing?existing:root.AddComponent<NetworkNotebookController>();controller.view=notebook.GetComponent<InformationNotebookView>();
                notebook.transform.SetAsLastSibling();notebook.SetActive(false);
                foreach(var text in notebook.GetComponentsInChildren<Text>(true))
                    if(text.name=="Keyboard Guide")text.text="↑ ↓ 스크롤 · Q / Tab 닫기";
                var hint=root.transform.Find("Notebook Hint");if(hint)Object.DestroyImmediate(hint.gameObject);
                var label=Rect("Notebook Hint",root.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(35,-246),new Vector2(300,32)).gameObject.AddComponent<Text>();
                var source=root.transform.Find("Assets - Top Left/Cash").GetComponent<Text>();
                label.font=source.font;label.fontSize=22;label.color=new Color(1,.96f,.85f);label.text="[Tab] 내 정보";label.raycastTarget=false;
            });
            AssetDatabase.SaveAssets();
        }
        static void Edit(string path,Action<GameObject> change)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try{change(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Assign(SerializedObject so,string name,Object[] values){var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
        static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);Place(r,anchor,pivot,pos,size);return r;}
        static void Place(RectTransform r,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;}
    }
}
