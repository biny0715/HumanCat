using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 배치된 가구의 "편집(Edit) 모드" 매니저.
///
/// [기능]
/// 1) [편집] 토글 버튼으로 모드 ON/OFF (UIBlocker 로 플레이어 이동 차단)
/// 2) 모드 ON 상태에서 Furniture Layer 가구 클릭 → 선택 + 외곽선 하이라이트
/// 3) 선택 가구에 대해 [이동] / [철거] 버튼 노출
/// 4) [이동] → PlacementManager.TryBeginRelocate (원본 비활성, 취소 시 원복)
/// 5) [철거] → 가구 Destroy + Repository 제거 + 인벤토리 회수(+1)
///
/// [컨벤션 준수]
/// - Input System (Touchscreen / Mouse / Keyboard) — 구식 Input 클래스 금지
/// - UI hit 검사: EventSystem.RaycastAll (IsPointerOverGameObject 의 stale hover 문제 회피)
/// - 정적 이벤트 구독은 OnDestroy 에서만 해제
/// - 핸들러 안 `this == null` 가드 (씬 전환 race)
/// - Day/Night 전환 시 자동 Exit (PlacementManager 와 동일 정책)
/// - 배치 모드 중에는 클릭 감지 비활성 — 모드 중첩 방지
/// </summary>
public class EditModeController : MonoBehaviour
{
    public static EditModeController Instance { get; private set; }

    [Header("Toggle Button (Edit Mode ON/OFF)")]
    [SerializeField] Button   editToggleButton;
    [SerializeField] TMP_Text editToggleLabel;
    [SerializeField] string   labelOff = "편집 모드";
    [SerializeField] string   labelOn  = "편집 종료";

    [Header("Action Panel (선택 시 표시)")]
    [SerializeField] GameObject actionPanel;
    [SerializeField] Button     moveButton;
    [SerializeField] Button     removeButton;
    [SerializeField] TMP_Text   selectionLabel;

    [Header("Highlight")]
    [SerializeField] Color outlineColor      = new Color(1f, 0.85f, 0.2f, 1f);
    [Min(0.5f)]
    [SerializeField] float outlinePixelWidth = 2f;

    public bool              IsEditMode       { get; private set; }
    public FurnitureInstance CurrentSelection => currentSelection;

    /// <summary>편집 모드 진입/종료 시 발화. 다른 UI 버튼이 자동 비활성화에 사용.</summary>
    public event System.Action<bool> OnEditModeChanged;

    FurnitureInstance  currentSelection;
    FurnitureHighlight currentHighlight;
    Camera             mainCam;
    bool               uiBlockerAcquired;

    static readonly List<RaycastResult> uiRaycastBuffer = new List<RaycastResult>();

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        mainCam  = Camera.main;

        if (editToggleButton != null) editToggleButton.onClick.AddListener(Toggle);
        if (moveButton       != null) moveButton.onClick.AddListener(OnMove);
        if (removeButton     != null) removeButton.onClick.AddListener(OnRemove);

        // 편집 버튼은 배치 모드/상점 열림 시 비활성 — 편집 모드 자체는 종료용으로 항상 활성.
        if (editToggleButton != null && editToggleButton.GetComponent<ModeGatedButton>() == null)
        {
            var gate = editToggleButton.gameObject.AddComponent<ModeGatedButton>();
            gate.SetBlockedBy(BlockingMode.Placement | BlockingMode.Shop);
        }

        if (actionPanel != null) actionPanel.SetActive(false);
        UpdateToggleLabel();

        // ShopUI/InventoryUI 같은 모달 팝업이 위에 그려지도록 sibling 을 맨 앞으로(= 화면에서 가장 뒤)
        transform.SetAsFirstSibling();
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 외부(IndoorOnlyVisibility 등)에 의해 EditModeUI 가 비활성될 때 잠금/선택 상태를 정리.
    /// Outdoor 로 나갔는데 EditMode 가 ON 인 채로 UIBlocker 가 남는 케이스 방지.
    /// 정적 이벤트 구독은 OnDestroy 에서만 해제 — 다시 활성되면 그대로 살아있음.
    /// </summary>
    void OnDisable()
    {
        if (IsEditMode) Exit();
    }

    /// <summary>Day/Night 가 바뀌면(캐릭터 전환 포함) 편집 모드 자동 종료.</summary>
    void HandleGameStateChanged(GameState _)
    {
        if (this == null) return;
        if (IsEditMode) Exit();
    }

    // ── Toggle / Enter / Exit ────────────────────────────────────────────

    public void Toggle()
    {
        if (IsEditMode) Exit();
        else            Enter();
    }

    public bool Enter()
    {
        if (IsEditMode) return false;

        if (PlayerController.Instance == null ||
            PlayerController.Instance.CurrentType != PlayerType.Human)
        {
            Debug.Log("[EditMode] Human 캐릭터에서만 진입 가능");
            return false;
        }
        if (SceneController.Instance == null ||
            SceneController.Instance.CurrentEnvironment != EnvironmentType.Indoor)
        {
            Debug.Log("[EditMode] Indoor 에서만 진입 가능");
            return false;
        }
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsActive)
        {
            Debug.Log("[EditMode] 배치 모드 중에는 편집 모드 진입 불가");
            return false;
        }

