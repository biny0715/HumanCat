using System;
using UnityEngine;

/// <summary>
/// 열린 UI 개수를 세어 플레이어 입력을 자동으로 차단/복구한다.
///
/// [설계 의도]
/// - Acquire/Release 쌍으로 안전한 counting. UI 들은 OnEnable/OnDisable 에서 짝지어 호출.
/// - lockCount > 0 → 입력 차단. 0 으로 떨어지면 자동 복구.
/// - PlayerController 한 곳에서 InputEnabled 를 토글하므로 UI 가 직접 PlayerController 를 참조할 필요 없음.
/// - 다양한 UI(Shop, Inventory, Popup 등)가 같은 매니저를 공유 → race-free.
/// </summary>
public class UIBlocker : MonoBehaviour
{
    public static UIBlocker Instance { get; private set; }

    int lockCount;

    public bool IsBlocked => lockCount > 0;
    public event Action<bool> OnBlockStateChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Acquire()
    {
        lockCount++;
        if (lockCount == 1) Notify();
    }

    public void Release()
    {
        if (lockCount <= 0) return;
        lockCount--;
        if (lockCount == 0) Notify();
    }

    void Notify()
    {
        OnBlockStateChanged?.Invoke(IsBlocked);
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetInputEnabled(!IsBlocked);
    }

    /// <summary>유틸: 어디서든 안전하게 lock 획득. 매니저가 없으면 무시.</summary>
    public static void AcquireSafe()
    {
        if (Instance != null) Instance.Acquire();
    }

    public static void ReleaseSafe()
    {
        if (Instance != null) Instance.Release();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug → Force Release All")]
    void ForceReleaseAll()
    {
        lockCount = 0;
        Notify();
    }
#endif
}
