/// <summary>
/// PlayerMover를 상속받아 스탯 기반 이동 속도를 적용하는 미니게임 전용 Mover.
/// StatManager.ComputedMoveSpeed를 기반으로 moveSpeed를 설정한다.
/// </summary>
public class MiniPlayerMover : PlayerMover
{
    /// <summary>StatManager의 스탯을 이동 속도에 반영한다.</summary>
    public void ApplyStats()
    {
        if (StatManager.Instance == null) return;
        SetMoveSpeed(StatManager.Instance.ComputedMoveSpeed);
    }
}
