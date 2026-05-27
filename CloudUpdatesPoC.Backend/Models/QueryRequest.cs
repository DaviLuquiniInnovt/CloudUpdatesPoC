namespace CloudUpdatesPoC.Models;

public class QueryRequest
{
    public string? ProfileId { get; set; }
    public List<string> Services { get; set; } = new();
    public string Question { get; set; } = "";
}