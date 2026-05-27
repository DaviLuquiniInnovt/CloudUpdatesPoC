using System.Text;
using CloudUpdatesPoC.Models;

namespace CloudUpdatesPoC.Services;

public class RagService
{
    private readonly VectorStore _store;
    private readonly GeminiClient _gemini;
    private readonly WorkloadProfileCatalog _profiles;
    private readonly ILogger<RagService> _logger;

    public RagService(
        VectorStore store,
        GeminiClient gemini,
        WorkloadProfileCatalog profiles,
        ILogger<RagService> logger)
    {
        _store = store;
        _gemini = gemini;
        _profiles = profiles;
        _logger = logger;
    }

    public async Task<RagResponse> QueryAsync(QueryRequest request, CancellationToken ct)
    {
        var profile = _profiles.TryGet(request.ProfileId);
        var effectiveServices = MergeServices(profile, request.Services);

        var queryText = BuildQueryText(request, profile, effectiveServices);

        var queryEmbedding = await _gemini.GenerateEmbeddingAsync(queryText, ct);

        Func<VectorRecord, bool>? filter = null;
        if (effectiveServices.Count > 0)
        {
            filter = r =>
            {
                var categories = r.Categories ?? new List<string>();
                var title = r.Title ?? "";

                return effectiveServices.Any(s =>
                    !string.IsNullOrWhiteSpace(s) &&
                    (categories.Any(c => !string.IsNullOrWhiteSpace(c) &&
                                        c.Contains(s, StringComparison.OrdinalIgnoreCase))
                    || title.Contains(s, StringComparison.OrdinalIgnoreCase)));
            };
        }

        var topResults = _store.Search(queryEmbedding, topK: 10, filter: filter);

        if (topResults.Count == 0)
        {
            var emptyMessage = _store.All.Count == 0
                ? "Nenhum update indexado ainda. Aguarde a primeira coleta."
                : "Nenhum update relevante encontrado para os serviços e perfil informados. Tente ampliar a lista de serviços ou reformular a pergunta.";

            return new RagResponse
            {
                Answer = emptyMessage,
                Sources = new()
            };
        }

        var prompt = BuildPrompt(request, profile, effectiveServices, topResults);
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

    private static List<string> MergeServices(WorkloadProfile? profile, List<string> requestServices)
    {
        var merged = new List<string>();
        if (profile != null)
        {
            merged.AddRange(profile.Services);
        }

        foreach (var s in requestServices)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (!merged.Any(m => m.Equals(s, StringComparison.OrdinalIgnoreCase)))
            {
                merged.Add(s.Trim());
            }
        }

        return merged;
    }

    private static string BuildQueryText(
        QueryRequest request,
        WorkloadProfile? profile,
        List<string> effectiveServices)
    {
        var sb = new StringBuilder();

        if (profile != null)
        {
            AppendProfileContext(sb, profile);
        }

        if (!string.IsNullOrWhiteSpace(request.Question))
        {
            sb.AppendLine(request.Question.Trim());
        }
        else
        {
            sb.AppendLine("Updates recentes que podem impactar este workload");
        }

        sb.AppendLine($"Serviços: {string.Join(", ", effectiveServices)}");
        return sb.ToString();
    }

    private static string BuildPrompt(
        QueryRequest request,
        WorkloadProfile? profile,
        List<string> effectiveServices,
        List<(VectorRecord Record, float Score)> topResults)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Você é um analista de cloud especializado em AWS e Azure.");
        sb.AppendLine("Analise APENAS os updates listados abaixo e produza uma resposta em português (Brasil), em Markdown.");
        sb.AppendLine("Use o perfil de workload do cliente como contexto fixo — NÃO use frases hipotéticas como \"se você usar Lambda\" quando o perfil já confirma esse serviço.");
        sb.AppendLine("Fale em termos diretos: \"no seu workload\", \"na sua stack\", \"para este perfil\".");
        sb.AppendLine();

