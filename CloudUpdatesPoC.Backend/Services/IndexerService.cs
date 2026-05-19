using System.Text.Json;
using CloudUpdatesPoC.Models;

namespace CloudUpdatesPoC.Services;

public class IndexerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IndexerService> _logger;
    private readonly string _updatesPath;

    public IndexerService(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<IndexerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _updatesPath = Path.Combine(
            config["Storage:LocalPath"] ?? "data", "updates");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Espera 30s no boot para o coletor já ter rodado pelo menos uma vez
        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await IndexNewUpdatesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na indexação");
            }

            // Roda a cada hora
            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }

    private async Task IndexNewUpdatesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<VectorStore>();
        var gemini = scope.ServiceProvider.GetRequiredService<GeminiClient>();

        if (!Directory.Exists(_updatesPath))
        {
            _logger.LogWarning("Pasta {Path} ainda não existe", _updatesPath);
            return;
        }

        var files = Directory.EnumerateFiles(_updatesPath, "*.json", SearchOption.AllDirectories);
        int indexados = 0;

        foreach (var file in files)
        {
            if (ct.IsCancellationRequested) break;

            var json = await File.ReadAllTextAsync(file, ct);
            var update = JsonSerializer.Deserialize<CloudUpdate>(json);
            if (update is null) continue;

            if (store.Contains(update.Id)) continue;

            // O texto que vira embedding é o título + categorias + resumo
            // Isso é o que o LLM vai usar para buscar
            var textForEmbedding =
                $"{update.Title}\nCategorias: {string.Join(", ", update.Categories)}\n{update.Summary}";

            try
            {
                var embedding = await gemini.GenerateEmbeddingAsync(textForEmbedding, ct);

                store.Add(new VectorRecord
                {
                    Id = update.Id,
                    Provider = update.Provider,
                    Title = update.Title,
                    Summary = update.Summary,
                    Url = update.Url,
                    PublishedAt = update.PublishedAt,
                    Categories = update.Categories,
                    Embedding = embedding
                });

                indexados++;

                // Tier gratuito do Gemini tem rate limit, então damos um respiro
                await Task.Delay(700, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao indexar {Id}", update.Id);
            }
        }

        if (indexados > 0)
        {
            await store.SaveAsync(ct);
            _logger.LogInformation("Indexados {Count} novos registros. Total: {Total}",
                indexados, store.All.Count);
        }
    }
}