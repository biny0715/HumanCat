using UnityEngine;
using UnityEditor;

/// <summary>
/// HumanCat > Setup Backgrounds 메뉴로 배경 스프라이트 할당 및 씬 구조를 정리한다.
/// </summary>
public static class BackgroundSetup
{
    [MenuItem("HumanCat/Setup Backgrounds")]
    static void Setup()
    {
        CleanupStrayObjects();
        SetupOutdoorBackgrounds();
        SetupIndoorBackground();
        WireSceneController();
        MovePlayerToCharacters();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.Refresh();

        Debug.Log("[BackgroundSetup] 배경 설정 완료");
    }

    // ── 루트에 남은 중복 오브젝트 정리 ──────────────────────────────────

    static void CleanupStrayObjects()
    {
        // 루트 레벨의 중복 GameManager 제거 ([ Managers ] 안에 올바른 것이 있음)
        var strayGM = FindRootOnly("GameManager");
        if (strayGM != null) Object.DestroyImmediate(strayGM);

        // 루트 레벨의 구버전 Background 제거
        var strayBg = FindRootOnly("Background");
        if (strayBg != null) Object.DestroyImmediate(strayBg);
    }

    // ── Outdoor 배경 스프라이트 할당 ─────────────────────────────────────

    static void SetupOutdoorBackgrounds()
    {
        var outdoor = GameObject.Find("Outdoor");
        if (outdoor == null) { Debug.LogWarning("Outdoor 오브젝트를 찾을 수 없음"); return; }

        // Background_Day
        var bgDay = FindOrCreateChild(outdoor, "Background_Day");
        AssignBackground(bgDay, "Assets/Art/Backgrounds/Outdoor_Day.png", sortOrder: -10);

        // Background_Night (초기 비활성)
        var bgNight = FindOrCreateChild(outdoor, "Background_Night");
        AssignBackground(bgNight, "Assets/Art/Backgrounds/Outdoor_Night.png", sortOrder: -10);
        bgNight.SetActive(false);
    }

    // ── Indoor 배경 스프라이트 할당 ──────────────────────────────────────

    static void SetupIndoorBackground()
    {
        // GameObject.Find는 비활성 오브젝트를 탐색하지 못하므로 부모에서 직접 찾는다.
        var indoor = FindInactiveByName("Indoor");
        if (indoor == null) { Debug.LogWarning("Indoor 오브젝트를 찾을 수 없음"); return; }

        var bg = FindOrCreateChild(indoor, "Background");
        AssignBackground(bg, "Assets/Art/Backgrounds/Indoor_Day.png", sortOrder: -10);
    }

    // ── SceneController 레퍼런스 연결 ────────────────────────────────────

    static void WireSceneController()
    {
        var sc = Object.FindAnyObjectByType<SceneController>();
        if (sc == null) { Debug.LogWarning("SceneController를 찾을 수 없음"); return; }

        var outdoor = GameObject.Find("Outdoor");
        var indoor  = FindInactiveByName("Indoor");
        var outdoorBgDay   = outdoor?.transform.Find("Background_Day")?.gameObject;
        var outdoorBgNight = outdoor?.transform.Find("Background_Night")?.gameObject;
        var nightOverlay   = Object.FindAnyObjectByType<UnityEngine.UI.Image>();

        var so = new SerializedObject(sc);
        so.FindProperty("outdoorBgDay")  .objectReferenceValue = outdoorBgDay;
        so.FindProperty("outdoorBgNight").objectReferenceValue = outdoorBgNight;
        so.FindProperty("outdoorRoot")   .objectReferenceValue = outdoor;
        so.FindProperty("indoorRoot")    .objectReferenceValue = indoor;

        // NightOverlay Image 연결 (NightOverlay 이름의 오브젝트 우선 탐색)
        var overlayGo = GameObject.Find("NightOverlay");
        if (overlayGo != null)
        {
            var overlayImg = overlayGo.GetComponent<UnityEngine.UI.Image>();
            if (overlayImg != null)
                so.FindProperty("nightOverlay").objectReferenceValue = overlayImg;
        }

        so.ApplyModifiedProperties();
        Debug.Log("[BackgroundSetup] SceneController 레퍼런스 연결 완료");
    }

    // ── Player를 [ Characters ] 하위로 이동 ──────────────────────────────

    static void MovePlayerToCharacters()
    {
        var characters = GameObject.Find("[ Characters ]");
        if (characters == null) return;

        // MaleHuman 또는 NormalCat이 루트에 있으면 이동
        string[] playerNames = { "MaleHuman", "NormalCat", "Player" };
        foreach (var playerName in playerNames)
        {
            var player = FindRootOnly(playerName);
            if (player != null)
                player.transform.SetParent(characters.transform, worldPositionStays: true);
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    static void AssignBackground(GameObject go, string spritePath, int sortOrder)
    {
        // 스프라이트 로드
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            // Sprite 모드가 Single이 아닌 경우 서브에셋으로 로드
            var assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            foreach (var asset in assets)
                if (asset is Sprite s) { sprite = s; break; }
        }

        if (sprite == null)
        {
            Debug.LogWarning($"스프라이트 로드 실패: {spritePath}");
            return;
        }

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();

        sr.sprite       = sprite;
        sr.sortingOrder = sortOrder;
    }

    static GameObject FindOrCreateChild(GameObject parent, string childName)
    {
        var existing = parent.transform.Find(childName);
        if (existing != null) return existing.gameObject;

        var child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    /// <summary>씬 루트 레벨에서만 탐색 (자식 포함 X).</summary>
    static GameObject FindRootOnly(string name)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    /// <summary>비활성 오브젝트 포함 이름 탐색. Transform.Find는 비활성도 탐색 가능.</summary>
    static GameObject FindInactiveByName(string name)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            var found = root.transform.Find(name);
            if (found != null) return found.gameObject;
            // 한 단계 더 깊이 탐색
            foreach (Transform child in root.transform)
            {
                var deep = child.Find(name);
                if (deep != null) return deep.gameObject;
            }
        }
        return null;
    }
}
