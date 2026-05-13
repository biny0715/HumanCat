using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// LoginScene UI 하이라키 자동 구성 유틸. MCP editor_invoke_method 로 호출.
/// 이미 같은 이름의 GameObject가 있으면 재사용/덮어쓰기.
/// </summary>
public static class LoginSceneBuilder
{
    const float CanvasW = 1080f;
    const float CanvasH = 1920f;

    const string FontAssetPath = "Assets/Art/Fonts/Maplestory OTF Bold SDF.asset";

    /// <summary>현재 씬의 모든 TMP_Text에 Main 씬과 동일한 폰트를 적용.</summary>
    public static void ApplyFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            Debug.LogError("[LoginSceneBuilder] Font asset not found: " + FontAssetPath);
            return;
        }

        var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int applied = 0;
        foreach (var t in texts)
        {
            t.font = font;
            EditorUtility.SetDirty(t);
            applied++;
        }

        EditorSceneManagerMarkActiveSceneDirty();
        Debug.Log($"[LoginSceneBuilder] Font applied to {applied} TMP_Text(s).");
    }

    /// <summary>CutsceneRoot 배경을 검은색으로(=letterbox 띠 색) + ImageView preserveAspect=true. 모든 기종에서 이미지 전체 보존.</summary>
    public static void CutsceneFitMode()
    {
        var root = GameObject.Find("UICanvas/CutsceneRoot");
        if (root == null) { Debug.LogError("[Builder] CutsceneRoot not found."); return; }

        var rootImg = root.GetComponent<Image>();
        if (rootImg != null)
        {
            rootImg.color         = Color.black;
            rootImg.raycastTarget = true;
        }

        var view = root.transform.Find("ImageView")?.GetComponent<Image>();
        if (view != null)
        {
            view.preserveAspect = true;
            var arf = view.GetComponent<AspectRatioFitter>();
            if (arf != null) UnityEngine.Object.DestroyImmediate(arf);
        }

        EditorSceneManagerMarkActiveSceneDirty();
        Debug.Log("[Builder] Cutscene = Fit (letterbox + black bg).");
    }

    /// <summary>이미지를 화면을 꽉 채우도록 envelope. 비율 차이만큼 가장자리가 crop 됨.</summary>
    public static void CutsceneCoverMode()
    {
        var root = GameObject.Find("UICanvas/CutsceneRoot");
        if (root == null) { Debug.LogError("[Builder] CutsceneRoot not found."); return; }

        var view = root.transform.Find("ImageView")?.GetComponent<Image>();
        if (view == null) { Debug.LogError("[Builder] ImageView not found."); return; }

        view.preserveAspect = false; // AspectRatioFitter 가 RectTransform 직접 조정
        var arf = view.GetComponent<AspectRatioFitter>();
        if (arf == null) arf = view.gameObject.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        // 첫 컷의 sprite 비율을 기준값으로 — 이후 컷마다 같은 비율이면 OK, 다르면 런타임에 갱신 필요
        var firstCut = root.GetComponent<CutsceneManager>();
        if (firstCut != null)
        {
            var so = new SerializedObject(firstCut);
            var cuts = so.FindProperty("cuts");
            if (cuts.arraySize > 0)
            {
                var sp = cuts.GetArrayElementAtIndex(0).FindPropertyRelative("image").objectReferenceValue as Sprite;
                if (sp != null) arf.aspectRatio = sp.rect.width / sp.rect.height;
            }
        }

        EditorSceneManagerMarkActiveSceneDirty();
        Debug.Log($"[Builder] Cutscene = Cover (envelope), aspectRatio={arf.aspectRatio:F3}.");
    }

    /// <summary>UICanvas 가 WorldSpace 등으로 되어 있을 때 ScreenSpaceOverlay 로 강제.</summary>
    public static void FixCanvas()
    {
        var go = GameObject.Find("UICanvas") ?? GameObject.Find("Canvas");
        if (go == null) { Debug.LogError("[LoginSceneBuilder] UICanvas not found."); return; }

        var canvas = go.GetComponent<Canvas>();
        if (canvas == null) { Debug.LogError("[LoginSceneBuilder] Canvas component missing."); return; }

        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect  = false;
        canvas.sortingOrder  = 0;
        canvas.worldCamera   = null;

        var scaler = go.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
        }

        EditorUtility.SetDirty(canvas);
        EditorSceneManagerMarkActiveSceneDirty();
        Debug.Log($"[LoginSceneBuilder] Canvas renderMode = {canvas.renderMode}");
    }

    /// <summary>현재 씬의 CutsceneManager.cuts 에 시나리오 대사를 주입.</summary>
    public static void ApplyDialogues()
    {
        var cutsceneRoot = GameObject.Find("UICanvas/CutsceneRoot");
        if (cutsceneRoot == null) { Debug.LogError("[LoginSceneBuilder] CutsceneRoot not found."); return; }
        var cm = cutsceneRoot.GetComponent<CutsceneManager>();
        if (cm == null)         { Debug.LogError("[LoginSceneBuilder] CutsceneManager not found."); return; }

        var scenario = new[]
        {
            new[] { "오늘도 평화로운 하루였다.",
                    "아이들도 건강해 보이고…",
                    "이대로만 계속되면 좋을 텐데." },

            new[] { "…어?",
                    "저 고양이…",
                    "왜 저렇게 힘이 없어 보이지?" },

            new[] { "괜찮아… 내가 도와줄게.",
                    "조금만 기다려…" },

            new[] { "어…?!",
                    "잠깐—!" },

            new[] { "…뭐지…",
                    "앞이… 안 보여…",
                    "…",
                    "(의식이 끊어진다)" },

            new[] { "…여긴…",
                    "어?",
                    "왜 이렇게… 낮지?",
                    "…이게 뭐야…",
                    "내 손이… 아니야.",
                    "…설마…" },

            new[] { "…이건…",
                    "말도 안 돼…",
                    "내가… 고양이가 된 거야…?",
                    "도대체… 무슨 일이 일어난 거지…" },

            new[] { "…?!",
                    "몸이… 돌아왔어…?",
                    "아침 6시…",
                    "설마…",
                    "밤이 되면… 다시…?",
                    "이건… 그냥 사고가 아니야.",
                    "우선… 오늘 밤 다시 확인해보자." }
        };

        var so       = new SerializedObject(cm);
        var cutsProp = so.FindProperty("cuts");

        if (cutsProp.arraySize < scenario.Length)
            cutsProp.arraySize = scenario.Length;

        for (int i = 0; i < scenario.Length; i++)
        {
            var elem      = cutsProp.GetArrayElementAtIndex(i);
            var dialogues = elem.FindPropertyRelative("dialogues");
            dialogues.arraySize = scenario[i].Length;
            for (int j = 0; j < scenario[i].Length; j++)
                dialogues.GetArrayElementAtIndex(j).stringValue = scenario[i][j];
        }
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(cm);
        EditorSceneManagerMarkActiveSceneDirty();
        Debug.Log($"[LoginSceneBuilder] Dialogues applied to {scenario.Length} cuts.");
    }

    public static void Build()
    {
        var canvasGo = GameObject.Find("UICanvas") ?? GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            Debug.LogError("[LoginSceneBuilder] UICanvas not found.");
            return;
        }
        var canvasRt = canvasGo.GetComponent<RectTransform>();

        // 1) 3 root panels (full-screen stretched)
        var cutsceneRoot  = EnsureChildPanel(canvasRt, "CutsceneRoot");
        var nameInputRoot = EnsureChildPanel(canvasRt, "NameInputRoot");
        var loginRoot     = EnsureChildPanel(canvasRt, "LoginRoot");

        BuildCutscene (cutsceneRoot);
        BuildNameInput(nameInputRoot);
        BuildLogin    (loginRoot);

        // 2) LoginManager GameObject
        var loginManagerGo = GameObject.Find("LoginManager");
        if (loginManagerGo == null)
        {
            loginManagerGo = new GameObject("LoginManager");
            Undo.RegisterCreatedObjectUndo(loginManagerGo, "Create LoginManager");
        }
        var loginManager = loginManagerGo.GetComponent<LoginManager>() ?? loginManagerGo.AddComponent<LoginManager>();

        // 3) Wire LoginManager references via SerializedObject
        WireLoginManager(loginManager,
            cutsceneRoot.gameObject, nameInputRoot.gameObject, loginRoot.gameObject,
            cutsceneRoot.GetComponent<CutsceneManager>(),
            nameInputRoot.GetComponent<NameInputUI>(),
            loginRoot.GetComponent<LoginUI>());

        // 4) Default: only CutsceneRoot active (first-launch scenario)
        cutsceneRoot .gameObject.SetActive(true);
        nameInputRoot.gameObject.SetActive(false);
        loginRoot    .gameObject.SetActive(false);

        EditorUtility.SetDirty(loginManagerGo);
        EditorSceneManagerMarkActiveSceneDirty();
        Debug.Log("[LoginSceneBuilder] Build complete.");
    }

    // ── Root sections ─────────────────────────────────────────────────────

    static void BuildCutscene(RectTransform root)
    {
        var cm = root.GetComponent<CutsceneManager>() ?? root.gameObject.AddComponent<CutsceneManager>();

        // Background image (full-screen, preserves aspect for cutscene art)
        var bg = EnsureChild<Image>(root, "ImageView");
        StretchFull(bg.rectTransform);
        bg.raycastTarget  = true;
        bg.preserveAspect = true;

        // Text (bottom area) + TypewriterEffect
        var txt = EnsureTMP(root, "TextView");
        var txtRt = txt.rectTransform;
        SetAnchor(txtRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        txtRt.anchoredPosition = new Vector2(0f, 280f);
        txtRt.sizeDelta        = new Vector2(900f, 360f);
        txt.alignment = TextAlignmentOptions.Top;
        txt.fontSize  = 42f;
        txt.text      = string.Empty;

        var tw = txt.gameObject.GetComponent<TypewriterEffect>() ?? txt.gameObject.AddComponent<TypewriterEffect>();
        var soTw = new SerializedObject(tw);
        soTw.FindProperty("target").objectReferenceValue = txt;
        soTw.FindProperty("charsPerSecond").floatValue = 30f;
        soTw.ApplyModifiedProperties();

        // Skip button (top-right)
        var skip = EnsureButton(root, "SkipButton", "Skip");
        var skipRt = skip.GetComponent<RectTransform>();
        SetAnchor(skipRt, new Vector2(1f, 1f), new Vector2(1f, 1f));
        skipRt.anchoredPosition = new Vector2(-100f, -80f);
        skipRt.sizeDelta        = new Vector2(160f, 80f);

        // Root: transparent Image for IPointerClickHandler to receive taps
        var rootImg = root.gameObject.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
        rootImg.color         = new Color(0f, 0f, 0f, 0f);
        rootImg.raycastTarget = true;

        // FadePanel: black overlay rendered ON TOP (= last child). CanvasGroup for alpha.
        var fade = EnsureChild<Image>(root, "FadePanel");
        StretchFull(fade.rectTransform);
        fade.color         = Color.black;
        fade.raycastTarget = false;
        var fadeGroup = fade.gameObject.GetComponent<CanvasGroup>();
        if (fadeGroup == null) fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
        Debug.Log("[Builder] FadePanel CanvasGroup: " + (fadeGroup != null ? "OK" : "NULL"));
        fadeGroup.alpha          = 1f;
        fadeGroup.blocksRaycasts = true;
        fadeGroup.interactable   = false;
        fade.transform.SetAsLastSibling();

        // Sibling order: ImageView, TextView, SkipButton, FadePanel(last)
        bg  .transform.SetAsFirstSibling();
        txt .transform.SetSiblingIndex(1);
        skip.transform.SetSiblingIndex(2);
        fade.transform.SetAsLastSibling();

        // Auto-populate cuts from Cut_01..Cut_NN
        var cutData = LoadCutsFromFolder("Assets/Art/Cutscenes/Login", "Cut_");

        // Wire CutsceneManager
        var soCm = new SerializedObject(cm);
        soCm.FindProperty("imageView") .objectReferenceValue = bg;
        soCm.FindProperty("typewriter").objectReferenceValue = tw;
        soCm.FindProperty("skipButton").objectReferenceValue = skip;
        soCm.FindProperty("fadePanel") .objectReferenceValue = fadeGroup;

        // Cuts array
        var cutsProp = soCm.FindProperty("cuts");
        cutsProp.arraySize = cutData.Length;
        for (int i = 0; i < cutData.Length; i++)
        {
            var elem = cutsProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("image").objectReferenceValue = cutData[i].sprite;
            var dialogues = elem.FindPropertyRelative("dialogues");
            dialogues.arraySize = 1;
            dialogues.GetArrayElementAtIndex(0).stringValue = $"컷 {i + 1} 대사 1 (Inspector에서 수정)";
            elem.FindPropertyRelative("fadeInDuration") .floatValue = 0.5f;
            elem.FindPropertyRelative("fadeOutDuration").floatValue = 0.5f;
        }
        soCm.ApplyModifiedProperties();
    }

    struct CutAssetInfo { public Sprite sprite; }

    static CutAssetInfo[] LoadCutsFromFolder(string folder, string prefix)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        var list  = new System.Collections.Generic.List<(string path, Sprite sprite)>();
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith(prefix)) continue;

            EnsureSpriteImport(path);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) list.Add((path, sp));
        }
        list.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));

        var arr = new CutAssetInfo[list.Count];
        for (int i = 0; i < list.Count; i++) arr[i] = new CutAssetInfo { sprite = list[i].sprite };
        return arr;
    }

    static void EnsureSpriteImport(string assetPath)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;
        bool changed = false;
        if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
        if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (changed) { ti.SaveAndReimport(); }
    }

    static void BuildNameInput(RectTransform root)
    {
        var ui = root.GetComponent<NameInputUI>() ?? root.gameObject.AddComponent<NameInputUI>();

        // 화면 꽉 채우는 배경 (Envelope) + 후보 sprite 는 Inspector 에서 드래그
        AttachBackground(root);

        // Title image above the inputs (HumanCat_Title)
        var title = EnsureChild<Image>(root, "TitleImage");
        EnsureSpriteImport("Assets/Art/UI/HumanCat_Title.png");
        var titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/HumanCat_Title.png");
        if (titleSprite != null) title.sprite = titleSprite;
        title.preserveAspect = true;
        title.raycastTarget  = false;
        SetCentered(title.rectTransform, 0f, 400f, 720f, 720f);

        var userInput    = EnsureInputField(root, "UserNameInput",    "사용자 이름");
        var shelterInput = EnsureInputField(root, "ShelterNameInput", "보호소 이름");
        var submit       = EnsureButton    (root, "SubmitButton",     "확인");
        var error        = EnsureTMP       (root, "ErrorText");

        SetCentered(userInput   .GetComponent<RectTransform>(),     0f, -120f, 760f, 110f);
        SetCentered(shelterInput.GetComponent<RectTransform>(),     0f, -260f, 760f, 110f);
        SetCentered(submit      .GetComponent<RectTransform>(),     0f, -440f, 360f, 110f);
        SetCentered(error       .rectTransform,                     0f, -560f, 900f, 80f);

        error.alignment = TextAlignmentOptions.Center;
        error.color     = new Color(1f, 0.4f, 0.4f, 1f);
        error.fontSize  = 32f;
        error.text      = "";
        error.gameObject.SetActive(false);

        var so = new SerializedObject(ui);
        so.FindProperty("userNameInput")   .objectReferenceValue = userInput;
        so.FindProperty("shelterNameInput").objectReferenceValue = shelterInput;
        so.FindProperty("submitButton")    .objectReferenceValue = submit;
        so.FindProperty("errorText")       .objectReferenceValue = error;
        so.ApplyModifiedProperties();
    }

    static void BuildLogin(RectTransform root)
    {
        var ui = root.GetComponent<LoginUI>() ?? root.gameObject.AddComponent<LoginUI>();

        // 화면 꽉 채우는 배경 (Envelope) + 후보 sprite 는 Inspector 에서 드래그
        AttachBackground(root);

        var userLabel    = EnsureTMP   (root, "UserNameLabel");
        var shelterLabel = EnsureTMP   (root, "ShelterNameLabel");
        var loginBtn     = EnsureButton(root, "LoginButton", "로그인");

        SetCentered(userLabel   .rectTransform,                300f, 760f, 110f);
        SetCentered(shelterLabel.rectTransform,                140f, 760f, 110f);
        SetCentered(loginBtn    .GetComponent<RectTransform>(),-80f, 360f, 110f);

        userLabel   .alignment = TextAlignmentOptions.Center; userLabel   .fontSize = 56f; userLabel   .text = "사용자 이름";
        shelterLabel.alignment = TextAlignmentOptions.Center; shelterLabel.fontSize = 40f; shelterLabel.text = "보호소 이름";

        var so = new SerializedObject(ui);
        so.FindProperty("userNameLabel")   .objectReferenceValue = userLabel;
        so.FindProperty("shelterNameLabel").objectReferenceValue = shelterLabel;
        so.FindProperty("loginButton")     .objectReferenceValue = loginBtn;
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// 패널에 풀스크린 cover 배경 + BackgroundRandomizer 부착.
    /// AspectRatioFitter.EnvelopeParent 로 letterbox 없이 화면 전체 채움.
    /// candidates 배열은 Inspector 에서 사용자가 직접 드래그.
    /// </summary>
    static void AttachBackground(RectTransform root)
    {
        // BackgroundImage 자식 (anchor center, sizeDelta 0 — Fitter 가 크기 제어)
        var bg = EnsureChild<Image>(root, "BackgroundImage");
        var bgRt = bg.rectTransform;
        bgRt.anchorMin        = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax        = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta        = new Vector2(1080f, 1920f); // 초기값, Fitter 가 envelope 으로 재조정
        bgRt.localScale       = Vector3.one;
        bg.preserveAspect = false;
        bg.raycastTarget  = false;
        bg.transform.SetAsFirstSibling();

        var fitter = bg.gameObject.GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode  = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 941f / 1672f; // Cut 이미지 기본 비율 (런타임에 sprite 따라 갱신)

        // root 자체는 검정 (혹시 빈 영역 생길 때 fallback)
        var rootImg = root.gameObject.GetComponent<Image>();
        if (rootImg == null) rootImg = root.gameObject.AddComponent<Image>();
        rootImg.color         = Color.black;
        rootImg.raycastTarget = false;

        // BackgroundRandomizer 를 root 에 부착하고 참조 wire
        var rnd = root.gameObject.GetComponent<BackgroundRandomizer>();
        if (rnd == null) rnd = root.gameObject.AddComponent<BackgroundRandomizer>();
        var so = new SerializedObject(rnd);
        so.FindProperty("targetImage") .objectReferenceValue = bg;
        so.FindProperty("aspectFitter").objectReferenceValue = fitter;
        so.ApplyModifiedProperties();
    }

    static void WireLoginManager(LoginManager m, GameObject cutsceneRoot, GameObject nameInputRoot, GameObject loginRoot,
                                 CutsceneManager cm, NameInputUI nu, LoginUI lu)
    {
        var so = new SerializedObject(m);
        so.FindProperty("cutsceneRoot") .objectReferenceValue = cutsceneRoot;
        so.FindProperty("nameInputRoot").objectReferenceValue = nameInputRoot;
        so.FindProperty("loginRoot")    .objectReferenceValue = loginRoot;
        so.FindProperty("cutscene")     .objectReferenceValue = cm;
        so.FindProperty("nameInput")    .objectReferenceValue = nu;
        so.FindProperty("loginUI")      .objectReferenceValue = lu;
        so.FindProperty("mainSceneName").stringValue = "Main";
        so.ApplyModifiedProperties();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    static RectTransform EnsureChildPanel(RectTransform parent, string name)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null)
        {
            StretchFull(existing); // 기존 객체도 anchor 재설정 (이전에 MCP로 100×100 default 였던 케이스 보정)
            return existing;
        }

        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        StretchFull(rt);
        return rt;
    }

    static T EnsureChild<T>(RectTransform parent, string name) where T : Component
    {
        var t = parent.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = t.gameObject;
        }
        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static TMP_Text EnsureTMP(RectTransform parent, string name)
    {
        var tmp = EnsureChild<TextMeshProUGUI>(parent, name);
        tmp.text = string.Empty;
        return tmp;
    }

    static Button EnsureButton(RectTransform parent, string name, string label)
    {
        var t = parent.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = t.gameObject;
            if (go.GetComponent<Image>()  == null) go.AddComponent<Image>();
            if (go.GetComponent<Button>() == null) go.AddComponent<Button>();
        }

        var img = go.GetComponent<Image>();
        img.color = new Color(0.20f, 0.55f, 0.95f, 1f);

        // Child label
        var labelT = go.transform.Find("Label");
        GameObject labelGo;
        if (labelT == null)
        {
            labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
        }
        else
        {
            labelGo = labelT.gameObject;
        }
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>() ?? labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text      = label;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.fontSize  = 40f;
        labelTmp.color     = Color.white;
        StretchFull(labelTmp.rectTransform);

        return go.GetComponent<Button>();
    }

    static TMP_InputField EnsureInputField(RectTransform parent, string name, string placeholder)
    {
        var t = parent.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = t.gameObject;
            if (go.GetComponent<Image>()          == null) go.AddComponent<Image>();
            if (go.GetComponent<TMP_InputField>() == null) go.AddComponent<TMP_InputField>();
        }

        var bg = go.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 1f);
        bg.type  = Image.Type.Sliced;

        var input = go.GetComponent<TMP_InputField>();

        // Text Area
        var areaT = go.transform.Find("Text Area");
        GameObject areaGo;
        if (areaT == null)
        {
            areaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            areaGo.transform.SetParent(go.transform, false);
        }
        else
        {
            areaGo = areaT.gameObject;
            if (areaGo.GetComponent<RectMask2D>() == null) areaGo.AddComponent<RectMask2D>();
        }
        var areaRt = areaGo.GetComponent<RectTransform>();
        areaRt.anchorMin = new Vector2(0f, 0f);
        areaRt.anchorMax = new Vector2(1f, 1f);
        areaRt.offsetMin = new Vector2(20f, 10f);
        areaRt.offsetMax = new Vector2(-20f, -10f);

        // Placeholder
        var phT = areaGo.transform.Find("Placeholder");
        GameObject phGo;
        if (phT == null)
        {
            phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(areaGo.transform, false);
        }
        else phGo = phT.gameObject;
        var ph = phGo.GetComponent<TextMeshProUGUI>() ?? phGo.AddComponent<TextMeshProUGUI>();
        ph.text      = placeholder;
        ph.color     = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        ph.fontSize  = 40f;
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        StretchFull(ph.rectTransform);

        // Text
        var txtT = areaGo.transform.Find("Text");
        GameObject txtGo;
        if (txtT == null)
        {
            txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(areaGo.transform, false);
        }
        else txtGo = txtT.gameObject;
        var tx = txtGo.GetComponent<TextMeshProUGUI>() ?? txtGo.AddComponent<TextMeshProUGUI>();
        tx.text      = string.Empty;
        tx.color     = Color.black;
        tx.fontSize  = 40f;
        tx.alignment = TextAlignmentOptions.MidlineLeft;
        StretchFull(tx.rectTransform);

        // Wire InputField references
        input.textViewport       = areaRt;
        input.textComponent      = tx;
        input.placeholder        = ph;
        input.characterLimit     = 6;

        return input;
    }

    // ── RectTransform setters ─────────────────────────────────────────────

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
        rt.localScale       = Vector3.one;
    }

    static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.localScale = Vector3.one;
    }

    static void SetCentered(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        rt.localScale = Vector3.one;
    }

    static void SetCentered(RectTransform rt, float y, float w, float h)
        => SetCentered(rt, 0f, y, w, h);

    static void EditorSceneManagerMarkActiveSceneDirty()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }
}
