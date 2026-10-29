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

        // 상·하의 선택 인덱스를 범위 내로 설정하고 외형 적용
        public void SetOutfit(int top, int bottom)
        {
            upper = Mathf.Clamp(top, 0, uppers == null ? 0 : Mathf.Max(0, uppers.Length - 1));
            lower = Mathf.Clamp(bottom, 0, lowers == null ? 0 : Mathf.Max(0, lowers.Length - 1));
            ApplyAppearance();
        }

        // 넥타이 색상을 설정하고 외형 적용
        public void SetTieColor(Color value)
        {
            tieColor = value;
            ApplyAppearance();
        }

        // 선택된 상·하의만 표시하고 넥타이 색상을 머티리얼 블록으로 적용
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

        // 선택한 인덱스의 렌더러만 활성화
        private static void ApplySelection(SkinnedMeshRenderer[] renderers, int selection)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i]) renderers[i].enabled = i == selection;
        }

        // 시작 시 외형 적용
        private void Awake() => ApplyAppearance();
        // 인스펙터 값 변경 시 외형 적용
        private void OnValidate() => ApplyAppearance();
    }
}
