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

/// <summary>JsonUtility 저장용 컨테이너. 한 캐릭터분의 인벤토리.</summary>
[Serializable]
public class InventoryData
{
    public int                 maxSlot = InventoryManager.DefaultMaxSlot;
    public List<InventorySlot> slots   = new List<InventorySlot>();
}

/// <summary>저장 파일 최상위. Human/Cat 각각 별도 인벤토리를 보관.</summary>
[Serializable]
public class InventorySaveData
{
    public InventoryData human = new InventoryData();
    public InventoryData cat   = new InventoryData();
}

/// <summary>
/// 인벤토리 매니저 (싱글톤, DontDestroyOnLoad).
///
/// [설계 의도]
/// - Human / Cat 두 캐릭터가 각자 독립 인벤토리를 가진다.
/// - PlayerController.CurrentType 으로 "현재 인벤토리" 결정. Day/Night 전환 시 자동으로 따라감.
/// - TryAddItem 등 기본 API 는 현재 인벤토리에 작용. 특정 타입을 명시하려면
///   TryAddItemFor(PlayerType, ...) 사용.
/// - JsonUtility 단일 키 저장 (Inventory.Data) — 내부에 InventorySaveData 통째로 직렬화.
/// - GameManager.OnStateChanged 구독으로 캐릭터 전환 시 OnInventoryChanged 발행 → UI 자동 갱신.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public const string SaveKey         = "Inventory.Data";
    public const int    DefaultMaxSlot  = 100;
    public const int    HardMaxSlot     = 999;

    [Header("Bootstrap")]
    [Tooltip("Resources 하위에서 ItemData 자산을 스캔할 폴더. 기본 'Items'.")]
    [SerializeField] string resourcesItemFolder = "Items";

    InventorySaveData             save;
    Dictionary<string, ItemData>  registry;

    /// <summary>현재 인벤토리(추가/삭제/슬롯 확장 또는 캐릭터 전환) 가 변할 때 호출.</summary>
    public event Action OnInventoryChanged;

    // ── 읽기 전용 프로퍼티 (현재 인벤토리 기준) ──────────────────────────

    public int  MaxSlot    => Current.maxSlot;
    public int  UsedSlots  => Current.slots.Count;
    public bool IsFull     => UsedSlots >= MaxSlot;
    public IReadOnlyList<InventorySlot> Slots => Current.slots;

    /// <summary>현재 활성 캐릭터의 InventoryData. PlayerController 없으면 Human 기본.</summary>
    public InventoryData Current => GetInventory(ResolveCurrentType());

    /// <summary>지정한 캐릭터의 InventoryData 조회.</summary>
    public InventoryData GetInventory(PlayerType type)
        => type == PlayerType.Human ? save.human : save.cat;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        LoadRegistry();
        Load();
    }

    void Start()
    {
        // 캐릭터 전환 → 현재 인벤토리도 바뀌므로 UI 새로고침을 위해 이벤트 발행
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        if (Instance == this) Instance = null;
    }

    void HandleStateChanged(GameState _) => OnInventoryChanged?.Invoke();

    static PlayerType ResolveCurrentType()
        => PlayerController.Instance != null ? PlayerController.Instance.CurrentType : PlayerType.Human;

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

    // ── 퍼블릭 API (현재 인벤토리) ────────────────────────────────────────

    public bool CanAddItem(string itemId, int count) => CanAddItemFor(ResolveCurrentType(), itemId, count);
    public bool TryAddItem(string itemId, int count) => TryAddItemFor(ResolveCurrentType(), itemId, count);
    public bool TryRemoveItem(string itemId, int count) => TryRemoveItemFor(ResolveCurrentType(), itemId, count);
    public int  GetCount(string itemId) => GetCountFor(ResolveCurrentType(), itemId);

    public void ExpandMaxSlot(int delta) => ExpandMaxSlotFor(ResolveCurrentType(), delta);

    // ── 퍼블릭 API (특정 캐릭터 지정) ─────────────────────────────────────

    public bool CanAddItemFor(PlayerType type, string itemId, int count)
    {
        if (count <= 0) return false;
        var def = GetItem(itemId);
        if (def == null) return false;
        var inv = GetInventory(type);

        int max = def.MaxStack;
        if (def.Stackable)
        {
            int remaining = count;
            foreach (var s in inv.slots)
            {
                if (s.itemId != itemId) continue;
                int room = max - s.count;
                if (room > 0) remaining -= room;
                if (remaining <= 0) return true;
            }
            int newSlotsNeeded = Mathf.CeilToInt(remaining / (float)max);
            return inv.slots.Count + newSlotsNeeded <= inv.maxSlot;
        }
        return inv.slots.Count + count <= inv.maxSlot;
    }

    public bool TryAddItemFor(PlayerType type, string itemId, int count)
    {
        if (!CanAddItemFor(type, itemId, count)) return false;
        var def = GetItem(itemId);
        var inv = GetInventory(type);
        int max = def.MaxStack;

        if (def.Stackable)
        {
            int remaining = count;
            foreach (var s in inv.slots)
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
                inv.slots.Add(new InventorySlot(itemId, add));
                remaining -= add;
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
                inv.slots.Add(new InventorySlot(itemId, 1));
        }

        Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryRemoveItemFor(PlayerType type, string itemId, int count)
    {
        if (count <= 0) return false;
        if (GetCountFor(type, itemId) < count) return false;
        var inv = GetInventory(type);

        int remaining = count;
        for (int i = inv.slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var s = inv.slots[i];
            if (s.itemId != itemId) continue;
            int take = Mathf.Min(s.count, remaining);
            s.count -= take;
            remaining -= take;
            if (s.count <= 0) inv.slots.RemoveAt(i);
        }
        Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetCountFor(PlayerType type, string itemId)
    {
        int total = 0;
        foreach (var s in GetInventory(type).slots)
            if (s.itemId == itemId) total += s.count;
        return total;
    }

    public void ExpandMaxSlotFor(PlayerType type, int delta)
    {
        if (delta <= 0) return;
        var inv = GetInventory(type);
        int before = inv.maxSlot;
        inv.maxSlot = Mathf.Min(inv.maxSlot + delta, HardMaxSlot);
        if (inv.maxSlot == before) return;
        Save();
        OnInventoryChanged?.Invoke();
    }

    // ── 저장 / 로드 ───────────────────────────────────────────────────────

    void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    void Load()
    {
        string raw = PlayerPrefs.GetString(SaveKey, null);
        if (string.IsNullOrEmpty(raw))
        {
            save = new InventorySaveData();
            return;
        }
        try
        {
            save = JsonUtility.FromJson<InventorySaveData>(raw) ?? new InventorySaveData();
        }
        catch
        {
            Debug.LogWarning("[Inventory] 저장 데이터 파싱 실패 — 초기화");
            save = new InventorySaveData();
            return;
        }

        if (save.human == null) save.human = new InventoryData();
        if (save.cat   == null) save.cat   = new InventoryData();
        Sanitize(save.human);
        Sanitize(save.cat);
    }

    void Sanitize(InventoryData inv)
    {
        if (inv.slots == null) inv.slots = new List<InventorySlot>();
        if (inv.maxSlot <= 0)  inv.maxSlot = DefaultMaxSlot;
        int before = inv.slots.Count;
        inv.slots.RemoveAll(s => s == null || s.count <= 0 || GetItem(s.itemId) == null);
        if (inv.slots.Count != before)
            Debug.LogWarning($"[Inventory] 무효 슬롯 {before - inv.slots.Count}개 정리됨");
    }

#if UNITY_EDITOR
    [ContextMenu("Debug → Reset Inventory (Both)")]
    void ResetInventory()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        save = new InventorySaveData();
        OnInventoryChanged?.Invoke();
        Debug.Log("[Inventory] Human/Cat 인벤토리 모두 초기화 완료");
    }

    [ContextMenu("Debug → Print Inventory (Both)")]
    void PrintInventory()
    {
        Debug.Log($"[Inventory.Human] maxSlot={save.human.maxSlot}, used={save.human.slots.Count}");
        foreach (var s in save.human.slots) Debug.Log($"  H - {s.itemId} x{s.count}");
        Debug.Log($"[Inventory.Cat] maxSlot={save.cat.maxSlot}, used={save.cat.slots.Count}");
        foreach (var s in save.cat.slots) Debug.Log($"  C - {s.itemId} x{s.count}");
    }
#endif
}
