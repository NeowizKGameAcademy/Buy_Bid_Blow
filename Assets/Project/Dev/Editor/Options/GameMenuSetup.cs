using System;
using System.IO;
using BuyBidBlow.UI.Menu;
using BuyBidBlow.UI.Result;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BuyBidBlow.UI.Menu.MenuWidgets;
namespace BuyBidBlow.Dev.Editor {
[InitializeOnLoad]public static class GameMenuSetup {
 static GameMenuSetup(){EditorApplication.update+=()=>{if(!File.Exists("Library/GameMenuSetup.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Library/GameMenuSetup.request");try{Build();File.WriteAllText("Library/GameMenuSetup.result","PASS");}catch(Exception e){File.WriteAllText("Library/GameMenuSetup.result",e.ToString());Debug.LogException(e);}};}
 [MenuItem("Tools/Buy Bid Blow/UI/Rebuild Shared Menu And Results")]
 public static void Build(){foreach(var host in UnityEngine.Object.FindObjectsByType<CueAudio.CueAudioHost>(FindObjectsSortMode.None)){if(host.gameObject.name=="GameAudioHost(Clone)"&&!Application.isPlaying)UnityEngine.Object.DestroyImmediate(host.gameObject);}MenuWidgets.Font=AssetDatabase.LoadAssetAtPath<UnityEngine.Font>("Assets/Font/public/static/alternative/Pretendard-Bold.ttf");SettingsSkin.Import();SettingsSkin.AlignHudTimer();Menu();Results();AssetDatabase.SaveAssets();}
 static Image Window(string name,Transform p,float w,float h,string title)=>SettingsSkin.Window(name,p,w,h,title);
 static Button Button(string name,Transform p,string label,float x,float y,float w=260,float h=58){var b=SettingsSkin.StyledButton(name,p,label,x,y,w,h);var t=b.GetComponentInChildren<Text>();t.fontSize=26;t.resizeTextForBestFit=true;t.resizeTextMinSize=18;t.resizeTextMaxSize=26;return b;}
 static void Menu(){
 var go=new GameObject("Shared Game Menu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=5000;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
 try{var view=go.AddComponent<GameMenuController>();var launch=SettingsSkin.Launcher(go.transform);view.launcher=launch;
 var shade=Panel("Dim",go.transform,0,0,1920,1080,new Color(0,0,0,.68f));shade.rectTransform.anchorMin=Vector2.zero;shade.rectTransform.anchorMax=Vector2.one;shade.rectTransform.offsetMin=shade.rectTransform.offsetMax=Vector2.zero;view.shade=shade.gameObject;
 var menu=Window("Game Menu",shade.transform,650,700,"게임 메뉴");view.menuPanel=menu.gameObject;
 view.resume=Button("Resume",menu.transform,"계속하기",0,140,420);view.settings=Button("Settings",menu.transform,"설정",0,50,420);view.controls=Button("Controls",menu.transform,"조작 안내",0,-40,420);view.leave=Button("Leave",menu.transform,"메인화면으로",0,-130,420);Text("Online Hint",menu.transform,"온라인 경기는 계속 진행됩니다.\nW/S 선택 · E 실행 · Esc 닫기",0,-255,530,90,24).alignment=TextAnchor.MiddleCenter;
 SettingsSkin.Assemble(view,shade.transform);
 var confirm=Window("Leave Confirmation",shade.transform,820,460,"메인화면으로 나갈까요?");view.leavePanel=confirm.gameObject;Text("Leave Explanation",confirm.transform,"현재 접속이 종료됩니다.\n경기 중이라면 이탈로 처리됩니다.",0,5,700,110,29).alignment=TextAnchor.MiddleCenter;view.cancelLeave=Button("Cancel Leave",confirm.transform,"취소",-185,-145,280);view.confirmLeave=Button("Confirm Leave",confirm.transform,"나가기",185,-145,280);
 var controls=Window("Controls",shade.transform,850,700,"조작 안내");view.controlsPanel=controls.gameObject;Text("Controls Body",controls.transform,"WASD 이동 · 마우스 시점 · Space 점프\nE 상호작용 · Q 닫기/퇴석\nTab 내 정보 · 수첩 W/S 스크롤\n거래: W/S 탭 · A/D 수량 · E 확인\n경매: A/D 금액 · E 입찰 · C 비공개 취소",0,-5,730,380,29);view.closeControls=Button("Close Controls",controls.transform,"돌아가기",0,-260,360);
 shade.gameObject.SetActive(false);PrefabUtility.SaveAsPrefabAsset(go,"Assets/Project/UI/Menu/Options/Resources/BBB_GameMenu.prefab");
 }finally{UnityEngine.Object.DestroyImmediate(go);}}
 static void Results(){var root=Window("Detailed Results",null,1640,930,"경기 결과");try{var v=root.gameObject.AddComponent<DetailedResultsView>();v.roundsLabel=Text("Rounds",root.transform,"13 / 13 ROUND · 최종 결과",0,325,1450,36,24);v.roundsLabel.alignment=TextAnchor.MiddleCenter;
 v.playerButtons=new Button[6];v.playerLabels=new Text[6];for(int i=0;i<6;i++){v.playerButtons[i]=Button("Player "+i,root.transform,"플레이어",-580,235-i*80,350,72);v.playerLabels[i]=v.playerButtons[i].GetComponentInChildren<Text>();v.playerLabels[i].fontSize=25;}
 v.title=Text("Selected Player",root.transform,"내 자산",220,270,1000,65,37);v.summaryTab=Button("Summary Tab",root.transform,"최종 자산 내역",0,190,310);v.graphTab=Button("Graph Tab",root.transform,"라운드별 자산 추이",370,190,350);
 var summary=Rect("Summary",root.transform,220,-90,1030,440);v.summaryPanel=summary.gameObject;v.total=Text("Total",summary,"총자산 $200",0,160,970,65,43);v.cash=Text("Cash",summary,"현금 $200",0,80,970,52,30);v.stock=Text("Stock Value",summary,"최종 주식 가치 $0",0,20,970,52,30);v.stockRows=new Text[3];for(int i=0;i<3;i++)v.stockRows[i]=Text("Company "+i,summary,"Straw 0주 × $20 = $0",0,-65-i*62,970,52,28);
 var chart=Rect("Graph",root.transform,220,-90,1030,470);v.graphPanel=chart.gameObject;var graph=Rect("Asset History Graph",chart,45,40,830,280);graph.gameObject.AddComponent<CanvasRenderer>();v.graph=graph.gameObject.AddComponent<AssetHistoryGraph>();v.yAxis=new Text[5];for(int i=0;i<5;i++)v.yAxis[i]=Text("Y Axis "+i,chart,"$200",-440,180-i*70,110,38,20);for(int i=0;i<=13;i++)Text("Round "+i,chart,i.ToString(),-370+i*830f/13,-126,54,35,20).alignment=TextAnchor.MiddleCenter;
 v.readout=Text("Round Readout",chart,"시작 자산",0,-174,1000,42,27);v.legend=Text("Legend",chart,"플레이어 선택 시 선 강조",0,-229,1000,65,22);
 v.returnButton=Button("Return Lobby",root.transform,"E 로비로 돌아가기",135,-370,380);v.menuButton=Button("Return Main Menu",root.transform,"메인화면으로",560,-370,320);Text("Navigation",root.transform,"W/S 플레이어 선택 · A/D 탭 전환\n총자산 동점 → 주식 수 비교 → 같으면 공동 순위",-390,-375,720,65,22);
 PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Project/UI/Result/Resources/BBB_DetailedResults.prefab");}finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}}
}
}
