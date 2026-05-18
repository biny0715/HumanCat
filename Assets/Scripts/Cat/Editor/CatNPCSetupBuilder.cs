using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CatNPC.prefab 자동 생성기.
///
/// 메뉴: HumanCat → Cat NPC → Build CatNPC Prefab
///
/// 결과 prefab 구성:
///   - SpriteRenderer (default: CheeseCat_Idle_0)
///   - Animator (controller: CheeseCatController)
///   - Rigidbody2D (Kinematic, gravityScale=0)
///   - CircleCollider2D (isTrigger=false)
///   - CatNPCController
///       · floorLayer = "Floor"
///       · ref 슬롯(spriteRenderer/animator/rb) 자동 연결
///
/// 사용자는 prefab 인스턴스를 [ Environment ]/Indoor 아래에 배치하면 됨.
/// SleepCat 외형을 원하면 Animator.controller 와 SpriteRenderer.sprite 만 인스펙터에서 교체.
/// </summary>
public static class CatNPCSetupBuilder
{
    const string PrefabFolder = "Assets/Prefabs/CatNPC";
    const string PrefabPath   = "Assets/Prefabs/CatNPC/CatNPC.prefab";

    const string DefaultControllerPath = "Assets/Animations/CheeseCatController.controller";
    const string DefaultSpriteSheetPath = "Assets/Art/Cat/CheeseCat/CheeseCat_Idle.png";
    const string DefaultSpriteName      = "CheeseCat_Idle_0";

    [MenuItem("HumanCat/Cat NPC/Build CatNPC Prefab")]
    public static void BuildCatNPCPrefab()
    {
        // 1) 폴더 보장
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "CatNPC");

        // 2) 임시 GameObject 구성
        var go = new GameObject("CatNPC");
        var sr = go.AddComponent<SpriteRenderer>();
        // YSort 가 같은 SortingLayer 안에서만 Y 정렬 보장 — Player/가구와 동일한 'Object' 사용
        sr.sortingLayerName = "Object";

        var anim = go.AddComponent<Animator>();
        var rb   = go.AddComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.gravityScale   = 0f;
        rb.constraints    = RigidbodyConstraints2D.FreezeRotation;

        var col           = go.AddComponent<CircleCollider2D>();
        col.radius        = 0.3f;
        // isTrigger=true — Player/Cat 캐릭터끼리 collider 충돌로 이동 방해되지 않도록.
        // 클릭 감지(OverlapPoint) 는 trigger 와 무관하게 작동.
        col.isTrigger     = true;

        var ctrl = go.AddComponent<CatNPCController>();
        go.AddComponent<CatNPC>(); // 식별 + 클릭 감지 (CatNPCController 와 독립)
        go.AddComponent<YSort>();  // Y 위치 기반 정렬 — Object SortingLayer 안에서 가구/Player 와 자동 정렬

