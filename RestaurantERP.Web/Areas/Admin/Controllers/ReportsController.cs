using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ReportsController : Controller
{
    private readonly IApplicationDbContext _context;

    public ReportsController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? tab, DateTime? from, DateTime? to)
    {
        var toDate = (to ?? DateTime.UtcNow.Date).Date;
        var fromDate = (from ?? toDate.AddDays(-29)).Date;
        if (fromDate > toDate)
            (fromDate, toDate) = (toDate, fromDate);

        var rangeEnd = toDate.AddDays(1);

        ViewBag.Tab = tab ?? "sales";
        ViewBag.From = fromDate.ToString("yyyy-MM-dd");
        ViewBag.To = toDate.ToString("yyyy-MM-dd");

        var orders = await _context.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt < rangeEnd && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        ViewBag.TotalSales = orders.Sum(o => o.TotalAmount);
        ViewBag.OrderCount = orders.Count;
        ViewBag.AvgOrderValue = orders.Count > 0 ? orders.Average(o => o.TotalAmount) : 0m;
        ViewBag.TotalTax = orders.Sum(o => o.TaxAmount);
        ViewBag.TotalDiscount = orders.Sum(o => o.DiscountAmount);
        ViewBag.DeliveryFees = orders.Sum(o => o.DeliveryFee);

        var dayCount = (toDate - fromDate).Days + 1;
        var days = Enumerable.Range(0, dayCount).Select(i => fromDate.AddDays(i)).ToList();
        var salesByDay = orders
            .GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

        ViewBag.ChartLabels = days.Select(d => dayCount <= 7 ? d.ToString("ddd") : d.ToString("dd MMM")).ToList();
        ViewBag.ChartData = days.Select(d => salesByDay.GetValueOrDefault(d, 0m)).ToList();

        ViewBag.OrdersByType = Enum.GetValues<OrderType>()
            .Select(t => new { Label = t.ToString(), Count = orders.Count(o => o.OrderType == t), Total = orders.Where(o => o.OrderType == t).Sum(o => o.TotalAmount) })
            .Where(x => x.Count > 0)
            .ToList();

        ViewBag.OrdersByStatus = Enum.GetValues<OrderStatus>()
            .Select(s => new { Label = s.ToString(), Count = orders.Count(o => o.Status == s) })
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .ToList();

        ViewBag.OrdersBySource = Enum.GetValues<OrderSource>()
            .Select(s => new { Label = s.ToString(), Count = orders.Count(o => o.OrderSource == s), Total = orders.Where(o => o.OrderSource == s).Sum(o => o.TotalAmount) })
            .Where(x => x.Count > 0)
            .ToList();

        var orderIds = orders.Select(o => o.Id).ToList();
        ViewBag.TopMenuItems = await _context.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .GroupBy(oi => oi.MenuItemName)
            .Select(g => new { Name = g.Key, Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.TotalPrice) })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync();

        ViewBag.PaymentsByMethod = Enum.GetValues<PaymentMethod>()
            .Select(m => new { Label = m.ToString(), Count = orders.Count(o => o.PaymentMethod == m), Total = orders.Where(o => o.PaymentMethod == m).Sum(o => o.TotalAmount) })
            .Where(x => x.Count > 0)
            .ToList();

        ViewBag.PaymentsByStatus = Enum.GetValues<PaymentStatus>()
            .Select(s => new { Label = s.ToString(), Count = orders.Count(o => o.PaymentStatus == s) })
            .Where(x => x.Count > 0)
            .ToList();

        var bills = await _context.Bills
            .Where(b => b.CreatedAt >= fromDate && b.CreatedAt < rangeEnd)
            .ToListAsync();

        ViewBag.BillCount = bills.Count;
        ViewBag.BillTotal = bills.Sum(b => b.TotalAmount);
        ViewBag.PosBillCount = bills.Count(b => b.Source == BillSource.Offline);
        ViewBag.OnlineBillCount = bills.Count(b => b.Source == BillSource.Online);

        ViewBag.LowStockItems = await _context.Ingredients
            .Where(i => i.CurrentStock <= i.LowStockThreshold)
            .OrderBy(i => i.CurrentStock)
            .Take(10)
            .ToListAsync();

        return View();
    }
}
