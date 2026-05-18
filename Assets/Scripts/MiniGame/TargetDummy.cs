using UnityEngine;

/// <summary>
/// 플레이어를 피해 달아나는 유기묘 AI.
///
/// - 도망 방향 = (자신 - 플레이어) + 랜덤 ±30°
/// - 주변 장애물 감지 → 회피 스티어링 합산
/// - 장애물 회피 중 속도가 ×1.5까지 점점 빨라지고, 회피 끝나면 점점 원래 속도로 복귀
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TargetDummy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float fleeSpeed         = 3f;
    [SerializeField] float dirChangeInterval = 2f;
    [SerializeField] float reactionDistance  = 4f;

    [Header("Obstacle Avoidance")]
    [SerializeField] float avoidRadius       = 2.5f;  // 장애물 감지 반경
    [SerializeField] float avoidStrength     = 4f;    // 회피 힘 크기
    [SerializeField] float speedBoostMax     = 1.5f;  // 회피 시 최대 속도 배율
    [SerializeField] float speedChangeRate   = 2f;    // 속도 배율 변화 속도 (초당)

    [Header("Stuck Recovery")]
    [Tooltip("이 간격마다 이동량을 확인해 stuck 여부 판단.")]
    [SerializeField] float stuckCheckInterval = 0.4f;
    [Tooltip("위 시간동안 이만큼도 이동 못 하면 stuck 으로 간주.")]
    [SerializeField] float stuckMoveThreshold = 0.25f;
    [Tooltip("Stuck 탈출 시 임의 방향으로 강제 유지하는 시간.")]
    [SerializeField] float stuckEscapeDuration = 0.6f;

    [Header("References")]
    [SerializeField] Transform player;

    Rigidbody2D    rb;
    Animator       anim;
    SpriteRenderer sr;
    Vector2        fleeDir;
    float          dirTimer;
    float          speedMult = 1f;
    bool           running   = false;
    bool           facingLeft = true;

    Vector2 lastStuckCheckPos;
    float   stuckCheckTimer;
    float   escapeTimer;   // > 0 이면 player 추적 무시하고 escapeDir 유지

    static readonly int IsRunning = Animator.StringToHash("isMoving");

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Awake()
    {
        rb               = GetComponent<Rigidbody2D>();
        anim             = GetComponent<Animator>();
        sr               = GetComponent<SpriteRenderer>();
        rb.gravityScale  = 0f;
        rb.freezeRotation = true;
    }

    void Start() => RefreshFleeDir();

    // ── 물리 업데이트 ─────────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (!running)
        {
            rb.linearVelocity = Vector2.zero;
            if (anim) anim.SetBool(IsRunning, false);
            return;
        }

        if (player == null) return;

        dirTimer    += Time.fixedDeltaTime;
        escapeTimer  = Mathf.Max(0f, escapeTimer - Time.fixedDeltaTime);

        // Escape 중이 아닐 때만 player 기반 방향 갱신
        if (escapeTimer <= 0f)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            float interval = dist < reactionDistance ? Mathf.Min(dirChangeInterval, 0.8f) : dirChangeInterval;
            if (dirTimer >= interval)
            {
                dirTimer = 0f;
                RefreshFleeDir();
            }
        }

        // 장애물 회피 스티어링
        Vector2 avoidance  = CalcAvoidance();
        bool    isAvoiding = avoidance.sqrMagnitude > 0.01f;

        // Stuck 감지: 장애물 옆에서 거의 안 움직이면 임의 방향으로 탈출
        stuckCheckTimer += Time.fixedDeltaTime;
        if (stuckCheckTimer >= stuckCheckInterval)
        {
            if (escapeTimer <= 0f)
            {
                float moved = Vector2.Distance((Vector2)transform.position, lastStuckCheckPos);
                if (isAvoiding && moved < stuckMoveThreshold)
                {
                    BeginEscape(avoidance);
                }
            }
            lastStuckCheckPos = transform.position;
            stuckCheckTimer   = 0f;
        }

        // 속도 배율 점진적 변화
        float targetMult = isAvoiding ? speedBoostMax : 1f;
        speedMult = Mathf.MoveTowards(speedMult, targetMult, speedChangeRate * Time.fixedDeltaTime);

        // Escape 중엔 회피 합산을 줄이고 fleeDir(임의 방향) 우선
        Vector2 finalDir = escapeTimer > 0f
            ? (fleeDir + avoidance * 0.3f).normalized
            : (fleeDir + avoidance).normalized;
        rb.linearVelocity = finalDir * (fleeSpeed * speedMult);

        if (anim) anim.SetBool(IsRunning, true);
        UpdateFacing(finalDir);
    }

    /// <summary>Stuck 감지 시 호출 — 장애물 합력의 수직 방향으로 임의 우/좌 선택해 탈출 시도.</summary>
    void BeginEscape(Vector2 avoidance)
    {
        // 1순위: 장애물 합력에 수직(좌/우 랜덤) — 좁은 통로에서 빠져나가기 좋다.
        Vector2 baseDir = avoidance.sqrMagnitude > 0.01f
            ? new Vector2(-avoidance.y, avoidance.x).normalized
            : Random.insideUnitCircle.normalized;
        if (Random.value < 0.5f) baseDir = -baseDir;

        // 2순위: 그래도 너무 작으면 완전 임의
        if (baseDir.sqrMagnitude < 0.01f)
            baseDir = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.right;

        fleeDir     = baseDir;
        escapeTimer = stuckEscapeDuration;
        dirTimer    = 0f;
    }

    // ── 내부 ──────────────────────────────────────────────────────────────

    Vector2 CalcAvoidance()
    {
        var     hits  = Physics2D.OverlapCircleAll(transform.position, avoidRadius);
        Vector2 steer = Vector2.zero;

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<Obstacle>() == null) continue;

            Vector2 away = (Vector2)transform.position - (Vector2)col.transform.position;
            float   dist = away.magnitude;
            if (dist < 0.01f) continue;

            // 가까울수록 강하게 밀어냄
            float strength = (avoidRadius - dist) / avoidRadius;
            steer += away.normalized * strength * avoidStrength;
        }

        return steer;
    }

    void UpdateFacing(Vector2 dir)
    {
        if (sr == null) return;
        // 히스테리시스 — 현재 방향과 반대로 충분히 가야만 flip. 좌우 진동 시 잦은 깜빡임 방지.
        const float threshold = 0.5f;
        if      (facingLeft  && dir.x >  threshold) facingLeft = false;
        else if (!facingLeft && dir.x < -threshold) facingLeft = true;
        sr.flipX = facingLeft;
    }

    void RefreshFleeDir()
    {
        if (player == null) return;
        Vector2 awayDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
        if (awayDir == Vector2.zero) awayDir = Vector2.right;
        float angle = Random.Range(-30f, 30f);
        fleeDir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * awayDir);
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public void SetRunning(bool value)
    {
        running = value;
        if (!value)
        {
            rb.linearVelocity = Vector2.zero;
            if (anim) anim.SetBool(IsRunning, false);
        }
    }

    public void SetPlayer(Transform playerTransform) => player = playerTransform;
    public void SetFleeSpeed(float speed)            => fleeSpeed = speed;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, avoidRadius);
    }
#endif
}
