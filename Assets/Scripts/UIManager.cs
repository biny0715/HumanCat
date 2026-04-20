using UnityEngine;

/// <summary>
/// UI 전반을 관리하는 싱글톤.
/// - Arrow UI: Outdoor일 때만 표시
/// - Day/Night 전환 Popup: TriggerZone에서 호출
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Arrow UI")]
    [SerializeField] GameObject arrowUI;
    [SerializeField] GameObject miniGameArrowUI;

    [Header("Popup Prefabs")]
    [SerializeField] GameObject toNightPopupPrefab;
    [SerializeField] GameObject toDayPopupPrefab;

    [Header("Popup Parent (Canvas)")]
    [SerializeField] Transform popupParent;

    public bool IsPopupOpen => activePopup != null;

    GameObject activePopup;

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged += OnEnvironmentChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        StartCoroutine(InitArrowNextFrame());
    }

    System.Collections.IEnumerator InitArrowNextFrame()
    {
        yield return null;
        var initEnv = SceneController.Instance != null
            ? SceneController.Instance.CurrentEnvironment
            : EnvironmentType.Outdoor;
        UpdateArrow(initEnv);
        UpdateMiniGameArrow(initEnv);
    }

    void OnDestroy()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged -= OnEnvironmentChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    /// <summary>
    /// TriggerZone에서 호출. 현재 Day/Night 상태에 맞는 Popup을 표시.
    /// </summary>
    public void ShowDayNightPopup()
    {
        if (IsPopupOpen) return;

        bool isNight = GameManager.Instance != null && GameManager.Instance.IsNight;
        var prefab = isNight ? toDayPopupPrefab : toNightPopupPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("[UIManager] Popup 프리팹이 연결되지 않았습니다.");
            return;
        }

        activePopup = Instantiate(prefab, popupParent);
        var popup = activePopup.GetComponent<DayNightPopup>();
        if (popup != null)
            popup.Initialize(!isNight, this);
    }

    /// <summary>MiniGameTriggerZone에서 호출. 미니게임 진입 UI 표시.</summary>
    public void ShowMiniGamePrompt()
    {
        // TODO: 미니게임 시작 팝업 연결
        Debug.Log("[UIManager] 미니게임 트리거 진입");
    }

    /// <summary>현재 열린 Popup 닫기.</summary>
    public void HidePopup()
    {
        if (activePopup == null) return;
        Destroy(activePopup);
        activePopup = null;
    }

    // ── 내부 ──────────────────────────────────────────────────────────────

    void OnEnvironmentChanged(EnvironmentType env)
    {
        UpdateArrow(env);
        UpdateMiniGameArrow(env);
    }

    void OnGameStateChanged(GameState state)
    {
        var env = SceneController.Instance != null
            ? SceneController.Instance.CurrentEnvironment
            : EnvironmentType.Outdoor;
        UpdateMiniGameArrow(env);
    }

    void UpdateArrow(EnvironmentType env)
    {
        if (arrowUI != null)
            arrowUI.SetActive(env == EnvironmentType.Outdoor);
    }

    void UpdateMiniGameArrow(EnvironmentType env)
    {
        if (miniGameArrowUI == null) return;
        bool isNight = GameManager.Instance != null && GameManager.Instance.IsNight;
        miniGameArrowUI.SetActive(env == EnvironmentType.Outdoor && isNight);
    }
}