        IsEditMode = true;
        if (!uiBlockerAcquired)
        {
            UIBlocker.AcquireSafe();
            uiBlockerAcquired = true;
        }
        UpdateToggleLabel();
        OnEditModeChanged?.Invoke(true);
        return true;
    }

    public void Exit()
    {
        if (!IsEditMode) return;
        ClearSelection();
        IsEditMode = false;
        if (uiBlockerAcquired)
        {
            UIBlocker.ReleaseSafe();
            uiBlockerAcquired = false;
        }
        UpdateToggleLabel();
        OnEditModeChanged?.Invoke(false);
    }

    void UpdateToggleLabel()
    {
        if (editToggleLabel != null)
            editToggleLabel.text = IsEditMode ? labelOn : labelOff;
    }

    // ── Click Detection ─────────────────────────────────────────────────

    void Update()
    {
        if (!IsEditMode) return;
        // 배치 모드가 활성이면 그쪽이 입력 우선 — 편집 모드 입력은 잠시 멈춤.
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsActive) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Exit();
            return;
        }

        Vector2? pressed = ReadPressOnce();
        if (!pressed.HasValue) return;

        Vector2 sp = pressed.Value;
        if (IsPointerOverUI(sp)) return; // UI 위 클릭은 무시
        if (mainCam == null) return;

        Vector3 world = mainCam.ScreenToWorldPoint(sp);
        world.z = 0f;

        var fi = FindFurnitureAtWorld(world);
        if (fi == null)
        {
            // 빈 영역 클릭 → 선택 해제 (직관적 UX)
            ClearSelection();
            return;
        }
        Select(fi);
    }

    /// <summary>
    /// placedFurnitureRoot 아래 활성 가구 중 sprite 결합 bounds 가 클릭 위치를 포함하는 것을 찾는다.
    /// collider 유무와 무관하게 동작 — Furniture Layer Mask 가 아닌 sprite 모양으로 hit 판정.
    /// 여러 가구가 겹치면 sortingOrder 가 가장 큰(= 화면에서 위에 보이는) 가구 우선.
    /// </summary>
    static FurnitureInstance FindFurnitureAtWorld(Vector3 world)
    {
        var pm = PlacementManager.Instance;
        if (pm == null || pm.PlacedFurnitureRoot == null) return null;

        FurnitureInstance best     = null;
        int               bestOrder = int.MinValue;

        var fis = pm.PlacedFurnitureRoot.GetComponentsInChildren<FurnitureInstance>(false);
        foreach (var fi in fis)
        {
            if (fi == null) continue;
            if (!TryGetSpriteBounds(fi, out Bounds combined, out int topOrder)) continue;
            if (world.x < combined.min.x || world.x > combined.max.x) continue;
            if (world.y < combined.min.y || world.y > combined.max.y) continue;

            if (topOrder > bestOrder)
            {
                bestOrder = topOrder;
                best      = fi;
            }
        }
        return best;
    }

    static bool TryGetSpriteBounds(FurnitureInstance fi, out Bounds combined, out int topOrder)
    {
        combined = default;
        topOrder = int.MinValue;
        bool init = false;

        var srs = fi.GetComponentsInChildren<SpriteRenderer>(false);
        foreach (var sr in srs)
        {
            if (sr == null || !sr.enabled || sr.sprite == null) continue;
            if (!init) { combined = sr.bounds; init = true; }
            else combined.Encapsulate(sr.bounds);
            if (sr.sortingOrder > topOrder) topOrder = sr.sortingOrder;
        }
        return init;
    }

    static Vector2? ReadPressOnce()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            return touch.primaryTouch.position.ReadValue();
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return mouse.position.ReadValue();
        return null;
    }

    static bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        uiRaycastBuffer.Clear();
        EventSystem.current.RaycastAll(data, uiRaycastBuffer);
        return uiRaycastBuffer.Count > 0;
    }

    // ── Selection ──────────────────────────────────────────────────────

    void Select(FurnitureInstance fi)
    {
        if (fi == null) return;
        if (currentSelection == fi) return; // 동일 가구 재클릭 → 무시

        ClearSelection();

        currentSelection = fi;
        currentHighlight = fi.GetComponent<FurnitureHighlight>()
                        ?? fi.gameObject.AddComponent<FurnitureHighlight>();
        currentHighlight.Configure(outlineColor, outlinePixelWidth);
        currentHighlight.Show();

        if (actionPanel != null) actionPanel.SetActive(true);
        if (selectionLabel != null)
        {
            var item = fi.ResolveItemData();
            selectionLabel.text = item != null ? item.DisplayName : fi.ItemId;
        }
    }

    void ClearSelection()
    {
        if (currentHighlight != null) currentHighlight.Hide();
        currentHighlight = null;
        currentSelection = null;
        if (actionPanel != null) actionPanel.SetActive(false);
    }

    // ── Actions ────────────────────────────────────────────────────────

    void OnMove()
    {
        if (currentSelection == null) return;
        var fi   = currentSelection;
        var item = fi.ResolveItemData();
        if (item == null)
        {
            Debug.LogWarning($"[EditMode] ItemData 조회 실패: {fi.ItemId}");
            return;
        }
        var pm = PlacementManager.Instance;
        if (pm == null) return;

        Vector2    originalPos = fi.transform.position;
        GameObject originalGo  = fi.gameObject;

        // 선택/하이라이트는 정리. 원본 GameObject 는 PlacementManager 가 관리(비활성 → 확정/취소).
        ClearSelection();

        if (!pm.TryBeginRelocate(item, originalPos, originalGo))
            Debug.LogWarning("[EditMode] Relocate 진입 실패");
    }

    void OnRemove()
    {
        if (currentSelection == null) return;
        var fi   = currentSelection;
        var item = fi.ResolveItemData();
        if (item == null)
        {
            Debug.LogWarning($"[EditMode] ItemData 조회 실패: {fi.ItemId}");
            return;
        }

        Vector2 pos = fi.transform.position;
        var go = fi.gameObject;
        ClearSelection();

        InventoryManager.Instance?.TryAddItem(item.ItemId, 1);
        PlacementRepository.Remove(item.ItemId, pos);
        Destroy(go);
    }
}
