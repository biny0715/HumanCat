using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 패널 (페이지 기반).
///
/// [설계 의도]
/// - 두 가지 진입 경로 — 모두 동일 패널을 모드만 바꿔 사용:
///     (1) Standalone : GNB 인벤토리 버튼 클릭 시. 닫기 버튼 활성, 클릭=사용 팝업.
///     (2) Shop       : ShopTrigger 가 발행한 OnShopOpenRequested 를 구독해 자동 활성.
///                       닫기 버튼 비활성, 클릭=판매 팝업.
/// - 페이지 풀(itemsPerPage 만큼) 만 인스턴스화 후 Bind 재사용 — Shop 과 동일 비용 0 패턴.
/// - InventoryManager.OnInventoryChanged 구독으로 캐릭터 전환·구매·판매에 자동 갱신.
/// - UIBlocker 와 짝 — OnEnable/OnDisable 에서 Acquire/Release.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public enum Mode { Standalone, Shop }

    [Header("Layout")]
    [SerializeField] Transform        itemListContent;
    [SerializeField] InventoryItemRow itemRowPrefab;
    [SerializeField] int              itemsPerPage = 6;

    [Header("Optional Display")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text pageIndicatorText;
    [SerializeField] Button   closeButton;
    [SerializeField] Button   prevButton;
    [SerializeField] Button   nextButton;

    [Header("Popups")]
    [SerializeField] SellPopupUI sellPopup;
    [SerializeField] UsePopupUI  usePopup;

    readonly List<InventoryItemRow> pool = new();
    Mode currentMode = Mode.Standalone;
    int  currentPage;
    bool built;
    bool subscribed;

    int TotalSlots => InventoryManager.Instance != null ? InventoryManager.Instance.Slots.Count : 0;
    int PageCount  => itemsPerPage > 0 ? Mathf.Max(1, Mathf.CeilToInt(TotalSlots / (float)itemsPerPage)) : 1;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake() => Initialize();

    /// <summary>외부(Bootstrap) 강제 초기화. 비활성 상태에서도 동작.</summary>
    public void Initialize()
    {
        if (built) return;
        BuildPool();
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (prevButton  != null) prevButton.onClick.AddListener(OnPrev);
        if (nextButton  != null) nextButton.onClick.AddListener(OnNext);
        Subscribe();
        gameObject.SetActive(false); // 시작 시 닫혀있음
    }

    void OnEnable()
    {
        Subscribe();
        UIBlocker.AcquireSafe();
        RefreshPage();
    }

    void OnDisable()
    {
        UIBlocker.ReleaseSafe();
    }

    void OnDestroy() => Unsubscribe();

    void Subscribe()
    {
        if (subscribed) return;
        ShopTrigger.OnShopOpenRequested  += HandleShopOpen;
        ShopTrigger.OnShopCloseRequested += HandleShopClose;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        ShopTrigger.OnShopOpenRequested  -= HandleShopOpen;
        ShopTrigger.OnShopCloseRequested -= HandleShopClose;
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        subscribed = false;
    }

    // ── 풀 생성 / 페이지 갱신 ─────────────────────────────────────────────

    void BuildPool()
    {
        if (itemRowPrefab == null || itemListContent == null)
        {
            Debug.LogWarning($"[InventoryUI] '{name}' 슬롯 누락 — content/rowPrefab 확인.");
            built = true;
            return;
        }
        int count = Mathf.Max(1, itemsPerPage);
        for (int i = 0; i < count; i++)
        {
            var row = Instantiate(itemRowPrefab, itemListContent);
            row.gameObject.SetActive(false);
            row.OnClicked += HandleRowClicked;
            pool.Add(row);
        }
        built = true;
    }

    void RefreshPage()
    {
        if (InventoryManager.Instance == null) return;
        var slots = InventoryManager.Instance.Slots;
        int total = slots.Count;
        int start = currentPage * itemsPerPage;
        if (currentPage >= PageCount) currentPage = Mathf.Max(0, PageCount - 1);

        for (int i = 0; i < pool.Count; i++)
        {
            int idx = start + i;
            var row = pool[i];
            if (idx < total)
            {
                row.Bind(slots[idx]);
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

    // ── 모드 전환 진입점 ──────────────────────────────────────────────────

    public void OpenStandalone()
    {
        currentMode = Mode.Standalone;
        currentPage = 0;
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        UpdateTitle();
        gameObject.SetActive(true); // OnEnable → RefreshPage
    }

    public void OpenShop()
    {
        currentMode = Mode.Shop;
        currentPage = 0;
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        UpdateTitle();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (this == null) return;
        if (sellPopup != null) sellPopup.Hide();
        if (usePopup  != null) usePopup.Hide();
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    void UpdateTitle()
    {
        if (titleText == null) return;
        titleText.text = currentMode == Mode.Shop ? "인벤토리 (판매)" : "인벤토리";
    }

    // ── 이벤트 처리 ──────────────────────────────────────────────────────

    void HandleShopOpen(Shop _)
    {
        if (this == null) return;
        OpenShop();
    }

    void HandleShopClose()
    {
        if (this == null) return;
        // Shop 모드일 때만 같이 닫음. Standalone 으로 열린 상태였다면 영향 없음.
        if (currentMode == Mode.Shop) Close();
    }

    void HandleInventoryChanged()
    {
        if (this == null) return;
        if (gameObject.activeInHierarchy) RefreshPage();
    }

    void HandleRowClicked(InventorySlot slot)
    {
        if (slot == null || InventoryManager.Instance == null) return;
        var item = InventoryManager.Instance.GetItem(slot.itemId);
        if (item == null) return;

        if (currentMode == Mode.Shop)
        {
            if (sellPopup != null) sellPopup.Show(item);
        }
        else
        {
            if (usePopup != null) usePopup.Show(item, this);
        }
    }

    // ── 페이지 버튼 ───────────────────────────────────────────────────────

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
}
