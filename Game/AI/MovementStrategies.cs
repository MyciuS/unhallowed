using System.Numerics;
using Isaac.Core;
using Isaac.Game.Dungeon;
using Isaac.Game.Entities;

namespace Isaac.Game.AI;

/// <summary>
/// STRATEGY pattern, the Strategy interface for movement: decides an enemy's velocity.
/// <see cref="Enemy"/> (the Context) holds one and can swap it at runtime.
/// A strategy may keep its own state (timers, directions), so every enemy gets its own instance.
/// </summary>
public interface IMovementStrategy
{
    void Move(Enemy self, Player? target, World world, float dt);
}

internal static class Steering
{
    public static Vector2 Toward(Vector2 from, Vector2 to) => Vector2.Normalize(to - from + new Vector2(0.001f));
}

/// <summary>Walks at the nearest living player, taking the shortest way around rocks instead of getting stuck on them.</summary>
public sealed class ChaseMovement : IMovementStrategy
{
    private readonly PathFollower _path = new();

    public void Move(Enemy self, Player? target, World world, float dt)
    {
        if (target == null) { self.Vel = Vector2.Zero; return; }
        var goal = self.Flying ? target.Pos : _path.Steer(world, self.Pos, self.Radius, target.Pos);
        self.Vel = Steering.Toward(self.Pos, goal) * self.Speed;
    }
}

/// <summary>Erratic buzzing towards the player, like Isaac's flies.</summary>
public sealed class BuzzMovement : IMovementStrategy
{
    private float _phase = Random.Shared.NextSingle() * MathF.Tau;

    public void Move(Enemy self, Player? target, World world, float dt)
    {
        _phase += dt * 6f;
        var toward = target == null ? Vector2.Zero : Steering.Toward(self.Pos, target.Pos);
        var wobble = new Vector2(MathF.Cos(_phase), MathF.Sin(_phase * 1.3f));
        self.Vel = Vector2.Normalize(toward + wobble * 0.9f + new Vector2(0.001f)) * self.Speed;
    }
}

/// <summary>Keeps a preferred distance from the player, strafing around them.</summary>
public sealed class KeepDistanceMovement(float preferred) : IMovementStrategy
{
    private readonly float _strafeSign = Random.Shared.Next(2) == 0 ? -1 : 1;

    public void Move(Enemy self, Player? target, World world, float dt)
    {
        if (target == null) { self.Vel = Vector2.Zero; return; }
        var delta = target.Pos - self.Pos;
        float dist = delta.Length();
        var dir = dist > 0.01f ? delta / dist : Vector2.UnitX;
        var strafe = new Vector2(-dir.Y, dir.X) * _strafeSign;
        float radial = dist > preferred + 40 ? 1 : dist < preferred - 40 ? -1 : 0;
        self.Vel = Vector2.Normalize(dir * radial + strafe * 0.6f) * self.Speed;
    }
}

/// <summary>
/// Normally stays where it has a clear shot at the player, walking around rocks to get one.
/// When the player walks towards it, it runs for cover behind a rock instead.
/// Rooms without rocks offer no cover, so there it just keeps its distance.
/// </summary>
public sealed class TakeCoverMovement(float preferred) : IMovementStrategy
{
    private const float MinSpotDistance = 150f; // spots right next to the player are no good for hiding or shooting
    private const float HideTime = 0.6f;        // keep hiding a little while, so it does not flicker
    private const float LookAhead = 0.25f;      // seconds of movement checked for losing the shot

    private readonly KeepDistanceMovement _keepDistance = new(preferred);
    private readonly PathFollower _path = new();
    private Vector2? _spot;
    private float _rethink;
    private float _hideTimer;
    private bool _hiding;

