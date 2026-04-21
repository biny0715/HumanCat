using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// MiniGame_Chase 씬에 레벨/스탯 시스템을 설치한다.
/// HumanCat → MiniGame → Setup Stat System 에서 실행.
/// </summary>
public static class MiniGameStatSetup
{
    [MenuItem("HumanCat/MiniGame/Add Back Button to StatPanel")]
    public static void AddBackButton()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas 없음"); return; }

        var statPanel = canvas.transform.Find("StatPanel");
        if (statPanel == null) { Debug.LogError("StatPanel 없음"); return; }

        if (statPanel.Find("BackBtn") != null)
        { Debug.Log("BackBtn 이미 존재"); return; }

        // PlayBtn 위치 기준으로 왼쪽에 배치
        var playBtnT = statPanel.Find("PlayBtn");
        Vector2 playPos = playBtnT != null
            ? playBtnT.GetComponent<RectTransform>().anchoredPosition
            : new Vector2(0f, -210f);
        Vector2 playSize = playBtnT != null
            ? playBtnT.GetComponent<RectTransform>().sizeDelta
            : new Vector2(240f, 75f);

        float gap     = 20f;
        Vector2 backPos  = new Vector2(playPos.x - playSize.x * 0.5f - playSize.x * 0.5f - gap, playPos.y);

        var backBtn = CreateButton("BackBtn", statPanel, "돌아가기", backPos);
        backBtn.GetComponent<RectTransform>().sizeDelta = playSize;

        // 한글 폰트
        ApplyKoreanFont(backBtn);

