using System;
using System.Globalization;
using UnityEngine;

public enum CurrencyType { Fish, Gold }

/// <summary>
/// Fish / Gold 두 재화를 관리하는 싱글톤.
///
/// [설계 의도]
/// - 값 변경은 반드시 Add / TrySubtract / Set 을 통해서만 수행 (직접 set 차단).
/// - 변경 즉시 PlayerPrefs에 저장 → 앱 강제 종료 후에도 유지.
/// - UI 등 외부 모듈은 OnCurrencyChanged 이벤트를 구독해 반응한다 (폴링 금지).
/// - DontDestroyOnLoad: 씬 전환 시에도 값/이벤트 구독자 유지.
/// - 100억(10^10)은 int 범위를 넘으므로 long 사용.
///   PlayerPrefs 는 long 미지원이라 문자열로 저장.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    /// <summary>각 재화의 상한값 (100억).</summary>
    public const long MaxValue = 10_000_000_000L;

    /// <summary>최초 실행 시 플레이어에게 지급되는 시작 재화.</summary>
    public const long StartingFish = 10;
    public const long StartingGold = 500;

    // 다른 매니저와 충돌 방지용 prefix
    public const string KeyFish = "Currency.Fish";
    public const string KeyGold = "Currency.Gold";

    /// <summary>(타입, 새 값) — 값이 실제로 바뀐 경우에만 호출.</summary>
    public event Action<CurrencyType, long> OnCurrencyChanged;

    public long Fish { get; private set; }
    public long Gold { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // Awake 단계에서 로드 → 다른 컴포넌트의 Start/OnEnable 가 안전하게 값 읽음.
        Load();
    }

    // ── 퍼블릭 API ───────────────────────────────────────────────────────

    public long Get(CurrencyType type)
        => type == CurrencyType.Fish ? Fish : Gold;

    /// <summary>지정한 재화를 amount 만큼 증가. 0 이하 무시. Max 초과 시 Max로 클램프.</summary>
    public void Add(CurrencyType type, long amount)
    {
        if (amount <= 0) return;
        SetInternal(type, Get(type) + amount);
    }

    /// <summary>차감 시도. 잔액 부족이면 false 반환(차감하지 않음).</summary>
    public bool TrySubtract(CurrencyType type, long amount)
    {
        if (amount <= 0) return false;
        long current = Get(type);
        if (current < amount) return false;
        SetInternal(type, current - amount);
        return true;
    }

    /// <summary>강제로 특정 값으로 설정 (디버그/보상 지급 등). 0 ~ Max 로 클램프.</summary>
    public void Set(CurrencyType type, long value)
        => SetInternal(type, value);

    // ── 내부 로직 ────────────────────────────────────────────────────────

    void SetInternal(CurrencyType type, long value)
    {
        long clamped = Clamp(value);
        long before  = Get(type);
        if (before == clamped) return;

        if (type == CurrencyType.Fish) Fish = clamped;
        else                           Gold = clamped;

        Save(type, clamped);
        OnCurrencyChanged?.Invoke(type, clamped);
    }

    static long Clamp(long value)
    {
        if (value < 0)        return 0;
        if (value > MaxValue) return MaxValue;
        return value;
    }

    // ── 저장 / 불러오기 ──────────────────────────────────────────────────

    void Save(CurrencyType type, long value)
    {
        string key = type == CurrencyType.Fish ? KeyFish : KeyGold;
        PlayerPrefs.SetString(key, value.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
    }

    void Load()
    {
        // 키가 없으면 최초 실행으로 간주하고 시작 재화를 시드.
        // (DebugMenu 가 Edit 모드에서 키를 미리 만들면 정상값을 그대로 사용)
        if (PlayerPrefs.HasKey(KeyFish))
        {
            Fish = ReadLong(KeyFish);
        }
        else
        {
            Fish = StartingFish;
            Save(CurrencyType.Fish, Fish);
        }

        if (PlayerPrefs.HasKey(KeyGold))
        {
            Gold = ReadLong(KeyGold);
        }
        else
        {
            Gold = StartingGold;
            Save(CurrencyType.Gold, Gold);
        }
    }

    static long ReadLong(string key)
    {
        string raw = PlayerPrefs.GetString(key, "0");
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v))
            return Clamp(v);
        return 0;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug → Reset Currencies")]
    void ResetCurrencies()
    {
        PlayerPrefs.DeleteKey(KeyFish);
        PlayerPrefs.DeleteKey(KeyGold);
        PlayerPrefs.Save();
        Fish = 0;
        Gold = 0;
        OnCurrencyChanged?.Invoke(CurrencyType.Fish, 0);
        OnCurrencyChanged?.Invoke(CurrencyType.Gold, 0);
        Debug.Log("[CurrencyManager] 재화 초기화 완료");
    }

    [ContextMenu("Debug → Add 1,000 Fish")]
    void DebugAddFish() => Add(CurrencyType.Fish, 1000);

    [ContextMenu("Debug → Add 1,000 Gold")]
    void DebugAddGold() => Add(CurrencyType.Gold, 1000);
#endif
}
