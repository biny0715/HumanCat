using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 비활성 상태로 둔 ShopUI 패널들의 사전 초기화를 담당.
///
/// [설계 의도]
/// - 사용자가 인스펙터에서 ShopUI 패널을 비활성으로 두고 싶어도, MonoBehaviour 의
///   Awake/Start 는 비활성 GameObject 에서 호출되지 않는다.
/// - 이 부트스트랩은 활성 GameObject 에 붙어 Start() 에서 panels[] 의 각 ShopUI 에
///   Initialize() 를 직접 호출 → 행 인스턴스화는 게임 시작 시 1회만 발생.
/// - 트리거가 발동되면 ShopUI 가 자체 SetActive(true) 로 즉시 표시 (인스턴스 비용 0).
///
/// [Prewarm 패턴]
/// - panel 을 한 번도 SetActive(true) 하지 않으면 자식 Button 들의 Awake 가 호출되지 않고
///   첫 활성 시 첫 클릭이 무시되는 Unity UI 의 알려진 현상이 발생.
/// - 해결: CanvasGroup 으로 가시/raycast 차단 후 SetActive(true) → 한 프레임 대기로 자식
///   Awake/Start/Layout 모두 완료 → SetActive(false) 로 복귀. 사용자 화면에는 안 보임.
/// </summary>
public class ShopUIBootstrap : MonoBehaviour
{
    [Tooltip("게임 시작 시 사전 초기화할 ShopUI 패널들 (비활성 상태여도 OK).")]
    [SerializeField] List<ShopUI> panels = new();

    void Start() => StartCoroutine(Bootstrap());

    IEnumerator Bootstrap()
    {
        // 1) 가시/raycast 차단한 채로 활성화 → 자식 Awake/Start 강제 호출
        var prepared = new List<CanvasGroup>();
        foreach (var p in panels)
        {
            if (p == null) continue;
            // Unity Object 의 fake-null 때문에 ?? 연산자 사용 금지 — TryGetComponent 로 명시 검사
            if (!p.TryGetComponent<CanvasGroup>(out var cg))
                cg = p.gameObject.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            prepared.Add(cg);
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
            p.Initialize();
        }

        // 2) 한 프레임 대기 — Awake/Start/Layout/Selectable 초기화 완료
        yield return null;

        // 3) 닫힌 상태로 복귀 + CanvasGroup 정상값 복원
        for (int i = 0; i < panels.Count; i++)
        {
            var p = panels[i];
            if (p == null) continue;
            if (p.gameObject.activeSelf) p.gameObject.SetActive(false);
        }
        foreach (var cg in prepared)
        {
            if (cg == null) continue;
            cg.alpha          = 1f;
            cg.blocksRaycasts = true;
        }
    }
}
