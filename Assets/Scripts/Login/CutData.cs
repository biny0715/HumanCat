using System;
using UnityEngine;

/// <summary>
/// 컷씬의 한 컷 데이터. CutsceneManager가 배열로 보관해 순차 재생.
///
/// [설계 의도]
/// - ScriptableObject 대신 [Serializable] class 로 둬서 단일 매니저 Inspector 안에서 편집.
///   (재사용/공용 컷씬이 생기면 동일 구조로 ScriptableObject 변환 용이)
/// - 컷마다 페이드 길이를 따로 둘 수 있어 연출에 유연성 부여.
/// </summary>
[Serializable]
public class CutData
{
    [Tooltip("컷에 표시할 Sprite")]
    public Sprite image;

    [Tooltip("컷에 차례대로 출력될 대사. 한 줄당 1탭으로 진행.")]
    [TextArea(2, 5)] public string[] dialogues;

    [Tooltip("컷 시작 시 페이드인 시간 (초). 0 이면 즉시.")]
    [Min(0f)] public float fadeInDuration  = 0.5f;

    [Tooltip("다음 컷으로 넘어가기 전 페이드아웃 시간 (초). 0 이면 즉시.")]
    [Min(0f)] public float fadeOutDuration = 0.5f;
}
