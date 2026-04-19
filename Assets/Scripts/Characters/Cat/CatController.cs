using UnityEngine;

/// <summary>
/// 고양이 상태의 이동 속도, Animator Controller 등 고양이 전용 설정을 관리.
/// 나중에 고양이 전용 능력(대시, 점프 등)도 여기에 추가한다.
/// </summary>
public class CatController : CharacterControllerBase
{
    protected override void OnActivate()
    {
        Debug.Log("[CatController] 활성화");
    }

    protected override void OnDeactivate()
    {
        Debug.Log("[CatController] 비활성화");
    }
}
