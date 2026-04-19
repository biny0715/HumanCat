using UnityEngine;

public enum PlayerType { Cat, Human }

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerMover))]
public class PlayerAnimator : MonoBehaviour
{
    public PlayerType playerType = PlayerType.Cat;

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

        if (mover.MoveDirection.x < 0f)      sr.flipX = false;
        else if (mover.MoveDirection.x > 0f) sr.flipX = true;
    }
}
