using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerMover))]
public class PlayerAnimator : MonoBehaviour
{
    Animator anim;
    SpriteRenderer sr;
    PlayerMover mover;

    static readonly int IsMoving = Animator.StringToHash("isMoving");

    void Awake()
    {
        anim  = GetComponent<Animator>();
        sr    = GetComponent<SpriteRenderer>();
        mover = GetComponent<PlayerMover>();
    }

    void Update()
    {
        anim.SetBool(IsMoving, mover.IsMoving);

        if (mover.MoveDirection.x < 0f) sr.flipX = true;
        else if (mover.MoveDirection.x > 0f) sr.flipX = false;
    }
}
