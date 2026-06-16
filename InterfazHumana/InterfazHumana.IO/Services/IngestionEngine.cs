using InterfazHumana.IO.Data;
using InterfazHumana.IO.Models;

namespace InterfazHumana.IO.Services;

public sealed class IngestionEngine
{
    private readonly SourceSiteRepository _sourceSiteRepository;
    private readonly IngestionLogRepository _ingestionLogRepository;
    private readonly RawContentRepository _rawContentRepository;
    private readonly IBlogScraperFactory _scraperFactory;
    private readonly int _batchSize;
    private readonly HttpClient _httpClient;

    public IngestionEngine(
        SourceSiteRepository sourceSiteRepository,
        IngestionLogRepository ingestionLogRepository,
        RawContentRepository rawContentRepository,
        IBlogScraperFactory scraperFactory,
        int batchSize,
        HttpClient httpClient)
    {
        _sourceSiteRepository = sourceSiteRepository;
        _ingestionLogRepository = ingestionLogRepository;
        _rawContentRepository = rawContentRepository;
        _scraperFactory = scraperFactory;
        _batchSize = Math.Max(1, batchSize);
        _httpClient = httpClient;
    }

    public async Task StartIngestionAsync(int sourceSiteId, int maxPagesToScan)
    {
        var sourceSite = _sourceSiteRepository.GetById(sourceSiteId);
        if (sourceSite is null)
        {
            Console.WriteLine($"No existe SourceSite con Id={sourceSiteId}.");
            return;
        }

        var scraper = _scraperFactory.CreateScraper(sourceSite);
        if (scraper is not LinksDvScraper linksDvScraper)
        {
            Console.WriteLine($"[{sourceSite.Name}] El scraper activo no soporta paginado de LinksDV.");
            return;
        }

        var stats = await DiscoverFromLinksDvAsync(sourceSite, linksDvScraper, maxPagesToScan);
        Console.WriteLine($"[{sourceSite.Name}] Tarjetas procesadas: {stats.CardsProcessed}");
        Console.WriteLine($"[{sourceSite.Name}] Nuevos enlaces externos en Pending: {stats.NewPending}");
        Console.WriteLine($"[{sourceSite.Name}] Enlaces omitidos por incrementalidad: {stats.Skipped}");

        var deepDownloadStats = await ProcessPendingBatchAsync(sourceSite.Id, _batchSize);
        Console.WriteLine($"[{sourceSite.Name}] HTMLs externos guardados: {deepDownloadStats.Downloaded}");
        Console.WriteLine($"[{sourceSite.Name}] Fallidos en descarga profunda: {deepDownloadStats.Failed}");
    }

    private async Task<(int CardsProcessed, int NewPending, int Skipped)> DiscoverFromLinksDvAsync(
        SourceSite sourceSite,
        LinksDvScraper scraper,
        int maxPagesToScan)
    {
        var cardsProcessed = 0;
        var newPending = 0;
        var skipped = 0;

        for (var pageNumber = 1; pageNumber <= Math.Max(1, maxPagesToScan); pageNumber++)
        {
            List<LinksDvScraper.DiscoveredLink> discoveredLinks;
            try
            {
                discoveredLinks = await scraper.ScrapePageAsync(pageNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{sourceSite.Name}] Error en página {pageNumber}: {ex.Message}");
                break;
            }

            if (discoveredLinks.Count == 0)
            {
                Console.WriteLine($"[{sourceSite.Name}] Página {pageNumber} sin resultados, se detiene descubrimiento.");
                break;
            }

            cardsProcessed += discoveredLinks.Count;

            foreach (var discovered in discoveredLinks)
            {
                if (_ingestionLogRepository.ExistsByUrl(discovered.TargetUrl))
                {
                    skipped++;
                    continue;
                }

                var logId = _ingestionLogRepository.Add(new IngestionLog(
                    Id: 0,
                    SourceSiteId: sourceSite.Id,
                    TargetUrl: discovered.TargetUrl,
                    LinksDvPage: discovered.PageNumber,
                    Status: "Pending",
                    ErrorMessage: null,
                    LastScrapedAt: DateTime.UtcNow));

                _rawContentRepository.AddMetadata(new RawContent(
                    Id: 0,
                    IngestionLogId: (int)logId,
                    RawHtml: null,
                    ExtractedTitle: discovered.Title,
                    ExtractedDescription: discovered.Description,
                    ExtractedImageUrl: discovered.ImageUrl));

                newPending++;
            }
        }

        return (cardsProcessed, newPending, skipped);
    }

    private async Task<(int Downloaded, int Failed)> ProcessPendingBatchAsync(int sourceSiteId, int batchSize)
    {
        var pendingLogs = _ingestionLogRepository.GetPendingBatch(sourceSiteId, batchSize);
        Console.WriteLine($"Pendientes a procesar (lote): {pendingLogs.Count}");

        var downloaded = 0;
        var failed = 0;

        foreach (var pendingLog in pendingLogs)
        {
            try
            {
                var response = await _httpClient.GetAsync(pendingLog.TargetUrl);
                response.EnsureSuccessStatusCode();
                var rawHtml = await response.Content.ReadAsStringAsync();

                _rawContentRepository.UpdateRawHtmlByIngestionLogId(pendingLog.Id, rawHtml);

                _ingestionLogRepository.UpdateStatus(
                    pendingLog.Id,
                    "Downloaded",
                    errorMessage: null,
                    lastScrapedAt: DateTime.UtcNow);

                downloaded++;
            }
            catch (Exception ex)
            {
                _ingestionLogRepository.UpdateStatus(
                    pendingLog.Id,
                    "Failed",
                    errorMessage: ex.Message,
                    lastScrapedAt: DateTime.UtcNow);

                failed++;
            }
        }

        return (downloaded, failed);
    }
}

