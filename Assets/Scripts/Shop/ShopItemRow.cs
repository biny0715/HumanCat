using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 패널에서 한 아이템을 표시하는 행 (UI).
///
/// [설계 의도]
/// - ShopUI 가 stockList 순회하며 이 프리팹을 인스턴스화.
/// - Bind(item, shop, ui) 로 데이터를 받고, Buy 버튼은 shop.Buy() 호출 후 ui.Refresh().
/// - 재화 부족/인벤토리 가득 등 구매 불가 상태면 buyButton 비활성화로 표현.
/// </summary>
public class ShopItemRow : MonoBehaviour
{
    [SerializeField] Image    iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text priceText;
    [SerializeField] Button   buyButton;

    ItemData item;
    Shop     shop;

    /// <summary>아이템과 소속 상점을 바인딩. Refresh 는 외부 이벤트(재화/인벤토리 변경)로 갱신된다.</summary>
    public void Bind(ItemData itemData, Shop shopRef)
    {
        item = itemData;
        shop = shopRef;

        if (iconImage != null) iconImage.sprite = item.Icon;
        if (nameText  != null) nameText.text    = item.DisplayName;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (shop == null || item == null) return;

        if (priceText != null)
        {
            string unit = shop.AcceptedCurrency == CurrencyType.Fish ? "F" : "G";
            priceText.text = $"{shop.GetPrice(item):N0} {unit}";
        }
        if (buyButton != null)
            buyButton.interactable = shop.CanBuy(item, 1) == Shop.BuyResult.Success;
    }

    void OnBuyClicked()
    {
        if (shop == null || item == null) return;
        shop.Buy(item, 1);
        // CurrencyChanged / InventoryChanged 이벤트로 owner 가 Refresh 됨
    }
}
