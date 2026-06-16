using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace InterfazHumana.IO.Services;

public sealed class FabioScraper : IBlogScraper
{
	private readonly HttpClient _httpClient;
	private static readonly Regex PostPathRegex = new("^/\\d+-", RegexOptions.Compiled);

	public FabioScraper(HttpClient httpClient)
	{
		_httpClient = httpClient;

		if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
		{
			_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("InterfazHumanaBot/0.1");
		}
	}

	public async Task<List<string>> ExtractPostUrlsAsync(string baseUrl)
	{
		var baseUri = new Uri(baseUrl);
		var baseDomain = NormalizeDomain(baseUri.Host);
		var html = await _httpClient.GetStringAsync(baseUri);

		var doc = new HtmlDocument();
		doc.LoadHtml(html);

		var anchors = doc.DocumentNode.SelectNodes("//article//a[@href]")
					  ?? doc.DocumentNode.SelectNodes("//a[@rel='bookmark' or contains(@class, 'post-title') or contains(@class, 'entry-title')]")
					  ?? doc.DocumentNode.SelectNodes("//a[contains(@href, '/archivo/') or contains(@href, '/post/') or contains(@href, '/blog/')]")
					  ?? Enumerable.Empty<HtmlNode>();

		var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var anchor in anchors)
		{
			var href = anchor.GetAttributeValue("href", string.Empty);
			if (string.IsNullOrWhiteSpace(href) || href.StartsWith('#'))
			{
				continue;
			}

			if (!Uri.TryCreate(baseUri, href, out var absoluteUri))
			{
				continue;
			}

			if (!string.Equals(NormalizeDomain(absoluteUri.Host), baseDomain, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!IsLikelyPostPath(absoluteUri.AbsolutePath))
			{
				continue;
			}

			urls.Add(absoluteUri.GetLeftPart(UriPartial.Path).TrimEnd('/'));
		}

		return urls.ToList();
	}

	public async Task<(string RawHtml, string CleanedText, List<string> Images)> ScrapePostAsync(string postUrl)
	{
		var postUri = new Uri(postUrl);
		var html = await _httpClient.GetStringAsync(postUri);

		var doc = new HtmlDocument();
		doc.LoadHtml(html);

		foreach (var node in doc.DocumentNode.SelectNodes("//script|//style|//noscript") ?? Enumerable.Empty<HtmlNode>())
		{
			node.Remove();
		}

		var contentNode = doc.DocumentNode.SelectSingleNode("//article")
						 ?? doc.DocumentNode.SelectSingleNode("//main")
						 ?? doc.DocumentNode.SelectSingleNode("//body")
						 ?? doc.DocumentNode;

		var cleanedText = NormalizeWhitespace(HtmlEntity.DeEntitize(contentNode.InnerText));

		var images = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var imageNode in contentNode.SelectNodes(".//img[@src]") ?? Enumerable.Empty<HtmlNode>())
		{
			var src = imageNode.GetAttributeValue("src", string.Empty);
			if (string.IsNullOrWhiteSpace(src))
			{
				continue;
			}

			if (Uri.TryCreate(postUri, src, out var absoluteImageUri))
			{
				images.Add(absoluteImageUri.ToString());
			}
		}

		return (html, cleanedText, images.ToList());
	}

	private static string NormalizeWhitespace(string input)
	{
		return Regex.Replace(input, "\\s+", " ").Trim();
	}

	private static string NormalizeDomain(string host)
	{
		return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
			? host[4..]
			: host;
	}

	private static bool IsLikelyPostPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || path.Length <= 1)
		{
			return false;
		}

		return PostPathRegex.IsMatch(path);
	}
}

