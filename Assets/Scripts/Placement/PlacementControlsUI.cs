using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배치 모드 동안 화면 하단에 표시되는 [배치] [취소] 컨트롤.
///
/// [설계 의도]
/// - PlacementManager.OnBegan / OnEnded 를 구독해 자동으로 Show/Hide.
/// - [배치] 버튼은 preview 가 valid 일 때만 interactable. 매 프레임 갱신.
/// - [취소] 버튼은 항상 활성.
/// </summary>
public class PlacementControlsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;

    [Header("Buttons")]
    [SerializeField] Button   confirmButton;
    [SerializeField] Button   cancelButton;
    [SerializeField] TMP_Text statusText;

    void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton  != null) cancelButton.onClick.AddListener(OnCancel);
        Hide();
    }

    void OnEnable()
    {
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnBegan += Show;
            PlacementManager.Instance.OnEnded += Hide;
        }
    }

    void OnDisable()
    {
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnBegan -= Show;
            PlacementManager.Instance.OnEnded -= Hide;
        }
    }

    void Start()
    {
        // Awake 순서로 PlacementManager 가 아직 없을 수 있으니 Start 에서도 구독 시도.
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnBegan -= Show;
            PlacementManager.Instance.OnEnded -= Hide;
            PlacementManager.Instance.OnBegan += Show;
            PlacementManager.Instance.OnEnded += Hide;
        }
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        var mgr = PlacementManager.Instance;
        if (mgr == null) return;
        if (confirmButton != null) confirmButton.interactable = mgr.CurrentPreviewIsValid;
        if (statusText    != null)
        {
            if (mgr.CurrentPreviewIsValid) statusText.text = "배치 가능";
            else statusText.text = mgr.CurrentInvalidReason switch
            {
                PlacementManager.InvalidReason.SurfaceMismatch  => "이곳에 놓을 수 없습니다 (바닥/벽이 아님)",
                PlacementManager.InvalidReason.FurnitureOverlap => "다른 가구와 겹칩니다",
                _                                               => "배치 불가",
            };
        }
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnConfirm()
    {
        PlacementManager.Instance?.Confirm();
    }

    void OnCancel()
    {
        PlacementManager.Instance?.Cancel();
    }
}
