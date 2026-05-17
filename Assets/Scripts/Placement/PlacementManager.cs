using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 가구 배치 모드의 핵심 매니저.
///
/// [상태]
/// - Idle    : 평소 (preview 없음)
/// - Placing : TryBegin 후 preview 활성. 드래그로 위치 이동, 손 떼면 위치 고정.
///             확정/취소는 PlacementControlsUI 의 [배치]/[취소] 버튼이 호출.
///
/// [중요 변경]
/// - 드래그 종료(손 떼기)는 더 이상 자동 배치하지 않는다 — 사용자가 [배치] 버튼을 눌러야 확정.
/// - Preview 는 ItemData.icon 이 아니라 placementPrefab 을 직접 Instantiate 해 실제 크기/모양 시각화.
///   자식 SpriteRenderer 색상은 PlacementPreview 가 일괄 토글.
/// - Preview 의 Collider2D 는 비활성화 — 자기 자신이 furniture mask 에 잡혀 충돌 처리되지 않도록.
/// </summary>
public class PlacementManager : MonoBehaviour
{
    public enum State { Idle, Placing }

    public static PlacementManager Instance { get; private set; }

    [Header("Grid")]
    [SerializeField] float gridSize = 0.5f;

    [Header("Surface Masks")]
    [SerializeField] LayerMask floorMask;
    [SerializeField] LayerMask wallMask;

    [Header("Collision Mask")]
    [SerializeField] LayerMask furnitureMask;
    [SerializeField] string    furnitureLayerName = "Furniture";

    [Header("Scene Refs")]
    [SerializeField] Transform placedFurnitureRoot;

    [Header("Preview Offset")]
    [SerializeField] Vector2 spawnOffsetFromPlayer = new Vector2(1.5f, 0f);

    [Header("Magnetic Snap")]
    [Tooltip("Wall-only + BottomFree 가구(창문/문 등)가 Wall 영역의 하단에 마그네틱 스냅되는 최대 거리. " +
             "preview 의 sprite 하단이 wall collider 하단으로부터 이 거리 이내에 들어오면 정확히 정렬된다. " +
             "Wall+Floor 가구(책장 등)는 거리에 관계없이 항상 정렬되므로 이 값과 무관.")]
    [Min(0f)] [SerializeField] float magneticBottomThreshold = 1.0f;

    public enum InvalidReason { None, SurfaceMismatch, FurnitureOverlap }

    public State         CurrentState           { get; private set; } = State.Idle;
    public bool          IsActive               => CurrentState == State.Placing;
    public bool          CurrentPreviewIsValid  => preview != null && preview.IsValid;
    public InvalidReason CurrentInvalidReason   { get; private set; } = InvalidReason.None;

    public event Action OnBegan;
    public event Action OnEnded;

    PlacementPreview preview;
    ItemData         currentItem;
    Camera           mainCam;
    bool             dragging;

    static readonly List<RaycastResult> uiRaycastBuffer = new List<RaycastResult>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        mainCam  = Camera.main;
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

    /// <summary>Day/Night 가 바뀌면 진행 중인 배치 모드를 자동 취소.</summary>
    void HandleGameStateChanged(GameState _)
    {
        if (IsActive) Cancel();
    }

    // ── 외부 진입점 ─────────────────────────────────────────────────────

    public bool TryBegin(ItemData item)
    {
        if (IsActive) return false;
        if (item == null || !item.Placeable)
        {
            Debug.LogWarning("[PlacementManager] 배치 불가 아이템");
            return false;
        }
        if (PlayerController.Instance == null ||
            PlayerController.Instance.CurrentType != PlayerType.Human)
        {
            Debug.LogWarning("[PlacementManager] Human 캐릭터에서만 배치 가능");
            return false;
        }
        if (SceneController.Instance == null ||
            SceneController.Instance.CurrentEnvironment != EnvironmentType.Indoor)
        {
            Debug.LogWarning("[PlacementManager] Indoor 에서만 배치 가능");
            return false;
        }

        currentItem  = item;
        dragging     = false;
        CreatePreview(item);
        CurrentState = State.Placing;
        UIBlocker.AcquireSafe();
        OnBegan?.Invoke();
        return true;
    }