        if (profile != null)
        {
            sb.AppendLine("## Conta cloud simulada (dados fixos — trate como inventário real do cliente)");
            AppendProfileContext(sb, profile);
            sb.AppendLine();
        }

        sb.AppendLine($"Serviços considerados na análise (perfil + ajustes do usuário): {string.Join(", ", effectiveServices)}");

        if (!string.IsNullOrWhiteSpace(request.Question))
        {
            sb.AppendLine($"Pergunta do usuário: {request.Question.Trim()}");
        }

        sb.AppendLine();
        sb.AppendLine("## Updates recuperados (use APENAS estes como evidência):");

        for (int i = 0; i < topResults.Count; i++)
        {
            var r = topResults[i].Record;
            sb.AppendLine($"[{i + 1}] [{r.Provider}] {r.Title}");
            sb.AppendLine($"    Data: {r.PublishedAt:yyyy-MM-dd}");
            sb.AppendLine($"    Categorias: {string.Join(", ", r.Categories)}");
            sb.AppendLine($"    Resumo: {TruncateSummary(r.Summary, 800)}");
            sb.AppendLine();
        }

        sb.AppendLine("## Formato obrigatório da resposta (Markdown)");
        sb.AppendLine("Siga exatamente esta estrutura:");
        sb.AppendLine("## Resumo executivo");
        sb.AppendLine("(2-4 frases sintetizando o que importa para este perfil — não seja uma lista solta)");
        sb.AppendLine();
        sb.AppendLine("## Updates com potencial impacto");
        sb.AppendLine("Para cada update relevante, use um bloco com:");
        sb.AppendLine("### [n] Título do update");
        sb.AppendLine("- **O que mudou:** (resuma em 1-2 frases)");
        sb.AppendLine("- **Impacto financeiro:** (use valores e % de spend do perfil quando aplicável; ex.: \"representa ~22% do gasto mensal\")");
        sb.AppendLine("- **Impacto técnico:**");
        sb.AppendLine("- **Impacto operacional:**");
        sb.AppendLine("- **Impacto de segurança/compliance:** (omitir a linha inteira se não aplicável)");
        sb.AppendLine("- **Por que isso importa para este perfil:**");
        sb.AppendLine();
        sb.AppendLine("## Ações sugeridas");
        sb.AppendLine("(bullets práticos e priorizados)");
        sb.AppendLine();
        sb.AppendLine("Regras:");
        sb.AppendLine("- Inclua APENAS updates com impacto real para este perfil e stack.");
        sb.AppendLine("- Se nenhum update for relevante, escreva isso no Resumo executivo e deixe a seção de updates vazia ou com uma frase clara.");
        sb.AppendLine("- Cite updates pelo número [1], [2], etc.");
        sb.AppendLine("- Não invente serviços, custos ou recursos que não estejam no perfil ou nos updates.");
        sb.AppendLine("- Priorize impactos alinhados às sensibilidades do perfil.");
        sb.AppendLine("- Quantifique impacto financeiro com base nos números do perfil (gasto mensal, top services, budget).");
        sb.AppendLine("- Referencie recursos concretos do inventário (tipos, quantidades, regiões) quando explicar impacto técnico/operacional.");

