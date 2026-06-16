namespace InterfazHumana.IO.Models;

public sealed record SourceSite(
    int Id,
    string Name,
    string BaseUrl,
    bool IsActive,
    DateTime CreatedAt
);

