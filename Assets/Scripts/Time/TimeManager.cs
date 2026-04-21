using System;
using UnityEngine;

/// <summary>
/// 게임 시간 싱글톤 (DontDestroyOnLoad).
///
/// [시간 흐름]
///   현실 1초 = 게임 24초  (realSecondsPerGameDay = 3600 기준)
///   → 현실 1시간 = 게임 24시간
///
/// [낮/밤 전환]
///   06:00 → GameManager.ChangeState(Day)  + OnDayStart 이벤트
///   18:00 → GameManager.ChangeState(Night)+ OnNightStart 이벤트
///
/// [저장/로드]
///   PlayerPrefs에 현재 분(float)과 저장 시각(UTC ticks) 보관.
///   앱 재실행 시 오프라인 경과 시간을 반영한다.
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")]
    [Tooltip("현실 몇 초가 게임 하루(24시간)인가. 기본 3600 = 현실 1시간")]
    [SerializeField] float realSecondsPerGameDay = 3600f;

    [Tooltip("시작 시 기본 게임 시간(분). 저장 값이 없을 때 사용. 720 = 정오")]
    [SerializeField] float defaultStartMinutes = 720f;

    float gameMinutes;           // 0 ~ 1439 (00:00 ~ 23:59)
    float minutesPerRealSecond;
    bool  prevIsDay;

    const string KEY_MINUTES = "time_gameMinutes";
    const string KEY_TICKS   = "time_saveTicks";

    // ── 읽기 전용 프로퍼티 ────────────────────────────────────────────────

    public float  GameMinutes => gameMinutes;
    public int    GameHour    => Mathf.FloorToInt(gameMinutes / 60f) % 24;
    public int    GameMinute  => Mathf.FloorToInt(gameMinutes % 60f);
    public bool   IsDay       => GameHour >= 6 && GameHour < 18;
    public bool   IsNight     => !IsDay;
    public string TimeString  => $"{GameHour:00}:{GameMinute:00}";

    // ── 이벤트 ───────────────────────────────────────────────────────────

    public event Action OnDayStart;
    public event Action OnNightStart;

    // ── 초기화 ───────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        minutesPerRealSecond = (24f * 60f) / realSecondsPerGameDay;
        Load();
        SyncGameManager();
        prevIsDay = IsDay;
    }

    // ── 매 프레임 ─────────────────────────────────────────────────────────

    void Update() => Tick(Time.deltaTime * minutesPerRealSecond);

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    /// <summary>게임 시간을 hours만큼 즉시 앞으로 이동. 미니게임 종료 시 +2h 등에 사용.</summary>
    public void AddGameHours(float hours) => Tick(hours * 60f);

    /// <summary>게임 시간을 지정 시각으로 직접 설정. TriggerZone 전환 시 경계값 맞춤에 사용.</summary>
    public void SetTime(int hour, int minute = 0)
    {
        gameMinutes = Mathf.Clamp(hour * 60f + minute, 0f, 24f * 60f - 1f);
        SyncGameManager();
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(KEY_MINUTES, gameMinutes);
        PlayerPrefs.SetString(KEY_TICKS,  DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    // ── 내부 ─────────────────────────────────────────────────────────────

    void Tick(float deltaMinutes)
    {
        bool wasDayBefore = IsDay;
        gameMinutes = (gameMinutes + deltaMinutes) % (24f * 60f);

        if (IsDay == wasDayBefore) return;

        SyncGameManager();
        if (IsDay) OnDayStart?.Invoke();
        else       OnNightStart?.Invoke();
    }

    void SyncGameManager()
    {
        GameManager.Instance?.ChangeState(IsDay ? GameState.Day : GameState.Night);
    }

    void Load()
    {
        gameMinutes = PlayerPrefs.GetFloat(KEY_MINUTES, defaultStartMinutes);

        if (PlayerPrefs.HasKey(KEY_TICKS) &&
            long.TryParse(PlayerPrefs.GetString(KEY_TICKS), out long savedTicks))
        {
            double elapsedSec = (DateTime.UtcNow.Ticks - savedTicks) / (double)TimeSpan.TicksPerSecond;
            if (elapsedSec > 0)
                gameMinutes = (gameMinutes + (float)(elapsedSec * minutesPerRealSecond)) % (24f * 60f);
        }
    }

    void OnApplicationPause(bool pausing) { if (pausing) Save(); }
    void OnApplicationQuit() => Save();

    /// <summary>씬에 TimeManager가 없을 때 자동 생성. 각 씬 진입 시 호출 권장.</summary>
    public static TimeManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        return new GameObject("TimeManager").AddComponent<TimeManager>();
    }
}
