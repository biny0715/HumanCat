using System;
using UnityEngine;

public enum GameState { Day, Night }

/// <summary>
/// 게임 전체 상태를 관리하는 싱글톤.
/// 상태 변경 시 OnStateChanged 이벤트를 발행하여
/// 다른 컴포넌트가 직접 폴링하지 않고 구독 방식으로 반응할 수 있도록 한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Day;

    // 상태 변경 시 구독자에게 새 상태를 전달
    public event Action<GameState> OnStateChanged;

    // 자주 쓰이는 상태를 프로퍼티로 노출하여 if 비교 코드 가독성 향상
    public bool IsDay   => CurrentState == GameState.Day;
    public bool IsNight => CurrentState == GameState.Night;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] State → {CurrentState}");
    }
}
