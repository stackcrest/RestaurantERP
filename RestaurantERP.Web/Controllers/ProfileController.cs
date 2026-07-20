using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;
using RestaurantERP.Web.Services;

namespace RestaurantERP.Web.Controllers;

[Authorize]
public class ProfileController : BaseController
{
    private readonly IUploadStorageService _uploads;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IUploadStorageService uploads,
        ILogger<ProfileController> logger)
        : base(userManager, context)
    {
        _uploads = uploads;
        _logger = logger;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return View("Guest");

        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = roles;
        ViewBag.OrderCount = await _context.Orders.CountAsync(o => o.UserId == user.Id);
        ViewBag.CompletedOrders = await _context.Orders.CountAsync(o =>
            o.UserId == user.Id && (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered));
        ViewBag.MemberSince = user.CreatedAt;
        return View(user);
    }

    public async Task<IActionResult> Edit()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Edit(
        string fullName, string? phoneNumber, string? address, string? city, string? pinCode,
        IFormFile? avatar, bool removeAvatar = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(fullName))
        {
            TempData["Error"] = "Full name is required.";
            return View(user);
        }

        var oldAvatarUrl = user.AvatarUrl;

        try
        {
            if (removeAvatar)
            {
                user.AvatarUrl = null;
            }
            else if (avatar != null && avatar.Length > 0)
            {
                var (uploaded, error) = await _uploads.SaveImageAsync(avatar, "avatars");
                if (uploaded == null)
                {
                    TempData["Error"] = error ?? "Could not upload image.";
                    return View(user);
                }
                user.AvatarUrl = uploaded;
            }

            user.FullName = fullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            user.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
            user.City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
            user.PinCode = string.IsNullOrWhiteSpace(pinCode) ? null : pinCode.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return View(user);
            }

            if (removeAvatar || (avatar != null && avatar.Length > 0))
                await _uploads.DeleteIfExistsAsync(oldAvatarUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profile for user {UserId}", user.Id);
            TempData["Error"] = "Could not save profile. Please try again.";
            return View(user);
        }

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Index));
    }
}
