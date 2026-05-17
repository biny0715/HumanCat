using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 터치/마우스 입력을 추상화하여 이벤트로 전달.
///
/// [설계 의도]
/// - 입력 소스(터치/마우스/게임패드 등)가 바뀌어도 이 파일만 수정하면 된다.
/// - 스크린 좌표를 그대로 전달한다. 월드 좌표 변환은 소비자(PlayerController)가 담당.
///   → InputReader가 Camera를 알 필요가 없으므로 결합도가 낮아진다.
/// - 터치 우선: 실기기(터치) 환경에서 마우스 입력이 중복 발생하지 않도록 early return.
/// </summary>
public class InputReader : MonoBehaviour
{
    /// <summary>탭/클릭 발생 시 스크린 좌표를 전달.</summary>
    public event Action<Vector2> OnTapPerformed;

    static readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>();

    void Update()
    {
        if (TryReadTouch(out Vector2 pos) || TryReadMouse(out pos))
        {
            if (!IsPointerOverUI(pos))
                OnTapPerformed?.Invoke(pos);
        }
    }

    /// <summary>
    /// 직접 GraphicRaycaster 호출로 매번 정확히 검사.
    /// EventSystem 내부 hover 캐시(IsPointerOverGameObject) 는 프레임 갱신 순서에 따라
    /// 첫 클릭에 false 를 반환하는 경우가 있어 직접 RaycastAll 로 회피한다.
    /// </summary>
    static bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        raycastBuffer.Clear();
        EventSystem.current.RaycastAll(data, raycastBuffer);
        return raycastBuffer.Count > 0;
    }

    static bool TryReadTouch(out Vector2 screenPos)
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = touch.primaryTouch.position.ReadValue();
            return true;
        }
        screenPos = default;
        return false;
    }

    static bool TryReadMouse(out Vector2 screenPos)
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPos = mouse.position.ReadValue();
            return true;
        }
        screenPos = default;
        return false;
    }
}
