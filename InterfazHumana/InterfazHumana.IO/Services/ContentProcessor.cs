using System.Text;
using System.Text.Json;
using InterfazHumana.IO.Data;
using InterfazHumana.IO.Models;

namespace InterfazHumana.IO.Services;

public sealed class ContentProcessor
{
    private readonly IngestionLogRepository _ingestionLogRepository;
    private readonly RawContentRepository _rawContentRepository;
    private readonly ProcessedPostRepository _processedPostRepository;
    private readonly HttpClient _httpClient;
    private readonly int _batchSize;
    private readonly string _aiEndpoint;
    private readonly string _aiModel;
    private readonly string _systemPrompt;

    public ContentProcessor(
        IngestionLogRepository ingestionLogRepository,
        RawContentRepository rawContentRepository,
        ProcessedPostRepository processedPostRepository,
        HttpClient httpClient,
        int batchSize,
        string aiEndpoint,
        string aiModel,
        string systemPrompt)
    {
        _ingestionLogRepository = ingestionLogRepository;
        _rawContentRepository = rawContentRepository;
        _processedPostRepository = processedPostRepository;
        _httpClient = httpClient;
        _batchSize = Math.Max(1, batchSize);
        _aiEndpoint = aiEndpoint;
        _aiModel = aiModel;
        _systemPrompt = systemPrompt;
    }

    public async Task ProcessDownloadedContentAsync()
    {
        while (true)
        {
            var batch = _ingestionLogRepository.GetByStatusBatch("Downloaded", _batchSize);
            if (batch.Count == 0)
            {
                Console.WriteLine("[Processor] No hay contenido descargado pendiente de procesamiento.");
                break;
            }

            Console.WriteLine($"[Processor] Procesando lote Downloaded: {batch.Count}");

            foreach (var log in batch)
            {
                try
                {
                    var rawContent = _rawContentRepository.GetByIngestionLogId(log.Id);
                    if (rawContent is null || string.IsNullOrWhiteSpace(rawContent.RawHtml))
                    {
                        throw new InvalidOperationException("RawHtml no encontrado para el IngestionLog.");
                    }

                    var cleanedText = TextExtractor.ExtractArticleText(rawContent.RawHtml);
                    if (string.IsNullOrWhiteSpace(cleanedText))
                    {
                        throw new InvalidOperationException("No se pudo extraer texto legible del HTML.");
                    }

                    _rawContentRepository.UpdateCleanedTextByIngestionLogId(log.Id, cleanedText);

                    var rewrittenText = await RewriteWithLocalAIAsync(cleanedText);

                    _processedPostRepository.Add(new ProcessedPost(
                        Id: 0,
                        IngestionLogId: log.Id,
                        CategoryId: null,
                        Title: rawContent.ExtractedTitle,
                        Content: rewrittenText,
                        PublishedAt: DateTime.UtcNow));

                    _ingestionLogRepository.UpdateStatus(
                        log.Id,
                        "Processed",
                        errorMessage: null,
                        lastScrapedAt: DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _ingestionLogRepository.UpdateStatus(
                        log.Id,
                        "Failed",
                        errorMessage: ex.Message,
                        lastScrapedAt: DateTime.UtcNow);
                }
            }
        }
    }

    private async Task<string> RewriteWithLocalAIAsync(string originalText)
    {
        var payload = new
        {
            model = _aiModel,
            messages = new object[]
            {
                new { role = "system", content = _systemPrompt },
                new { role = "user", content = originalText }
            },
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, _aiEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("La IA local devolvio contenido vacio.");
        }

        return content.Trim();
    }
}

