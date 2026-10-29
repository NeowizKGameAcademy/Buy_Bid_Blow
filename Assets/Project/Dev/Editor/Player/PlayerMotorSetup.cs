using System;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // The local player body: kinematic CharacterMotor, PlayerLook, PlayerInputReader (Input/PlayerControls) and
    // the same wolf (PF_Wolf_Character + BBBWolfCharacter) remote avatars use, shown through PlayerAvatar.
    // Safe to run again.
    public static class PlayerMotorSetup
    {
        const string PrefabPath = "Assets/Project/Characters/Player/Player.prefab";
        const string WolfPath = "Assets/Project/Characters/Player/Wolf/WolfExpressions/PF_Wolf_Character.prefab";
        const string ControlsPath = "Assets/Project/Characters/Player/Input/PlayerControls.inputactions";
        const string ModelName = "Wolf";

        [MenuItem("Tools/Buy Bid Blow/Player/Setup Character Motor")]
        public static void Setup()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Player character motor set up: " + PrefabPath);
        }

        static void Configure(GameObject root)
        {
            ReplaceModel(root);
            var groundCheck = root.transform.Find("GroundCheck");
            if (groundCheck) Object.DestroyImmediate(groundCheck.gameObject);

            var body = root.GetComponent<Rigidbody>();
            body.isKinematic = true; body.interpolation = RigidbodyInterpolation.None;

            var motor = Ensure<CharacterMotor>(root);
            var look = Ensure<PlayerLook>(root);
            var input = Ensure<PlayerInputReader>(root);
            var avatar = Ensure<PlayerAvatar>(root);
            var seating = root.GetComponent<PlayerSeating>();
            var radial = root.GetComponentInChildren<RadialMenuController>(true);
            var trade = root.GetComponentInChildren<TradePanelController>(true);
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath) ?? throw new InvalidOperationException("Missing " + ControlsPath);

            Set(motor, "capsule", root.GetComponent<CapsuleCollider>());
            Set(look, "playerCamera", root.GetComponent<PlayerCamera>());
            Set(look, "seating", seating);
            Set(input, "controls", controls);
            Set(input, "motor", motor);
            Set(input, "look", look);
            Set(input, "interaction", root.GetComponent<PlayerInteraction>());
            Set(input, "tradePanel", trade);
            Set(input, "radial", radial);
            Set(avatar, "motor", motor);
            Set(avatar, "character", root.GetComponentInChildren<BBBWolfCharacter>(true));
            Set(avatar, "seating", seating);
            Set(avatar, "radial", radial);
        }

        // Swaps the old local-only model (Wolf_Model + Player Animation.controller) for PF_Wolf_Character,
        // keeping the first-person hidden layer on the same parts.
        static void ReplaceModel(GameObject root)
        {
            if (root.transform.Find(ModelName)) return;
            var old = root.transform.Cast<Transform>().FirstOrDefault(t => t.name == "Wolf_Model")
                ?? throw new InvalidOperationException("Player.prefab has neither Wolf nor Wolf_Model.");
            var hiddenLayers = old.GetComponentsInChildren<Renderer>(true).ToDictionary(r => r.name, r => r.gameObject.layer);
            var wolf = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WolfPath), root.transform);
            wolf.name = ModelName;
            wolf.transform.SetSiblingIndex(old.GetSiblingIndex());
            // PF_Wolf_Character faces -Z at real size, like the network avatar (PlayerNet.ModelYaw).
            wolf.transform.SetLocalPositionAndRotation(Vector3.zero, PlayerNet.ModelYaw);
            wolf.transform.localScale = Vector3.one;
            foreach (var r in wolf.GetComponentsInChildren<Renderer>(true))
                if (hiddenLayers.TryGetValue(r.name, out var layer)) r.gameObject.layer = layer;
            Object.DestroyImmediate(old.gameObject);
        }

        static T Ensure<T>(GameObject root) where T : Component => root.TryGetComponent(out T existing) ? existing : root.AddComponent<T>();

        static void Set(Object target, string field, Object value)
        {
            if (!value) throw new InvalidOperationException($"{target.GetType().Name}.{field}: nothing to assign");
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetupBatch()
        {
            try { Setup(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
