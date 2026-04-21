using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MiniGameState { Ready, Playing, Success, Fail }

/// <summary>
/// 러너 미니게임 전체 흐름 관리.
/// - 30초 타이머, 거리 초과/HP 0 실패, 타임오버 또는 근접 성공
/// - 게임 종료 시 TimeManager +2h 적용, 아침(06~17시)이 되면 MorningPanel 표시
/// </summary>
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [Header("Game Rules")]
    [SerializeField] float gameDuration      = 30f;
    [SerializeField] float catchDistance     = 0.8f;
    [SerializeField] float maxCatchDistance  = 8f;
    [SerializeField] float graceTime         = 3f;

    [Header("References")]
    [SerializeField] MiniGamePlayer   player;
    [SerializeField] TargetDummy      targetDummy;
    [SerializeField] TileManager      tileManager;
    [SerializeField] ObstacleManager  obstacleManager;

    [Header("UI")]
    [SerializeField] TMP_Text   timerText;
    [SerializeField] TMP_Text   hpText;
    [SerializeField] Slider     hpSlider;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject successPanel;
    [SerializeField] GameObject morningPanel;

    public MiniGameState State { get; private set; } = MiniGameState.Ready;

    float timeRemaining;
    float graceTimer;

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        TimeManager.EnsureInstance();   // TimeManager가 없으면 자동 생성
    }

    void Start() { /* StatUI.OnPlayPressed()에서 StartGame() 호출 */ }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public void StartGame()
    {
        player?.ApplyStats();
        player?.SetRunning(true);
        targetDummy?.SetRunning(true);

        timeRemaining = gameDuration;
        graceTimer    = 0f;
        State         = MiniGameState.Playing;

        gameOverPanel?.SetActive(false);
        successPanel?.SetActive(false);
        morningPanel?.SetActive(false);

        player?.ResetHP();
        RefreshHPUI();
    }

    public void SetDuration(float duration) => gameDuration = duration;

    public void OnPlayerDamaged(int hp, float hpPercent)
    {
        RefreshHPUI();
        if (hp <= 0 && State == MiniGameState.Playing)
            Fail("체력이 0이 되었습니다!");
    }

    // ── 매 프레임 ─────────────────────────────────────────────────────────

    void Update()
    {
        if (State != MiniGameState.Playing) return;

        timeRemaining -= Time.deltaTime;
        graceTimer    += Time.deltaTime;
        RefreshTimerUI();

        if (timeRemaining <= 0f) { Success(); return; }

        if (graceTimer >= graceTime)
        {
            CheckCatchCondition();
            CheckFailConditions();
        }
    }

    // ── 내부 ──────────────────────────────────────────────────────────────

    void CheckCatchCondition()
    {
        if (player == null || targetDummy == null) return;
        float dist = Vector2.Distance(player.transform.position, targetDummy.transform.position);
        if (dist <= catchDistance) Success();
    }

    void CheckFailConditions()
    {
        if (player == null || targetDummy == null) return;
        float dist = Vector2.Distance(player.transform.position, targetDummy.transform.position);
        if (dist > maxCatchDistance)
            Fail($"거리가 너무 멀어졌습니다! ({dist:F1}m)");
    }

    void Success()
    {
        State = MiniGameState.Success;
        StatManager.Instance?.OnGameSuccess();
        StopAll();
        if (HandleTimeAfterGame()) return;
        successPanel?.SetActive(true);
    }

    void Fail(string reason)
    {
        State = MiniGameState.Fail;
        StopAll();
        Debug.Log($"[MiniGame] 실패: {reason}");
        if (HandleTimeAfterGame()) return;
        gameOverPanel?.SetActive(true);
    }

    /// <summary>
    /// 게임 종료 시 +2h 적용 후 아침 여부 판단.
    /// 아침이면 MorningPanel을 보여주고 true 반환.
    /// </summary>
    bool HandleTimeAfterGame()
    {
        var tm = TimeManager.Instance;
        if (tm != null)
        {
            tm.AddGameHours(2f);
            tm.Save();
        }

        bool isMorning = tm != null ? tm.IsDay
                       : (GameManager.Instance != null && GameManager.Instance.IsDay);
        if (isMorning)
        {
            morningPanel?.SetActive(true);
            return true;
        }
        return false;
    }

    void StopAll()
    {
        player?.SetRunning(false);
        targetDummy?.SetRunning(false);
        tileManager?.SetRunning(false);
        obstacleManager?.SetRunning(false);
    }

    void RefreshTimerUI()
    {
        if (timerText == null) return;
        int sec = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
        timerText.text = $"{sec / 60:00}:{sec % 60:00}";
    }

    void RefreshHPUI()
    {
        if (player == null) return;
        if (hpSlider != null) hpSlider.value = player.HPPercent;
        if (hpText   != null) hpText.text    = $"HP {player.HP}";
    }

    public void PreviewStats()
    {
        var sm = StatManager.Instance;
        if (sm == null) return;

        if (hpSlider != null) hpSlider.value = 1f;
        if (hpText   != null) hpText.text    = $"HP {sm.ComputedMaxHP}";

        float duration = LevelManager.Instance != null
            ? LevelManager.Instance.ComputedDuration
            : gameDuration;
        int sec = Mathf.CeilToInt(duration);
        if (timerText != null) timerText.text = $"{sec / 60:00}:{sec % 60:00}";
    }
}