    /// <summary>[배치] 버튼이 호출. valid 일 때만 실제 배치.</summary>
    public void Confirm()
    {
        if (!IsActive) return;
        if (preview == null) return;
        if (!preview.IsValid)
        {
            Debug.Log("[PlacementManager] 현재 위치는 배치 불가 (빨강) — 위치를 옮기세요");
            return;
        }
        ConfirmPlacement();
    }

    /// <summary>[취소] 버튼 또는 ESC 가 호출.</summary>
    public void Cancel() => End(confirmed: false);

    // ── 내부 ────────────────────────────────────────────────────────────

    void CreatePreview(ItemData item)
    {
        var instance = Instantiate(item.PlacementPrefab, placedFurnitureRoot);
        instance.name = $"Preview_{item.ItemId}";

        NormalizeScale(instance, item.PlacementPrefab, placedFurnitureRoot);

        // 충돌 검사 시 자기 자신을 furniture 로 잡지 않도록 collider 비활성
        foreach (var c in instance.GetComponentsInChildren<Collider2D>(true))
            c.enabled = false;

        var pv = instance.AddComponent<PlacementPreview>();
        pv.Initialize();

        Vector3 origin = PlayerController.Instance.transform.position
                       + (Vector3)spawnOffsetFromPlayer;
        instance.transform.position = SnapToGrid(origin);
        instance.transform.rotation = Quaternion.identity;
        preview = pv;
    }

    /// <summary>
    /// 부모의 lossyScale 영향을 무력화해 인스턴스의 world scale 이 prefab.localScale 과 동일하게 보이도록 보정.
    /// Indoor(scale 2,2,2) 같은 부모 아래 두어도 가구 본연의 크기가 유지된다.
    /// </summary>
    public static void NormalizeScale(GameObject instance, GameObject prefab, Transform parent)
    {
        if (instance == null || prefab == null) return;
        Vector3 desired     = prefab.transform.localScale;
        Vector3 parentLossy = parent != null ? parent.lossyScale : Vector3.one;
        instance.transform.localScale = new Vector3(
            desired.x / Mathf.Max(0.0001f, parentLossy.x),
            desired.y / Mathf.Max(0.0001f, parentLossy.y),
            desired.z / Mathf.Max(0.0001f, parentLossy.z));
    }

    /// <summary>
    /// 배치된 가구에 Collider2D 가 하나도 없으면 자식 SpriteRenderer 결합 bounds 기준 BoxCollider2D 자동 추가.
    /// 추가된 collider 는 trigger 로 설정 — 충돌 검사(OverlapBox)에만 사용되고 캐릭터 통과를 막지 않는다.
    /// </summary>
    public static void EnsureFurnitureCollider(GameObject instance)
    {
        if (instance == null) return;
        if (instance.GetComponentInChildren<Collider2D>(true) != null) return;

        var srs = instance.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs.Length == 0) return;

        bool   init     = false;
        Bounds combined = default;
        foreach (var sr in srs)
        {
            if (sr == null) continue;
            if (!init) { combined = sr.bounds; init = true; }
            else combined.Encapsulate(sr.bounds);
        }
        if (!init) return;

