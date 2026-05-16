using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GNB 등에 붙여 "상점 열기" 클릭을 처리.
/// Indoor 상태에서만 활성화되며, 클릭 시 지정한 ShopTrigger 의 ForceOpen() 을 호출.
///
/// [설계 의도]
/// - SceneController.OnEnvironmentChanged 구독으로 Indoor 진입 시 자동 활성, Outdoor 시 비활성.
/// - 초기 활성 여부도 SceneController.CurrentEnvironment 로 즉시 결정.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShopOpenButton : MonoBehaviour
{
    [SerializeField] ShopTrigger target;

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(Open);
    }

    void OnEnable()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.OnEnvironmentChanged += HandleEnv;
            ApplyVisibility(SceneController.Instance.CurrentEnvironment);
        }
        else
        {
            // SceneController 가 아직 없으면 일단 숨김. 곧 Subscribe 시점에 갱신됨.
            ApplyVisibility(EnvironmentType.Outdoor);
        }
    }

    void Start()
    {
        // SceneController 가 Awake 보다 늦은 경우를 대비해 한 번 더 동기화
        if (SceneController.Instance != null)
            ApplyVisibility(SceneController.Instance.CurrentEnvironment);
    }

    void OnDisable()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged -= HandleEnv;
    }

    void HandleEnv(EnvironmentType env) => ApplyVisibility(env);

    void ApplyVisibility(EnvironmentType env)
    {
        // 자기 자신을 SetActive 로 토글하면 OnDisable 이 다시 호출돼 구독 해제됨.
        // 대신 Button 컴포넌트와 그래픽만 토글.
        bool show = env == EnvironmentType.Indoor;
        if (btn != null) btn.interactable = show;
        var img = GetComponent<Image>();
        if (img != null) img.enabled = show;
        // 라벨 등 자식 그래픽도 함께 토글
        foreach (var g in GetComponentsInChildren<Graphic>(true))
            g.enabled = show;
    }

    void Open()
    {
        if (target == null) { Debug.LogWarning("[ShopOpenButton] target ShopTrigger 미할당"); return; }
        target.ForceOpen();
    }
}
