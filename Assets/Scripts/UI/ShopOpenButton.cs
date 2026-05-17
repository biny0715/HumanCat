using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GNB 등에 붙여 "상점 열기" 클릭을 처리.
/// 가시성(Indoor 시에만 보이기) 은 부모에 붙은 <see cref="IndoorOnlyVisibility"/> 가 SetActive 로 관리한다.
/// 본 컴포넌트는 단순히 클릭 → ShopTrigger.ForceOpen() 만 책임진다.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShopOpenButton : MonoBehaviour
{
    [SerializeField] ShopTrigger target;

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(Open);

        // 배치 모드 / 편집 모드 중에는 상점 버튼 자동 비활성 (자기 자신 열림은 영향 X)
        if (GetComponent<ModeGatedButton>() == null)
        {
            var gate = gameObject.AddComponent<ModeGatedButton>();
            gate.SetBlockedBy(BlockingMode.Placement | BlockingMode.EditMode);
        }
    }

    void Open()
    {
        if (target == null) { Debug.LogWarning("[ShopOpenButton] target ShopTrigger 미할당"); return; }
        target.ForceOpen();
    }
}
