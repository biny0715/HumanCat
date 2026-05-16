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
/// - 영역 안에 플레이어가 있는 동안 GameManager.OnStateChanged 가 발생하면, 새 캐릭터
///   타입에 맞는 상점으로 자동 전환한다 (Human↔Cat 변경 시 패널 즉시 교체).
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

    Collider2D       col;
    PlayerController activePlayer;
    Shop             currentShop;

    /// <summary>(shop) — 상점이 열려야 할 때.</summary>
    public static event Action<Shop> OnShopOpenRequested;

    /// <summary>플레이어가 영역을 벗어났거나 강제로 닫혔을 때.</summary>
    public static event Action OnShopCloseRequested;

    /// <summary>버튼/외부에서 닫기를 요청 (모든 ShopUI / InventoryUI 가 닫힘).</summary>
    public static void RequestCloseAll() => OnShopCloseRequested?.Invoke();

    /// <summary>버튼 등 외부에서 강제로 상점을 연다. 영역 진입 없이 동작.</summary>
    public void ForceOpen()
    {
        if (activePlayer == null) activePlayer = PlayerController.Instance;
        if (activePlayer == null)
        {
            Debug.LogWarning($"[ShopTrigger] ForceOpen 실패 — PlayerController 없음 ({name})");
            return;
        }
        OpenForCurrentType();
    }

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
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

    /// <summary>플레이어가 영역 안에 있을 때 Day/Night 가 바뀌면 상점도 같이 전환.</summary>
    void HandleStateChanged(GameState _)
    {
        if (activePlayer == null) return;
        OpenForCurrentType();
    }

    void OpenForCurrentType()
    {
        if (activePlayer == null) return;
        var target = activePlayer.CurrentType == PlayerType.Human ? humanShop : catShop;
        if (target == null)
        {
            Debug.LogWarning($"[ShopTrigger] {activePlayer.CurrentType} 용 상점이 비어있음 ({name})");
            return;
        }
        currentShop = target;
        OnShopOpenRequested?.Invoke(currentShop);
    }
}