    public void Move(Enemy self, Player? target, World world, float dt)
    {
        if (target == null) { self.Vel = Vector2.Zero; return; }

        _hideTimer = IsApproaching(target, self) ? HideTime : _hideTimer - dt;
        bool hiding = _hideTimer > 0;
        if (hiding != _hiding)
        {
            _hiding = hiding;
            _spot = null;
            _rethink = 0;
        }

        if (!hiding && AimedShotAttack.CanHit(world, self.Pos, target.Pos))
        {
            // Clear shot: keep the usual distance, but never strafe or close in so far that a rock gets in the way.
            _spot = null;
            _keepDistance.Move(self, target, world, dt);
            var ahead = world.MoveBody(self.Pos, self.Radius, self.Vel * LookAhead, self.Flying);
            if (!AimedShotAttack.CanHit(world, ahead, target.Pos))
                self.Vel = Vector2.Zero;
            return;
        }

        _rethink -= dt;
        if (_rethink <= 0 || _spot is { } s && !IsGoodSpot(world, target, s, hiding))
        {
            _spot = FindSpot(self, target, world, hiding);
            _rethink = 0.5f;
        }

        if (_spot is not { } spot)
        {
            // Nowhere to hide: keep the distance. No clear shot anywhere: close in on the player.
            if (hiding)
                _keepDistance.Move(self, target, world, dt);
            else
                self.Vel = Steering.Toward(self.Pos, _path.Steer(world, self.Pos, self.Radius, target.Pos)) * self.Speed;
        }
        else if (Vector2.Distance(self.Pos, spot) < 4)
            self.Vel = Vector2.Zero;
        else
            self.Vel = Steering.Toward(self.Pos, _path.Steer(world, self.Pos, self.Radius, spot)) * self.Speed;
    }

    private static bool IsApproaching(Player player, Enemy self)
    {
        if (player.Vel.LengthSquared() < 40 * 40)
            return false;
        var toSelf = Vector2.Normalize(self.Pos - player.Pos + new Vector2(0.001f));
        return Vector2.Dot(Vector2.Normalize(player.Vel), toSelf) > 0.7f;
    }

    /// <summary>Cover: the player cannot see the spot. Firing position: a tear from the spot would reach the player.</summary>
    private static bool IsGoodSpot(World world, Player target, Vector2 spot, bool cover) =>
        Vector2.Distance(spot, target.Pos) >= MinSpotDistance
        && (cover ? !world.HasLineOfSight(target.Pos, spot) : AimedShotAttack.CanHit(world, spot, target.Pos));

    /// <summary>The nearest tile (by walking distance) that is good cover, or a good firing position.</summary>
    private static Vector2? FindSpot(Enemy self, Player target, World world, bool cover)
    {
        var dist = RoomNavigation.Distances(world.Room, RoomNavigation.TileOf(self.Pos));
        Vector2? best = null;
        float bestDist = float.PositiveInfinity;
        for (int x = 0; x < Config.RoomTilesW; x++)
        for (int y = 0; y < Config.RoomTilesH; y++)
        {
            if (dist[x, y] >= bestDist)
                continue;
            var spot = Room.TileCenter(x, y);
            if (!IsGoodSpot(world, target, spot, cover))
                continue;
            bestDist = dist[x, y];
            best = spot;
        }
        return best;
    }
}

/// <summary>Wanders slowly; when lined up with a player, dashes in a straight line until it hits a wall or rock.</summary>
public sealed class ChargeMovement : IMovementStrategy
{
    private const float DashSpeed = 3.2f;   // times normal speed
    private const float RecoverTime = 0.35f; // dazed after slamming into a wall

    private Vector2? _dashDir;
    private float _recover;
    private Vector2 _wanderDir;
    private float _wanderTime;

    public void Move(Enemy self, Player? target, World world, float dt)
    {
        if (_dashDir is { } dash)
        {
            var vel = dash * self.Speed * DashSpeed;
            if (!Blocked(self, world, vel * dt))
            {
                self.Vel = vel;
                return;
            }
            _dashDir = null;
            _recover = RecoverTime;
        }

        if (_recover > 0)
        {
            _recover -= dt;
            self.Vel = Vector2.Zero;
            return;
        }

        if (target != null)
        {
            var d = target.Pos - self.Pos;
            if (MathF.Abs(d.X) < 24 || MathF.Abs(d.Y) < 24)
            {
                var dir = MathF.Abs(d.X) < 24 ? new Vector2(0, MathF.Sign(d.Y)) : new Vector2(MathF.Sign(d.X), 0);
                // Do not start a dash straight into the rock we are standing against.
                if (!Blocked(self, world, dir * self.Speed * DashSpeed * dt))
                {
                    _dashDir = dir;
                    self.Vel = dir * self.Speed * DashSpeed;
                    return;
                }
            }
        }

        _wanderTime -= dt;
        if (_wanderTime <= 0)
        {
            _wanderTime = 0.8f + Random.Shared.NextSingle();
            _wanderDir = Random.Shared.Next(4) switch
            {
                0 => Vector2.UnitX, 1 => -Vector2.UnitX, 2 => Vector2.UnitY, _ => -Vector2.UnitY,
            };
        }
        self.Vel = _wanderDir * self.Speed;
    }

    /// <summary>True if a wall or rock stops most of this step.</summary>
    private static bool Blocked(Enemy self, World world, Vector2 step)
    {
        var moved = world.MoveBody(self.Pos, self.Radius, step, self.Flying) - self.Pos;
        return moved.LengthSquared() < step.LengthSquared() * 0.25f;
    }
}

