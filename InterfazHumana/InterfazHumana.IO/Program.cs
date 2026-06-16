using InterfazHumana.IO.Config;
using InterfazHumana.IO.Data;
using InterfazHumana.IO.Models;
using InterfazHumana.IO.Services;

namespace InterfazHumana.IO;

internal static class Program
{
    private const string ConnectionString = "Data Source=interfazhumana.db;Cache=Shared;";

    private static async Task Main()
    {
        var settings = AppSettingsLoader.Load(AppContext.BaseDirectory);

        DatabaseInitializer.Initialize(ConnectionString);

        var dbContext = new DatabaseContext(ConnectionString);
        var sourceSiteRepository = new SourceSiteRepository(dbContext);
        var ingestionLogRepository = new IngestionLogRepository(dbContext);
        var rawContentRepository = new RawContentRepository(dbContext);

        var dbPath = Path.GetFullPath("interfazhumana.db");
        Console.WriteLine($"SQLite inicializado correctamente. Archivo: {dbPath}");
        Console.WriteLine($"Config -> LinksDV MaxPages: {settings.Scrapers.LinksDv.MaxPages}, DiscoveryPages: {settings.Ingestion.DiscoveryPages}, BatchSize: {settings.Ingestion.BatchSize}");

        if (sourceSiteRepository.Count() == 0)
        {
            sourceSiteRepository.Add(new SourceSite(
                Id: 0,
                Name: "LinksDV",
                BaseUrl: "https://linksdv.com",
                IsActive: true,
                CreatedAt: DateTime.UtcNow));
        }

        var linksDvSite = sourceSiteRepository.GetAll()
            .FirstOrDefault(site => site.BaseUrl.Contains("linksdv.com", StringComparison.OrdinalIgnoreCase));

        if (linksDvSite is null)
        {
            sourceSiteRepository.Add(new SourceSite(
                Id: 0,
                Name: "LinksDV",
                BaseUrl: "https://linksdv.com",
                IsActive: true,
                CreatedAt: DateTime.UtcNow));

            linksDvSite = sourceSiteRepository.GetAll()
                .FirstOrDefault(site => site.BaseUrl.Contains("linksdv.com", StringComparison.OrdinalIgnoreCase));
        }

        if (linksDvSite is null)
        {
            Console.WriteLine("No se pudo ubicar el SourceSite de LinksDV para iniciar ingesta.");
            return;
        }

        using var httpClient = new HttpClient();
        var scraperFactory = new BlogScraperFactory(httpClient, settings);
        var ingestionEngine = new IngestionEngine(
            sourceSiteRepository,
            ingestionLogRepository,
            rawContentRepository,
            scraperFactory,
            settings.Ingestion.BatchSize,
            httpClient);

        Console.WriteLine($"Iniciando ingesta para SourceSite Id={linksDvSite.Id} ({linksDvSite.Name})...");
        await ingestionEngine.StartIngestionAsync(linksDvSite.Id, settings.Ingestion.DiscoveryPages);
    }
}