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

    static readonly int IsRunning = Animator.StringToHash("isRunning");

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

        dirTimer += Time.fixedDeltaTime;
        float dist = Vector2.Distance(transform.position, player.position);
        float interval = dist < reactionDistance ? Mathf.Min(dirChangeInterval, 0.8f) : dirChangeInterval;
        if (dirTimer >= interval)
        {
            dirTimer = 0f;
            RefreshFleeDir();
        }

        // 장애물 회피 스티어링
        Vector2 avoidance  = CalcAvoidance();
        bool    isAvoiding = avoidance.sqrMagnitude > 0.01f;

        // 속도 배율 점진적 변화
        float targetMult = isAvoiding ? speedBoostMax : 1f;
        speedMult = Mathf.MoveTowards(speedMult, targetMult, speedChangeRate * Time.fixedDeltaTime);

        Vector2 finalDir = (fleeDir + avoidance).normalized;
        rb.linearVelocity = finalDir * (fleeSpeed * speedMult);

        if (anim) anim.SetBool(IsRunning, true);
        UpdateFacing(finalDir);
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
        if      (dir.x < -0.35f) facingLeft = true;
        else if (dir.x >  0.35f) facingLeft = false;
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
