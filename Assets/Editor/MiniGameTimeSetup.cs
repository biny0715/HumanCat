using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MiniGame_Chase 씬에 TimeManager 오브젝트, MorningPanel,
/// StatPanel 시간 텍스트를 설치한다.
/// HumanCat → MiniGame → Setup Time System 에서 실행.
/// </summary>
public static class MiniGameTimeSetup
{
    [MenuItem("HumanCat/MiniGame/Setup Time System")]
    public static void Setup()
    {
        EnsureTimeManager();
        SetupMorningPanel();
        AddStatPanelTimeText();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[MiniGameTimeSetup] 완료!");
    }

    // ── TimeManager GO ────────────────────────────────────────────────────

    static void EnsureTimeManager()
    {
        if (GameObject.Find("TimeManager") != null)
        { Debug.Log("[MiniGameTimeSetup] TimeManager 이미 존재"); return; }

        var go = new GameObject("TimeManager");
        go.AddComponent<TimeManager>();
        Debug.Log("[MiniGameTimeSetup] TimeManager 생성 완료");
    }

    // ── MorningPanel ──────────────────────────────────────────────────────

    static void SetupMorningPanel()
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) { Debug.LogError("Canvas 없음"); return; }

        // 이미 있으면 스킵
        if (canvasGO.transform.Find("MorningPanel") != null)
        { Debug.Log("[MiniGameTimeSetup] MorningPanel 이미 존재"); WireMorningPanel(canvasGO); return; }

        // 전체화면 반투명 패널
        var panelGO = new GameObject("MorningPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(1f, 0.85f, 0.4f, 0.92f);
        var rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // 메시지 텍스트
        var msgGO = new GameObject("MorningText");
        msgGO.transform.SetParent(panelGO.transform, false);
        var msgRT = msgGO.AddComponent<RectTransform>();
        msgRT.anchoredPosition = new Vector2(0f, 60f);
        msgRT.sizeDelta        = new Vector2(500f, 80f);
        var msgTmp = msgGO.AddComponent<TextMeshProUGUI>();
        msgTmp.text      = "아침이 되어 집으로 돌아갑니다.";
        msgTmp.fontSize  = 28;
        msgTmp.alignment = TextAlignmentOptions.Center;
        msgTmp.color     = new Color(0.2f, 0.1f, 0f);

        // 확인 버튼
        var btnGO = new GameObject("ConfirmBtn");
        btnGO.transform.SetParent(panelGO.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchoredPosition = new Vector2(0f, -30f);
        btnRT.sizeDelta        = new Vector2(160f, 50f);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.7f, 0.4f, 0.1f);
        btnGO.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text      = "확인";
        labelTmp.fontSize  = 22;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color     = Color.white;

        panelGO.SetActive(false);

        // 한글 폰트 적용
        ApplyKoreanFont(panelGO);

        Debug.Log("[MiniGameTimeSetup] MorningPanel 생성 완료");

        WireMorningPanel(canvasGO);
    }

    static void WireMorningPanel(GameObject canvasGO)
    {
        var panelT = canvasGO.transform.Find("MorningPanel");
        if (panelT == null) return;

        // MiniGameManager에 morningPanel 연결
        var mgr = GameObject.Find("MiniGameManager")?.GetComponent<MiniGameManager>();
        if (mgr != null)
        {
            var so = new SerializedObject(mgr);
            so.FindProperty("morningPanel").objectReferenceValue = panelT.gameObject;
            so.ApplyModifiedProperties();
        }

        // MiniGameResultUI에 확인 버튼 연결
        var resultUI = canvasGO.GetComponent<MiniGameResultUI>();
        if (resultUI == null) resultUI = canvasGO.AddComponent<MiniGameResultUI>();

        var confirmBtn = panelT.Find("ConfirmBtn")?.GetComponent<Button>();
        if (confirmBtn != null)
            AddPersistentListener(confirmBtn, resultUI, "OnMorningConfirm");

        Debug.Log("[MiniGameTimeSetup] MorningPanel 연결 완료");
    }

    // ── StatPanel 시간 텍스트 ─────────────────────────────────────────────

    static void AddStatPanelTimeText()
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) return;

        var statPanel = canvasGO.transform.Find("StatPanel");
        if (statPanel == null) { Debug.LogWarning("StatPanel 없음"); return; }

        // 이미 있으면 스킵
        if (statPanel.Find("StatTimeText") != null)
        { Debug.Log("[MiniGameTimeSetup] StatTimeText 이미 존재"); WireStatTimeText(canvasGO, statPanel); return; }

        var go = new GameObject("StatTimeText");
        go.transform.SetParent(statPanel, false);
        var rt = go.AddComponent<RectTransform>();
        // 우측 하단 앵커
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 20f);
        rt.sizeDelta        = new Vector2(160f, 40f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "00:00";
        tmp.fontSize  = 22;
        tmp.alignment = TextAlignmentOptions.Right;
        tmp.color     = Color.white;

        ApplyKoreanFont(statPanel.gameObject);
        Debug.Log("[MiniGameTimeSetup] StatTimeText 생성 완료");

        WireStatTimeText(canvasGO, statPanel);
    }

    static void WireStatTimeText(GameObject canvasGO, Transform statPanel)
    {
        var statUI = GameObject.Find("StatUI")?.GetComponent<StatUI>();
        if (statUI == null) return;

        var timeText = statPanel.Find("StatTimeText")?.GetComponent<TMP_Text>();
        if (timeText == null) return;

        var so = new SerializedObject(statUI);
        so.FindProperty("statTimeText").objectReferenceValue = timeText;
        so.ApplyModifiedProperties();
        Debug.Log("[MiniGameTimeSetup] statTimeText 연결 완료");
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

    static void AddPersistentListener(Button btn, Object target, string methodName)
    {
        var so    = new SerializedObject(btn);
        var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        // 중복 방지
        for (int i = 0; i < calls.arraySize; i++)
        {
            if (calls.GetArrayElementAtIndex(i)
                     .FindPropertyRelative("m_MethodName").stringValue == methodName) return;
        }
        calls.InsertArrayElementAtIndex(calls.arraySize);
        var call = calls.GetArrayElementAtIndex(calls.arraySize - 1);
        call.FindPropertyRelative("m_Target").objectReferenceValue     = target;
        call.FindPropertyRelative("m_MethodName").stringValue          = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex             = 1;
        call.FindPropertyRelative("m_CallState").enumValueIndex        = 2;
        so.ApplyModifiedProperties();
    }
}
