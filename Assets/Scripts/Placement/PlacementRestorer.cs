using UnityEngine;

/// <summary>
/// 게임 시작 시 PlacementRepository 에 저장된 가구들을 Indoor 에 복원한다.
///
/// [설계 의도]
/// - Main 씬의 Indoor/Furniture 영역에 자동 배치. Start 1회 실행.
/// - InventoryManager.GetItem(itemId) 로 ItemData 조회 → placementPrefab 인스턴스화.
/// - PlayerPrefs 리셋 시 (Debug → Reset All Save Data) 자동으로 빈 상태에서 시작.
/// </summary>
public class PlacementRestorer : MonoBehaviour
{
    [Tooltip("복원된 가구의 부모. 보통 [ Environment ]/Indoor/Furniture.")]
    [SerializeField] Transform placedFurnitureRoot;
    [SerializeField] string    furnitureLayerName = "Furniture";

    void Start() => Restore();

    public void Restore()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[PlacementRestorer] InventoryManager 없음 — 복원 스킵");
            return;
        }

        int layer = LayerMask.NameToLayer(furnitureLayerName);
        int restored = 0;

        foreach (var r in PlacementRepository.All)
        {
            var item = InventoryManager.Instance.GetItem(r.itemId);
            if (item == null || item.PlacementPrefab == null) continue;

            var go = Instantiate(item.PlacementPrefab,
                                 new Vector3(r.x, r.y, 0f),
                                 Quaternion.identity,
                                 placedFurnitureRoot);
            PlacementManager.NormalizeScale(go, item.PlacementPrefab, placedFurnitureRoot);
            PlacementManager.EnsureFurnitureCollider(go);
            if (layer >= 0) SetLayerRecursive(go, layer);
            restored++;
        }
        if (restored > 0)
            Debug.Log($"[PlacementRestorer] 가구 {restored} 개 복원 완료");
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursive(t.gameObject, layer);
    }
}
