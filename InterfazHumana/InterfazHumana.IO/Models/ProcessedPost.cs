namespace InterfazHumana.IO.Models;

public sealed record ProcessedPost(
    int Id,
    int IngestionLogId,
    int? CategoryId,
    string Title,
    string Content,
    DateTime PublishedAt
);

