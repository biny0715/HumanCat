using UnityEngine;
using UnityEditor;

public static class PlayerMoverSetup
{
    [MenuItem("HumanCat/Setup PlayerMover Settings")]
    static void Setup()
    {
        string[] prefabPaths =
        {
            "Assets/Prefabs/Characters/MaleHuman.prefab",
            "Assets/Prefabs/Characters/NormalCat.prefab",
        };

        var bgBounds = GetBackgroundBounds();

        foreach (var path in prefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[PlayerMoverSetup] 프리팹 없음: {path}");
                continue;
            }

            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var root = scope.prefabContentsRoot;

            // ── Rigidbody2D → Dynamic, 중력 없음 ────────────────────────
            // Dynamic + velocity 이동 = 물리 엔진이 장애물 충돌을 자동 처리.
            // 탑다운 2D이므로 gravityScale = 0.
            var rb = root.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                var rbSO = new SerializedObject(rb);
                rbSO.FindProperty("m_BodyType").intValue     = 0;   // 0=Dynamic
                rbSO.FindProperty("m_GravityScale").floatValue = 0f;
                rbSO.FindProperty("m_Constraints").intValue  = 4;   // FreezeRotation
                rbSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // ── CapsuleCollider2D 추가 (없으면) ──────────────────────────
            // 물리 충돌로 막히려면 Collider가 있어야 한다.
            var col = root.GetComponent<CapsuleCollider2D>();
            if (col == null) col = root.AddComponent<CapsuleCollider2D>();

            col.size      = new Vector2(0.5f, 0.8f);
            col.offset    = new Vector2(0f, 0f);
            col.isTrigger = false;

            // ── YSort 추가 + Sorting Layer 설정 ─────────────────────────
            // 오브젝트 프리팹과 동일한 "Object" 레이어에서 Y축 정렬.
            if (root.GetComponent<YSort>() == null)
                root.AddComponent<YSort>();

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingLayerName = "Object";

            // ── PlayerMover 설정 ─────────────────────────────────────────
            var mover = root.GetComponent<PlayerMover>();
            if (mover == null)
            {
                Debug.LogWarning($"[PlayerMoverSetup] PlayerMover 없음: {path}");
                continue;
            }

            var so = new SerializedObject(mover);

            so.FindProperty("moveSpeed")   .floatValue = 5f;
            so.FindProperty("stopDistance").floatValue = 0.15f;

            if (bgBounds.HasValue)
            {
                so.FindProperty("useBounds") .boolValue    = true;
                so.FindProperty("boundsMin") .vector2Value = bgBounds.Value.min;
                so.FindProperty("boundsMax") .vector2Value = bgBounds.Value.max;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[PlayerMoverSetup] {root.name} 설정 완료 | Dynamic" +
                      (bgBounds.HasValue ? $" | Bounds={bgBounds.Value.size}" : " | Bounds=없음"));
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerMoverSetup] 완료");
    }

    static Bounds? GetBackgroundBounds()
    {
        var bgDay = GameObject.Find("Background_Day");
        if (bgDay == null) return null;
        var sr = bgDay.GetComponent<SpriteRenderer>();
        return (sr != null && sr.sprite != null) ? sr.bounds : (Bounds?)null;
    }
}
