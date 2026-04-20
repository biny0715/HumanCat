using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ToNightApply_Popup / ToDayApply_Popup 공통 컴포넌트.
/// Inspector에서 Accept / Cancel 버튼을 연결하고 Initialize()로 목표 상태를 주입.
/// </summary>
public class DayNightPopup : MonoBehaviour
{
    [SerializeField] Button acceptButton;
    [SerializeField] Button cancelButton;

    // true → Night로 전환 / false → Day로 전환
    bool goToNight;
    UIManager uiManager;

    public void Initialize(bool goToNight, UIManager uiManager)
    {
        this.goToNight  = goToNight;
        this.uiManager  = uiManager;

        acceptButton?.onClick.AddListener(OnAccept);
        cancelButton?.onClick.AddListener(OnCancel);
    }

    void OnAccept()
    {
        var targetState = goToNight ? GameState.Night : GameState.Day;
        GameManager.Instance?.ChangeState(targetState);
        uiManager.HidePopup();
    }

    void OnCancel() => uiManager.HidePopup();
}
