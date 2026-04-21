using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MiniGame_Chase 씬에 Arrow UI + 결과창 버튼을 설치한다.
/// HumanCat → MiniGame → Setup Arrow & Result UI 에서 실행.
/// </summary>
public static class MiniGameArrowResultSetup
{
    [MenuItem("HumanCat/MiniGame/Fix Game Duration to 30s")]
    public static void FixDuration()
    {
        var mgr = GameObject.Find("MiniGameManager")?.GetComponent<MiniGameManager>();
        if (mgr != null)
        {
            var so = new SerializedObject(mgr);
            so.FindProperty("gameDuration").floatValue = 30f;
            so.ApplyModifiedProperties();
            Debug.Log("[Fix] MiniGameManager.gameDuration → 30s");
        }
        else Debug.LogError("MiniGameManager 없음");

        var lm = GameObject.Find("LevelManager")?.GetComponent<LevelManager>();
        if (lm != null)
        {
            var so = new SerializedObject(lm);
            so.FindProperty("baseDuration").floatValue = 30f;
            so.ApplyModifiedProperties();
            Debug.Log("[Fix] LevelManager.baseDuration → 30s");
        }
        else Debug.LogError("LevelManager 없음");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    [MenuItem("HumanCat/MiniGame/Setup Arrow & Result UI")]
    public static void Setup()
    {
        SetupArrowUI();
        SetupResultButtons();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[MiniGameArrowResultSetup] 완료!");
    }

    // ── Arrow UI ─────────────────────────────────────────────────────────

    static void SetupArrowUI()
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) { Debug.LogError("Canvas 없음"); return; }

        // 이미 존재하면 스킵
        if (canvasGO.transform.Find("ArrowImage") != null)
        {
            Debug.Log("[MiniGameArrowResultSetup] ArrowImage 이미 존재");
            // 그래도 TargetArrow 연결은 확인
            WireTargetArrow(canvasGO);
            return;
        }

        // ArrowImage 생성
        var arrowGO = new GameObject("ArrowImage");
        arrowGO.transform.SetParent(canvasGO.transform, false);
        var rt = arrowGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48f, 48f);
        rt.anchoredPosition = Vector2.zero;

        var img = arrowGO.AddComponent<Image>();
        // 화살표 스프라이트가 없으면 Unity 기본 흰색으로 표시
        // (Inspector에서 나중에 화살표 스프라이트로 교체 가능)
        img.color = new Color(1f, 0.85f, 0f, 0.9f); // 노란색

        Debug.Log("[MiniGameArrowResultSetup] ArrowImage 생성 완료");

        WireTargetArrow(canvasGO);
    }

    static void WireTargetArrow(GameObject canvasGO)
    {
        // TargetArrow 컴포넌트 연결 (Canvas 오브젝트에 붙임)
        var ta = canvasGO.GetComponent<TargetArrow>() ?? canvasGO.AddComponent<TargetArrow>();
        var so = new SerializedObject(ta);

        var targetDummy = GameObject.Find("TargetDummy");
        if (targetDummy != null)
            so.FindProperty("target").objectReferenceValue = targetDummy.transform;

        var arrowRT = canvasGO.transform.Find("ArrowImage")?.GetComponent<RectTransform>();
        if (arrowRT != null)
            so.FindProperty("arrowRT").objectReferenceValue = arrowRT;

        so.ApplyModifiedProperties();
        Debug.Log("[MiniGameArrowResultSetup] TargetArrow 연결 완료");
    }

    // ── GameOverPanel / SuccessPanel 버튼 ────────────────────────────────

    static void SetupResultButtons()
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) { Debug.LogError("Canvas 없음"); return; }

        if (canvasGO.GetComponent<MiniGameResultUI>() == null)
            canvasGO.AddComponent<MiniGameResultUI>();

        SetupPanelButtons("GameOverPanel", canvasGO);
        SetupPanelButtons("SuccessPanel",  canvasGO);
    }

    static void SetupPanelButtons(string panelName, GameObject canvasGO)
    {
        // Find including inactive children
        var panelT  = canvasGO.transform.Find(panelName);
        var panelGO = panelT != null ? panelT.gameObject : null;
        if (panelGO == null) { Debug.LogWarning($"[MiniGameArrowResultSetup] {panelName} 없음"); return; }

        var resultUI = canvasGO.GetComponent<MiniGameResultUI>();

        // 이미 버튼이 있으면 스킵
        if (panelGO.transform.Find("RetryBtn") != null)
        {
            Debug.Log($"[MiniGameArrowResultSetup] {panelName} 버튼 이미 존재");
            return;
        }

        // 다시하기 버튼
        var retryBtn  = CreateButton("RetryBtn",  panelGO.transform, "다시하기", new Vector2(-70f, -80f));
        // 돌아가기 버튼
        var backBtn   = CreateButton("BackBtn",   panelGO.transform, "돌아가기", new Vector2( 70f, -80f));

        // 한글 폰트
        ApplyKoreanFont(panelGO);

        // 버튼 리스너 등록 (SerializedObject 방식)
        if (resultUI != null)
        {
            AddPersistentListener(retryBtn.GetComponent<Button>(), resultUI, "OnRetry");
            AddPersistentListener(backBtn.GetComponent<Button>(),  resultUI, "OnBack");
        }

        Debug.Log($"[MiniGameArrowResultSetup] {panelName} 버튼 생성 완료");
    }

    static void AddPersistentListener(Button btn, Object target, string methodName)
    {
        var so    = new SerializedObject(btn);
        var clicks = so.FindProperty("m_OnClick");
        var calls  = clicks.FindPropertyRelative("m_PersistentCalls.m_Calls");

        calls.InsertArrayElementAtIndex(calls.arraySize);
        var call = calls.GetArrayElementAtIndex(calls.arraySize - 1);
        call.FindPropertyRelative("m_Target").objectReferenceValue  = target;
        call.FindPropertyRelative("m_MethodName").stringValue       = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex          = 1; // void/no args
        call.FindPropertyRelative("m_CallState").enumValueIndex     = 2; // RuntimeOnly
        so.ApplyModifiedProperties();
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────

    static void ApplyKoreanFont(GameObject root)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Art/Fonts/Maplestory OTF Bold SDF.asset");
        if (font == null) return;
        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.font = font;
    }

    static GameObject CreateButton(string name, Transform parent, string label, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(130f, 45f);

        var img = go.AddComponent<Image>();
        img.color = name == "RetryBtn"
            ? new Color(0.2f, 0.6f, 1f)    // 파랑
            : new Color(0.5f, 0.5f, 0.5f); // 회색

        go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return go;
    }
}
