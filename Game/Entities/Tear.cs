using System.Numerics;

namespace Isaac.Game.Entities;

/// <summary>A projectile.</summary>
public sealed class Tear
{
    public Vector2 Pos;
    public Vector2 Vel;
    public float Damage;
    public float Radius;
    public float Life;
    public bool FromEnemy;
    public bool Dead;
}
