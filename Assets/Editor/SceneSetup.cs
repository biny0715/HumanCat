using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// HumanCat > Setup Scene Hierarchy 메뉴로 씬 기본 구조를 자동 생성한다.
/// 이미 존재하는 오브젝트는 건드리지 않는다.
/// </summary>
public static class SceneSetup
{
    [MenuItem("HumanCat/Setup Scene Hierarchy")]
    static void Setup()
    {
        // [Managers]
        var managers   = GetOrCreate("[ Managers ]");
        var gmGo       = GetOrCreateChild(managers, "GameManager");
        AddIfMissing<GameManager>(gmGo);

        var scGo       = GetOrCreateChild(managers, "SceneController");
        var sc         = AddIfMissing<SceneController>(scGo);

        // [Environment]
        var env        = GetOrCreate("[ Environment ]");
        var outdoor    = GetOrCreateChild(env, "Outdoor");
        var indoor     = GetOrCreateChild(env, "Indoor");
        indoor.SetActive(false);  // 초기 비활성

        // Outdoor 하위 (Background_Day, Background_Night, Props)
        GetOrCreateChild(outdoor, "Background_Day");
        GetOrCreateChild(outdoor, "Background_Night");
        GetOrCreateChild(outdoor, "Props");

        // Indoor 하위
        GetOrCreateChild(indoor, "Background");
        GetOrCreateChild(indoor, "Furniture");

        // [Characters]
        GetOrCreate("[ Characters ]");

        // [UI] - Canvas + NightOverlay
        var uiGo       = GetOrCreate("[ UI ]");
        var canvas     = AddIfMissing<Canvas>(uiGo);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        AddIfMissing<CanvasScaler>(uiGo);
        AddIfMissing<GraphicRaycaster>(uiGo);

        var overlayGo  = GetOrCreateChild(uiGo, "NightOverlay");
        var img        = AddIfMissing<Image>(overlayGo);
        img.color      = new Color(0.05f, 0.05f, 0.2f, 0f);
        img.raycastTarget = false;

        var rt         = overlayGo.GetComponent<RectTransform>();
        rt.anchorMin   = Vector2.zero;
        rt.anchorMax   = Vector2.one;
        rt.offsetMin   = Vector2.zero;
        rt.offsetMax   = Vector2.zero;

        // SceneController 필드 연결
        var so = new SerializedObject(sc);
        so.FindProperty("outdoorRoot") .objectReferenceValue = outdoor;
        so.FindProperty("indoorRoot")  .objectReferenceValue = indoor;
        so.FindProperty("nightOverlay").objectReferenceValue = img;
        so.ApplyModifiedProperties();

        // Debug 버튼 (우측 상단 고정)
        var debugGo  = GetOrCreateChild(uiGo, "DebugUI");
        var debugUI  = AddIfMissing<DayNightDebugUI>(debugGo);

        var btnGo    = GetOrCreateChild(debugGo, "ToggleButton");
        var btn      = AddIfMissing<Button>(btnGo);
        var btnImage = AddIfMissing<Image>(btnGo);
        btnImage.color = new Color(0f, 0f, 0f, 0.6f);

        var btnRt    = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 1f);
        btnRt.anchorMax = new Vector2(1f, 1f);
        btnRt.pivot     = new Vector2(1f, 1f);
        btnRt.anchoredPosition = new Vector2(-20f, -20f);
        btnRt.sizeDelta = new Vector2(160f, 60f);

        var labelGo  = GetOrCreateChild(btnGo, "Label");
        var label    = AddIfMissing<Text>(labelGo);
        label.text      = "→ Night";
        label.alignment = TextAnchor.MiddleCenter;
        label.color     = Color.white;
        label.fontSize  = 24;

        var labelRt  = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin  = Vector2.zero;
        labelRt.anchorMax  = Vector2.one;
        labelRt.offsetMin  = Vector2.zero;
        labelRt.offsetMax  = Vector2.zero;

        var debugSO = new SerializedObject(debugUI);
        debugSO.FindProperty("toggleButton").objectReferenceValue = btn;
        debugSO.FindProperty("buttonLabel") .objectReferenceValue = label;
        debugSO.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SceneSetup] 씬 구조 생성 완료");
    }

    static GameObject GetOrCreate(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go : new GameObject(name);
    }

    static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    static T AddIfMissing<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}
