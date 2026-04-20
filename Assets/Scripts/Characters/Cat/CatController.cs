/// <summary>
/// 고양이 상태 전용 설정 및 동작.
///
/// [확장 포인트]
/// - 고양이 전용 능력(대시, 높은 이동 속도, 장애물 통과 등)은 여기에 추가한다.
/// - moveSpeed / animatorController / spriteFacingRight는 Inspector에서 설정.
/// </summary>
public class CatController : CharacterControllerBase
{
    protected override void OnActivate()
    {
        Anim.SetFacingRight(true);
    }

    protected override void OnDeactivate()
    {
        // 고양이 비활성화 시 정리 (예: 진행 중인 대시 취소)
    }
}
