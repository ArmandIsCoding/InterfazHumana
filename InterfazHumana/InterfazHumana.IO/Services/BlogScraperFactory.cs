using InterfazHumana.IO.Config;
using InterfazHumana.IO.Models;

namespace InterfazHumana.IO.Services;

public sealed class BlogScraperFactory : IBlogScraperFactory
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    public BlogScraperFactory(HttpClient httpClient, AppSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public IBlogScraper CreateScraper(SourceSite sourceSite)
    {
        if (sourceSite.BaseUrl.Contains("linksdv.com", StringComparison.OrdinalIgnoreCase))
        {
            return new LinksDvScraper(_httpClient, _settings.Scrapers.LinksDv.MaxPages);
        }


        throw new NotSupportedException($"No hay scraper configurado para: {sourceSite.BaseUrl}");
    }
}

