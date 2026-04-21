using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// obstacle_sprite_atlas의 모든 슬라이스 스프라이트로 프리팹을 자동 생성.
/// HumanCat → MiniGame → Generate Obstacle Prefabs 메뉴에서 실행.
///
/// 생성 결과:
/// - Assets/Prefabs/MiniGame/Obstacles/Obstacle_Base.prefab (베이스)
/// - Assets/Prefabs/MiniGame/Obstacles/Obstacle_0 ~ Obstacle_17.prefab (각 스프라이트)
/// - ObstacleManager의 obstaclePrefabs 리스트 자동 연결
/// </summary>
public static class ObstaclePrefabGenerator
{
    const string AtlasPath   = "Assets/Art/Obstacle/obstacle_sprite_atlas.png";
    const string PrefabDir   = "Assets/Prefabs/MiniGame/Obstacles";
    const string BaseName    = "Obstacle_Base";

    [MenuItem("HumanCat/MiniGame/Generate Obstacle Prefabs")]
    public static void Generate()
    {
        // ── 폴더 생성 ─────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/MiniGame"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "MiniGame");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs/MiniGame", "Obstacles");

        // ── 스프라이트 로드 ────────────────────────────────────────────────
        var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath);
        var spriteList = new List<Sprite>();
        foreach (var obj in sprites)
            if (obj is Sprite sp) spriteList.Add(sp);

        if (spriteList.Count == 0)
        {
            Debug.LogError($"[ObstaclePrefabGenerator] 스프라이트를 찾을 수 없습니다: {AtlasPath}");
            return;
        }

        // 이름순 정렬 (obstacle_sprite_atlas_0, _1, ...)
        spriteList.Sort((a, b) =>
        {
            int numA = ExtractIndex(a.name);
            int numB = ExtractIndex(b.name);
            return numA.CompareTo(numB);
        });

        // ── 베이스 프리팹 생성 ────────────────────────────────────────────
        string basePath = $"{PrefabDir}/{BaseName}.prefab";
        var    baseGO   = CreateObstacleGO(BaseName, spriteList[0], damage: 10);
        var    basePrefab = PrefabUtility.SaveAsPrefabAsset(baseGO, basePath);
        Object.DestroyImmediate(baseGO);
        Debug.Log($"[ObstaclePrefabGenerator] 베이스 프리팹 생성: {basePath}");

        // ── 스프라이트별 프리팹 생성 ──────────────────────────────────────
        var generatedPrefabs = new List<Obstacle>();

        for (int i = 0; i < spriteList.Count; i++)
        {
            var sprite  = spriteList[i];
            int damage  = CalcDamage(sprite);
            string name = $"Obstacle_{i}";
            string path = $"{PrefabDir}/{name}.prefab";

            var go      = CreateObstacleGO(name, sprite, damage);
            var prefab  = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            var obstacleComp = prefab.GetComponent<Obstacle>();
            if (obstacleComp != null) generatedPrefabs.Add(obstacleComp);

            Debug.Log($"[ObstaclePrefabGenerator] {name} 생성 (damage={damage}, size={sprite.rect.width:0}x{sprite.rect.height:0})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── ObstacleManager 레퍼런스 연결 ────────────────────────────────
        var manager = Object.FindFirstObjectByType<ObstacleManager>();
        if (manager != null)
        {
            var so   = new SerializedObject(manager);
            var prop = so.FindProperty("obstaclePrefabs");
            prop.ClearArray();
            for (int i = 0; i < generatedPrefabs.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = generatedPrefabs[i];
            }
            so.ApplyModifiedProperties();
            Debug.Log($"[ObstaclePrefabGenerator] ObstacleManager에 {generatedPrefabs.Count}개 프리팹 연결 완료");
        }
        else
        {
            Debug.LogWarning("[ObstaclePrefabGenerator] ObstacleManager를 씬에서 찾을 수 없습니다. 수동으로 연결하세요.");
        }

        // 씬 저장
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[ObstaclePrefabGenerator] 완료! 총 {generatedPrefabs.Count}개 프리팹 생성");
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    static GameObject CreateObstacleGO(string name, Sprite sprite, int damage)
    {
        var go = new GameObject(name);

        var sr    = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // 스프라이트 크기에 맞게 CapsuleCollider2D 설정
        var col    = go.AddComponent<CapsuleCollider2D>();
        col.isTrigger = true;
        float w    = sprite.rect.width  / sprite.pixelsPerUnit;
        float h    = sprite.rect.height / sprite.pixelsPerUnit;
        col.size   = new Vector2(w * 0.8f, h * 0.8f);

        var obs    = go.AddComponent<Obstacle>();

        // damage 직렬화 필드 설정
        var so     = new SerializedObject(obs);
        so.FindProperty("damage").intValue = damage;
        so.ApplyModifiedProperties();

        return go;
    }

    // 스프라이트 넓이(픽셀)에 비례한 데미지 계산 (10 ~ 40)
    static int CalcDamage(Sprite sprite)
    {
        float area    = sprite.rect.width * sprite.rect.height;
        float minArea = 87f  * 41f;   // 가장 작은 스프라이트 기준
        float maxArea = 253f * 135f;  // 가장 큰 스프라이트 기준
        float t       = Mathf.InverseLerp(minArea, maxArea, area);
        return Mathf.RoundToInt(Mathf.Lerp(10f, 40f, t));
    }

    static int ExtractIndex(string spriteName)
    {
        int idx = spriteName.LastIndexOf('_');
        if (idx < 0) return 0;
        return int.TryParse(spriteName.Substring(idx + 1), out int n) ? n : 0;
    }
}
