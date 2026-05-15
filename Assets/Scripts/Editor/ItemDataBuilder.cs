using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Prefabs/Objects 의 모든 프리팹을 기반으로 Assets/Resources/Items 에 ItemData 자산을 일괄 생성.
///
/// [규칙]
///   - 자산 경로  : Assets/Resources/Items/Item_&lt;PrefabName&gt;.asset
///   - itemId     : 프리팹 이름 그대로
///   - maxStack   : 10
///   - stackable  : true
///   - placementPrefab : 해당 프리팹
///   - icon       : 프리팹 첫 번째 자식(이름 "Sprite")의 SpriteRenderer.sprite
///   - 그 외 (displayName / description / 가격 / allowedSurfaces) : 디자이너가 직접 채움
///
/// 이미 존재하는 자산은 건너뛴다 (사용자 작업 보존).
///
/// 메뉴: HumanCat → Item → Generate ItemData from Prefabs/Objects
/// </summary>
public static class ItemDataBuilder
{
    const string PrefabsFolder = "Assets/Prefabs/Objects";
    const string ItemsFolder   = "Assets/Resources/Items";

    [MenuItem("HumanCat/Item/Generate ItemData from Prefabs-Objects")]
    public static void GenerateFromPrefabs()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(ItemsFolder);

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsFolder });
        int created = 0, skipped = 0, missingSprite = 0;

        foreach (var guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            string itemAssetPath = $"{ItemsFolder}/Item_{prefab.name}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(itemAssetPath) != null)
            {
                skipped++;
                continue;
            }

            Sprite icon = FindSpriteOnFirstChild(prefab);
            if (icon == null)
            {
                missingSprite++;
                Debug.LogWarning($"[ItemDataBuilder] Sprite 미발견: {prefab.name} (첫 자식이 'Sprite'/SpriteRenderer 아님)");
            }

            var data = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(data, itemAssetPath);

            var so = new SerializedObject(data);
            so.FindProperty("itemId").stringValue                   = prefab.name;
            so.FindProperty("icon").objectReferenceValue            = icon;
            so.FindProperty("stackable").boolValue                  = true;
            so.FindProperty("maxStack").intValue                    = 10;
            so.FindProperty("placementPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ItemDataBuilder] 완료 — 생성:{created} / 스킵(이미 있음):{skipped} / Sprite 미발견:{missingSprite}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int last = path.LastIndexOf('/');
        if (last < 0) return;
        string parent = path.Substring(0, last);
        string leaf   = path.Substring(last + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static Sprite FindSpriteOnFirstChild(GameObject prefab)
    {
        if (prefab == null || prefab.transform.childCount == 0) return null;
        var first = prefab.transform.GetChild(0);
        if (first.name != "Sprite") return null;
        var sr = first.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }
}
