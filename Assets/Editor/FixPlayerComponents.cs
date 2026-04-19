using UnityEngine;
using UnityEditor;

public static class FixPlayerComponents
{
    [MenuItem("HumanCat/Fix Player Components in Scene")]
    static void Fix()
    {
        // MaleHuman 또는 NormalCat 이름의 오브젝트를 모두 찾아 수정
        string[] targets = { "MaleHuman", "NormalCat", "Player" };

        foreach (var targetName in targets)
        {
            var go = GameObject.Find(targetName);
            if (go == null) continue;

            // 누락된 컴포넌트 추가 (이미 있으면 중복 추가 안 함)
            AddIfMissing<InputReader>(go);
            AddIfMissing<CatController>(go);
            AddIfMissing<HumanController>(go);
            AddIfMissing<PlayerController>(go);

            // 기존 Animator Controller가 있으면 유지
            var anim = go.GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController == null)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    targetName == "MaleHuman"
                        ? "Assets/Animations/Human/HumanController.controller"
                        : "Assets/Animations/PlayerController.controller");
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
            }

            Debug.Log($"[FixPlayerComponents] {targetName} 컴포넌트 보정 완료");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }

    static void AddIfMissing<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
            go.AddComponent<T>();
    }
}
