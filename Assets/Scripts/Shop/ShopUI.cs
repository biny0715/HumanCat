using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미리 생성된 정적 상점 패널.
///
/// [설계 의도]
/// - 게임 시작 시 (Awake) 단 한 번 stockList 기반으로 ShopItemRow 들을 자식으로 인스턴스화한다.
///   이후 트리거 시점엔 GameObject.SetActive(true/false) 만 — 패널 열기/닫기에 인스턴스 비용 0.
/// - 한 패널 = 한 Shop. 두 캐릭터를 위해 CatShop / HumanShop 두 ShopUI 를 둔다 (각자 자기 shop 만 신경 씀).
/// - ShopTrigger 의 정적 이벤트(OnShopOpenRequested) 를 구독해 자기 shop 이 요청되면 SetActive.
/// - 인스펙터에서 GameObject 가 비활성 상태로 있어도 동작해야 하므로, 부모 Bootstrap 이
///   Initialize() 를 호출하는 흐름으로 동작한다 (Awake 의존 제거).
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("이 패널이 표시할 Shop. 인스펙터에서 Shop_Human 또는 Shop_Cat 를 드래그.")]
    [SerializeField] Shop shop;

    [Header("Layout")]
    [SerializeField] Transform   itemListContent;
    [SerializeField] ShopItemRow itemRowPrefab;

    [Header("Optional Display")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text currencyHintText;
    [SerializeField] Button   closeButton;

    readonly List<ShopItemRow> rows = new();
    bool built;
    bool subscribed;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        Initialize();
    }

    /// <summary>외부(Bootstrap)에서 강제 초기화. 비활성 GameObject 도 동작.</summary>
    public void Initialize()
    {
        if (built) return;
        BuildRows();
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        Subscribe();
        if (titleText != null && shop != null) titleText.text = shop.ShopName;
    }

    void OnEnable()
    {
        Subscribe();
        UpdateCurrencyHint();
        RefreshRows();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (subscribed) return;
        ShopTrigger.OnShopOpenRequested  += HandleOpen;
        ShopTrigger.OnShopCloseRequested += HandleClose;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshRows;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        ShopTrigger.OnShopOpenRequested  -= HandleOpen;
        ShopTrigger.OnShopCloseRequested -= HandleClose;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshRows;
        subscribed = false;
    }

    // ── 사전 행 생성 ──────────────────────────────────────────────────────

    void BuildRows()
    {
        if (shop == null || itemRowPrefab == null || itemListContent == null)
        {
            Debug.LogWarning($"[ShopUI] '{name}' 슬롯 누락 — shop/content/rowPrefab 확인.");
            built = true;
            return;
        }

        foreach (var item in shop.StockList)
        {
            if (item == null) continue;
            var row = Instantiate(itemRowPrefab, itemListContent);
            row.Bind(item, shop);
            rows.Add(row);
        }
        built = true;
    }

    // ── 이벤트 처리 ──────────────────────────────────────────────────────

    void HandleOpen(Shop requested)
    {
        if (requested != shop) return;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        UpdateCurrencyHint();
        RefreshRows();
    }

    void HandleClose()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    public void Close() => HandleClose();

    void HandleCurrencyChanged(CurrencyType _, long __)
    {
        if (gameObject.activeInHierarchy) UpdateCurrencyHint();
        RefreshRows();
    }

    // ── 갱신 ─────────────────────────────────────────────────────────────

    void UpdateCurrencyHint()
    {
        if (currencyHintText == null || shop == null || CurrencyManager.Instance == null) return;
        long val   = CurrencyManager.Instance.Get(shop.AcceptedCurrency);
        string unit = shop.AcceptedCurrency == CurrencyType.Fish ? "Fish" : "Gold";
        currencyHintText.text = $"보유 {unit}: {val:N0}";
    }

    void RefreshRows()
    {
        foreach (var r in rows)
            if (r != null) r.Refresh();
    }
}
