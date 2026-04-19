using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class HumanAnimatorSetup
{
    const string MaleIdlePath      = "Assets/Art/Human/Male/Male_Idle.png";
    const string MaleWalkPath      = "Assets/Art/Human/Male/Male_Walk.png";
    const string AnimDir           = "Assets/Animations/Human";
    const string HumanIdleClipPath = "Assets/Animations/Human/HumanIdle.anim";
    const string HumanWalkClipPath = "Assets/Animations/Human/HumanWalk.anim";
    const string ControllerPath    = "Assets/Animations/Human/HumanController.controller";
    const float  Fps               = 8f;

    [MenuItem("HumanCat/Create HumanAnimatorController")]
    static void Create()
    {
        System.IO.Directory.CreateDirectory(AnimDir);

        var idleClip = CreateClip(MaleIdlePath, "Male_Idle", HumanIdleClipPath);
        var walkClip = CreateClip(MaleWalkPath, "Male_Walk", HumanWalkClipPath);

        BuildController(idleClip, walkClip);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HumanAnimatorSetup] 완료 → " + ControllerPath);
    }

    static AnimationClip CreateClip(string texturePath, string spritePrefix, string savePath)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith(spritePrefix))
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError($"[HumanAnimatorSetup] 스프라이트 없음: {texturePath}");
            return null;
        }

        var clip = new AnimationClip { frameRate = Fps };

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
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
        Debug.Log($"[HumanAnimatorSetup] 클립: {savePath} ({sprites.Length}프레임)");
        return clip;
    }

    static void BuildController(AnimationClip idleClip, AnimationClip walkClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                         ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        while (controller.parameters.Length > 0) controller.RemoveParameter(0);
        var sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states) sm.RemoveState(s.state);

        controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);

        var idle = sm.AddState("Idle"); idle.motion = idleClip;
        var walk = sm.AddState("Walk"); walk.motion = walkClip;
        sm.defaultState = idle;

        var toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false; toWalk.duration = 0.05f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        var toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false; toIdle.duration = 0.05f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        Selection.activeObject = controller;
    }
}
