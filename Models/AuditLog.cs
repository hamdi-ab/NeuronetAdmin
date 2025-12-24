using System.ComponentModel.DataAnnotations;

namespace NeuronetAdmin.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty; // e.g., "USER_CREATED", "VERIFICATION_APPROVED"

    [Required]
    public string PerformedBy { get; set; } = string.Empty; // Admin who performed the action

    public string Details { get; set; } = string.Empty; // Contextual info

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
