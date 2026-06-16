using InterfazHumana.IO.Models;
using Microsoft.Data.Sqlite;

namespace InterfazHumana.IO.Data;

public sealed class RawContentRepository
{
    private readonly DatabaseContext _dbContext;

    public RawContentRepository(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public long AddMetadata(RawContent rawContent)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO RawContents (IngestionLogId, RawHtml, CleanedText, ExtractedTitle, ExtractedDescription, ExtractedImageUrl)
            VALUES ($ingestionLogId, $rawHtml, $cleanedText, $extractedTitle, $extractedDescription, $extractedImageUrl)
            ON CONFLICT(IngestionLogId) DO NOTHING;
            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$ingestionLogId", rawContent.IngestionLogId);
        command.Parameters.AddWithValue("$rawHtml", (object?)rawContent.RawHtml ?? DBNull.Value);
        command.Parameters.AddWithValue("$cleanedText", (object?)rawContent.CleanedText ?? DBNull.Value);
        command.Parameters.AddWithValue("$extractedTitle", rawContent.ExtractedTitle);
        command.Parameters.AddWithValue("$extractedDescription", rawContent.ExtractedDescription);
        command.Parameters.AddWithValue("$extractedImageUrl", (object?)rawContent.ExtractedImageUrl ?? DBNull.Value);
        command.Prepare();

        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public bool UpdateRawHtmlByIngestionLogId(int ingestionLogId, string rawHtml)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE RawContents
            SET RawHtml = $rawHtml
            WHERE IngestionLogId = $ingestionLogId;
            """;

        command.Parameters.AddWithValue("$ingestionLogId", ingestionLogId);
        command.Parameters.AddWithValue("$rawHtml", rawHtml);
        command.Prepare();

        return command.ExecuteNonQuery() > 0;
    }

    public bool UpdateCleanedTextByIngestionLogId(int ingestionLogId, string cleanedText)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE RawContents
            SET CleanedText = $cleanedText
            WHERE IngestionLogId = $ingestionLogId;
            """;

        command.Parameters.AddWithValue("$ingestionLogId", ingestionLogId);
        command.Parameters.AddWithValue("$cleanedText", cleanedText);
        command.Prepare();

        return command.ExecuteNonQuery() > 0;
    }

    public RawContent? GetByIngestionLogId(int ingestionLogId)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, IngestionLogId, RawHtml, CleanedText, ExtractedTitle, ExtractedDescription, ExtractedImageUrl
            FROM RawContents
            WHERE IngestionLogId = $ingestionLogId
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$ingestionLogId", ingestionLogId);
        command.Prepare();

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public bool ExistsByIngestionLogId(int ingestionLogId)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1
            FROM RawContents
            WHERE IngestionLogId = $ingestionLogId
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$ingestionLogId", ingestionLogId);
        command.Prepare();

        return command.ExecuteScalar() is not null;
    }

    private static RawContent Map(SqliteDataReader reader)
    {
        return new RawContent(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6));
    }
}

