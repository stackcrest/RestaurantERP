using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class DashboardController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICommissionService _commissionService;

    public DashboardController(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICommissionService commissionService)
    {
        _context = context;
        _userManager = userManager;
        _commissionService = commissionService;
    }

    public async Task<IActionResult> Index()
    {
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.TotalOrders = await _context.Orders.CountAsync();
        ViewBag.TotalRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount);
        
        ViewBag.MonthlyRevenue = await _context.Orders
            .Where(o => o.CreatedAt >= thisMonth && o.Status == OrderStatus.Completed)
            .SumAsync(o => o.TotalAmount);

        ViewBag.TotalCommission = await _commissionService.GetTotalCommissionAsync();
        ViewBag.MonthlyCommission = await _commissionService.GetTotalCommissionAsync(thisMonth, DateTime.UtcNow.Date);
        ViewBag.ActiveFeatures = await _context.FeatureFlags.CountAsync(f => f.IsEnabled);
        ViewBag.TotalPages = await _context.UiPages.CountAsync();

        ViewBag.RecentLogs = await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        return View();
    }
}
