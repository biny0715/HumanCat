using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ToMiniGame_Popup 전용 컴포넌트.
/// 확인(Yes_Btn) → MiniGame_Chase 씬 로드
/// 취소(No_Btn)  → 팝업 닫기
/// </summary>
public class MiniGamePopup : MonoBehaviour
{
    [SerializeField] Button confirmBtn;
    [SerializeField] Button cancelBtn;

    void Awake()
    {
        confirmBtn?.onClick.AddListener(OnConfirm);
        cancelBtn?.onClick.AddListener(OnCancel);
    }

    void OnConfirm()
    {
        UIManager.Instance?.HidePopup();
        SceneManager.LoadScene("MiniGame_Chase");
    }

    void OnCancel() => UIManager.Instance?.HidePopup();
}
