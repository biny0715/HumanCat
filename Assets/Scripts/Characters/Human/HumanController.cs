/// <summary>
/// 인간 상태 전용 설정 및 동작.
///
/// [확장 포인트]
/// - 보호소 운영 관련 상호작용(오브젝트 픽업, NPC 대화 등)은 여기에 추가한다.
/// - moveSpeed / animatorController / spriteFacingRight는 Inspector에서 설정.
/// </summary>
public class HumanController : CharacterControllerBase
{
    protected override void OnActivate()
    {
        Anim.SetFacingRight(false);
    }

    protected override void OnDeactivate()
    {
        // 인간 비활성화 시 정리 (예: 상호작용 중이던 NPC와의 대화 종료)
    }
}
