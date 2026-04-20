using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오른쪽 방향 Arrow UI 애니메이션.
/// 수평 바운스 + 알파 펄스를 Sin파로 구동.
/// </summary>
[RequireComponent(typeof(Image))]
public class ArrowAnimator : MonoBehaviour
{
    [Header("Bounce")]
    [SerializeField] float bounceDistance = 18f;  // 오른쪽으로 이동할 최대 픽셀
    [SerializeField] float bounceSpeed    = 2.2f;  // 왕복 속도

    [Header("Fade")]
    [SerializeField] float alphaMin  = 0.35f;
    [SerializeField] float alphaMax  = 1.0f;
    [SerializeField] float fadeSpeed = 2.2f;       // bounceSpeed와 동기화하면 자연스러움

    Image            image;
    RectTransform    rt;
    Vector2          originPos;

    void Awake()
    {
        image     = GetComponent<Image>();
        rt        = GetComponent<RectTransform>();
        originPos = rt.anchoredPosition;
    }

    void OnEnable()
    {
        // 활성화될 때 원점 재설정 (위치가 변경됐을 경우 대비)
        originPos = rt.anchoredPosition;
    }

    void Update()
    {
        float t = Time.time;

        // 0 → 1 → 0 사이클: (sin + 1) / 2
        float cycle = (Mathf.Sin(t * bounceSpeed * Mathf.PI) + 1f) * 0.5f;

        // 바운스: 오른쪽으로 밀렸다가 복귀
        rt.anchoredPosition = originPos + new Vector2(cycle * bounceDistance, 0f);

        // 페이드: 멀어질수록 밝아짐 (앞으로 나아가는 느낌)
        float alpha = Mathf.Lerp(alphaMin, alphaMax,
            (Mathf.Sin(t * fadeSpeed * Mathf.PI) + 1f) * 0.5f);

        var c = image.color;
        c.a = alpha;
        image.color = c;
    }

    void OnDisable()
    {
        // 비활성화 시 원위치 복원
        if (rt != null) rt.anchoredPosition = originPos;
    }
}
