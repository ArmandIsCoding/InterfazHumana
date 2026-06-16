using InterfazHumana.IO.Models;

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
            INSERT INTO RawContents (IngestionLogId, RawHtml, ExtractedTitle, ExtractedDescription, ExtractedImageUrl)
            VALUES ($ingestionLogId, $rawHtml, $extractedTitle, $extractedDescription, $extractedImageUrl)
            ON CONFLICT(IngestionLogId) DO NOTHING;
            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$ingestionLogId", rawContent.IngestionLogId);
        command.Parameters.AddWithValue("$rawHtml", (object?)rawContent.RawHtml ?? DBNull.Value);
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
}

