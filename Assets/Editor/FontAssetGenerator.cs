using UnityEngine;
using UnityEditor;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class FontAssetGenerator
{
    const string FontFolder        = "Assets/Art/Fonts";
    const int    AtlasWidth        = 2048;
    const int    AtlasHeight       = 2048;
    const int    SamplingPointSize = 90;
    const int    Padding           = 9;

    [MenuItem("HumanCat/Generate TMP Font Assets")]
    static void Generate()
    {
        var guids = AssetDatabase.FindAssets("t:Font", new[] { FontFolder });

        if (guids.Length == 0)
        {
            Debug.LogWarning("[FontAssetGenerator] 폰트 파일을 찾지 못했습니다: " + FontFolder);
            return;
        }

        int created = 0;

        foreach (var guid in guids)
        {
            var fontPath = AssetDatabase.GUIDToAssetPath(guid);
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) continue;

            string assetPath = System.IO.Path.ChangeExtension(fontPath, null) + " SDF.asset";

            // 기존 asset 덮어쓰기
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                SamplingPointSize,
                Padding,
                GlyphRenderMode.SDFAA,
                AtlasWidth,
                AtlasHeight,
                AtlasPopulationMode.Dynamic);

            if (fontAsset == null)
            {
                Debug.LogError($"[FontAssetGenerator] 생성 실패: {font.name}");
                continue;
            }

            fontAsset.name = font.name + " SDF";

            AssetDatabase.CreateAsset(fontAsset, assetPath);

            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            // 머티리얼도 서브에셋으로 저장
            if (fontAsset.material != null)
            {
                fontAsset.material.name = font.name + " Atlas Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            created++;
            Debug.Log($"[FontAssetGenerator] 생성 완료: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FontAssetGenerator] 완료 — {created}개 SDF Asset 생성");
    }
}
