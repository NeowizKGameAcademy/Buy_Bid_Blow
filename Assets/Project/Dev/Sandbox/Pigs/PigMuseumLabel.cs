using UnityEngine;

namespace BuyBidBlow.Dev
{
    // Built-in TextMesh font shaders draw through geometry; use depth-tested gallery lettering.
    [RequireComponent(typeof(TextMesh))]
    public sealed class PigMuseumLabel : MonoBehaviour
    {
        public Shader textShader;
        private Material runtimeMaterial;
        private Material originalMaterial;
        private TextMesh mesh;
        private Renderer textRenderer;

        private void OnEnable()
        {
            mesh = GetComponent<TextMesh>();
            textRenderer = GetComponent<Renderer>();
            if (!textShader || !mesh.font) return;
            originalMaterial = textRenderer.sharedMaterial;
            runtimeMaterial = new Material(textShader);
            textRenderer.sharedMaterial = runtimeMaterial;
            Font.textureRebuilt += RefreshFont;
            RefreshFont(mesh.font);
        }

        private void RefreshFont(Font font)
        {
            if (font == mesh.font && runtimeMaterial)
                runtimeMaterial.mainTexture = font.material.mainTexture;
        }

        private void OnDisable()
        {
            Font.textureRebuilt -= RefreshFont;
            if (textRenderer) textRenderer.sharedMaterial = originalMaterial;
            if (runtimeMaterial) Destroy(runtimeMaterial);
        }
    }
}
