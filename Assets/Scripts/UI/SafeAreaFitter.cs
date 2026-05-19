using UnityEngine;

/// <summary>
/// RectTransform 을 Screen.safeArea 에 맞춰 자동 조정.
///
/// [사용법]
/// - Canvas 바로 아래에 빈 RectTransform("SafeArea") 을 만들고 이 스크립트를 붙인다.
/// - 모든 UI 자식을 그 SafeArea 아래로 옮기면 노치/홈 인디케이터 영역을 자동 회피.
///
/// [설계 의도]
/// - anchor 를 정규화된 safeArea 로 설정, offset 은 0 으로 → 화면 회전/디바이스 변경에도 즉시 반영.
/// - Update 에서 매 프레임 비교하지만 변경 시에만 적용 → 성능 부담 미미.
/// - Game 뷰는 safeArea 가 전체 화면이라 효과 안 보임. Device Simulator 로 확인.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafeArea = new Rect(0, 0, 0, 0);
    Vector2Int lastScreenSize = Vector2Int.zero;
    ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (ShouldRefresh()) Apply();
    }

    bool ShouldRefresh()
    {
        return Screen.safeArea != lastSafeArea
            || Screen.width  != lastScreenSize.x
            || Screen.height != lastScreenSize.y
            || Screen.orientation != lastOrientation;
    }

    void Apply()
    {
        if (rt == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;
        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        lastSafeArea    = safe;
        lastScreenSize  = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;
    }
}
