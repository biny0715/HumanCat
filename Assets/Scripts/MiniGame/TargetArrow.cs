using UnityEngine;

/// <summary>
/// TargetDummy가 화면 밖으로 나갔을 때 화면 가장자리에 방향 화살표를 표시한다.
/// - Playing 상태에서만 활성
/// - 뷰포트 [margin, 1-margin] 안에 들어오면 자동으로 숨김
/// </summary>
public class TargetArrow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform     target;       // TargetDummy Transform
    [SerializeField] RectTransform arrowRT;      // 화살표 UI RectTransform

    [Header("Settings")]
    [SerializeField] float edgePadding  = 60f;   // 화면 가장자리 여백(px)
    [SerializeField] float visibleMargin = 0.05f; // 이 범위 안에 들어오면 보이는 것으로 판단

    Camera cam;

    void Awake() => cam = Camera.main;

    void Update()
    {
        if (arrowRT == null || target == null) return;

        // Playing 상태가 아니면 숨김
        if (MiniGameManager.Instance == null ||
            MiniGameManager.Instance.State != MiniGameState.Playing)
        {
            arrowRT.gameObject.SetActive(false);
            return;
        }

        Vector3 vp = cam.WorldToViewportPoint(target.position);
        bool isVisible = vp.z > 0
            && vp.x >= visibleMargin && vp.x <= 1f - visibleMargin
            && vp.y >= visibleMargin && vp.y <= 1f - visibleMargin;

        arrowRT.gameObject.SetActive(!isVisible);
        if (!isVisible) PlaceArrow();
    }

    void PlaceArrow()
    {
        // 화면 상 위치 계산 (뒤에 있으면 반전)
        Vector3 screenPos = cam.WorldToScreenPoint(target.position);
        if (screenPos.z < 0f) screenPos = -screenPos;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir    = ((Vector2)screenPos - center);
        if (dir == Vector2.zero) dir = Vector2.up;
        dir.Normalize();

        // 화면 절반 크기에서 패딩 적용
        float hw = center.x - edgePadding;
        float hh = center.y - edgePadding;

        // dir를 화면 경계까지 스케일
        float scaleX = Mathf.Abs(dir.x) > 0.001f ? hw / Mathf.Abs(dir.x) : float.MaxValue;
        float scaleY = Mathf.Abs(dir.y) > 0.001f ? hh / Mathf.Abs(dir.y) : float.MaxValue;
        float scale  = Mathf.Min(scaleX, scaleY);

        arrowRT.position = new Vector3(center.x + dir.x * scale, center.y + dir.y * scale, 0f);

        // 화살표 회전 (스프라이트가 위쪽(↑)을 기준으로 함)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowRT.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetTarget(Transform t) => target = t;
}
