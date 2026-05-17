using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일반(Standalone) 모드에서 인벤토리 아이템 클릭 시 뜨는 사용/배치 확인 팝업.
///
/// [규칙]
/// - 일반 소비 아이템: Use → 인벤토리 닫기. (현재는 구조만)
/// - 배치 가능 아이템(ItemData.Placeable && Human && Indoor):
///     · 제목: "xx을(를) 배치하시겠습니까?"
///     · Use 클릭 → 인벤토리 닫고 PlacementManager.TryBegin
/// - Cancel: 팝업만 닫고 인벤토리 유지
/// </summary>
public class UsePopupUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text   titleText;
    [SerializeField] TMP_Text   descText;

    [Header("Buttons")]
    [SerializeField] Button useButton;
    [SerializeField] Button cancelButton;

    ItemData     currentItem;
    InventoryUI  owner;

    void Awake()
    {
        if (useButton    != null) useButton.onClick.AddListener(OnUse);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        Hide();
    }

    public void Show(ItemData item, InventoryUI inventoryOwner)
    {
        if (item == null) return;
        currentItem = item;
        owner       = inventoryOwner;

        bool placeable = item.Placeable;
        if (titleText != null)
            titleText.text = placeable
                ? $"{item.DisplayName}을(를) 배치하시겠습니까?"
                : $"{item.DisplayName} 사용하기";

        if (descText != null)
            descText.text = string.IsNullOrEmpty(item.Description)
                ? (placeable ? "원하는 위치를 선택해 배치하세요." : "이 아이템을 사용하시겠습니까?")
                : item.Description;

        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        currentItem = null;
        owner       = null;
        if (panel != null) panel.SetActive(false);
    }

    void OnUse()
    {
        if (currentItem == null) { Hide(); return; }

        var item        = currentItem;
        var closingOwner = owner;
        Hide();
        if (closingOwner != null) closingOwner.Close();

        // 배치 가능 아이템이면 배치 모드 진입
        if (item.Placeable && PlacementManager.Instance != null)
        {
            bool ok = PlacementManager.Instance.TryBegin(item);
            if (!ok)
                Debug.Log("[UsePopup] 배치 조건 불충족 — Human 캐릭터 + Indoor 인지 확인");
        }
        // 일반 사용 아이템: 향후 효과(회복 등) 처리 지점
    }

    void Cancel() => Hide();
}
