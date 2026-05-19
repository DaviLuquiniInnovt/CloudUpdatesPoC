using System.ServiceModel.Syndication;
using System.Xml;
using CloudUpdatesPoC.Models;

namespace CloudUpdatesPoC.Services;

public class UpdateCollectorService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateCollectorService> _logger;

    private const string AwsFeedUrl = "https://aws.amazon.com/about-aws/whats-new/recent/feed/";
    private const string AzureFeedUrl = "https://www.microsoft.com/releasecommunications/api/v2/azure/rss";

    public UpdateCollectorService(
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider,
        ILogger<UpdateCollectorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<IUpdateStorage>();

                _logger.LogInformation("Iniciando coleta...");

                var aws = await CollectFromFeed(AwsFeedUrl, "AWS", ct);
                _logger.LogInformation("AWS: {Count} itens", aws.Count);

                var azure = await CollectFromFeed(AzureFeedUrl, "Azure", ct);
                _logger.LogInformation("Azure: {Count} itens", azure.Count);

                var novos = 0;
                foreach (var update in aws.Concat(azure))
                {
                    if (!await storage.ExistsAsync(update, ct))
                    {
                        await storage.SaveAsync(update, ct);
                        novos++;
                    }
                }

                _logger.LogInformation("Coleta finalizada. {Novos} novos itens.", novos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na coleta");
            }

            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }

    private async Task<List<CloudUpdate>> CollectFromFeed(
        string feedUrl, string provider, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var xml = await client.GetStringAsync(feedUrl, ct);

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null 
        };
        using var reader = XmlReader.Create(new StringReader(xml), settings);

        var feed = SyndicationFeed.Load(reader);

        return feed.Items.Select(item => new CloudUpdate
        {
            Id = SanitizeId(item.Id),
            Provider = provider,
            Title = item.Title?.Text ?? "",
            Summary = item.Summary?.Text ?? "",
            Url = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
            PublishedAt = item.PublishDate.UtcDateTime,
            Categories = item.Categories.Select(c => c.Name).ToList()
        }).ToList();
    }

    private static string SanitizeId(string id)
    {
        return id.Replace("https://", "")
                 .Replace("http://", "")
                 .Replace("/", "_")
                 .Replace(":", "_")
                 .Replace("?", "_")
                 .Replace("&", "_");
    }
}