        var col = instance.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Vector3 lossy   = instance.transform.lossyScale;
        Vector3 localC  = instance.transform.InverseTransformPoint(combined.center);
        col.offset = new Vector2(localC.x, localC.y);
        col.size   = new Vector2(
            combined.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
            combined.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)));
    }

    void Update()
    {
        if (!IsActive) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cancel();
            return;
        }

        HandleDrag();
        UpdateValidation();
    }

    void HandleDrag()
    {
        // 1순위: 터치
        var touch = Touchscreen.current;
        if (touch != null)
        {
            var press = touch.primaryTouch.press;
            if (press.wasPressedThisFrame)
            {
                Vector2 p = touch.primaryTouch.position.ReadValue();
                if (!IsPointerOverUI(p))
                {
                    dragging = true;
                    MovePreviewToScreen(p);
                }
                return;
            }
            if (dragging && press.isPressed)
            {
                MovePreviewToScreen(touch.primaryTouch.position.ReadValue());
                return;
            }
            if (dragging && press.wasReleasedThisFrame)
            {
                MovePreviewToScreen(touch.primaryTouch.position.ReadValue());
                dragging = false; // 손 떼면 그 자리에 그대로 — 배치는 [배치] 버튼이 트리거
                return;
            }
            if (press.isPressed) return;
        }

        // 2순위: 마우스
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 p = mouse.position.ReadValue();
            if (!IsPointerOverUI(p))
            {
                dragging = true;
                MovePreviewToScreen(p);
            }
            return;
        }
        if (dragging && mouse.leftButton.isPressed)
        {
            MovePreviewToScreen(mouse.position.ReadValue());
            return;
        }
        if (dragging && mouse.leftButton.wasReleasedThisFrame)
        {
            MovePreviewToScreen(mouse.position.ReadValue());
            dragging = false;
        }
    }

    void MovePreviewToScreen(Vector2 screenPos)
    {
        if (preview == null || mainCam == null) return;
        Vector3 world = mainCam.ScreenToWorldPoint(screenPos);
        world.z = 0f;
        preview.transform.position = SnapToGrid(world);

        // Wall 영역에서 sprite 하단을 Wall 하단에 정렬 (가구 유형별 규칙)
        ApplyMagneticBottomSnap();
    }

    /// <summary>
    /// Wall 영역에 있을 때 preview 의 sprite 하단을 Wall collider 의 bounds.min.y 에 정렬.
    ///
    /// 가구 유형별 규칙:
    /// - Floor+Wall (책장 등) : 거리 무관하게 항상 정렬. 벽에 붙는 가구는 발을 벽 영역의 바닥에 둔다는 의미.
    /// - Wall-only + BottomFree (창문/문 등) : 평소엔 자유 이동, sprite 하단이 wall 하단으로부터
    ///   magneticBottomThreshold 이내일 때만 마그네틱 스냅.
    /// - Wall-only + !BottomFree (포스터/액자 등) : 스냅 없음. 사용자가 둔 위치 그대로.
    /// X 는 그리드 스냅 결과 그대로 유지.
    /// </summary>
    void ApplyMagneticBottomSnap()
    {
        if (currentItem == null || preview == null) return;

        bool allowFloor = (currentItem.AllowedSurfaces & PlacementSurface.Floor) != 0;
        bool allowWall  = (currentItem.AllowedSurfaces & PlacementSurface.Wall)  != 0;
        if (!allowWall) return; // 벽에 안 붙는 가구는 스냅 대상 아님

        var wallCol = Physics2D.OverlapPoint(preview.transform.position, wallMask);
        if (wallCol == null) return; // Wall 영역이 아니면 보정 안 함

        Vector2 size           = preview.GetWorldSize();
        Vector2 spriteCenter   = preview.GetWorldCenter();
        float   spriteBottomY  = spriteCenter.y - size.y * 0.5f;
        float   wallBottomY    = wallCol.bounds.min.y;
        float   deltaY         = wallBottomY - spriteBottomY;

        bool shouldSnap;
        if (allowFloor)
            shouldSnap = true; // Wall+Floor: 항상 바닥 정렬
        else
            shouldSnap = currentItem.BottomFree && Mathf.Abs(deltaY) <= magneticBottomThreshold;

        if (shouldSnap && Mathf.Abs(deltaY) > 0.0001f)
            preview.transform.position += new Vector3(0f, deltaY, 0f);
    }

    Vector3 SnapToGrid(Vector3 p)
    {
        return new Vector3(
            Mathf.Round(p.x / gridSize) * gridSize,
            Mathf.Round(p.y / gridSize) * gridSize,
            0f);
    }

    void UpdateValidation()
    {
        if (preview == null || currentItem == null) return;
        bool surfaceOk   = CheckSurface(currentItem, preview.transform.position);
        bool collisionOk = CheckCollision(preview);

        if      (!surfaceOk)   CurrentInvalidReason = InvalidReason.SurfaceMismatch;
        else if (!collisionOk) CurrentInvalidReason = InvalidReason.FurnitureOverlap;
        else                   CurrentInvalidReason = InvalidReason.None;

        preview.SetValid(surfaceOk && collisionOk);
    }

    bool CheckSurface(ItemData item, Vector2 pos)
    {
        bool allowFloor = (item.AllowedSurfaces & PlacementSurface.Floor) != 0;
        bool allowWall  = (item.AllowedSurfaces & PlacementSurface.Wall)  != 0;
        if (allowFloor && Physics2D.OverlapPoint(pos, floorMask) != null) return true;
        if (allowWall  && Physics2D.OverlapPoint(pos, wallMask)  != null) return true;
        return false;
    }

    /// <summary>
    /// 가구 간 충돌 검사.
    /// 규칙: 각 가구의 "위쪽 50%" 영역은 다른 가구와 겹쳐도 허용. "아래쪽 50%" 끼리만 겹치면 invalid.
    ///       (예: 키 큰 가구의 윗부분에 작은 가구를 살짝 올려두는 식의 자연스러운 레이아웃 허용)
    /// </summary>
    bool CheckCollision(PlacementPreview pv)
    {
        Vector2 size = pv.GetWorldSize();
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);

        // preview 영역 (월드) — sprite 결합 bounds center 사용 (transform.position 은 pivot 일 수 있음)
        Vector2 pc      = pv.GetWorldCenter();
        float   pMinX   = pc.x - size.x * 0.5f;
        float   pMaxX   = pc.x + size.x * 0.5f;
        float   pMinY   = pc.y - size.y * 0.5f;
        float   pMidY   = pc.y;                  // preview 아래쪽 절반의 상단 = center.y

        // 1차 필터: preview 전체 사각형으로 hit 후보 모음
        var hits = Physics2D.OverlapBoxAll(pc, size, 0f, furnitureMask);
        foreach (var c in hits)
        {
            if (c == null) continue;
            Bounds b = c.bounds;

            // X 축 교집합
            float xOverlapMin = Mathf.Max(pMinX, b.min.x);
            float xOverlapMax = Mathf.Min(pMaxX, b.max.x);
            if (xOverlapMax <= xOverlapMin) continue; // X 안 겹치면 무시

            // Y 축: preview 아래절반 [pMinY, pMidY] vs other 아래절반 [b.min.y, b.center.y]
            float yOverlapMin = Mathf.Max(pMinY, b.min.y);
            float yOverlapMax = Mathf.Min(pMidY, b.center.y);
            if (yOverlapMax > yOverlapMin) return false; // 두 아래절반이 동시에 겹침 → invalid
        }
        return true;
    }

    void ConfirmPlacement()
    {
        Vector3 pos = preview.transform.position;
        var instance = Instantiate(currentItem.PlacementPrefab, pos, Quaternion.identity, placedFurnitureRoot);
        NormalizeScale(instance, currentItem.PlacementPrefab, placedFurnitureRoot);
        EnsureFurnitureCollider(instance);    // 향후 배치 시 OverlapBox 충돌 검사에 잡히도록
        ApplyFurnitureLayer(instance);

        InventoryManager.Instance?.TryRemoveItem(currentItem.ItemId, 1);
        PlacementRepository.Add(currentItem.ItemId, pos);

        End(confirmed: true);
    }

    void ApplyFurnitureLayer(GameObject root)
    {
        int layer = LayerMask.NameToLayer(furnitureLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[PlacementManager] Layer '{furnitureLayerName}' 없음 — 'Add Placement Layers' 메뉴 실행 필요");
            return;
        }
        SetLayerRecursive(root, layer);
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursive(t.gameObject, layer);
    }

    void End(bool confirmed)
    {
        if (preview != null) Destroy(preview.gameObject);
        preview      = null;
        currentItem  = null;
        dragging     = false;
        CurrentState = State.Idle;
        UIBlocker.ReleaseSafe();
        OnEnded?.Invoke();
    }

    static bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        uiRaycastBuffer.Clear();
        EventSystem.current.RaycastAll(data, uiRaycastBuffer);
        return uiRaycastBuffer.Count > 0;
    }
}
