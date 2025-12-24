using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuronetAdmin.Data;
using NeuronetAdmin.Models;
using NeuronetAdmin.Models.ViewModels;
using NeuronetAdmin.Services;

namespace NeuronetAdmin.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IAuditService auditService)
    {
        _logger = logger;
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return View("Welcome"); // Show simple welcome if not logged in
        }

        var model = new DashboardViewModel
        {
            TotalUsers = await _context.Users.CountAsync(),
            PendingVerifications = await _context.VerificationRecords.CountAsync(v => v.Status == VerificationStatus.Pending),
            VerifiedCounselors = await _context.VerificationRecords.CountAsync(v => v.Status == VerificationStatus.Verified),
            RecentActivity = await _auditService.GetRecentLogsAsync(10)
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
