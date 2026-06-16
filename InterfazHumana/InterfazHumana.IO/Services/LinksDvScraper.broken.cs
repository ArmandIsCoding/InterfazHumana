using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace InterfazHumana.IO.Services;

public sealed class LinksDvScraper : IBlogScraper
{
    private readonly HttpClient _httpClient;
    private readonly HttpClient _nonRedirectHttpClient;
    private readonly int _maxPages;
            var anchors = doc.DocumentNode.SelectNodes("//a[@href]");
    public sealed record DiscoveredLink(string Title, string Description, string TargetUrl, string? ImageUrl, int PageNumber);
            foreach (var anchor in anchors)
    public LinksDvScraper(HttpClient httpClient, int maxPages)
    {
        _httpClient = httpClient;
        _maxPages = Math.Max(1, maxPages);
                    continue;
        _nonRedirectHttpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        });
                    continue;
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("InterfazHumanaBot/0.1");
        }
                    continue;
        if (!_nonRedirectHttpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _nonRedirectHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("InterfazHumanaBot/0.1");
        }
    }
                    continue;
    public async Task<List<string>> ExtractPostUrlsAsync(string baseUrl)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var pageNumber = 1; pageNumber <= _maxPages; pageNumber++)
        {
            var discovered = await ScrapePageAsync(pageNumber);
            if (discovered.Count == 0)
            {
                break;
            }
    }
            foreach (var link in discovered)
            {
                urls.Add(link.TargetUrl);
            }
        }

        return urls.ToList();
    }

    public async Task<List<DiscoveredLink>> ScrapePageAsync(int pageNumber)
    {
        var pageUri = new Uri($"https://linksdv.com/index?p={pageNumber}");
        var html = await _httpClient.GetStringAsync(pageUri);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
                         ?? doc.DocumentNode.SelectSingleNode("//body");
        var cards = SelectNodesSafe(doc.DocumentNode, "//div[contains(@class,'card') and @id_link]").ToList();
        var discovered = new List<DiscoveredLink>();
            .Select(node => NormalizeWhitespace(HtmlEntity.DeEntitize(node.InnerText)))
        foreach (var card in cards)
        {
            var cardBody = card.SelectSingleNode(".//div[contains(@class,'card-body')]") ?? card;
            var titleAnchor = card.SelectSingleNode(".//h5[contains(@class,'card-title')]/a")
                              ?? card.SelectSingleNode(".//a[@href]");
            ? string.Join(Environment.NewLine, textLines)
            if (titleAnchor is null)
            {
                continue;
            }
        {
            var title = NormalizeWhitespace(HtmlEntity.DeEntitize(titleAnchor.InnerText));
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var idLink = card.GetAttributeValue("id_link", string.Empty).Trim();
            var href = titleAnchor.GetAttributeValue("href", string.Empty).Trim();
            var targetUrl = await ResolveExternalTargetUrlAsync(pageUri, href, idLink);
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                continue;
            }
    }
            var cardTextNode = cardBody.SelectSingleNode(".//p[contains(@class,'card-text')]");
            var description = NormalizeWhitespace(HtmlEntity.DeEntitize(cardTextNode?.InnerText ?? string.Empty));
            if (description.StartsWith(title, StringComparison.OrdinalIgnoreCase))
            {
                description = description[title.Length..].Trim();
            }
        }
            var imageNode = card.SelectSingleNode(".//img[contains(@class,'card-img-top')]");
            var imageSrc = imageNode?.GetAttributeValue("src", string.Empty);
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(imageSrc) && Uri.TryCreate(pageUri, imageSrc, out var absoluteImageUri))
            {
                imageUrl = absoluteImageUri.ToString();
            }
        var query = absoluteUri.Query;
            discovered.Add(new DiscoveredLink(title, description, targetUrl, imageUrl, pageNumber));
        }
               && query.Contains("p=", StringComparison.OrdinalIgnoreCase);
        return discovered;
    }
    private static string NormalizeDomain(string host)
    public async Task<(string RawHtml, string CleanedText, List<string> Images)> ScrapePostAsync(string postUrl)
    {
        var postUri = new Uri(postUrl);
        var html = await _httpClient.GetStringAsync(postUri);
    }
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
    {
        foreach (var node in SelectNodesSafe(doc.DocumentNode, "//script|//style|//noscript"))
        {
            node.Remove();
        }

        var contentNode = doc.DocumentNode.SelectSingleNode("//article")
                         ?? doc.DocumentNode.SelectSingleNode("//main")
                         ?? doc.DocumentNode.SelectSingleNode("//body")
                         ?? doc.DocumentNode;

        var cleanedText = NormalizeWhitespace(HtmlEntity.DeEntitize(contentNode.InnerText));
        var images = new List<string>();
        foreach (var imageNode in SelectNodesSafe(contentNode, ".//img[@src]"))
        {
            var src = imageNode.GetAttributeValue("src", string.Empty);
            if (!string.IsNullOrWhiteSpace(src) && Uri.TryCreate(postUri, src, out var absoluteImageUri))
            {
                images.Add(absoluteImageUri.ToString());
            }
        }

        return (html, cleanedText, images);
    }

    private async Task<string?> ResolveExternalTargetUrlAsync(Uri pageUri, string href, string idLink)
    {
        var gotoHref = href;
        if (!gotoHref.Contains("goto.php", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(idLink))
        {
            gotoHref = $"goto.php?id_link={idLink}";
        }

        if (!Uri.TryCreate(pageUri, gotoHref, out var gotoUri))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, gotoUri);
            using var response = await _nonRedirectHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.Headers.Location is not null)
            {
                return response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location.ToString()
                    : new Uri(gotoUri, response.Headers.Location).ToString();
            }

            return gotoUri.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeWhitespace(string input)
    {
        return Regex.Replace(input, "\\s+", " ").Trim();
    }

    private static IEnumerable<HtmlNode> SelectNodesSafe(HtmlNode root, string xpath)
    {
        return root.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>();
    }
}

