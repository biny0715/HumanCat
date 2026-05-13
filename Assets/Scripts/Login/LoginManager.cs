using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// LoginScene 전체 흐름을 제어하는 컨트롤러.
///
/// [설계 의도]
/// - 최초 실행 여부를 PlayerPrefs로 판단해 컷씬 → 이름 입력 또는 로그인 UI로 분기.
/// - UI 객체들은 모두 같은 씬에 GameObject로 두고 SetActive로 교체한다.
///   (모바일 부담을 줄이기 위해 추가 씬 로드/리로드 없이 한 씬에서 처리)
/// - 다른 모듈은 LoginManager.OnLoginComplete를 구독해 후속 처리(예: 로그)를 한다.
/// - DontDestroyOnLoad 미사용: LoginScene 한정 매니저.
/// </summary>
public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    // 저장 키 (다른 매니저와 충돌 방지용 prefix)
    public const string KeyHasInit     = "Login.HasInit";
    public const string KeyUserName    = "Login.UserName";
    public const string KeyShelterName = "Login.ShelterName";

    public event Action OnLoginComplete;

    [Header("Flow Roots")]
    [SerializeField] GameObject cutsceneRoot;     // CutsceneManager가 붙은 루트
    [SerializeField] GameObject nameInputRoot;    // NameInputUI가 붙은 루트
    [SerializeField] GameObject loginRoot;        // LoginUI가 붙은 루트

    [Header("Components")]
    [SerializeField] CutsceneManager cutscene;
    [SerializeField] NameInputUI     nameInput;
    [SerializeField] LoginUI         loginUI;

    [Header("Scene Transition")]
    [SerializeField] string mainSceneName = "Main";

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // 컴포넌트 이벤트 구독 (직접 호출 대신 이벤트 기반)
        if (cutscene  != null) cutscene .OnFinished      += ShowNameInput;
        if (nameInput != null) nameInput.OnNameSubmitted += OnNameSubmitted;
        if (loginUI   != null) loginUI  .OnLoginPressed  += EnterMain;

        if (IsFirstLaunch())
        {
            ShowCutscene();
        }
        else
        {
            ShowLogin();
        }
    }

    void OnDestroy()
    {
        if (cutscene  != null) cutscene .OnFinished      -= ShowNameInput;
        if (nameInput != null) nameInput.OnNameSubmitted -= OnNameSubmitted;
        if (loginUI   != null) loginUI  .OnLoginPressed  -= EnterMain;
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public static bool IsFirstLaunch()
        => PlayerPrefs.GetInt(KeyHasInit, 0) == 0;

    public static string SavedUserName
        => PlayerPrefs.GetString(KeyUserName, string.Empty);

    public static string SavedShelterName
        => PlayerPrefs.GetString(KeyShelterName, string.Empty);

    // ── 상태 전환 ─────────────────────────────────────────────────────────

    void ShowCutscene()
    {
        SetActiveOnly(cutsceneRoot);
        cutscene?.Play();
    }

    void ShowNameInput()
    {
        SetActiveOnly(nameInputRoot);
        nameInput?.Show();
    }

    void ShowLogin()
    {
        SetActiveOnly(loginRoot);
        loginUI?.Show(SavedUserName, SavedShelterName);
    }

    void OnNameSubmitted(string userName, string shelterName)
    {
        PlayerPrefs.SetString(KeyUserName,    userName);
        PlayerPrefs.SetString(KeyShelterName, shelterName);
        PlayerPrefs.SetInt   (KeyHasInit,     1);
        PlayerPrefs.Save();

        EnterMain();
    }

    void EnterMain()
    {
        OnLoginComplete?.Invoke();
        SceneManager.LoadScene(mainSceneName);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    /// <summary>지정한 루트만 활성화하고 나머지는 끈다.</summary>
    void SetActiveOnly(GameObject target)
    {
        if (cutsceneRoot  != null) cutsceneRoot .SetActive(cutsceneRoot  == target);
        if (nameInputRoot != null) nameInputRoot.SetActive(nameInputRoot == target);
        if (loginRoot     != null) loginRoot    .SetActive(loginRoot     == target);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug → Reset Login Data")]
    void ResetLogin()
    {
        PlayerPrefs.DeleteKey(KeyHasInit);
        PlayerPrefs.DeleteKey(KeyUserName);
        PlayerPrefs.DeleteKey(KeyShelterName);
        PlayerPrefs.Save();
        Debug.Log("[LoginManager] 로그인 데이터 초기화 완료");
    }
#endif
}