/// <summary>Bounces around the room on a diagonal.</summary>
public sealed class BounceMovement(float speedMultiplier) : IMovementStrategy
{
    private Vector2 _bounceDir = Vector2.Normalize(new Vector2(1, 0.7f));
    private Vector2 _lastPos;

    public void Move(Enemy self, Player? target, World world, float dt)
    {
        // If we did not move along an axis last frame we hit a wall: reflect.
        if (_lastPos != Vector2.Zero)
        {
            var moved = self.Pos - _lastPos;
            if (MathF.Abs(moved.X) < 0.01f) _bounceDir.X = -_bounceDir.X;
            if (MathF.Abs(moved.Y) < 0.01f) _bounceDir.Y = -_bounceDir.Y;
        }
        _lastPos = self.Pos;
        self.Vel = _bounceDir * self.Speed * speedMultiplier;
    }
}

/// <summary>
/// Stands still, then hops onto a tile towards the player. While in the air it has a
/// <see cref="Enemy.Height"/> above the ground, and it sets <see cref="Enemy.JustLanded"/> on touchdown.
/// </summary>
public sealed class JumpMovement(float restTime, float airTime, float maxDistance, float jumpHeight) : IMovementStrategy
{
    private readonly float _restTime = restTime;
    private float _rest = restTime * (0.5f + Random.Shared.NextSingle());
    private float _air = -1; // time spent in the current jump, negative while on the ground
    private Vector2 _from, _to;

    public void Move(Enemy self, Player? target, World world, float dt)
    {
        self.Vel = Vector2.Zero;

        if (_air >= 0)
        {
            _air += dt;
            float t = MathF.Min(1, _air / airTime);
            self.Pos = Vector2.Lerp(_from, _to, t);
            self.Height = MathF.Sin(t * MathF.PI) * jumpHeight;
            if (t >= 1)
            {
                _air = -1;
                _rest = _restTime;
                self.Height = 0;
                self.JustLanded = true;
            }
            return;
        }

        _rest -= dt;
        if (_rest > 0 || target == null)
            return;
        _from = self.Pos;
        _to = PickLanding(self, target, world);
        _air = 0;
    }

    /// <summary>The centre of a tile towards the player, at most <c>maxDistance</c> away.</summary>
    private Vector2 PickLanding(Enemy self, Player target, World world)
    {
        var offset = target.Pos - self.Pos;
        if (offset.Length() > maxDistance)
            offset = Vector2.Normalize(offset) * maxDistance;
        var (tx, ty) = RoomNavigation.TileOf(self.Pos + offset);
        if (world.Room.IsSolidTile(tx, ty))
            return self.Pos;
        var spot = Room.TileCenter(tx, ty);
        return new Vector2(
            Math.Clamp(spot.X, self.Radius, Config.RoomW - self.Radius),
            Math.Clamp(spot.Y, self.Radius, Config.RoomH - self.Radius));
    }
}

/// <summary>
/// Every <c>interval</c> seconds picks a new move at random: straight at the player, or sideways to them.
/// When the player is close enough to reach in one move, it goes for them more often.
/// </summary>
public sealed class SkitterMovement(float interval) : IMovementStrategy
{
    private const float TowardChance = 0.5f;
    private const float InReachTowardChance = TowardChance * 1.4f;
    private const float TowardSpeedBoost = 1.4f; // lunges at the player faster than it scuttles sideways

    private readonly float _interval = interval;
    private float _timer;
    private Vector2 _dir;
    private float _speedFactor = 1f;

    public void Move(Enemy self, Player? target, World world, float dt)
    {
        if (target == null) { self.Vel = Vector2.Zero; return; }
        _timer -= dt;
        if (_timer <= 0)
        {
            _timer = _interval;
            var toward = Steering.Toward(self.Pos, target.Pos);
            // In reach: a move straight at the player would get all the way there.
            bool inReach = Vector2.Distance(self.Pos, target.Pos) <= self.Speed * TowardSpeedBoost * _interval;
            if (Random.Shared.NextSingle() < (inReach ? InReachTowardChance : TowardChance))
            {
                _dir = toward;
                _speedFactor = TowardSpeedBoost;
            }
            else
            {
                _dir = new Vector2(-toward.Y, toward.X) * (Random.Shared.Next(2) == 0 ? 1 : -1);
                _speedFactor = 1f;
            }
        }
        self.Vel = _dir * self.Speed * _speedFactor;
    }
}
