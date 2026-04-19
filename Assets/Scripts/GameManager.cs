using System;
using UnityEngine;

public enum GameState { Day, Night }

/// <summary>
/// 게임 전체 상태를 관리하는 싱글톤.
///
/// [설계 의도]
/// - 상태 변경은 반드시 ChangeState()를 통해서만 수행한다.
/// - 상태 변경 시 PlayerPrefs에 즉시 저장하여 앱 재실행 후에도 유지된다.
/// - 다른 시스템은 OnStateChanged 이벤트를 구독하여 반응한다. (폴링 금지)
/// - DontDestroyOnLoad: 씬 전환이 생겨도 GameManager는 유지된다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<GameState> OnStateChanged;

    public GameState CurrentState { get; private set; } = GameState.Day;
    public bool IsDay   => CurrentState == GameState.Day;
    public bool IsNight => CurrentState == GameState.Night;

    const string SaveKey = "GameState";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 앱 재실행 시 저장된 상태 복원.
        // Awake에서 로드하므로 다른 컴포넌트의 Start()가 CurrentState를 읽을 때
        // 이미 올바른 값이 들어 있다.
        LoadState();
    }

    // ── 퍼블릭 API ───────────────────────────────────────────────────────

    /// <summary>상태를 전환하고 저장한다. 동일 상태는 무시.</summary>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        SaveState();
        OnStateChanged?.Invoke(newState);
    }

    /// <summary>Day ↔ Night 토글. UI 버튼에서 직접 바인딩 가능.</summary>
    public void ToggleState()
        => ChangeState(IsDay ? GameState.Night : GameState.Day);

    // ── 저장 / 불러오기 ──────────────────────────────────────────────────

    void SaveState()
    {
        PlayerPrefs.SetInt(SaveKey, (int)CurrentState);
        PlayerPrefs.Save(); // 즉시 디스크에 기록 (앱 강제 종료 대비)
    }

    void LoadState()
    {
        // 저장값이 없으면 Day(0) 기본값 사용
        CurrentState = (GameState)PlayerPrefs.GetInt(SaveKey, (int)GameState.Day);
    }

#if UNITY_EDITOR
    /// <summary>에디터 전용: 저장된 상태 초기화.</summary>
    [ContextMenu("Debug → Reset Saved State")]
    void ResetSavedState()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        Debug.Log("[GameManager] 저장된 상태 초기화 완료");
    }
#endif
}
