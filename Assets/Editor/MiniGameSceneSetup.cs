using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// MiniGame_Chase 씬의 Canvas UI 생성 + 모든 컴포넌트 레퍼런스 자동 연결.
/// HumanCat → MiniGame → Setup MiniGame Scene 메뉴에서 실행.
/// </summary>
public static class MiniGameSceneSetup
{
    [MenuItem("HumanCat/MiniGame/Setup MiniGame Scene")]
    public static void Setup()
    {
        // ── 기존 오브젝트 찾기 ────────────────────────────────────────────
        var playerGO        = GameObject.Find("Player");
        var targetDummyGO   = GameObject.Find("TargetDummy");
        var tileManagerGO   = GameObject.Find("TileManager");
        var obstacleManagerGO = GameObject.Find("ObstacleManager");
        var miniGameManagerGO = GameObject.Find("MiniGameManager");
        var mainCamera      = Camera.main?.gameObject;

        if (playerGO == null || targetDummyGO == null || tileManagerGO == null ||
            obstacleManagerGO == null || miniGameManagerGO == null || mainCamera == null)
        {
            Debug.LogError("[MiniGameSceneSetup] 필수 오브젝트를 찾을 수 없습니다. 씬 구성 후 다시 실행하세요.");
            return;
        }

        // ── Canvas 생성 ───────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── TimerText ─────────────────────────────────────────────────────
        var timerGO   = new GameObject("TimerText");
        timerGO.transform.SetParent(canvasGO.transform, false);
        var timerText = timerGO.AddComponent<TextMeshProUGUI>();
        timerText.text      = "01:00";
        timerText.fontSize  = 48;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color     = Color.white;
        var timerRT = timerGO.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0.5f, 1f);
        timerRT.anchorMax = new Vector2(0.5f, 1f);
        timerRT.pivot     = new Vector2(0.5f, 1f);
        timerRT.anchoredPosition = new Vector2(0f, -20f);
        timerRT.sizeDelta        = new Vector2(200f, 60f);

