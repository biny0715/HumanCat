using UnityEngine;

/// <summary>
/// 순수 물리 이동만 담당. 입력/애니메이션과 완전히 분리.
/// 외부에서 MoveTo()를 호출하면 목표 지점까지 이동하고 도착 시 자동 정지.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMover : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float stopDistance = 0.1f;

    public bool IsMoving       { get; private set; }
    public Vector2 MoveDirection { get; private set; }

    Rigidbody2D rb;
    Vector2 targetPosition;

    // stopDistance를 제곱해서 캐싱 → FixedUpdate마다 곱셈 1회 절약
    float stopDistanceSqr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        targetPosition    = rb.position;
        stopDistanceSqr   = stopDistance * stopDistance;
    }

    /// <summary>CharacterController가 캐릭터 타입별 속도를 주입할 때 사용.</summary>
    public void SetMoveSpeed(float speed) => moveSpeed = speed;

    /// <summary>목표 월드 좌표로 이동 시작.</summary>
    public void MoveTo(Vector2 worldPosition)
    {
        targetPosition = worldPosition;
        IsMoving = true;
    }

    /// <summary>즉시 정지 (상태 전환, 씬 변경 시 사용).</summary>
    public void Stop()
    {
        IsMoving     = false;
        MoveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (!IsMoving) return;

        Vector2 dir = targetPosition - rb.position;

        // sqrMagnitude 비교로 sqrt 연산 제거
        if (dir.sqrMagnitude <= stopDistanceSqr)
        {
            rb.MovePosition(targetPosition);
            Stop();
            return;
        }

        MoveDirection = dir.normalized;
        rb.MovePosition(rb.position + MoveDirection * moveSpeed * Time.fixedDeltaTime);
    }
}
