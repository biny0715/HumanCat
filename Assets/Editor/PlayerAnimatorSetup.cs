using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class PlayerAnimatorSetup
{
    const string IdleSpritePath  = "Assets/Art/Cat/NormalCat/normalCat_Idle.png";
    const string WalkSpritePath  = "Assets/Art/Cat/NormalCat/normal_Cat_Walk.png";
    const string RunSpritePath   = "Assets/Art/Cat/NormalCat/normalCat_Run.png";
    const string AnimDir         = "Assets/Animations";
    const string ControllerPath  = "Assets/Animations/PlayerController.controller";
    const string IdleClipPath    = "Assets/Animations/CatIdle.anim";
    const string WalkClipPath    = "Assets/Animations/CatWalk.anim";
    const string RunClipPath     = "Assets/Animations/CatRun.anim";
    const float  Fps             = 10f;

    [MenuItem("HumanCat/Create PlayerAnimatorController")]
    static void Create()
    {
        System.IO.Directory.CreateDirectory(AnimDir);

        var idleClip = CreateClip(IdleSpritePath, "normalCat_Idle",   IdleClipPath, loop: true);
        var walkClip = CreateClip(WalkSpritePath, "normal_Cat_Walk",  WalkClipPath, loop: true);
        var runClip  = CreateClip(RunSpritePath,  "normalCat_Run",    RunClipPath,  loop: true);

        BuildController(idleClip, walkClip, runClip);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlayerAnimatorSetup] 완료 → " + ControllerPath);
    }

    static AnimationClip CreateClip(string texturePath, string spritePrefix, string savePath, bool loop)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith(spritePrefix))
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError($"[PlayerAnimatorSetup] 스프라이트 없음: {texturePath} / prefix={spritePrefix}");
            return null;
        }

        var clip = new AnimationClip { frameRate = Fps };

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding = new EditorCurveBinding
        {
            type         = typeof(SpriteRenderer),
            path         = "",
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / Fps, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AssetDatabase.CreateAsset(clip, savePath);
        Debug.Log($"[PlayerAnimatorSetup] 클립: {savePath} ({sprites.Length}프레임)");
        return clip;
    }

    static void BuildController(AnimationClip idleClip, AnimationClip walkClip, AnimationClip runClip)
    {
        // 기존 컨트롤러가 있으면 재사용 (씬 참조 유지), 없으면 새로 생성
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                         ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // 기존 파라미터·레이어 초기화
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);
        var existingSm = controller.layers[0].stateMachine;
        foreach (var s in existingSm.states)
            existingSm.RemoveState(s.state);
        foreach (var t in existingSm.anyStateTransitions)
            existingSm.RemoveAnyStateTransition(t);
        controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // 상태 추가
        var idle = sm.AddState("Idle");
        idle.motion = idleClip;

        var walk = sm.AddState("Walk");
        walk.motion = walkClip;

        var run = sm.AddState("Run");   // 미니게임용 — 트랜지션 없음
        run.motion = runClip;

        sm.defaultState = idle;

        // Idle → Walk
        var toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration    = 0.05f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        // Walk → Idle
        var toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration    = 0.05f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        Selection.activeObject = controller;
        Debug.Log("[PlayerAnimatorSetup] Idle↔Walk 트랜지션 완료. Run은 미니게임용 독립 상태.");
    }
}