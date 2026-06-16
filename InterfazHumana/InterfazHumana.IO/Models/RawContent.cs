namespace InterfazHumana.IO.Models;

public sealed record RawContent(
    int Id,
    int IngestionLogId,
    string? RawHtml,
    string ExtractedTitle,
    string ExtractedDescription,
    string? ExtractedImageUrl
);

