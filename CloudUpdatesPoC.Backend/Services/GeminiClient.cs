using System.Text;
using System.Text.Json;

namespace CloudUpdatesPoC.Services;

public class GeminiClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<GeminiClient> _logger;

    // Modelos do Gemini (tier gratuito)
    private const string EmbeddingModel = "gemini-embedding-001";
    private const string LlmModel = "gemini-3.1-flash-lite";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiClient(HttpClient http, IConfiguration config, ILogger<GeminiClient> logger)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey não configurada");
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        var url = $"{BaseUrl}/{EmbeddingModel}:embedContent?key={_apiKey}";

        var body = new
        {
            content = new
            {
                parts = new[] { new { text } }
            }
        };

        var response = await PostJsonAsync(url, body, ct);
        var values = response
            .GetProperty("embedding")
            .GetProperty("values");

        var result = new float[values.GetArrayLength()];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = values[i].GetSingle();
        }
        return result;
    }

    public async Task<string> GenerateAnswerAsync(string prompt, CancellationToken ct)
    {
        var url = $"{BaseUrl}/{LlmModel}:generateContent?key={_apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            }
        };

        var response = await PostJsonAsync(url, body, ct);

        return response
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";
    }

    private async Task<JsonElement> PostJsonAsync(string url, object body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await _http.PostAsync(url, content, ct);
        var respText = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini erro {Status}: {Body}", resp.StatusCode, respText);
            resp.EnsureSuccessStatusCode();
        }

        return JsonDocument.Parse(respText).RootElement.Clone();
    }
}