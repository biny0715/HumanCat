using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 처음 시작 유저 또는 디버그 Reset 시 자동 적용되는 기본 가구 배치 세트.
///
/// [설계 의도]
/// - Resources/DefaultPlacements.asset 에 두면 런타임에 PlacementRepository 가 자동 로드.
/// - 디자이너가 직접 편집해도 되고, 에디터 메뉴 "Save Current as Default" 로
///   현재 PlayerPrefs 상태를 그대로 export 가능.
/// - JsonUtility 직렬화에 쓰이는 PlacementRecord 구조 그대로 재사용 — 변환 없음.
/// </summary>
[CreateAssetMenu(fileName = "DefaultPlacements", menuName = "HumanCat/Default Placements")]
public class DefaultPlacementSet : ScriptableObject
{
    public List<PlacementRecord> records = new List<PlacementRecord>();
}
