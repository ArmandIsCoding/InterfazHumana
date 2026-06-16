using System.Globalization;
using InterfazHumana.IO.Models;
using Microsoft.Data.Sqlite;

namespace InterfazHumana.IO.Data;

public sealed class SourceSiteRepository
{
    private readonly DatabaseContext _dbContext;

    public SourceSiteRepository(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public int Count()
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM SourceSites;";
        command.Prepare();

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public long Add(SourceSite sourceSite)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO SourceSites (Name, BaseUrl, IsActive, CreatedAt)
            VALUES ($name, $baseUrl, $isActive, $createdAt);
            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$name", sourceSite.Name);
        command.Parameters.AddWithValue("$baseUrl", sourceSite.BaseUrl);
        command.Parameters.AddWithValue("$isActive", sourceSite.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$createdAt", sourceSite.CreatedAt.ToString("o"));
        command.Prepare();

        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public SourceSite? GetById(int id)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Name, BaseUrl, IsActive, CreatedAt
            FROM SourceSites
            WHERE Id = $id
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$id", id);
        command.Prepare();

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public IReadOnlyList<SourceSite> GetAll()
    {
        var result = new List<SourceSite>();

        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, BaseUrl, IsActive, CreatedAt FROM SourceSites ORDER BY Id;";
        command.Prepare();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public bool Update(SourceSite sourceSite)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE SourceSites
            SET Name = $name,
                BaseUrl = $baseUrl,
                IsActive = $isActive,
                CreatedAt = $createdAt
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", sourceSite.Id);
        command.Parameters.AddWithValue("$name", sourceSite.Name);
        command.Parameters.AddWithValue("$baseUrl", sourceSite.BaseUrl);
        command.Parameters.AddWithValue("$isActive", sourceSite.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$createdAt", sourceSite.CreatedAt.ToString("o"));
        command.Prepare();

        return command.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SourceSites WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.Prepare();

        return command.ExecuteNonQuery() > 0;
    }

    private static SourceSite Map(SqliteDataReader reader)
    {
        return new SourceSite(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3) == 1,
            DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}

