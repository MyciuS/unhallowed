using Microsoft.Data.Sqlite;

namespace Unhallowed.Persistence;

/// <summary>A row of the <c>EnemyDefinition</c> table - the data half of an enemy archetype.</summary>
public sealed record EnemyDefinition(
    string Archetype,
    string DisplayName,
    int MaxHealth,
    int ContactDamage,
    float Radius,
    string MovementKind,
    float MoveSpeed);

/// <summary>A row of the <c>RoomDefinition</c> table.</summary>
public sealed record RoomDefinition(
    string Id,
    string RoomType,
    float Width,
    float Height,
    int EnemyBudget);

/// <summary>
/// Abstraction over enemy storage. Keeping this an interface is what lets the Factory and
/// Prototype implementations be tested without a database at all.
/// </summary>
public interface IEnemyDefinitionRepository
{
    IReadOnlyList<EnemyDefinition> GetAll();

    EnemyDefinition? Find(string archetype);
}

/// <summary>SQLite-backed implementation. Requirement 7.</summary>
public sealed class SqliteEnemyDefinitionRepository(GameDatabase database) : IEnemyDefinitionRepository
{
    public IReadOnlyList<EnemyDefinition> GetAll()
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Archetype, DisplayName, MaxHealth, ContactDamage, Radius, MovementKind, MoveSpeed
            FROM EnemyDefinition
            ORDER BY Archetype;
            """;

        using var reader = command.ExecuteReader();
        var results = new List<EnemyDefinition>();
        while (reader.Read())
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public EnemyDefinition? Find(string archetype)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Archetype, DisplayName, MaxHealth, ContactDamage, Radius, MovementKind, MoveSpeed
            FROM EnemyDefinition
            WHERE Archetype = $archetype;
            """;
        command.Parameters.AddWithValue("$archetype", archetype);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    private static EnemyDefinition Map(SqliteDataReader reader) => new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetInt32(2),
        reader.GetInt32(3),
        reader.GetFloat(4),
        reader.GetString(5),
        reader.GetFloat(6));
}

/// <summary>
/// In-memory stand-in used by unit tests and the pattern demos, so neither needs a real file.
/// </summary>
public sealed class InMemoryEnemyDefinitionRepository(IEnumerable<EnemyDefinition>? seed = null)
    : IEnemyDefinitionRepository
{
    private readonly List<EnemyDefinition> _rows = seed?.ToList() ??
    [
        new("gaper", "Gaper", 8, 1, 14f, "Chaser", 55f),
        new("fly", "Fly", 4, 1, 10f, "Wanderer", 40f),
        new("charger", "Charger", 12, 1, 16f, "Charger", 220f),
        new("turret", "Turret", 10, 1, 14f, "Stationary", 0f),
        new("monstro", "Monstro", 60, 2, 30f, "Charger", 180f),
    ];

    public IReadOnlyList<EnemyDefinition> GetAll() => _rows;

    public EnemyDefinition? Find(string archetype)
        => _rows.FirstOrDefault(r => r.Archetype == archetype);
}
