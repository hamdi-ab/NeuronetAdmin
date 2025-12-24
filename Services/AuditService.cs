using Microsoft.EntityFrameworkCore;
using NeuronetAdmin.Data;
using NeuronetAdmin.Models;

namespace NeuronetAdmin.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action, string performedBy, string details)
    {
        var log = new AuditLog
        {
            Action = action,
            PerformedBy = performedBy,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 10)
    {
        return await _context.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
