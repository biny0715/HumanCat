using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>한 마리 고양이의 저장 단위. JsonUtility 직렬화용 public 필드.</summary>
[Serializable]
public class CatInstanceData
{
    public string itemId;
    public float  x, y, z;

    public Vector3 Position => new Vector3(x, y, z);
}

/// <summary>JsonUtility 최상위 컨테이너.</summary>
[Serializable]
public class CatSaveData
{
    public List<CatInstanceData> cats = new List<CatInstanceData>();
}

/// <summary>
/// 고양이 NPC 의 구매/저장/스폰/제거를 중앙 관리하는 싱글톤.
///
/// [설계 의도]
/// - InventoryManager 와 완전히 분리 — Cat 은 "아이템" 이 아니라 "엔티티" 라는 사용자 요구사항 반영.
/// - 저장 단위는 CatInstanceData(itemId + position) 한 장. PlayerPrefs 키 'CatNpc.Data' 에 JSON.
/// - Indoor 첫 진입 시 1회만 Load 결과로 스폰 (loaded 플래그). 중복 스폰 방지.
/// - 가시성: catRoot 가 [ Environment ]/Indoor 자식이면 SceneController 가 Indoor 토글 시 자동 비활성.
/// - 신규 SpawnCat 위치는 catRoot 기준 spawnSearchRadius 반경 + Floor 마스크 검사.
/// </summary>
public class CatManager : MonoBehaviour
{
    public static CatManager Instance { get; private set; }

    public const string SaveKey = "CatNpc.Data";

    [Header("Scene Refs")]
    [Tooltip("스폰된 고양이의 부모. 권장: [ Environment ]/Indoor/Cats")]
    [SerializeField] Transform catRoot;
    [Tooltip("Floor 영역 판정. spawn 위치가 이 마스크 안에 있는 점만 허용.")]
    [SerializeField] LayerMask floorMask;

    [Header("Spawn")]
    [Tooltip("Floor 위 spawn 위치 탐색 반경 (catRoot 기준).")]
    [SerializeField] float spawnSearchRadius = 10f;
    [Min(1)] [SerializeField] int  spawnSearchMaxTries = 32;

    [Header("Bootstrap")]
    [Tooltip("Resources 하위에서 CatItemData 자산을 스캔할 폴더. 기본 'CatItems'.")]
    [SerializeField] string catItemsResourcesFolder = "CatItems";

    /// <summary>현재 활성화된 NPC 인스턴스 (참조 추적).</summary>
    readonly List<CatNPC> activeCats = new List<CatNPC>();

    Dictionary<string, CatItemData> registry = new Dictionary<string, CatItemData>();
    CatSaveData save;
    bool        spawnedOnce;

    public IReadOnlyList<CatNPC> ActiveCats => activeCats;

    /// <summary>고양이 목록(save.cats) 가 변경될 때 발화. Spawn / Remove / Load 후.</summary>
    public event System.Action OnCatChanged;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad 안 함 — catRoot 가 씬의 Indoor/Cats 를 참조하므로 씬 전환 시
        // CatManager 도 함께 파괴되어야 fake-null 참조 발생 방지.
        // 매 씬 진입 시 새 CatManager + 새 catRoot. PlayerPrefs 로 데이터 영속화.

