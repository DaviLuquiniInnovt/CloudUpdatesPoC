using CloudUpdatesPoC.Models;

namespace CloudUpdatesPoC.Services;

public interface IUpdateStorage
{
    Task<bool> ExistsAsync(CloudUpdate update, CancellationToken ct);
    Task SaveAsync(CloudUpdate update, CancellationToken ct);
}