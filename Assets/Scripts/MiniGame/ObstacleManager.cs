using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장애물 오브젝트 풀 + 스폰/회수 관리.
/// 플레이어를 중심으로 한 링 영역에 랜덤 스폰,
/// 플레이어에서 너무 멀어지면 자동 회수한다.
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] List<Obstacle> obstaclePrefabs;
    [SerializeField] int            poolSizePerType = 3;

    [Header("Spawn")]
    [SerializeField] Transform followTarget;
    [SerializeField] float     spawnInterval   = 1.8f;
    [SerializeField] float     spawnRadius     = 14f;   // 플레이어 중심 스폰 거리
    [SerializeField] float     despawnDistance = 20f;   // 이 거리 초과 시 회수

    List<List<Obstacle>> pools = new();
    float spawnTimer;
    bool  running = true;

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Start() => BuildPool();

    void BuildPool()
    {
        pools.Clear();
        foreach (var prefab in obstaclePrefabs)
        {
            var subPool = new List<Obstacle>();
            for (int i = 0; i < poolSizePerType; i++)
            {
                var obs = Instantiate(prefab, transform);
                obs.gameObject.SetActive(false);
                subPool.Add(obs);
            }
            pools.Add(subPool);
        }
    }

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!running || followTarget == null || pools.Count == 0) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawn();
        }

        DespawnFarObstacles();
    }

    // ── 스폰 / 회수 ───────────────────────────────────────────────────────

    void TrySpawn()
    {
        int typeIdx = Random.Range(0, pools.Count);
        var obs     = GetPooled(pools[typeIdx]);
        if (obs == null) return;

        float angle  = Random.Range(0f, 360f);
        float rad    = angle * Mathf.Deg2Rad;
        float x      = followTarget.position.x + Mathf.Cos(rad) * spawnRadius;
        float y      = followTarget.position.y + Mathf.Sin(rad) * spawnRadius;

        obs.transform.position = new Vector3(x, y, 0f);
        obs.gameObject.SetActive(true);
    }

    void DespawnFarObstacles()
    {
        float despawnSqr = despawnDistance * despawnDistance;
        foreach (var subPool in pools)
            foreach (var obs in subPool)
                if (obs.gameObject.activeSelf)
                {
                    float sqrDist = ((Vector2)obs.transform.position - (Vector2)followTarget.position).sqrMagnitude;
                    if (sqrDist > despawnSqr)
                        obs.gameObject.SetActive(false);
                }
    }

    static Obstacle GetPooled(List<Obstacle> subPool)
    {
        foreach (var obs in subPool)
            if (!obs.gameObject.activeSelf) return obs;
        return null;
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public void SetRunning(bool value)        => running = value;
    public void SetSpawnInterval(float value) => spawnInterval = value;
}
