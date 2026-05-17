using System.Collections;
using UnityEngine;

/// <summary>
/// EditModeUI 전용 가시성 컴포넌트. target 을 다음 두 조건이 모두 만족될 때만 SetActive(true).
///   1) SceneController.CurrentEnvironment == Indoor
///   2) PlayerController.CurrentType == Human (밤=Cat 일 때는 비활성)
///
/// [설계 의도]
/// - IndoorOnlyVisibility 와 동일한 self-toggle 불가 패턴 — 항상 활성인 부모([ UI ])에 두고
///   자식 target(EditModeUI) 을 외부에서 토글.
/// - PlayerController.OnPlayerTypeChanged 를 구독 — PlayerController 가 캐릭터를 SwitchTo 한 직후
///   이벤트가 발화되므로 GameState/Player 동기화 race 회피.
/// - Start 시 즉시 적용 + 다음 프레임 한 번 더 적용 (Start 순서 race 방어):
///   PlayerController.Start 가 늦으면 첫 Apply 가 잘못된 결과여도, 그 후 SwitchTo 의
///   OnPlayerTypeChanged 또는 다음 프레임 Apply 로 자동 정정.
/// </summary>
public class EditModeUIVisibility : MonoBehaviour
{
    [SerializeField] GameObject target;

    void Start()
    {
        Subscribe();
        Apply();
        StartCoroutine(DeferredApply());
    }

    IEnumerator DeferredApply()
    {
        yield return null;          // 다른 컴포넌트들의 Start 까지 완료된 뒤 재검사
        Subscribe();                // 늦게 생성된 PlayerController/SceneController 도 구독 시도
        Apply();
    }

    void OnDestroy()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged -= HandleEnvChanged;
        if (PlayerController.Instance != null)
            PlayerController.Instance.OnPlayerTypeChanged -= HandlePlayerTypeChanged;
    }

    void Subscribe()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.OnEnvironmentChanged -= HandleEnvChanged;
            SceneController.Instance.OnEnvironmentChanged += HandleEnvChanged;
        }
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnPlayerTypeChanged -= HandlePlayerTypeChanged;
            PlayerController.Instance.OnPlayerTypeChanged += HandlePlayerTypeChanged;
        }
    }

    void HandleEnvChanged(EnvironmentType _)       => Apply();
    void HandlePlayerTypeChanged(PlayerType _)     => Apply();

    void Apply()
    {
        if (target == null) return;
        bool indoor = SceneController.Instance != null
                   && SceneController.Instance.CurrentEnvironment == EnvironmentType.Indoor;
        bool human  = PlayerController.Instance != null
                   && PlayerController.Instance.CurrentType == PlayerType.Human;
        target.SetActive(indoor && human);
    }
}
