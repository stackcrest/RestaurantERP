using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class DashboardController : Controller
{
    private readonly IApplicationDbContext _context;

    public DashboardController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var todayOrders = await _context.Orders
            .Where(o => o.CreatedAt >= today && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        ViewBag.TodaySales = todayOrders.Sum(o => o.TotalAmount);
        ViewBag.TodayOrders = todayOrders.Count;
        ViewBag.AvgOrderValue = todayOrders.Any() ? todayOrders.Average(o => o.TotalAmount) : 0;
        ViewBag.PendingOrders = await _context.Orders
            .CountAsync(o => o.Status == OrderStatus.Placed || o.Status == OrderStatus.Confirmed);

        ViewBag.LowStock = await _context.Ingredients
            .CountAsync(i => i.CurrentStock <= i.LowStockThreshold);

        var tables = await _context.Tables.ToListAsync();
        ViewBag.ActiveTables = tables.Count(t => t.Status == TableStatus.Occupied);
        ViewBag.TotalTables = tables.Count;

        var recentOrders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .ToListAsync();
        ViewBag.RecentOrders = recentOrders;

        var last7Days = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-6 + i))
            .ToList();

        var salesByDay = await _context.Orders
            .Where(o => o.CreatedAt >= today.AddDays(-6) && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        ViewBag.ChartLabels = last7Days.Select(d => d.ToString("ddd")).ToList();
        ViewBag.ChartData = last7Days.Select(d => salesByDay.FirstOrDefault(s => s.Date == d)?.Total ?? 0).ToList();

        return View();
    }
}
