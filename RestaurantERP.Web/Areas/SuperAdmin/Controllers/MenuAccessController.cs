using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class MenuAccessController : Controller
{
    private readonly IPermissionService _permissionService;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public MenuAccessController(IPermissionService permissionService, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _permissionService = permissionService;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(string? role)
    {
        var selectedRole = role ?? AdminMenuSections.ManageableRoles[0];
        ViewBag.SelectedRole = selectedRole;
        ViewBag.Roles = await _roleManager.Roles
            .Where(r => AdminMenuSections.ManageableRoles.Contains(r.Name!))
            .OrderBy(r => r.Name)
            .ToListAsync();
        ViewBag.Sections = AdminMenuSections.All;
        ViewBag.Access = await _permissionService.GetRoleSectionAccessAsync(selectedRole);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Save(string roleName)
    {
        if (!AdminMenuSections.ManageableRoles.Contains(roleName))
        {
            TempData["Error"] = "Invalid role selected.";
            return RedirectToAction(nameof(Index));
        }

        var access = new Dictionary<string, (bool CanRead, bool CanWrite)>();

        foreach (var section in AdminMenuSections.All)
        {
            var canRead = Request.Form[$"read_{section.Key}"].ToString() == "on";
            var canWrite = Request.Form[$"write_{section.Key}"].ToString() == "on";
            if (canWrite) canRead = true;
            access[section.Key] = (canRead, canWrite);
        }

        await _permissionService.SaveRoleSectionAccessAsync(roleName, access);
        TempData["Success"] = $"Menu access updated for {roleName} role.";
        return RedirectToAction(nameof(Index), new { role = roleName });
    }
}
