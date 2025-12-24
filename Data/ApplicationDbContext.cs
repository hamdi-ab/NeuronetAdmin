using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NeuronetAdmin.Models;

namespace NeuronetAdmin.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<VerificationRecord> VerificationRecords { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
}
