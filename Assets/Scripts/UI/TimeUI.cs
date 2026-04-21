using UnityEngine;
using TMPro;

/// <summary>
/// 어느 씬에나 붙일 수 있는 시간 표시 컴포넌트.
/// TimeManager에서 매 프레임 값을 읽어 텍스트를 갱신한다.
/// </summary>
public class TimeUI : MonoBehaviour
{
    [SerializeField] TMP_Text timeText;   // HH:mm
    [SerializeField] TMP_Text phaseText;  // "낮" / "밤"

    void Update()
    {
        var tm = TimeManager.Instance;
        if (tm == null) return;

        if (timeText)  timeText.text  = tm.TimeString;
        if (phaseText) phaseText.text = tm.IsDay ? "낮" : "밤";
    }
}
