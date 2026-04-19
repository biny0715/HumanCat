using UnityEngine;

/// <summary>
/// 애니메이션 상태 제어만 담당.
/// PlayerMover를 직접 참조하지 않고 PlayerController가 데이터를 주입한다.
/// Cat/Human 모두 "isMoving" 파라미터를 공유하므로 타입 분기 없이 동일하게 처리.
/// 타입별 다른 동작이 필요하면 SetAnimatorController()로 Controller만 교체한다.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    static readonly int HashIsMoving = Animator.StringToHash("isMoving");

    Animator         anim;
    SpriteRenderer   sr;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr   = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// CharacterController가 활성화될 때 타입에 맞는 Controller를 교체.
    /// 이 방식으로 Cat ↔ Human 전환 시 Animator 재생성 없이 Controller만 바꾼다.
    /// </summary>
    public void SetController(RuntimeAnimatorController controller)
    {
        if (anim.runtimeAnimatorController == controller) return;
        anim.runtimeAnimatorController = controller;
    }

    /// <summary>매 프레임 PlayerController가 호출하여 상태를 동기화.</summary>
    public void Tick(bool isMoving, Vector2 moveDirection)
    {
        anim.SetBool(HashIsMoving, isMoving);
        UpdateFacing(moveDirection);
    }

    // 스프라이트 원본이 왼쪽을 바라보는 기준
    void UpdateFacing(Vector2 direction)
    {
        if (direction.x < 0f)      sr.flipX = false;
        else if (direction.x > 0f) sr.flipX = true;
    }
}
