using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BuyBidBlow.UI.HUD
{
    public class RadialMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private Image[] menu = new Image[4];
        [Header("Selection")]
        [Tooltip("Cursor positions inside this radius never select a menu item.")]
        [FormerlySerializedAs("innerRadius")]
        [SerializeField, Min(0f)] private float selectionDeadZoneRadius = 160f;
        [Header("Radius animation")]
        [SerializeField, Min(0f)] private float revealStartDelay;
        [SerializeField, Min(0f)] private float revealStagger = 0.08f;
        [SerializeField, Min(0.01f)] private float revealDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float hoverDuration = 0.06f;
        [SerializeField, Min(1f)] private float hoverRadiusMultiplier = 1.1f;
        [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private RectTransform menuRectTransform;
        private Canvas rootCanvas;
        private int selectedIndex = -1;
        private int hoveredIndex = -1;
        private Material[] itemMaterials;
        private float[] defaultOuterRadii;
        private float[] currentOuterRadii;
        private float[] hoverAmounts;
        private float openedAt;

        private static readonly int OuterRadiusId = Shader.PropertyToID("_OuterRadius");
        private static readonly int InnerRadiusId = Shader.PropertyToID("_InnerRadius");

        public bool IsOpen { get; private set; }
        public int SelectedIndex => selectedIndex;
        /// <summary>Raised when Shift is released over a menu item. Value is 1 through 4.</summary>
        public event Action<int> SelectionConfirmed;

        private void Awake()
        {
            ResolveMenuReferences();
            CreateIndependentMaterials();
            CacheRadiusValues();
            SetMenuVisible(false);
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (Mouse.current != null)
                selectedIndex = GetSelectedIndex(Mouse.current.position.ReadValue());

            SetHoveredIndex(selectedIndex);
            AnimateRadius();
        }

        public void BeginSelection()
        {
            if (IsOpen)
                return;

            ResolveMenuReferences();
            if (menuRoot == null || menuRectTransform == null)
            {
                Debug.LogWarning("Radial Menu: Emotion Menu/Image references were not found.", this);
                return;
            }

            IsOpen = true;
            selectedIndex = -1;
            SetHoveredIndex(-1);
            openedAt = Time.unscaledTime;
            ResetRadiusToInnerEdge();
            SetMenuVisible(true);
        }

        public void EndSelection(bool confirm)
        {
            if (!IsOpen)
                return;

            var confirmedIndex = selectedIndex;
            IsOpen = false;
            selectedIndex = -1;
            SetHoveredIndex(-1);
            RestoreDefaultRadii();
            SetMenuVisible(false);

            if (confirm && confirmedIndex >= 0)
                SelectionConfirmed?.Invoke(confirmedIndex + 1);
        }

        private void ResolveMenuReferences()
        {
            if (menuRoot == null)
            {
                var emotionMenu = transform.Find("Emotion Menu");
                if (emotionMenu != null)
                    menuRoot = emotionMenu.gameObject;
            }

            if (menuRoot == null)
                return;

            menuRectTransform = menuRoot.transform as RectTransform;
            rootCanvas = menuRoot.GetComponentInParent<Canvas>();

            if (menu == null || menu.Length == 0 || AllEntriesAreEmpty())
                menu = menuRoot.GetComponentsInChildren<Image>(true);

            // These are visual slices, not rectangular button hit targets.
            foreach (var image in menu)
            {
                if (image != null)
                    image.raycastTarget = false;
            }
        }

        private void CreateIndependentMaterials()
        {
            if (menu == null || itemMaterials != null)
                return;

            itemMaterials = new Material[menu.Length];
            for (var i = 0; i < menu.Length; i++)
            {
                if (menu[i] == null || menu[i].material == null)
                    continue;

                itemMaterials[i] = new Material(menu[i].material)
                {
                    name = $"{menu[i].material.name} (Radial Menu {i + 1})"
                };
                menu[i].material = itemMaterials[i];
            }
        }

        private void CacheRadiusValues()
        {
            if (itemMaterials == null || defaultOuterRadii != null)
                return;

            defaultOuterRadii = new float[itemMaterials.Length];
            currentOuterRadii = new float[itemMaterials.Length];
            hoverAmounts = new float[itemMaterials.Length];
            for (var i = 0; i < itemMaterials.Length; i++)
            {
                var material = itemMaterials[i];
                if (material == null || !material.HasProperty(OuterRadiusId))
                    continue;

                defaultOuterRadii[i] = material.GetFloat(OuterRadiusId);
                currentOuterRadii[i] = defaultOuterRadii[i];
            }
        }

        private void SetHoveredIndex(int index)
        {
            if (hoveredIndex == index)
                return;

            hoveredIndex = index;
        }

        private void AnimateRadius()
        {
            if (itemMaterials == null || defaultOuterRadii == null || currentOuterRadii == null)
                return;

            var elapsed = Time.unscaledTime - openedAt;
            for (var i = 0; i < itemMaterials.Length; i++)
            {
                var material = itemMaterials[i];
                if (material == null || !material.HasProperty(OuterRadiusId))
                    continue;

                var itemElapsed = elapsed - revealStartDelay - i * revealStagger;
                var revealProgress = Mathf.Clamp01(itemElapsed / revealDuration);
                var revealAmount = revealCurve != null
                    ? revealCurve.Evaluate(revealProgress)
                    : revealProgress;
                var baseRadius = Mathf.Lerp(GetInnerRadius(material), defaultOuterRadii[i], revealAmount);

                var hoverTarget = i == hoveredIndex ? 1f : 0f;
                hoverAmounts[i] = Mathf.MoveTowards(
                    hoverAmounts[i], hoverTarget, Time.unscaledDeltaTime / hoverDuration);
                currentOuterRadii[i] = Mathf.Lerp(
                    baseRadius, baseRadius * hoverRadiusMultiplier, hoverAmounts[i]);
                material.SetFloat(OuterRadiusId, currentOuterRadii[i]);
            }
        }

        private void ResetRadiusToInnerEdge()
        {
            if (itemMaterials == null || currentOuterRadii == null)
                return;

            for (var i = 0; i < itemMaterials.Length; i++)
            {
                var material = itemMaterials[i];
                if (material == null || !material.HasProperty(OuterRadiusId))
                    continue;

                currentOuterRadii[i] = GetInnerRadius(material);
                if (hoverAmounts != null)
                    hoverAmounts[i] = 0f;
                material.SetFloat(OuterRadiusId, currentOuterRadii[i]);
            }
        }

        private void RestoreDefaultRadii()
        {
            if (itemMaterials == null || defaultOuterRadii == null)
                return;

            for (var i = 0; i < itemMaterials.Length; i++)
            {
                var material = itemMaterials[i];
                if (material != null && material.HasProperty(OuterRadiusId))
                    material.SetFloat(OuterRadiusId, defaultOuterRadii[i]);
            }
        }

        private static float GetInnerRadius(Material material)
        {
            return material.HasProperty(InnerRadiusId) ? material.GetFloat(InnerRadiusId) : 0f;
        }

        /// <summary>Returns this menu item's independent runtime material. Number is 1-based.</summary>
        public Material GetItemMaterial(int menuNumber)
        {
            var index = menuNumber - 1;
            return itemMaterials != null && index >= 0 && index < itemMaterials.Length
                ? itemMaterials[index]
                : null;
        }

        private void OnDestroy()
        {
            if (itemMaterials == null)
                return;

            foreach (var material in itemMaterials)
            {
                if (material != null)
                    Destroy(material);
            }
        }

        private bool AllEntriesAreEmpty()
        {
            foreach (var image in menu)
            {
                if (image != null)
                    return false;
            }

            return true;
        }

        private void SetMenuVisible(bool visible)
        {
            if (menuRoot != null)
                menuRoot.SetActive(visible);
        }

        private int GetSelectedIndex(Vector2 screenPosition)
        {
            if (menuRectTransform == null || menu == null || menu.Length == 0)
                return -1;

            var cameraForCanvas = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(menuRectTransform, screenPosition, cameraForCanvas, out var localPoint))
                return -1;

            var distance = localPoint.magnitude;
            // There is no outer limit: a slice remains selectable all the way
            // to the edge of the screen. Only the central empty circle is dead.
            if (distance < selectionDeadZoneRadius)
                return -1;

            var angle = Mathf.Repeat(Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg, 360f);

            // The displayed shape has four 60-degree pieces, with empty gaps
            // above (60..120) and below (240..300). Numbering requested,
            // clockwise from left-bottom: left-bottom -> left-top ->
            // right-top -> right-bottom.
            var index = angle switch
            {
                >= 180f and < 240f => 0, // 1: left-bottom
                >= 120f and < 180f => 1, // 2: left-top
                >= 0f and < 60f => 2,    // 3: right-top
                >= 300f and < 360f => 3, // 4: right-bottom
                _ => -1                  // visual gap: no selection
            };

            return index >= 0 && index < menu.Length && menu[index] != null ? index : -1;
        }
    }
}
