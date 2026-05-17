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
