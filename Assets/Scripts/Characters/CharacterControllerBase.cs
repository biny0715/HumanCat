using UnityEngine;

/// <summary>
/// Cat / Human 캐릭터 컨트롤러의 공통 추상 베이스.
/// 각 타입의 이동 속도, Animator Controller 등을 캡슐화하고
/// PlayerController가 타입에 무관하게 동일한 인터페이스로 조작할 수 있도록 한다.
/// </summary>
public abstract class CharacterControllerBase : MonoBehaviour
{
    [SerializeField] protected RuntimeAnimatorController animatorController;
    [SerializeField] protected float moveSpeed = 5f;

    protected PlayerMover   Mover   { get; private set; }
    protected PlayerAnimator Anim   { get; private set; }

    /// <summary>PlayerController가 활성화 시점에 의존성을 주입.</summary>
    public void Initialize(PlayerMover mover, PlayerAnimator anim)
    {
        Mover = mover;
        Anim  = anim;

        Mover.SetMoveSpeed(moveSpeed);
        Anim.SetController(animatorController);

        OnActivate();
    }

    public void Deactivate() => OnDeactivate();

    // 서브클래스에서 활성화/비활성화 시 추가 동작 정의
    protected virtual void OnActivate()   { }
    protected virtual void OnDeactivate() { }
}
