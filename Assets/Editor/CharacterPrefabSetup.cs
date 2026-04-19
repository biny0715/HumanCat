using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// NormalCat / MaleHuman 플레이어 프리팹을 자동 생성하는 Editor 유틸리티.
/// 리팩토링된 PlayerController 기반 구조(InputReader, PlayerMover, PlayerAnimator,
/// CatController/HumanController, PlayerController)를 모두 포함한다.
/// </summary>
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
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        go.AddComponent<InputReader>();
        go.AddComponent<PlayerMover>();
        go.AddComponent<Animator>();
        go.AddComponent<PlayerAnimator>();
        go.AddComponent<CatController>();
        go.AddComponent<HumanController>();

        var pc = go.AddComponent<PlayerController>();
        SetSerializedField(pc, "startingType", (int)PlayerType.Cat);

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
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        go.AddComponent<InputReader>();
        go.AddComponent<PlayerMover>();
        go.AddComponent<Animator>();
        go.AddComponent<PlayerAnimator>();
        go.AddComponent<CatController>();
        go.AddComponent<HumanController>();

        var pc = go.AddComponent<PlayerController>();
        SetSerializedField(pc, "startingType", (int)PlayerType.Human);

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

    static void SetSerializedField(Object target, string fieldName, int value)
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) prop.enumValueIndex = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
    }
}
