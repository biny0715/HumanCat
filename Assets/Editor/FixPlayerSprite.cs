using UnityEngine;
using UnityEditor;
using System.Linq;

public class FixPlayerSprite
{
    public static void Execute()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[Fix] Player 없음"); return; }

        // Animator Controller 재할당
        var animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animations/PlayerController.controller");
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                Debug.Log("[Fix] Animator Controller 재할당 완료");
            }
            else Debug.LogError("[Fix] PlayerController.controller 없음");
        }

        // Sprite 재할당 (비어있을 경우)
        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Cat/NormalCat/normalCat_Idle.png")
                .OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault();
            if (sprite != null) sr.sprite = sprite;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}
