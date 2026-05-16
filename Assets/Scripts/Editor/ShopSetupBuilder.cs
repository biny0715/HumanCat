using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main 씬에 다음을 자동 구축:
///   1. Indoor/Furniture/Objects_1_3 에 BoxCollider2D + ShopTrigger + 자식 Shop_Human/Shop_Cat
///   2. [ UI ] 하위에 CatShop / HumanShop 두 ShopUI 패널 (비활성 상태)
///   3. [ UI ] 하위에 ShopUIBootstrap (게임 시작 시 두 패널의 행을 한 번에 사전 생성)
///   4. ShopItemRow 프리팹 (없으면 생성)
///
/// 메뉴: HumanCat → Shop → Setup Objects_1_3 Shop (Main scene)
/// </summary>
public static class ShopSetupBuilder
{
    const string TargetName    = "Objects_1_3";
    const string MainScene     = "Assets/Scenes/Main.unity";
    const string UIRootName    = "[ UI ]";
    const string FontBoldPath  = "Assets/Art/Fonts/Maplestory OTF Bold SDF.asset";
    const string FontLightPath = "Assets/Art/Fonts/Maplestory OTF Light SDF.asset";
    const string RowPrefabPath = "Assets/Prefabs/UI/ShopItemRow.prefab";

    [MenuItem("HumanCat/Shop/Setup Objects_1_3 Shop (Main scene)")]
    public static void SetupShopOnTarget()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        var target = FindInSceneIncludingInactive(TargetName);
        if (target == null)
        {
            Debug.LogError($"[ShopSetupBuilder] '{TargetName}' 를 찾을 수 없음.");
            return;
        }

