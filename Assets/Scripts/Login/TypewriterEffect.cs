using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// TMP_Text 한 글자씩 출력하는 타이핑 이펙트.
///
/// [설계 의도]
/// - text를 매 프레임 Substring 하지 않고 maxVisibleCharacters 만 늘려 표시한다.
///   → rich-text 태그와 레이아웃이 흔들리지 않고, GC 부담도 없다 (모바일 친화).
/// - Play(text) 호출이 들어오면 직전 코루틴을 중단하고 새로 시작 → 도중 호출도 안전.
/// - CompleteImmediate() 로 즉시 끝낼 수 있어 "타이핑 중 탭 → 즉시 표시" 패턴 지원.
/// </summary>
public class TypewriterEffect : MonoBehaviour
{
    public event Action OnComplete;

    [SerializeField] TMP_Text target;

    [Tooltip("초당 출력할 글자 수")]
    [SerializeField] float charsPerSecond = 30f;

    Coroutine routine;
    bool      playing;

    public bool IsPlaying => playing;

    void Reset()
    {
        target = GetComponent<TMP_Text>();
    }

    /// <summary>주어진 문자열을 한 글자씩 출력 시작. 직전 출력은 즉시 중단.</summary>
    public void Play(string fullText)
    {
        if (target == null) { OnComplete?.Invoke(); return; }

        Stop();
        target.text = fullText ?? string.Empty;
        target.maxVisibleCharacters = 0;
        target.ForceMeshUpdate();
        playing = true;
        routine = StartCoroutine(PlayRoutine());
    }

    /// <summary>즉시 모든 글자를 표시하고 OnComplete 발행.</summary>
    public void CompleteImmediate()
    {
        if (!playing) return;
        Stop();
        if (target != null) target.maxVisibleCharacters = int.MaxValue;
        playing = false;
        OnComplete?.Invoke();
    }

    /// <summary>이펙트 강제 중단 (이벤트 미발행).</summary>
    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    IEnumerator PlayRoutine()
    {
        int   total    = target.textInfo.characterCount;
        float interval = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

        for (int i = 1; i <= total; i++)
        {
            target.maxVisibleCharacters = i;
            if (interval > 0f) yield return new WaitForSeconds(interval);
            else               yield return null;
        }

        playing = false;
        routine = null;
        OnComplete?.Invoke();
    }
}
