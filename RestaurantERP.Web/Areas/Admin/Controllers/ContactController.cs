using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ContactController : Controller
{
    private readonly IContactInquiryService _contactService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContactController(IContactInquiryService contactService, UserManager<ApplicationUser> userManager)
    {
        _contactService = contactService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? status)
    {
        ContactInquiryStatus? filter = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContactInquiryStatus>(status, out var parsed))
            filter = parsed;

        ViewBag.StatusCounts = await _contactService.GetStatusCountsAsync();
        ViewBag.CurrentStatus = status;
        var inquiries = await _contactService.GetInquiriesAsync(filter);
        return View(inquiries);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var inquiry = await _contactService.GetByIdAsync(id);
        if (inquiry == null) return NotFound();
        return View(inquiry);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, ContactInquiryStatus status, string? adminNotes)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            await _contactService.UpdateAsync(id, status, adminNotes, user?.Id);
            TempData["Success"] = "Contact inquiry updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _contactService.DeleteAsync(id);
            TempData["Success"] = "Contact inquiry deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
