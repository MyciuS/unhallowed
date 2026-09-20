namespace Unhallowed.Core.Common;

/// <summary>Minimal 2D vector. Kept in Core so the domain never depends on a UI library.</summary>
public readonly record struct Vec2(float X, float Y)
{
    public static readonly Vec2 Zero = new(0f, 0f);

    public float Length => MathF.Sqrt((X * X) + (Y * Y));

    public Vec2 Normalized()
    {
        var length = Length;
        return length < 0.0001f ? Zero : new Vec2(X / length, Y / length);
    }

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);

    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);

    public static Vec2 operator *(Vec2 a, float scalar) => new(a.X * scalar, a.Y * scalar);

    public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
}
