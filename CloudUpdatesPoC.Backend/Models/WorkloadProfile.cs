namespace CloudUpdatesPoC.Models;

public class WorkloadProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Services { get; set; } = new();
    public string BusinessContext { get; set; } = "";
    public string ArchitectureNotes { get; set; } = "";
    public List<string> Priorities { get; set; } = new();
    public ProfileSensitivity Sensitivity { get; set; } = new();
    public ProfileOrganization Organization { get; set; } = new();
    public List<ProfileCloudAccount> Accounts { get; set; } = new();
    public ProfileFinancials Financials { get; set; } = new();
    public List<ProfileResourceInventory> Inventory { get; set; } = new();
    public ProfileOperationalReality Operations { get; set; } = new();
    public ProfileCompliance Compliance { get; set; } = new();
}

public class ProfileOrganization
{
    public string CompanyName { get; set; } = "";
    public string Industry { get; set; } = "";
    public int EmployeeCount { get; set; }
    public string ProductDescription { get; set; } = "";
}

public class ProfileCloudAccount
{
    public string Provider { get; set; } = "";
    public string AccountLabel { get; set; } = "";
    public string AccountId { get; set; } = "";
    public List<string> PrimaryRegions { get; set; } = new();
    public List<string> Environments { get; set; } = new();
}

public class ProfileCostItem
{
    public string Service { get; set; } = "";
    public decimal MonthlyUsd { get; set; }
    public decimal PercentOfTotal { get; set; }
    public string Notes { get; set; } = "";
}

public class ProfileFinancials
{
    public decimal MonthlySpendUsd { get; set; }
    public decimal AnnualRunRateUsd { get; set; }
    public decimal MonthlyBudgetUsd { get; set; }
    public decimal BudgetUtilizationPercent { get; set; }
    public string BillingModel { get; set; } = "";
    public string CostTrend { get; set; } = "";
    public List<ProfileCostItem> TopCostServices { get; set; } = new();
    public List<string> CostDrivers { get; set; } = new();
    public List<string> Commitments { get; set; } = new();
    public List<string> OptimizationOpportunities { get; set; } = new();
}

public class ProfileResourceInventory
{
    public string Service { get; set; } = "";
    public string ResourceDescription { get; set; } = "";
    public string QuantityOrSku { get; set; } = "";
    public string Environment { get; set; } = "";
}

public class ProfileOperationalReality
{
    public string DeploymentFrequency { get; set; } = "";
    public string OncallModel { get; set; } = "";
    public List<string> CriticalSlas { get; set; } = new();
    public string ChangeWindow { get; set; } = "";
    public string IncidentSensitivity { get; set; } = "";
}

public class ProfileCompliance
{
    public List<string> Frameworks { get; set; } = new();
    public string DataClassification { get; set; } = "";
    public string IdentityModel { get; set; } = "";
    public List<string> NetworkPosture { get; set; } = new();
}

public class ProfileSensitivity
{
    public string Financial { get; set; } = "";
    public string Technical { get; set; } = "";
    public string Operational { get; set; } = "";
    public string SecurityCompliance { get; set; } = "";
}

public class WorkloadProfileSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Services { get; set; } = new();
    public WorkloadProfileSnapshot Snapshot { get; set; } = new();
}

public class WorkloadProfileSnapshot
{
    public string CompanyName { get; set; } = "";
    public string Industry { get; set; } = "";
    public decimal MonthlySpendUsd { get; set; }
    public decimal MonthlyBudgetUsd { get; set; }
    public decimal BudgetUtilizationPercent { get; set; }
    public List<string> AccountLabels { get; set; } = new();
    public List<string> PrimaryRegions { get; set; } = new();
    public List<ProfileCostItem> TopCostServices { get; set; } = new();
    public List<string> InventoryHighlights { get; set; } = new();
    public List<string> ComplianceFrameworks { get; set; } = new();
}
