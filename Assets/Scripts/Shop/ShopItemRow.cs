using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 패널에서 한 아이템을 표시하는 행 (UI).
///
/// [설계 의도]
/// - ShopUI 가 stockList 순회하며 이 프리팹을 인스턴스화.
/// - Bind(item, shop) 로 데이터를 받고, Buy 버튼은 즉시 구매 대신
///   OnBuyRequested(item) 이벤트만 발행한다. ShopUI 가 구독해서 BuyPopup 을 띄움.
/// - 재화 부족/인벤토리 가득 등 구매 불가 상태면 buyButton 비활성화로 표현.
/// </summary>
public class ShopItemRow : MonoBehaviour
{
    [SerializeField] Image    iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text priceText;
    [SerializeField] Button   buyButton;

    public event Action<ItemData>    OnBuyRequested;
    public event Action<CatItemData> OnBuyCatRequested;

    // 한 row 는 일반(item) 또는 Cat(catItem) 중 하나만 바인딩. 다른 한쪽은 null.
    ItemData    item;
    CatItemData catItem;
    Shop        shop;

    /// <summary>일반 아이템 바인딩.</summary>
    public void Bind(ItemData itemData, Shop shopRef)
    {
        item    = itemData;
        catItem = null;
        shop    = shopRef;

        if (iconImage != null) iconImage.sprite = item.Icon;
        if (nameText  != null) nameText.text    = item.DisplayName;

        AttachClickListener();
        Refresh();
    }

    /// <summary>Cat 아이템 바인딩 — InventoryManager 흐름과 분리.</summary>
    public void BindCat(CatItemData catItemData, Shop shopRef)
    {
        item    = null;
        catItem = catItemData;
        shop    = shopRef;

        if (iconImage != null) iconImage.sprite = catItem.Icon;
        if (nameText  != null) nameText.text    = catItem.DisplayName;

        AttachClickListener();
        Refresh();
    }

    void AttachClickListener()
    {
        if (buyButton == null) return;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void Refresh()
    {
        if (shop == null) return;

        if (item != null)
        {
            if (priceText != null)
            {
                string unit = shop.AcceptedCurrency == CurrencyType.Fish ? "F" : "G";
                priceText.text = $"{shop.GetPrice(item):N0} {unit}";
            }
            if (buyButton != null)
                buyButton.interactable = shop.CanBuy(item, 1) == Shop.BuyResult.Success;
        }
        else if (catItem != null)
        {
            if (priceText != null)
                priceText.text = $"{shop.GetCatPrice(catItem):N0} F";
            if (buyButton != null)
                buyButton.interactable = shop.CanBuyCat(catItem, 1) == Shop.BuyResult.Success;
        }
    }

    void OnBuyClicked()
    {
        if (shop == null) return;
        if      (item    != null) OnBuyRequested?.Invoke(item);       // ShopUI 가 BuyPopup.Show 로 위임
        else if (catItem != null) OnBuyCatRequested?.Invoke(catItem); // ShopUI 가 BuyPopup.ShowCat 로 위임
    }
}
