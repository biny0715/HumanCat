using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리 시스템 UI를 Main 씬에 일괄 자동 구축.
///
/// 생성/연결:
///   1. Assets/Prefabs/UI/InventoryItemRow.prefab (없으면 생성, 있으면 덮어쓰기)
///   2. [ UI ]/InventoryPanel + InventoryUI 컴포넌트 (ShopUI 와 동일 톤)
///   3. [ UI ]/SellPopup + SellPopupUI
///   4. [ UI ]/UsePopup  + UsePopupUI
///   5. [ UI ]/InventoryUIBootstrap (InventoryUI.Initialize 호출용)
///   6. [ UI ]/GNB 에 InventoryBtn 추가 + InventoryOpenButton 컴포넌트
///   7. (LoginScene) UIBlocker GameObject 자동 배치
///   8. 모든 TMP_Text 에 Maplestory 폰트 적용
///
/// 메뉴: HumanCat → Inventory → Setup Inventory UI (ALL)
/// </summary>
public static class InventorySetupBuilder
{
    const string MainScene     = "Assets/Scenes/Main.unity";
    const string LoginScene    = "Assets/Scenes/LoginScene.unity";
    const string UIRootName    = "[ UI ]";
    const string GNBName       = "GNB";
    const string FontBoldPath  = "Assets/Art/Fonts/Maplestory OTF Bold SDF.asset";
    const string FontLightPath = "Assets/Art/Fonts/Maplestory OTF Light SDF.asset";
    const string RowPrefabPath = "Assets/Prefabs/UI/InventoryItemRow.prefab";

    // 색상 (Shop UI 와 통일)
    static readonly Color PanelBg     = new Color(0.95f, 0.88f, 0.72f, 1f);
    static readonly Color RowBg       = new Color(0.82f, 0.68f, 0.46f, 1f);
    static readonly Color NavBg       = new Color(0.55f, 0.35f, 0.18f, 1f);
    static readonly Color CloseBg     = new Color(0.4f,  0.1f,  0.1f,  1f);
    static readonly Color ConfirmBg   = new Color(0.20f, 0.45f, 0.25f, 1f);
    static readonly Color CancelBg    = new Color(0.55f, 0.35f, 0.18f, 1f);
    static readonly Color TextDark    = new Color(0.10f, 0.06f, 0.03f);
    static readonly Color TextOnDark  = Color.white;
    static readonly Color PriceColor  = new Color(0.50f, 0.25f, 0.05f);

    [MenuItem("HumanCat/Inventory/Setup Inventory UI (ALL)")]
    public static void SetupAll()
    {
        EnsureMainOpen();

        var uiRoot = FindInActiveScene(UIRootName);
        if (uiRoot == null) { Debug.LogError("[InventorySetupBuilder] '[ UI ]' 없음"); return; }

        var rowPrefab = BuildItemRowPrefab();

        var inventoryUI = BuildInventoryPanel(uiRoot.transform, rowPrefab);
        var sellPopup   = BuildSellPopup(uiRoot.transform);
        var usePopup    = BuildUsePopup(uiRoot.transform);

        WireInventoryPopups(inventoryUI, sellPopup, usePopup);
        BuildInventoryBootstrap(uiRoot.transform, inventoryUI);
        AddInventoryButtonToGNB(uiRoot.transform, inventoryUI);

        ApplyFontsRecursive(uiRoot);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EnsureUIBlockerInLoginScene();

        Debug.Log("[InventorySetupBuilder] 인벤토리 UI 일괄 세팅 완료 — Main / LoginScene 각각 저장 필요");
    }

    // ── InventoryItemRow 프리팹 ──────────────────────────────────────────

    static InventoryItemRow BuildItemRowPrefab()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/UI");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath) != null)
            AssetDatabase.DeleteAsset(RowPrefabPath);

