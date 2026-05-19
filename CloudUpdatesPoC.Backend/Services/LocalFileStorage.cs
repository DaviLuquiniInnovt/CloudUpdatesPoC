using System.Text.Json;
using CloudUpdatesPoC.Models;

namespace CloudUpdatesPoC.Services;

public class LocalFileStorage : IUpdateStorage
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(ILogger<LocalFileStorage> logger, IConfiguration config)
    {
        _logger = logger;
        // Pega de appsettings ou usa "data" como default
        _basePath = config["Storage:LocalPath"] ?? "data";
        Directory.CreateDirectory(_basePath);
    }

    public Task<bool> ExistsAsync(CloudUpdate update, CancellationToken ct)
    {
        var path = BuildPath(update);
        return Task.FromResult(File.Exists(path));
    }

    public async Task SaveAsync(CloudUpdate update, CancellationToken ct)
    {
        var path = BuildPath(update);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(update,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(path, json, ct);
        _logger.LogInformation("Salvo: {Path}", path);
    }

    private string BuildPath(CloudUpdate update)
    {
        return Path.Combine(
            _basePath,
            "updates",
            update.Provider,
            update.PublishedAt.ToString("yyyy"),
            update.PublishedAt.ToString("MM"),
            update.PublishedAt.ToString("dd"),
            $"{update.Id}.json"
        );
    }
}