using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GNB 등에 붙여 "인벤토리 열기" 클릭을 InventoryUI.OpenStandalone() 으로 연결.
/// UnityEvent 직접 연결 대신 컴포넌트로 두는 이유: Editor 메뉴에서 슬롯 자동 연결이 쉽다.
/// </summary>
[RequireComponent(typeof(Button))]
public class InventoryOpenButton : MonoBehaviour
{
    [SerializeField] InventoryUI target;

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(Open);

        // 배치 모드 / 편집 모드 / 상점 열림 중에는 인벤토리 버튼 자동 비활성
        if (GetComponent<ModeGatedButton>() == null)
        {
            var gate = gameObject.AddComponent<ModeGatedButton>();
            gate.SetBlockedBy(BlockingMode.Placement | BlockingMode.EditMode | BlockingMode.Shop);
        }
    }

    void Open()
    {
        if (target != null) target.OpenStandalone();
    }
}
