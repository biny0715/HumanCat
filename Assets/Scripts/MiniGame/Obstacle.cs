using UnityEngine;

/// <summary>
/// 장애물 한 개를 나타내는 컴포넌트.
/// ObstacleManager의 오브젝트 풀에서 꺼내 쓰인다.
///
/// - isTrigger = true 로 설정해야 한다 (Awake에서 강제 적용).
/// - 플레이어와 충돌 시 damage만큼 HP를 감소시키고 자신은 비활성화된다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour
{
    [SerializeField] int damage = 20;

    public int Damage => damage;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<MiniGamePlayer>();
        if (player == null) return;

        player.TakeDamage(damage);
        gameObject.SetActive(false); // 풀에 반환 (비활성화)
    }
}
