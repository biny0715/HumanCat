using UnityEngine;

/// <summary>
/// 플레이어 컴포넌트의 오케스트레이터(Coordinator).
///
/// [설계 의도]
/// - 이 클래스는 직접 이동/애니메이션 로직을 갖지 않는다.
///   각 컴포넌트에 위임하고, 이벤트 구독으로 흐름만 연결한다.
/// - 초기 타입 결정 우선순위:
///   1순위) GameManager.CurrentState (저장된 상태)
///   2순위) Inspector의 startingType (GameManager 없는 테스트 씬용)
/// - 입력 → 이동: InputReader.OnTapPerformed → PlayerMover.MoveTo()
/// - 상태 전환: GameManager.OnStateChanged → SwitchTo()
/// - 애니메이션: Update → PlayerAnimator.Tick() (변경분만 전달)
/// </summary>
[RequireComponent(typeof(InputReader))]
[RequireComponent(typeof(PlayerMover))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(CatController))]
[RequireComponent(typeof(HumanController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerType startingType = PlayerType.Cat;

    InputReader             inputReader;
    PlayerMover             mover;
    PlayerAnimator          playerAnimator;
    CatController           catController;
    HumanController         humanController;
    CharacterControllerBase activeController;
    Camera                  mainCam;

    void Awake()
    {
        inputReader     = GetComponent<InputReader>();
        mover           = GetComponent<PlayerMover>();
        playerAnimator  = GetComponent<PlayerAnimator>();
        catController   = GetComponent<CatController>();
        humanController = GetComponent<HumanController>();

        // Camera.main은 매 호출마다 FindObject를 수행하므로 Awake에서 캐싱.
        mainCam = Camera.main;
    }

    void Start()
    {
        inputReader.OnTapPerformed += OnTapInput;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        // GameManager의 저장 상태를 우선 사용.
        // GameManager.Awake()가 먼저 실행되므로 CurrentState는 이미 로드된 값이다.
        var initialType = ResolveInitialType();
        SwitchTo(initialType);

        // 저장된 위치·스케일 복원
        if (GameManager.Instance != null && GameManager.Instance.HasSavedPosition)
        {
            var gm = GameManager.Instance;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position       = gm.SavedPlayerPosition;
                rb.linearVelocity = Vector2.zero;
            }
            transform.position   = gm.SavedPlayerPosition;
            transform.localScale = gm.SavedPlayerScale;
        }
    }

    void OnDestroy()
    {
        inputReader.OnTapPerformed -= OnTapInput;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    void OnApplicationQuit()           => SaveCurrentState();
    void OnApplicationPause(bool pause) { if (pause) SaveCurrentState(); }

    void SaveCurrentState()
    {
        if (GameManager.Instance == null) return;
        bool isIndoor = SceneController.Instance?.CurrentEnvironment == EnvironmentType.Indoor;
        GameManager.Instance.SaveGameState(isIndoor, transform.position, transform.localScale);
    }

    void Update()
    {
        playerAnimator.Tick(mover.IsMoving, mover.MoveDirection);
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────

    void OnTapInput(Vector2 screenPos)
    {
        if (mainCam == null) return;
        mover.MoveTo(mainCam.ScreenToWorldPoint(screenPos));
    }

    // 낮(Day) → Human, 밤(Night) → Cat
    void OnGameStateChanged(GameState newState)
    {
        mover.Stop();
        SwitchTo(newState == GameState.Night ? PlayerType.Cat : PlayerType.Human);
    }

    // ── 캐릭터 전환 ──────────────────────────────────────────────────────

    /// <summary>
    /// 초기 타입 결정.
    /// GameManager가 있으면 저장된 상태를 따르고,
    /// 없으면 Inspector 값(startingType)을 사용한다.
    /// </summary>
    PlayerType ResolveInitialType()
    {
        if (GameManager.Instance == null) return startingType;
        return GameManager.Instance.IsNight ? PlayerType.Cat : PlayerType.Human;
    }

    void SwitchTo(PlayerType type)
    {
        activeController?.Deactivate();

        activeController = type switch
        {
            PlayerType.Cat   => catController,
            PlayerType.Human => humanController,
            _                => null
        };

        activeController?.Initialize(mover, playerAnimator);
    }

    /// <summary>외부(UI, 이벤트 등)에서 타입을 직접 전환할 때 사용.</summary>
    public void SetPlayerType(PlayerType type) => SwitchTo(type);
}
