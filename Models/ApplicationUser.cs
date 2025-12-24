using Microsoft.AspNetCore.Identity;

namespace NeuronetAdmin.Models;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Could link to counselor profile here if needed
}
