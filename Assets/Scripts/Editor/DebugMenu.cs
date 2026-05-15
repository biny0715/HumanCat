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
}
