using System.Globalization;
using InterfazHumana.IO.Models;
using Microsoft.Data.Sqlite;

namespace InterfazHumana.IO.Data;

public sealed class IngestionLogRepository
{
    private readonly DatabaseContext _dbContext;

    public IngestionLogRepository(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool ExistsByUrl(string url)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1
            FROM IngestionLogs
            WHERE TargetUrl = $targetUrl
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$targetUrl", url);
        command.Prepare();

        return command.ExecuteScalar() is not null;
    }

    public long Add(IngestionLog ingestionLog)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO IngestionLogs
                (SourceSiteId, TargetUrl, ContentHash, Status, ErrorMessage, LastScrapedAt)
            VALUES
                ($sourceSiteId, $targetUrl, $contentHash, $status, $errorMessage, $lastScrapedAt);
            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$sourceSiteId", ingestionLog.SourceSiteId);
        command.Parameters.AddWithValue("$targetUrl", ingestionLog.TargetUrl);
        command.Parameters.AddWithValue("$contentHash", ingestionLog.ContentHash);
        command.Parameters.AddWithValue("$status", ingestionLog.Status);
        command.Parameters.AddWithValue("$errorMessage", (object?)ingestionLog.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastScrapedAt", ingestionLog.LastScrapedAt.ToString("o"));
        command.Prepare();

        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public IngestionLog? GetById(int id)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, SourceSiteId, TargetUrl, ContentHash, Status, ErrorMessage, LastScrapedAt
            FROM IngestionLogs
            WHERE Id = $id
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$id", id);
        command.Prepare();

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public bool Update(IngestionLog ingestionLog)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE IngestionLogs
            SET SourceSiteId = $sourceSiteId,
                TargetUrl = $targetUrl,
                ContentHash = $contentHash,
                Status = $status,
                ErrorMessage = $errorMessage,
                LastScrapedAt = $lastScrapedAt
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", ingestionLog.Id);
        command.Parameters.AddWithValue("$sourceSiteId", ingestionLog.SourceSiteId);
        command.Parameters.AddWithValue("$targetUrl", ingestionLog.TargetUrl);
        command.Parameters.AddWithValue("$contentHash", ingestionLog.ContentHash);
        command.Parameters.AddWithValue("$status", ingestionLog.Status);
        command.Parameters.AddWithValue("$errorMessage", (object?)ingestionLog.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastScrapedAt", ingestionLog.LastScrapedAt.ToString("o"));
        command.Prepare();

        return command.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM IngestionLogs WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.Prepare();

        return command.ExecuteNonQuery() > 0;
    }

    private static IngestionLog Map(SqliteDataReader reader)
    {
        return new IngestionLog(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}

