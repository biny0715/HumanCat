using UnityEngine;

public enum StatType { Speed, HP, Resistance }

/// <summary>
/// 스탯 데이터 저장/로드 + 계산된 능력치 제공.
/// PlayerPrefs로 영구 저장.
/// </summary>
public class StatManager : MonoBehaviour
{
    public static StatManager Instance { get; private set; }

    [Header("Base Values")]
    [SerializeField] float baseSpeed     = 5f;
    [SerializeField] int   baseHP        = 100;
    [SerializeField] float baseHitMult   = 0.2f;   // 충돌 시 기본 속도 배율 (80% 감소)

    [Header("Stat Scaling")]
    [SerializeField] float speedPerPoint  = 0.3f;   // 스탯 1당 이동 속도 증가
    [SerializeField] int   hpPerPoint     = 5;      // 스탯 1당 최대 HP 증가
    [SerializeField] float resistPerPoint = 0.02f;  // 스탯 1당 충돌 감소 완화
    [SerializeField] float maxHitMult     = 0.7f;   // 최대 충돌 배율 상한 (30% 감소)

    const string KEY_LEVEL  = "mini_level";
    const string KEY_POINTS = "mini_statPoints";
    const string KEY_SPEED  = "mini_speedStat";
    const string KEY_HP     = "mini_hpStat";
    const string KEY_RESIST = "mini_resistStat";

    // ── 현재 스탯 (읽기 전용) ─────────────────────────────────────────────
    public int Level      { get; private set; } = 1;
    public int StatPoints { get; private set; } = 0;
    public int SpeedStat  { get; private set; } = 0;
    public int HpStat     { get; private set; } = 0;
    public int ResistStat { get; private set; } = 0;

    // ── 계산된 능력치 ──────────────────────────────────────────────────────
    public float ComputedMoveSpeed => baseSpeed + SpeedStat * speedPerPoint;
    public int   ComputedMaxHP     => baseHP    + HpStat    * hpPerPoint;
    public float ComputedHitMult   => Mathf.Min(baseHitMult + ResistStat * resistPerPoint, maxHitMult);

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 저장 / 로드 ───────────────────────────────────────────────────────

    public void Save()
    {
        PlayerPrefs.SetInt(KEY_LEVEL,  Level);
        PlayerPrefs.SetInt(KEY_POINTS, StatPoints);
        PlayerPrefs.SetInt(KEY_SPEED,  SpeedStat);
        PlayerPrefs.SetInt(KEY_HP,     HpStat);
        PlayerPrefs.SetInt(KEY_RESIST, ResistStat);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        Level      = PlayerPrefs.GetInt(KEY_LEVEL,  1);
        StatPoints = PlayerPrefs.GetInt(KEY_POINTS, 0);
        SpeedStat  = PlayerPrefs.GetInt(KEY_SPEED,  0);
        HpStat     = PlayerPrefs.GetInt(KEY_HP,     0);
        ResistStat = PlayerPrefs.GetInt(KEY_RESIST, 0);
    }

    // ── 레벨 / 포인트 조작 ────────────────────────────────────────────────

    /// <summary>미니게임 성공 시 호출. 레벨+1, 스탯포인트+1 후 저장.</summary>
    public void OnGameSuccess()
    {
        Level++;
        StatPoints++;
        Save();
    }

    /// <summary>스탯 포인트를 소비해 해당 스탯을 1 올린다.</summary>
    public bool TryAllocate(StatType type)
    {
        if (StatPoints <= 0) return false;
        StatPoints--;
        switch (type)
        {
            case StatType.Speed:      SpeedStat++;  break;
            case StatType.HP:         HpStat++;     break;
            case StatType.Resistance: ResistStat++; break;
        }
        Save();
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Reset All Data")]
    void ResetAll()
    {
        PlayerPrefs.DeleteKey(KEY_LEVEL);
        PlayerPrefs.DeleteKey(KEY_POINTS);
        PlayerPrefs.DeleteKey(KEY_SPEED);
        PlayerPrefs.DeleteKey(KEY_HP);
        PlayerPrefs.DeleteKey(KEY_RESIST);
        Load();
        Debug.Log("[StatManager] 데이터 초기화 완료");
    }
#endif
}
