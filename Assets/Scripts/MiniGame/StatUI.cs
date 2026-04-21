using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 게임 시작 전 스탯 배분 UI.
/// - 낮(06:00~17:59)에는 미니게임 진입 불가 → Main 씬으로 복귀
/// - StatPanel 우측 하단에 현재 게임 시간 표시
/// </summary>
public class StatUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject statPanel;

    [Header("Info Texts")]
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text pointsText;

    [Header("Speed Stat")]
    [SerializeField] TMP_Text speedValueText;
    [SerializeField] Button   speedAddBtn;

    [Header("HP Stat")]
    [SerializeField] TMP_Text hpValueText;
    [SerializeField] Button   hpAddBtn;

    [Header("Resistance Stat")]
    [SerializeField] TMP_Text resistValueText;
    [SerializeField] Button   resistAddBtn;

    [Header("Play Button")]
    [SerializeField] Button playBtn;
    [SerializeField] Button backBtn;

    [Header("Time Display (StatPanel 내 시간 텍스트)")]
    [SerializeField] TMP_Text statTimeText;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Start()
    {
        TimeManager.EnsureInstance();

        // 낮 시간 진입 제한
        if (GameManager.Instance != null && GameManager.Instance.IsDay)
        {
            Debug.Log("[StatUI] 낮 시간 - 미니게임 진입 불가. Main 씬으로 이동.");
            SceneManager.LoadScene("Main");
            return;
        }

        speedAddBtn?.onClick.AddListener(() => Allocate(StatType.Speed));
        hpAddBtn?.onClick.AddListener(()    => Allocate(StatType.HP));
        resistAddBtn?.onClick.AddListener(() => Allocate(StatType.Resistance));
        playBtn?.onClick.AddListener(OnPlayPressed);
        backBtn?.onClick.AddListener(() => SceneManager.LoadScene("Main"));

        Show();
    }

    void Update()
    {
        // StatPanel이 열려 있는 동안 시간 텍스트 실시간 갱신
        if (statPanel != null && statPanel.activeSelf && statTimeText != null)
        {
            var tm = TimeManager.Instance;
            statTimeText.text = tm != null ? tm.TimeString : "--:--";
        }
    }

    // ── 표시 / 갱신 ───────────────────────────────────────────────────────

    public void Show()
    {
        statPanel?.SetActive(true);
        Refresh();
    }

    void Refresh()
    {
        var sm = StatManager.Instance;
        if (sm == null) return;

        if (levelText)   levelText.text   = $"Lv. {sm.Level}";
        if (pointsText)  pointsText.text  = $"스탯 포인트: {sm.StatPoints}";

        if (speedValueText)  speedValueText.text  = $"{sm.SpeedStat}  (+{sm.ComputedMoveSpeed:F1} 속도)";
        if (hpValueText)     hpValueText.text     = $"{sm.HpStat}  (+{sm.HpStat * 5} HP)";
        if (resistValueText) resistValueText.text = $"{sm.ResistStat}  (충돌시 속도 감소 완화)";

        MiniGameManager.Instance?.PreviewStats();

        bool hasPoints = sm.StatPoints > 0;
        if (speedAddBtn)  speedAddBtn.interactable  = hasPoints;
        if (hpAddBtn)     hpAddBtn.interactable     = hasPoints;
        if (resistAddBtn) resistAddBtn.interactable = hasPoints;
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────────────────

    void Allocate(StatType type)
    {
        if (StatManager.Instance == null) return;
        StatManager.Instance.TryAllocate(type);
        Refresh();
    }

    void OnPlayPressed()
    {
        statPanel?.SetActive(false);
        LevelManager.Instance?.ApplyDifficulty();
        MiniGameManager.Instance?.StartGame();
    }
}
