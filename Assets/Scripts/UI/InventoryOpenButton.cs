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
    }

    void Open()
    {
        if (target != null) target.OpenStandalone();
    }
}