        // ── Indoor 트리거/Shop_Human/Shop_Cat ────────────────────────────
        var col = target.GetComponent<Collider2D>();
        if (col == null)
        {
            var box = target.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2f, 2f);
            box.isTrigger = true;
            col = box;
        }
        else col.isTrigger = true;

        var humanShop = EnsureShopChild(target, "Shop_Human", CurrencyType.Gold, "일반 상점");
        var catShop   = EnsureShopChild(target, "Shop_Cat",   CurrencyType.Fish, "고양이 상점");

        var trigger = target.GetComponent<ShopTrigger>() ?? target.AddComponent<ShopTrigger>();
        var tso = new SerializedObject(trigger);
        tso.FindProperty("humanShop").objectReferenceValue = humanShop;
        tso.FindProperty("catShop").objectReferenceValue   = catShop;
        tso.ApplyModifiedPropertiesWithoutUndo();

        // ── 기존 자동 ShopPanel 정리 ─────────────────────────────────────
        var legacy = GameObject.Find("Canvas/ShopPanel");
        if (legacy != null)
        {
            Debug.Log("[ShopSetupBuilder] 기존 Canvas/ShopPanel 삭제");
            Object.DestroyImmediate(legacy);
        }

        // ── [ UI ] 하위 패널 두 개 + 부트스트랩 ──────────────────────────
        var uiRoot = FindInSceneIncludingInactive(UIRootName);
        if (uiRoot == null)
        {
            Debug.LogError($"[ShopSetupBuilder] '{UIRootName}' 를 찾을 수 없음.");
            return;
        }

        var rowPrefab = BuildShopItemRowPrefab();

        var humanPanel = EnsureShopPanel(uiRoot.transform, "HumanShop", humanShop, rowPrefab);
        var catPanel   = EnsureShopPanel(uiRoot.transform, "CatShop",   catShop,   rowPrefab);

        EnsureBootstrap(uiRoot.transform, humanPanel, catPanel);

        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[ShopSetupBuilder] 세팅 완료 — Ctrl+S 로 저장하세요.");
    }

    // ── Shop GameObject (트리거 자식) ────────────────────────────────────

    static Shop EnsureShopChild(GameObject parent, string name, CurrencyType currency, string shopName)
    {
        var existing = parent.transform.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;
        }
        var shop = go.GetComponent<Shop>() ?? go.AddComponent<Shop>();
        var so = new SerializedObject(shop);
        so.FindProperty("acceptedCurrency").enumValueIndex = (int)currency;
        so.FindProperty("shopName").stringValue            = shopName;
        so.ApplyModifiedPropertiesWithoutUndo();
        return shop;
    }

    // ── ShopUI 패널 (UI 자식) ────────────────────────────────────────────

    static ShopUI EnsureShopPanel(Transform uiParent, string panelName, Shop sourceShop, ShopItemRow rowPrefab)
    {
        var existing = uiParent.Find(panelName);
        GameObject panel;
        if (existing != null)
        {
            panel = existing.gameObject;
            // 자동 생성된 자식만 정리 (사용자가 추가한 자식 보존)
            string[] autoNames = { "Title", "CurrencyHint", "ItemListContent", "CloseButton", "Nav" };
            for (int i = panel.transform.childCount - 1; i >= 0; i--)
            {
                var c = panel.transform.GetChild(i);
                foreach (var n in autoNames)
                    if (c.name == n) { Object.DestroyImmediate(c.gameObject); break; }
            }
        }
        else
        {
            panel = NewUI(panelName, uiParent);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720, 1100);   // 모바일 세로 화면용 크게
            rt.anchoredPosition = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        }

        // Title (상단 100px) — 검은색
        var title = NewUI("Title", panel.transform);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0, 100);
        titleRt.anchoredPosition = new Vector2(0, -35);
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text = sourceShop.ShopName;
        titleTmp.fontSize = 56;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.10f, 0.06f, 0.03f); // 진한 갈색에 가까운 검정

        // ItemListContent (행 6개에 맞게 영역 — 행 110*6 + spacing 14*5 = 730)
        var content = NewUI("ItemListContent", panel.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 0);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.offsetMin = new Vector2(48, 130);   // Nav 위 안전 거리
        contentRt.offsetMax = new Vector2(-48, -210); // height ≈ 760 (6행+여유)
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.UpperCenter;   // 위에서부터 정렬 — 적게 남으면 아래 공백
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;            // 높이 강제 확장 안 함
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;            // 높이는 행 LayoutElement 가 결정

        // Close (우상단, 패널 안쪽 여백 확보)
        var close = NewUI("CloseButton", panel.transform);
        var closeRt = close.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 1);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.sizeDelta = new Vector2(70, 70);
        closeRt.anchoredPosition = new Vector2(-60, -45);
        var closeImg = close.AddComponent<Image>();
        closeImg.color = new Color(0.4f, 0.1f, 0.1f, 1f);
        var closeBtn = close.AddComponent<Button>();
        var closeLabel = NewUI("Label", close.transform);
        var closeLabelRt = closeLabel.GetComponent<RectTransform>();
        closeLabelRt.anchorMin = Vector2.zero;
        closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.offsetMin = Vector2.zero;
        closeLabelRt.offsetMax = Vector2.zero;
        var closeLabelTmp = closeLabel.AddComponent<TextMeshProUGUI>();
        closeLabelTmp.text = "X";
        closeLabelTmp.fontSize = 44;
        closeLabelTmp.alignment = TextAlignmentOptions.Center;
        closeLabelTmp.color = Color.white;

        // Nav (하단, Prev / PageIndicator / Next) — 갈색 톤
        var nav = NewUI("Nav", panel.transform);
        var navRt = nav.GetComponent<RectTransform>();
        navRt.anchorMin = new Vector2(0, 0);
        navRt.anchorMax = new Vector2(1, 0);
        navRt.pivot = new Vector2(0.5f, 0);
        navRt.sizeDelta = new Vector2(0, 100);
        navRt.anchoredPosition = new Vector2(0, 20);   // 패널 하단 가까이

        var prevBtn = MakeNavButton(nav.transform, "PrevButton", "◀", new Vector2(0, 0.5f), new Vector2(80, 15));
        var pageInd = NewUI("PageIndicator", nav.transform);
        var pageRt = pageInd.GetComponent<RectTransform>();
        pageRt.anchorMin = new Vector2(0.5f, 0.5f);
        pageRt.anchorMax = new Vector2(0.5f, 0.5f);
        pageRt.pivot = new Vector2(0.5f, 0.5f);
        pageRt.sizeDelta = new Vector2(220, 80);
        pageRt.anchoredPosition = Vector2.zero;
        var pageTmp = pageInd.AddComponent<TextMeshProUGUI>();
        pageTmp.text = "1 / 1";
        pageTmp.fontSize = 36;
        pageTmp.alignment = TextAlignmentOptions.Center;
        pageTmp.color = new Color(0.20f, 0.12f, 0.04f); // 진한 갈색
        var nextBtn = MakeNavButton(nav.transform, "NextButton", "▶", new Vector2(1, 0.5f), new Vector2(-80, 15));

        // ShopUI 컴포넌트 (CurrencyHint 제거 — null 유지)
        var ui = panel.GetComponent<ShopUI>() ?? panel.AddComponent<ShopUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("shop").objectReferenceValue              = sourceShop;
        so.FindProperty("itemListContent").objectReferenceValue   = content.transform;
        so.FindProperty("itemRowPrefab").objectReferenceValue     = rowPrefab;
        so.FindProperty("itemsPerPage").intValue                  = 6;
        so.FindProperty("titleText").objectReferenceValue         = titleTmp;
        so.FindProperty("currencyHintText").objectReferenceValue  = null;
        so.FindProperty("closeButton").objectReferenceValue       = closeBtn;
        so.FindProperty("prevButton").objectReferenceValue        = prevBtn;
        so.FindProperty("nextButton").objectReferenceValue        = nextBtn;
        so.FindProperty("pageIndicatorText").objectReferenceValue = pageTmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return ui;
    }

    static Button MakeNavButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot     = new Vector2(anchor.x, 0.5f);
        rt.sizeDelta = new Vector2(110, 92);
        rt.anchoredPosition = anchoredPos;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.55f, 0.35f, 0.18f, 1f); // 갈색 톤
        var btn = go.AddComponent<Button>();
        var labelGo = NewUI("Label", go.transform);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 40;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return btn;
    }

    // ── Bootstrap GameObject ─────────────────────────────────────────────

    static void EnsureBootstrap(Transform uiParent, ShopUI human, ShopUI cat)
    {
        var existing = uiParent.Find("ShopUIBootstrap");
        GameObject go = existing != null ? existing.gameObject : new GameObject("ShopUIBootstrap");
        if (existing == null)
        {
            go.transform.SetParent(uiParent, false);
            go.AddComponent<RectTransform>();
        }
        var boot = go.GetComponent<ShopUIBootstrap>() ?? go.AddComponent<ShopUIBootstrap>();
        var so = new SerializedObject(boot);
        var prop = so.FindProperty("panels");
        prop.arraySize = 2;
        prop.GetArrayElementAtIndex(0).objectReferenceValue = human;
        prop.GetArrayElementAtIndex(1).objectReferenceValue = cat;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── ShopItemRow 프리팹 ───────────────────────────────────────────────

    static ShopItemRow BuildShopItemRowPrefab()
    {
        const string folder = "Assets/Prefabs/UI";
        const string path   = "Assets/Prefabs/UI/ShopItemRow.prefab";

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        // 기존 프리팹이 있으면 강제 삭제 후 새 크기로 재생성
        var existingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existingAsset != null)
            AssetDatabase.DeleteAsset(path);

        var row = NewUI("ShopItemRow", null);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 110);
        var bg = row.AddComponent<Image>();
        bg.color = new Color(0.82f, 0.68f, 0.46f, 1f); // 패널과 어울리는 따뜻한 베이지/갈색

        // 부모 VerticalLayoutGroup 이 자식 적을 때 늘리지 못하도록 LayoutElement 로 높이 고정
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight       = 110;
        rowLE.preferredHeight = 110;
        rowLE.flexibleHeight  = 0;

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
        nameTmp.color = new Color(0.15f, 0.08f, 0.02f); // 진갈색
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var price = NewUI("Price", row.transform);
        var priceLE = price.AddComponent<LayoutElement>();
        priceLE.preferredWidth = 160;
        var priceTmp = price.AddComponent<TextMeshProUGUI>();
        priceTmp.text = "0 G";
        priceTmp.fontSize = 32;
        priceTmp.color = new Color(0.50f, 0.25f, 0.05f); // 진주황/갈색
        priceTmp.alignment = TextAlignmentOptions.MidlineRight;

        var buy = NewUI("BuyButton", row.transform);
        var buyLE = buy.AddComponent<LayoutElement>();
        buyLE.preferredWidth = 140;
        var buyImg = buy.AddComponent<Image>();
        buyImg.color = new Color(0.55f, 0.35f, 0.18f, 1f); // 갈색 (Nav 와 통일)
        var buyBtn = buy.AddComponent<Button>();
        var buyLabel = NewUI("Label", buy.transform);
        var buyLabelRt = buyLabel.GetComponent<RectTransform>();
        buyLabelRt.anchorMin = Vector2.zero;
        buyLabelRt.anchorMax = Vector2.one;
        buyLabelRt.offsetMin = Vector2.zero;
        buyLabelRt.offsetMax = Vector2.zero;
        var buyLabelTmp = buyLabel.AddComponent<TextMeshProUGUI>();
        buyLabelTmp.text = "구매";
        buyLabelTmp.fontSize = 32;
        buyLabelTmp.color = Color.white;
        buyLabelTmp.alignment = TextAlignmentOptions.Center;

        var sir = row.AddComponent<ShopItemRow>();
        var so = new SerializedObject(sir);
        so.FindProperty("iconImage").objectReferenceValue = iconImg;
        so.FindProperty("nameText").objectReferenceValue  = nameTmp;
        so.FindProperty("priceText").objectReferenceValue = priceTmp;
        so.FindProperty("buyButton").objectReferenceValue = buyBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(row, path);
        Object.DestroyImmediate(row);
        return prefab.GetComponent<ShopItemRow>();
    }

    // ── 폰트 일괄 적용 ────────────────────────────────────────────────────

    [MenuItem("HumanCat/Shop/Apply Maplestory Fonts to ShopUI")]
    public static void ApplyMaplestoryFonts()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        var bold  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBoldPath);
        var light = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLightPath);
        if (bold == null || light == null)
        {
            Debug.LogError("[ShopSetupBuilder] Maplestory 폰트 자산을 찾을 수 없음. 경로 확인.");
            return;
        }

        // 1) 씬의 ShopUI 패널들 (Title=Bold, 나머지=Light)
        int sceneChanged = 0;
        foreach (var ui in Object.FindObjectsByType<ShopUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var t in ui.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = t.name == "Title" ? bold : light;
                EditorUtility.SetDirty(t);
                sceneChanged++;
            }
        }

        // 2) ShopItemRow 프리팹 (Name/Price/Label 모두 Light)
        int prefabChanged = 0;
        var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
        if (rowPrefab != null)
        {
            var contents = PrefabUtility.LoadPrefabContents(RowPrefabPath);
            foreach (var t in contents.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = light;
                prefabChanged++;
            }
            PrefabUtility.SaveAsPrefabAsset(contents, RowPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        EditorSceneManager.MarkSceneDirty(active);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ShopSetupBuilder] 폰트 적용 완료 — 씬 텍스트:{sceneChanged}, ShopItemRow 프리팹 텍스트:{prefabChanged}. Ctrl+S 로 씬 저장하세요.");
    }

    // ── HumanShop → CatShop 미러링 ────────────────────────────────────────

    [MenuItem("HumanCat/Shop/Mirror HumanShop Design to CatShop")]
    public static void MirrorHumanShopToCatShop()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        var uiRoot = FindInSceneIncludingInactive(UIRootName);
        if (uiRoot == null) { Debug.LogError("[ShopSetupBuilder] '[ UI ]' 없음"); return; }

        var human  = uiRoot.transform.Find("HumanShop");
        var oldCat = uiRoot.transform.Find("CatShop");
        if (human == null) { Debug.LogError("[ShopSetupBuilder] HumanShop 없음 — 먼저 Setup 실행 필요"); return; }

        // 1) 기존 CatShop / ShopTrigger 에서 Shop_Cat 참조 확보
        Shop catShop = ResolveShopReference(oldCat, "catShop");
        if (catShop == null)
        {
            Debug.LogError("[ShopSetupBuilder] Shop_Cat 참조를 찾을 수 없음. ShopTrigger 또는 기존 CatShop 확인.");
            return;
        }

        // 2) 기존 CatShop 위치(sibling index) 보존 후 삭제
        int siblingIdx = oldCat != null ? oldCat.GetSiblingIndex() : -1;
        if (oldCat != null) Object.DestroyImmediate(oldCat.gameObject);

        // 3) HumanShop 복제 → CatShop 이름
        var newCatGo = Object.Instantiate(human.gameObject, uiRoot.transform);
        newCatGo.name = "CatShop";
        if (siblingIdx >= 0) newCatGo.transform.SetSiblingIndex(siblingIdx);
        newCatGo.SetActive(false); // 닫힘 상태로 유지

        // 4) ShopUI.shop 재연결 + Title 텍스트 갱신
        var newUi = newCatGo.GetComponent<ShopUI>();
        if (newUi != null)
        {
            var so = new SerializedObject(newUi);
            so.FindProperty("shop").objectReferenceValue = catShop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        var titleTr = newCatGo.transform.Find("Title");
        if (titleTr != null)
        {
            var tmp = titleTr.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = catShop.ShopName;
        }

        // 5) ShopUIBootstrap.panels[0]=HumanShop, [1]=CatShop 으로 재정렬
        RewireBootstrap(uiRoot.transform, human.GetComponent<ShopUI>(), newUi);

        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log($"[ShopSetupBuilder] CatShop 미러링 완료 (shop='{catShop.ShopName}'). Ctrl+S 로 저장하세요.");
    }

    static Shop ResolveShopReference(Transform oldCat, string triggerFieldName)
    {
        if (oldCat != null)
        {
            var ui = oldCat.GetComponent<ShopUI>();
            if (ui != null)
            {
                var so = new SerializedObject(ui);
                var s = so.FindProperty("shop").objectReferenceValue as Shop;
                if (s != null) return s;
            }
        }
        var trig = Object.FindFirstObjectByType<ShopTrigger>();
        if (trig != null)
        {
            var so = new SerializedObject(trig);
            return so.FindProperty(triggerFieldName).objectReferenceValue as Shop;
        }
        return null;
    }

    static void RewireBootstrap(Transform uiParent, ShopUI human, ShopUI cat)
    {
        var bootTr = uiParent.Find("ShopUIBootstrap");
        if (bootTr == null) return;
        var boot = bootTr.GetComponent<ShopUIBootstrap>();
        if (boot == null) return;
        var so = new SerializedObject(boot);
        var prop = so.FindProperty("panels");
        prop.arraySize = 2;
        prop.GetArrayElementAtIndex(0).objectReferenceValue = human;
        prop.GetArrayElementAtIndex(1).objectReferenceValue = cat;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject FindInSceneIncludingInactive(string name)
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
}
