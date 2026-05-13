using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상태 기반 컷씬 매니저. (Cut 1..N → 각 컷마다 대사 1..M)
///
/// [상태 전이]
///   Idle
///    └▶ FadingIn ──┐
///                  ▼
///               Typing ──(tap)──▶ (즉시 완료) ──▶ WaitingDelay ──▶ WaitingInput
///                                                                       │
///                                  (다음 대사) ◀─────────────────────────┘
///                                  (마지막 대사면) → FadingOut → (다음 컷 있으면 FadingIn / 없으면 Finished)
///
/// [입력 규칙]
/// - Typing 중 탭   : 현재 줄 즉시 완료
/// - WaitingDelay 중: 입력 무시 (0.3초 throttle)
/// - WaitingInput 중: 다음 줄 또는 다음 컷
/// - 그 외(Fade 중) : 입력 무시
/// - Skip 버튼 : 항상 즉시 종료
/// </summary>
public class CutsceneManager : MonoBehaviour, IPointerClickHandler
{
    public enum State { Idle, FadingIn, Typing, WaitingDelay, WaitingInput, FadingOut, Finished }

    public event Action OnFinished;

    [Header("Cuts")]
    [SerializeField] CutData[] cuts;

    [Header("UI References")]
    [SerializeField] Image            imageView;
    [SerializeField] TypewriterEffect typewriter;
    [SerializeField] Button           skipButton;

    [Header("Fade Overlay")]
    [Tooltip("검은 패널에 붙은 CanvasGroup. 시작 시 alpha=1 (검은 화면)에서 시작해 페이드인.")]
    [SerializeField] CanvasGroup fadePanel;

    [Header("Behaviour")]
    [Tooltip("타이핑 완료 후 입력 받기까지 대기 시간(초). 너무 빠른 탭 방지.")]
    [Min(0f)] [SerializeField] float inputDelayAfterTyping = 0.3f;

    [Tooltip("Skip 시 페이드아웃 시간(초).")]
    [Min(0f)] [SerializeField] float skipFadeOutDuration = 0.4f;

    State state = State.Idle;
    int   cutIndex;
    int   lineIndex;

    public State CurrentState => state;

    // ── 초기화 ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (skipButton != null) skipButton.onClick.AddListener(Skip);
        if (typewriter != null) typewriter.OnComplete += OnTypewriterComplete;
        if (fadePanel  != null)
        {
            fadePanel.alpha          = 1f;     // 시작은 검은 화면
            fadePanel.blocksRaycasts = true;   // 페이드아웃 동안 입력 차단
            fadePanel.interactable   = false;
        }
    }

    void OnDestroy()
    {
        if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
        if (typewriter != null) typewriter.OnComplete -= OnTypewriterComplete;
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    /// <summary>컷씬 처음부터 재생.</summary>
    public void Play()
    {
        if (cuts == null || cuts.Length == 0) { Finish(); return; }
        StopAllCoroutines();
        cutIndex  = 0;
        lineIndex = 0;
        StartCoroutine(StartCutRoutine(cuts[0]));
    }

    /// <summary>즉시 종료(페이드아웃 후 OnFinished).</summary>
    public void Skip()
    {
        if (state == State.Finished) return;
        StopAllCoroutines();
        if (typewriter != null) typewriter.Stop();
        StartCoroutine(SkipRoutine());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (state)
        {
            case State.Typing:
                typewriter?.CompleteImmediate();  // 즉시 완료 → OnTypewriterComplete 호출됨
                break;
            case State.WaitingInput:
                Advance();
                break;
            // FadingIn / FadingOut / WaitingDelay / Idle / Finished : 입력 무시
        }
    }

    // ── 상태 진행 ─────────────────────────────────────────────────────────

    IEnumerator StartCutRoutine(CutData cut)
    {
        state = State.FadingIn;
        if (imageView != null) imageView.sprite = cut.image;
        yield return FadeTo(0f, cut.fadeInDuration);
        BeginLine(FirstLineOf(cut));
    }

    void BeginLine(string line)
    {
        state = State.Typing;
        if (typewriter != null) typewriter.Play(line);
        else                    OnTypewriterComplete();   // 타이프 없으면 즉시 진행
    }

    void OnTypewriterComplete()
    {
        if (state != State.Typing) return;
        StartCoroutine(WaitInputDelay());
    }

    IEnumerator WaitInputDelay()
    {
        state = State.WaitingDelay;
        if (inputDelayAfterTyping > 0f)
            yield return new WaitForSeconds(inputDelayAfterTyping);
        state = State.WaitingInput;
    }

    void Advance()
    {
        var cut = cuts[cutIndex];
        lineIndex++;

        if (cut.dialogues != null && lineIndex < cut.dialogues.Length)
        {
            BeginLine(cut.dialogues[lineIndex]);
            return;
        }

        // 다음 컷으로
        cutIndex++;
        if (cutIndex >= cuts.Length)
            StartCoroutine(FinishRoutine(cut.fadeOutDuration));
        else
            StartCoroutine(NextCutRoutine(cut, cuts[cutIndex]));
    }

    IEnumerator NextCutRoutine(CutData prev, CutData next)
    {
        state = State.FadingOut;
        yield return FadeTo(1f, prev.fadeOutDuration);

        lineIndex = 0;
        if (imageView != null) imageView.sprite = next.image;

        state = State.FadingIn;
        yield return FadeTo(0f, next.fadeInDuration);

        BeginLine(FirstLineOf(next));
    }

    IEnumerator FinishRoutine(float fadeOut)
    {
        state = State.FadingOut;
        yield return FadeTo(1f, fadeOut);
        Finish();
    }

    IEnumerator SkipRoutine()
    {
        state = State.FadingOut;
        yield return FadeTo(1f, skipFadeOutDuration);
        Finish();
    }

    void Finish()
    {
        if (state == State.Finished) return;
        state = State.Finished;
        OnFinished?.Invoke();
    }

    // ── 페이드 ────────────────────────────────────────────────────────────

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadePanel == null) yield break;

        if (duration <= 0f)
        {
            fadePanel.alpha          = targetAlpha;
            fadePanel.blocksRaycasts = targetAlpha > 0.99f;
            yield break;
        }

        // 페이드 진행 중에는 raycast 차단 여부를 보수적으로 설정
        // (FadingOut: 즉시 차단, FadingIn: 끝나면 해제)
        bool blockDuring = targetAlpha > 0.5f;
        fadePanel.blocksRaycasts = blockDuring;

        float start = fadePanel.alpha;
        float t     = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(start, targetAlpha, t / duration);
            yield return null;
        }

        fadePanel.alpha          = targetAlpha;
        fadePanel.blocksRaycasts = targetAlpha > 0.99f;
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    static string FirstLineOf(CutData cut)
        => (cut.dialogues != null && cut.dialogues.Length > 0) ? cut.dialogues[0] : string.Empty;
}
