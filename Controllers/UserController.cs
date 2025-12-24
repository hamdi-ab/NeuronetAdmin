using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NeuronetAdmin.Models;
using NeuronetAdmin.Models.ViewModels;
using NeuronetAdmin.Services;

namespace NeuronetAdmin.Controllers;

[Authorize(Roles = "Admin")] // Ensure only Admin can access
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    // GET: user
    public async Task<IActionResult> Index(string? searchString, string? roleFilter)
    {
        var usersQuery = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            usersQuery = usersQuery.Where(u => u.Email.Contains(searchString) || u.FullName.Contains(searchString));
        }

        var users = await usersQuery.ToListAsync();
        var userViewModels = new List<UserListViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "None";

            if (!string.IsNullOrEmpty(roleFilter) && role != roleFilter)
            {
                continue;
            }

            userViewModels.Add(new UserListViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                IsActive = user.IsActive,
                Role = role
            });
        }

        ViewBag.SearchString = searchString;
        ViewBag.RoleFilter = roleFilter;
        ViewBag.Roles = new SelectList(new[] { "Admin", "Counselor", "Guardian", "Adolescent" });

        return View(userViewModels);
    }

    // GET: user/create
    public IActionResult Create()
    {
        ViewBag.Roles = new SelectList(new[] { "Admin", "Counselor", "Guardian", "Adolescent" });
        return View();
    }

    // POST: user/create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = true,
                EmailConfirmed = true // Auto-confirm for admin created users
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Ensure role exists
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                await _userManager.AddToRoleAsync(user, model.Role);
                
                // LOGGING
                await _auditService.LogAsync("USER_CREATED", User.Identity?.Name ?? "Unknown", $"Created user {user.Email} as {model.Role}");

                TempData["SuccessMessage"] = $"User {user.Email} created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.Roles = new SelectList(new[] { "Admin", "Counselor", "Guardian", "Adolescent" }, model.Role);
        return View(model);
    }

    // GET: user/edit/id
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault() ?? ""
        };

        ViewBag.Roles = new SelectList(new[] { "Admin", "Counselor", "Guardian", "Adolescent" }, model.Role);
        return View(model);
    }

    // POST: user/edit/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email; // Keep synonymous
            user.FullName = model.FullName;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                // Update Role
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentRole = currentRoles.FirstOrDefault();

                if (currentRole != model.Role)
                {
                    if (currentRole != null)
                        await _userManager.RemoveFromRoleAsync(user, currentRole);
                    
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));

                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // LOGGING
                await _auditService.LogAsync("USER_UPDATED", User.Identity?.Name ?? "Unknown", $"Updated profile for {user.Email}. Role: {model.Role}, Active: {model.IsActive}");

                TempData["SuccessMessage"] = "User details updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.Roles = new SelectList(new[] { "Admin", "Counselor", "Guardian", "Adolescent" }, model.Role);
        return View(model);
    }

    // GET: user/delete/id
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();

        // Prevent self-deletion
        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        return View(user);
    }

    // POST: user/delete/id
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        // Prevent self-deletion
        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            // Or soft delete:
            // user.IsActive = false;
            // await _userManager.UpdateAsync(user);
            
            // Hard delete per CRUD requirements:
             await _userManager.DeleteAsync(user);

             // LOGGING
             await _auditService.LogAsync("USER_DELETED", User.Identity?.Name ?? "Unknown", $"Deleted account {user.Email}");

             TempData["SuccessMessage"] = "User deleted successfully.";
        }
        else 
        {
            TempData["ErrorMessage"] = "Error: User not found.";
        }

        return RedirectToAction(nameof(Index));
    }
}
