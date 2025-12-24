using System.ComponentModel.DataAnnotations;

namespace NeuronetAdmin.Models.ViewModels;

public class CounselorApplicationViewModel
{
    [Required]
    [Display(Name = "Full Name")]
    public string CounselorName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Professional Affiliation / License ID")]
    public string ProfessionalAffiliation { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Institutional Email")]
    public string InstitutionalEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
