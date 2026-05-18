using UnityEngine;

/// <summary>
/// Indoor 영역에서 자율 이동하는 고양이 NPC.
///
/// [설계 의도]
/// - PlayerController / CharacterControllerBase 와 완전 독립 — Animator 와 Rigidbody2D 만 사용.
/// - 단순 상태 머신 (Idle / Move). 추가 상태(예: Flee, MoveToFood, Sleep) 는 enum 확장 + 분기만 추가.
/// - Indoor 만 활성: SceneController.OnEnvironmentChanged 구독으로 Indoor↔Outdoor 즉시 반응.
///   외부에서 GameObject 활성/비활성 토글 시에도 안전하도록 OnEnable/OnDestroy 패턴 준수.
/// - Floor 판정: Physics2D.OverlapPoint(floorLayer) — Floor Collider2D 가 있는 영역 안에서만 이동.
/// - 여러 인스턴스가 정적 상태 공유 없이 독립 작동 — 모든 상태 변수는 인스턴스 필드.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class CatNPCController : MonoBehaviour
{
    /// <summary>NPC 상태. 향후 Flee/Food/Sleep 등으로 확장 가능.</summary>
    public enum CatState { Idle, Move }

    [Header("Movement")]
    [Tooltip("Move 상태에서의 초당 이동 거리(월드 단위).")]
    [SerializeField] float moveSpeed = 20f;
    [Tooltip("현재 위치 기준 랜덤 목적지 반경.")]
    [SerializeField] float moveRadius = 30f;
    [Tooltip("도착 판정 거리.")]
    [SerializeField] float arriveThreshold = 0.05f;
    [Tooltip("Floor 위 랜덤 좌표 시도 횟수. 실패 시 그 사이클은 Idle 유지.")]
    [Min(1)] [SerializeField] int maxRandomTries = 16;

    [Header("Idle")]
    [SerializeField] float idleTimeMin = 1f;
    [SerializeField] float idleTimeMax = 30f;
    [Tooltip("Idle 종료 시 Move 로 전환될 확률. 1-값 만큼 Idle 재진입.")]
    [Range(0f, 1f)] [SerializeField] float moveProbability = 0.5f;

    [Header("Idle Head Movement")]
    [Tooltip("Idle 중 좌우 고개 돌리기 검사 간격 (랜덤). 매 간격마다 headTurnChance 확률로 flipX 토글.")]
    [SerializeField] float headTurnIntervalMin = 1.5f;
    [SerializeField] float headTurnIntervalMax = 3.5f;
    [Range(0f, 1f)] [SerializeField] float headTurnChance = 0.5f;

    [Header("Layers")]
    [Tooltip("이동 영역 판정 — Layer 'Floor' 권장.")]
    [SerializeField] LayerMask floorLayer;

    [Header("Refs (auto-bind on Awake if empty)")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator       animator;
    [SerializeField] Rigidbody2D    rb;

    static readonly int IsMovingHash = Animator.StringToHash("isMoving");

    public CatState CurrentState => state;

    CatState state = CatState.Idle;
    Vector2  moveTarget;
    float    stateTimer;
    float    headTurnTimer;

    bool isIndoor;
    bool envSubscribed;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator       == null) animator       = GetComponent<Animator>();
        if (rb             == null) rb             = GetComponent<Rigidbody2D>();

        // Rigidbody2D 기본값 강제 — NPC 는 중력 X, 직접 위치 제어
        if (rb != null)
        {
            rb.gravityScale = 0f;
            if (rb.bodyType == RigidbodyType2D.Dynamic) rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void OnEnable()
    {
        SubscribeEnvironment();
        EnterIdle();
    }

    void OnDestroy()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged -= HandleEnvironmentChanged;
    }

    void SubscribeEnvironment()
    {
        if (envSubscribed) return;
        if (SceneController.Instance == null) return; // Start 시점에 늦으면 Update 에서 재시도
        SceneController.Instance.OnEnvironmentChanged += HandleEnvironmentChanged;
        isIndoor = SceneController.Instance.CurrentEnvironment == EnvironmentType.Indoor;
        envSubscribed = true;
    }

    void HandleEnvironmentChanged(EnvironmentType env)
    {
        if (this == null) return;
        isIndoor = env == EnvironmentType.Indoor;
        if (!isIndoor)
        {
            // Outdoor 진입 시 진행 중인 Move 중단, Idle 로 복귀
            state = CatState.Idle;
            SetMovingAnim(false);
        }
    }

    // ── Tick ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (!envSubscribed) SubscribeEnvironment(); // SceneController 가 늦게 생성된 경우
        if (!isIndoor) return;

        switch (state)
        {
            case CatState.Idle: TickIdle(); break;
            case CatState.Move: TickMove(); break;
        }

        TickHeadTurn();
    }

    // ── State: Idle ──────────────────────────────────────────────────────

    void EnterIdle()
    {
        state           = CatState.Idle;
        stateTimer      = Random.Range(idleTimeMin, idleTimeMax);
        headTurnTimer   = Random.Range(headTurnIntervalMin, headTurnIntervalMax);
        SetMovingAnim(false);
    }

    void TickIdle()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        // 종료: 확률로 Move 시도, 실패 시 Idle 재진입
        if (Random.value < moveProbability && TryPickRandomFloorPoint(out var target))
        {
            moveTarget = target;
            state      = CatState.Move;
            SetMovingAnim(true);
            ApplyFacingTowards(target);
        }
        else
        {
            EnterIdle();
        }
    }

    // ── State: Move ──────────────────────────────────────────────────────

    void TickMove()
    {
        Vector2 cur  = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 next = Vector2.MoveTowards(cur, moveTarget, moveSpeed * Time.deltaTime);

        if (rb != null) rb.MovePosition(next);
        else            transform.position = next;

        // 이동 방향에 따라 flipX
        if      (next.x < cur.x - 0.0001f) spriteRenderer.flipX = true;
        else if (next.x > cur.x + 0.0001f) spriteRenderer.flipX = false;

        if (Vector2.Distance(next, moveTarget) <= arriveThreshold)
            EnterIdle();
    }

    // ── Head turn (Idle 중) ──────────────────────────────────────────────

    void TickHeadTurn()
    {
        if (state != CatState.Idle || spriteRenderer == null) return;
        headTurnTimer -= Time.deltaTime;
        if (headTurnTimer > 0f) return;
        headTurnTimer = Random.Range(headTurnIntervalMin, headTurnIntervalMax);
        if (Random.value < headTurnChance) spriteRenderer.flipX = !spriteRenderer.flipX;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// 현재 위치 기준 moveRadius 안에서 Floor 위 한 점을 랜덤 선택.
    /// maxRandomTries 안에 못 찾으면 false. (대개 Floor 영역 안에 있다면 1~2회 안에 성공)
    /// </summary>
    bool TryPickRandomFloorPoint(out Vector2 result)
    {
        Vector2 origin = transform.position;
        for (int i = 0; i < maxRandomTries; i++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle * moveRadius;
            if (IsOnFloor(candidate))
            {
                result = candidate;
                return true;
            }
        }
        result = default;
        return false;
    }

    /// <summary>지정 좌표가 Floor 레이어 collider 안에 들어가는지 검사.</summary>
    public bool IsOnFloor(Vector2 pos) => Physics2D.OverlapPoint(pos, floorLayer) != null;

    void ApplyFacingTowards(Vector2 target)
    {
        if (spriteRenderer == null) return;
        float dx = target.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.0001f) return;
        spriteRenderer.flipX = dx < 0f;
    }

    void SetMovingAnim(bool moving)
    {
        if (animator != null) animator.SetBool(IsMovingHash, moving);
    }
}
