using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 가구 배치 시스템 셋업 자동화:
///   1. TagManager.asset 에 Floor / Wall / Furniture Layer 자동 추가
///   2. Main 씬의 [ Managers ] 하위에 PlacementManager + PlacementRestorer GameObject 배치
///   3. 슬롯(LayerMask, placedFurnitureRoot) 자동 연결
///
/// 메뉴:
///   HumanCat → Placement → Add Placement Layers
///   HumanCat → Placement → Setup PlacementManager (Main scene)
///
/// 사용자가 직접 해야 할 일:
///   - Indoor 의 바닥/벽 GameObject 에 Collider2D 추가 + 해당 Layer 할당 (디자이너 영역)
///   - 가구 prefab 들은 PlacementManager 가 자동으로 Furniture Layer 적용
/// </summary>
public static class PlacementSetupBuilder
{
    const string MainScene = "Assets/Scenes/Main.unity";

    static readonly string[] LayersToAdd = { "Floor", "Wall", "Furniture" };

    [MenuItem("HumanCat/Placement/Add Placement Layers")]
    public static void AddLayers()
    {
        var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManager == null || tagManager.Length == 0)
        {
            Debug.LogError("[PlacementSetupBuilder] TagManager.asset 못 찾음");
            return;
        }
        var so = new SerializedObject(tagManager[0]);
        var layers = so.FindProperty("layers");

