using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Builds dynamic TMP font assets for the bundled Pretendard static font files.
/// Dynamic, multi-atlas assets keep the project size small while supporting Korean
/// (and any other glyphs available in Pretendard) as text is encountered at runtime.
/// </summary>
public static class PretendardTmpFontAssetBuilder
{
    private const string SourceFolder = "Assets/Font/public/static";
    private const string OutputFolder = "Assets/Font/TMP";

    private static readonly (string name, int weight)[] Faces =
    {
        ("Thin", 100),
        ("ExtraLight", 200),
        ("Light", 300),
        ("Regular", 400),
        ("Medium", 500),
        ("SemiBold", 600),
        ("Bold", 700),
        ("ExtraBold", 800),
        ("Black", 900),
    };

    [MenuItem("Tools/Fonts/Build Pretendard TMP Assets")]
    public static void Build()
    {
        EnsureFolder(OutputFolder);

        var assets = new Dictionary<int, TMP_FontAsset>();
        foreach (var face in Faces)
        {
            string sourcePath = $"{SourceFolder}/Pretendard-{face.name}.otf";
            Font source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null)
            {
                Debug.LogError($"Pretendard source font was not found: {sourcePath}");
                continue;
            }

            string outputPath = $"{OutputFolder}/Pretendard {face.weight} SDF.asset";
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
            if (asset == null)
            {
                asset = TMP_FontAsset.CreateFontAsset(
                    source,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048,
                    AtlasPopulationMode.Dynamic,
                    true);

                if (asset == null)
                {
                    Debug.LogError($"Could not create TMP font asset for {sourcePath}.");
                    continue;
                }

                asset.name = $"Pretendard {face.weight} SDF";
                AssetDatabase.CreateAsset(asset, outputPath);

                foreach (Texture2D atlas in asset.atlasTextures)
                    AssetDatabase.AddObjectToAsset(atlas, asset);

                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            assets[face.weight] = asset;
        }

        if (assets.TryGetValue(400, out TMP_FontAsset regular))
        {
            foreach (var face in Faces)
            {
                if (!assets.TryGetValue(face.weight, out TMP_FontAsset typeface))
                    continue;

                TMP_FontWeightPair pair = regular.fontWeightTable[face.weight / 100];
                pair.regularTypeface = typeface;
                regular.fontWeightTable[face.weight / 100] = pair;
            }

            EditorUtility.SetDirty(regular);
        }

        foreach (TMP_FontAsset asset in assets.Values)
            EditorUtility.SetDirty(asset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created Pretendard TMP dynamic font assets. Use Pretendard 400 SDF as the family root.");
    }

    // The project is often open while this helper is added. Build once after Unity
    // recompiles this script so no second Editor instance is needed.
    [InitializeOnLoadMethod]
    private static void BuildOnceAfterImport()
    {
        const string sessionKey = "PretendardTmpFontAssetBuilder.Built";
        if (SessionState.GetBool(sessionKey, false))
            return;

        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(sessionKey, false))
                return;

            SessionState.SetBool(sessionKey, true);
            Build();
        };
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
