using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 아이템 구매 확인 팝업. 수량 조절(+/-) + 총 가격 + 확인/취소.
///
/// [설계 의도]
/// - ShopItemRow 의 구매 버튼은 이 팝업을 띄우기만 한다 (즉시 구매 안 함).
/// - 수량은 1 부터 시작. + 버튼은 Shop.CanBuy(item, quantity+1)이 Success 일 때만 활성.
///   - 자동으로 재화 부족 / 인벤토리 가득 한도 반영.
/// - 확인 버튼은 현재 수량으로 CanBuy 가능할 때만 활성.
/// - 총 가격 = unitPrice × quantity, "X 골드" / "X 피쉬" 형식.
/// </summary>
public class BuyPopupUI : MonoBehaviour
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

    ItemData currentItem;
    Shop     currentShop;
    int      quantity = 1;

    void Awake()
    {
        if (minusButton   != null) minusButton.onClick.AddListener(OnMinus);
        if (plusButton    != null) plusButton.onClick.AddListener(OnPlus);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton  != null) cancelButton.onClick.AddListener(Hide);
        Hide();
    }

    public void Show(ItemData item, Shop shop)
    {
        if (item == null || shop == null) return;
        currentItem = item;
        currentShop = shop;
        quantity    = 1;
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
        currentShop = null;
        if (panel != null) panel.SetActive(false);
    }

    void OnPlus()
    {
        if (currentItem == null || currentShop == null) return;
        if (currentShop.CanBuy(currentItem, quantity + 1) != Shop.BuyResult.Success) return;
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
        if (currentItem == null || currentShop == null) { Hide(); return; }
        currentShop.Buy(currentItem, quantity);
        Hide();
    }

    void Refresh()
    {
        if (currentItem == null || currentShop == null) return;
        int unit  = currentShop.GetPrice(currentItem);
        int total = unit * quantity;
        string unitName = currentShop.AcceptedCurrency == CurrencyType.Fish ? "피쉬" : "골드";

        if (quantityText   != null) quantityText.text   = $"x{quantity}";
        if (totalPriceText != null) totalPriceText.text = $"{total:N0} {unitName}";

        if (minusButton   != null) minusButton.interactable = quantity > 1;
        if (plusButton    != null) plusButton.interactable  = currentShop.CanBuy(currentItem, quantity + 1) == Shop.BuyResult.Success;
        if (confirmButton != null) confirmButton.interactable = currentShop.CanBuy(currentItem, quantity) == Shop.BuyResult.Success;
    }
}
