using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// MiniGame_Chase 씬 + 보너스 코인 시스템용 일회성 빌더.
/// MCP가 SpriteRenderer 등 내장 컴포넌트 프로퍼티를 못 만지므로,
/// 이 정적 메서드들을 거쳐 우회 설정한다.
///
/// 메뉴에서도 호출 가능: HumanCat → MiniGame → ...
/// </summary>
public static class MiniGameSceneBuilder
{
    const string FishCoinPrefabPath = "Assets/Prefabs/MiniGame/FishCoin.prefab";
    const string CoinSpritePath     = "Assets/Art/UI/HumanCat_Coin.png";

    // ── FishCoin 프리팹 빌드 ───────────────────────────────────────────────

    [MenuItem("HumanCat/MiniGame/Build FishCoin Prefab")]
    public static void BuildFishCoinPrefab()
    {
        // 디렉터리 보장
        const string dir = "Assets/Prefabs/MiniGame";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "MiniGame");
        }

        // 임시 GameObject 구성. 크기 조정은 디자이너가 프리팹에서 직접 관리하도록 코드가 강제하지 않는다.
        var go = new GameObject("FishCoin");
        try
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite           = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSpritePath);
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.5f;

            go.AddComponent<FishCoinPickup>();

            // 프리팹으로 저장 (덮어쓰기)
            PrefabUtility.SaveAsPrefabAsset(go, FishCoinPrefabPath);
            Debug.Log($"[MiniGameSceneBuilder] FishCoin 프리팹 저장 완료: {FishCoinPrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    // ── 씬에 FishCoinSpawner GameObject 배치 ──────────────────────────────

    [MenuItem("HumanCat/MiniGame/Setup FishCoinSpawner in Scene")]
    public static void SetupFishCoinSpawnerInScene()
    {
        // 기존 스포너가 있으면 재사용, 없으면 생성
        var existing = GameObject.Find("FishCoinSpawner");
        var spawnerGo = existing != null ? existing : new GameObject("FishCoinSpawner");
        var spawner   = spawnerGo.GetComponent<FishCoinSpawner>()
                     ?? spawnerGo.AddComponent<FishCoinSpawner>();

        // SerializedObject 로 SerializeField 슬롯 채우기
        var prefab = AssetDatabase.LoadAssetAtPath<FishCoinPickup>(FishCoinPrefabPath);
        var player = GameObject.Find("Player");

        var so = new SerializedObject(spawner);
        var pickupProp = so.FindProperty("pickupPrefab");
        var followProp = so.FindProperty("followTarget");
        if (pickupProp != null) pickupProp.objectReferenceValue = prefab;
        if (followProp != null) followProp.objectReferenceValue = player != null ? player.transform : null;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[MiniGameSceneBuilder] FishCoinSpawner 배치/슬롯 연결 완료");
    }

    // ── MiniGameManager 에 FishCoinSpawner 참조 연결 ──────────────────────

    [MenuItem("HumanCat/MiniGame/Wire MiniGameManager FishCoinSpawner")]
    public static void WireMiniGameManagerSpawner()
    {
        var mgmGo = GameObject.Find("MiniGameManager");
        if (mgmGo == null) { Debug.LogError("MiniGameManager GameObject 없음"); return; }
        var mgm = mgmGo.GetComponent<MiniGameManager>();
        if (mgm == null) { Debug.LogError("MiniGameManager 컴포넌트 없음"); return; }

        var spawnerGo = GameObject.Find("FishCoinSpawner");
        var spawner   = spawnerGo != null ? spawnerGo.GetComponent<FishCoinSpawner>() : null;
        if (spawner == null) { Debug.LogError("FishCoinSpawner 없음 → 먼저 Setup FishCoinSpawner in Scene 실행"); return; }

        var so = new SerializedObject(mgm);
        var prop = so.FindProperty("fishCoinSpawner");
        if (prop != null) prop.objectReferenceValue = spawner;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[MiniGameSceneBuilder] MiniGameManager.fishCoinSpawner 연결 완료");
    }

    // ── StatPanel 의 RewardText 생성 + StatUI 슬롯 연결 ───────────────────

    [MenuItem("HumanCat/MiniGame/Setup RewardText in StatPanel")]
    public static void SetupRewardText()
    {
        // LevelText 와 PointsText 사이에 RewardText 생성
        var levelGo  = GameObject.Find("LevelText");
        var pointsGo = GameObject.Find("PointsText");
        if (levelGo == null || pointsGo == null)
        {
            Debug.LogError("LevelText 또는 PointsText 를 찾을 수 없음");
            return;
        }

        var parent = levelGo.transform.parent;
        if (parent == null || pointsGo.transform.parent != parent)
        {
            Debug.LogError("LevelText/PointsText 부모가 다름 — StatPanel 안에 있는지 확인");
            return;
        }

        // 이미 존재하면 재사용
        var existing = parent.Find("RewardText");
        GameObject rewardGo;
        if (existing != null)
        {
            rewardGo = existing.gameObject;
        }
        else
        {
            // LevelText 를 복제해 기본 스타일을 가져옴
            rewardGo = Object.Instantiate(levelGo, parent);
            rewardGo.name = "RewardText";
        }

        // LevelText 바로 아래, PointsText 바로 위로 배치
        rewardGo.transform.SetSiblingIndex(levelGo.transform.GetSiblingIndex() + 1);

        // 텍스트 초기값
        var tmp = rewardGo.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = "기대 보상\n성공: -- / 캐치 시 추가: --";

        // StatUI 슬롯 연결
        var statUIGo = GameObject.Find("StatUI");
        var statUI   = statUIGo != null ? statUIGo.GetComponent<StatUI>() : null;
        if (statUI != null && tmp != null)
        {
            var so = new SerializedObject(statUI);
            var prop = so.FindProperty("rewardText");
            if (prop != null) prop.objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[MiniGameSceneBuilder] StatUI.rewardText 연결 완료");
        }
        else
        {
            Debug.LogWarning("StatUI 컴포넌트 미발견 — 슬롯 수동 연결 필요");
        }
    }

    // ── 한 번에 전부 ──────────────────────────────────────────────────────

    [MenuItem("HumanCat/MiniGame/Setup ALL (FishCoin + Spawner + RewardText)")]
    public static void SetupAll()
    {
        BuildFishCoinPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SetupFishCoinSpawnerInScene();
        WireMiniGameManagerSpawner();
        SetupRewardText();
        Debug.Log("[MiniGameSceneBuilder] 전체 세팅 완료 — 씬 저장 필요");
    }
}
