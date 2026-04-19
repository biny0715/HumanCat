using UnityEngine;

/// <summary>
/// Cat / Human 컨트롤러의 공통 추상 베이스.
///
/// [설계 의도]
/// - 타입별로 달라지는 값(속도, AnimatorController, 스프라이트 방향)을
///   Inspector에서 설정하고, Initialize() 시점에 PlayerMover/PlayerAnimator에 주입한다.
/// - PlayerController는 이 인터페이스만 알면 된다. Cat/Human 세부 구현을 몰라도 된다. (OCP)
/// - OnActivate/OnDeactivate: 서브클래스가 활성화 시점에 추가 동작을 정의하는 훅.
///   (예: 고양이 대시 쿨다운 리셋, 인간 인벤토리 UI 열기 등)
/// </summary>
public abstract class CharacterControllerBase : MonoBehaviour
{
    [SerializeField] RuntimeAnimatorController animatorController;
    [SerializeField] float moveSpeed       = 5f;

    // 스프라이트 원본 방향: 고양이(true=오른쪽), 인간(false=왼쪽)
    [SerializeField] bool  spriteFacingRight = false;

    protected PlayerMover   Mover { get; private set; }
    protected PlayerAnimator Anim { get; private set; }

    /// <summary>
    /// PlayerController가 타입 전환 시 호출. 의존성을 주입하고 활성화한다.
    /// </summary>
    public void Initialize(PlayerMover mover, PlayerAnimator anim)
    {
        Mover = mover;
        Anim  = anim;

        Mover.SetMoveSpeed(moveSpeed);

        if (animatorController != null)
            Anim.SetController(animatorController);

        Anim.SetFacingRight(spriteFacingRight);

        OnActivate();
    }

    /// <summary>비활성화 시 호출. 진행 중인 동작을 정리한다.</summary>
    public void Deactivate() => OnDeactivate();

    protected virtual void OnActivate()   { }
    protected virtual void OnDeactivate() { }
}
