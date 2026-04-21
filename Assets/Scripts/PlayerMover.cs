using System;
using UnityEngine;

/// <summary>
/// 클릭 이동. Rigidbody2D(Dynamic) + Collider2D로 장애물 충돌 차단.
/// 물리 엔진이 충돌을 처리하므로 별도 회피 로직 없음.
///
/// [경계 제한]
/// SetBounds()로 배경 크기에 맞게 설정.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed    = 5f;
    [SerializeField] float stopDistance = 0.15f;

    [Header("Bounds")]
    [SerializeField] bool    useBounds = false;
    [SerializeField] Vector2 boundsMin;
    [SerializeField] Vector2 boundsMax;

    /// <summary>최종 목적지 도착 시 발행.</summary>
    public event Action OnArrived;

    public bool    IsMoving      { get; private set; }
    public Vector2 MoveDirection { get; private set; }

    [Header("Stuck Detection")]
    [SerializeField] float stuckThreshold = 0.02f;  // 이 거리 미만이면 막힌 것으로 판단
    [SerializeField] float stuckTimeout   = 0.4f;   // 이 시간(초) 이상 막히면 정지

    Rigidbody2D rb;
    Vector2     targetPosition;
    float       stopDistanceSqr;
    Vector2     lastPosition;
    float       stuckTimer;

    void Awake()
    {
        rb              = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        targetPosition  = rb.position;
        stopDistanceSqr = stopDistance * stopDistance;
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public float MoveSpeed                => moveSpeed;
    public void  SetMoveSpeed(float speed) => moveSpeed = speed;

    public void MoveTo(Vector2 worldPosition)
    {
        targetPosition = worldPosition;
        IsMoving       = true;
        lastPosition   = rb.position;
        stuckTimer     = 0f;
    }

    public void Stop()
    {
        IsMoving          = false;
        MoveDirection     = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>배경 크기 기반 이동 영역 설정. SceneController에서 호출.</summary>
    public void SetBounds(Bounds bounds)
    {
        useBounds = true;
        boundsMin = bounds.min;
        boundsMax = bounds.max;
    }

    // ── FixedUpdate ───────────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (!IsMoving)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = targetPosition - rb.position;

        if (toTarget.sqrMagnitude <= stopDistanceSqr)
        {
            Stop();
            OnArrived?.Invoke();
            return;
        }

        // 막힘 감지: 일정 시간 동안 이동량이 너무 작으면 정지
        float moved = (rb.position - lastPosition).magnitude;
        if (moved < stuckThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTimeout) { Stop(); return; }
        }
        else
        {
            stuckTimer   = 0f;
            lastPosition = rb.position;
        }

        Vector2 dir = toTarget.normalized;
        Vector2 vel = dir * moveSpeed;

        if (useBounds)
        {
            Vector2 nextPos = rb.position + vel * Time.fixedDeltaTime;
            nextPos = ClampToBounds(nextPos);
            if (nextPos.x == boundsMin.x || nextPos.x == boundsMax.x) vel.x = 0f;
            if (nextPos.y == boundsMin.y || nextPos.y == boundsMax.y) vel.y = 0f;
        }

        MoveDirection     = dir;
        rb.linearVelocity = vel;
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    Vector2 ClampToBounds(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x),
            Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.green;
            var center = new Vector3((boundsMin.x + boundsMax.x) * 0.5f,
                                     (boundsMin.y + boundsMax.y) * 0.5f, 0f);
            var size   = new Vector3(boundsMax.x - boundsMin.x,
                                     boundsMax.y - boundsMin.y, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
}
