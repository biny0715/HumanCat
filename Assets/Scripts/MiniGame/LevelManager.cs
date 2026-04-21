using UnityEngine;

/// <summary>
/// 레벨에 따라 난이도를 조정.
/// ApplyDifficulty()를 게임 시작 직전에 호출한다.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Flee Speed Scaling")]
    [SerializeField] float fleeSpeedBase     = 3f;
    [SerializeField] float fleeSpeedPerLevel = 0.15f;   // 레벨당 도망 속도 증가

    [Header("Spawn Interval Scaling")]
    [SerializeField] float spawnIntervalBase   = 0.8f;
    [SerializeField] float spawnIntervalReduce = 0.02f;  // 레벨당 간격 감소
    [SerializeField] float spawnIntervalMin    = 0.3f;   // 최소 스폰 간격 (하한)

    [Header("Duration Scaling")]
    [SerializeField] float baseDuration          = 30f;
    [SerializeField] float durationPerTenLevels  = 5f;   // 10레벨마다 +5초

    [Header("References")]
    [SerializeField] TargetDummy     targetDummy;
    [SerializeField] ObstacleManager obstacleManager;
    [SerializeField] MiniGameManager miniGameManager;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>현재 레벨 기준으로 난이도를 적용한다. 게임 시작 직전 호출.</summary>
    public void ApplyDifficulty()
    {
        int level = StatManager.Instance != null ? StatManager.Instance.Level : 1;

        // 도망 고양이 속도
        float flee = fleeSpeedBase + (level - 1) * fleeSpeedPerLevel;
        targetDummy?.SetFleeSpeed(flee);

        // 장애물 스폰 간격
        float interval = Mathf.Max(spawnIntervalMin,
            spawnIntervalBase - (level - 1) * spawnIntervalReduce);
        obstacleManager?.SetSpawnInterval(interval);

        // 생존 시간 (10레벨마다 +5초)
        float duration = baseDuration + Mathf.Floor((level - 1) / 10f) * durationPerTenLevels;
        miniGameManager?.SetDuration(duration);

        Debug.Log($"[LevelManager] Lv{level} 적용 — flee:{flee:F2} interval:{interval:F2} duration:{duration:F0}s");
    }

    public int   CurrentLevel      => StatManager.Instance != null ? StatManager.Instance.Level : 1;
    public float ComputedDuration  => baseDuration + Mathf.Floor((CurrentLevel - 1) / 10f) * durationPerTenLevels;
}
