using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// MiniGame_Chase 씬의 Player / TargetDummy에 캐릭터 비주얼을 설정한다.
///
/// HumanCat → MiniGame → Setup Characters 메뉴에서 실행.
///
/// - Player      : NormalCat 스프라이트 + PlayerController.controller + PlayerAnimator
/// - TargetDummy : BlackCat 스프라이트 + BlackCatController.controller (isRunning bool)
/// </summary>
public static class MiniGameCharacterSetup
{
    const string PlayerControllerPath  = "Assets/Animations/PlayerController.controller";
    const string BlackCtrlPath         = "Assets/Animations/BlackCatController.controller";
    const string BlackIdleClipPath     = "Assets/Animations/BlackCat_Idle.anim";
    const string BlackRunClipPath      = "Assets/Animations/BlackCat_Run.anim";
    const string BlackIdleAtlasPath    = "Assets/Art/Cat/BlackCat/BlackCat_Idle.png";
    const string BlackRunAtlasPath     = "Assets/Art/Cat/BlackCat/BlackCat_Run.png";

    [MenuItem("HumanCat/MiniGame/Setup Characters")]
    public static void SetupCharacters()
    {
        EnsureBlackCatAssets();
        SetupPlayer();
        SetupTargetDummy();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        Debug.Log("[MiniGameCharacterSetup] 완료!");
    }

    // ── BlackCat 에셋 생성 ────────────────────────────────────────────────

    static void EnsureBlackCatAssets()
    {
        EnsureBlackCatClip(BlackIdleClipPath, BlackIdleAtlasPath, "BlackCat_Idle", 6, 10f);
        EnsureBlackCatClip(BlackRunClipPath,  BlackRunAtlasPath,  "BlackCat_Run",  9, 10f);
        EnsureBlackCatController();
    }

    static void EnsureBlackCatClip(string clipPath, string atlasPath, string prefix, int frameCount, float fps)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip { frameRate = fps };
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        // Loop 설정
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // 스프라이트 로드
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
        var sprites = new List<Sprite>();
        foreach (var obj in allAssets)
            if (obj is Sprite sp) sprites.Add(sp);

        sprites.Sort((a, b) =>
        {
            int ia = ExtractIndex(a.name, prefix);
            int ib = ExtractIndex(b.name, prefix);
            return ia.CompareTo(ib);
        });

        if (sprites.Count == 0)
        {
            Debug.LogError($"[MiniGameCharacterSetup] 스프라이트 없음: {atlasPath}");
            return;
        }

        // 키프레임 설정
        float interval = 1f / fps;
        var binding = new EditorCurveBinding
        {
            type         = typeof(SpriteRenderer),
            path         = "",
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i * interval, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        EditorUtility.SetDirty(clip);
        Debug.Log($"[MiniGameCharacterSetup] {clipPath} 생성 ({sprites.Count}프레임)");
    }

    static void EnsureBlackCatController()
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(BlackCtrlPath);
        if (ctrl != null) return;

        ctrl = AnimatorController.CreateAnimatorControllerAtPath(BlackCtrlPath);

        // Bool 파라미터 추가
        ctrl.AddParameter("isRunning", AnimatorControllerParameterType.Bool);

        var root = ctrl.layers[0].stateMachine;

        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BlackIdleClipPath);
        var runClip  = AssetDatabase.LoadAssetAtPath<AnimationClip>(BlackRunClipPath);

        var idleState = root.AddState("Idle");
        idleState.motion = idleClip;
        root.defaultState = idleState;

        var runState = root.AddState("Run");
        runState.motion = runClip;

        // Idle → Run
        var toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration    = 0f;
        toRun.AddCondition(AnimatorConditionMode.If, 0, "isRunning");

        // Run → Idle
        var toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration    = 0f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");

        EditorUtility.SetDirty(ctrl);
        Debug.Log($"[MiniGameCharacterSetup] {BlackCtrlPath} 생성");
    }

    // ── Player 설정 ───────────────────────────────────────────────────────

    static void SetupPlayer()
    {
        var playerGO = GameObject.Find("Player");
        if (playerGO == null) { Debug.LogError("[MiniGameCharacterSetup] Player 오브젝트 없음"); return; }

        // SpriteRenderer
        var sr = playerGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = playerGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // NormalCat idle 첫 프레임을 기본 스프라이트로
        var idleSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Cat/NormalCat/normalCat_Idle.png");
        foreach (var obj in idleSprites)
            if (obj is Sprite sp && sp.name.EndsWith("_0")) { sr.sprite = sp; break; }

        // Animator
        var anim = playerGO.GetComponent<Animator>();
        if (anim == null) anim = playerGO.AddComponent<Animator>();
        var playerCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath);
        if (playerCtrl != null) anim.runtimeAnimatorController = playerCtrl;

        // PlayerAnimator
        if (playerGO.GetComponent<PlayerAnimator>() == null)
            playerGO.AddComponent<PlayerAnimator>();

        Debug.Log("[MiniGameCharacterSetup] Player 설정 완료");
    }

    // ── TargetDummy 설정 ──────────────────────────────────────────────────

    static void SetupTargetDummy()
    {
        var dummyGO = GameObject.Find("TargetDummy");
        if (dummyGO == null) { Debug.LogError("[MiniGameCharacterSetup] TargetDummy 오브젝트 없음"); return; }

        // SpriteRenderer
        var sr = dummyGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = dummyGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // BlackCat idle 첫 프레임을 기본 스프라이트로
        var idleSprites = AssetDatabase.LoadAllAssetsAtPath(BlackIdleAtlasPath);
        foreach (var obj in idleSprites)
            if (obj is Sprite sp && sp.name.EndsWith("_0")) { sr.sprite = sp; break; }

        // Animator
        var anim = dummyGO.GetComponent<Animator>();
        if (anim == null) anim = dummyGO.AddComponent<Animator>();
        var blackCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BlackCtrlPath);
        if (blackCtrl != null) anim.runtimeAnimatorController = blackCtrl;

        Debug.Log("[MiniGameCharacterSetup] TargetDummy 설정 완료");
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    static int ExtractIndex(string name, string prefix)
    {
        string suffix = name.Substring(prefix.Length).TrimStart('_');
        return int.TryParse(suffix, out int n) ? n : 0;
    }
}
