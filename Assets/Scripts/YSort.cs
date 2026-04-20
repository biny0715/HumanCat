using UnityEngine;

/// <summary>
/// Y축 위치 기반으로 SpriteRenderer.sortingOrder를 매 프레임 갱신한다.
///
/// [동작 원리]
/// sortingOrder = baseOrder - round(position.y × scale)
/// → Y가 낮을수록(화면 아래) 높은 order → 앞에 그려짐
///
/// [사용 조건]
/// - Pivot이 오브젝트 바닥 기준이어야 Y값이 정확히 발 위치를 나타낸다.
/// - SpriteRenderer가 이 컴포넌트와 같은 GameObject 또는 직계 자식에 있어야 한다.
/// - 같은 Sorting Layer 내에서만 순서가 보장된다.
///
/// [권장 설정]
/// - Sorting Layer: "Object" (별도 레이어 추천)
/// - baseOrder: 0
/// - sortScale: 100 (1 유닛 = 100 order 간격)
/// </summary>
public class YSort : MonoBehaviour
{
    [Tooltip("정렬 기준값. 여러 레이어 그룹을 구분할 때 사용.")]
    [SerializeField] int baseOrder = 0;

    [Tooltip("Y 좌표 → sortingOrder 변환 배율. 클수록 세밀하게 정렬됨.")]
    [SerializeField] float sortScale = 100f;

    SpriteRenderer sr;

    void Awake()
    {
        // 자기 자신 또는 직계 자식에서 SpriteRenderer를 찾는다
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (sr == null)
            Debug.LogWarning($"[YSort] SpriteRenderer를 찾지 못했습니다: {gameObject.name}");
    }

    // Update 대신 LateUpdate 사용 — 이동 처리가 끝난 뒤 정렬
    void LateUpdate()
    {
        if (sr == null) return;
        sr.sortingOrder = baseOrder - Mathf.RoundToInt(transform.position.y * sortScale);
    }

#if UNITY_EDITOR
    // 에디터에서도 실시간 미리보기
    void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = baseOrder - Mathf.RoundToInt(transform.position.y * sortScale);
    }
#endif
}
