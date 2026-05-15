using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리 한 칸. JsonUtility 직렬화를 위해 [Serializable] + public 필드.
/// </summary>
[Serializable]
public class InventorySlot
{
    public string itemId;
    public int    count;

    public InventorySlot() { }
    public InventorySlot(string id, int c) { itemId = id; count = c; }
}

/// <summary>JsonUtility 저장용 컨테이너.</summary>
[Serializable]
public class InventoryData
{
    public int                 maxSlot = InventoryManager.DefaultMaxSlot;
    public List<InventorySlot> slots   = new List<InventorySlot>();
}

/// <summary>
/// 인벤토리 매니저 (싱글톤, DontDestroyOnLoad).
///
/// [설계 의도]
/// - 상점/드롭/제작 등 모든 아이템 획득 경로의 진입점.
/// - 슬롯 = "한 칸". 스택 아이템은 같은 ID가 모이고, 비스택은 1개당 1칸.
/// - 저장은 PlayerPrefs(JSON) 단일 키 사용 (Inventory.Data) — 다른 매니저 키와 충돌 없음.
/// - 데이터 안정성: 로드 시 등록되지 않은 itemId 는 정합성 정리 단계에서 제거.
/// - 확장성: maxSlot 은 인벤토리 데이터에 포함되어 영구 저장. 상점에서 '슬롯 확장권'으로
///   ExpandMaxSlot(delta) 호출만 하면 됨.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public const string SaveKey         = "Inventory.Data";
    public const int    DefaultMaxSlot  = 100;
    public const int    HardMaxSlot     = 999;   // 영구 상한 (확장권 누적 안전망)

    [Header("Bootstrap")]
    [Tooltip("Resources 하위에서 ItemData 자산을 스캔할 폴더. 기본 'Items'.")]
    [SerializeField] string resourcesItemFolder = "Items";

    InventoryData                 data;
    Dictionary<string, ItemData>  registry;

    /// <summary>인벤토리 내용이 변할 때(추가/삭제/슬롯 확장) 호출.</summary>
    public event Action OnInventoryChanged;

    // ── 읽기 전용 프로퍼티 ────────────────────────────────────────────────

    public int  MaxSlot    => data.maxSlot;
    public int  UsedSlots  => data.slots.Count;
    public bool IsFull     => UsedSlots >= MaxSlot;
    public IReadOnlyList<InventorySlot> Slots => data.slots;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadRegistry();
        Load();
    }

    // ── 아이템 정의 조회 ──────────────────────────────────────────────────

    void LoadRegistry()
    {
        registry = new Dictionary<string, ItemData>();
        var assets = Resources.LoadAll<ItemData>(resourcesItemFolder);
        foreach (var a in assets)
        {
            if (string.IsNullOrEmpty(a.ItemId))
            {
                Debug.LogWarning($"[Inventory] ItemId 누락 — 무시: {a.name}");
                continue;
            }
            if (registry.ContainsKey(a.ItemId))
            {
                Debug.LogError($"[Inventory] ItemId 중복: '{a.ItemId}' — '{a.name}' 무시");
                continue;
            }
            registry[a.ItemId] = a;
        }
    }

    /// <summary>ID로 아이템 정의 조회. 없으면 null.</summary>
    public ItemData GetItem(string itemId)
        => registry != null && itemId != null && registry.TryGetValue(itemId, out var a) ? a : null;

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    /// <summary>이 아이템을 count 만큼 더 담을 수 있는지. 슬롯/스택 제약 모두 검사.</summary>
    public bool CanAddItem(string itemId, int count)
    {
        if (count <= 0) return false;
        var def = GetItem(itemId);
        if (def == null) return false;

        int max = def.MaxStack;

        if (def.Stackable)
        {
            int remaining = count;
            foreach (var s in data.slots)
            {
                if (s.itemId != itemId) continue;
                int room = max - s.count;
                if (room > 0) remaining -= room;
                if (remaining <= 0) return true;
            }
            int newSlotsNeeded = Mathf.CeilToInt(remaining / (float)max);
            return UsedSlots + newSlotsNeeded <= MaxSlot;
        }
        else
        {
            return UsedSlots + count <= MaxSlot;
        }
    }

    /// <summary>아이템 추가 시도. 공간 부족 시 false(원상 유지).</summary>
    public bool TryAddItem(string itemId, int count)
    {
        if (!CanAddItem(itemId, count)) return false;
        var def = GetItem(itemId);
        int max = def.MaxStack;

        if (def.Stackable)
        {
            int remaining = count;
            foreach (var s in data.slots)
            {
                if (remaining <= 0) break;
                if (s.itemId != itemId) continue;
                int room = max - s.count;
                if (room <= 0) continue;
                int add = Mathf.Min(room, remaining);
                s.count += add;
                remaining -= add;
            }
            while (remaining > 0)
            {
                int add = Mathf.Min(max, remaining);
                data.slots.Add(new InventorySlot(itemId, add));
                remaining -= add;
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
                data.slots.Add(new InventorySlot(itemId, 1));
        }

        Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>아이템 차감 시도. 수량 부족 시 false(원상 유지).</summary>
    public bool TryRemoveItem(string itemId, int count)
    {
        if (count <= 0) return false;
        if (GetCount(itemId) < count) return false;

        int remaining = count;
        for (int i = data.slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var s = data.slots[i];
            if (s.itemId != itemId) continue;
            int take = Mathf.Min(s.count, remaining);
            s.count -= take;
            remaining -= take;
            if (s.count <= 0) data.slots.RemoveAt(i);
        }
        Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>해당 아이템 총 보유 수량 (모든 슬롯 합산).</summary>
    public int GetCount(string itemId)
    {
        int total = 0;
        foreach (var s in data.slots)
            if (s.itemId == itemId) total += s.count;
        return total;
    }

    /// <summary>최대 슬롯 확장 (상점 '슬롯 확장권' 등에서 호출). 0 이하 무시, 하드 상한 적용.</summary>
    public void ExpandMaxSlot(int delta)
    {
        if (delta <= 0) return;
        int before = data.maxSlot;
        data.maxSlot = Mathf.Min(data.maxSlot + delta, HardMaxSlot);
        if (data.maxSlot == before) return;
        Save();
        OnInventoryChanged?.Invoke();
    }

    // ── 저장 / 로드 ───────────────────────────────────────────────────────

    void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    void Load()
    {
        string raw = PlayerPrefs.GetString(SaveKey, null);
        if (string.IsNullOrEmpty(raw))
        {
            data = new InventoryData();
            return;
        }
        try
        {
            data = JsonUtility.FromJson<InventoryData>(raw) ?? new InventoryData();
        }
        catch
        {
            Debug.LogWarning("[Inventory] 저장 데이터 파싱 실패 — 초기화");
            data = new InventoryData();
            return;
        }

        if (data.slots == null)    data.slots   = new List<InventorySlot>();
        if (data.maxSlot <= 0)     data.maxSlot = DefaultMaxSlot;

        // 등록 안 된 itemId / 잘못된 count 정리
        int before = data.slots.Count;
        data.slots.RemoveAll(s => s == null || s.count <= 0 || GetItem(s.itemId) == null);
        if (data.slots.Count != before)
            Debug.LogWarning($"[Inventory] 무효 슬롯 {before - data.slots.Count}개 정리됨");
    }

#if UNITY_EDITOR
    [ContextMenu("Debug → Reset Inventory")]
    void ResetInventory()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        data = new InventoryData();
        OnInventoryChanged?.Invoke();
        Debug.Log("[Inventory] 인벤토리 초기화 완료");
    }

    [ContextMenu("Debug → Print Inventory")]
    void PrintInventory()
    {
        Debug.Log($"[Inventory] maxSlot={MaxSlot}, used={UsedSlots}");
        foreach (var s in data.slots)
            Debug.Log($"  - {s.itemId} x{s.count}");
    }
#endif
}
