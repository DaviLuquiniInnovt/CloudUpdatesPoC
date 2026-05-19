using System.Text;
using CloudUpdatesPoC.Models;

namespace CloudUpdatesPoC.Services;

public class RagService
{
    private readonly VectorStore _store;
    private readonly GeminiClient _gemini;
    private readonly ILogger<RagService> _logger;

    public RagService(VectorStore store, GeminiClient gemini, ILogger<RagService> logger)
    {
        _store = store;
        _gemini = gemini;
        _logger = logger;
    }

    public async Task<RagResponse> QueryAsync(QueryRequest request, CancellationToken ct)
    {
        // 1. Monta o texto da query
        var queryText = string.IsNullOrWhiteSpace(request.Question)
            ? $"Updates que afetam: {string.Join(", ", request.Services)}"
            : $"{request.Question}\nServiços usados: {string.Join(", ", request.Services)}";

        // 2. Gera embedding da pergunta
        var queryEmbedding = await _gemini.GenerateEmbeddingAsync(queryText, ct);

        // 3. Busca os top 10 mais similares
     Func<VectorRecord, bool>? filter = null;
        if (request.Services is { Count: > 0 })
        {
            filter = r =>
            {
                var categories = r.Categories ?? new List<string>();
                var title = r.Title ?? "";

                return request.Services.Any(s =>
                    !string.IsNullOrWhiteSpace(s) &&
                    (categories.Any(c => !string.IsNullOrWhiteSpace(c) &&
                                        c.Contains(s, StringComparison.OrdinalIgnoreCase))
                    || title.Contains(s, StringComparison.OrdinalIgnoreCase)));
            };
        }

        var topResults = _store.Search(queryEmbedding, topK: 10, filter: filter);

        if (topResults.Count == 0)
        {
            return new RagResponse
            {
                Answer = "Nenhum update indexado ainda. Aguarde a primeira coleta.",
                Sources = new()
            };
        }

        // 4. Monta o prompt com o contexto
        var prompt = BuildPrompt(request, topResults);

        // 5. Chama o LLM
        var answer = await _gemini.GenerateAnswerAsync(prompt, ct);

        return new RagResponse
        {
            Answer = answer,
            Sources = topResults.Select(r => new SourceItem
            {
                Title = r.Record.Title,
                Provider = r.Record.Provider,
                Url = r.Record.Url,
                PublishedAt = r.Record.PublishedAt,
                Score = r.Score
            }).ToList()
        };
    }

    private static string BuildPrompt(
        QueryRequest request,
        List<(VectorRecord Record, float Score)> topResults)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Você é um analista de cloud especializado em AWS e Azure.");
        sb.AppendLine("Sua tarefa: analisar a lista de updates abaixo e identificar APENAS os que podem impactar o workload do cliente.");
        sb.AppendLine();
        sb.AppendLine($"Serviços usados pelo cliente: {string.Join(", ", request.Services)}");

        if (!string.IsNullOrWhiteSpace(request.Question))
        {
            sb.AppendLine($"Pergunta específica: {request.Question}");
        }

        sb.AppendLine();
        sb.AppendLine("Updates recentes (use APENAS estes para responder):");
        sb.AppendLine();

        for (int i = 0; i < topResults.Count; i++)
        {
            var r = topResults[i].Record;
            sb.AppendLine($"[{i + 1}] [{r.Provider}] {r.Title}");
            sb.AppendLine($"    Data: {r.PublishedAt:yyyy-MM-dd}");
            sb.AppendLine($"    Categorias: {string.Join(", ", r.Categories)}");
            sb.AppendLine($"    Resumo: {r.Summary}");
            sb.AppendLine();
        }

        sb.AppendLine("Instruções:");
        sb.AppendLine("- Liste APENAS updates relevantes para os serviços do cliente.");
        sb.AppendLine("- Para cada update relevante, explique POR QUE pode impactar o workload.");
        sb.AppendLine("- Se nenhum update for relevante, diga isso claramente.");
        sb.AppendLine("- Cite os updates pelo número entre colchetes [1], [2], etc.");

        return sb.ToString();
    }
}

public class RagResponse
{
    public string Answer { get; set; } = "";
    public List<SourceItem> Sources { get; set; } = new();
}

public class SourceItem
{
    public string Title { get; set; } = "";
    public string Provider { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public float Score { get; set; }
}