        // StatUI에 backBtn 연결
        var statUI = GameObject.Find("StatUI")?.GetComponent<StatUI>();
        if (statUI != null)
        {
            var so = new SerializedObject(statUI);
            so.FindProperty("backBtn").objectReferenceValue = backBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MiniGameStatSetup] BackBtn 생성 완료");
    }

    [MenuItem("HumanCat/MiniGame/Setup Stat System")]
    public static void Setup()
    {
        SetupManagers();
        SwapToMiniPlayerMover();
        SetupStatUI();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[MiniGameStatSetup] 완료!");
    }

    // ── 매니저 오브젝트 생성 ──────────────────────────────────────────────

    [MenuItem("HumanCat/MiniGame/Add EventSystem")]
    public static void CreateEventSystem()
    {
        if (FindGO("EventSystem") != null) { Debug.Log("EventSystem 이미 존재"); return; }
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MiniGameStatSetup] EventSystem 생성 완료");
    }

    static void SetupManagers()
    {
        EnsureComponent<StatManager>("StatManager");
        var lmGO = EnsureComponent<LevelManager>("LevelManager");

        // LevelManager 레퍼런스 연결
        var lm = lmGO.GetComponent<LevelManager>();
        var so = new SerializedObject(lm);
        SetRef(so, "targetDummy",     FindGO("TargetDummy"));
        SetRef(so, "obstacleManager", FindGO("ObstacleManager"));
        SetRef(so, "miniGameManager", FindGO("MiniGameManager"));
        so.ApplyModifiedProperties();

        Debug.Log("[MiniGameStatSetup] StatManager / LevelManager 생성 완료");
    }

    // ── PlayerMover → MiniPlayerMover 교체 ────────────────────────────────

    static void SwapToMiniPlayerMover()
    {
        var playerGO = FindGO("Player");
        if (playerGO == null) { Debug.LogError("Player 오브젝트 없음"); return; }

        // 이미 MiniPlayerMover가 있으면 스킵
        if (playerGO.GetComponent<MiniPlayerMover>() != null)
        {
            Debug.Log("[MiniGameStatSetup] MiniPlayerMover 이미 존재");
            return;
        }

        // 기존 PlayerMover 수치 기억 후 제거
        var old = playerGO.GetComponent<PlayerMover>();
        float speed = 5f;
        if (old != null)
        {
            speed = old.MoveSpeed;
            Object.DestroyImmediate(old);
        }

        // MiniPlayerMover 추가 + speed 복원
        var mpm = playerGO.AddComponent<MiniPlayerMover>();
        var mpmSo = new SerializedObject(mpm);
        mpmSo.FindProperty("moveSpeed").floatValue = speed;
        mpmSo.ApplyModifiedProperties();

        Debug.Log("[MiniGameStatSetup] PlayerMover → MiniPlayerMover 교체 완료");
    }

    // ── Stat UI 팝업 생성 ─────────────────────────────────────────────────

    static void SetupStatUI()
    {
        // 기존 Canvas 찾기
        var canvasGO = FindGO("Canvas");
        if (canvasGO == null) { Debug.LogError("Canvas 오브젝트 없음"); return; }

        // 이미 StatPanel 있으면 스킵
        var existing = canvasGO.transform.Find("StatPanel");
        if (existing != null) { Debug.Log("[MiniGameStatSetup] StatPanel 이미 존재"); return; }

        // ── StatPanel ─────────────────────────────────────────────────────
        var panelGO = CreateUIObject("StatPanel", canvasGO.transform);
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.85f);

        // ── 타이틀 ────────────────────────────────────────────────────────
        var title = CreateText("TitleText", panelGO.transform, "스탯 배분", 28, new Vector2(0, 180));

        // ── 레벨 / 포인트 텍스트 ──────────────────────────────────────────
        var levelText  = CreateText("LevelText",  panelGO.transform, "Lv. 1",         22, new Vector2(0, 130));
        var pointsText = CreateText("PointsText", panelGO.transform, "스탯 포인트: 0", 22, new Vector2(0,  90));

        // ── Speed 행 ──────────────────────────────────────────────────────
        var speedVal = CreateText("SpeedValueText", panelGO.transform, "0  (+5.0 속도)", 20, new Vector2(-50, 30));
        var speedBtn = CreateButton("SpeedAddBtn", panelGO.transform, "+", new Vector2(120, 30));

        // ── HP 행 ─────────────────────────────────────────────────────────
        var hpVal = CreateText("HPValueText", panelGO.transform, "0  (+0 HP)", 20, new Vector2(-50, -20));
        var hpBtn = CreateButton("HPAddBtn", panelGO.transform, "+", new Vector2(120, -20));

        // ── Resistance 행 ─────────────────────────────────────────────────
        var resistVal = CreateText("ResistValueText", panelGO.transform, "0  (충돌 감소 20%유지)", 20, new Vector2(-50, -70));
        var resistBtn = CreateButton("ResistAddBtn", panelGO.transform, "+", new Vector2(120, -70));

        // ── Play 버튼 ─────────────────────────────────────────────────────
        var playBtn = CreateButton("PlayBtn", panelGO.transform, "▶ PLAY", new Vector2(0, -140));
        var playRT  = playBtn.GetComponent<RectTransform>();
        playRT.sizeDelta = new Vector2(160, 50);

        // ── StatUI 컴포넌트 연결 ──────────────────────────────────────────
        var statUIGO = FindGO("StatUI") ?? new GameObject("StatUI");
        var statUI   = statUIGO.GetComponent<StatUI>() ?? statUIGO.AddComponent<StatUI>();
        var uiSo     = new SerializedObject(statUI);
        uiSo.FindProperty("statPanel").objectReferenceValue       = panelGO;
        uiSo.FindProperty("levelText").objectReferenceValue       = levelText;
        uiSo.FindProperty("pointsText").objectReferenceValue      = pointsText;
        uiSo.FindProperty("speedValueText").objectReferenceValue  = speedVal;
        uiSo.FindProperty("speedAddBtn").objectReferenceValue     = speedBtn.GetComponent<Button>();
        uiSo.FindProperty("hpValueText").objectReferenceValue     = hpVal;
        uiSo.FindProperty("hpAddBtn").objectReferenceValue        = hpBtn.GetComponent<Button>();
        uiSo.FindProperty("resistValueText").objectReferenceValue = resistVal;
        uiSo.FindProperty("resistAddBtn").objectReferenceValue    = resistBtn.GetComponent<Button>();
        uiSo.FindProperty("playBtn").objectReferenceValue         = playBtn.GetComponent<Button>();
        uiSo.ApplyModifiedProperties();

        // 한글 폰트 적용
        ApplyKoreanFont(panelGO);

        Debug.Log("[MiniGameStatSetup] StatUI 생성 완료");
    }

    static void ApplyKoreanFont(GameObject root)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Art/Fonts/Maplestory OTF Bold SDF.asset");
        if (font == null) return;

        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.font = font;
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    static GameObject EnsureComponent<T>(string goName) where T : Component
    {
        var go = FindGO(goName) ?? new GameObject(goName);
        if (go.GetComponent<T>() == null) go.AddComponent<T>();
        return go;
    }

    static GameObject FindGO(string name) => GameObject.Find(name);

    static void SetRef(SerializedObject so, string prop, GameObject go)
    {
        if (go == null) return;
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = go.GetComponent(p.type) ?? (Object)go;
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static TMP_Text CreateText(string name, Transform parent, string text, int size, Vector2 anchoredPos)
    {
        var go = CreateUIObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(400, 35);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return tmp;
    }

    static GameObject CreateButton(string name, Transform parent, string label, Vector2 anchoredPos)
    {
        var go  = CreateUIObject(name, parent);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(60, 35);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);

        go.AddComponent<Button>();

        var labelGO = CreateUIObject("Label", go.transform);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return go;
    }
}
