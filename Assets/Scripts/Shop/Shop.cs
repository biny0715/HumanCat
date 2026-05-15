using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Indoor 오브젝트에 붙이는 상점 컴포넌트.
///
/// [설계 의도]
/// - 상점은 "판매 목록 + 구매 결제 로직"만 담당. UI는 별도 컴포넌트가
///   StockList를 읽고 Buy를 호출하는 구조 (관심사 분리).
/// - 결제는 트랜잭션 형태: 재화/슬롯 검사 → 통과 시 일괄 적용. 부분 실패 없음.
/// - 결과는 BuyResult 열거형으로 반환 → UI 에서 case 별 메시지를 띄우기 쉬움.
/// - OnPurchased / OnPurchaseFailed 이벤트로 효과음·연출 훅 가능.
///
/// 사용 예:
///   var result = shop.Buy(item, count: 1);
///   if (result == Shop.BuyResult.Success) PlayBuyFx();
///   else ShowMessage(result);
/// </summary>
[DisallowMultipleComponent]
public class Shop : MonoBehaviour
{
    public enum BuyResult
    {
        Success,
        UnknownItem,
        NotInStock,
        InsufficientCurrency,
        InventoryFull,
    }

    [Header("Stock")]
    [Tooltip("이 상점이 판매하는 아이템 목록. ItemData 자산을 드래그.")]
    [SerializeField] List<ItemData> stockList = new List<ItemData>();

    [Header("Currency")]
    [Tooltip("이 상점이 받는 재화. Human 상점=Gold, Cat 상점=Fish 등.")]
    [SerializeField] CurrencyType acceptedCurrency = CurrencyType.Gold;

    [Header("Optional Display")]
    [Tooltip("상점 이름. UI 표기용.")]
    [SerializeField] string shopName = "상점";

    public string                  ShopName         => shopName;
    public IReadOnlyList<ItemData> StockList        => stockList;
    public CurrencyType            AcceptedCurrency => acceptedCurrency;

    /// <summary>이 상점에서의 아이템 가격 (acceptedCurrency 기준).</summary>
    public int GetPrice(ItemData item)
    {
        if (item == null) return 0;
        return acceptedCurrency == CurrencyType.Fish ? item.FishPrice : item.GoldPrice;
    }

    public event Action<ItemData, int>            OnPurchased;
    public event Action<ItemData, int, BuyResult> OnPurchaseFailed;

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    /// <summary>이 아이템이 이 상점에서 판매 중인지.</summary>
    public bool Carries(ItemData item) => item != null && stockList.Contains(item);

    /// <summary>구매 가능 여부만 미리 점검 (UI 비활성화 판단용).</summary>
    public BuyResult CanBuy(ItemData item, int count = 1)
    {
        if (item == null || count <= 0) return BuyResult.UnknownItem;
        if (!Carries(item))              return BuyResult.NotInStock;

        var cm = CurrencyManager.Instance;
        var im = InventoryManager.Instance;
        if (cm == null || im == null)    return BuyResult.UnknownItem;

        long total = (long)GetPrice(item) * count;
        if (cm.Get(acceptedCurrency) < total)   return BuyResult.InsufficientCurrency;
        if (!im.CanAddItem(item.ItemId, count)) return BuyResult.InventoryFull;
        return BuyResult.Success;
    }

    /// <summary>구매 실행. 트랜잭션 — 실패 시 어떤 값도 변경되지 않는다.</summary>
    public BuyResult Buy(ItemData item, int count = 1)
    {
        var pre = CanBuy(item, count);
        if (pre != BuyResult.Success)
        {
            OnPurchaseFailed?.Invoke(item, count, pre);
            return pre;
        }

        var cm = CurrencyManager.Instance;
        var im = InventoryManager.Instance;
        long total = (long)GetPrice(item) * count;

        if (total > 0) cm.TrySubtract(acceptedCurrency, total);
        im.TryAddItem(item.ItemId, count);

        OnPurchased?.Invoke(item, count);
        return BuyResult.Success;
    }
}
