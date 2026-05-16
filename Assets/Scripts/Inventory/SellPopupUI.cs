using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 모드에서 인벤토리 아이템 클릭 시 뜨는 판매 확인 팝업.
///
/// [규칙]
/// - 판매 가격 = ItemData 의 (현재 캐릭터 재화 기준 가격) / 2, 0 미만은 0.
///   Human=Gold, Cat=Fish 인벤토리이므로 그 쪽 가격을 절반으로 환불.
/// - 확인 → 인벤토리에서 1개 차감 + CurrencyManager.Add.
/// - 취소 → 닫기만.
/// </summary>
public class SellPopupUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text   titleText;
    [SerializeField] TMP_Text   priceText;

    [Header("Buttons")]
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;

    ItemData     currentItem;
    int          sellPrice;
    CurrencyType currency;

    void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton  != null) cancelButton.onClick.AddListener(Cancel);
        Hide();
    }

    public void Show(ItemData item)
    {
        if (item == null) return;
        currentItem = item;

        var type = PlayerController.Instance != null ? PlayerController.Instance.CurrentType : PlayerType.Human;
        if (type == PlayerType.Human)
        {
            sellPrice = Mathf.Max(0, item.GoldPrice / 2);
            currency  = CurrencyType.Gold;
        }
        else
        {
            sellPrice = Mathf.Max(0, item.FishPrice / 2);
            currency  = CurrencyType.Fish;
        }

        if (titleText != null) titleText.text = item.DisplayName;
        if (priceText != null)
        {
            string unit = currency == CurrencyType.Fish ? "피쉬" : "골드";
            priceText.text = $"{sellPrice:N0} {unit}";
        }
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        currentItem = null;
        if (panel != null) panel.SetActive(false);
    }

    void Confirm()
    {
        if (currentItem == null) { Hide(); return; }
        var inv = InventoryManager.Instance;
        if (inv == null) { Hide(); return; }

        if (!inv.TryRemoveItem(currentItem.ItemId, 1))
        {
            Debug.LogWarning($"[SellPopup] {currentItem.ItemId} 차감 실패");
            Hide();
            return;
        }
        if (sellPrice > 0) CurrencyManager.Instance?.Add(currency, sellPrice);
        Hide();
    }

    void Cancel() => Hide();
}
