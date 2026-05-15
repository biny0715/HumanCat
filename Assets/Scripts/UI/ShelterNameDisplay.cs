using TMPro;
using UnityEngine;

/// <summary>
/// Main 씬의 라벨에 PlayerPrefs 저장값(보호소 이름)을 표시.
///
/// [설계 의도]
/// - PlayerPrefs 키 문자열을 직접 다루지 않고 LoginManager.SavedShelterName 정적 헬퍼를 사용.
///   (저장 위치/규칙이 LoginManager 안에서만 바뀌면 Main 코드 영향 없음)
/// - LoginManager 인스턴스는 LoginScene에만 존재(Destroy)하지만,
///   정적 API 는 PlayerPrefs 만 읽으므로 어느 씬에서도 안전하게 호출 가능.
/// - Reset() 으로 같은 오브젝트의 TMP_Text 를 자동 연결 → Inspector 작업 최소화.
/// </summary>
public class ShelterNameDisplay : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("이름을 표시할 TMP_Text. 비워두면 같은 GameObject에서 자동 검색.")]
    [SerializeField] TMP_Text target;

    [Header("Fallback")]
    [Tooltip("저장된 값이 없거나 비어있을 때 표시할 텍스트")]
    [SerializeField] string fallbackText = "이름 없음";

    void Reset()
    {
        target = GetComponent<TMP_Text>();
    }

    void Start()
    {
        Refresh();
    }

    /// <summary>외부에서 강제 갱신이 필요할 때 호출 (예: 이름 변경 기능 추가 시).</summary>
    public void Refresh()
    {
        if (target == null) return;
        var saved = LoginManager.SavedShelterName;
        target.text = string.IsNullOrEmpty(saved) ? fallbackText : saved;
    }
}
