using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>모드별 비트 플래그. 한 버튼이 여러 모드에 동시 블록될 수 있음.</summary>
[Flags]
public enum BlockingMode
{
    None      = 0,
    Placement = 1 << 0, // 가구 배치 모드 활성 중
    EditMode  = 1 << 1, // 편집(Edit) 모드 활성 중
    Shop      = 1 << 2, // 상점 UI 열림
}

/// <summary>
/// GNB 등의 Button 을 특정 모드 활성 중에 자동으로 interactable=false 로 토글한다.
///
/// [설계 의도]
/// - 매 프레임 Update 에서 모드 상태를 폴링 — 이벤트 구독 race(Instance 가 늦게 생성되는 경우) 회피.
///   비교 비용이 매우 가벼우므로 GNB 버튼 3~4개 정도면 영향 없음.
/// - Shop 열림 상태만 ShopTrigger 의 정적 이벤트로 추적 (Shop 열림/닫힘은 매 프레임 폴링할 게터가 없음).
///   ShopTrigger 가 OnShopOpenRequested / OnShopCloseRequested 만 노출하므로 정적 캐시로 관리.
/// - 값이 실제로 바뀔 때만 button.interactable 을 set — 동일 값 setter 호출 회피.
/// </summary>
[RequireComponent(typeof(Button))]
public class ModeGatedButton : MonoBehaviour
{
    [Tooltip("이 모드들이 활성화되어 있을 때 버튼이 interactable=false 가 된다. " +
             "여러 비트 동시 선택 가능 (Mask Field).")]
    [SerializeField] BlockingMode blockedBy = BlockingMode.Placement | BlockingMode.EditMode | BlockingMode.Shop;

    Button button;
    bool   lastBlock;
    bool   initialized;

    // 정적 캐시 — Shop 은 어떤 trigger 가 발화했든 단일 상태로 본다.
    static bool shopOpen;
    static bool staticSubscribed;

    /// <summary>코드에서 동적으로 블록 모드 변경 (예: 자동 부착 시 케이스별 마스크 지정).</summary>
    public void SetBlockedBy(BlockingMode mode) => blockedBy = mode;

    void Awake()
    {
        button = GetComponent<Button>();
        EnsureStaticSubscribed();
    }

    static void EnsureStaticSubscribed()
    {
        if (staticSubscribed) return;
        ShopTrigger.OnShopOpenRequested  += _  => shopOpen = true;
        ShopTrigger.OnShopCloseRequested += () => shopOpen = false;
        staticSubscribed = true;
    }

    void Update()
    {
        if (button == null) return;

        bool block = false;
        if ((blockedBy & BlockingMode.Placement) != 0)
            block |= PlacementManager.Instance != null && PlacementManager.Instance.IsActive;
        if ((blockedBy & BlockingMode.EditMode) != 0)
            block |= EditModeController.Instance != null && EditModeController.Instance.IsEditMode;
        if ((blockedBy & BlockingMode.Shop) != 0)
            block |= shopOpen;

        if (!initialized || block != lastBlock)
        {
            button.interactable = !block;
            lastBlock           = block;
            initialized         = true;
        }
    }
}
