using System.Linq;
using UnityEngine;
using UnityEditor;

public static class CharacterPrefabSetup
{
    const string PrefabDir = "Assets/Prefabs/Characters";

    [MenuItem("HumanCat/Create Character Prefabs")]
    static void Create()
    {
        System.IO.Directory.CreateDirectory(PrefabDir);

        CreateNormalCatPrefab();
        CreateMaleHumanPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterPrefabSetup] 프리팹 생성 완료");
    }

    // ── NormalCat ────────────────────────────────────────────────────
    static void CreateNormalCatPrefab()
    {
        var go = new GameObject("NormalCat");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadFirstSprite("Assets/Art/Cat/NormalCat/normalCat_Idle.png", "normalCat_Idle");
        sr.sortingOrder = 1;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        go.AddComponent<PlayerMover>();

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = LoadController("Assets/Animations/PlayerController.controller");

        var pa = go.AddComponent<PlayerAnimator>();
        pa.playerType = PlayerType.Cat;

        go.AddComponent<NormalCat>();

        SavePrefab(go, $"{PrefabDir}/NormalCat.prefab");
        Object.DestroyImmediate(go);
        Debug.Log("[CharacterPrefabSetup] NormalCat 프리팹 생성 완료");
    }

    // ── MaleHuman ────────────────────────────────────────────────────
    static void CreateMaleHumanPrefab()
    {
        var go = new GameObject("MaleHuman");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadFirstSprite("Assets/Art/Human/Male/Male_Idle.png", "Male_Idle");
        sr.sortingOrder = 1;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        go.AddComponent<PlayerMover>();

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = LoadController("Assets/Animations/Human/HumanController.controller");

        var pa = go.AddComponent<PlayerAnimator>();
        pa.playerType = PlayerType.Human;

        go.AddComponent<MaleHuman>();

        SavePrefab(go, $"{PrefabDir}/MaleHuman.prefab");
        Object.DestroyImmediate(go);
        Debug.Log("[CharacterPrefabSetup] MaleHuman 프리팹 생성 완료");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────
    static Sprite LoadFirstSprite(string path, string prefix)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith(prefix))
            .OrderBy(s => s.name)
            .FirstOrDefault();
    }

    static RuntimeAnimatorController LoadController(string path)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        if (ctrl == null) Debug.LogError($"[CharacterPrefabSetup] Controller 없음: {path}");
        return ctrl;
    }

    static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
    }
}
