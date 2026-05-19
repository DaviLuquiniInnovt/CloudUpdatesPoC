namespace CloudUpdatesPoC.Models;

public class QueryRequest
{
    public List<string> Services { get; set; } = new();
    public string Question { get; set; } = "";
}