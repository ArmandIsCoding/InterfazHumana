namespace InterfazHumana.IO.Models;

public sealed record IngestionLog(
    int Id,
    int SourceSiteId,
    string TargetUrl,
    int LinksDvPage,
    string Status,
    string? ErrorMessage,
    DateTime LastScrapedAt
);

