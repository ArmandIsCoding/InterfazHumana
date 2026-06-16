using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace InterfazHumana.IO.Services;

public static class TextExtractor
{
    public static string ExtractArticleText(string rawHtml)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
        {
            return string.Empty;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(rawHtml);

        RemoveNodes(doc, "//script|//style|//nav|//footer|//header|//aside|//form|//noscript");
        RemoveNodes(doc,
            "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'sidebar') or " +
            "contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'comments') or " +
            "contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'comments')]");

        var contentNode = doc.DocumentNode.SelectSingleNode("//article")
                         ?? doc.DocumentNode.SelectSingleNode("//*[@itemprop='articleBody']")
                         ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class,'entry-content')]")
                         ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class,'post-content')]")
                         ?? doc.DocumentNode.SelectSingleNode("//main")
                         ?? doc.DocumentNode.SelectSingleNode("//body")
                         ?? doc.DocumentNode;

        var text = HtmlEntity.DeEntitize(contentNode.InnerText);
        return NormalizeText(text);
    }

    private static void RemoveNodes(HtmlDocument doc, string xpath)
    {
        foreach (var node in doc.DocumentNode.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>())
        {
            node.Remove();
        }
    }

    private static string NormalizeText(string input)
    {
        var normalizedLines = input
            .Replace("\r", "")
            .Split('\n')
            .Select(line => Regex.Replace(line, "\\s+", " ").Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, normalizedLines);
    }
}

