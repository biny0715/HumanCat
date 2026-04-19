using UnityEngine;
using UnityEditor;

public static class FixPrefabControllers
{
    [MenuItem("HumanCat/Fix Prefab Controllers")]
    static void Fix()
    {
        FixPrefab(
            "Assets/Prefabs/Characters/NormalCat.prefab",
            "Assets/Animations/PlayerController.controller",
            "Assets/Animations/PlayerController.controller",
            PlayerType.Cat
        );

        FixPrefab(
            "Assets/Prefabs/Characters/MaleHuman.prefab",
            "Assets/Animations/Human/HumanController.controller",
            "Assets/Animations/PlayerController.controller",
            PlayerType.Human
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FixPrefabControllers] 완료");
    }

    static void FixPrefab(string prefabPath, string humanCtrlPath, string catCtrlPath, PlayerType startType)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning($"프리팹 없음: {prefabPath}"); return; }

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var root = scope.prefabContentsRoot;

        var humanCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(humanCtrlPath);
        var catCtrl   = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(catCtrlPath);

        // Animator에 기본 컨트롤러 할당
        var anim = root.GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController == null)
            anim.runtimeAnimatorController = startType == PlayerType.Human ? humanCtrl : catCtrl;

        // CatController — 고양이 스프라이트는 오른쪽이 기본 방향
        var cat = EnsureComponent<CatController>(root);
        var catSO = new SerializedObject(cat);
        SetObjRef(catSO, "animatorController", catCtrl);
        SetBool(catSO, "spriteFacingRight", true);
        catSO.ApplyModifiedPropertiesWithoutUndo();

        // HumanController — 인간 스프라이트는 왼쪽이 기본 방향
        var human = EnsureComponent<HumanController>(root);
        var humanSO = new SerializedObject(human);
        SetObjRef(humanSO, "animatorController", humanCtrl);
        SetBool(humanSO, "spriteFacingRight", false);
        humanSO.ApplyModifiedPropertiesWithoutUndo();

        // InputReader
        EnsureComponent<InputReader>(root);

        // PlayerController
        var pc   = EnsureComponent<PlayerController>(root);
        var pcSO = new SerializedObject(pc);
        var prop = pcSO.FindProperty("startingType");
        if (prop != null) prop.enumValueIndex = (int)startType;
        pcSO.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[FixPrefabControllers] {root.name} 수정 완료");
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static void SetObjRef(SerializedObject so, string field, Object value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = value;
    }

    static void SetBool(SerializedObject so, string field, bool value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.boolValue = value;
    }
}
