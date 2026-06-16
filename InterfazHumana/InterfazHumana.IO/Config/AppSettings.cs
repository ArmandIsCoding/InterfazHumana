namespace InterfazHumana.IO.Config;

public sealed class AppSettings
{
    public IngestionSettings Ingestion { get; init; } = new();

    public ScrapersSettings Scrapers { get; init; } = new();

    public ProcessingSettings Processing { get; init; } = new();
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

public sealed class ProcessingSettings
{
    public int BatchSize { get; init; } = 10;

    public LocalAiSettings LocalAi { get; init; } = new();
}

public sealed class LocalAiSettings
{
    public string Endpoint { get; init; } = "http://localhost:8080/v1/chat/completions";

    public string Model { get; init; } = "mlx-community";

    public string SystemPrompt { get; init; } =
        "Sos un redactor experto para el sitio InterfazHumana.com.ar. Tu tarea es reescribir la siguiente noticia con un tono tecnologico, propio, directo, atrapante y en espanol, citando datos clave de forma fluida pero cambiando la redaccion por completo para que sea contenido original.";
}

