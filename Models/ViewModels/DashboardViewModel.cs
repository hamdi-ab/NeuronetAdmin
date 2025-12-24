using NeuronetAdmin.Models;

namespace NeuronetAdmin.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int PendingVerifications { get; set; }
    public int VerifiedCounselors { get; set; }
    public List<AuditLog> RecentActivity { get; set; } = new();
}
