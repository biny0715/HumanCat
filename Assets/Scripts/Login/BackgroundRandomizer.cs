using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 패널 활성화(또는 외부 Apply 호출)될 때마다 후보 sprite 중 하나를 랜덤 선택해 배경 Image 에 적용.
///
/// [설계 의도]
/// - LoginRoot / NameInputRoot 같은 화면에서 공용으로 쓸 수 있도록 별도 컴포넌트로 분리.
/// - AspectRatioFitter.EnvelopeParent 와 같이 쓰면 화면을 꽉 채우고(letterbox 없음) 가장자리만 crop.
///   sprite 가 바뀌면 aspectRatio 도 즉시 재계산.
/// - Inspector 드래그 워크플로우: candidates 만 직접 채우면 끝.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BackgroundRandomizer : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] Image             targetImage;
    [SerializeField] AspectRatioFitter aspectFitter;

    [Header("Candidates")]
    [Tooltip("패널이 활성화될 때마다 이 중 하나가 랜덤으로 선택된다. Inspector 에서 직접 드래그.")]
    public Sprite[] candidates;

    void Reset()
    {
        targetImage  = GetComponentInChildren<Image>(true);
        aspectFitter = GetComponentInChildren<AspectRatioFitter>(true);
    }

    void OnEnable() => Apply();

    /// <summary>현재 후보 중 하나를 랜덤 선택해 적용. 외부에서 강제로 갱신하고 싶을 때 호출.</summary>
    public void Apply()
    {
        if (targetImage == null || candidates == null || candidates.Length == 0) return;

        int idx = Random.Range(0, candidates.Length);
        var sp  = candidates[idx];
        if (sp == null) return;

        targetImage.sprite = sp;
        if (aspectFitter != null)
            aspectFitter.aspectRatio = sp.rect.width / sp.rect.height;
    }
}
