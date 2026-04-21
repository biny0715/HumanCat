using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameResultUI : MonoBehaviour
{
    public void OnRetry() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void OnBack()  => SceneManager.LoadScene("Main");

    /// <summary>아침 패널 확인 버튼. 스탯·시간 저장 후 Main 씬으로 이동.</summary>
    public void OnMorningConfirm()
    {
        StatManager.Instance?.Save();
        TimeManager.Instance?.Save();
        SceneManager.LoadScene("Main");
    }
}
