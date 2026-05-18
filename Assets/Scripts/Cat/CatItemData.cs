using UnityEngine;

/// <summary>
/// 고양이 NPC 를 상점에서 판매하기 위한 데이터.
///
/// [설계 의도]
/// - ItemData 와 완전히 별개의 ScriptableObject. 가구/배치 관련 필드 없음.
/// - Cat 은 "아이템" 이 아니라 "엔티티" — InventoryManager 와 무관, CatManager 가 관리.
/// - Resources/CatItems/ 폴더에 두면 CatManager 가 부팅 시 일괄 로드.
///
/// 생성 위치: Assets/Resources/CatItems/Cat_XXX.asset
/// </summary>
[CreateAssetMenu(fileName = "Cat_New", menuName = "HumanCat/Cat Item Data")]
public class CatItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("저장/복구 키. 한 번 정하면 바꾸지 말 것 (세이브 호환).")]
    [SerializeField] string itemId;
    [SerializeField] string displayName;
    [SerializeField] Sprite icon;

    [Header("Price")]
    [Tooltip("CatShop 에서의 판매 가격 (Fish 기준). 판매 시 절반 환급.")]
    [Min(0)] [SerializeField] int fishPrice = 5;

    [Header("Cat")]
    [Tooltip("Spawn 시 인스턴스화할 CatNPC prefab.")]
    [SerializeField] GameObject catPrefab;

    [Tooltip("이 자산이 고양이 엔티티임을 명시. Shop 의 BuyCat 흐름에서 검증.")]
    [SerializeField] bool isCat = true;

    public string     ItemId      => itemId;
    public string     DisplayName => string.IsNullOrEmpty(displayName) ? itemId : displayName;
    public Sprite     Icon        => icon;
    public int        FishPrice   => fishPrice;
    public GameObject CatPrefab   => catPrefab;
    public bool       IsCat       => isCat;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(name))
            itemId = name.ToLowerInvariant().Replace(" ", "_");
    }
#endif
}
