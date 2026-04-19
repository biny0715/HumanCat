using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개발/QA용 Day/Night 토글 버튼.
/// 빌드 시 제거하려면 이 오브젝트를 비활성화하거나
/// #if DEVELOPMENT_BUILD || UNITY_EDITOR 조건부 컴파일 사용.
/// </summary>
public class DayNightDebugUI : MonoBehaviour
{
    [SerializeField] Button toggleButton;
    [SerializeField] Text   buttonLabel;   // Legacy UI Text

    void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(OnToggleClicked);

        // GameManager 상태 변경 시 라벨 업데이트
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += UpdateLabel;
            UpdateLabel(GameManager.Instance.CurrentState);
        }
    }

    void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(OnToggleClicked);

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= UpdateLabel;
    }

    void OnToggleClicked() => GameManager.Instance?.ToggleState();

    void UpdateLabel(GameState state)
    {
        if (buttonLabel != null)
            buttonLabel.text = state == GameState.Day ? "→ Night" : "→ Day";
    }
}
