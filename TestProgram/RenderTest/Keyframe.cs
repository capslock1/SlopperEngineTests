using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OpenTK.Mathematics;
using SlopperEngine.Core;

namespace TestProgram.RenderTest;

/// <summary>
/// A keyframe in a fly through animation.
/// </summary>
/// <param name="Time">The time at which the keyframe happens.</param>
/// <param name="Position">The position of the keyframe.</param>
/// <param name="Rotation">The rotation of the keyframe.</param>
public record struct Keyframe(float Time, Vector3 Position, Quaternion Rotation)
{
    public static readonly Keyframe Identity = new(0, default, Quaternion.Identity);
    public static Keyframe operator+(Keyframe left, Keyframe right) => new(left.Time+right.Time, left.Position+right.Position, left.Rotation*right.Rotation);
    public static Keyframe operator-(Keyframe left, Keyframe right) => new(left.Time-right.Time, left.Position-right.Position, left.Rotation*Quaternion.Conjugate(right.Rotation));
    public static Keyframe Lerp(Keyframe left, Keyframe right, float t) => new(float.Lerp(left.Time, right.Time, t), Vector3.Lerp(left.Position, right.Position, t), Quaternion.Slerp(left.Rotation, right.Rotation, t));

    public static Keyframe InterpolateBetween(Keyframe previous, Keyframe current, Keyframe next, Keyframe next2, float currentToNext, float velocityMultiplier = 0.25f)
    {
        // Just trust the math
        Keyframe velocityStart = next - previous;
        Keyframe velocityEnd = next2 - current;
        var control1 = Lerp(Identity, velocityStart, velocityMultiplier) + current;
        var control2 = next - Lerp(Identity, velocityEnd, velocityMultiplier);
        // From here I'm reasonably confident this is a bezier curve
        // This could be done more efficiently, but consider that it's not my problem
        var quad1 = Lerp(current, control1, currentToNext);
        var quad2 = Lerp(control1, control2, currentToNext);
        var quad3 = Lerp(control2, next, currentToNext);

        var linear1 = Lerp(quad1, quad2, currentToNext);
        var linear2 = Lerp(quad2, quad3, currentToNext);

        var res = Lerp(linear1, linear2, currentToNext);
        // System.Console.WriteLine(Matrix3.CreateFromQuaternion(res.Rotation).Row2);
        // System.Console.WriteLine(Matrix4.LookAt(default, Matrix3.CreateFromQuaternion(res.Rotation).Row2, Vector3.UnitY));
        // res.Rotation = Matrix4.LookAt(default, -Matrix3.CreateFromQuaternion(res.Rotation).Column2, Vector3.UnitY).ExtractRotation();
        // System.Console.WriteLine(res.Rotation);

        return res;
    }

    public static List<Keyframe> LoadFromCsv(Asset csvFile)
    {
        if (!csvFile.CanRead) throw new System.Exception("Haha, I can't read!");
        List<Keyframe> res = [];
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        foreach (var line in csvFile.ReadAllLines())
        {
            var values = line.Split(',');

            Keyframe k = default;

            k.Time = float.Parse(values[0]);
            k.Position = new Vector3
            (
                float.Parse(values[1]),
                float.Parse(values[2]),
                float.Parse(values[3])
            );
            k.Rotation = new Quaternion
            (
                float.Parse(values[4]),
                float.Parse(values[5]),
                float.Parse(values[6]),
                float.Parse(values[7])
            );

            res.Add(k);
        }
        
        return res;
    }
    
    public static void SaveToCsv(Asset csvFile, List<Keyframe> keyframes)
    {
        if (!csvFile.CanWrite) throw new System.Exception("File cannot write!");
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        using var stream = csvFile.GetStream();
        using var textStream = new StreamWriter(stream, Encoding.UTF8);
        
        foreach (var keyframe in keyframes)
        {
            textStream.Write(keyframe.Time.ToString("0.000"));
            textStream.Write(',');
            textStream.Write(keyframe.Position.X.ToString("0.000"));
            textStream.Write(',');
            textStream.Write(keyframe.Position.Y.ToString("0.000"));
            textStream.Write(',');
            textStream.Write(keyframe.Position.Z.ToString("0.000"));
            textStream.Write(',');
            textStream.Write(keyframe.Rotation.X.ToString("0.000"));
            textStream.Write(',');
            textStream.Write(keyframe.Rotation.Y.ToString("0.000"));
            textStream.Write(',');
            textStream.Write(keyframe.Rotation.Z.ToString("0.000"));
            textStream.Write(',');
            textStream.Write(keyframe.Rotation.W.ToString("0.000"));
            textStream.WriteLine();
        }
    }
}

