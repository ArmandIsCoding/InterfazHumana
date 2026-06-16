namespace InterfazHumana.IO.Config;

public sealed class AppSettings
{
    public IngestionSettings Ingestion { get; init; } = new();

    public ScrapersSettings Scrapers { get; init; } = new();
}

public sealed class IngestionSettings
{
    public int DiscoveryPages { get; init; } = 3;

    public int BatchSize { get; init; } = 10;
}

public sealed class ScrapersSettings
{
    public LinksDvSettings LinksDv { get; init; } = new();
}

public sealed class LinksDvSettings
{
    public int MaxPages { get; init; } = 5;
}

