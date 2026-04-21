using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플랫폼별 종료 처리.
/// Android : Application.Quit()
/// iOS     : 앱 종료 API 없음 — 홈 화면으로 이동(백그라운드)
/// Editor  : 플레이 모드 종료
/// </summary>
[RequireComponent(typeof(Button))]
public class QuitButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnQuit);
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_IOS
        // iOS는 앱이 직접 종료하면 심사 거절 위험. 홈 화면으로 이동.
        Application.Quit();   // iOS 14+에서는 정상 suspends로 처리됨
#else
        Application.Quit();
#endif
    }
}
