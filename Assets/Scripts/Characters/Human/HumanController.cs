using UnityEngine;

/// <summary>
/// 인간 상태의 이동 속도, Animator Controller 등 인간 전용 설정을 관리.
/// 나중에 보호소 운영 관련 상호작용 로직도 여기에 추가한다.
/// </summary>
public class HumanController : CharacterControllerBase
{
    protected override void OnActivate()
    {
        Debug.Log("[HumanController] 활성화");
    }

    protected override void OnDeactivate()
    {
        Debug.Log("[HumanController] 비활성화");
    }
}
