using System;
using UnityEngine;

/// <summary>
/// 아이템 배치 가능 표면. [Flags] 라 한 아이템이 여러 표면을 동시에 지원할 수 있음.
/// 예) 화분 = Floor | Wall.
/// </summary>
[Flags]
public enum PlacementSurface
{
    None  = 0,
    Floor = 1 << 0,
    Wall  = 1 << 1,
}

/// <summary>
/// 게임 내 아이템 한 종류를 정의하는 ScriptableObject.
///
/// [설계 의도]
/// - 아이템 메타데이터(이름/아이콘/가격/스택 규칙)를 에디터 자산으로 관리.
///   디자이너가 코드 변경 없이 아이템을 추가/수정 가능.
/// - 저장/로드 안정성을 위해 ItemId를 별도 string으로 둔다 (파일명이 바뀌어도 세이브 데이터 호환).
/// - Resources/Items 폴더에 두면 InventoryManager가 부팅 시 일괄 로드한다.
///
/// 생성 방법: Project 우클릭 → Create → HumanCat → Item Data
/// 권장 위치 : Assets/Resources/Items/ (서브 폴더 자유)
/// </summary>
[CreateAssetMenu(fileName = "Item_New", menuName = "HumanCat/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("저장/복구에 쓰이는 고유 ID. 한 번 정하면 바꾸지 말 것 (세이브 호환).")]
    [SerializeField] string itemId;
    [SerializeField] string displayName;
    [SerializeField] Sprite icon;
    [TextArea(2, 4)]
    [SerializeField] string description;

    [Header("Price (둘 다 0이면 무료)")]
    [Min(0)] [SerializeField] int fishPrice;
    [Min(0)] [SerializeField] int goldPrice;

    [Header("Stack")]
    [Tooltip("같은 아이템을 한 슬롯에 겹쳐 둘 수 있는지.")]
    [SerializeField] bool stackable = true;
    [Tooltip("스택 최대 수량. 비스택 아이템에는 무시되고 항상 1로 취급.")]
    [Min(1)] [SerializeField] int maxStack = 999;

    [Header("Placement (선택)")]
    [Tooltip("인벤토리에서 '배치하기' 시 인스턴스화할 프리팹. 배치 불가능한 소모/확장 아이템은 비워둔다.")]
    [SerializeField] GameObject placementPrefab;

    [Tooltip("이 아이템을 놓을 수 있는 표면. 여러 개 선택 가능 (예: 바닥+벽).")]
    [SerializeField] PlacementSurface allowedSurfaces = PlacementSurface.Floor;

    [Tooltip("Wall 전용 가구 중 평소엔 자유롭게 움직이고 바닥 가까이만 마그네틱 스냅되는 가구(창문/문 등) 면 true. " +
             "Floor+Wall 가구(책장 등)는 이 값과 무관하게 항상 바닥 정렬됨.")]
    [SerializeField] bool bottomFree;

    public string           ItemId           => itemId;
    public string           DisplayName      => string.IsNullOrEmpty(displayName) ? itemId : displayName;
    public Sprite           Icon             => icon;
    public string           Description      => description;
    public int              FishPrice        => fishPrice;
    public int              GoldPrice        => goldPrice;
    public bool             Stackable        => stackable;
    public int              MaxStack         => stackable ? maxStack : 1;
    public GameObject       PlacementPrefab  => placementPrefab;
    public PlacementSurface AllowedSurfaces  => allowedSurfaces;
    public bool             BottomFree       => bottomFree;

    /// <summary>이 아이템이 월드에 배치 가능한지. 프리팹과 허용 표면이 모두 있어야 한다.</summary>
    public bool Placeable => placementPrefab != null && allowedSurfaces != PlacementSurface.None;

    /// <summary>지정된 표면에 이 아이템을 놓을 수 있는지.</summary>
    public bool AllowsSurface(PlacementSurface surface)
        => (allowedSurfaces & surface) != 0;

#if UNITY_EDITOR
    void OnValidate()
    {
        // 새 자산을 만들 때 빈 itemId 면 파일명 기반으로 자동 채움 (이후 수동 검토 권장).
        if (string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(name))
            itemId = name.ToLowerInvariant().Replace(" ", "_");
    }
#endif
}
