using UnityEngine;

/// <summary>
/// 출입문에 붙이는 트리거 콜라이더.
/// 플레이어가 진입하면 SceneController를 통해 환경을 전환한다.
///
/// [사용법]
/// 1. 출입문 위치에 빈 GameObject 생성
/// 2. Collider2D(IsTrigger = true) 추가
/// 3. 이 스크립트 추가, targetEnvironment 설정
/// 4. Player 오브젝트에 "Player" 태그 설정
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] EnvironmentType targetEnvironment = EnvironmentType.Indoor;

    void Awake()
    {
        // 반드시 Trigger여야 한다
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        SceneController.Instance?.SetEnvironment(targetEnvironment);
    }
}
