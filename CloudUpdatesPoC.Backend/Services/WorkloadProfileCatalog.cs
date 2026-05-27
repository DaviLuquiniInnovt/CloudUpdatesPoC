using CloudUpdatesPoC.Models;

namespace CloudUpdatesPoC.Services;

public class WorkloadProfileCatalog
{
    private readonly IReadOnlyDictionary<string, WorkloadProfile> _profiles;

    public WorkloadProfileCatalog()
    {
        var list = new List<WorkloadProfile>
        {
            BuildNebulaPayProfile(),
            BuildHorizonRetailProfile(),
            BuildMedCoreProfile(),
            BuildLogiChainProfile()
        };

        _profiles = list.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<WorkloadProfileSummary> ListSummaries() =>
        _profiles.Values.Select(ToSummary).ToList();

    public WorkloadProfile? TryGet(string? profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId)) return null;
        return _profiles.TryGetValue(profileId.Trim(), out var profile) ? profile : null;
    }

    private static WorkloadProfileSummary ToSummary(WorkloadProfile p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Services = p.Services,
        Snapshot = new WorkloadProfileSnapshot
        {
            CompanyName = p.Organization.CompanyName,
            Industry = p.Organization.Industry,
            MonthlySpendUsd = p.Financials.MonthlySpendUsd,
            MonthlyBudgetUsd = p.Financials.MonthlyBudgetUsd,
            BudgetUtilizationPercent = p.Financials.BudgetUtilizationPercent,
            AccountLabels = p.Accounts.Select(a => $"{a.Provider}: {a.AccountLabel} ({a.AccountId})").ToList(),
            PrimaryRegions = p.Accounts.SelectMany(a => a.PrimaryRegions).Distinct().ToList(),
            TopCostServices = p.Financials.TopCostServices.Take(5).ToList(),
            InventoryHighlights = p.Inventory.Take(6).Select(i =>
                $"{i.Service}: {i.ResourceDescription} — {i.QuantityOrSku} [{i.Environment}]").ToList(),
            ComplianceFrameworks = p.Compliance.Frameworks
        }
    };

    private static WorkloadProfile BuildNebulaPayProfile() => new()
    {
        Id = "serverless-saas-aws",
        Name = "NebulaPay — SaaS Serverless (AWS)",
        Description = "Fintech B2B de pagamentos recorrentes. Conta AWS prod ativa em us-east-1 com stack serverless madura.",
        Services = ["Lambda", "API Gateway", "S3", "RDS", "DynamoDB", "CloudFront", "SQS", "Secrets Manager", "Cognito"],
        BusinessContext =
            "NebulaPay processa ~2,4M transações/mês para 380 clientes PJ. Picos no dia 5 e dia 20 (fechamento). " +
            "Qualquer degradação na API de cobrança impacta receita no mesmo dia.",
        ArchitectureNotes =
            "APIs REST e webhooks em Lambda + API Gateway (HTTP + REST). Filas SQS para conciliação e antifraude assíncrono. " +
            "RDS PostgreSQL Multi-AZ para ledger; DynamoDB para idempotência e sessões. CloudFront na frente do portal merchant. " +
            "Deploy via GitHub Actions, 12–18 releases/semana no serviço core.",
        Priorities = ["latência P95 < 180ms na API de cobrança", "custo por transação", "disponibilidade 99,95%", "auditoria PCI"],
        Organization = new ProfileOrganization
        {
            CompanyName = "NebulaPay Tecnologia Ltda.",
            Industry = "Fintech / pagamentos B2B",
            EmployeeCount = 85,
            ProductDescription = "Plataforma de cobrança recorrente e conciliação para SaaS e marketplaces."
        },
        Accounts =
        [
            new ProfileCloudAccount
            {
                Provider = "AWS",
                AccountLabel = "nebulapay-prod",
                AccountId = "782910345678",
                PrimaryRegions = ["us-east-1", "us-west-2"],
                Environments = ["production", "staging"]
            },
            new ProfileCloudAccount
            {
                Provider = "AWS",
                AccountLabel = "nebulapay-nonprod",
                AccountId = "112098765432",
                PrimaryRegions = ["us-east-1"],
                Environments = ["development", "qa"]
            }
        ],
        Financials = new ProfileFinancials
        {
            MonthlySpendUsd = 48_200,
            AnnualRunRateUsd = 578_400,
            MonthlyBudgetUsd = 52_000,
            BudgetUtilizationPercent = 92.7m,
            BillingModel = "Pay-as-you-go com Savings Plans Compute (1 ano, all-upfront parcial)",
            CostTrend = "+6,2% vs mês anterior (aumento em invocações Lambda e egress CloudFront)",
            TopCostServices =
            [
                new() { Service = "Amazon RDS", MonthlyUsd = 11_240, PercentOfTotal = 23.3m, Notes = "db.r6g.large Multi-AZ + 400GB gp3" },
                new() { Service = "AWS Lambda", MonthlyUsd = 10_560, PercentOfTotal = 21.9m, Notes = "142 funções; pico 18M invocações/mês" },
                new() { Service = "Amazon API Gateway", MonthlyUsd = 6_820, PercentOfTotal = 14.1m, Notes = "REST + HTTP APIs; alto volume webhook" },
                new() { Service = "Amazon CloudFront", MonthlyUsd = 5_140, PercentOfTotal = 10.7m, Notes = "Portal merchant + assets estáticos" },
                new() { Service = "Amazon DynamoDB", MonthlyUsd = 4_310, PercentOfTotal = 8.9m, Notes = "8 tabelas on-demand; hot partition em idempotency" },
                new() { Service = "Amazon S3", MonthlyUsd = 2_980, PercentOfTotal = 6.2m, Notes = "Logs, exports NF-e, backups" }
            ],
            CostDrivers =
            [
                "Invocações Lambda no motor de conciliação (+22% MoM)",
                "Data transfer out CloudFront para merchants na LATAM",
                "RDS storage growth por retenção de 13 meses de ledger"
            ],
            Commitments =
            [
                "Compute Savings Plan: USD 4.200/mês comprometido",
                "RDS Reserved Instance 1yr para instância primária ledger"
            ],
            OptimizationOpportunities =
            [
                "Rightsizing de 23 funções com memória 1024MB usando média < 200MB",
                "Revisar TTL e GSIs em DynamoDB idempotency",
                "Lifecycle S3 para logs > 90 dias → Glacier"
            ]
        },
        Inventory =
        [
            new() { Service = "Lambda", ResourceDescription = "Funções de cobrança, webhooks, conciliação", QuantityOrSku = "142 funções (128 prod)", Environment = "production" },
            new() { Service = "API Gateway", ResourceDescription = "APIs públicas merchant + internas", QuantityOrSku = "3 REST + 1 HTTP API", Environment = "production" },
            new() { Service = "RDS", ResourceDescription = "PostgreSQL ledger", QuantityOrSku = "db.r6g.large Multi-AZ", Environment = "production" },
            new() { Service = "DynamoDB", ResourceDescription = "Idempotência e cache de sessão", QuantityOrSku = "8 tabelas on-demand", Environment = "production" },
            new() { Service = "SQS", ResourceDescription = "Filas de conciliação e DLQ", QuantityOrSku = "14 filas standard", Environment = "production" },
            new() { Service = "Cognito", ResourceDescription = "User pool merchants", QuantityOrSku = "~12.400 usuários ativos", Environment = "production" },
            new() { Service = "CloudFront", ResourceDescription = "Distribuições portal", QuantityOrSku = "2 distribuições", Environment = "production" }
        ],
        Operations = new ProfileOperationalReality
        {
            DeploymentFrequency = "12–18 deploys/semana no serviço core; feature flags via AppConfig",
            OncallModel = "Plantão 24x7 rotação 6 engenheiros (PagerDuty)",
            CriticalSlas = ["API cobrança 99,95% mensal", "P95 latência < 180ms", "processamento webhook < 5 min"],
            ChangeWindow = "Ter–Qui 22h–02h BRT; freeze em semana de fechamento mensal",
            IncidentSensitivity = "Sev-1 se falha de cobrança > 5 min ou fila conciliação > 30 min de atraso"
        },
        Compliance = new ProfileCompliance
        {
            Frameworks = ["PCI-DSS SAQ-A", "LGPD", "SOC 2 Type II (em auditoria)"],
            DataClassification = "PII financeira e tokens de pagamento tokenizados (não armazena PAN completo)",
            IdentityModel = "IAM roles por serviço; SSO corporativo; sem access keys long-lived em prod",
            NetworkPosture = ["VPC com subnets privadas para RDS", "WAF no CloudFront", "SG least-privilege"]
        },
        Sensitivity = new ProfileSensitivity
        {
            Financial = "crítica — margem apertada; Lambda+RDS+API GW = 59% do spend",
            Technical = "crítica — runtime Lambda e limites API Gateway afetam releases diários",
            Operational = "alta — filas e conciliação são sensíveis a atraso",
            SecurityCompliance = "crítica — PCI e LGPD; mudanças em auth/WAF são bloqueantes"
        }
    };

    private static WorkloadProfile BuildHorizonRetailProfile() => new()
    {
        Id = "kubernetes-platform-azure",
        Name = "Horizon Retail — Plataforma Kubernetes (Azure)",
        Description = "Varejo omnichannel. Subscription Azure prod com 3 clusters AKS e malha de microsserviços.",
        Services = ["AKS", "Azure Container Registry", "Azure Monitor", "Application Gateway", "Azure Key Vault", "Azure SQL", "Azure Service Bus", "Azure Front Door"],
        BusinessContext =
            "Horizon Retail opera e-commerce + 120 lojas. Plataforma digital atende catálogo, estoque, checkout e logística. " +
            "Black Friday e datas sazonais exigem scale-out previsível.",
        ArchitectureNotes =
            "3 clusters AKS (prod, staging, tools). Ingress via Application Gateway + WAF. Microsserviços .NET 8 em containers. " +
            "Azure SQL Hyperscale para pedidos; Service Bus para eventos de estoque. GitOps com FluxCD.",
        Priorities = ["disponibilidade do checkout", "tempo de deploy < 15 min", "observabilidade unificada", "custo de cluster controlado"],
        Organization = new ProfileOrganization
        {
            CompanyName = "Horizon Retail Group S.A.",
            Industry = "Varejo / e-commerce",
            EmployeeCount = 4200,
            ProductDescription = "Operação omnichannel de moda e lifestyle."
        },
        Accounts =
        [
            new ProfileCloudAccount
            {
                Provider = "Azure",
                AccountLabel = "hrg-digital-prod",
                AccountId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                PrimaryRegions = ["Brazil South", "East US 2"],
                Environments = ["production"]
            },
            new ProfileCloudAccount
            {
                Provider = "Azure",
                AccountLabel = "hrg-digital-nonprod",
                AccountId = "f9e8d7c6-b5a4-3210-fedc-ba0987654321",
                PrimaryRegions = ["Brazil South"],
                Environments = ["staging", "development"]
            }
        ],
        Financials = new ProfileFinancials
        {
            MonthlySpendUsd = 67_400,
            AnnualRunRateUsd = 808_800,
            MonthlyBudgetUsd = 70_000,
            BudgetUtilizationPercent = 96.3m,
            BillingModel = "Enterprise Agreement com commitment de compute",
            CostTrend = "+3,1% MoM (scale de nodes AKS pós-campanha)",
            TopCostServices =
            [
                new() { Service = "Azure Kubernetes Service", MonthlyUsd = 24_600, PercentOfTotal = 36.5m, Notes = "3 clusters; ~186 nodes prod (D4s_v5 mix)" },
                new() { Service = "Azure SQL Database", MonthlyUsd = 14_200, PercentOfTotal = 21.1m, Notes = "Hyperscale pedidos + réplicas leitura" },
                new() { Service = "Application Gateway", MonthlyUsd = 7_850, PercentOfTotal = 11.6m, Notes = "WAF v2 + múltiplos listeners" },
                new() { Service = "Azure Monitor", MonthlyUsd = 5_420, PercentOfTotal = 8.0m, Notes = "Logs 1.2TB/mês; métricas customizadas" },
                new() { Service = "Azure Front Door", MonthlyUsd = 4_180, PercentOfTotal = 6.2m, Notes = "CDN global checkout" },
                new() { Service = "Azure Service Bus", MonthlyUsd = 3_090, PercentOfTotal = 4.6m, Notes = "Premium tier eventos estoque" }
            ],
            CostDrivers =
            [
                "Node pool checkout-prod com autoscale agressivo",
                "Retenção de logs 90 dias no Log Analytics",
                "Réplicas de leitura SQL em pico"
            ],
            Commitments =
            [
                "Azure reservation 3yr para D-family nos node pools estáveis",
                "SQL reserved capacity parcial"
            ],
            OptimizationOpportunities =
            [
                "Cluster autoscaler tuning no pool catalog",
                "Sampling de traces em não-críticos",
                "Archive tier para logs > 60 dias"
            ]
        },
        Inventory =
        [
            new() { Service = "AKS", ResourceDescription = "Cluster checkout", QuantityOrSku = "68 nodes D4s_v5 (prod)", Environment = "production" },
            new() { Service = "AKS", ResourceDescription = "Cluster catálogo/estoque", QuantityOrSku = "52 nodes D4s_v5", Environment = "production" },
            new() { Service = "Azure SQL", ResourceDescription = "DB pedidos Hyperscale", QuantityOrSku = "32 vCores + 2 réplicas leitura", Environment = "production" },
            new() { Service = "Application Gateway", ResourceDescription = "Ingress WAF", QuantityOrSku = "2 instâncias WAF_v2", Environment = "production" },
            new() { Service = "Azure Service Bus", ResourceDescription = "Tópicos estoque/checkout", QuantityOrSku = "Premium namespace", Environment = "production" },
            new() { Service = "Azure Key Vault", ResourceDescription = "Secrets e certs TLS", QuantityOrSku = "4 vaults", Environment = "production" }
        ],
        Operations = new ProfileOperationalReality
        {
            DeploymentFrequency = "8–14 deploys/dia somando todos os microsserviços; canary no checkout",
            OncallModel = "SRE 24x7 + plantão squad checkout em datas críticas",
            CriticalSlas = ["Checkout 99,9%", "API catálogo P99 < 400ms", "eventos estoque < 2 min end-to-end"],
            ChangeWindow = "Deploy contínuo exceto Black Friday (−14 dias freeze parcial)",
            IncidentSensitivity = "Sev-1 se checkout indisponível > 2 min ou perda de eventos de estoque"
        },
        Compliance = new ProfileCompliance
        {
            Frameworks = ["LGPD", "ISO 27001", "PCI-DSS escopo reduzido no checkout"],
            DataClassification = "PII clientes, histórico de compras, dados de pagamento tokenizados",
            IdentityModel = "Entra ID + workload identity federada nos pods",
            NetworkPosture = ["Private clusters AKS", "Private Link para SQL", "NSG por subnet"]
        },
        Sensitivity = new ProfileSensitivity
        {
            Financial = "alta — AKS+SQL = 57% do spend; budget quase no limite",
            Technical = "crítica — upgrades AKS e AGW afetam todos os times",
            Operational = "crítica — operação diária de clusters e alertas",
            SecurityCompliance = "alta — identidade federada e WAF"
        }
    };

    private static WorkloadProfile BuildMedCoreProfile() => new()
    {
        Id = "data-analytics-azure",
        Name = "MedCore — Data & Analytics (Azure)",
        Description = "Healthtech analytics. Lakehouse Azure com Databricks como motor principal de processamento.",
        Services = ["Azure Databricks", "Azure Data Lake Storage", "Azure Synapse", "Event Hubs", "Azure Functions", "Power BI", "Microsoft Purview"],
        BusinessContext =
            "MedCore consolida dados clínicos e operacionais de 14 hospitais parceiros (anonimizados). " +
            "Dashboards regulatórios e modelos preditivos de ocupação alimentam decisões diárias.",
        ArchitectureNotes =
            "Ingestão via Event Hubs → ADLS Bronze/Silver/Gold → Databricks Unity Catalog. " +
            "Synapse serverless para queries ad hoc. Power BI Premium para ~220 usuários internos.",
        Priorities = ["governança de dados", "custo de Databricks", "SLA pipelines D+1", "anonimização"],
        Organization = new ProfileOrganization
        {
            CompanyName = "MedCore Analytics Ltda.",
            Industry = "Healthtech / dados clínicos",
            EmployeeCount = 210,
            ProductDescription = "Plataforma de analytics e reporting para redes de saúde."
        },
        Accounts =
        [
            new ProfileCloudAccount
            {
                Provider = "Azure",
                AccountLabel = "medcore-data-prod",
                AccountId = "c3d4e5f6-a7b8-9012-cdef-345678901234",
                PrimaryRegions = ["East US 2", "Brazil South"],
                Environments = ["production", "sandbox-ml"]
            }
        ],
        Financials = new ProfileFinancials
        {
            MonthlySpendUsd = 89_150,
            AnnualRunRateUsd = 1_069_800,
            MonthlyBudgetUsd = 92_000,
            BudgetUtilizationPercent = 96.9m,
            BillingModel = "Pay-as-you-go + Databricks commit units pré-compradas",
            CostTrend = "+8,4% MoM (novos jobs ML e aumento DBU)",
            TopCostServices =
            [
                new() { Service = "Azure Databricks", MonthlyUsd = 41_200, PercentOfTotal = 46.2m, Notes = "~18.400 DBU/mês; jobs ETL + ML" },
                new() { Service = "Azure Data Lake Storage", MonthlyUsd = 12_600, PercentOfTotal = 14.1m, Notes = "480TB logical; hot tier Gold" },
                new() { Service = "Azure Synapse", MonthlyUsd = 9_800, PercentOfTotal = 11.0m, Notes = "Serverless SQL pools" },
                new() { Service = "Event Hubs", MonthlyUsd = 7_400, PercentOfTotal = 8.3m, Notes = "2 namespaces Premium" },
                new() { Service = "Power BI", MonthlyUsd = 6_200, PercentOfTotal = 7.0m, Notes = "Premium P1 capacity" },
                new() { Service = "Azure Functions", MonthlyUsd = 2_100, PercentOfTotal = 2.4m, Notes = "Orquestração leve e webhooks" }
            ],
            CostDrivers =
            [
                "Jobs Spark de enriquecimento clínico (novo pipeline)",
                "Retenção Gold 24 meses regulatório",
                "Picos de ingestão Event Hubs em horário de pico hospitalar"
            ],
            Commitments =
            [
                "Databricks commit: 15.000 DBU/mês",
                "Reserva storage ADLS hot tier parcial"
            ],
            OptimizationOpportunities =
            [
                "Photon e autoscaling nos clusters ETL noturnos",
                "Compactação Delta e VACUUM agendado",
                "Downgrade queries ad hoc Synapse para horário comercial"
            ]
        },
        Inventory =
        [
            new() { Service = "Azure Databricks", ResourceDescription = "Workspace produção", QuantityOrSku = "3 clusters (ETL, ML, ad hoc)", Environment = "production" },
            new() { Service = "Azure Data Lake Storage", ResourceDescription = "Lakehouse medallion", QuantityOrSku = "480TB logical", Environment = "production" },
            new() { Service = "Event Hubs", ResourceDescription = "Ingestão streaming", QuantityOrSku = "2 namespaces Premium, 32 partitions", Environment = "production" },
            new() { Service = "Azure Synapse", ResourceDescription = "SQL serverless", QuantityOrSku = "2 pools serverless", Environment = "production" },
            new() { Service = "Power BI", ResourceDescription = "Capacidade Premium", QuantityOrSku = "P1, ~220 usuários", Environment = "production" },
            new() { Service = "Microsoft Purview", ResourceDescription = "Catálogo e lineage", QuantityOrSku = "1 account", Environment = "production" }
        ],
        Operations = new ProfileOperationalReality
        {
            DeploymentFrequency = "Pipelines dbt/Databricks 2–4 deploys/dia; notebooks ML semanais",
            OncallModel = "Data platform oncall horário comercial + plantão domingo para jobs críticos",
            CriticalSlas = ["Pipeline regulatório D+1 até 06:00 BRT", "freshness Gold < 4h para dashboards operacionais"],
            ChangeWindow = "Jobs críticos apenas 20h–06h; freeze em auditorias trimestrais",
            IncidentSensitivity = "Sev-2 se atraso > 2h em pipeline regulatório; Sev-1 se vazamento de dados"
        },
        Compliance = new ProfileCompliance
        {
            Frameworks = ["LGPD", "HIPAA-aligned controls", "ANVISA reporting"],
            DataClassification = "Dados de saúde anonimizados; PHI em zonas isoladas com mascaramento",
            IdentityModel = "Entra ID + SCIM; RBAC Unity Catalog por domínio",
            NetworkPosture = ["Private endpoints ADLS/Databricks", "Sem acesso público aos workspaces"]
        },
        Sensitivity = new ProfileSensitivity
        {
            Financial = "crítica — Databricks sozinho é 46% do spend",
            Technical = "alta — versões runtime e APIs de conectores",
            Operational = "média-alta — atraso de pipeline, não tempo real",
            SecurityCompliance = "crítica — dados de saúde e auditoria"
        }
    };

    private static WorkloadProfile BuildLogiChainProfile() => new()
    {
        Id = "hybrid-integration",
        Name = "LogiChain — Integração Híbrida (AWS + Azure)",
        Description = "Logística global. Backbone de integração entre ERP SAP, parceiros e TMS em AWS e Azure.",
        Services = ["Amazon SQS", "AWS Lambda", "Amazon API Gateway", "Azure Service Bus", "Azure Functions", "Azure API Management", "VPC", "Azure ExpressRoute"],
        BusinessContext =
            "LogiChain conecta 40+ parceiros logísticos. Volume médio 850k mensagens/dia, picos em Black Friday logística. " +
            "Contratos SLA com multas por atraso de status de remessa.",
        ArchitectureNotes =
            "AWS concentra APIs B2B expostas e processamento de alto volume; Azure integra ERP e TMS legado via Service Bus. " +
            "Conectividade híbrida ExpressRoute + VPN site-to-site. Schema registry centralizado.",
        Priorities = ["confiabilidade de filas", "rastreabilidade ponta a ponta", "compatibilidade de contratos API", "custo de transferência cross-cloud"],
        Organization = new ProfileOrganization
        {
            CompanyName = "LogiChain Global Logistics",
            Industry = "Logística / supply chain",
            EmployeeCount = 1200,
            ProductDescription = "Hub de integração de eventos de transporte e status de remessas."
        },
        Accounts =
        [
            new ProfileCloudAccount
            {
                Provider = "AWS",
                AccountLabel = "logichain-integration-prod",
                AccountId = "334455667788",
                PrimaryRegions = ["eu-west-1", "sa-east-1"],
                Environments = ["production"]
            },
            new ProfileCloudAccount
            {
                Provider = "Azure",
                AccountLabel = "logichain-erp-prod",
                AccountId = "d4e5f6a7-b8c9-0123-def4-567890abcdef",
                PrimaryRegions = ["West Europe", "Brazil South"],
                Environments = ["production"]
            }
        ],
        Financials = new ProfileFinancials
        {
            MonthlySpendUsd = 54_680,
            AnnualRunRateUsd = 656_160,
            MonthlyBudgetUsd = 58_000,
            BudgetUtilizationPercent = 94.3m,
            BillingModel = "AWS e Azure EA separados; chargeback por domínio de integração",
            CostTrend = "+4,7% MoM (aumento data transfer cross-cloud)",
            TopCostServices =
            [
                new() { Service = "Azure Service Bus", MonthlyUsd = 11_400, PercentOfTotal = 20.8m, Notes = "Premium; 6 namespaces" },
                new() { Service = "AWS Lambda", MonthlyUsd = 9_200, PercentOfTotal = 16.8m, Notes = "Transformações e roteamento" },
                new() { Service = "Amazon API Gateway", MonthlyUsd = 7_600, PercentOfTotal = 13.9m, Notes = "APIs parceiros B2B" },
                new() { Service = "Azure API Management", MonthlyUsd = 6_900, PercentOfTotal = 12.6m, Notes = "Tier Premium internal + external" },
                new() { Service = "Data Transfer", MonthlyUsd = 5_800, PercentOfTotal = 10.6m, Notes = "Cross-cloud AWS↔Azure" },
                new() { Service = "Amazon SQS", MonthlyUsd = 4_200, PercentOfTotal = 7.7m, Notes = "42 filas + DLQ" }
            ],
            CostDrivers =
            [
                "Egress cross-cloud entre regiões sa-east-1 e Brazil South",
                "Service Bus Premium throughput em pico",
                "Retentativas Lambda por timeout de parceiros"
            ],
            Commitments =
            [
                "AWS Savings Plan compute parcial",
                "APIM reserved capacity 1yr"
            ],
            OptimizationOpportunities =
            [
                "Consolidar payloads e compressão nas filas",
                "Circuit breaker para parceiros com timeout alto",
                "Revisar duplicação de mensagens entre clouds"
            ]
        },
        Inventory =
        [
            new() { Service = "Amazon SQS", ResourceDescription = "Filas de eventos parceiros", QuantityOrSku = "42 filas standard + 8 DLQ", Environment = "production" },
            new() { Service = "AWS Lambda", ResourceDescription = "Transformação/roteamento", QuantityOrSku = "67 funções", Environment = "production" },
            new() { Service = "Amazon API Gateway", ResourceDescription = "APIs B2B externas", QuantityOrSku = "2 REST APIs", Environment = "production" },
            new() { Service = "Azure Service Bus", ResourceDescription = "Backbone ERP/TMS", QuantityOrSku = "6 namespaces Premium", Environment = "production" },
            new() { Service = "Azure Functions", ResourceDescription = "Adaptadores SAP", QuantityOrSku = "34 functions (Premium plan)", Environment = "production" },
            new() { Service = "Azure API Management", ResourceDescription = "Gateway interno/parceiros", QuantityOrSku = "Premium tier", Environment = "production" }
        ],
        Operations = new ProfileOperationalReality
        {
            DeploymentFrequency = "5–8 deploys/semana por domínio; contract tests obrigatórios",
            OncallModel = "Integração 24x7 com runbooks por parceiro",
            CriticalSlas = ["Entrega de status < 5 min end-to-end", "disponibilidade APIs B2B 99,9%"],
            ChangeWindow = "Qui 23h–05h UTC; parceiros críticos notificados 72h antes",
            IncidentSensitivity = "Sev-1 se fila principal > 15 min ou perda de mensagens"
        },
        Compliance = new ProfileCompliance
        {
            Frameworks = ["SOC 2", "ISO 27001", "LGPD", "contratos SLA com parceiros"],
            DataClassification = "Dados de remessa e localização; sem dados de cartão",
            IdentityModel = "OAuth2 client credentials para parceiros; managed identities Azure",
            NetworkPosture = ["VPC privada AWS", "VNet integrada ExpressRoute", "IP allowlist parceiros"]
        },
        Sensitivity = new ProfileSensitivity
        {
            Financial = "alta — data transfer cross-cloud é 10,6% e crescendo",
            Technical = "crítica — breaking changes em APIs/filas",
            Operational = "crítica — filas e DLQ são centro do negócio",
            SecurityCompliance = "alta — credenciais de parceiros e tráfego B2B"
        }
    };
}
