using UnityEngine;

/// <summary>
/// 플레이어 컴포넌트들의 조율자(Coordinator).
/// 입력 → 이동 → 애니메이션 흐름을 연결하고,
/// GameState 변경에 따라 Cat ↔ Human을 전환한다.
///
/// [설계 의도]
/// - 이 클래스는 직접 이동/애니메이션 로직을 갖지 않는다.
/// - 각 컴포넌트에 위임하고 흐름(연결)만 담당한다. (오케스트레이터 패턴)
/// - 이렇게 하면 Cat/Human 중 하나의 로직이 바뀌어도 이 클래스를 수정할 필요 없다.
/// </summary>
[RequireComponent(typeof(InputReader))]
[RequireComponent(typeof(PlayerMover))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(CatController))]
[RequireComponent(typeof(HumanController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerType startingType = PlayerType.Cat;

    InputReader              inputReader;
    PlayerMover              mover;
    PlayerAnimator           playerAnimator;
    CatController            catController;
    HumanController          humanController;
    CharacterControllerBase  activeController;
    Camera                   mainCam;

    void Awake()
    {
        inputReader    = GetComponent<InputReader>();
        mover          = GetComponent<PlayerMover>();
        playerAnimator = GetComponent<PlayerAnimator>();
        catController  = GetComponent<CatController>();
        humanController = GetComponent<HumanController>();
        mainCam        = Camera.main;
    }

    void Start()
    {
        // 이벤트 구독 - Update에서 직접 폴링하지 않아 불필요한 조건 분기 제거
        inputReader.OnTapPerformed += OnTapInput;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        SwitchTo(startingType);
    }

    void OnDestroy()
    {
        // 메모리 누수 방지: 구독 해제
        inputReader.OnTapPerformed -= OnTapInput;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    void Update()
    {
        // 애니메이션 동기화만 담당 (로직 없음)
        playerAnimator.Tick(mover.IsMoving, mover.MoveDirection);
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────────────

    void OnTapInput(Vector2 screenPos)
    {
        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        mover.MoveTo(worldPos);
    }

    // 낮(Day) → Human, 밤(Night) → Cat 전환
    void OnGameStateChanged(GameState newState)
    {
        mover.Stop();
        SwitchTo(newState == GameState.Night ? PlayerType.Cat : PlayerType.Human);
    }

    // ── 캐릭터 전환 ───────────────────────────────────────────────────

    void SwitchTo(PlayerType type)
    {
        activeController?.Deactivate();

        // switch expression으로 if/else 체인 제거
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
