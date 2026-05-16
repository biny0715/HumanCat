using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일반(Standalone) 모드에서 인벤토리 아이템 클릭 시 뜨는 사용 확인 팝업.
///
/// [규칙]
/// - 사용 → 인벤토리 UI 닫고 입력 복귀(UIBlocker.Release 는 InventoryUI.OnDisable 이 처리).
///   Placeable 아이템은 향후 "배치 모드" 진입 지점으로 확장 예정 (현재는 구조만).
/// - 취소 → 팝업만 닫고 인벤토리는 유지.
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
        if (titleText != null) titleText.text = $"{item.DisplayName} 사용하기";
        if (descText  != null) descText.text  = string.IsNullOrEmpty(item.Description)
            ? (item.Placeable ? "배치할 수 있는 아이템입니다." : "이 아이템을 사용하시겠습니까?")
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

        // 배치 가능 아이템이면 향후 BeginPlacement 호출 지점. 현재는 단순 닫기.
        // TODO: PlacementController.Instance?.BeginPlacement(currentItem);

        var closingOwner = owner;
        Hide();
        if (closingOwner != null) closingOwner.Close();
    }

    void Cancel() => Hide();
}
