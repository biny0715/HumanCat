using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 고양이 NPC 클릭 시 뜨는 "내보내기(판매)" 확인 팝업.
///
/// [설계 의도]
/// - BuyPopup / SellPopup 과 같은 패턴: panel SetActive 토글 + 확인/취소 버튼.
/// - Singleton — CatNPC 가 어디서든 CatRemovePopupUI.Instance.Show(npc) 로 호출.
/// - 실제 판매 로직은 CatNPC.Sell() 에 위임 (재화 지급/저장 데이터 정리/파괴).
/// </summary>
public class CatRemovePopupUI : MonoBehaviour
{
    /// <summary>
    /// Singleton 접근. Lazy lookup — popup root 가 비활성 상태로 시작해도
    /// 첫 호출 시 FindAnyObjectByType(Include Inactive) 로 찾아내 Show 가 정상 작동.
    /// </summary>
    public static CatRemovePopupUI Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<CatRemovePopupUI>(FindObjectsInactive.Include);
            return _instance;
        }
    }
    static CatRemovePopupUI _instance;

    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text   titleText;
    [SerializeField] TMP_Text   descText;
    [SerializeField] TMP_Text   sellPriceText;

    [Header("Buttons")]
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;

    CatNPC currentNPC;

    void Awake()
    {
        _instance = this;

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton  != null) cancelButton.onClick.AddListener(Hide);
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public void Show(CatNPC npc)
    {
        if (npc == null) return;
        currentNPC = npc;

        var data      = CatManager.Instance?.GetItem(npc.ItemId);
        int sellPrice = data != null ? Mathf.Max(0, data.FishPrice / 2) : 0;
        string name   = data != null ? data.DisplayName : npc.ItemId;

        if (titleText     != null) titleText.text     = "고양이를 내보내시겠습니까?";
        if (descText      != null) descText.text      = $"{name}";
        if (sellPriceText != null) sellPriceText.text = $"{sellPrice:N0} Fish";

        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        currentNPC = null;
        if (panel != null) panel.SetActive(false);
    }

    void OnConfirm()
    {
        var npc = currentNPC;
        Hide();
        if (npc != null) npc.Sell();
    }
}
