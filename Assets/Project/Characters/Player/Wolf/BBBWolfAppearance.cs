using UnityEngine;

namespace BuyBidBlow.Characters
{
    // Independent customization API for lobby UI and network appearance hooks.
    public sealed class BBBWolfAppearance : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer[] uppers;
        [SerializeField] private SkinnedMeshRenderer[] lowers;
        [SerializeField] private SkinnedMeshRenderer tie;
        [SerializeField, Range(0, 2)] private int upper;
        [SerializeField, Range(0, 1)] private int lower;
        [SerializeField] private Color tieColor = new Color(.54f, .16f, .22f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock block;

        public int Upper => upper;
        public int Lower => lower;
        public Color TieColor => tieColor;

        public void SetOutfit(int top, int bottom)
        {
            upper = Mathf.Clamp(top, 0, uppers == null ? 0 : Mathf.Max(0, uppers.Length - 1));
            lower = Mathf.Clamp(bottom, 0, lowers == null ? 0 : Mathf.Max(0, lowers.Length - 1));
            ApplyAppearance();
        }

        public void SetTieColor(Color value)
        {
            tieColor = value;
            ApplyAppearance();
        }

        public void ApplyAppearance()
        {
            ApplySelection(uppers, upper);
            ApplySelection(lowers, lower);
            if (!tie) return;
            if (block == null) block = new MaterialPropertyBlock();
            tie.GetPropertyBlock(block);
            block.SetColor(BaseColorId, tieColor);
            tie.SetPropertyBlock(block);
        }

        private static void ApplySelection(SkinnedMeshRenderer[] renderers, int selection)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i]) renderers[i].enabled = i == selection;
        }

        private void Awake() => ApplyAppearance();
        private void OnValidate() => ApplyAppearance();
    }
}
