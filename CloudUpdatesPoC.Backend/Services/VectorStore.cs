using System.Text.Json;
using CloudUpdatesPoC.Models;

namespace CloudUpdatesPoC.Services;

public class VectorStore
{
    private readonly string _path;
    private readonly ILogger<VectorStore> _logger;
    private List<VectorRecord> _records = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public VectorStore(IConfiguration config, ILogger<VectorStore> logger)
    {
        _path = config["VectorStore:Path"] ?? "data/vectors.json";
        _logger = logger;
        Load();
    }

    public IReadOnlyList<VectorRecord> All => _records;

    private void Load()
    {
        if (!File.Exists(_path))
        {
            _records = new List<VectorRecord>();
            return;
        }

        var json = File.ReadAllText(_path);
        _records = JsonSerializer.Deserialize<List<VectorRecord>>(json) ?? new();
        _logger.LogInformation("VectorStore carregado: {Count} registros", _records.Count);
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var json = JsonSerializer.Serialize(_records);
            await File.WriteAllTextAsync(_path, json, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool Contains(string id) => _records.Any(r => r.Id == id);

    public void Add(VectorRecord record)
    {
        if (!Contains(record.Id))
        {
            _records.Add(record);
        }
    }

    // Busca os top-K mais similares ao embedding da pergunta
    public List<(VectorRecord Record, float Score)> Search(
        float[] queryEmbedding, int topK, Func<VectorRecord, bool>? filter = null)
    {
        var candidates = filter is null ? _records : _records.Where(filter);

        return candidates
            .Select(r => (Record: r, Score: CosineSimilarity(queryEmbedding, r.Embedding)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB) + 1e-10f);
    }
}