        LoadRegistry();
        Load();
    }

    void Start()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged += HandleEnvironmentChanged;

        // 이미 Indoor 라면 즉시 스폰
        if (SceneController.Instance != null &&
            SceneController.Instance.CurrentEnvironment == EnvironmentType.Indoor)
            EnsureSpawnedFromSave();
    }

    void OnDestroy()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.OnEnvironmentChanged -= HandleEnvironmentChanged;
        if (Instance == this) Instance = null;
    }

    void HandleEnvironmentChanged(EnvironmentType env)
    {
        if (env == EnvironmentType.Indoor)
            EnsureSpawnedFromSave();
        // Outdoor 시 SetActive 토글은 안 함 — catRoot 가 Indoor 자식이면 부모가 자동 비활성
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    /// <summary>itemId 로 CatItemData 조회. 없으면 null.</summary>
    public CatItemData GetItem(string itemId)
        => registry != null && itemId != null && registry.TryGetValue(itemId, out var d) ? d : null;

    /// <summary>해당 itemId 의 고양이를 이미 보유 중인지 (저장 데이터 기준 — Indoor 미진입 상태에서도 정확).</summary>
    public bool HasCat(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || save == null) return false;
        foreach (var r in save.cats)
            if (r != null && r.itemId == itemId) return true;
        return false;
    }

    /// <summary>
    /// 신규 고양이 스폰. 인벤토리는 건드리지 않음 (Shop.Buy 에서 재화 차감 후 호출).
    /// Indoor 가 아니면 실패. spawn 위치는 Floor 마스크 안에서 랜덤 — 실패 시 catRoot 위치.
    /// </summary>
    public bool SpawnCat(CatItemData data)
    {
        if (data == null || data.CatPrefab == null)
        {
            Debug.LogWarning("[CatManager] CatItemData 또는 catPrefab 없음");
            return false;
        }
        if (catRoot == null)
        {
            Debug.LogWarning("[CatManager] catRoot 미연결 — Setup 메뉴 실행 필요");
            return false;
        }
        if (SceneController.Instance == null ||
            SceneController.Instance.CurrentEnvironment != EnvironmentType.Indoor)
        {
            Debug.LogWarning("[CatManager] Indoor 가 아님 — 스폰 보류");
            return false;
        }

        Vector3 pos      = PickRandomFloorPosition();
        var     npc      = InstantiateCat(data, pos);
        if (npc == null) return false;

        save.cats.Add(new CatInstanceData {
            itemId = data.ItemId, x = pos.x, y = pos.y, z = pos.z
        });
        Save();
        OnCatChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// CatNPC 인스턴스를 제거 + 저장 데이터에서 가장 가까운 매칭 레코드 한 개 삭제.
    /// Destroy 는 호출자가 직접 — 매니저는 데이터만 정리.
    /// </summary>
    public bool RemoveCat(CatNPC npc)
    {
        if (npc == null) return false;
        activeCats.Remove(npc);

        // 같은 itemId 의 레코드 중 위치 기준 가장 가까운 1개 삭제
        Vector3 pos    = npc.transform.position;
        int     bestIx = -1;
        float   bestSq = float.MaxValue;
        for (int i = 0; i < save.cats.Count; i++)
        {
            var r = save.cats[i];
            if (r.itemId != npc.ItemId) continue;
            float sq = (r.Position - pos).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; bestIx = i; }
        }
        if (bestIx >= 0) save.cats.RemoveAt(bestIx);

        Save();
        OnCatChanged?.Invoke();
        return true;
    }

    // ── 내부 ──────────────────────────────────────────────────────────────

    void EnsureSpawnedFromSave()
    {
        if (spawnedOnce) return;
        spawnedOnce = true;

        if (catRoot == null)
        {
            Debug.LogWarning("[CatManager] catRoot 미연결 — Load 한 고양이 복원 불가");
            return;
        }

        int restored = 0;
        foreach (var r in save.cats)
        {
            var data = GetItem(r.itemId);
            if (data == null || data.CatPrefab == null) continue;
            if (InstantiateCat(data, r.Position) != null) restored++;
        }
        if (restored > 0)
        {
            Debug.Log($"[CatManager] 고양이 {restored} 마리 복원 완료");
            OnCatChanged?.Invoke();
        }
    }

    CatNPC InstantiateCat(CatItemData data, Vector3 pos)
    {
        var go  = Instantiate(data.CatPrefab, pos, Quaternion.identity, catRoot);
        // 부모(Indoor 등)의 lossyScale 영향을 무력화 — prefab.localScale 그대로 보이도록.
        // PlacementManager 의 가구 처리와 동일 패턴.
        PlacementManager.NormalizeScale(go, data.CatPrefab, catRoot);

        var npc = go.GetComponent<CatNPC>();
        if (npc == null) npc = go.AddComponent<CatNPC>();
        npc.Setup(data.ItemId);
        activeCats.Add(npc);
        return npc;
    }

    Vector3 PickRandomFloorPosition()
    {
        Vector3 center = catRoot.position;
        for (int i = 0; i < spawnSearchMaxTries; i++)
        {
            Vector2 candidate = (Vector2)center + UnityEngine.Random.insideUnitCircle * spawnSearchRadius;
            if (Physics2D.OverlapPoint(candidate, floorMask) != null)
                return new Vector3(candidate.x, candidate.y, 0f);
        }
        Debug.LogWarning("[CatManager] Floor 위 spawn 위치 찾지 못함 — center 로 fallback");
        return center;
    }

    void LoadRegistry()
    {
        registry.Clear();
        var assets = Resources.LoadAll<CatItemData>(catItemsResourcesFolder);
        foreach (var a in assets)
        {
            if (a == null || string.IsNullOrEmpty(a.ItemId)) continue;
            registry[a.ItemId] = a;
        }
    }

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
            save = new CatSaveData();
            return;
        }
        try
        {
            save = JsonUtility.FromJson<CatSaveData>(raw) ?? new CatSaveData();
        }
        catch
        {
            Debug.LogWarning("[CatManager] 저장 데이터 파싱 실패 — 초기화");
            save = new CatSaveData();
        }
        if (save.cats == null) save.cats = new List<CatInstanceData>();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug → Reset Cat Save Data")]
    void ResetCatSaveData()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        save = new CatSaveData();
        Debug.Log("[CatManager] 고양이 저장 데이터 초기화 완료");
    }
#endif
}
