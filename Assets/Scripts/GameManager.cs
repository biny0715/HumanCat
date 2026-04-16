using UnityEngine;

public enum GameState { Day, Night }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Day;

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
        Debug.Log($"[GameManager] State changed → {CurrentState}");
    }
}
