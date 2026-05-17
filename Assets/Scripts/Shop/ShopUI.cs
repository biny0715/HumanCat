using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미리 생성된 정적 상점 패널 (페이징 지원).
///
/// [설계 의도]
/// - 게임 시작 시 itemsPerPage 만큼의 ShopItemRow 만 사전 인스턴스화한다.
///   페이지 변경 시 같은 행에 다음 페이지 아이템을 Bind → 인스턴스 비용 0.
/// - 한 패널 = 한 Shop. CatShop / HumanShop 두 ShopUI 가 각자 자기 shop 만 신경 씀.
/// - ShopTrigger 의 정적 이벤트(OnShopOpenRequested) 를 구독해 매칭되는 shop 일 때만 SetActive.
/// - 페이지 끝/처음에서 Prev/Next 버튼은 자동으로 SetActive(false) 처리한다.
/// - Bootstrap 이 비활성 패널에서도 Initialize() 를 직접 호출하므로 Awake 의존성 제거.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("이 패널이 표시할 Shop. 인스펙터에서 Shop_Human 또는 Shop_Cat 를 드래그.")]
    [SerializeField] Shop shop;

    [Header("Layout")]
    [SerializeField] Transform   itemListContent;
    [SerializeField] ShopItemRow itemRowPrefab;
    [Tooltip("한 페이지에 표시할 아이템 수. 인스턴스화도 이 수만큼만 한다.")]
    [SerializeField] int itemsPerPage = 6;

    [Header("Optional Display")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text currencyHintText;
    [SerializeField] Button   closeButton;

    [Header("Paging")]
    [SerializeField] Button prevButton;
    [SerializeField] Button nextButton;
    [SerializeField] TMP_Text pageIndicatorText; // 선택 — "1 / 5"

    [Header("Popup")]
    [SerializeField] BuyPopupUI buyPopup;

    readonly List<ShopItemRow> pool = new();
    int  currentPage;
    bool built;
    bool subscribed;

    int PageCount => shop != null && itemsPerPage > 0
        ? Mathf.Max(1, Mathf.CeilToInt(shop.StockList.Count / (float)itemsPerPage))
        : 1;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake() => Initialize();

    /// <summary>외부(Bootstrap)에서 강제 초기화. 비활성 GameObject 도 동작.</summary>
    public void Initialize()
    {
        if (built) return;
        BuildRowPool();
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (prevButton  != null) prevButton.onClick.AddListener(OnPrev);
        if (nextButton  != null) nextButton.onClick.AddListener(OnNext);
        Subscribe();
        if (titleText != null && shop != null) titleText.text = shop.ShopName;
        currentPage = 0;
        RefreshPage();
    }

    void OnEnable()
    {
        Subscribe();
        UpdateCurrencyHint();
        RefreshVisibleRows();
        UIBlocker.AcquireSafe();   // 패널 활성화 → 플레이어 이동 차단
    }

    void OnDisable()
    {
        UIBlocker.ReleaseSafe();
        // Unsubscribe 는 OnDestroy 에서만. 패널이 일시 비활성된 동안에도 정적 이벤트(OnShopOpenRequested) 를
        // 받아야 다음 트리거 진입 시 다시 SetActive(true) 가 가능하다.
        // 씬 전환 race 는 HandleOpen/HandleClose/HandleCurrencyChanged 의 `this == null` 가드가 처리한다.
    }

    void OnDestroy() => Unsubscribe();

    void Subscribe()
    {
        if (subscribed) return;
        ShopTrigger.OnShopOpenRequested  += HandleOpen;
        ShopTrigger.OnShopCloseRequested += HandleClose;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshVisibleRows;
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
            InventoryManager.Instance.OnInventoryChanged -= RefreshVisibleRows;
        subscribed = false;
    }

    // ── 행 풀 생성 / 페이지 갱신 ──────────────────────────────────────────

    void BuildRowPool()
    {
        if (shop == null || itemRowPrefab == null || itemListContent == null)
        {
            Debug.LogWarning($"[ShopUI] '{name}' 슬롯 누락 — shop/content/rowPrefab 확인.");
            built = true;
            return;
        }
        int count = Mathf.Max(1, itemsPerPage);
        for (int i = 0; i < count; i++)
        {
            var row = Instantiate(itemRowPrefab, itemListContent);
            row.gameObject.SetActive(false);
            row.OnBuyRequested += HandleBuyRequested;
            pool.Add(row);
        }
        built = true;
    }

    void HandleBuyRequested(ItemData item)
    {
        if (item == null || shop == null) return;
        if (buyPopup != null) buyPopup.Show(item, shop);
        else Debug.LogWarning($"[ShopUI] '{name}' BuyPopup 미연결 — 구매 팝업을 띄울 수 없음");
    }

    void RefreshPage()
    {
        if (shop == null) return;
        int total = shop.StockList.Count;
        int start = currentPage * itemsPerPage;

        for (int i = 0; i < pool.Count; i++)
        {
            int idx = start + i;
            var row = pool[i];
            if (idx < total)
            {
                row.Bind(shop.StockList[idx], shop);
                row.gameObject.SetActive(true);
            }
            else
            {
                row.gameObject.SetActive(false);
            }
        }
        UpdateNavButtons();
    }

    void UpdateNavButtons()
    {
        int pc = PageCount;
        if (prevButton != null) prevButton.gameObject.SetActive(currentPage > 0);
        if (nextButton != null) nextButton.gameObject.SetActive(currentPage < pc - 1);
        if (pageIndicatorText != null) pageIndicatorText.text = $"{currentPage + 1} / {pc}";
    }

    void RefreshVisibleRows()
    {
        foreach (var r in pool)
            if (r != null && r.gameObject.activeSelf) r.Refresh();
    }

    // ── 이벤트 처리 ──────────────────────────────────────────────────────

    void HandleOpen(Shop requested)
    {
        // 종료 중 destroyed 인스턴스가 정적 이벤트로 콜백되는 케이스 방어
        if (this == null) return;
        if (requested != shop)
        {
            // 다른 캐릭터용 shop 요청 → 본 패널은 닫음 (낮/밤 전환 시 자동 교체용)
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }
        currentPage = 0;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        UpdateCurrencyHint();
        RefreshPage();
    }

    void HandleClose()
    {
        if (this == null) return;
        if (buyPopup != null) buyPopup.Hide();
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    /// <summary>닫기 버튼 핸들러. 자기 자신뿐 아니라 동시 활성된 InventoryUI 도 함께 닫는다.</summary>
    public void Close() => ShopTrigger.RequestCloseAll();

    void OnPrev()
    {
        if (currentPage <= 0) return;
        currentPage--;
        RefreshPage();
    }

    void OnNext()
    {
        if (currentPage >= PageCount - 1) return;
        currentPage++;
        RefreshPage();
    }

    void HandleCurrencyChanged(CurrencyType _, long __)
    {
        // 씬 전환 중 destroyed 인스턴스가 정적 이벤트로 콜백되는 경우 방어.
        if (this == null) return;
        if (gameObject.activeInHierarchy) UpdateCurrencyHint();
        RefreshVisibleRows();
    }

    // ── 갱신 ─────────────────────────────────────────────────────────────

    void UpdateCurrencyHint()
    {
        if (currencyHintText == null || shop == null || CurrencyManager.Instance == null) return;
        long val   = CurrencyManager.Instance.Get(shop.AcceptedCurrency);
        string unit = shop.AcceptedCurrency == CurrencyType.Fish ? "Fish" : "Gold";
        currencyHintText.text = $"보유 {unit}: {val:N0}";
    }
}
