using UnityEngine;

/// <summary>
/// 러너 미니게임 카메라.
/// X축만 플레이어를 추적하고 Y는 고정한다.
/// </summary>
public class RunnerCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float     smoothSpeed = 6f;
    [SerializeField] float     offsetX     = 2f;   // 플레이어 앞쪽 여백
    [SerializeField] float     fixedY      = 0f;
    [SerializeField] float     fixedZ      = -10f;

    void LateUpdate()
    {
        if (target == null) return;

        float targetX  = target.position.x + offsetX;
        float currentX = transform.position.x;
        float smoothX  = Mathf.Lerp(currentX, targetX, smoothSpeed * Time.deltaTime);

        transform.position = new Vector3(smoothX, fixedY, fixedZ);
    }

    public void SetTarget(Transform t) => target = t;
}
