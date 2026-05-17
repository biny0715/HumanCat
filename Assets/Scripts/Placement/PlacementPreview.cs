using UnityEngine;

/// <summary>
/// 배치 모드에서 placementPrefab 인스턴스에 부착되는 컴포넌트.
///
/// [규칙]
/// - PlacementManager 가 placementPrefab 을 Instantiate 한 뒤 이 컴포넌트를 자동 부착.
/// - 자식의 모든 SpriteRenderer 의 원본 색상을 캐싱하고, valid/invalid 는 살짝 tint + 반투명만 적용.
///   → prefab 의 실제 디테일이 그대로 보임. 단색으로 덮지 않는다.
/// - 충돌 검사 size 는 자식 SpriteRenderer 들의 결합 bounds 로 계산해 실제 가구 크기와 일치.
/// </summary>
public class PlacementPreview : MonoBehaviour
{
    static readonly Color ValidTint   = new Color(0.4f, 1.0f, 0.4f, 1f);
    static readonly Color InvalidTint = new Color(1.0f, 0.3f, 0.3f, 1f);
    const float TintStrength = 0.35f;   // 0=원본 그대로, 1=완전히 tint
    const float PreviewAlpha = 0.85f;

    SpriteRenderer[] renderers;
    Color[]          originalColors;

    public bool IsValid { get; private set; }

    /// <summary>PlacementManager 가 Instantiate 직후 호출. 자식 SR 과 원본 색 캐싱.</summary>
    public void Initialize()
    {
        renderers      = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        SetValid(true);
    }

    public void SetValid(bool valid)
    {
        IsValid = valid;
        if (renderers == null) return;
        Color tint = valid ? ValidTint : InvalidTint;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            Color orig    = originalColors[i];
            Color blended = Color.Lerp(orig, tint, TintStrength);
            blended.a     = orig.a * PreviewAlpha;
            r.color = blended;
        }
    }

    /// <summary>충돌 검사용 월드 사이즈 — 모든 자식 SpriteRenderer 결합 bounds.</summary>
    public Vector2 GetWorldSize() => GetCombinedBounds(out var b) ? (Vector2)b.size : Vector2.one;

    /// <summary>
    /// 충돌 검사용 월드 중심 — sprite 결합 bounds 의 center.
    /// transform.position 은 prefab 의 root pivot (보통 발 위치) 이라서 sprite 중심과 다를 수 있다.
    /// </summary>
    public Vector2 GetWorldCenter() => GetCombinedBounds(out var b) ? (Vector2)b.center : (Vector2)transform.position;

    bool GetCombinedBounds(out Bounds combined)
    {
        combined = default;
        if (renderers == null || renderers.Length == 0) return false;
        bool init = false;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (!init) { combined = r.bounds; init = true; }
            else combined.Encapsulate(r.bounds);
        }
        return init;
    }
}
