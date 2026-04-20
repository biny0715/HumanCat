using UnityEngine;

/// <summary>
/// 밤(Night) 상태에서만 활성화되는 미니게임 진입 트리거.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MiniGameTriggerZone : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsNight) return;
        if (UIManager.Instance != null && UIManager.Instance.IsPopupOpen) return;

        UIManager.Instance?.ShowMiniGamePrompt();
    }
}
