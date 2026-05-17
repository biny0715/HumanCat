using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>한 가구의 배치 기록.</summary>
[Serializable]
public class PlacementRecord
{
    public string itemId;
    public float  x;
    public float  y;
}

/// <summary>JsonUtility 직렬화용 최상위 컨테이너.</summary>
[Serializable]
public class PlacementSaveData
{
    public List<PlacementRecord> records = new List<PlacementRecord>();
}

/// <summary>
/// 가구 배치 데이터의 영구 저장 (PlayerPrefs + JSON).
///
/// [설계 의도]
/// - 정적 클래스. 모든 매니저/UI에서 직접 호출 가능.
/// - 키 네임스페이스 'Furniture.Placements' 로 분리 (다른 시스템과 충돌 X).
/// - Add / Remove / Clear 만 노출하고 내부 캐시는 lazy load.
/// </summary>
public static class PlacementRepository
{
    public const string SaveKey = "Furniture.Placements";

    static PlacementSaveData cache;

    static PlacementSaveData Data
    {
        get
        {
            if (cache == null) Load();
            return cache;
        }
    }

    static void Load()
    {
        string raw = PlayerPrefs.GetString(SaveKey, null);
        if (string.IsNullOrEmpty(raw))
        {
            cache = new PlacementSaveData();
            return;
        }
        try
        {
            cache = JsonUtility.FromJson<PlacementSaveData>(raw) ?? new PlacementSaveData();
        }
        catch
        {
            Debug.LogWarning("[PlacementRepository] 저장 데이터 파싱 실패 — 초기화");
            cache = new PlacementSaveData();
        }
        if (cache.records == null) cache.records = new List<PlacementRecord>();
    }

    static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }

    public static IReadOnlyList<PlacementRecord> All => Data.records;

    /// <summary>가구를 배치하면 호출.</summary>
    public static void Add(string itemId, Vector2 pos)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        Data.records.Add(new PlacementRecord { itemId = itemId, x = pos.x, y = pos.y });
        Save();
    }

    /// <summary>전체 초기화 (디버그 / 새 게임 용).</summary>
    public static void Clear()
    {
        cache = new PlacementSaveData();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
