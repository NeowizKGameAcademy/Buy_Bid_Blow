using System;
using System.IO;
using System.Linq;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using BuyBidBlow.UI.Notebook;
using BuyBidBlow.UI.Result;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad]
    public static class MatchFeedbackSetup
    {
        static MatchFeedbackSetup(){EditorApplication.update+=()=>
        {
            if(!File.Exists("Library/MatchFeedbackSetup.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)return;
            File.Delete("Library/MatchFeedbackSetup.request");try{Apply();File.WriteAllText("Library/MatchFeedbackSetup.result","PASS");}catch(Exception e){File.WriteAllText("Library/MatchFeedbackSetup.result",e.ToString());Debug.LogException(e);}
        };}
        public static void Apply()
        {
            const string cursorPath="Assets/Project/UI/HUD/Cursor/WolfPaw_Source.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(cursorPath);
            importer.textureType=TextureImporterType.Cursor;importer.maxTextureSize=64;importer.isReadable=true;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var pointer=AssetDatabase.LoadAssetAtPath<Texture2D>(cursorPath);
            Edit("Assets/Project/Characters/Player/Player.prefab",root=>
            {
                var cursor=root.GetComponentInChildren<CursorController>(true);var so=new SerializedObject(cursor);
                so.FindProperty("pointer").objectReferenceValue=pointer;so.ApplyModifiedPropertiesWithoutUndo();
            });
            foreach(var name in new[]{"PF_TradingPanel","PF_TradingPanel_Straw","PF_TradingPanel_Timber","PF_TradingPanel_Brick"})
                Edit("Assets/Project/UI/Market/TradingPanel/"+name+".prefab",root=>
                {
                    foreach(var g in root.GetComponentsInChildren<Graphic>(true))g.raycastTarget=false;
                    foreach(var button in root.GetComponentsInChildren<Button>(true))
                        if(button.targetGraphic)button.targetGraphic.raycastTarget=true;
                    var key=root.transform.Find("Close Hint/Keycap");
                    if(key && !key.GetComponent<Button>())
                    {
                        var button=key.gameObject.AddComponent<Button>();button.targetGraphic=key.GetComponent<Image>();
                        if(button.targetGraphic)button.targetGraphic.raycastTarget=true;
                    }
                });
            Edit(NetworkHUDSetup.PrefabPath,root=>
            {
                if(!root.GetComponent<GraphicRaycaster>())root.AddComponent<GraphicRaycaster>();
                foreach(var n in new[]{"Match Notices","Auction Result","Match Results","Active Bet"}){var old=root.transform.Find(n);if(old)Object.DestroyImmediate(old.gameObject);}
                var feedback=root.GetComponent<NetworkMatchFeedback>();if(!feedback)feedback=root.AddComponent<NetworkMatchFeedback>();
                feedback.notebook=root.GetComponent<NetworkNotebookController>();
                feedback.notices=Instantiate<MarketNoticeView>("Assets/Project/UI/Market/Notifications/PF_MarketNotices.prefab",root.transform,"Match Notices");
                feedback.notices.gameObject.SetActive(true);
                var stack=(RectTransform)feedback.notices.transform;
                stack.anchorMin=stack.anchorMax=stack.pivot=new Vector2(1,1);stack.anchoredPosition=new Vector2(-30,-155);
                feedback.auctionResult=Instantiate<AuctionResultView>("Assets/Project/UI/Auction/Result/PF_AuctionResult.prefab",root.transform,"Auction Result");
                feedback.results=Instantiate<MatchResultView>("Assets/Project/UI/Result/PF_MatchResults.prefab",root.transform,"Match Results");
                feedback.auctionResult.gameObject.SetActive(false);feedback.results.gameObject.SetActive(false);
                foreach(var modal in new BuyBidBlow.UI.Shared.ModalView[]{feedback.auctionResult,feedback.results})
                    foreach(var button in modal.Buttons)if(button.targetGraphic)button.targetGraphic.raycastTarget=true;
                var bet=Rect("Active Bet",root.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(35,-242),new Vector2(430,48));
                bet.gameObject.AddComponent<Image>().color=new Color(.015f,.06f,.05f,.8f);
                var icon=Rect("Company Icon",bet,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(12,0),new Vector2(36,36)).gameObject.AddComponent<Image>();icon.preserveAspect=true;
                var label=Rect("Bet Status",bet,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(62,0),new Vector2(355,40)).gameObject.AddComponent<Text>();
                label.font=root.transform.Find("Assets - Top Left/Cash").GetComponent<Text>().font;label.fontSize=23;label.color=new Color(.35f,.8f,1);label.alignment=TextAnchor.MiddleLeft;
                feedback.betStatus=bet.gameObject;feedback.betIcon=icon;feedback.betLabel=label;
                feedback.companyIcons=new[]{"Straw","Timber","Brick"}.Select(c=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/UI/Market/TradingPanel/Sprites/IC_"+c+".png")).ToArray();
                bet.gameObject.SetActive(false);foreach(var g in bet.GetComponentsInChildren<Graphic>(true))g.raycastTarget=false;
                root.transform.Find("Notebook Hint").GetComponent<RectTransform>().anchoredPosition=new Vector2(35,-302);
            });
            AssetDatabase.SaveAssets();
        }
        static T Instantiate<T>(string path,Transform parent,string name)where T:Component
        {var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);instance.name=name;return instance.GetComponent<T>();}
        static void Edit(string path,Action<GameObject> action){var root=PrefabUtility.LoadPrefabContents(path);try{action(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    }
}
