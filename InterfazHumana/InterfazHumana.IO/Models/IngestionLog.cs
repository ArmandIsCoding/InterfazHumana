namespace InterfazHumana.IO.Models;

public sealed record IngestionLog(
    int Id,
    int SourceSiteId,
    string TargetUrl,
    string ContentHash,
    string Status,
    string? ErrorMessage,
    DateTime LastScrapedAt
);

