using NeuronetAdmin.Models;

namespace NeuronetAdmin.Services;

public interface IAuditService
{
    Task LogAsync(string action, string performedBy, string details);
    Task<List<AuditLog>> GetRecentLogsAsync(int count = 10);
}
