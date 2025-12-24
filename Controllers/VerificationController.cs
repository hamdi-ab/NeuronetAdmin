using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuronetAdmin.Data;
using NeuronetAdmin.Models;
using NeuronetAdmin.Services;

namespace NeuronetAdmin.Controllers;

[Authorize(Roles = "Admin")]
public class VerificationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public VerificationController(
        ApplicationDbContext context, 
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _auditService = auditService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Verification
    public async Task<IActionResult> Index(VerificationStatus? statusFilter)
    {
        var records = _context.VerificationRecords.AsQueryable();

        if (statusFilter.HasValue)
        {
            records = records.Where(r => r.Status == statusFilter.Value);
        }

        ViewBag.StatusFilter = statusFilter;
        return View(await records.OrderByDescending(v => v.RequestDate).ToListAsync());
    }

    // GET: Verification/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var verificationRecord = await _context.VerificationRecords
            .FirstOrDefaultAsync(m => m.Id == id);
        if (verificationRecord == null) return NotFound();

        return View(verificationRecord);
    }

    // GET: Verification/Create (Admin: Auto-approves)
    public IActionResult Create()
    {
        return View();
    }

    // POST: Verification/Create (Admin: Auto-approves)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CounselorId,CounselorName,ProfessionalAffiliation,InstitutionalEmail")] VerificationRecord verificationRecord)
    {
        if (ModelState.IsValid)
        {
            verificationRecord.RequestDate = DateTime.UtcNow;
            verificationRecord.Status = VerificationStatus.Verified; // Auto-approve for Admin
            _context.Add(verificationRecord);
            await _context.SaveChangesAsync();

            // Auto-create user
            await ApproveAndActivateUser(verificationRecord);
            
            // Check if user was linked successfully
            if (!string.IsNullOrEmpty(verificationRecord.CounselorId))
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Counselor created, auto-approved, and user account generated (Password: Counselor123!).";
            }
            else
            {
                 // In a real app we would bubble up errors from ApproveAndActivateUser, but for now generic error
                TempData["ErrorMessage"] = "Counselor verified but user creation failed.";
            }

            return RedirectToAction(nameof(Index));
        }
        return View(verificationRecord);
    }

    // GET: Verification/Apply (Public)
    [AllowAnonymous]
    public IActionResult Apply()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
        {
             return RedirectToAction(nameof(Index));
        }
        return View();
    }

    // POST: Verification/Apply (Public)
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(NeuronetAdmin.Models.ViewModels.CounselorApplicationViewModel model)
    {
        if (ModelState.IsValid)
        {
            // 1. Create User (Inactive)
            var user = new ApplicationUser
            {
                UserName = model.InstitutionalEmail,
                Email = model.InstitutionalEmail,
                FullName = model.CounselorName,
                IsActive = false, // Locked until approved
                EmailConfirmed = true 
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign Role
                if (!await _roleManager.RoleExistsAsync("Counselor"))
                {
                     await _roleManager.CreateAsync(new IdentityRole("Counselor"));
                }
                await _userManager.AddToRoleAsync(user, "Counselor");

                // 2. Create Verification Record linked to User
                var verificationRecord = new VerificationRecord
                {
                    CounselorId = user.Id,
                    CounselorName = model.CounselorName,
                    ProfessionalAffiliation = model.ProfessionalAffiliation,
                    InstitutionalEmail = model.InstitutionalEmail,
                    Status = VerificationStatus.Pending,
                    RequestDate = DateTime.UtcNow
                };

                _context.Add(verificationRecord);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ApplySuccess));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult ApplySuccess()
    {
        return View();
    }

    // POST: Verification/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var record = await _context.VerificationRecords.FindAsync(id);
        if (record != null)
        {
            // Activate existing user or create if added by Admin
            await ApproveAndActivateUser(record);
            
            record.Status = VerificationStatus.Verified;
            _context.Update(record);
            await _context.SaveChangesAsync();

            // LOGGING
            await _auditService.LogAsync("VERIFICATION_APPROVED", User.Identity?.Name ?? "Unknown", $"Approved counselor {record.CounselorName}.");
            TempData["SuccessMessage"] = "Counselor approved and account activated.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ApproveAndActivateUser(VerificationRecord record)
    {
        var user = await _userManager.FindByEmailAsync(record.InstitutionalEmail);
        
        // Scenario A: User registered via Apply form (Exists, Inactive)
        if (user != null)
        {
            if (!user.IsActive)
            {
                user.IsActive = true;
                await _userManager.UpdateAsync(user);
            }
            
            // Ensure ID linkage if missing
            if (string.IsNullOrEmpty(record.CounselorId)) 
            {
                record.CounselorId = user.Id;
            }
            return;
        }

        // Scenario B: Admin created request manually (User doesn't exist yet) -> Create with default password
        // Logic from before
        user = new ApplicationUser
        {
            UserName = record.InstitutionalEmail,
            Email = record.InstitutionalEmail,
            FullName = record.CounselorName,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, "Counselor123!");
        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync("Counselor")) await _roleManager.CreateAsync(new IdentityRole("Counselor"));
            await _userManager.AddToRoleAsync(user, "Counselor");
            record.CounselorId = user.Id;
        }
    }

    // GET: Verification/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var record = await _context.VerificationRecords.FindAsync(id);
        if (record == null) return NotFound();
        return View(record);
    }

    // POST: Verification/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,CounselorId,CounselorName,ProfessionalAffiliation,InstitutionalEmail,Status,RequestDate")] VerificationRecord record)
    {
        if (id != record.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(record);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Verification record updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VerificationRecordExists(record.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(record);
    }

    private bool VerificationRecordExists(int id)
    {
        return _context.VerificationRecords.Any(e => e.Id == id);
    }

    // POST: Verification/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var record = await _context.VerificationRecords.FindAsync(id);
        if (record != null)
        {
            record.Status = VerificationStatus.Rejected;
            _context.Update(record);
            await _context.SaveChangesAsync();

            // LOGGING
            await _auditService.LogAsync("VERIFICATION_REJECTED", User.Identity?.Name ?? "Unknown", $"Rejected counselor {record.CounselorName}");
            TempData["SuccessMessage"] = "Counselor application rejected.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Verification/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var record = await _context.VerificationRecords
            .FirstOrDefaultAsync(m => m.Id == id);
        if (record == null) return NotFound();

        return View(record);
    }

    // POST: Verification/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var record = await _context.VerificationRecords.FindAsync(id);
        if (record != null)
        {
            _context.VerificationRecords.Remove(record);
            await _context.SaveChangesAsync();
            
            // LOGGING
            await _auditService.LogAsync("VERIFICATION_DELETED", User.Identity?.Name ?? "Unknown", $"Deleted verification request for {record.CounselorName}");
            TempData["SuccessMessage"] = "Record deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }
}
