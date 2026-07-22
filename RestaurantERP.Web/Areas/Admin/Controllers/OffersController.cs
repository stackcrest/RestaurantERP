using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class OffersController : Controller
{
    private readonly ICouponService _offers;

    public OffersController(ICouponService offers) => _offers = offers;

    public async Task<IActionResult> Index()
    {
        var result = await _offers.GetCouponsAsync();
        return View(result.Data ?? new List<CouponDto>());
    }

    public IActionResult Create()
    {
        ViewBag.ValidFrom = DateTime.Today;
        ViewBag.ValidTo = DateTime.Today.AddMonths(3);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string code, string? title, string? description, string? subtitle, string? badgeText, string? ctaText,
        decimal discountPercentage, decimal? maxDiscountAmount, decimal? minimumOrderAmount,
        DateTime validFrom, DateTime validTo, int? usageLimit, int priority,
        bool showOnHomepage, bool firstOrderOnly, bool isActive)
    {
        var result = await _offers.CreateCouponAsync(new CreateCouponDto
        {
            Code = code,
            Title = title,
            Description = description,
            Subtitle = subtitle,
            BadgeText = badgeText,
            CtaText = ctaText,
            DiscountPercentage = discountPercentage,
            MaxDiscountAmount = maxDiscountAmount,
            MinimumOrderAmount = minimumOrderAmount,
            ValidFrom = validFrom,
            ValidTo = validTo.Date.AddDays(1).AddSeconds(-1),
            UsageLimit = usageLimit,
            Priority = priority,
            ShowOnHomepage = showOnHomepage,
            FirstOrderOnly = firstOrderOnly,
            IsActive = isActive
        });

        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Offer created!" : result.Message;
        if (!result.Success)
        {
            ViewBag.ValidFrom = validFrom;
            ViewBag.ValidTo = validTo;
            return View();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _offers.GetByIdAsync(id);
        if (!result.Success || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id, string code, string? title, string? description, string? subtitle, string? badgeText, string? ctaText,
        decimal discountPercentage, decimal? maxDiscountAmount, decimal? minimumOrderAmount,
        DateTime validFrom, DateTime validTo, int? usageLimit, int priority,
        bool showOnHomepage, bool firstOrderOnly, bool isActive)
    {
        var result = await _offers.UpdateCouponAsync(id, new CreateCouponDto
        {
            Code = code,
            Title = title,
            Description = description,
            Subtitle = subtitle,
            BadgeText = badgeText,
            CtaText = ctaText,
            DiscountPercentage = discountPercentage,
            MaxDiscountAmount = maxDiscountAmount,
            MinimumOrderAmount = minimumOrderAmount,
            ValidFrom = validFrom,
            ValidTo = validTo.Date.AddDays(1).AddSeconds(-1),
            UsageLimit = usageLimit,
            Priority = priority,
            ShowOnHomepage = showOnHomepage,
            FirstOrderOnly = firstOrderOnly,
            IsActive = isActive
        });

        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Offer updated!" : result.Message;
        if (!result.Success) return RedirectToAction(nameof(Edit), new { id });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var result = await _offers.ToggleActiveAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _offers.DeleteCouponAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
