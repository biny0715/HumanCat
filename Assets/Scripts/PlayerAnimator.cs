using UnityEngine;

/// <summary>
/// Animator 파라미터 제어와 스프라이트 방향 전환만 담당.
///
/// [설계 의도]
/// - PlayerController가 Tick()을 매 프레임 호출하지만,
///   실제 Animator.SetBool은 값이 바뀔 때만 호출한다. (불필요한 native 호출 방지)
/// - Cat ↔ Human 전환 시 RuntimeAnimatorController만 교체한다.
///   Animator 컴포넌트 자체를 재생성하지 않으므로 전환이 빠르다.
/// - spriteFacingRight: 스프라이트 원본 방향이 캐릭터마다 다르므로
///   CharacterControllerBase에서 주입받는다. (PlayerAnimator가 캐릭터 종류를 알 필요 없음)
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    static readonly int HashIsMoving = Animator.StringToHash("isMoving");

    Animator       anim;
    SpriteRenderer sr;
    bool           spriteFacingRight;
    bool           lastIsMoving;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr   = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 캐릭터 전환 시 Animator Controller를 교체한다.
    /// 동일 Controller면 교체를 건너뛰어 불필요한 재초기화를 방지한다.
    /// </summary>
    public void SetController(RuntimeAnimatorController controller)
    {
        if (controller == null || anim.runtimeAnimatorController == controller) return;
        anim.runtimeAnimatorController = controller;
    }

    /// <summary>
    /// 스프라이트의 원본 방향을 설정한다.
    /// true = 스프라이트가 오른쪽을 바라봄 (고양이),
    /// false = 스프라이트가 왼쪽을 바라봄 (인간).
    /// </summary>
    public void SetFacingRight(bool facingRight) => spriteFacingRight = facingRight;

    /// <summary>
    /// PlayerController가 매 프레임 호출. 변경분만 Animator에 전달한다.
    /// </summary>
    public void Tick(bool isMoving, Vector2 moveDirection)
    {
        if (isMoving != lastIsMoving)
        {
            anim.SetBool(HashIsMoving, isMoving);
            lastIsMoving = isMoving;
        }

        if (isMoving)
            UpdateFacing(moveDirection);
    }

    void UpdateFacing(Vector2 direction)
    {
        if      (direction.x < 0f) sr.flipX =  spriteFacingRight;
        else if (direction.x > 0f) sr.flipX = !spriteFacingRight;
    }
}
