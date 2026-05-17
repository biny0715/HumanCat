using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 모드에서 인벤토리 아이템 클릭 시 뜨는 판매 확인 팝업.
/// 수량 조절(+/-) + 총 가격 + 확인/취소.
///
/// [규칙]
/// - 판매 단가 = ItemData 의 (현재 캐릭터 재화 기준 가격) / 2, 0 미만은 0.
///   Human=Gold, Cat=Fish 인벤토리이므로 그 쪽 가격을 절반으로 환불.
/// - 수량 상한 = 인벤토리 보유 수량 (GetCount).
/// - 확인 → 인벤토리에서 quantity 개 차감 + CurrencyManager.Add(unit × quantity).
/// </summary>
public class SellPopupUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text   titleText;
    [SerializeField] TMP_Text   quantityText;
    [SerializeField] TMP_Text   totalPriceText;

    [Header("Quantity Buttons")]
    [SerializeField] Button minusButton;
    [SerializeField] Button plusButton;

    [Header("Action Buttons")]
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;

    ItemData     currentItem;
    int          unitPrice;
    CurrencyType currency;
    int          quantity = 1;
    int          maxQuantity = 1;

    void Awake()
    {
        if (minusButton   != null) minusButton.onClick.AddListener(OnMinus);
        if (plusButton    != null) plusButton.onClick.AddListener(OnPlus);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton  != null) cancelButton.onClick.AddListener(Hide);
        Hide();
    }

    public void Show(ItemData item)
    {
        if (item == null) return;
        currentItem = item;
        quantity    = 1;

        var type = PlayerController.Instance != null ? PlayerController.Instance.CurrentType : PlayerType.Human;
        if (type == PlayerType.Human)
        {
            unitPrice = Mathf.Max(0, item.GoldPrice / 2);
            currency  = CurrencyType.Gold;
        }
        else
        {
            unitPrice = Mathf.Max(0, item.FishPrice / 2);
            currency  = CurrencyType.Fish;
        }

        maxQuantity = InventoryManager.Instance != null
            ? Mathf.Max(1, InventoryManager.Instance.GetCount(item.ItemId))
            : 1;

        if (titleText != null) titleText.text = item.DisplayName;
        if (panel     != null)
        {
            panel.SetActive(true);
            // 첫 활성 시 LayoutGroup 위치 지연으로 인한 첫 클릭 무시 회피
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.transform as RectTransform);
        }
        Refresh();
    }

    public void Hide()
    {
        currentItem = null;
        if (panel != null) panel.SetActive(false);
    }

    void OnPlus()
    {
        if (quantity >= maxQuantity) return;
        quantity++;
        Refresh();
    }

    void OnMinus()
    {
        if (quantity <= 1) return;
        quantity--;
        Refresh();
    }

    void OnConfirm()
    {
        if (currentItem == null) { Hide(); return; }
        var inv = InventoryManager.Instance;
        if (inv == null) { Hide(); return; }

        if (!inv.TryRemoveItem(currentItem.ItemId, quantity))
        {
            Debug.LogWarning($"[SellPopup] {currentItem.ItemId} x{quantity} 차감 실패");
            Hide();
            return;
        }
        int total = unitPrice * quantity;
        if (total > 0) CurrencyManager.Instance?.Add(currency, total);
        Hide();
    }

    void Refresh()
    {
        int total = unitPrice * quantity;
        string unitName = currency == CurrencyType.Fish ? "피쉬" : "골드";

        if (quantityText   != null) quantityText.text   = $"x{quantity}";
        if (totalPriceText != null) totalPriceText.text = $"{total:N0} {unitName}";

        if (minusButton != null) minusButton.interactable = quantity > 1;
        if (plusButton  != null) plusButton.interactable  = quantity < maxQuantity;
        if (confirmButton != null) confirmButton.interactable = quantity > 0 && quantity <= maxQuantity;
    }
}
