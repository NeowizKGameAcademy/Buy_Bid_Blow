using System;
using System.Linq;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Round;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class NetworkHUDSetup
    {
        const string Folder = "Assets/Project/UI/HUD";
        public const string PrefabPath = Folder + "/PF_NetworkPlayerHUD.prefab";
        [MenuItem("Tools/Buy Bid Blow/UI/Install Network Player HUD")]
        public static void Apply()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Dev/Sandbox/Interface/PlayerUI/Prefabs/PF_PlayerHUD.prefab");
            var root = new GameObject("Network Player HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            try
            {
                root.layer = 5;
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
                foreach (var name in new[] { "Assets - Top Left", "Round Timer - Top Right" })
                {
                    var child = source.transform.Find(name);
                    if (!child) throw new Exception("Missing Dev HUD area: " + name);
                    var clone = Object.Instantiate(child.gameObject, root.transform, false); clone.name = name; clone.SetActive(true);
                    if(name == "Round Timer - Top Right") ((RectTransform)clone.transform).anchoredPosition = new Vector2(-110,-24);
                    foreach (var graphic in clone.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                }
                var view = root.AddComponent<NetworkPlayerHUD>();
                var assets = root.transform.Find("Assets - Top Left"); var timer = root.transform.Find("Round Timer - Top Right");
                view.cashLabel = Text(assets,"Cash"); view.reservedLabel = Text(assets,"Reserved Cash"); view.totalLabel = Text(assets,"Total Value");
                view.holdings = Enumerable.Range(0,3).Select(i=>Text(assets,"Holding Quantity " + i)).ToArray();
                view.roundLabel = Text(timer,"Round"); view.timeLabel = Text(timer,"Time"); view.phaseLabel = Text(timer,"Phase");
                var banner = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/UI/Round/PF_RoundStart.prefab");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(banner, root.transform);
                view.roundStart = instance.GetComponent<RoundStartView>(); view.roundStart.Hide();
                view.cashLabel.text = view.totalLabel.text = "—"; view.reservedLabel.text = "자산 수신 대기";
                foreach(var text in view.holdings) text.text = "—";
                view.roundLabel.text = "경기 연결 대기"; view.timeLabel.text = "--:--"; view.phaseLabel.text = "대기 중";
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/SC_ExchangeFurnished.unity");
            var session = Object.FindFirstObjectByType<HallSession>();
            if (!session) throw new Exception("HallSession is required");
            foreach(var previous in session.gameplayPresentation.GetComponentsInChildren<NetworkPlayerHUD>(true)) Object.DestroyImmediate(previous.gameObject);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), session.gameplayPresentation.transform);
            hud.transform.localScale = Vector3.one;
            // Only replace the old money readout. Keep its crosshair and interaction prompts.
            var oldMoney = session.player.transform.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).FirstOrDefault(t=>t.name == "Player Money");
            if (oldMoney) oldMoney.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            NetworkHUDVerification.Run();
            System.IO.File.WriteAllText("Library/NetworkHUDSetup.result", "PASS: Dev assets/time HUD extracted; production scene connected; RoundStartView wired.");
        }
        static Text Text(Transform parent,string name)
        {
            var child = parent.Find(name);
            if (!child || !child.TryGetComponent<Text>(out var text)) throw new Exception("Missing HUD text: " + name);
            return text;
        }
    }
}
