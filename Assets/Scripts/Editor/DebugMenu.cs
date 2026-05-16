using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터 전용 디버그 도구.
/// 메뉴 위치: HumanCat → Debug → ...
///
/// [Reset All Save Data]
/// PlayerPrefs 에 저장된 게임 측 모든 키를 일괄 삭제한다.
/// 영향 범위:
///   - 로그인 (Login.*)
///   - GameState / 위치 / Indoor 여부
///   - 게임 시간 (time_*)
///   - 미니게임 스탯/레벨 (mini_*)
///   - 재화 (Currency.*)
/// </summary>
public static class DebugMenu
{
    [MenuItem("HumanCat/Debug/Reset All Save Data")]
    public static void ResetAllSaveData()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Reset All Save Data",
            "PlayerPrefs 에 저장된 게임 데이터를 모두 삭제합니다.\n\n" +
            "• 로그인 / 사용자 이름 / 보호소 이름\n" +
            "• Day/Night 상태, 플레이어 위치/스케일, Indoor 여부\n" +
            "• 게임 시간\n" +
            "• 미니게임 레벨/스탯/포인트\n" +
            "• Fish / Gold 재화\n\n" +
            "되돌릴 수 없습니다. 진행할까요?",
            "삭제",
            "취소");
        if (!ok) return;

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[DebugMenu] PlayerPrefs 전체 삭제 완료.");

        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[DebugMenu] 현재 Play 중입니다. " +
                "메모리에 로드된 매니저 값(레벨/재화 등)은 다음 Play 재시작에 반영됩니다.");
        }
    }

    // ── 재화 강제 지급 ───────────────────────────────────────────────────

    [MenuItem("HumanCat/Debug/Add 1000 Fish + 1000 Gold")]
    public static void AddCoins()
    {
        const long delta = 1000;

        if (EditorApplication.isPlaying && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.Add(CurrencyType.Fish, delta);
            CurrencyManager.Instance.Add(CurrencyType.Gold, delta);
            Debug.Log("[DebugMenu] Fish +1000, Gold +1000 적용 (Play 중)");
            return;
        }

        // Edit 모드: PlayerPrefs 직접 갱신. CurrencyManager 와 동일하게 string 으로 long 직렬화.
        long fish = ReadLongPref(CurrencyManager.KeyFish) + delta;
        long gold = ReadLongPref(CurrencyManager.KeyGold) + delta;
        fish = ClampCurrency(fish);
        gold = ClampCurrency(gold);
        PlayerPrefs.SetString(CurrencyManager.KeyFish, fish.ToString(System.Globalization.CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(CurrencyManager.KeyGold, gold.ToString(System.Globalization.CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        Debug.Log($"[DebugMenu] Fish={fish:N0}, Gold={gold:N0} 저장 (다음 Play 시작 시 반영)");
    }

    static long ReadLongPref(string key)
    {
        string raw = PlayerPrefs.GetString(key, "0");
        return long.TryParse(raw, System.Globalization.NumberStyles.Integer,
                             System.Globalization.CultureInfo.InvariantCulture, out long v) ? v : 0;
    }

    static long ClampCurrency(long v)
        => v < 0 ? 0 : (v > CurrencyManager.MaxValue ? CurrencyManager.MaxValue : v);

    // ── 시간 강제 설정 ───────────────────────────────────────────────────

    [MenuItem("HumanCat/Debug/Set Game Time to 17-50")]
    public static void SetGameTimeTo1750() => SetGameTime(17, 50);

    /// <summary>
    /// 게임 시간을 (hour, minute)으로 설정.
    /// - Play 중: 활성 TimeManager 에 즉시 적용 + 저장
    /// - Edit 모드: PlayerPrefs(time_gameMinutes/time_saveTicks) 직접 갱신 → 다음 Play 시작 시 적용
    /// </summary>
    public static void SetGameTime(int hour, int minute)
    {
        float minutes = hour * 60f + minute;

        if (EditorApplication.isPlaying && TimeManager.Instance != null)
        {
            TimeManager.Instance.SetTime(hour, minute);
            TimeManager.Instance.Save();
            Debug.Log($"[DebugMenu] 게임 시간 {hour:00}:{minute:00} 적용 (Play 중)");
            return;
        }

        // Edit 모드 — PlayerPrefs 에 직접 기록. saveTicks 도 지금으로 갱신해
        // TimeManager.Load() 의 오프라인 경과 계산이 0이 되도록 한다.
        PlayerPrefs.SetFloat("time_gameMinutes", minutes);
        PlayerPrefs.SetString("time_saveTicks", System.DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
        Debug.Log($"[DebugMenu] 게임 시간 {hour:00}:{minute:00} 저장 (다음 Play 시작 시 적용)");
    }
}
