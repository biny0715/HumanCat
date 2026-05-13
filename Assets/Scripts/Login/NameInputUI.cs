using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 사용자/보호소 이름 입력 UI. 한글 기준 최대 6자 제한 + 빈 값 방지.
///
/// [한글 6자 제한 핵심]
/// - C# string.Length 는 한글 1글자를 1 char로 센다. (BMP 내, 일반 한글 음절은 모두 BMP)
/// - byte가 아니라 char.Length 로 자르면 "한글/영문/숫자/이모지(BMP)" 모두 6자 제한.
/// - TMP_InputField.onValidateInput 콜백으로 6자 초과 입력을 차단.
/// - IME 조합(한글)에 의해 한 번에 여러 char가 들어오는 케이스도 onValueChanged 에서 한 번 더 잘라 안전망 구축.
/// - 이모지 등 surrogate pair는 char 2개로 세지는데, 게임 정책상 한글만 받으면 KoreanOnly 옵션으로 차단.
/// </summary>
public class NameInputUI : MonoBehaviour
{
    public event Action<string /*userName*/, string /*shelterName*/> OnNameSubmitted;

    [Header("Inputs")]
    [SerializeField] TMP_InputField userNameInput;
    [SerializeField] TMP_InputField shelterNameInput;

    [Header("Action")]
    [SerializeField] Button submitButton;
    [SerializeField] TMP_Text errorText;     // (옵션) 유효성 에러 표시

    [Header("Limits")]
    [Tooltip("한글 1글자 = 1 char 기준 최대 글자 수.")]
    [SerializeField] int   maxCharacters = 6;
    [Tooltip("true 면 한글(자음+모음+완성형)만 입력 허용. 영문/숫자 차단.")]
    [SerializeField] bool  koreanOnly    = false;

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Awake()
    {
        WireInput(userNameInput);
        WireInput(shelterNameInput);

        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmit);

        ClearError();
    }

    void OnDestroy()
    {
        UnwireInput(userNameInput);
        UnwireInput(shelterNameInput);

        if (submitButton != null)
            submitButton.onClick.RemoveListener(OnSubmit);
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public void Show()
    {
        if (userNameInput    != null) userNameInput   .text = string.Empty;
        if (shelterNameInput != null) shelterNameInput.text = string.Empty;
        ClearError();
        userNameInput?.Select();
        userNameInput?.ActivateInputField();
    }

    // ── 입력 처리 ─────────────────────────────────────────────────────────

    void WireInput(TMP_InputField field)
    {
        if (field == null) return;

        field.characterLimit  = maxCharacters; // 1차 방어선
        field.onValidateInput += ValidateChar; // 2차 방어선 (입력 시점 차단)
        field.onValueChanged.AddListener(OnInputChanged); // 3차 방어선 (IME paste 등)
    }

    void UnwireInput(TMP_InputField field)
    {
        if (field == null) return;
        field.onValidateInput -= ValidateChar;
        field.onValueChanged.RemoveListener(OnInputChanged);
    }

    /// <summary>한 글자씩 검증. 반환값이 '\0' 이면 입력 거부.</summary>
    char ValidateChar(string text, int charIndex, char addedChar)
    {
        if (koreanOnly && !IsKorean(addedChar))
            return '\0';
        return addedChar;
    }

    /// <summary>IME 조합·paste 로 한 번에 여러 char 가 들어오는 경우 안전망.</summary>
    void OnInputChanged(string value)
    {
        // characterLimit가 이미 잘라주지만, 일부 IME에서 임시로 초과될 수 있어 한 번 더 보장.
        var sender = userNameInput != null && userNameInput.text == value
            ? userNameInput
            : shelterNameInput;

        if (sender == null) return;
        if (sender.text.Length <= maxCharacters) return;

        sender.text = sender.text.Substring(0, maxCharacters);
        sender.caretPosition = sender.text.Length;
    }

    // ── 제출 ──────────────────────────────────────────────────────────────

    void OnSubmit()
    {
        string user    = userNameInput    != null ? userNameInput   .text.Trim() : string.Empty;
        string shelter = shelterNameInput != null ? shelterNameInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(shelter))
        {
            ShowError("이름과 보호소 이름을 모두 입력해 주세요.");
            return;
        }

        if (user.Length > maxCharacters || shelter.Length > maxCharacters)
        {
            ShowError($"이름은 최대 {maxCharacters}자까지 입력할 수 있어요.");
            return;
        }

        ClearError();
        OnNameSubmitted?.Invoke(user, shelter);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    /// <summary>한글 음절(가–힣) + 자모 영역.</summary>
    static bool IsKorean(char c)
    {
        return (c >= 0xAC00 && c <= 0xD7A3) || // 한글 음절
               (c >= 0x1100 && c <= 0x11FF) || // 한글 자모
               (c >= 0x3130 && c <= 0x318F);   // 호환 자모
    }

    void ShowError(string msg)
    {
        if (errorText == null) return;
        errorText.text = msg;
        errorText.gameObject.SetActive(true);
    }

    void ClearError()
    {
        if (errorText == null) return;
        errorText.text = string.Empty;
        errorText.gameObject.SetActive(false);
    }
}
