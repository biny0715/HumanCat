using UnityEngine;

/// <summary>
/// 고양이 NPC 의 식별 + 판매 처리.
///
/// [설계 의도]
/// - itemId 보관(저장 매칭 키) + Sell() API 만 담당. 클릭 감지는 CatNPCClickDispatcher 가 중앙 처리.
/// - CatNPCController(AI 이동) 와 분리.
/// - Sell() 은 재화 지급 + CatManager.RemoveCat + Destroy 일괄 처리.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CatNPC : MonoBehaviour
{
    [Tooltip("이 인스턴스가 어떤 CatItemData 에서 스폰됐는지 — Save/Load 시 매칭 키.")]
    [SerializeField] string itemId;

    public string ItemId => itemId;

    /// <summary>CatManager 가 스폰 직후 호출. itemId 주입.</summary>
    public void Setup(string id) => itemId = id;

    /// <summary>판매 — 재화 지급 + 저장 데이터 제거 + GameObject 파괴.</summary>
    public void Sell()
    {
        var data      = CatManager.Instance?.GetItem(itemId);
        int sellPrice = data != null ? Mathf.Max(0, data.FishPrice / 2) : 0;

        if (sellPrice > 0)
            CurrencyManager.Instance?.Add(CurrencyType.Fish, sellPrice);

        CatManager.Instance?.RemoveCat(this);
        Destroy(gameObject);
    }
}
