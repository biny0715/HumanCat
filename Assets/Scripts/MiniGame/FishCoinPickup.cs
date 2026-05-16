using UnityEngine;

/// <summary>
/// 미니게임 중 어쩌다 한 번 등장하는 보너스 물고기 코인.
/// FishCoinSpawner의 오브젝트 풀에서 꺼내 쓰인다.
///
/// - isTrigger = true 로 강제.
/// - 플레이어와 충돌 시 CurrencyManager 에 Fish +amount 가산 후 자기 자신을 비활성화.
/// - 데미지/속도 영향 없음 (순수 보상).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FishCoinPickup : MonoBehaviour
{
    [Tooltip("플레이어가 획득 시 추가되는 Fish 수량.")]
    [SerializeField] int amount = 1;

    public int Amount => amount;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<MiniGamePlayer>() == null) return;

        // 즉시 지급하지 않고 세션 누적 → 게임 종료(Success/Fail) 시 일괄 지급.
        MiniGameManager.Instance?.AddFishGain(amount);
        gameObject.SetActive(false); // 풀로 반환
    }
}
