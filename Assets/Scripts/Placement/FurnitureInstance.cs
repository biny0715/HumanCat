using UnityEngine;

/// <summary>
/// 배치된 가구 인스턴스에 부착되는 식별/메타 컴포넌트.
///
/// [설계 의도]
/// - Edit Mode 에서 클릭된 가구로부터 ItemData 를 역추적하기 위한 단일 진입점.
/// - PlacementManager.ConfirmPlacement / PlacementRestorer.Restore 양쪽에서 동일하게 부착.
/// - itemId 만 직렬화 — ItemData 자체는 InventoryManager.GetItem(itemId) 로 조회.
///   세이브 호환 (ScriptableObject 참조보다 안전).
/// </summary>
[DisallowMultipleComponent]
public class FurnitureInstance : MonoBehaviour
{
    [SerializeField] string itemId;

    public string ItemId => itemId;

    /// <summary>InventoryManager 에서 itemId 로 ItemData 조회. 매니저 없으면 null.</summary>
    public ItemData ResolveItemData()
        => InventoryManager.Instance != null ? InventoryManager.Instance.GetItem(itemId) : null;

    public void Setup(string id)
    {
        itemId = id;
    }
}
