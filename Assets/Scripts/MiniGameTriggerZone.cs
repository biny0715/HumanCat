using UnityEngine;

/// <summary>
/// 밤(Night) + Outdoor 상태에서만 활성화되는 미니게임 진입 트리거.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MiniGameTriggerZone : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnStateChanged;
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged += OnEnvironmentChanged;

        Refresh();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged -= OnEnvironmentChanged;
    }

    void OnStateChanged(GameState _)            => Refresh();
    void OnEnvironmentChanged(EnvironmentType _) => Refresh();

    void Refresh()
    {
        bool isNight   = GameManager.Instance != null && GameManager.Instance.IsNight;
        bool isOutdoor = SceneController.Instance == null ||
                         SceneController.Instance.CurrentEnvironment == EnvironmentType.Outdoor;
        gameObject.SetActive(isNight && isOutdoor);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (UIManager.Instance != null && UIManager.Instance.IsPopupOpen) return;
        UIManager.Instance?.ShowMiniGamePrompt();
    }
}