        var row = NewUI("InventoryItemRow", null);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 110);
        var bg = row.AddComponent<Image>();
        bg.color = RowBg;

        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 110; le.preferredHeight = 110; le.flexibleHeight = 0;

        // 행 전체를 클릭하기 위한 Button
        var rowBtn = row.AddComponent<Button>();
        rowBtn.targetGraphic = bg;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 8, 8);
        hlg.spacing = 14;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        var icon = NewUI("Icon", row.transform);
        var iconLE = icon.AddComponent<LayoutElement>();
        iconLE.preferredWidth = 90; iconLE.preferredHeight = 90;
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = Color.white;

        var name = NewUI("Name", row.transform);
        var nameLE = name.AddComponent<LayoutElement>();
        nameLE.flexibleWidth = 1;
        var nameTmp = name.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "이름";
        nameTmp.fontSize = 36;
        nameTmp.color = TextDark;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var count = NewUI("Count", row.transform);
        var countLE = count.AddComponent<LayoutElement>();
        countLE.preferredWidth = 140;
        var countTmp = count.AddComponent<TextMeshProUGUI>();
        countTmp.text = "x0";
        countTmp.fontSize = 32;
        countTmp.color = PriceColor;
        countTmp.alignment = TextAlignmentOptions.MidlineRight;

        var sir = row.AddComponent<InventoryItemRow>();
        var so = new SerializedObject(sir);
        so.FindProperty("iconImage").objectReferenceValue = iconImg;
        so.FindProperty("nameText").objectReferenceValue  = nameTmp;
        so.FindProperty("countText").objectReferenceValue = countTmp;
        so.FindProperty("rowButton").objectReferenceValue = rowBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(row, RowPrefabPath);
        Object.DestroyImmediate(row);
        return prefab.GetComponent<InventoryItemRow>();
    }

    // ── InventoryPanel ──────────────────────────────────────────────────

    static InventoryUI BuildInventoryPanel(Transform uiParent, InventoryItemRow rowPrefab)
    {
        var existing = uiParent.Find("InventoryPanel");
        GameObject panel;
        if (existing != null)
        {
            panel = existing.gameObject;
            for (int i = panel.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);
            // ShopUI 가 붙어있다면 제거 (Duplicate 잔재 방지)
            var oldShop = panel.GetComponent<ShopUI>();
            if (oldShop != null) Object.DestroyImmediate(oldShop);
        }
        else
        {
            panel = NewUI("InventoryPanel", uiParent);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720, 1100);
            rt.anchoredPosition = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = PanelBg;
        }

        // Title
        var title = NewUI("Title", panel.transform);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0, 100);
        titleRt.anchoredPosition = new Vector2(0, -35);
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "인벤토리";
        titleTmp.fontSize = 56;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = TextDark;

        // ItemListContent
        var content = NewUI("ItemListContent", panel.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 0); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.offsetMin = new Vector2(48, 130);
        contentRt.offsetMax = new Vector2(-48, -210);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // CloseButton (우상단)
        var close = NewUI("CloseButton", panel.transform);
        var closeRt = close.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 1); closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.sizeDelta = new Vector2(70, 70);
        closeRt.anchoredPosition = new Vector2(-60, -45);
        var closeImg = close.AddComponent<Image>(); closeImg.color = CloseBg;
        var closeBtn = close.AddComponent<Button>();
        AddCenteredLabel(close.transform, "X", 44, TextOnDark);

        // Nav (Prev / Indicator / Next)
        var nav = NewUI("Nav", panel.transform);
        var navRt = nav.GetComponent<RectTransform>();
        navRt.anchorMin = new Vector2(0, 0); navRt.anchorMax = new Vector2(1, 0);
        navRt.pivot = new Vector2(0.5f, 0);
        navRt.sizeDelta = new Vector2(0, 100);
        navRt.anchoredPosition = new Vector2(0, 20);

        var prevBtn = MakeNavButton(nav.transform, "PrevButton", "◀", new Vector2(0, 0.5f), new Vector2(80, 15));
        var pageInd = NewUI("PageIndicator", nav.transform);
        var pageRt = pageInd.GetComponent<RectTransform>();
        pageRt.anchorMin = new Vector2(0.5f, 0.5f); pageRt.anchorMax = new Vector2(0.5f, 0.5f);
        pageRt.pivot = new Vector2(0.5f, 0.5f);
        pageRt.sizeDelta = new Vector2(220, 80);
        var pageTmp = pageInd.AddComponent<TextMeshProUGUI>();
        pageTmp.text = "1 / 1";
        pageTmp.fontSize = 36;
        pageTmp.alignment = TextAlignmentOptions.Center;
        pageTmp.color = TextDark;
        var nextBtn = MakeNavButton(nav.transform, "NextButton", "▶", new Vector2(1, 0.5f), new Vector2(-80, 15));

        // InventoryUI 컴포넌트
        var ui = panel.GetComponent<InventoryUI>() ?? panel.AddComponent<InventoryUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("itemListContent").objectReferenceValue   = content.transform;
        so.FindProperty("itemRowPrefab").objectReferenceValue     = rowPrefab;
        so.FindProperty("itemsPerPage").intValue                  = 6;
        so.FindProperty("titleText").objectReferenceValue         = titleTmp;
        so.FindProperty("pageIndicatorText").objectReferenceValue = pageTmp;
        so.FindProperty("closeButton").objectReferenceValue       = closeBtn;
        so.FindProperty("prevButton").objectReferenceValue        = prevBtn;
        so.FindProperty("nextButton").objectReferenceValue        = nextBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return ui;
    }

    // ── SellPopup ───────────────────────────────────────────────────────

    static SellPopupUI BuildSellPopup(Transform uiParent)
    {
        var go = ReplaceOrCreate(uiParent, "SellPopup");
        SetCenter(go, new Vector2(560, 420));
        var bg = go.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0.6f); // 반투명 어두운 오버레이

        var panel = NewUI("Panel", go.transform);
        SetCenter(panel, new Vector2(520, 380));
        var panelImg = panel.AddComponent<Image>(); panelImg.color = PanelBg;

        var title = AddText(panel.transform, "Title", "아이템", 44, TextDark, TextAlignmentOptions.Center,
            new Vector2(0, 1), new Vector2(0.5f, 1f), new Vector2(0, 80), new Vector2(0, -30));

        AddText(panel.transform, "Content", "판매 하시겠습니까?", 32, TextDark, TextAlignmentOptions.Center,
            new Vector2(0, 1), new Vector2(0.5f, 1f), new Vector2(0, 60), new Vector2(0, -120));

        var price = AddText(panel.transform, "Price", "0 골드", 36, PriceColor, TextAlignmentOptions.Center,
            new Vector2(0, 1), new Vector2(0.5f, 1f), new Vector2(0, 60), new Vector2(0, -190));

        var confirm = MakePopupButton(panel.transform, "ConfirmButton", "확인", ConfirmBg,
            new Vector2(0.3f, 0), new Vector2(160, 80), new Vector2(0, 40));
        var cancel  = MakePopupButton(panel.transform, "CancelButton",  "취소", CancelBg,
            new Vector2(0.7f, 0), new Vector2(160, 80), new Vector2(0, 40));

        var ui = go.GetComponent<SellPopupUI>() ?? go.AddComponent<SellPopupUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue         = go;
        so.FindProperty("titleText").objectReferenceValue     = title;
        so.FindProperty("priceText").objectReferenceValue     = price;
        so.FindProperty("confirmButton").objectReferenceValue = confirm;
        so.FindProperty("cancelButton").objectReferenceValue  = cancel;
        so.ApplyModifiedPropertiesWithoutUndo();

        go.SetActive(false);
        return ui;
    }

    // ── UsePopup ────────────────────────────────────────────────────────

    static UsePopupUI BuildUsePopup(Transform uiParent)
    {
        var go = ReplaceOrCreate(uiParent, "UsePopup");
        SetCenter(go, new Vector2(560, 420));
        var bg = go.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0.6f);

        var panel = NewUI("Panel", go.transform);
        SetCenter(panel, new Vector2(520, 380));
        var panelImg = panel.AddComponent<Image>(); panelImg.color = PanelBg;

        var title = AddText(panel.transform, "Title", "아이템 사용하기", 40, TextDark, TextAlignmentOptions.Center,
            new Vector2(0, 1), new Vector2(0.5f, 1f), new Vector2(0, 80), new Vector2(0, -30));

        var desc = AddText(panel.transform, "Desc", "사용 하시겠습니까?", 30, TextDark, TextAlignmentOptions.Center,
            new Vector2(0, 1), new Vector2(0.5f, 1f), new Vector2(0, 120), new Vector2(0, -150));

        var use    = MakePopupButton(panel.transform, "UseButton",    "사용", ConfirmBg,
            new Vector2(0.3f, 0), new Vector2(160, 80), new Vector2(0, 40));
        var cancel = MakePopupButton(panel.transform, "CancelButton", "취소", CancelBg,
            new Vector2(0.7f, 0), new Vector2(160, 80), new Vector2(0, 40));

        var ui = go.GetComponent<UsePopupUI>() ?? go.AddComponent<UsePopupUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue        = go;
        so.FindProperty("titleText").objectReferenceValue    = title;
        so.FindProperty("descText").objectReferenceValue     = desc;
        so.FindProperty("useButton").objectReferenceValue    = use;
        so.FindProperty("cancelButton").objectReferenceValue = cancel;
        so.ApplyModifiedPropertiesWithoutUndo();

        go.SetActive(false);
        return ui;
    }

    static void WireInventoryPopups(InventoryUI inv, SellPopupUI sell, UsePopupUI use)
    {
        var so = new SerializedObject(inv);
        so.FindProperty("sellPopup").objectReferenceValue = sell;
        so.FindProperty("usePopup").objectReferenceValue  = use;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Bootstrap ───────────────────────────────────────────────────────

    static void BuildInventoryBootstrap(Transform uiParent, InventoryUI panel)
    {
        var existing = uiParent.Find("InventoryUIBootstrap");
        GameObject go = existing != null ? existing.gameObject : new GameObject("InventoryUIBootstrap");
        if (existing == null)
        {
            go.transform.SetParent(uiParent, false);
            go.AddComponent<RectTransform>();
        }
        var boot = go.GetComponent<InventoryUIBootstrap>() ?? go.AddComponent<InventoryUIBootstrap>();
        var so = new SerializedObject(boot);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── GNB 인벤토리 버튼 ────────────────────────────────────────────────

    static void AddInventoryButtonToGNB(Transform uiParent, InventoryUI inv)
    {
        var gnb = uiParent.Find(GNBName);
        if (gnb == null)
        {
            Debug.LogWarning("[InventorySetupBuilder] GNB 못 찾음 — 인벤토리 버튼은 수동 배치 필요");
            return;
        }
        var existing = gnb.Find("InventoryBtn");
        GameObject btnGo = existing != null ? existing.gameObject : NewUI("InventoryBtn", gnb);

        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot     = new Vector2(1, 0.5f);
        rt.sizeDelta = new Vector2(100, 100);
        rt.anchoredPosition = new Vector2(-160, 0); // Quit_Btn 좌측에 배치 (조정 가능)

        var img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
        img.color = NavBg;
        var btn = btnGo.GetComponent<Button>() ?? btnGo.AddComponent<Button>();

        // 라벨
        Transform labelTr = btnGo.transform.Find("Label");
        if (labelTr == null)
        {
            var label = NewUI("Label", btnGo.transform);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = "가방"; tmp.fontSize = 32; tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = TextOnDark;
        }

        var opener = btnGo.GetComponent<InventoryOpenButton>() ?? btnGo.AddComponent<InventoryOpenButton>();
        var so = new SerializedObject(opener);
        so.FindProperty("target").objectReferenceValue = inv;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── GNB 아이콘 버튼 (Inventory 좌하단 / Shop 우하단) ─────────────────

    const string InventoryIconPath = "Assets/Art/UI/InventoryIcon.png";
    const string ShopIconPath      = "Assets/Art/UI/ShopIcon.png";
    const string ShopTriggerObject = "Objects_1_3";

    [MenuItem("HumanCat/Inventory/Setup GNB Icons (Inventory + Shop)")]
    public static void SetupGnbIcons()
    {
        EnsureMainOpen();
        var uiRoot = FindInActiveScene(UIRootName);
        if (uiRoot == null) { Debug.LogError("[InventorySetupBuilder] '[ UI ]' 없음"); return; }
        var gnb = uiRoot.transform.Find(GNBName);
        if (gnb == null) { Debug.LogError("[InventorySetupBuilder] GNB 없음"); return; }

        var inventoryUI = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        var shopTrigger = FindShopTrigger();
        if (inventoryUI == null) Debug.LogWarning("[InventorySetupBuilder] InventoryUI 없음 — 인벤토리 버튼 target 비어둠");
        if (shopTrigger == null) Debug.LogWarning($"[InventorySetupBuilder] '{ShopTriggerObject}' 의 ShopTrigger 없음 — 상점 버튼 target 비어둠");

        BuildGnbIconButton(gnb, "InventoryBtn", InventoryIconPath,
            new Vector2(0, 0), new Vector2(60, 60),
            attach: go => WireInventoryButton(go, inventoryUI));

        BuildGnbIconButton(gnb, "ShopBtn", ShopIconPath,
            new Vector2(1, 0), new Vector2(-60, 60),
            attach: go => WireShopButton(go, shopTrigger));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[InventorySetupBuilder] GNB 아이콘 버튼(인벤토리/상점) 배치 완료 — Ctrl+S 로 저장");
    }

    static void BuildGnbIconButton(Transform gnb, string name, string spritePath,
                                   Vector2 anchor, Vector2 anchoredPos,
                                   System.Action<GameObject> attach)
    {
        // 기존 동일 이름 정리
        var existing = gnb.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var go = NewUI(name, gnb);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot     = new Vector2(anchor.x, 0);
        rt.sizeDelta = new Vector2(96, 96);
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null) img.sprite = sprite;
        else Debug.LogWarning($"[InventorySetupBuilder] 스프라이트 없음: {spritePath}");
        img.preserveAspect = true;

        go.AddComponent<Button>();
        attach?.Invoke(go);
    }

    static void WireInventoryButton(GameObject go, InventoryUI inventoryUI)
    {
        var opener = go.AddComponent<InventoryOpenButton>();
        var so = new SerializedObject(opener);
        so.FindProperty("target").objectReferenceValue = inventoryUI;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireShopButton(GameObject go, ShopTrigger trigger)
    {
        var opener = go.AddComponent<ShopOpenButton>();
        var so = new SerializedObject(opener);
        so.FindProperty("target").objectReferenceValue = trigger;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static ShopTrigger FindShopTrigger()
    {
        var go = FindInActiveScene(ShopTriggerObject);
        return go != null ? go.GetComponent<ShopTrigger>() : null;
    }

    // ── UIBlocker (LoginScene 자동 배치) ─────────────────────────────────

    static void EnsureUIBlockerInLoginScene()
    {
        var current = EditorSceneManager.GetActiveScene();
        var login = EditorSceneManager.OpenScene(LoginScene, OpenSceneMode.Additive);
        try
        {
            bool found = false;
            foreach (var root in login.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<UIBlocker>(true) != null) { found = true; break; }
            }
            if (!found)
            {
                var go = new GameObject("UIBlocker");
                SceneManager_MoveToScene(go, login);
                go.AddComponent<UIBlocker>();
                EditorSceneManager.MarkSceneDirty(login);
                EditorSceneManager.SaveScene(login);
                Debug.Log("[InventorySetupBuilder] UIBlocker 를 LoginScene 에 자동 배치 + 저장");
            }
            else
            {
                Debug.Log("[InventorySetupBuilder] LoginScene 에 이미 UIBlocker 존재 — 스킵");
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(login, true);
            if (current.IsValid()) EditorSceneManager.SetActiveScene(current);
        }
    }

    static void SceneManager_MoveToScene(GameObject go, UnityEngine.SceneManagement.Scene scene)
    {
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    }

    // ── 폰트 일괄 적용 ────────────────────────────────────────────────────

    static void ApplyFontsRecursive(GameObject root)
    {
        var bold  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBoldPath);
        var light = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLightPath);
        if (bold == null || light == null)
        {
            Debug.LogWarning("[InventorySetupBuilder] Maplestory 폰트 못 찾음 — 폰트 적용 스킵");
            return;
        }

        // InventoryPanel / Popups 의 TMP_Text 적용
        foreach (var target in new[] { "InventoryPanel", "SellPopup", "UsePopup" })
        {
            var t = root.transform.Find(target);
            if (t == null) continue;
            foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                tmp.font = tmp.name == "Title" ? bold : light;
        }
        // 새로 추가한 GNB 인벤토리 버튼의 라벨도 적용
        var gnb = root.transform.Find(GNBName);
        if (gnb != null)
        {
            var btnTr = gnb.Find("InventoryBtn");
            if (btnTr != null)
                foreach (var tmp in btnTr.GetComponentsInChildren<TMP_Text>(true))
                    tmp.font = bold;
        }
        // 행 프리팹도 패치
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
        if (prefab != null)
        {
            var contents = PrefabUtility.LoadPrefabContents(RowPrefabPath);
            foreach (var tmp in contents.GetComponentsInChildren<TMP_Text>(true))
                tmp.font = light;
            PrefabUtility.SaveAsPrefabAsset(contents, RowPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }
        AssetDatabase.SaveAssets();
    }

    // ── 작은 헬퍼들 ──────────────────────────────────────────────────────

    static void EnsureMainOpen()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject ReplaceOrCreate(Transform parent, string name)
    {
        // 기존이 있으면 통째로 삭제 후 새로 만든다 (컴포넌트 충돌 방지).
        var existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        return NewUI(name, parent);
    }

    static void SetCenter(GameObject go, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    static TMP_Text AddText(Transform parent, string name, string text, int size, Color color,
        TextAlignmentOptions align, Vector2 anchorMin, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = new Vector2(1, anchorMin.y);
        rt.pivot     = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.alignment = align;
        return tmp;
    }

    static Button MakePopupButton(Transform parent, string name, string label, Color color,
        Vector2 anchor, Vector2 size, Vector2 anchoredPos)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var img = go.AddComponent<Image>(); img.color = color;
        var btn = go.AddComponent<Button>();
        AddCenteredLabel(go.transform, label, 32, TextOnDark);
        return btn;
    }

    static Button MakeNavButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot     = new Vector2(anchor.x, 0.5f);
        rt.sizeDelta = new Vector2(110, 92);
        rt.anchoredPosition = anchoredPos;
        var img = go.AddComponent<Image>(); img.color = NavBg;
        var btn = go.AddComponent<Button>();
        AddCenteredLabel(go.transform, label, 40, TextOnDark);
        return btn;
    }

    static void AddCenteredLabel(Transform parent, string text, int fontSize, Color color)
    {
        var go = NewUI("Label", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    static GameObject FindInActiveScene(string name)
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var hit = FindByNameRecursive(root.transform, name);
            if (hit != null) return hit.gameObject;
        }
        return null;
    }

    static Transform FindByNameRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var hit = FindByNameRecursive(parent.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int last = path.LastIndexOf('/');
        if (last < 0) return;
        EnsureFolder(path.Substring(0, last));
        AssetDatabase.CreateFolder(path.Substring(0, last), path.Substring(last + 1));
    }
}
