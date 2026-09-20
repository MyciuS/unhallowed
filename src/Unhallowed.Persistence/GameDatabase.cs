using Microsoft.Data.Sqlite;

namespace Unhallowed.Persistence;

/// <summary>
/// Creates and seeds the SQLite file used for requirement 7 (map elements and enemies stored in
/// a database). Deliberately raw ADO.NET rather than an ORM: requirement 3 bans frameworks, and
/// hand-written SQL keeps the repository classes honest instead of hiding them behind magic.
/// </summary>
public sealed class GameDatabase(string connectionString)
{
    /// <summary>Default file-based database, created next to the running executable.</summary>
    public static GameDatabase Default { get; } = new("Data Source=unhallowed.sqlite");

    /// <summary>In-memory database for tests and the pattern demos.</summary>
    public static GameDatabase InMemory() => new("Data Source=:memory:;Cache=Shared");

    public string ConnectionString { get; } = connectionString;

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    /// <summary>Creates the schema if it is missing, then seeds the starter rows once.</summary>
    public void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS EnemyDefinition (
                Archetype     TEXT    PRIMARY KEY,
                DisplayName   TEXT    NOT NULL,
                MaxHealth     INTEGER NOT NULL,
                ContactDamage INTEGER NOT NULL,
                Radius        REAL    NOT NULL,
                MovementKind  TEXT    NOT NULL,
                MoveSpeed     REAL    NOT NULL
            );

            CREATE TABLE IF NOT EXISTS RoomDefinition (
                Id           TEXT    PRIMARY KEY,
                RoomType     TEXT    NOT NULL,
                Width        REAL    NOT NULL,
                Height       REAL    NOT NULL,
                EnemyBudget  INTEGER NOT NULL
            );
            """;
        command.ExecuteNonQuery();

        Seed(connection);
    }

    private static void Seed(SqliteConnection connection)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM EnemyDefinition;";
        if (Convert.ToInt64(check.ExecuteScalar()) > 0)
        {
            return;
        }

        using var transaction = connection.BeginTransaction();
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            """
            INSERT INTO EnemyDefinition
                (Archetype, DisplayName, MaxHealth, ContactDamage, Radius, MovementKind, MoveSpeed)
            VALUES
                ('gaper',   'Gaper',   8,  1, 14, 'Chaser',     55),
                ('fly',     'Fly',     4,  1, 10, 'Wanderer',   40),
                ('charger', 'Charger', 12, 1, 16, 'Charger',   220),
                ('turret',  'Turret',  10, 1, 14, 'Stationary',  0),
                ('monstro', 'Monstro', 60, 2, 30, 'Charger',   180);

            INSERT INTO RoomDefinition (Id, RoomType, Width, Height, EnemyBudget)
            VALUES
                ('starter',  'Normal',   960, 540, 4),
                ('treasure', 'Treasure', 960, 540, 0),
                ('boss',     'Boss',     960, 540, 1);
            """;
        insert.ExecuteNonQuery();
        transaction.Commit();
    }
}
