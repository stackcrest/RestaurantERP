using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Web.Models;

namespace RestaurantERP.Web.Controllers;

public partial class HomeController : BaseController
{
    private readonly IContactInquiryService _contactService;

    public HomeController(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IContactInquiryService contactService)
        : base(userManager, context)
    {
        _contactService = contactService;
    }
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Take(10)
            .ToListAsync();

        var featured = await _context.MenuItems
            .Include(m => m.Category)
            .Include(m => m.Variants.Where(v => v.IsActive))
            .Where(m => m.IsFeatured && m.IsAvailable)
            .Take(8)
            .ToListAsync();

        var theme = await _context.ThemeSettings
            .Where(t => t.IsPublished)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync();

        ViewBag.Categories = categories;
        ViewBag.Featured = featured;
        ViewBag.Theme = theme;
        ViewBag.CartQuantities = await GetCartQuantitiesAsync();
        ViewBag.ItemsWithOptions = await GetMenuItemsWithOptionsAsync();
        // ActiveOffer is also loaded by SiteDataFilter for layout; ensure available on home
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contact() => View(new ContactFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _contactService.SubmitAsync(model.Name, model.Email, model.Phone, model.Subject, model.Message, ip);
            TempData["Success"] = "Thank you! Your message has been sent. We'll get back to you soon.";
            return RedirectToAction(nameof(Contact));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
