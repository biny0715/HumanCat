using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UISetup : MonoBehaviour
{
    [MenuItem("HumanCat/Setup UI")]
    static void Setup()
    {
        var uiRoot = GameObject.Find("[ UI ]");
        if (uiRoot == null) { Debug.LogError("[UISetup] [ UI ] 없음"); return; }

        // ── Arrow 스프라이트 연결 ──────────────────────────────────────
        var arrowGo = uiRoot.transform.Find("Arrow")?.gameObject;
        if (arrowGo != null)
        {
            var arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Arrow.png");
            var img = arrowGo.GetComponent<Image>();
            if (img == null) img = arrowGo.AddComponent<Image>();
            if (arrowSprite != null) { img.sprite = arrowSprite; img.SetNativeSize(); }
            img.raycastTarget = false;

            var rt = arrowGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-30f, 0f);
        }

        // ── Popup 프리팹 설정 ─────────────────────────────────────────
        SetupPopupPrefab("Assets/Prefabs/UI/ToNightApply_Popup.prefab");
        SetupPopupPrefab("Assets/Prefabs/UI/ToDayApply_Popup.prefab");

        // ── UIManager 필드 연결 ────────────────────────────────────────
        var uiManagerGo = GameObject.Find("[ Managers ]/UIManager");
        if (uiManagerGo != null)
        {
            var mgr = uiManagerGo.GetComponent<UIManager>();
            if (mgr != null)
            {
                var so = new SerializedObject(mgr);
                so.FindProperty("arrowUI").objectReferenceValue = arrowGo;
                so.FindProperty("toNightPopupPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ToNightApply_Popup.prefab");
                so.FindProperty("toDayPopupPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ToDayApply_Popup.prefab");
                so.FindProperty("popupParent").objectReferenceValue = uiRoot.transform;
                so.ApplyModifiedProperties();
                Debug.Log("[UISetup] UIManager 연결 완료");
            }
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[UISetup] 완료");
    }

    static void SetupPopupPrefab(string path)
    {
        using var scope = new PrefabUtility.EditPrefabContentsScope(path);
        var root = scope.prefabContentsRoot;

        if (root.GetComponent<DayNightPopup>() == null)
            root.AddComponent<DayNightPopup>();

        var popup = root.GetComponent<DayNightPopup>();
        var so = new SerializedObject(popup);

        foreach (var btn in root.GetComponentsInChildren<Button>(true))
        {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("accept") || n.Contains("confirm") || n.Contains("ok") || n.Contains("yes"))
                so.FindProperty("acceptButton").objectReferenceValue = btn;
            else if (n.Contains("cancel") || n.Contains("no") || n.Contains("close"))
                so.FindProperty("cancelButton").objectReferenceValue = btn;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[UISetup] 팝업 설정: {path}");
    }
}
