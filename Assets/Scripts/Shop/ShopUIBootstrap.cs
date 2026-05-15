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
/// </summary>
public class ShopUIBootstrap : MonoBehaviour
{
    [Tooltip("게임 시작 시 사전 초기화할 ShopUI 패널들 (비활성 상태여도 OK).")]
    [SerializeField] List<ShopUI> panels = new();

    void Start()
    {
        foreach (var p in panels)
        {
            if (p == null) continue;
            p.Initialize();
            // 부트스트랩 직후엔 닫힌 상태로 유지.
            if (p.gameObject.activeSelf) p.gameObject.SetActive(false);
        }
    }
}
