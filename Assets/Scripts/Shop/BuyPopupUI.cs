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

    // 한 번에 한 종류만 표시 — 일반 또는 Cat
    ItemData    currentItem;
    CatItemData currentCatItem;
    Shop        currentShop;
    int         quantity = 1;

    void Awake()
    {
        if (minusButton   != null) minusButton.onClick.AddListener(OnMinus);
        if (plusButton    != null) plusButton.onClick.AddListener(OnPlus);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton  != null) cancelButton.onClick.AddListener(Hide);
        // Hide() 를 Awake 에서 호출하면 panel == self 인 경우 첫 활성 시 자기 자신을 즉시 비활성화 →
        // Show 의 SetActive(true) 가 무효화됨. 인스펙터에서 비활성으로 두면 게임 시작 시 안 보임.
    }

    public void Show(ItemData item, Shop shop)
    {
        if (item == null || shop == null) return;
        currentItem    = item;
        currentCatItem = null;
        currentShop    = shop;
        quantity       = 1;
        if (titleText != null) titleText.text = item.DisplayName;
        OpenPanel();
        Refresh();
    }

    /// <summary>Cat 전용 — 인벤토리 사용 안 함, 확인 시 Shop.BuyCat 호출.</summary>
    public void ShowCat(CatItemData cat, Shop shop)
    {
        if (cat == null || shop == null) return;
        currentItem    = null;
        currentCatItem = cat;
        currentShop    = shop;
        quantity       = 1;
        if (titleText != null) titleText.text = cat.DisplayName;
        OpenPanel();
        Refresh();
    }

    void OpenPanel()
    {
        if (panel == null) return;
        panel.SetActive(true);
        // 첫 활성 시 LayoutGroup 위치 지연으로 인한 첫 클릭 무시 회피
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.transform as RectTransform);
    }

    public void Hide()
    {
        currentItem    = null;
        currentCatItem = null;
        currentShop    = null;
        if (panel != null) panel.SetActive(false);
    }

    void OnPlus()
    {
        if (currentShop == null) return;
        if (!CanIncrement()) return;
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
        if (currentShop == null) { Hide(); return; }
        if      (currentItem    != null) currentShop.Buy(currentItem, quantity);
        else if (currentCatItem != null) currentShop.BuyCat(currentCatItem, quantity);
        Hide();
    }

    void Refresh()
    {
        if (currentShop == null) return;

        int unit;
        string unitName;
        bool plusOK, confirmOK;

        if (currentItem != null)
        {
            unit      = currentShop.GetPrice(currentItem);
            unitName  = currentShop.AcceptedCurrency == CurrencyType.Fish ? "피쉬" : "골드";
            plusOK    = currentShop.CanBuy(currentItem, quantity + 1) == Shop.BuyResult.Success;
            confirmOK = currentShop.CanBuy(currentItem, quantity)     == Shop.BuyResult.Success;
        }
        else if (currentCatItem != null)
        {
            unit      = currentShop.GetCatPrice(currentCatItem);
            unitName  = "피쉬"; // Cat 은 Fish 만
            plusOK    = false; // Cat 은 1마리 고정 — +/− 모두 비활성
            confirmOK = currentShop.CanBuyCat(currentCatItem, quantity) == Shop.BuyResult.Success;
        }
        else return;

        int total = unit * quantity;
        if (quantityText   != null) quantityText.text   = $"x{quantity}";
        if (totalPriceText != null) totalPriceText.text = $"{total:N0} {unitName}";

        // Cat 은 항상 quantity=1 고정이므로 minus/plus 둘 다 비활성. 일반 아이템은 기존 로직.
        bool isCat = currentCatItem != null;
        if (minusButton   != null) minusButton.interactable   = !isCat && quantity > 1;
        if (plusButton    != null) plusButton.interactable    = plusOK;
        if (confirmButton != null) confirmButton.interactable = confirmOK;
    }

    bool CanIncrement()
    {
        if      (currentItem    != null) return currentShop.CanBuy(currentItem, quantity + 1)       == Shop.BuyResult.Success;
        else if (currentCatItem != null) return currentShop.CanBuyCat(currentCatItem, quantity + 1) == Shop.BuyResult.Success;
        return false;
    }
}
