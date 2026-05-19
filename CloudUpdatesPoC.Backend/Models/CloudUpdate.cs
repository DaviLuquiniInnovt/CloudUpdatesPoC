namespace CloudUpdatesPoC.Models;

public class CloudUpdate
{
    public string Id { get; set; } = "";
    public string Provider { get; set; } = "";   // "AWS" ou "Azure"
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public List<string> Categories { get; set; } = new();
}