        return sb.ToString();
    }

    private static void AppendProfileContext(StringBuilder sb, WorkloadProfile profile)
    {
        sb.AppendLine($"- Empresa: {profile.Organization.CompanyName} ({profile.Organization.Industry}, ~{profile.Organization.EmployeeCount} colaboradores)");
        sb.AppendLine($"- Produto: {profile.Organization.ProductDescription}");
        sb.AppendLine($"- Perfil: {profile.Name}");
        sb.AppendLine($"- Descrição: {profile.Description}");
        sb.AppendLine($"- Contexto de negócio: {profile.BusinessContext}");
        sb.AppendLine($"- Arquitetura: {profile.ArchitectureNotes}");
        sb.AppendLine($"- Prioridades: {string.Join(", ", profile.Priorities)}");
        sb.AppendLine($"- Stack em uso: {string.Join(", ", profile.Services)}");

        sb.AppendLine();
        sb.AppendLine("### Contas e regiões");
        foreach (var acc in profile.Accounts)
        {
            sb.AppendLine($"- [{acc.Provider}] {acc.AccountLabel} ({acc.AccountId}) — regiões: {string.Join(", ", acc.PrimaryRegions)}; ambientes: {string.Join(", ", acc.Environments)}");
        }

        var fin = profile.Financials;
        sb.AppendLine();
        sb.AppendLine("### FinOps (último fechamento mensal simulado)");
        sb.AppendLine($"- Gasto mensal: USD {fin.MonthlySpendUsd:N0} | Run-rate anual: USD {fin.AnnualRunRateUsd:N0}");
        sb.AppendLine($"- Budget: USD {fin.MonthlyBudgetUsd:N0} ({fin.BudgetUtilizationPercent:N1}% utilizado)");
        sb.AppendLine($"- Modelo de cobrança: {fin.BillingModel}");
        sb.AppendLine($"- Tendência: {fin.CostTrend}");
        sb.AppendLine("- Top custos por serviço:");
        foreach (var cost in fin.TopCostServices)
        {
            sb.AppendLine($"  - {cost.Service}: USD {cost.MonthlyUsd:N0}/mês ({cost.PercentOfTotal:N1}%) — {cost.Notes}");
        }
        if (fin.CostDrivers.Count > 0)
        {
            sb.AppendLine($"- Drivers de custo: {string.Join("; ", fin.CostDrivers)}");
        }
        if (fin.Commitments.Count > 0)
        {
            sb.AppendLine($"- Compromissos/reservas: {string.Join("; ", fin.Commitments)}");
        }

        sb.AppendLine();
        sb.AppendLine("### Inventário em produção (recursos reais do perfil)");
        foreach (var item in profile.Inventory)
        {
            sb.AppendLine($"- {item.Service}: {item.ResourceDescription} | {item.QuantityOrSku} [{item.Environment}]");
        }

        sb.AppendLine();
        sb.AppendLine("### Operação");
        sb.AppendLine($"- Deploys: {profile.Operations.DeploymentFrequency}");
        sb.AppendLine($"- Plantão: {profile.Operations.OncallModel}");
        sb.AppendLine($"- SLAs críticos: {string.Join("; ", profile.Operations.CriticalSlas)}");
        sb.AppendLine($"- Janela de mudança: {profile.Operations.ChangeWindow}");
        sb.AppendLine($"- Sensibilidade a incidentes: {profile.Operations.IncidentSensitivity}");

        sb.AppendLine();
        sb.AppendLine("### Compliance e segurança");
        sb.AppendLine($"- Frameworks: {string.Join(", ", profile.Compliance.Frameworks)}");
        sb.AppendLine($"- Classificação de dados: {profile.Compliance.DataClassification}");
        sb.AppendLine($"- Identidade: {profile.Compliance.IdentityModel}");
        if (profile.Compliance.NetworkPosture.Count > 0)
        {
            sb.AppendLine($"- Rede: {string.Join("; ", profile.Compliance.NetworkPosture)}");
        }

        sb.AppendLine();
        sb.AppendLine("### Sensibilidades para priorização");
        sb.AppendLine($"- Financeira: {profile.Sensitivity.Financial}");
        sb.AppendLine($"- Técnica: {profile.Sensitivity.Technical}");
        sb.AppendLine($"- Operacional: {profile.Sensitivity.Operational}");
        sb.AppendLine($"- Segurança/compliance: {profile.Sensitivity.SecurityCompliance}");
    }

    private static string TruncateSummary(string summary, int maxLength)
    {
        if (string.IsNullOrEmpty(summary)) return "";
        var cleaned = summary.Replace("\r", " ").Replace("\n", " ").Trim();
        if (cleaned.Length <= maxLength) return cleaned;
        return cleaned[..maxLength] + "…";
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
