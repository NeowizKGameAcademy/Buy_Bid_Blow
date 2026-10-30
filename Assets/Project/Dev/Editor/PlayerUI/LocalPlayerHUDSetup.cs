using BuyBidBlow.Presentation;
using System;
using System.Linq;
using BuyBidBlow.Audio;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using BuyBidBlow.UI.Notebook;
using CueAudio;
using UnityEditor;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // The match HUD (Network Player HUD: assets, timer, notebook, notices, results) lives in the local Player,
    // next to its trading panel; the paw cursor and the jump/emote sounds belong to the same player.
    public static class LocalPlayerHUDSetup
    {
        const string PlayerPath = "Assets/Project/Characters/Player/Player.prefab";
        const string AvatarPath = "Assets/Project/Characters/Player/PlayerNet.prefab";
        const string PointerPath = "Assets/Project/UI/HUD/Cursor/WolfPaw_Source.png";
        const string CatalogGuid = "7417b44d9862ec845b72a6794ae585e0";

        [MenuItem("Tools/Buy Bid Blow/UI/Attach HUD To Local Player")]
        public static void Apply()
        {
            var player = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var hud = player.GetComponentsInChildren<NetworkPlayerHUD>(true).SingleOrDefault();
                if (!hud)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NetworkHUDSetup.PrefabPath), player.transform);
                    instance.name = "Network Player HUD"; hud = instance.GetComponent<NetworkPlayerHUD>();
                }
                var trading = player.GetComponentInChildren<TradePanelController>(true);
                hud.GetComponent<NetworkNotebookController>().trading = trading;
                hud.GetComponent<NetworkMatchFeedback>().trading = trading;
                // The HUD's asset panel replaces the old money readout.
                foreach (var money in player.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Where(t => t.name == "Player Money"))
                    money.gameObject.SetActive(false);

                var cursor = new SerializedObject(player.GetComponentInChildren<CursorController>(true));
                cursor.FindProperty("radialMenu").objectReferenceValue = player.GetComponentInChildren<RadialMenuController>(true);
                cursor.FindProperty("pointer").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(PointerPath)
                    ?? throw new InvalidOperationException("Missing " + PointerPath);
                cursor.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }

            // PlayerNet's jump/emote RPCs play these at the avatar, for the owner and everyone watching.
            var avatar = PrefabUtility.LoadPrefabContents(AvatarPath);
            try
            {
                var audio = avatar.GetComponent<PlayerActionAudio>() ?? avatar.AddComponent<PlayerActionAudio>();
                var catalog = AssetDatabase.LoadAssetAtPath<CueCatalog>(AssetDatabase.GUIDToAssetPath(CatalogGuid))
                    ?? throw new InvalidOperationException("Missing game cue catalog " + CatalogGuid);
                var so = new SerializedObject(audio);
                foreach (var (field, id) in new[] { ("footstep", "player.footstep"), ("jump", "player.jump") })
                {
                    so.FindProperty(field + ".catalog").objectReferenceValue = catalog;
                    so.FindProperty(field + ".id").stringValue = id;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(avatar, AvatarPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(avatar); }
            AssetDatabase.SaveAssets();
            Debug.Log("Network Player HUD, cursor and action audio attached to the local Player and its avatar.");
        }
    }
}
