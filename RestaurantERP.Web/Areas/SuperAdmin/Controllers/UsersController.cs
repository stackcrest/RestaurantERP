using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Web.Extensions;
using RestaurantERP.Web.Models;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(string? role)
    {
        var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var userRoles = new Dictionary<Guid, IList<string>>();

        foreach (var user in users)
            userRoles[user.Id] = await _userManager.GetRolesAsync(user);

        if (!string.IsNullOrEmpty(role))
            users = users.Where(u => userRoles[u.Id].Contains(role)).ToList();

        ViewBag.UserRoles = userRoles;
        ViewBag.CurrentRole = role;
        ViewBag.Roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        ViewBag.Roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

        if (!ModelState.IsValid)
            return View(model);

        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Invalid role selected.");
            return View(model);
        }

        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            EmailConfirmed = true,
            IsActive = model.IsActive
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        TempData["Success"] = $"User '{user.FullName}' created successfully.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return this.ItemNotFound("User not found or may have been removed.");

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            City = user.City,
            PinCode = user.PinCode,
            LoyaltyPoints = user.LoyaltyPoints,
            IsActive = user.IsActive
        };

        ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null)
            return this.ItemNotFound("User not found or may have been removed.");

        ViewBag.UserRoles = await _userManager.GetRolesAsync(user);

        if (!ModelState.IsValid)
            return View(model);

        var emailChanged = !string.Equals(user.Email, model.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var existing = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (existing != null && existing.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already used by another account.");
                return View(model);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pwdResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!pwdResult.Succeeded)
            {
                foreach (var error in pwdResult.Errors)
                    ModelState.AddModelError(nameof(model.NewPassword), error.Description);
                return View(model);
            }
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        user.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        user.PinCode = string.IsNullOrWhiteSpace(model.PinCode) ? null : model.PinCode.Trim();
        user.LoyaltyPoints = model.LoyaltyPoints;
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return this.ItemNotFound("User not found or may have been removed.");
        ViewBag.UserRoles = await _userManager.GetRolesAsync(user);
        ViewBag.AllRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRole(Guid userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null && !await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
            TempData["Success"] = $"Role {role} added.";
        }
        return RedirectToAction("Details", new { id = userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(Guid userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return RedirectToAction(nameof(Index));

        if (role == "SuperAdmin")
        {
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            if (superAdmins.Count <= 1 && await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                TempData["Error"] = "Cannot remove the last SuperAdmin account.";
                return RedirectToAction("Details", new { id = userId });
            }
        }

        if (await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.RemoveFromRoleAsync(user, role);
            TempData["Success"] = $"Role {role} removed.";
        }
        return RedirectToAction("Details", new { id = userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                await _userManager.SetLockoutEndDateAsync(user, null);
            else
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            TempData["Success"] = "User lock status updated.";
        }
        return RedirectToAction("Details", new { id });
    }
}
