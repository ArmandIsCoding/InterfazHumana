namespace InterfazHumana.IO.Services;

public interface IBlogScraper
{
    Task<List<string>> ExtractPostUrlsAsync(string baseUrl);

    Task<(string RawHtml, string CleanedText, List<string> Images)> ScrapePostAsync(string postUrl);
}

