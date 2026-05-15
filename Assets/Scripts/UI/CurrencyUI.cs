using TMPro;
using UnityEngine;

/// <summary>
/// Main 씬에서 Fish / Gold 재화를 TMP_Text 로 표시.
///
/// [설계 의도]
/// - CurrencyManager.OnCurrencyChanged 이벤트를 구독하여 값 변경 시 자동 갱신.
///   (Update 폴링 없음 → 모바일 부담 최소)
/// - OnEnable 에서 즉시 1회 Refresh → 씬 진입/오브젝트 활성화 직후에도 올바른 값 표시.
/// - CurrencyManager 가 아직 없으면(예: 부트스트랩 누락) 텍스트는 0으로 표시되고 경고 로그.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] TMP_Text fishCoinTxt;
    [SerializeField] TMP_Text goldCoinTxt;

    void OnEnable()
    {
        var cm = CurrencyManager.Instance;
        if (cm == null)
        {
            Debug.LogWarning("[CurrencyUI] CurrencyManager 인스턴스가 없습니다. 씬에 매니저를 배치했는지 확인하세요.");
            ApplyText(fishCoinTxt, 0);
            ApplyText(goldCoinTxt, 0);
            return;
        }

        cm.OnCurrencyChanged += HandleCurrencyChanged;
        Refresh();
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    /// <summary>외부에서 강제 갱신이 필요할 때 호출.</summary>
    public void Refresh()
    {
        var cm = CurrencyManager.Instance;
        if (cm == null) return;
        ApplyText(fishCoinTxt, cm.Fish);
        ApplyText(goldCoinTxt, cm.Gold);
    }

    void HandleCurrencyChanged(CurrencyType type, long value)
    {
        switch (type)
        {
            case CurrencyType.Fish: ApplyText(fishCoinTxt, value); break;
            case CurrencyType.Gold: ApplyText(goldCoinTxt, value); break;
        }
    }

    static void ApplyText(TMP_Text target, long value)
    {
        if (target == null) return;
        target.text = value.ToString("N0");   // 1,000 / 10,000,000 형식
    }
}
