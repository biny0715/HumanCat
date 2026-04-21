using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class MiniGameDebugTools
{
    [MenuItem("HumanCat/MiniGame/Reset Dummy Position")]
    public static void ResetDummyPosition()
    {
        var dummy = GameObject.Find("TargetDummy");
        if (dummy == null) { Debug.LogError("TargetDummy 없음"); return; }
        dummy.transform.position = new Vector3(2f, 0f, 0f);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Debug] TargetDummy 위치 → (2, 0, 0)");
    }

    [MenuItem("HumanCat/MiniGame/Scale StatPanel x1.5")]
    public static void ScaleStatPanel()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvas 없음"); return; }

        var panel = canvas.transform.Find("StatPanel");
        if (panel == null) { Debug.LogError("StatPanel 없음"); return; }

        const float scale = 1.5f;

        foreach (var rt in panel.GetComponentsInChildren<RectTransform>(true))
        {
            if (rt == panel) continue; // 패널 자체는 제외

            var so = new SerializedObject(rt);
            so.Update();

            var anchoredPos = rt.anchoredPosition;
            var sizeDelta   = rt.sizeDelta;

            rt.anchoredPosition = anchoredPos * scale;
            rt.sizeDelta        = sizeDelta   * scale;

            EditorUtility.SetDirty(rt);
        }

        foreach (var tmp in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.fontSize *= scale;
            EditorUtility.SetDirty(tmp);
        }

        // PlayBtn sizeDelta 별도 처리 (위에서 이미 처리되지만 확인용)
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Debug] StatPanel 자식 요소 1.5배 적용 완료");
    }

    [MenuItem("HumanCat/MiniGame/Debug/Reset Level & Stats")]
    public static void ResetLevelAndStats()
    {
        PlayerPrefs.DeleteKey("mini_level");
        PlayerPrefs.DeleteKey("mini_statPoints");
        PlayerPrefs.DeleteKey("mini_speedStat");
        PlayerPrefs.DeleteKey("mini_hpStat");
        PlayerPrefs.DeleteKey("mini_resistStat");
        PlayerPrefs.Save();

        // 실행 중이면 StatManager 인스턴스도 즉시 반영
        var sm = Object.FindFirstObjectByType<StatManager>();
        if (sm != null)
        {
            var so = new SerializedObject(sm);
            so.Update();
            // StatManager.Load()가 private이므로 런타임 중엔 씬 재시작 안내
            Debug.Log("[Debug] PlayerPrefs 초기화 완료. 씬을 재시작하면 스탯이 반영됩니다.");
        }
        else
        {
            Debug.Log("[Debug] PlayerPrefs 초기화 완료 (Level=1, StatPoints=0, 모든 스탯=0)");
        }
    }

    [MenuItem("HumanCat/MiniGame/Debug/Print Current Stats")]
    public static void PrintCurrentStats()
    {
        int level  = PlayerPrefs.GetInt("mini_level",      1);
        int points = PlayerPrefs.GetInt("mini_statPoints", 0);
        int speed  = PlayerPrefs.GetInt("mini_speedStat",  0);
        int hp     = PlayerPrefs.GetInt("mini_hpStat",     0);
        int resist = PlayerPrefs.GetInt("mini_resistStat", 0);

        Debug.Log($"[Debug] Level={level}  StatPoints={points}  Speed={speed}  HP={hp}  Resist={resist}");
    }
}
