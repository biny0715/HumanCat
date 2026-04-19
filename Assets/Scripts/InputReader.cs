using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 소스(터치/마우스)를 추상화하여 이벤트로 전달.
/// PlayerMover에서 입력을 분리함으로써 각 클래스가 단일 책임을 갖는다.
/// 나중에 입력 방식이 바뀌어도 이 파일만 수정하면 된다.
/// </summary>
public class InputReader : MonoBehaviour
{
    // 화면 탭 위치를 스크린 좌표로 전달 (WorldPoint 변환은 소비자가 담당)
    public event Action<Vector2> OnTapPerformed;

    void Update()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            OnTapPerformed?.Invoke(touch.primaryTouch.position.ReadValue());
            return;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            OnTapPerformed?.Invoke(mouse.position.ReadValue());
    }
}
