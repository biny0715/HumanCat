using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMover : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float stopDistance = 0.1f;

    Rigidbody2D rb;
    Vector2 targetPosition;
    public bool IsMoving { get; private set; }
    public Vector2 MoveDirection { get; private set; }
    bool isMoving;
    Camera cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        cam = Camera.main;
        targetPosition = rb.position;
    }

    void Update()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            SetTarget(touchscreen.primaryTouch.position.ReadValue());
            return;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            SetTarget(mouse.position.ReadValue());
        }
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        Vector2 dir = targetPosition - rb.position;
        if (dir.magnitude <= stopDistance)
        {
            rb.MovePosition(targetPosition);
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
            IsMoving = false;
            MoveDirection = Vector2.zero;
            return;
        }

        MoveDirection = dir.normalized;
        rb.MovePosition(rb.position + dir.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    void SetTarget(Vector2 screenPos)
    {
        targetPosition = cam.ScreenToWorldPoint(screenPos);
        isMoving = true;
        IsMoving = true;
    }
}
