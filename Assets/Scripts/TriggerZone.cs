using UnityEngine;

/// <summary>
/// Outdoor 맵 오른쪽 끝에 배치하는 Day/Night 전환 트리거.
/// BoxCollider2D(IsTrigger=true) + "Player" 태그로 감지.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TriggerZone : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Indoor 상태에서는 작동하지 않음
        if (SceneController.Instance != null &&
            SceneController.Instance.CurrentEnvironment == EnvironmentType.Indoor) return;

        // Popup이 이미 열려 있으면 무시
        if (UIManager.Instance != null && UIManager.Instance.IsPopupOpen) return;

        UIManager.Instance?.ShowDayNightPopup();
    }
}