        // 3) 기본 컨트롤러 / sprite 연결
        var defaultController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultControllerPath);
        if (defaultController != null) anim.runtimeAnimatorController = defaultController;
        else Debug.LogWarning($"[CatNPCSetupBuilder] {DefaultControllerPath} 없음 — 인스펙터에서 수동 연결 필요");

        var defaultSprite = LoadSpriteFromSheet(DefaultSpriteSheetPath, DefaultSpriteName);
        if (defaultSprite != null) sr.sprite = defaultSprite;
        else Debug.LogWarning($"[CatNPCSetupBuilder] sprite '{DefaultSpriteName}' 없음 — 인스펙터에서 수동 연결 필요");

        // 4) CatNPCController 슬롯 자동 연결
        var so = new SerializedObject(ctrl);
        int floorLayer = LayerMask.NameToLayer("Floor");
        if (floorLayer >= 0)
            so.FindProperty("floorLayer").intValue = 1 << floorLayer;
        else
            Debug.LogWarning("[CatNPCSetupBuilder] Layer 'Floor' 없음 — Placement Layers 메뉴 실행 권장");

        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("animator").objectReferenceValue       = anim;
        so.FindProperty("rb").objectReferenceValue             = rb;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 5) Prefab 저장 + 임시 인스턴스 파괴
        var saved = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        if (saved != null)
        {
            EditorGUIUtility.PingObject(saved);
            Selection.activeObject = saved;
            Debug.Log($"[CatNPCSetupBuilder] CatNPC.prefab 생성 완료 — {PrefabPath}");
        }
        else
        {
            Debug.LogError("[CatNPCSetupBuilder] Prefab 저장 실패");
        }
    }

    // ── 3종 Cat Variant prefab + CatItemData asset 자동 생성 ───────────

    struct CatVariantDef
    {
        public string     name;          // "BlackCat" 등
        public string     controllerPath;
        public string     sheetPath;
        public string     spriteName;    // 첫 frame 이름
        public string     displayName;   // ItemData displayName
        public string     itemId;        // ItemData itemId
    }

    static readonly CatVariantDef[] Variants =
    {
        new CatVariantDef {
            name           = "BlackCat",
            controllerPath = "Assets/Animations/BlackCatController.controller",
            sheetPath      = "Assets/Art/Cat/BlackCat/BlackCat_Idle.png",
            spriteName     = "BlackCat_Idle_0",
            displayName    = "검은 고양이",
            itemId         = "cat_black",
        },
        new CatVariantDef {
            name           = "CheeseCat",
            controllerPath = "Assets/Animations/CheeseCatController.controller",
            sheetPath      = "Assets/Art/Cat/CheeseCat/CheeseCat_Idle.png",
            spriteName     = "CheeseCat_Idle_0",
            displayName    = "치즈 고양이",
            itemId         = "cat_cheese",
        },
        new CatVariantDef {
            name           = "SleepCat",
            controllerPath = "Assets/Animations/SleepCatController.controller",
            sheetPath      = "Assets/Art/Cat/SleepCat/SleepCat_Idle.png",
            spriteName     = "SleepCat_Idle_0",
            displayName    = "잠자는 고양이",
            itemId         = "cat_sleep",
        },
    };

    const int DefaultCatFishPrice = 5;

    [MenuItem("HumanCat/Cat NPC/Build Cat Variants (prefab + CatItemData)")]
    public static void BuildCatVariants()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            Debug.LogError($"[CatNPCSetupBuilder] {PrefabFolder} 없음 — Build CatNPC Prefab 먼저 실행");
            return;
        }
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (basePrefab == null)
        {
            Debug.LogError($"[CatNPCSetupBuilder] base prefab {PrefabPath} 없음");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/CatItems"))
            AssetDatabase.CreateFolder("Assets/Resources", "CatItems");

        foreach (var def in Variants)
        {
            // 1) prefab variant 만들기 — base 인스턴스화 후 controller/sprite 교체 → 별도 prefab 으로 저장
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = def.name;

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(def.controllerPath);
            var anim       = instance.GetComponent<Animator>();
            if (anim != null && controller != null) anim.runtimeAnimatorController = controller;
            else Debug.LogWarning($"[CatNPCSetupBuilder] {def.name}: controller 못 찾음 ({def.controllerPath})");

            var sprite = LoadSpriteFromSheet(def.sheetPath, def.spriteName);
            var sr     = instance.GetComponent<SpriteRenderer>();
            if (sr != null && sprite != null) sr.sprite = sprite;
            else Debug.LogWarning($"[CatNPCSetupBuilder] {def.name}: sprite 못 찾음 ({def.spriteName})");

            string prefabPath = $"{PrefabFolder}/{def.name}.prefab";
            var prefab        = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            if (prefab == null)
            {
                Debug.LogError($"[CatNPCSetupBuilder] {def.name} prefab 저장 실패");
                continue;
            }

            // 2) CatItemData asset 만들기 — fishPrice=5 고정
            string assetPath = $"Assets/Resources/CatItems/Cat_{def.name}.asset";
            var data         = AssetDatabase.LoadAssetAtPath<CatItemData>(assetPath);
            bool created     = false;
            if (data == null)
            {
                data    = ScriptableObject.CreateInstance<CatItemData>();
                created = true;
            }
            var so = new SerializedObject(data);
            so.FindProperty("itemId").stringValue                  = def.itemId;
            so.FindProperty("displayName").stringValue             = def.displayName;
            so.FindProperty("fishPrice").intValue                  = DefaultCatFishPrice;
            so.FindProperty("catPrefab").objectReferenceValue      = prefab;
            so.FindProperty("isCat").boolValue                     = true;
            // icon — 첫 frame sprite 로 default 연결 (나중에 사용자가 인스펙터에서 변경 가능)
            if (sprite != null)
                so.FindProperty("icon").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (created)
            {
                AssetDatabase.CreateAsset(data, assetPath);
                Debug.Log($"[CatNPCSetupBuilder] {assetPath} 생성 (fishPrice={DefaultCatFishPrice})");
            }
            else
            {
                EditorUtility.SetDirty(data);
                Debug.Log($"[CatNPCSetupBuilder] {assetPath} 갱신");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CatNPCSetupBuilder] Cat Variants 3종 생성 완료");
    }

    // ── CatManager + Cats 부모 + Resources/CatItems 폴더 자동 셋업 ─────

    const string MainScene = "Assets/Scenes/Main.unity";

    [MenuItem("HumanCat/Cat NPC/Setup CatManager (Main scene)")]
    public static void SetupCatManagerInMain()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        // 1) Resources/CatItems 폴더 보장
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/CatItems"))
            AssetDatabase.CreateFolder("Assets/Resources", "CatItems");

        // 2) Indoor 아래 'Cats' 부모 GameObject 보장 — Indoor 토글에 자동 따라감
        var indoor = FindInActiveScene("Indoor");
        if (indoor == null)
        {
            Debug.LogError("[CatNPCSetupBuilder] 'Indoor' 없음 — [ Environment ]/Indoor 확인");
            return;
        }
        var catsParent = indoor.transform.Find("Cats")?.gameObject;
        if (catsParent == null)
        {
            catsParent = new GameObject("Cats");
            catsParent.transform.SetParent(indoor.transform, false);
        }

        // 3) [ Managers ] 아래 CatManager
        var managersHost = FindInActiveScene("[ Managers ]");
        if (managersHost == null)
        {
            Debug.LogError("[CatNPCSetupBuilder] '[ Managers ]' 없음");
            return;
        }
        var cmGo = managersHost.transform.Find("CatManager")?.gameObject;
        if (cmGo == null)
        {
            cmGo = new GameObject("CatManager");
            cmGo.transform.SetParent(managersHost.transform, false);
        }
        var cm = cmGo.GetComponent<CatManager>() ?? cmGo.AddComponent<CatManager>();

        var so = new SerializedObject(cm);
        so.FindProperty("catRoot").objectReferenceValue = catsParent.transform;
        int floor = LayerMask.NameToLayer("Floor");
        if (floor >= 0) so.FindProperty("floorMask").intValue = 1 << floor;
        else Debug.LogWarning("[CatNPCSetupBuilder] Layer 'Floor' 없음 — Add Placement Layers 메뉴 실행 권장");
        so.ApplyModifiedPropertiesWithoutUndo();

        // CatNPCClickDispatcher — InputReader 가 NPC 위 클릭을 위임할 더블클릭 처리기
        if (cmGo.GetComponent<CatNPCClickDispatcher>() == null)
            cmGo.AddComponent<CatNPCClickDispatcher>();

        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[CatNPCSetupBuilder] CatManager + Cats 부모 + Resources/CatItems + ClickDispatcher 셋업 완료 — Ctrl+S 로 저장");
    }

    // ── Cat Remove Popup UI 자동 생성 ──────────────────────────────────

    [MenuItem("HumanCat/Cat NPC/Setup CatRemovePopup UI (Main scene)")]
    public static void SetupCatRemovePopupUI()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        var uiRoot = FindInActiveScene("[ UI ]");
        if (uiRoot == null)
        {
            Debug.LogError("[CatNPCSetupBuilder] '[ UI ]' 없음");
            return;
        }

        // 기존 정리
        var existing = uiRoot.transform.Find("CatRemovePopup");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Root (Backdrop) — 검은 반투명 + 클릭 차단
        var root = NewUI("CatRemovePopup", uiRoot.transform);
        SetCenter(root.GetComponent<RectTransform>(), new Vector2(560, 420));
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0, 0, 0, 0.6f);

        // Panel — 컨텐츠
        var panel = NewUI("Panel", root.transform);
        SetCenter(panel.GetComponent<RectTransform>(), new Vector2(520, 380));
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(1f, 0.97f, 0.93f, 1f);

        var title  = AddText(panel.transform, "Title", "고양이를 내보내시겠습니까?", 40,
            new Color(0.2f, 0.15f, 0.1f), new Vector2(0, 1), new Vector2(0.5f, 1f),
            new Vector2(0, 80), new Vector2(0, -30));
        var desc   = AddText(panel.transform, "Desc", "", 32,
            new Color(0.3f, 0.25f, 0.2f), new Vector2(0, 1), new Vector2(0.5f, 1f),
            new Vector2(0, 50), new Vector2(0, -130));
        var price  = AddText(panel.transform, "SellPrice", "0 Fish", 36,
            new Color(0.5f, 0.3f, 0.15f), new Vector2(0, 1), new Vector2(0.5f, 1f),
            new Vector2(0, 60), new Vector2(0, -200));

        var confirm = MakeFlatButton(panel.transform, "ConfirmButton", "확인",
            new Color(0.55f, 0.20f, 0.20f, 1f),
            new Vector2(0.3f, 0), new Vector2(160, 80), new Vector2(0, 40));
        var cancel  = MakeFlatButton(panel.transform, "CancelButton", "취소",
            new Color(0.40f, 0.40f, 0.40f, 1f),
            new Vector2(0.7f, 0), new Vector2(160, 80), new Vector2(0, 40));

        var ui = root.AddComponent<CatRemovePopupUI>();
        var so = new SerializedObject(ui);
        // panel = root(self) — backdrop + 콘텐츠 한 번에 토글되어 modal 효과 + Outdoor 시 안 보임.
        so.FindProperty("panel").objectReferenceValue         = root;
        so.FindProperty("titleText").objectReferenceValue     = title;
        so.FindProperty("descText").objectReferenceValue      = desc;
        so.FindProperty("sellPriceText").objectReferenceValue = price;
        so.FindProperty("confirmButton").objectReferenceValue = confirm;
        so.FindProperty("cancelButton").objectReferenceValue  = cancel;
        so.ApplyModifiedPropertiesWithoutUndo();

        ApplyFonts(root);
        root.SetActive(false); // 시작 시 비활성 — Show 호출 시에만 보임

        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[CatNPCSetupBuilder] CatRemovePopup UI 생성 완료 — Ctrl+S 로 저장");
    }

    // ── 헬퍼 ───────────────────────────────────────────────────────────

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

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static void SetCenter(RectTransform rt, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    static TMP_Text AddText(Transform parent, string name, string text, int fontSize, Color color,
                            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
        return tmp;
    }

    static Button MakeFlatButton(Transform parent, string name, string label, Color color,
                                 Vector2 anchor, Vector2 size, Vector2 anchoredPos)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var img = go.AddComponent<Image>(); img.color = color;
        var btn = go.AddComponent<Button>();

        var labelGo = NewUI("Label", go.transform);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return btn;
    }

    static void ApplyFonts(GameObject root)
    {
        const string boldPath  = "Assets/Art/Fonts/Maplestory OTF Bold SDF.asset";
        const string lightPath = "Assets/Art/Fonts/Maplestory OTF Light SDF.asset";
        var bold  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(boldPath);
        var light = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(lightPath);
        if (bold == null && light == null) return;
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
            tmp.font = bold != null ? bold : light;
    }

    static Sprite LoadSpriteFromSheet(string sheetPath, string spriteName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        foreach (var a in assets)
        {
            if (a is Sprite s && s.name == spriteName) return s;
        }
        // fallback: 첫 sprite
        foreach (var a in assets)
            if (a is Sprite s) return s;
        return null;
    }
}
