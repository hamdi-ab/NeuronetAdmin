using System.ComponentModel.DataAnnotations;

namespace NeuronetAdmin.Models;

public class VerificationRecord
{
    public int Id { get; set; }

    [Required]
    public string CounselorId { get; set; } = string.Empty; // Links to ApplicationUser.Id

    // Navigation property if you want FK constraint:
    // public ApplicationUser Counselor { get; set; }

    [Required]
    public string CounselorName { get; set; } = string.Empty; // Snapshot or link

    [Required]
    public string ProfessionalAffiliation { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string InstitutionalEmail { get; set; } = string.Empty;

    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
}

public enum VerificationStatus
{
    Pending,
    Verified,
    Rejected
}
