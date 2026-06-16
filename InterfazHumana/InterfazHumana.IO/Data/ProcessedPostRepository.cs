using InterfazHumana.IO.Models;

namespace InterfazHumana.IO.Data;

public sealed class ProcessedPostRepository
{
    private readonly DatabaseContext _dbContext;

    public ProcessedPostRepository(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public long Add(ProcessedPost processedPost)
    {
        using var connection = _dbContext.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO ProcessedPosts (IngestionLogId, CategoryId, Title, Content, PublishedAt)
            VALUES ($ingestionLogId, $categoryId, $title, $content, $publishedAt);
            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$ingestionLogId", processedPost.IngestionLogId);
        command.Parameters.AddWithValue("$categoryId", (object?)processedPost.CategoryId ?? DBNull.Value);
        command.Parameters.AddWithValue("$title", processedPost.Title);
        command.Parameters.AddWithValue("$content", processedPost.Content);
        command.Parameters.AddWithValue("$publishedAt", processedPost.PublishedAt.ToString("o"));
        command.Prepare();

        return (long)(command.ExecuteScalar() ?? 0L);
    }
}

