using InterfazHumana.IO.Models;

namespace InterfazHumana.IO.Services;

public interface IBlogScraperFactory
{
    IBlogScraper CreateScraper(SourceSite sourceSite);
}

