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
            // 처음 시작 유저: Resources/DefaultPlacements.asset 의 기본 배치를 적용
            cache = LoadDefaultOrEmpty();
            return;
        }
        try
        {
            cache = JsonUtility.FromJson<PlacementSaveData>(raw) ?? new PlacementSaveData();
        }
        catch
        {
            Debug.LogWarning("[PlacementRepository] 저장 데이터 파싱 실패 — 초기화");
            cache = LoadDefaultOrEmpty();
        }
        if (cache.records == null) cache.records = new List<PlacementRecord>();
    }

    /// <summary>
    /// Resources/DefaultPlacements.asset 을 읽어 PlacementSaveData 로 변환.
    /// 자산이 없거나 비어있으면 빈 데이터 반환.
    /// </summary>
    static PlacementSaveData LoadDefaultOrEmpty()
    {
        var defaultSet = Resources.Load<DefaultPlacementSet>("DefaultPlacements");
        var data = new PlacementSaveData();
        if (defaultSet == null || defaultSet.records == null || defaultSet.records.Count == 0)
            return data;
        foreach (var r in defaultSet.records)
        {
            if (r == null || string.IsNullOrEmpty(r.itemId)) continue;
            data.records.Add(new PlacementRecord { itemId = r.itemId, x = r.x, y = r.y });
        }
        Debug.Log($"[PlacementRepository] 기본 배치 적용 — {data.records.Count} 개");
        return data;
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

    /// <summary>
    /// itemId + 위치가 매칭되는 첫 레코드를 제거 (철거/이동에서 호출).
    /// 위치 비교는 epsilon 허용 — float 직렬화 오차 대비.
    /// </summary>
    public static bool Remove(string itemId, Vector2 pos, float epsilon = 0.01f)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        var list = Data.records;
        for (int i = 0; i < list.Count; i++)
        {
            var r = list[i];
            if (r.itemId != itemId) continue;
            if (Mathf.Abs(r.x - pos.x) > epsilon) continue;
            if (Mathf.Abs(r.y - pos.y) > epsilon) continue;
            list.RemoveAt(i);
            Save();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 전체 초기화 (디버그 / 새 게임 용). PlayerPrefs 를 지우고 cache 를 기본 배치로 되돌린다 —
    /// 다음 씬 진입 시 PlacementRestorer 가 기본 배치 그대로 복원.
    /// </summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        cache = LoadDefaultOrEmpty();
    }
}
