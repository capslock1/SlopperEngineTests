using System;
using System.Collections.Generic;
using SlopperEngine.Core;
using SlopperEngine.SceneObjects;

namespace TestProgram.RenderTest;

/// <summary>
/// Plays an object's motion.
/// </summary>
public class FlyThroughPlayer : SceneObject
{
    /// <summary>
    /// Whether or not the motion is currently being played.
    /// </summary>
    public bool Playing;
    /// <summary>
    /// How many seconds into the animation the player is.
    /// </summary>
    public float AnimationProgress;
    /// <summary>
    /// The animation to use.
    /// </summary>
    public List<Keyframe> Keyframes = [];
    /// <summary>
    /// Overrides the frequency at which keyframes of the animation are evaluated.
    /// if 0 or less, animation will be played at real time instead.
    /// </summary>
    public float OverrideAnimationFrequency = -1;
    /// <summary>
    /// Gets called when the animation is finished.
    /// </summary>
    public event Action? OnAnimationFinish = null;

    [OnFrameUpdate]
    void Update(FrameUpdateArgs args)
    {
        if (!Playing) return;
        if(Keyframes.Count <= 0)
        {
            System.Console.WriteLine("couldnt play flythrough; it has no keyframes");
            Playing = false;
            return;
        }

        int index = Keyframes.BinarySearch(new Keyframe(AnimationProgress, default, default), new KeyframeComparer());
        if (~index == Keyframes.Count || index < 0)
            index = ~index;

        var previousKeyframe = Keyframes[ClampIndex(index-1)];
        var currentKeyframe = Keyframes[ClampIndex(index)];
        var nextKeyframe = Keyframes[ClampIndex(index+1)];
        var next2Keyframe = Keyframes[ClampIndex(index+2)];

        float distance = nextKeyframe.Time - currentKeyframe.Time;
        distance = distance == 0 ? 0 : (currentKeyframe.Time - AnimationProgress) / distance;
        Keyframe result = Keyframe.InterpolateBetween(previousKeyframe,currentKeyframe,nextKeyframe,next2Keyframe, 1 - distance);

        if (Parent is SceneObject3D Parent3D)
        {
            Parent3D.LocalPosition = result.Position;
            Parent3D.LocalRotation = result.Rotation;
        }

        if(index >= Keyframes.Count)
        {
            Playing = false;
            AnimationProgress = 0;
            OnAnimationFinish?.Invoke();
        }

        AnimationProgress += OverrideAnimationFrequency <= 0 ? args.DeltaTime : 1/OverrideAnimationFrequency;
    }

    int ClampIndex(int i)
    {
        return int.Max(0,int.Min(i, Keyframes.Count-1));
    }

    struct KeyframeComparer : IComparer<Keyframe>
    {
        public int Compare(Keyframe x, Keyframe y)
        {
            return x.Time.CompareTo(y.Time);
        }
    }
}