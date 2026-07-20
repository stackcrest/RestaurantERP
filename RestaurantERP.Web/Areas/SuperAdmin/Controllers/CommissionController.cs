using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class CommissionController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly ICommissionService _commissionService;

    public CommissionController(IApplicationDbContext context, ICommissionService commissionService)
    {
        _context = context;
        _commissionService = commissionService;
    }

    public async Task<IActionResult> Index()
    {
        var rules = await _context.CommissionRules.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        ViewBag.TotalEarned = await _commissionService.GetTotalCommissionAsync();
        ViewBag.MonthlyEarned = await _commissionService.GetTotalCommissionAsync(thisMonth, DateTime.UtcNow.Date);
        ViewBag.RecentEntries = await _context.CommissionLedgers
            .Include(l => l.Order)
            .Include(l => l.CommissionRule)
            .OrderByDescending(l => l.CreatedAt)
            .Take(20)
            .ToListAsync();

        return View(rules);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(string name, decimal percentage, int priority)
    {
        var rule = new CommissionRule
        {
            Name = name,
            Percentage = percentage,
            Priority = priority,
            IsActive = true
        };
        _context.CommissionRules.Add(rule);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Commission rule created!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var rule = await _context.CommissionRules.FindAsync(id);
        if (rule != null)
        {
            rule.IsActive = !rule.IsActive;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var rule = await _context.CommissionRules.FindAsync(id);
        if (rule != null)
        {
            rule.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Rule deleted.";
        }
        return RedirectToAction("Index");
    }
}
