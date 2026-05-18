using UnityEngine;

/// <summary>
/// CatNPC 클릭 입력을 중앙 처리하는 디스패처.
///
/// [규칙]
/// - 고양이 플레이어(CurrentType=Cat) 만 NPC 더블클릭으로 판매 팝업 가능. Human 은 즉시 이동.
/// - NPC 가 아닌 곳 클릭: false 반환 → InputReader 가 OnTapPerformed 발화 → 캐릭터 이동.
/// - NPC 위 첫 클릭: 보류(pending). 단일 클릭 확정 전까지 캐릭터 이동 발화 안 함.
/// - 더블클릭 window 안에 NPC 위 두 번째 클릭: 판매 팝업.
/// - 더블클릭 window 지나면 단일 클릭 확정 → OnDeferredTap 발화 → InputReader 가 OnTapPerformed 발화 → 이동.
///
/// [클릭 영역]
/// - Collider2D 대신 SpriteRenderer.bounds 사용 + spriteBoundsExpand 패딩.
///   collider 가 작아도(또는 trigger 라 raycast 와 별개여도) sprite 가시 영역 전체 hit 가능.
/// </summary>
[DisallowMultipleComponent]
public class CatNPCClickDispatcher : MonoBehaviour
{
    public static CatNPCClickDispatcher Instance { get; private set; }

    [Header("Double Click")]
    [Tooltip("더블클릭 인식 시간 (초). 첫 NPC 클릭 후 이 시간 안에 두 번째 NPC 클릭이 같은 NPC 위면 판매 팝업.")]
    [Min(0.05f)] [SerializeField] float doubleClickWindow = 0.3f;

    [Header("Click Hit Area")]
    [Tooltip("NPC 클릭 영역 — SpriteRenderer.bounds 를 이 값만큼 확장. 작은 collider 보완.")]
    [Min(0f)] [SerializeField] float spriteBoundsExpand = 0.4f;

    /// <summary>
    /// NPC 위 단일 클릭이 더블클릭 timeout 후 확정될 때 발화.
    /// InputReader 가 구독해 자기 OnTapPerformed 를 다시 발화 → 캐릭터 이동.
    /// </summary>
    public static event System.Action<Vector2> OnDeferredTap;

    struct PendingClick
    {
        public CatNPC  npc;
        public Vector2 screenPos;
        public float   deadline;
        public bool    active;
    }

    PendingClick pending;
    Camera       mainCam;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        mainCam  = Camera.main;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// InputReader 가 매 입력마다 호출. 반환 true 면 InputReader 가 OnTapPerformed 발화 보류.
    /// 고양이 플레이어 + NPC 위 클릭일 때만 true.
    /// </summary>
    public bool TryConsumeClick(Vector2 screenPos)
    {
        // 0번 조건: 고양이 플레이어만 NPC 처리
        if (PlayerController.Instance == null ||
            PlayerController.Instance.CurrentType != PlayerType.Cat)
            return false;

        var npc = FindCatNPCAt(screenPos);
        if (npc == null)
        {
            // NPC 아닌 곳 → pending 취소 후 일반 흐름으로 위임 (InputReader 가 이동)
            pending.active = false;
            return false;
        }

        if (pending.active && pending.npc == npc &&
            Time.unscaledTime <= pending.deadline)
        {
            // 더블클릭 확정 → 판매 팝업
            CatRemovePopupUI.Instance?.Show(npc);
            pending.active = false;
        }
        else
        {
            // 첫 클릭 — pending 저장. 캐릭터 이동은 보류.
            pending.npc       = npc;
            pending.screenPos = screenPos;
            pending.deadline  = Time.unscaledTime + doubleClickWindow;
            pending.active    = true;
        }
        return true;
    }

    void Update()
    {
        if (!pending.active) return;
        if (Time.unscaledTime < pending.deadline) return;

        // 더블클릭 window timeout — 단일 클릭으로 확정 → 캐릭터 이동
        var pos = pending.screenPos;
        pending.active = false;
        OnDeferredTap?.Invoke(pos);
    }

    /// <summary>
    /// 화면 좌표에 위치한 CatNPC 검색. SpriteRenderer.bounds + expand 기반 hit 판정.
    /// 여러 NPC 가 겹치면 sortingOrder 가 큰(앞에 그려진) NPC 선택.
    /// </summary>
    CatNPC FindCatNPCAt(Vector2 screenPos)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null || CatManager.Instance == null) return null;

        Vector3 world = mainCam.ScreenToWorldPoint(screenPos);
        world.z = 0f;

        CatNPC best      = null;
        int    bestOrder = int.MinValue;

        foreach (var npc in CatManager.Instance.ActiveCats)
        {
            if (npc == null) continue;
            var sr = npc.GetComponentInChildren<SpriteRenderer>();
            if (sr == null || !sr.enabled || sr.sprite == null) continue;

            Bounds b = sr.bounds;
            b.Expand(spriteBoundsExpand);
            if (!b.Contains(world)) continue;

            if (sr.sortingOrder > bestOrder)
            {
                bestOrder = sr.sortingOrder;
                best      = npc;
            }
        }
        return best;
    }
}