        // ── HP Slider ─────────────────────────────────────────────────────
        var sliderGO = new GameObject("HPSlider");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        var slider   = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 1f;
        var sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0f, 0f);
        sliderRT.anchorMax = new Vector2(0f, 0f);
        sliderRT.pivot     = new Vector2(0f, 0f);
        sliderRT.anchoredPosition = new Vector2(20f, 20f);
        sliderRT.sizeDelta        = new Vector2(300f, 30f);

        // Slider Background
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgImage  = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Slider Fill Area
        var fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero; fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.sizeDelta = Vector2.zero;

        var fillGO   = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillImage = fillGO.AddComponent<Image>();
        fillImage.color = Color.green;
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;

        slider.fillRect       = fillRT;
        slider.targetGraphic  = fillImage;

        // ── HP Text ───────────────────────────────────────────────────────
        var hpTextGO   = new GameObject("HPText");
        hpTextGO.transform.SetParent(canvasGO.transform, false);
        var hpText     = hpTextGO.AddComponent<TextMeshProUGUI>();
        hpText.text      = "HP 100";
        hpText.fontSize  = 28;
        hpText.color     = Color.white;
        var hpRT = hpTextGO.GetComponent<RectTransform>();
        hpRT.anchorMin = new Vector2(0f, 0f);
        hpRT.anchorMax = new Vector2(0f, 0f);
        hpRT.pivot     = new Vector2(0f, 0f);
        hpRT.anchoredPosition = new Vector2(20f, 55f);
        hpRT.sizeDelta        = new Vector2(200f, 40f);

        // ── GameOver Panel ────────────────────────────────────────────────
        var gameOverGO  = CreateFullscreenPanel(canvasGO, "GameOverPanel", new Color(0f, 0f, 0f, 0.7f));
        var gameOverTxt = CreateCenterText(gameOverGO, "GameOverText", "GAME OVER", 64, Color.red);
        gameOverGO.SetActive(false);

        // ── Success Panel ─────────────────────────────────────────────────
        var successGO  = CreateFullscreenPanel(canvasGO, "SuccessPanel", new Color(0f, 0f, 0f, 0.7f));
        var successTxt = CreateCenterText(successGO, "SuccessText", "SUCCESS!", 64, Color.yellow);
        successGO.SetActive(false);

        // ── 컴포넌트 레퍼런스 연결 ────────────────────────────────────────
        var mgm = miniGameManagerGO.GetComponent<MiniGameManager>();
        var so  = new SerializedObject(mgm);
        so.FindProperty("player")          .objectReferenceValue = playerGO.GetComponent<MiniGamePlayer>();
        so.FindProperty("targetDummy")     .objectReferenceValue = targetDummyGO.GetComponent<TargetDummy>();
        so.FindProperty("tileManager")     .objectReferenceValue = tileManagerGO.GetComponent<TileManager>();
        so.FindProperty("obstacleManager") .objectReferenceValue = obstacleManagerGO.GetComponent<ObstacleManager>();
        so.FindProperty("timerText")       .objectReferenceValue = timerText;
        so.FindProperty("hpText")          .objectReferenceValue = hpText;
        so.FindProperty("hpSlider")        .objectReferenceValue = slider;
        so.FindProperty("gameOverPanel")   .objectReferenceValue = gameOverGO;
        so.FindProperty("successPanel")    .objectReferenceValue = successGO;
        so.ApplyModifiedProperties();

        // TileManager
        var tm    = tileManagerGO.GetComponent<TileManager>();
        var tmSO  = new SerializedObject(tm);
        var tilesProp = tmSO.FindProperty("tiles");
        tilesProp.ClearArray();
        string[] tileNames = { "Tile_0", "Tile_1", "Tile_2", "Tile_3", "Tile_4" };
        for (int i = 0; i < tileNames.Length; i++)
        {
            tilesProp.InsertArrayElementAtIndex(i);
            var t = tileManagerGO.transform.Find(tileNames[i]);
            tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = t;
        }
        tmSO.FindProperty("followTarget").objectReferenceValue = playerGO.transform;
        tmSO.ApplyModifiedProperties();

        // ObstacleManager
        var om   = obstacleManagerGO.GetComponent<ObstacleManager>();
        var omSO = new SerializedObject(om);
        omSO.FindProperty("followTarget").objectReferenceValue = playerGO.transform;
        omSO.ApplyModifiedProperties();

        // TargetDummy
        var td   = targetDummyGO.GetComponent<TargetDummy>();
        var tdSO = new SerializedObject(td);
        tdSO.FindProperty("player").objectReferenceValue = playerGO.transform;
        tdSO.ApplyModifiedProperties();

        // RunnerCamera
        var rc   = mainCamera.GetComponent<RunnerCamera>();
        var rcSO = new SerializedObject(rc);
        rcSO.FindProperty("target").objectReferenceValue = playerGO.transform;
        rcSO.ApplyModifiedProperties();

        // Camera 위치 설정
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);

        // ── 씬 저장 ───────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[MiniGameSceneSetup] 씬 구성 완료! Canvas 생성 및 모든 레퍼런스 연결 완료.");
    }

    /// <summary>
    /// 1024×1024 정사각형 Seamless 타일맵 기준으로 3×3 재배치.
    /// 회전 없음, tileWidth/Height = 10.24 (1024px / 100PPU).
    /// col-major 순서: col0row0 ~ col2row2, 중앙(col1,row1)이 게임뷰 중심.
    /// </summary>
    [MenuItem("HumanCat/MiniGame/Setup 3x3 Tiles")]
    public static void Setup3x3Tiles()
    {
        var tileManagerGO = GameObject.Find("TileManager");
        if (tileManagerGO == null) { Debug.LogError("TileManager not found"); return; }

        // 타일 이름 목록 (Tile_0 ~ Tile_8)
        string[] tileNames = { "Tile_0","Tile_1","Tile_2","Tile_3","Tile_4","Tile_5","Tile_6","Tile_7","Tile_8" };

        // 3×3 위치: col-major 순서
        // 1024×1024 정사각형 타일: tileSize = 10.24 units
        // col0: x=-10.24 / col1: x=0 / col2: x=10.24
        // row0: y=10.24  / row1: y=0 / row2: y=-10.24
        float[] xs = { -10.24f, -10.24f, -10.24f,  0f,    0f,      0f,    10.24f, 10.24f, 10.24f };
        float[] ys = {  10.24f,    0f,   -10.24f, 10.24f, 0f,  -10.24f,   10.24f,  0f,   -10.24f };

        var transforms = new Transform[9];

        for (int i = 0; i < tileNames.Length; i++)
        {
            var child = tileManagerGO.transform.Find(tileNames[i]);
            if (child == null) { Debug.LogError($"{tileNames[i]} not found under TileManager"); return; }

            // SpriteRenderer 없으면 추가
            if (child.GetComponent<SpriteRenderer>() == null)
                child.gameObject.AddComponent<SpriteRenderer>();

            // 1024×1024 정사각형 — 회전 불필요
            child.localRotation = Quaternion.identity;

            // 위치
            child.position = new Vector3(xs[i], ys[i], 0f);

            transforms[i] = child;
        }

        // TileManager 컴포넌트 리스트 갱신
        var tm   = tileManagerGO.GetComponent<TileManager>();
        var tmSO = new SerializedObject(tm);
        var tilesProp = tmSO.FindProperty("tiles");
        tilesProp.ClearArray();
        for (int i = 0; i < transforms.Length; i++)
        {
            tilesProp.InsertArrayElementAtIndex(i);
            tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
        }
        tmSO.FindProperty("tileWidth") .floatValue = 10.24f;
        tmSO.FindProperty("tileHeight").floatValue = 10.24f;
        tmSO.FindProperty("columns")   .intValue   = 3;
        tmSO.FindProperty("rows")      .intValue   = 3;
        tmSO.ApplyModifiedProperties();

        // 플레이어 / TargetDummy 시작 위치 재설정
        var player = GameObject.Find("Player");
        var dummy  = GameObject.Find("TargetDummy");
        if (player != null) player.transform.position = new Vector3(0f, 0f, 0f);
        if (dummy  != null) dummy.transform.position  = new Vector3(2f, 0f, 0f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[MiniGameSceneSetup] 3×3 타일 배치 완료. 중앙 타일(Tile_4)이 게임뷰 중심.");
    }

    /// <summary>Tile_0의 스프라이트를 나머지 타일에 복사.</summary>
    [MenuItem("HumanCat/MiniGame/Copy Tile Sprite to All")]
    public static void CopyTileSprite()
    {
        var tileManagerGO = GameObject.Find("TileManager");
        if (tileManagerGO == null) { Debug.LogError("TileManager not found"); return; }

        var source = tileManagerGO.transform.Find("Tile_0")?.GetComponent<SpriteRenderer>();
        if (source == null || source.sprite == null)
        {
            Debug.LogError("Tile_0에 스프라이트가 없습니다. 먼저 Tile_0에 스프라이트를 할당하세요.");
            return;
        }

        int copied = 0;
        for (int i = 1; i <= 8; i++)
        {
            var tile = tileManagerGO.transform.Find($"Tile_{i}");
            if (tile == null) continue;
            var sr = tile.GetComponent<SpriteRenderer>();
            if (sr == null) sr = tile.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            copied++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[MiniGameSceneSetup] 스프라이트 복사 완료: {copied}개 타일에 적용");
    }

    [MenuItem("HumanCat/MiniGame/Set Camera Portrait")]
    public static void SetCameraPortrait()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogError("Main Camera not found"); return; }
        cam.orthographic     = true;
        cam.orthographicSize = 5.12f;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[MiniGameSceneSetup] Camera orthoSize = {cam.orthographicSize}, orthographic = {cam.orthographic}");
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    static GameObject CreateFullscreenPanel(GameObject parent, string name, Color bgColor)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return go;
    }

    static TextMeshProUGUI CreateCenterText(GameObject parent, string name, string text, float size, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(600f, 120f);
        return tmp;
    }
}
