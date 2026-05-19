using CloudUpdatesPoC.Models;
using CloudUpdatesPoC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<GeminiClient>();

builder.Services.AddSingleton<IUpdateStorage, LocalFileStorage>();
builder.Services.AddSingleton<VectorStore>();
builder.Services.AddScoped<RagService>();

builder.Services.AddHostedService<UpdateCollectorService>();
builder.Services.AddHostedService<IndexerService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Cloud Updates PoC rodando!");
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

// Quantos vetores estão indexados (útil para debugar)
app.MapGet("/stats", (VectorStore store) => Results.Ok(new
{
    indexed = store.All.Count,
    providers = store.All.GroupBy(r => r.Provider)
                         .ToDictionary(g => g.Key, g => g.Count())
}));

// O endpoint do RAG
app.MapPost("/query", async (QueryRequest req, RagService rag, CancellationToken ct) =>
{
    var result = await rag.QueryAsync(req, ct);
    return Results.Ok(result);
});

app.Run();