using System.Collections.Generic;
using OpenTK.Mathematics;
using SlopperEngine.Core;
using SlopperEngine.SceneObjects;

namespace TestProgram.RenderTest;

/// <summary>
/// Records an object's motion.
/// </summary>
public class FlyThroughRecorder : SceneObject
{
    /// <summary>
    /// The keyframes in the animation of the fly through.
    /// </summary>
    public List<Keyframe> Keyframes = [];

    /// <summary>
    /// How many samples to record into the keyframe list per second.
    /// </summary>
    public float RecordSamplesPerSecond = 0.5f;

    float recordingTime = -1;

    public FlyThroughRecorder(){}

    public void StartRecording()
    {
        recordingTime = 0;
        Keyframes.Clear();
    }

    public void StopRecording()
    {
        recordingTime = -1;
    }

    [OnFrameUpdate]
    void Update(FrameUpdateArgs args)
    {
        if(recordingTime >= 0)
        {
            recordingTime += args.DeltaTime;
            if(recordingTime*RecordSamplesPerSecond > Keyframes.Count)
            {
                var transform = GetGlobalTransform();
                Keyframes.Add(new(recordingTime, transform.ExtractTranslation(), transform.ExtractRotation()));
            }
        }
    }    
}

