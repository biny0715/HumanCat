using UnityEditor;
using UnityEngine;

/// <summary>
/// CatNPC.prefab 자동 생성기.
///
/// 메뉴: HumanCat → Cat NPC → Build CatNPC Prefab
///
/// 결과 prefab 구성:
///   - SpriteRenderer (default: CheeseCat_Idle_0)
///   - Animator (controller: CheeseCatController)
///   - Rigidbody2D (Kinematic, gravityScale=0)
///   - CircleCollider2D (isTrigger=false)
///   - CatNPCController
///       · floorLayer = "Floor"
///       · ref 슬롯(spriteRenderer/animator/rb) 자동 연결
///
/// 사용자는 prefab 인스턴스를 [ Environment ]/Indoor 아래에 배치하면 됨.
/// SleepCat 외형을 원하면 Animator.controller 와 SpriteRenderer.sprite 만 인스펙터에서 교체.
/// </summary>
public static class CatNPCSetupBuilder
{
    const string PrefabFolder = "Assets/Prefabs/CatNPC";
    const string PrefabPath   = "Assets/Prefabs/CatNPC/CatNPC.prefab";

    const string DefaultControllerPath = "Assets/Animations/CheeseCatController.controller";
    const string DefaultSpriteSheetPath = "Assets/Art/Cat/CheeseCat/CheeseCat_Idle.png";
    const string DefaultSpriteName      = "CheeseCat_Idle_0";

    [MenuItem("HumanCat/Cat NPC/Build CatNPC Prefab")]
    public static void BuildCatNPCPrefab()
    {
        // 1) 폴더 보장
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "CatNPC");

        // 2) 임시 GameObject 구성
        var go = new GameObject("CatNPC");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";

        var anim = go.AddComponent<Animator>();
        var rb   = go.AddComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.gravityScale   = 0f;
        rb.constraints    = RigidbodyConstraints2D.FreezeRotation;

        var col           = go.AddComponent<CircleCollider2D>();
        col.radius        = 0.3f;
        col.isTrigger     = false;

        var ctrl = go.AddComponent<CatNPCController>();

        // 3) 기본 컨트롤러 / sprite 연결
        var defaultController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultControllerPath);
        if (defaultController != null) anim.runtimeAnimatorController = defaultController;
        else Debug.LogWarning($"[CatNPCSetupBuilder] {DefaultControllerPath} 없음 — 인스펙터에서 수동 연결 필요");

        var defaultSprite = LoadSpriteFromSheet(DefaultSpriteSheetPath, DefaultSpriteName);
        if (defaultSprite != null) sr.sprite = defaultSprite;
        else Debug.LogWarning($"[CatNPCSetupBuilder] sprite '{DefaultSpriteName}' 없음 — 인스펙터에서 수동 연결 필요");

        // 4) CatNPCController 슬롯 자동 연결
        var so = new SerializedObject(ctrl);
        int floorLayer = LayerMask.NameToLayer("Floor");
        if (floorLayer >= 0)
            so.FindProperty("floorLayer").intValue = 1 << floorLayer;
        else
            Debug.LogWarning("[CatNPCSetupBuilder] Layer 'Floor' 없음 — Placement Layers 메뉴 실행 권장");

        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("animator").objectReferenceValue       = anim;
        so.FindProperty("rb").objectReferenceValue             = rb;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 5) Prefab 저장 + 임시 인스턴스 파괴
        var saved = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        if (saved != null)
        {
            EditorGUIUtility.PingObject(saved);
            Selection.activeObject = saved;
            Debug.Log($"[CatNPCSetupBuilder] CatNPC.prefab 생성 완료 — {PrefabPath}");
        }
        else
        {
            Debug.LogError("[CatNPCSetupBuilder] Prefab 저장 실패");
        }
    }

    static Sprite LoadSpriteFromSheet(string sheetPath, string spriteName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        foreach (var a in assets)
        {
            if (a is Sprite s && s.name == spriteName) return s;
        }
        // fallback: 첫 sprite
        foreach (var a in assets)
            if (a is Sprite s) return s;
        return null;
    }
}
