using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 두 번째 실행부터 표시되는 로그인 화면. 저장된 이름을 보여주고 메인으로 진입한다.
///
/// [설계 의도]
/// - 저장된 이름은 LoginManager가 PlayerPrefs에서 읽어 Show()로 주입한다.
///   (LoginUI는 저장소를 직접 모르도록 책임 분리)
/// - 배경은 같은 GameObject(또는 자식)에 붙은 BackgroundRandomizer 가 담당. LoginUI 는 모름.
/// - 로그인 버튼은 LoginManager.OnLoginPressed 이벤트로 위임. 씬 전환은 LoginManager가 담당.
/// </summary>
public class LoginUI : MonoBehaviour
{
    public event Action OnLoginPressed;

    [Header("Display")]
    [SerializeField] TMP_Text userNameLabel;
    [SerializeField] TMP_Text shelterNameLabel;

    [Header("Action")]
    [SerializeField] Button loginButton;

    void Awake()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(HandleLogin);
    }

    void OnDestroy()
    {
        if (loginButton != null)
            loginButton.onClick.RemoveListener(HandleLogin);
    }

    public void Show(string userName, string shelterName)
    {
        if (userNameLabel    != null) userNameLabel   .text = userName;
        if (shelterNameLabel != null) shelterNameLabel.text = shelterName;
    }

    void HandleLogin() => OnLoginPressed?.Invoke();
}
