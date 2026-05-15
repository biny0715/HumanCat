using System;
using UnityEngine;

/// <summary>
/// 플레이어가 가까이 가면 ShopUI 를 띄우는 근접 트리거.
///
/// [설계 의도]
/// - Indoor 가구 오브젝트에 붙여 "다가가면 상점이 열리는" 패턴.
/// - 두 상점(Human/Cat)을 참조하고 진입한 플레이어의 PlayerController.CurrentType 으로 분기.
///   캐릭터별로 받는 재화가 다르므로 (Human=Gold, Cat=Fish) 두 Shop 컴포넌트는 별도 자식.
/// - Trigger 진입/이탈 이벤트만 발행하고, 실제 UI 표시는 ShopUI 가 구독한다 (관심사 분리).
/// - [RequireComponent(Collider2D)] — 콜라이더는 isTrigger 로 강제 설정한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopTrigger : MonoBehaviour
{
    [Header("Shops")]
    [Tooltip("인간 캐릭터일 때 열릴 상점 (Gold 재화).")]
    [SerializeField] Shop humanShop;
    [Tooltip("고양이 캐릭터일 때 열릴 상점 (Fish 재화).")]
    [SerializeField] Shop catShop;

    Collider2D     col;
    PlayerController activePlayer;
    Shop             currentShop;

    /// <summary>(shop) — 상점이 열려야 할 때.</summary>
    public static event Action<Shop> OnShopOpenRequested;

    /// <summary>플레이어가 영역을 벗어났을 때.</summary>
    public static event Action OnShopCloseRequested;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc == null) return;
        activePlayer = pc;
        OpenForCurrentType();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc == null || pc != activePlayer) return;
        activePlayer = null;
        currentShop  = null;
        OnShopCloseRequested?.Invoke();
    }

    void OpenForCurrentType()
    {
        if (activePlayer == null) return;
        currentShop = activePlayer.CurrentType == PlayerType.Human ? humanShop : catShop;
        if (currentShop == null)
        {
            Debug.LogWarning($"[ShopTrigger] {activePlayer.CurrentType} 용 상점이 비어있음 ({name})");
            return;
        }
        OnShopOpenRequested?.Invoke(currentShop);
    }
}
