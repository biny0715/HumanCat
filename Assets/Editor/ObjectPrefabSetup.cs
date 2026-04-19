using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// HumanCat > Setup Object Prefabs 메뉴로 씬 오브젝트를 프리팹화하고 YSort를 적용한다.
///
/// [사용 순서]
/// 1. 씬 Hierarchy에서 Furniture 또는 Props 하위에 스프라이트 오브젝트 배치
/// 2. HumanCat > Setup Object Prefabs 실행
/// 3. Assets/Prefabs/Objects 에 프리팹 생성 + 씬 인스턴스가 프리팹으로 교체됨
///
/// [프리팹 구조]
/// ObjectName (YSort, Collider2D 유지)
///  └── Sprite 자식이 없으면 SpriteRenderer를 루트에 유지
///      Sprite 자식이 있으면 그 구조 그대로 보존
/// </summary>
public static class ObjectPrefabSetup
{
    const string PrefabFolder  = "Assets/Prefabs/Objects";
    const string SortingLayerName  = "Object"; // Inspector에서 이 레이어를 추가해야 함

    // 프리팹화할 부모 오브젝트 이름 목록
    static readonly string[] ParentNames = { "Furniture", "Props" };

    [MenuItem("HumanCat/Setup Object Prefabs")]
    static void Setup()
    {
        EnsureFolder(PrefabFolder);

        int created  = 0;
        int skipped  = 0;

        foreach (var parentName in ParentNames)
        {
            var parent = GameObject.Find(parentName);
            if (parent == null)
            {
                Debug.LogWarning($"[ObjectPrefabSetup] '{parentName}' 오브젝트를 찾지 못했습니다.");
                continue;
            }

            // 직계 자식만 대상 (손자 이하는 각 오브젝트 내부 구조로 취급)
            int childCount = parent.transform.childCount;
            if (childCount == 0)
            {
                Debug.LogWarning($"[ObjectPrefabSetup] '{parentName}'에 자식이 없습니다. " +
                                 "오브젝트를 배치한 후 다시 실행하세요.");
                continue;
            }

            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = parent.transform.GetChild(i).gameObject;
                ProcessObject(child, ref created, ref skipped);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[ObjectPrefabSetup] 완료 — 생성: {created}, 건너뜀(이미 프리팹): {skipped}");
    }

    static void ProcessObject(GameObject go, ref int created, ref int skipped)
    {
        // 이미 프리팹 인스턴스면 YSort만 보장하고 종료
        if (PrefabUtility.IsPartOfAnyPrefab(go))
        {
            EnsureYSort(go);
            skipped++;
            return;
        }

        // YSort 추가 (없으면)
        EnsureYSort(go);

        // SpriteRenderer Sorting Layer 설정
        ApplySortingLayerName(go);

        // 프리팹 저장 경로
        string safeName  = go.name.Replace(" ", "_").Replace("/", "_");
        string assetPath = $"{PrefabFolder}/{safeName}.prefab";

        // 같은 이름의 프리팹이 이미 있으면 덮어쓰기
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            go, assetPath, InteractionMode.AutomatedAction);

        if (prefab != null)
        {
            created++;
            Debug.Log($"[ObjectPrefabSetup] 프리팹 생성: {assetPath}");
        }
        else
        {
            Debug.LogError($"[ObjectPrefabSetup] 프리팹 생성 실패: {go.name}");
        }
    }

    // ── YSort 보장 ──────────────────────────────────────────────────────

    static void EnsureYSort(GameObject go)
    {
        if (go.GetComponent<YSort>() == null)
            go.AddComponent<YSort>();
    }

    // ── Sorting Layer 적용 ──────────────────────────────────────────────

    static void ApplySortingLayerName(GameObject go)
    {
        // 루트 + 자식 전체 SpriteRenderer에 적용
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            // "Object" 레이어가 존재할 때만 적용 (없으면 Default 유지)
            bool layerExists = false;
            foreach (var layer in UnityEngine.SortingLayer.layers)
                if (layer.name == SortingLayerName) { layerExists = true; break; }

            if (layerExists)
                sr.sortingLayerName = SortingLayerName;
        }
    }

    // ── 폴더 생성 ───────────────────────────────────────────────────────

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
            Debug.Log($"[ObjectPrefabSetup] 폴더 생성: {path}");
        }
    }

    // ── 스프라이트 시트 → 프리팹 일괄 생성 ─────────────────────────────

    static readonly string[] SpritePaths =
    {
        "Assets/Art/Objects/Objects_0.png",
        "Assets/Art/Objects/Objects_1.png",
    };

    [MenuItem("HumanCat/Create Prefabs from Sprite Sheets")]
    static void CreatePrefabsFromSpriteSheets()
    {
        EnsureFolder(PrefabFolder);

        int created = 0;

        foreach (var spritePath in SpritePaths)
        {
            // 스프라이트 시트에서 모든 Sprite 로드
            var assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            foreach (var asset in assets)
            {
                if (asset is not Sprite sprite) continue;

                string prefabPath = $"{PrefabFolder}/{sprite.name}.prefab";

                // 이미 존재하면 스킵
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    Debug.Log($"[ObjectPrefabSetup] 스킵 (이미 존재): {sprite.name}");
                    continue;
                }

                // 프리팹 구조 생성
                // Root: YSort + (Collider 자리)
                // └── Sprite: SpriteRenderer
                var root   = new GameObject(sprite.name);
                var child  = new GameObject("Sprite");
                child.transform.SetParent(root.transform, false);

                var sr = child.AddComponent<SpriteRenderer>();
                sr.sprite           = sprite;
                sr.sortingLayerName = SortingLayerName;

                root.AddComponent<YSort>();

                // 프리팹 저장
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);

                if (prefab != null)
                {
                    created++;
                    Debug.Log($"[ObjectPrefabSetup] 프리팹 생성: {prefabPath}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ObjectPrefabSetup] 스프라이트 시트 → 프리팹 완료: {created}개 생성");
    }

    // ── Sorting Layer 자동 추가 ─────────────────────────────────────────

    [MenuItem("HumanCat/Add Object Sorting Layer")]
    static void AddSortingLayerName()
    {
        const string path = "ProjectSettings/TagManager.asset";
        string text = System.IO.File.ReadAllText(path);

        // 파일에서 직접 확인
        if (text.Contains($"name: {SortingLayerName}"))
        {
            Debug.Log("[ObjectPrefabSetup] 'Object' Sorting Layer가 이미 존재합니다.");
            return;
        }

        // Default 항목 바로 뒤에 삽입
        const string afterDefault = "  - name: Default\n    uniqueID: 0\n    locked: 0";
        string insert = afterDefault +
                        $"\n  - name: {SortingLayerName}\n    uniqueID: 1148430510\n    locked: 0";

        text = text.Replace(afterDefault, insert);
        System.IO.File.WriteAllText(path, text);
        AssetDatabase.Refresh();
        Debug.Log("[ObjectPrefabSetup] 'Object' Sorting Layer 추가 완료");
    }
}
