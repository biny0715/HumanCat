using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

/// <summary>
/// Main 씬에 TimeManager·TimeUI·QuitButton·MiniGamePopup을 설치한다.
/// HumanCat → Main → Setup Main Scene 에서 실행.
/// </summary>
public static class MainSceneSetup
{
    [MenuItem("HumanCat/Main/Add World Space Text to Outdoor")]
    public static void AddWorldSpaceText()
    {
        var outdoorGO = GameObject.Find("Outdoor");
        if (outdoorGO == null) { Debug.LogError("Outdoor 오브젝트 없음"); return; }

        var go = new GameObject("WorldText");
        go.transform.SetParent(outdoorGO.transform, false);
        go.transform.position = Vector3.zero;

        var tmp = go.AddComponent<TMPro.TextMeshPro>();
        tmp.text      = "텍스트를 입력하세요";
        tmp.fontSize  = 3f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        // 한글 폰트
        var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
            "Assets/Art/Fonts/Maplestory OTF Bold SDF.asset");
        if (font != null) tmp.font = font;

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MainSceneSetup] WorldText 생성 완료");
    }

    [MenuItem("HumanCat/Main/Fix TriggerZone_MiniGame Component")]
    public static void AddMiniGameTriggerZone()
    {
        // 비활성 오브젝트도 찾기 위해 Resources 방식 사용
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject triggerGO = null;
        foreach (var t in all)
            if (t.name == "TriggerZone_MiniGame") { triggerGO = t.gameObject; break; }

        if (triggerGO == null) { Debug.LogError("TriggerZone_MiniGame 없음"); return; }

        if (triggerGO.GetComponent<MiniGameTriggerZone>() == null)
            triggerGO.AddComponent<MiniGameTriggerZone>();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MainSceneSetup] MiniGameTriggerZone 추가 완료");
    }

    [MenuItem("HumanCat/Main/Fix ToMiniGame_Popup Prefab")]
    public static void FixMiniGamePopupPrefab()
    {
        const string prefabPath = "Assets/Prefabs/UI/ToMiniGame_Popup.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null) { Debug.LogError("ToMiniGame_Popup.prefab 없음"); return; }

        // Prefab 수정
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;

            // DayNightPopup 제거 (혹시 남아있으면)
            var old = root.GetComponent<DayNightPopup>();
            if (old != null) Object.DestroyImmediate(old);

            // MiniGamePopup 추가 및 버튼 연결
            var popup = root.GetComponent<MiniGamePopup>() ?? root.AddComponent<MiniGamePopup>();
            var so    = new SerializedObject(popup);

            var yesBtn = FindChildButton(root, "Yes_Btn");
            var noBtn  = FindChildButton(root, "No_Btn");
            if (yesBtn != null) so.FindProperty("confirmBtn").objectReferenceValue = yesBtn;
            if (noBtn  != null) so.FindProperty("cancelBtn") .objectReferenceValue = noBtn;
            so.ApplyModifiedProperties();

            Debug.Log($"[MainSceneSetup] 프리팹 수정 완료 — Yes:{yesBtn?.name ?? "없음"}  No:{noBtn?.name ?? "없음"}");
        }

        // UIManager에 prefab 레퍼런스 연결
        var uiMgr = Object.FindFirstObjectByType<UIManager>();
        if (uiMgr != null)
        {
            var uiSo = new SerializedObject(uiMgr);
            uiSo.FindProperty("toMiniGamePopupPrefab").objectReferenceValue = prefabAsset;
            uiSo.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[MainSceneSetup] UIManager.toMiniGamePopupPrefab 연결 완료");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[MainSceneSetup] ToMiniGame_Popup 프리팹 수정 완료!");
    }

    [MenuItem("HumanCat/Main/Setup Main Scene")]
    public static void Setup()
    {
        SetupTimeManager();
        SetupTimeUI();
        SetupQuitButton();
        SetupMiniGamePopup();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[MainSceneSetup] 완료!");
    }

    // ── TimeManager ───────────────────────────────────────────────────────

    static void SetupTimeManager()
    {
        if (GameObject.Find("TimeManager") != null)
        { Debug.Log("[MainSceneSetup] TimeManager 이미 존재"); return; }

        var go = new GameObject("TimeManager");
        go.AddComponent<TimeManager>();
        Debug.Log("[MainSceneSetup] TimeManager 생성");
    }

    // ── TimeUI ────────────────────────────────────────────────────────────

    static void SetupTimeUI()
    {
        var timeGO = GameObject.Find("Time");
        if (timeGO == null) { Debug.LogWarning("[MainSceneSetup] 'Time' 오브젝트 없음"); return; }

        var timeUI = timeGO.GetComponent<TimeUI>() ?? timeGO.AddComponent<TimeUI>();
        var so     = new SerializedObject(timeUI);

        // 자식 중 텍스트를 자동으로 찾아 연결
        // "TimeText" 또는 첫 번째 TMP_Text → timeText 필드
        // "PhaseText" → phaseText 필드
        TMP_Text timeText  = FindChildText(timeGO, "TimeText")
                          ?? timeGO.GetComponent<TMP_Text>()
                          ?? timeGO.GetComponentInChildren<TMP_Text>(true);

        TMP_Text phaseText = FindChildText(timeGO, "PhaseText");

        if (timeText  != null) so.FindProperty("timeText") .objectReferenceValue = timeText;
        if (phaseText != null) so.FindProperty("phaseText").objectReferenceValue = phaseText;
        so.ApplyModifiedProperties();

        Debug.Log($"[MainSceneSetup] TimeUI 연결 — timeText:{timeText?.name ?? "없음"}  phaseText:{phaseText?.name ?? "없음"}");
    }

    static TMP_Text FindChildText(GameObject root, string childName)
    {
        var t = root.transform.Find(childName);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    // ── QuitButton ────────────────────────────────────────────────────────

    static void SetupQuitButton()
    {
        var quitGO = GameObject.Find("Quit_Btn");
        if (quitGO == null) { Debug.LogWarning("[MainSceneSetup] 'Quit_Btn' 오브젝트 없음"); return; }

        if (quitGO.GetComponent<QuitButton>() == null)
            quitGO.AddComponent<QuitButton>();

        Debug.Log("[MainSceneSetup] QuitButton 추가 완료");
    }

    // ── MiniGamePopup 생성 + 연결 ─────────────────────────────────────────

    static void SetupMiniGamePopup()
    {
        // [ UI ] 루트 탐색
        var uiRoot = GameObject.Find("[ UI ]");
        if (uiRoot == null) uiRoot = GameObject.Find("Canvas");
        if (uiRoot == null) { Debug.LogError("[MainSceneSetup] [ UI ] 루트 없음"); return; }

        // 이미 존재하면 연결만 재확인
        var existing = uiRoot.transform.Find("ToMiniGame_Popup");
        var popupGO  = existing != null ? existing.gameObject : CreateMiniGamePopup(uiRoot);

        // MiniGamePopup 컴포넌트
        var popup = popupGO.GetComponent<MiniGamePopup>() ?? popupGO.AddComponent<MiniGamePopup>();
        var so    = new SerializedObject(popup);
        var confirmBtn = FindChildButton(popupGO, "Confirm_Btn");
        var cancelBtn  = FindChildButton(popupGO, "Cancel_Btn");
        if (confirmBtn != null) so.FindProperty("confirmBtn").objectReferenceValue = confirmBtn;
        if (cancelBtn  != null) so.FindProperty("cancelBtn") .objectReferenceValue = cancelBtn;
        so.ApplyModifiedProperties();

        // UIManager 연결
        var uiMgr = Object.FindFirstObjectByType<UIManager>();
        if (uiMgr != null)
        {
            var uiSo = new SerializedObject(uiMgr);
            uiSo.FindProperty("toMiniGamePopup").objectReferenceValue = popupGO;
            uiSo.ApplyModifiedProperties();
        }

        popupGO.SetActive(false);
        Debug.Log("[MainSceneSetup] ToMiniGame_Popup 설치 완료");
    }

    static GameObject CreateMiniGamePopup(GameObject parent)
    {
        // 배경 패널
        var panelGO = new GameObject("ToMiniGame_Popup");
        panelGO.transform.SetParent(parent.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.78f);
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(420f, 220f);
        panelRT.anchoredPosition = Vector2.zero;

        // 메시지 텍스트
        var msgGO = new GameObject("MessageText");
        msgGO.transform.SetParent(panelGO.transform, false);
        var msgRT = msgGO.AddComponent<RectTransform>();
        msgRT.anchoredPosition = new Vector2(0f, 50f);
        msgRT.sizeDelta        = new Vector2(380f, 60f);
        var msgTmp = msgGO.AddComponent<TextMeshProUGUI>();
        msgTmp.text      = "미니게임을 시작하시겠습니까?";
        msgTmp.fontSize  = 24;
        msgTmp.alignment = TextAlignmentOptions.Center;
        msgTmp.color     = Color.white;

        // 확인 버튼
        CreatePopupButton("Confirm_Btn", "확인", new Vector2(-75f, -55f), new Color(0.2f, 0.6f, 1f), panelGO.transform);
        // 취소 버튼
        CreatePopupButton("Cancel_Btn", "취소", new Vector2( 75f, -55f), new Color(0.45f, 0.45f, 0.45f), panelGO.transform);

        // 한글 폰트
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/Maplestory OTF Bold SDF.asset");
        if (font != null)
            foreach (var t in panelGO.GetComponentsInChildren<TextMeshProUGUI>(true))
                t.font = font;

        return panelGO;
    }

    static void CreatePopupButton(string name, string label, Vector2 pos, Color color, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(130f, 45f);
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    static Button FindChildButton(GameObject root, string name)
    {
        foreach (var btn in root.GetComponentsInChildren<Button>(true))
            if (btn.name == name) return btn;
        return null;
    }
}
