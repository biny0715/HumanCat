using UnityEngine;

/// <summary>
/// 자유 이동 미니게임 플레이어.
/// 탭/클릭 위치로 PlayerMover를 통해 이동한다.
/// HP 시스템: TakeDamage() → MiniGameManager 통보.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MiniPlayerMover))]
[RequireComponent(typeof(InputReader))]
public class MiniGamePlayer : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] int maxHP = 100;   // StatManager로 덮어씀

    [Header("Speed Debuff")]
    [SerializeField] float hitSpeedMult    = 0.5f;   // 충돌 직후 속도 배율
    [SerializeField] float speedRecovRate  = 0.4f;   // 초당 회복량 (0.4 → 약 1.25초 완전 회복)

    public int   HP        { get; private set; }
    public float HPPercent => (float)HP / maxHP;

    MiniPlayerMover mover;
    InputReader     input;
    PlayerAnimator  playerAnim;
    bool            running   = false;  // StartGame() 전까지 이동 불가
    float           baseSpeed;
    float           speedMult = 1f;

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Awake()
    {
        mover      = GetComponent<MiniPlayerMover>();
        input      = GetComponent<InputReader>();
        playerAnim = GetComponent<PlayerAnimator>();
        HP         = maxHP;
        baseSpeed  = mover.MoveSpeed;
    }

    void Start()
    {
        if (playerAnim != null) playerAnim.SetFacingRight(true);
        input.OnTapPerformed += OnTap;
        ApplyStats();
    }

    /// <summary>StatManager 스탯을 HP/속도/저항에 반영. StartGame() 직전 호출.</summary>
    public void ApplyStats()
    {
        var sm = StatManager.Instance;
        if (sm == null) return;

        maxHP     = sm.ComputedMaxHP;
        HP        = maxHP;
        mover.ApplyStats();
        baseSpeed = mover.MoveSpeed;

        // hitSpeedMult를 스탯 기반으로 덮어씀
        hitSpeedMult = sm.ComputedHitMult;
    }

    void Update()
    {
        if (playerAnim != null)
            playerAnim.Tick(mover.IsMoving, mover.MoveDirection);

        // 속도 서서히 회복
        if (speedMult < 1f)
        {
            speedMult = Mathf.MoveTowards(speedMult, 1f, speedRecovRate * Time.deltaTime);
            mover.SetMoveSpeed(baseSpeed * speedMult);
        }
    }

    void OnDestroy()
    {
        if (input != null)
            input.OnTapPerformed -= OnTap;
    }

    // ── 입력 처리 ─────────────────────────────────────────────────────────

    void OnTap(Vector2 screenPos)
    {
        if (!running) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        mover.MoveTo(new Vector2(worldPos.x, worldPos.y));
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        if (!running) return;
        HP = Mathf.Max(0, HP - amount);

        // 속도 즉시 50%로 감소
        speedMult = hitSpeedMult;
        mover.SetMoveSpeed(baseSpeed * speedMult);

        MiniGameManager.Instance?.OnPlayerDamaged(HP, HPPercent);
    }

    public void ResetHP()
    {
        HP        = maxHP;
        speedMult = 1f;
        mover.SetMoveSpeed(baseSpeed);
    }

    public void SetRunning(bool value)
    {
        running = value;
        if (!value) mover.Stop();
    }
}
