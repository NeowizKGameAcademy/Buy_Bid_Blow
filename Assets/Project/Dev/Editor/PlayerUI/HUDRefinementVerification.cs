using System;
using System.IO;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Auction;
using BuyBidBlow.UI.Market;
using BuyBidBlow.UI.Notebook;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class HUDRefinementVerification
    {
        public static void Run()
        {
            var host=new GameObject("HUD layout verification",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            host.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            try
            {
                foreach(var company in new[]{"Straw","Timber","Brick"})
                {
                    var panel=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/UI/Market/TradingPanel/PF_TradingPanel_"+company+".prefab"),host.transform);
                    panel.SetActive(true);
                    int quantity=1;panel.GetComponent<TradingPanelView>().Present(new TradingPanelData{company=company,price=21,stock=12,owned=1,available=179},0,ref quantity);
                    Canvas.ForceUpdateCanvases();
                    var label=panel.transform.Find("Payment Summary/Cash After Label").GetComponent<Text>();
                    var value=panel.transform.Find("Payment Summary/Cash After").GetComponent<Text>();
                    var a=new Vector3[4];var b=new Vector3[4];label.rectTransform.GetWorldCorners(a);value.rectTransform.GetWorldCorners(b);
                    Check(label.text=="거래 후 현금" && value.text=="$158",company+" cash labels");
                    Check(a[2].x<b[0].x,company+" non-overlapping text rectangles");Object.DestroyImmediate(panel);
                }
                var auction=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/UI/Auction/Panel/PF_AuctionPanel.prefab"),host.transform);
                auction.GetComponent<AuctionPanelView>().Present(new AuctionPanelData{seated=true,live=true,lot=new Lot(new Info(2,Company.Straw,Direction.Rise,Company.Brick,Direction.Fall),new Info(3,Company.Straw,Direction.Fall,Company.Timber,Direction.Rise))});
                Check(auction.transform.Find("Lot Summary").gameObject.activeSelf,"compact lot visible");
                Check(!auction.transform.Find("Live Lot Details").gameObject.activeSelf,"legacy lot hidden");
                Check(auction.transform.Find("Lot Summary/Round 0").GetComponent<Text>().text=="2R","round text");
                Check(auction.transform.Find("Lot Summary/Company 1").GetComponent<Image>().sprite.name.Contains("Brick"),"company icon");
                var hud=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NetworkHUDSetup.PrefabPath));
                try
                {
                    var controller=hud.GetComponent<NetworkNotebookController>();Check(controller && controller.view,"notebook connected");
                    var view=controller.view;
                    view.Present(2,17,new[]{new Info(3,Company.Straw,Direction.Rise,Company.Brick,Direction.Fall)});
                    view.SetOpen(true,2);Check(view.IsOpen && view.gameObject.activeSelf,"notebook opens");
                    // Visible rows begin at 1R, so 3R is row 2.
                    Check(view.cellValues[6].text=="↑ 상승" && view.cellValues[8].text=="↓ 하락","owned information cells");
                    Check(view.cellValues[7].text=="","unknown company stays empty");
                    view.SetOpen(false,2);Check(!view.IsOpen,"notebook closes");
                    Check(hud.transform.Find("Notebook Hint").GetComponent<Text>().text.Contains("Tab"),"Tab hint");
                }
                finally{Object.DestroyImmediate(hud);}
                File.WriteAllText("Library/HUDRefinementVerification.result","PASS: 3 company cash rows do not overlap; compact round/company icons; notebook connected; known/unknown cells; open/close; Tab hint. Keyboard input is verified separately in Play mode.");
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void Check(bool ok,string message){if(!ok)throw new Exception("HUD refinement: "+message);}
    }
}