        int added = 0;
        foreach (var name in LayersToAdd)
        {
            if (AlreadyExists(layers, name)) continue;
            if (TryAddInEmptySlot(layers, name)) added++;
            else Debug.LogWarning($"[PlacementSetupBuilder] Layer '{name}' 추가 실패 — 빈 슬롯 부족");
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"[PlacementSetupBuilder] Layer 추가 완료 — 신규 {added} 개 (이미 있는 것은 스킵)");
    }

    static bool AlreadyExists(SerializedProperty layers, string name)
    {
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == name) return true;
        return false;
    }

    static bool TryAddInEmptySlot(SerializedProperty layers, string name)
    {
        // 0~7 은 Unity 빌트인 / 예약. 8 부터 빈 슬롯 탐색.
        for (int i = 8; i < layers.arraySize; i++)
        {
            var prop = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(prop.stringValue))
            {
                prop.stringValue = name;
                return true;
            }
        }
        return false;
    }

    // ── PlacementManager 씬 배치 ────────────────────────────────────────

    [MenuItem("HumanCat/Placement/Setup PlacementManager (Main scene)")]
    public static void SetupManagerInMain()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        var managersHost = FindInActiveScene("[ Managers ]");
        if (managersHost == null)
        {
            Debug.LogError("[PlacementSetupBuilder] '[ Managers ]' 없음 — Main 씬 확인");
            return;
        }

        var furniture = FindInActiveScene("Furniture");
        if (furniture == null)
        {
            Debug.LogError("[PlacementSetupBuilder] 'Furniture' 부모 없음 — Indoor/Furniture 확인");
            return;
        }

        // PlacementManager
        var pmGo = managersHost.transform.Find("PlacementManager")?.gameObject;
        if (pmGo == null)
        {
            pmGo = new GameObject("PlacementManager");
            pmGo.transform.SetParent(managersHost.transform, false);
        }
        var pm = pmGo.GetComponent<PlacementManager>() ?? pmGo.AddComponent<PlacementManager>();

        int floor     = LayerMask.NameToLayer("Floor");
        int wall      = LayerMask.NameToLayer("Wall");
        int furnLayer = LayerMask.NameToLayer("Furniture");

        var pso = new SerializedObject(pm);
        if (floor     >= 0) SetLayerMask(pso, "floorMask",     1 << floor);
        if (wall      >= 0) SetLayerMask(pso, "wallMask",      1 << wall);
        if (furnLayer >= 0) SetLayerMask(pso, "furnitureMask", 1 << furnLayer);
        pso.FindProperty("placedFurnitureRoot").objectReferenceValue = furniture.transform;
        pso.ApplyModifiedPropertiesWithoutUndo();

        // PlacementRestorer
        var prGo = managersHost.transform.Find("PlacementRestorer")?.gameObject;
        if (prGo == null)
        {
            prGo = new GameObject("PlacementRestorer");
            prGo.transform.SetParent(managersHost.transform, false);
        }
        var pr = prGo.GetComponent<PlacementRestorer>() ?? prGo.AddComponent<PlacementRestorer>();
        var prso = new SerializedObject(pr);
        prso.FindProperty("placedFurnitureRoot").objectReferenceValue = furniture.transform;
        prso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[PlacementSetupBuilder] PlacementManager / PlacementRestorer 배치 완료 — Ctrl+S 로 저장");

        if (floor < 0 || wall < 0 || furnLayer < 0)
            Debug.LogWarning("[PlacementSetupBuilder] Layer 가 일부 없음 — 'Add Placement Layers' 메뉴 먼저 실행");
    }

    static void SetLayerMask(SerializedObject so, string propName, int maskValue)
    {
        var p = so.FindProperty(propName);
        if (p != null) p.intValue = maskValue;
    }

    // ── PlacementControls UI 배치 ─────────────────────────────────────

    [MenuItem("HumanCat/Placement/Setup PlacementControls UI (Main scene)")]
    public static void SetupControlsUI()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        var uiRoot = FindInActiveScene("[ UI ]");
        if (uiRoot == null)
        {
            Debug.LogError("[PlacementSetupBuilder] '[ UI ]' 없음");
            return;
        }

        // 기존 동일 이름 정리
        var existing = uiRoot.transform.Find("PlacementControls");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Root — 항상 활성 (PlacementControlsUI 컴포넌트가 OnBegan/OnEnded 구독 유지)
        var root = NewUI("PlacementControls", uiRoot.transform);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0, 0);
        rootRt.anchorMax = new Vector2(1, 0);
        rootRt.pivot     = new Vector2(0.5f, 0);
        rootRt.sizeDelta = new Vector2(0, 200);
        rootRt.anchoredPosition = new Vector2(0, 40);

        // Panel — 자식. 토글되는 부분 (배치 모드 진입/종료 시에만 활성)
        var panel = NewUI("Panel", root.transform);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.12f, 0.75f);

        // Status Text (상단)
        var status = NewUI("Status", panel.transform);
        var statusRt = status.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0, 1);
        statusRt.anchorMax = new Vector2(1, 1);
        statusRt.pivot = new Vector2(0.5f, 1f);
        statusRt.sizeDelta = new Vector2(0, 60);
        statusRt.anchoredPosition = new Vector2(0, -10);
        var statusTmp = status.AddComponent<TextMeshProUGUI>();
        statusTmp.text = "배치 가능";
        statusTmp.fontSize = 36;
        statusTmp.alignment = TextAlignmentOptions.Center;
        statusTmp.color = Color.white;

        // Confirm
        var confirm = MakeFlatButton(panel.transform, "ConfirmButton", "배치",
            new Color(0.20f, 0.45f, 0.25f, 1f),
            new Vector2(0.5f, 0), new Vector2(220, 90),
            new Vector2(-130, 25));

        // Cancel
        var cancel = MakeFlatButton(panel.transform, "CancelButton", "취소",
            new Color(0.45f, 0.18f, 0.18f, 1f),
            new Vector2(0.5f, 0), new Vector2(220, 90),
            new Vector2(130, 25));

        // Component + 슬롯 연결 (panel ← 자식 Panel, root 와 분리)
        var ui = root.AddComponent<PlacementControlsUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue          = panel;
        so.FindProperty("confirmButton").objectReferenceValue  = confirm;
        so.FindProperty("cancelButton").objectReferenceValue   = cancel;
        so.FindProperty("statusText").objectReferenceValue     = statusTmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        ApplyFonts(root);
        root.SetActive(true);    // root 는 항상 활성
        panel.SetActive(false);  // 자식 panel 만 비활성으로 시작

        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[PlacementSetupBuilder] PlacementControls UI 배치 완료 — Ctrl+S 로 저장");
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static Button MakeFlatButton(Transform parent, string name, string label, Color color,
                                  Vector2 anchor, Vector2 size, Vector2 anchoredPos)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var img = go.AddComponent<Image>(); img.color = color;
        var btn = go.AddComponent<Button>();

        var labelGo = NewUI("Label", go.transform);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
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

    // ── EditMode UI 배치 (편집 토글 + 이동/철거 패널) ──────────────────

    [MenuItem("HumanCat/Placement/Setup EditMode UI (Main scene)")]
    public static void SetupEditModeUI()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        var uiRoot = FindInActiveScene("[ UI ]");
        if (uiRoot == null)
        {
            Debug.LogError("[PlacementSetupBuilder] '[ UI ]' 없음");
            return;
        }

        // 기존 동일 이름 정리
        var existing = uiRoot.transform.Find("EditModeUI");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Root — 항상 활성 (컴포넌트가 정적 이벤트 구독 유지). 전체 화면 영역으로.
        var root = NewUI("EditModeUI", uiRoot.transform);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // ── 우상단 토글 버튼 ───────────────────────────────────────────
        var toggleGo = NewUI("EditToggleButton", root.transform);
        var toggleRt = toggleGo.GetComponent<RectTransform>();
        toggleRt.anchorMin = new Vector2(1, 1);
        toggleRt.anchorMax = new Vector2(1, 1);
        toggleRt.pivot     = new Vector2(1, 1);
        toggleRt.sizeDelta = new Vector2(240, 96);
        toggleRt.anchoredPosition = new Vector2(-30, -30);
        var toggleImg = toggleGo.AddComponent<Image>();
        toggleImg.color = new Color(0.20f, 0.35f, 0.55f, 1f);
        var toggleBtn = toggleGo.AddComponent<Button>();

        var toggleLabelGo = NewUI("Label", toggleGo.transform);
        var toggleLabelRt = toggleLabelGo.GetComponent<RectTransform>();
        toggleLabelRt.anchorMin = Vector2.zero;
        toggleLabelRt.anchorMax = Vector2.one;
        toggleLabelRt.offsetMin = Vector2.zero;
        toggleLabelRt.offsetMax = Vector2.zero;
        var toggleLabelTmp = toggleLabelGo.AddComponent<TextMeshProUGUI>();
        toggleLabelTmp.text       = "편집 모드";
        toggleLabelTmp.fontSize   = 36;
        toggleLabelTmp.alignment  = TextAlignmentOptions.Center;
        toggleLabelTmp.color      = Color.white;

        // ── 하단 액션 패널 (이동/철거) ──────────────────────────────────
        var panel = NewUI("ActionPanel", root.transform);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0, 0);
        panelRt.anchorMax = new Vector2(1, 0);
        panelRt.pivot     = new Vector2(0.5f, 0);
        panelRt.sizeDelta = new Vector2(0, 200);
        panelRt.anchoredPosition = new Vector2(0, 40);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.10f, 0.10f, 0.12f, 0.85f);

        // Selection Label
        var sel = NewUI("SelectionLabel", panel.transform);
        var selRt = sel.GetComponent<RectTransform>();
        selRt.anchorMin = new Vector2(0, 1);
        selRt.anchorMax = new Vector2(1, 1);
        selRt.pivot = new Vector2(0.5f, 1f);
        selRt.sizeDelta = new Vector2(0, 60);
        selRt.anchoredPosition = new Vector2(0, -10);
        var selTmp = sel.AddComponent<TextMeshProUGUI>();
        selTmp.text       = "선택된 가구";
        selTmp.fontSize   = 32;
        selTmp.alignment  = TextAlignmentOptions.Center;
        selTmp.color      = Color.white;

        var move = MakeFlatButton(panel.transform, "MoveButton", "이동",
            new Color(0.25f, 0.45f, 0.70f, 1f),
            new Vector2(0.5f, 0), new Vector2(220, 90),
            new Vector2(-130, 25));

        var remove = MakeFlatButton(panel.transform, "RemoveButton", "철거",
            new Color(0.55f, 0.20f, 0.20f, 1f),
            new Vector2(0.5f, 0), new Vector2(220, 90),
            new Vector2(130, 25));

        // Component + 슬롯 연결
        var ctrl = root.AddComponent<EditModeController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("editToggleButton").objectReferenceValue = toggleBtn;
        so.FindProperty("editToggleLabel").objectReferenceValue  = toggleLabelTmp;
        so.FindProperty("actionPanel").objectReferenceValue      = panel;
        so.FindProperty("moveButton").objectReferenceValue       = move;
        so.FindProperty("removeButton").objectReferenceValue     = remove;
        so.FindProperty("selectionLabel").objectReferenceValue   = selTmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        ApplyFonts(root);
        root.SetActive(true);
        panel.SetActive(false); // 자식 패널만 비활성으로 시작 (선택 시 토글)

        // Indoor + Human(Day) 일 때만 표시되도록 [ UI ] 부모에 EditModeUIVisibility 자동 부착.
        // (self-toggle 불가 — 부모에 두고 target=EditModeUI 로 토글하는 게 표준 패턴)
        BindEditModeUIVisibilityComponent(uiRoot.gameObject, root);

        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[PlacementSetupBuilder] EditMode UI 배치 완료 — Ctrl+S 로 저장");
    }

    [MenuItem("HumanCat/Placement/Bind EditModeUI Visibility")]
    public static void BindEditModeUIVisibility()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainScene)
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);

        var uiRoot = FindInActiveScene("[ UI ]");
        var editUI = FindInActiveScene("EditModeUI");
        if (uiRoot == null || editUI == null)
        {
            Debug.LogError("[PlacementSetupBuilder] '[ UI ]' 또는 'EditModeUI' 없음 — Setup EditMode UI 메뉴 먼저 실행");
            return;
        }
        BindEditModeUIVisibilityComponent(uiRoot, editUI);
        EditorSceneManager.MarkSceneDirty(active);
    }

    /// <summary>
    /// host([ UI ]) 에 EditModeUIVisibility 를 부착하고 target 을 연결한다.
    /// 이전에 동일 target 으로 IndoorOnlyVisibility 가 부착되어 있으면 같이 정리 (중복 토글 방지).
    /// 이미 EditModeUIVisibility 가 같은 target 으로 있으면 스킵 (멱등).
    /// </summary>
    static void BindEditModeUIVisibilityComponent(GameObject host, GameObject target)
    {
        if (host == null || target == null) return;

        // 1) 이전 버전에서 부착해 둔 IndoorOnlyVisibility(target=EditModeUI) 정리
        foreach (var legacy in host.GetComponents<IndoorOnlyVisibility>())
        {
            var lso = new SerializedObject(legacy);
            if (lso.FindProperty("target").objectReferenceValue == target)
            {
                Object.DestroyImmediate(legacy);
                Debug.Log("[PlacementSetupBuilder] 이전 IndoorOnlyVisibility(EditModeUI) 제거 — EditModeUIVisibility 로 교체");
                break;
            }
        }

        // 2) 이미 EditModeUIVisibility 부착되어 있으면 스킵
        foreach (var existing in host.GetComponents<EditModeUIVisibility>())
        {
            var so = new SerializedObject(existing);
            if (so.FindProperty("target").objectReferenceValue == target)
            {
                Debug.Log("[PlacementSetupBuilder] EditModeUIVisibility 이미 부착됨 — 스킵");
                return;
            }
        }

        var vis = host.AddComponent<EditModeUIVisibility>();
        var vso = new SerializedObject(vis);
        vso.FindProperty("target").objectReferenceValue = target;
        vso.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[PlacementSetupBuilder] EditModeUIVisibility 부착 — Indoor + Human(Day) 일 때만 EditModeUI 표시");
    }

    // ── Default Placements (기본 배치 자산) ────────────────────────────

    const string DefaultPlacementsAssetPath = "Assets/Resources/DefaultPlacements.asset";

    [MenuItem("HumanCat/Placement/Save Current Placements as Default")]
    public static void SaveCurrentAsDefault()
    {
        // Resources 폴더 보장
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // 기존 자산이 있으면 records 만 교체 (참조 유지). 없으면 새로 생성.
        var asset = AssetDatabase.LoadAssetAtPath<DefaultPlacementSet>(DefaultPlacementsAssetPath);
        bool created = false;
        if (asset == null)
        {
            asset   = ScriptableObject.CreateInstance<DefaultPlacementSet>();
            created = true;
        }

        asset.records = new System.Collections.Generic.List<PlacementRecord>();
        foreach (var r in PlacementRepository.All)
        {
            if (r == null || string.IsNullOrEmpty(r.itemId)) continue;
            asset.records.Add(new PlacementRecord { itemId = r.itemId, x = r.x, y = r.y });
        }

        if (created) AssetDatabase.CreateAsset(asset, DefaultPlacementsAssetPath);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PlacementSetupBuilder] DefaultPlacements 저장 — {asset.records.Count} 개 ({DefaultPlacementsAssetPath})");
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
}
