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
    const string TargetName = "Objects_1_3";
    const string MainScene  = "Assets/Scenes/Main.unity";
    const string UIRootName = "[ UI ]";

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

        var humanShop = EnsureShopChild(target, "Shop_Human", CurrencyType.Gold, "인간 상점");
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
            // 기존 자식 정리 (재생성)
            for (int i = panel.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);
        }
        else
        {
            panel = NewUI(panelName, uiParent);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(540, 720);
            rt.anchoredPosition = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        }

        // Title
        var title = NewUI("Title", panel.transform);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0, 60);
        titleRt.anchoredPosition = Vector2.zero;
        var titleTmp = title.AddComponent<TextMeshProUGUI>();
        titleTmp.text = sourceShop.ShopName;
        titleTmp.fontSize = 32;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;

        // CurrencyHint
        var hint = NewUI("CurrencyHint", panel.transform);
        var hintRt = hint.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0, 1);
        hintRt.anchorMax = new Vector2(1, 1);
        hintRt.pivot = new Vector2(0.5f, 1f);
        hintRt.sizeDelta = new Vector2(0, 40);
        hintRt.anchoredPosition = new Vector2(0, -62);
        var hintTmp = hint.AddComponent<TextMeshProUGUI>();
        hintTmp.text = "보유 : --";
        hintTmp.fontSize = 22;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = new Color(1f, 0.85f, 0.4f);

        // Content
        var content = NewUI("ItemListContent", panel.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 0);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.offsetMin = new Vector2(20, 80);
        contentRt.offsetMax = new Vector2(-20, -110);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;

        // Close
        var close = NewUI("CloseButton", panel.transform);
        var closeRt = close.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 1);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.pivot = new Vector2(1, 1);
        closeRt.sizeDelta = new Vector2(60, 60);
        closeRt.anchoredPosition = new Vector2(-8, -8);
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
        closeLabelTmp.fontSize = 28;
        closeLabelTmp.alignment = TextAlignmentOptions.Center;
        closeLabelTmp.color = Color.white;

        // ShopUI 컴포넌트
        var ui = panel.GetComponent<ShopUI>() ?? panel.AddComponent<ShopUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("shop").objectReferenceValue            = sourceShop;
        so.FindProperty("itemListContent").objectReferenceValue = content.transform;
        so.FindProperty("itemRowPrefab").objectReferenceValue   = rowPrefab;
        so.FindProperty("titleText").objectReferenceValue       = titleTmp;
        so.FindProperty("currencyHintText").objectReferenceValue = hintTmp;
        so.FindProperty("closeButton").objectReferenceValue     = closeBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false); // 비활성으로 시작 — 부트스트랩이 Initialize() 호출
        return ui;
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

        var existing = AssetDatabase.LoadAssetAtPath<ShopItemRow>(path);
        if (existing != null) return existing;

        var row = NewUI("ShopItemRow", null);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 64);
        var bg = row.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.spacing = 8;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        var icon = NewUI("Icon", row.transform);
        var iconLE = icon.AddComponent<LayoutElement>();
        iconLE.preferredWidth = 56; iconLE.preferredHeight = 56;
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = Color.white;

        var name = NewUI("Name", row.transform);
        var nameLE = name.AddComponent<LayoutElement>();
        nameLE.flexibleWidth = 1;
        var nameTmp = name.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "이름";
        nameTmp.fontSize = 22;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var price = NewUI("Price", row.transform);
        var priceLE = price.AddComponent<LayoutElement>();
        priceLE.preferredWidth = 110;
        var priceTmp = price.AddComponent<TextMeshProUGUI>();
        priceTmp.text = "0 G";
        priceTmp.fontSize = 22;
        priceTmp.color = new Color(1f, 0.85f, 0.4f);
        priceTmp.alignment = TextAlignmentOptions.MidlineRight;

        var buy = NewUI("BuyButton", row.transform);
        var buyLE = buy.AddComponent<LayoutElement>();
        buyLE.preferredWidth = 90;
        var buyImg = buy.AddComponent<Image>();
        buyImg.color = new Color(0.2f, 0.45f, 0.25f, 1f);
        var buyBtn = buy.AddComponent<Button>();
        var buyLabel = NewUI("Label", buy.transform);
        var buyLabelRt = buyLabel.GetComponent<RectTransform>();
        buyLabelRt.anchorMin = Vector2.zero;
        buyLabelRt.anchorMax = Vector2.one;
        buyLabelRt.offsetMin = Vector2.zero;
        buyLabelRt.offsetMax = Vector2.zero;
        var buyLabelTmp = buyLabel.AddComponent<TextMeshProUGUI>();
        buyLabelTmp.text = "구매";
        buyLabelTmp.fontSize = 20;
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
