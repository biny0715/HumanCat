using System.Collections;
using UnityEngine;

/// <summary>
/// 비활성 상태의 InventoryUI 를 게임 시작 시 한 번 사전 초기화한다.
///
/// [설계 의도]
/// - InventoryUI 패널을 인스펙터에서 비활성으로 두면 Awake/Start 가 호출되지 않는다.
/// - 이 부트스트랩은 활성 GameObject 에 붙어 Start() 에서 panel.Initialize() 를 직접 호출
///   → 행 풀 / 이벤트 구독이 1회 완료된 뒤 패널은 비활성 상태로 돌아간다.
/// - ShopUIBootstrap 과 별도로 둔 이유: 타입이 다르고, 인벤토리 패널은 단일 인스턴스라
///   List 가 필요 없다.
///
/// [Prewarm 패턴]
/// - CanvasGroup 으로 가시/raycast 차단 후 SetActive(true) → 한 프레임 → SetActive(false).
///   자식 Button 들의 Awake/Start/Selectable 초기화 강제 → 첫 활성 시 첫 클릭 무시 회피.
/// </summary>
public class InventoryUIBootstrap : MonoBehaviour
{
    [Tooltip("게임 시작 시 사전 초기화할 InventoryUI 패널 (비활성 상태여도 OK).")]
    [SerializeField] InventoryUI panel;

    void Start() => StartCoroutine(Bootstrap());

    IEnumerator Bootstrap()
    {
        if (panel == null) yield break;

        var cg = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
        cg.alpha          = 0f;
        cg.blocksRaycasts = false;
        if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
        panel.Initialize();

        yield return null;

        if (panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
        cg.alpha          = 1f;
        cg.blocksRaycasts = true;
    }
}
