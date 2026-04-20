using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum EnvironmentType { Outdoor, Indoor }

/// <summary>
/// Indoor / Outdoor 전환과 Day / Night 배경 전환을 관리.
///
/// [설계 의도]
/// - Outdoor에는 Background_Day / Background_Night 두 오브젝트를 두고
///   Day/Night에 따라 SetActive로 교체한다.
///   스프라이트 swap이 아니라 GameObject 교체이므로 나중에 각 배경에
///   파티클, 애니메이션 등을 독립적으로 추가하기 쉽다.
/// - NightOverlay(UI Image)는 배경 외 캐릭터·소품을 자연스럽게 어둡게 만드는
///   보조 레이어로 유지한다.
/// - Indoor ↔ Outdoor 전환은 SetActive로 처리한다.
///   씬 로드 없이 즉시 전환되므로 모바일 성능에 유리하다.
/// - 싱글톤으로 노출하여 DoorTrigger 등 다른 컴포넌트에서 접근 가능하게 한다.
/// </summary>
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    public event System.Action<EnvironmentType> OnEnvironmentChanged;

    [Header("Outdoor Backgrounds")]
    [SerializeField] GameObject outdoorBgDay;    // Outdoor/Background_Day
    [SerializeField] GameObject outdoorBgNight;  // Outdoor/Background_Night

    [Header("Indoor Backgrounds")]
    [SerializeField] GameObject indoorBgDay;     // Indoor/Background_Day
    [SerializeField] GameObject indoorBgNight;   // Indoor/Background_Night

    [Header("Environment Roots")]
    [SerializeField] GameObject outdoorRoot;     // Outdoor 전체
    [SerializeField] GameObject indoorRoot;      // Indoor 전체

    [Header("Night Overlay (보조 어둠 효과)")]
    [SerializeField] Image nightOverlay;
    [SerializeField] Color nightOverlayColor  = new Color(0.05f, 0.05f, 0.2f, 0.45f);
    [SerializeField] float transitionDuration = 0.8f;

    EnvironmentType currentEnvironment = EnvironmentType.Outdoor;
    Coroutine       activeTransition;

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        var initialState = GameManager.Instance != null
            ? GameManager.Instance.CurrentState
            : GameState.Day;

        // 초기 환경 + 배경 즉시 적용 (저장된 환경 우선)
        bool savedIndoor = GameManager.Instance != null && GameManager.Instance.HasSavedPosition
                           && GameManager.Instance.SavedIsIndoor;
        var initialEnv = savedIndoor ? EnvironmentType.Indoor : EnvironmentType.Outdoor;
        currentEnvironment = initialEnv;
        ApplyEnvironment(initialEnv);
        OnEnvironmentChanged?.Invoke(initialEnv);
        ApplyDayNight(initialState, instant: true);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    /// <summary>
    /// Indoor / Outdoor 전환. DoorTrigger 또는 UI 버튼에서 호출.
    /// </summary>
    public void SetEnvironment(EnvironmentType environment)
    {
        if (currentEnvironment == environment) return;
        currentEnvironment = environment;
        ApplyEnvironment(environment);
        OnEnvironmentChanged?.Invoke(environment);
    }

    public EnvironmentType CurrentEnvironment => currentEnvironment;

    // ── GameManager 연동 ──────────────────────────────────────────────────

    void OnGameStateChanged(GameState newState)
        => ApplyDayNight(newState, instant: false);

    // ── 환경 전환 ─────────────────────────────────────────────────────────

    void ApplyEnvironment(EnvironmentType environment)
    {
        bool isOutdoor = environment == EnvironmentType.Outdoor;

        if (outdoorRoot != null) outdoorRoot.SetActive(isOutdoor);
        if (indoorRoot  != null) indoorRoot .SetActive(!isOutdoor);
    }

    // ── Day / Night 처리 ─────────────────────────────────────────────────

    void ApplyDayNight(GameState state, bool instant)
    {
        bool isNight = state == GameState.Night;

        // Outdoor 배경 교체
        if (outdoorBgDay   != null) outdoorBgDay  .SetActive(!isNight);
        if (outdoorBgNight != null) outdoorBgNight.SetActive(isNight);

        // Indoor 배경 교체
        if (indoorBgDay    != null) indoorBgDay   .SetActive(!isNight);
        if (indoorBgNight  != null) indoorBgNight .SetActive(isNight);

        // 보조 오버레이 (부드러운 전환)
        if (nightOverlay == null) return;

        Color target = isNight
            ? nightOverlayColor
            : new Color(nightOverlayColor.r, nightOverlayColor.g, nightOverlayColor.b, 0f);

        if (instant) { nightOverlay.color = target; return; }

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(FadeOverlay(target));
    }

    IEnumerator FadeOverlay(Color target)
    {
        Color start   = nightOverlay.color;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            nightOverlay.color = Color.Lerp(start, target, elapsed / transitionDuration);
            yield return null;
        }

        nightOverlay.color = target;
        activeTransition   = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Test → Outdoor")]
    void TestOutdoor() => SetEnvironment(EnvironmentType.Outdoor);

    [ContextMenu("Test → Indoor")]
    void TestIndoor() => SetEnvironment(EnvironmentType.Indoor);

    [ContextMenu("Test → Toggle Day/Night")]
    void TestToggle() => GameManager.Instance?.ToggleState();
#endif
}
