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

        // 시간을 경계값으로 맞춤 (밤 → 18:00, 낮 → 06:00)
        if (goToNight) TimeManager.Instance?.SetTime(18, 0);
        else           TimeManager.Instance?.SetTime(6,  0);

        uiManager.HidePopup();
    }

    void OnCancel() => uiManager.HidePopup();
}
