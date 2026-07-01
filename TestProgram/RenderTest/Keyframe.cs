using OpenTK.Mathematics;

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

        return Lerp(linear1, linear2, currentToNext);
    }
}

