using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FishCoinPickup 오브젝트 풀 + 희귀 스폰 관리.
///
/// [설계 의도]
/// - ObstacleManager와 거의 같은 패턴(플레이어 중심 링 스폰 + 멀어지면 회수).
/// - 단, "어쩌다 한 번" 등장이 목표 → 다음 조건을 동시에 적용:
///     1) 다음 스폰까지 대기 시간을 (min~max) 사이로 랜덤 추첨 (긴 간격).
///     2) 한 판 동안 maxSpawnsPerGame 회수만큼만 등장하고 멈춤.
///   기본값(min=12, max=22, maxSpawnsPerGame=2) 기준으로 30초짜리 미니게임에서
///   평균 1~2회 등장, 운 나쁘면 한 번도 안 뜰 수 있다.
/// - 외부 인터페이스는 ObstacleManager와 동일(SetRunning, ResetRun).
/// </summary>
public class FishCoinSpawner : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] FishCoinPickup pickupPrefab;
    [SerializeField] int            poolSize = 2;

    [Header("Spawn Target")]
    [SerializeField] Transform followTarget;     // 보통 MiniGamePlayer

    [Header("Spawn Rate (희귀)")]
    [Tooltip("다음 스폰까지의 최소 대기 시간 (초).")]
    [SerializeField] float minSpawnInterval = 12f;
    [Tooltip("다음 스폰까지의 최대 대기 시간 (초).")]
    [SerializeField] float maxSpawnInterval = 22f;
    [Tooltip("한 판(게임 1회)당 최대 스폰 개수. 0이면 무제한.")]
    [SerializeField] int   maxSpawnsPerGame = 2;

    [Header("Distance")]
    [SerializeField] float spawnRadius     = 10f;
    [SerializeField] float despawnDistance = 18f;

    readonly List<FishCoinPickup> pool = new();
    float nextSpawnTimer;
    int   spawnedCount;
    bool  running = true;

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Start()
    {
        BuildPool();
        nextSpawnTimer = PickInterval();
    }

    void BuildPool()
    {
        pool.Clear();
        if (pickupPrefab == null)
        {
            Debug.LogWarning("[FishCoinSpawner] pickupPrefab 미할당 - 스폰 비활성화.");
            return;
        }
        for (int i = 0; i < poolSize; i++)
        {
            var coin = Instantiate(pickupPrefab, transform);
            coin.gameObject.SetActive(false);
            pool.Add(coin);
        }
    }

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!running || followTarget == null || pool.Count == 0) return;

        if (maxSpawnsPerGame <= 0 || spawnedCount < maxSpawnsPerGame)
        {
            nextSpawnTimer -= Time.deltaTime;
            if (nextSpawnTimer <= 0f)
            {
                if (TrySpawn()) spawnedCount++;
                nextSpawnTimer = PickInterval();
            }
        }

        DespawnFar();
    }

    // ── 스폰 / 회수 ───────────────────────────────────────────────────────

    bool TrySpawn()
    {
        var coin = GetPooled();
        if (coin == null) return false;

        float angle = Random.Range(0f, 360f);
        float rad   = angle * Mathf.Deg2Rad;
        float x     = followTarget.position.x + Mathf.Cos(rad) * spawnRadius;
        float y     = followTarget.position.y + Mathf.Sin(rad) * spawnRadius;

        coin.transform.position = new Vector3(x, y, 0f);
        coin.gameObject.SetActive(true);
        return true;
    }

    void DespawnFar()
    {
        float despawnSqr = despawnDistance * despawnDistance;
        foreach (var coin in pool)
        {
            if (!coin.gameObject.activeSelf) continue;
            float sqrDist = ((Vector2)coin.transform.position - (Vector2)followTarget.position).sqrMagnitude;
            if (sqrDist > despawnSqr)
                coin.gameObject.SetActive(false);
        }
    }

    FishCoinPickup GetPooled()
    {
        foreach (var coin in pool)
            if (!coin.gameObject.activeSelf) return coin;
        return null;
    }

    float PickInterval()
    {
        float min = Mathf.Min(minSpawnInterval, maxSpawnInterval);
        float max = Mathf.Max(minSpawnInterval, maxSpawnInterval);
        return Random.Range(min, max);
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public void SetRunning(bool value) => running = value;

    /// <summary>새 게임 시작 시 호출. 스폰 카운터/타이머 초기화 + 활성화된 풀 회수.</summary>
    public void ResetRun()
    {
        spawnedCount   = 0;
        nextSpawnTimer = PickInterval();
        foreach (var coin in pool)
            if (coin.gameObject.activeSelf)
                coin.gameObject.SetActive(false);
    }
}
