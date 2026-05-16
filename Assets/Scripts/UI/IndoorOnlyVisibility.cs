using UnityEngine;

/// <summary>
/// 지정한 GameObject 를 Indoor 상태에서만 SetActive(true) 로 토글한다.
///
/// [설계 의도]
/// - 본 컴포넌트는 항상 활성인 부모(예: GNB)에 붙여 target 자식을 외부에서 토글한다.
///   자기 자신이 SetActive(false) 되면 OnDisable 로 구독이 끊기므로 self-toggle 은 불가.
/// - Start 시점에 즉시 CurrentEnvironment 를 적용 → 게임 시작 시 이미 Indoor 면 ShopBtn 도 즉시 표시.
/// </summary>
public class IndoorOnlyVisibility : MonoBehaviour
{
    [SerializeField] GameObject target;

    void Start()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.OnEnvironmentChanged += Apply;
            Apply(SceneController.Instance.CurrentEnvironment);
        }
        else
        {
            // SceneController 가 아직 없으면 일단 숨김. 나중에 SceneController 가 생기면
            // 이 컴포넌트가 다시 활성화될 때 Start 가 또 호출되지는 않으므로,
            // 안전을 위해 OnEnable 도 같은 일을 하게 둘 수도 있지만 보통 같은 씬에 있으므로 OK.
            if (target != null) target.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged -= Apply;
    }

    void Apply(EnvironmentType env)
    {
        if (target == null) return;
        target.SetActive(env == EnvironmentType.Indoor);
    }
}
