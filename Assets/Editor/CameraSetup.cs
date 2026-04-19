using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HumanCat > Setup Camera System 메뉴로 Cinemachine 카메라 시스템을 자동 구성한다.
/// Cinemachine 3.x (com.unity.cinemachine) 기준.
/// </summary>
public static class CameraSetup
{
    [MenuItem("HumanCat/Setup Camera System")]
    static void Setup()
    {
        var player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("[CameraSetup] Player 오브젝트를 찾을 수 없음. " +
                             "MaleHuman 또는 NormalCat이 씬에 있어야 합니다.");
            return;
        }

        SetupMainCamera();
        SetupCinemachineCamera(player);
        SetupConfiner();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[CameraSetup] 카메라 시스템 설정 완료");
    }

    // ── Main Camera 준비 ─────────────────────────────────────────────────

    static void SetupMainCamera()
    {
        var mainCam = Camera.main;
        if (mainCam == null) { Debug.LogWarning("[CameraSetup] Main Camera를 찾을 수 없음"); return; }

        // 구버전 CameraFollow 스크립트 제거
        var oldFollow = mainCam.GetComponent<CameraFollow>();
        if (oldFollow != null)
        {
            Object.DestroyImmediate(oldFollow);
            Debug.Log("[CameraSetup] 구버전 CameraFollow 제거 완료");
        }

        // CinemachineBrain 추가 (Cinemachine이 Main Camera를 구동하려면 필수)
        GetOrAdd<CinemachineBrain>(mainCam.gameObject);
        Debug.Log("[CameraSetup] CinemachineBrain → Main Camera 추가 완료");
    }

    // ── Cinemachine Camera ────────────────────────────────────────────────

    static void SetupCinemachineCamera(Transform followTarget)
    {
        // 기존 CinemachineCamera가 있으면 재사용
        var existing = Object.FindAnyObjectByType<CinemachineCamera>();
        var vcamGo   = existing != null
            ? existing.gameObject
            : new GameObject("CM_PlayerCamera");

        var vcam   = GetOrAdd<CinemachineCamera>(vcamGo);
        vcam.Follow = followTarget;

        // 2D에서 불필요한 RotationComposer 제거
        var rotComposer = vcamGo.GetComponent<CinemachineRotationComposer>();
        if (rotComposer != null) Object.DestroyImmediate(rotComposer);

        // PositionComposer (Dead Zone + Damping)
        var posComposer = GetOrAdd<CinemachinePositionComposer>(vcamGo);

        // Cinemachine 3.x: Composition 구조체를 통해 Dead Zone 설정
        var composition = posComposer.Composition;

        // Dead Zone: 화면 중앙 30% x 20% 범위 안에서는 카메라 고정
        composition.DeadZone.Enabled = true;
        composition.DeadZone.Size    = new Vector2(0.3f, 0.2f);

        // Hard Limits: Dead Zone 바깥 경계. 이 범위를 벗어나면 즉시 이동
        composition.HardLimits.Enabled = true;
        composition.HardLimits.Size    = new Vector2(0.8f, 0.8f);

        posComposer.Composition = composition;

        // Damping: 모바일 탑다운 권장값 (1~1.5)
        posComposer.Damping = new Vector3(1.2f, 1.2f, 0f);

        // Main Camera와 동일한 Orthographic Size 유지
        var mainCam = Camera.main;
        if (mainCam != null)
            vcam.Lens.OrthographicSize = mainCam.orthographicSize;

        Debug.Log($"[CameraSetup] CinemachineCamera 설정 완료 → Follow: {followTarget.name}");
    }

    // ── Confiner (카메라 이동 범위 제한) ─────────────────────────────────

    static void SetupConfiner()
    {
        // CameraConfiner 오브젝트 생성 또는 재사용
        var confinerGo = GameObject.Find("CameraConfiner") ?? new GameObject("CameraConfiner");
        var poly       = GetOrAdd<PolygonCollider2D>(confinerGo);
        poly.isTrigger = true; // Cinemachine 전용 — 물리 충돌 없어야 함

        // 배경 SpriteRenderer의 bounds를 기준으로 Collider 크기 설정
        var bgBounds = GetBackgroundBounds();
        if (bgBounds.HasValue)
        {
            SetPolygonToBounds(poly, bgBounds.Value);
            Debug.Log($"[CameraSetup] Confiner bounds → {bgBounds.Value}");
        }
        else
        {
            // 배경을 찾지 못한 경우 기본 20x20 영역 설정
            SetPolygonToBounds(poly, new Bounds(Vector3.zero, new Vector3(20f, 20f, 0f)));
            Debug.LogWarning("[CameraSetup] 배경 스프라이트를 찾지 못해 기본 20x20 영역으로 설정. " +
                             "CameraConfiner의 PolygonCollider2D를 수동으로 조정하세요.");
        }

        // CinemachineCamera에 Confiner2D 추가 및 연결
        var vcam     = Object.FindAnyObjectByType<CinemachineCamera>();
        if (vcam == null) return;

        var confiner = GetOrAdd<CinemachineConfiner2D>(vcam.gameObject);
        confiner.BoundingShape2D = poly;
        confiner.Damping         = 0f; // 경계 도달 시 즉시 멈춤
    }

    // ── 배경 Bounds 계산 ─────────────────────────────────────────────────

    /// <summary>
    /// Outdoor/Background_Day 스프라이트 기준으로 bounds를 반환한다.
    /// 없으면 씬 전체 SpriteRenderer 중 가장 큰 것을 사용.
    /// </summary>
    static Bounds? GetBackgroundBounds()
    {
        // 우선 Background_Day 탐색
        var bgDayGo = GameObject.Find("Background_Day");
        if (bgDayGo != null)
        {
            var sr = bgDayGo.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                return sr.bounds;
        }

        // 대체: 씬에서 가장 큰 SpriteRenderer
        float    maxArea = 0f;
        Bounds?  largest = null;

        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            var b    = sr.bounds;
            var area = b.size.x * b.size.y;
            if (area > maxArea) { maxArea = area; largest = b; }
        }

        return largest;
    }

    static void SetPolygonToBounds(PolygonCollider2D poly, Bounds b)
    {
        var min = (Vector2)b.min;
        var max = (Vector2)b.max;

        poly.SetPath(0, new[]
        {
            new Vector2(min.x, min.y),
            new Vector2(min.x, max.y),
            new Vector2(max.x, max.y),
            new Vector2(max.x, min.y),
        });
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    static Transform FindPlayer()
    {
        string[] names = { "MaleHuman", "NormalCat", "Player" };
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null) return go.transform;
        }
        return null;
    }

    [MenuItem("HumanCat/Fix CameraConfiner Trigger")]
    static void FixConfinerTrigger()
    {
        var go = GameObject.Find("CameraConfiner");
        if (go == null) { Debug.LogWarning("[CameraSetup] CameraConfiner 없음"); return; }

        var poly = go.GetComponent<PolygonCollider2D>();
        if (poly == null) { Debug.LogWarning("[CameraSetup] PolygonCollider2D 없음"); return; }

        poly.isTrigger = true;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[CameraSetup] CameraConfiner.PolygonCollider2D → isTrigger=true 완료");
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}
