using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 패널에서 한 슬롯을 표시하는 행 (UI).
///
/// [설계 의도]
/// - InventoryUI 가 stockList 대신 InventorySlot 을 순회해 이 프리팹을 인스턴스화 후 Bind.
/// - 행 전체를 Button 으로 만들어 클릭 시 OnClicked(slot) 발행 — InventoryUI 가 받아서
///   현재 모드(Shop=판매 팝업 / Standalone=사용 팝업)에 따라 분기.
/// - 가격 텍스트 자리에 보유 수량(x5)을 표시한다.
/// </summary>
public class InventoryItemRow : MonoBehaviour
{
    [SerializeField] Image    iconImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text countText;
    [SerializeField] Button   rowButton;

    public event Action<InventorySlot> OnClicked;

    InventorySlot boundSlot;

    void Awake()
    {
        if (rowButton != null)
            rowButton.onClick.AddListener(() => OnClicked?.Invoke(boundSlot));
    }

    public void Bind(InventorySlot slot)
    {
        boundSlot = slot;
        var def = InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(slot.itemId) : null;
        if (iconImage != null) iconImage.sprite = def != null ? def.Icon : null;
        if (nameText  != null) nameText.text    = def != null ? def.DisplayName : slot.itemId;
        if (countText != null) countText.text   = $"x{slot.count}";
    }
}
