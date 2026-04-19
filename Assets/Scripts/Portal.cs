using UnityEngine;

/// <summary>
/// Indoor ↔ Outdoor 전환 포탈.
///
/// [배치 방법]
/// 1. 빈 GameObject 생성 → "Portal_ToIndoor" / "Portal_ToOutdoor" 등으로 명명
/// 2. BoxCollider2D(IsTrigger = true) 추가 — 포탈 영역 크기 조절
/// 3. Portal.cs 추가 → Inspector에서 portalType 설정
/// 4. Player 오브젝트에 "Player" 태그 설정
///
/// [포탈 연결 구조]
/// Portal(ToIndoor) ──▶ SceneController.SetEnvironment(Indoor)
///                  ──▶ Player.position = indoorSpawnPos
///                  ──▶ Player.scale    = indoorScale
///                  ──▶ PlayerMover.Stop()
/// Portal(ToOutdoor)──▶ SceneController.SetEnvironment(Outdoor)
///                  ──▶ Player.position = outdoorSpawnPos
///                  ──▶ Player.scale    = outdoorScale
///                  ──▶ PlayerMover.Stop()
///
/// [무한 루프 방지]
/// 코루틴 대신 Time.time 비교로 쿨다운 처리.
/// 환경 전환 시 포탈 GameObject가 비활성화돼도 안전하게 동작한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    public enum PortalType { ToIndoor, ToOutdoor }

    [Header("Portal Type")]
    [SerializeField] PortalType portalType = PortalType.ToIndoor;

    [Header("Spawn Position")]
    [SerializeField] Vector3 indoorSpawnPos  = new Vector3(0f, -3f, 0f);
    [SerializeField] Vector3 outdoorSpawnPos = new Vector3(0f, -5f, 0f);

    [Header("Player Scale")]
    [SerializeField] Vector3 indoorScale  = new Vector3(2f, 2f, 2f);
    [SerializeField] Vector3 outdoorScale = new Vector3(1f, 1f, 1f);

    [Header("Cooldown")]
    [SerializeField] float cooldown = 0.5f;

    // Time.time 기반 쿨다운 — 코루틴 불필요, 오브젝트 비활성화에 안전
    float nextTeleportTime = 0f;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < nextTeleportTime)    return;
        if (!other.CompareTag("Player"))     return;

        Teleport(other.transform);
    }

    void Teleport(Transform player)
    {
        // 쿨다운 설정 (오브젝트가 비활성화돼도 Time.time은 유지됨)
        nextTeleportTime = Time.time + cooldown;

        bool toIndoor = portalType == PortalType.ToIndoor;

        // 환경 전환
        var env = toIndoor ? EnvironmentType.Indoor : EnvironmentType.Outdoor;
        SceneController.Instance?.SetEnvironment(env);

        // 위치 및 스케일 변경
        Vector3 spawnPos = toIndoor ? indoorSpawnPos : outdoorSpawnPos;
        Vector3 scale    = toIndoor ? indoorScale    : outdoorScale;

        var rb = player.GetComponent<Rigidbody2D>();

        // Rigidbody2D가 있으면 물리 위치도 함께 이동 후 속도 초기화
        if (rb != null)
        {
            rb.position       = spawnPos;
            rb.linearVelocity = Vector2.zero;
        }

        player.position   = spawnPos;
        player.localScale = scale;

        // 이동 명령 취소 — 포탈 이후 이전 목적지로 계속 이동하는 현상 방지
        player.GetComponent<PlayerMover>()?.Stop();
    }
}
