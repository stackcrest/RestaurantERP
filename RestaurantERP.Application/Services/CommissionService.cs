using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class CommissionService : ICommissionService
{
    private readonly IApplicationDbContext _context;

    public CommissionService(IApplicationDbContext context) => _context = context;

    public async Task ApplyCommissionAsync(Order order)
    {
        if (order.Status == OrderStatus.Cancelled) return;

        var alreadyApplied = await _context.CommissionLedgers.AnyAsync(l => l.OrderId == order.Id);
        if (alreadyApplied) return;

        var rules = await _context.CommissionRules
            .Where(r => r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        var rule = rules.FirstOrDefault(r =>
            (r.OrderType == null || r.OrderType == order.OrderType) &&
            (r.PaymentMethod == null || r.PaymentMethod == order.PaymentMethod));

        if (rule == null) return;

        _context.CommissionLedgers.Add(new CommissionLedger
        {
            OrderId = order.Id,
            CommissionRuleId = rule.Id,
            OrderAmount = order.TotalAmount,
            CommissionPercentage = rule.Percentage,
            CommissionAmount = Math.Round(order.TotalAmount * rule.Percentage / 100, 2)
        });
        await _context.SaveChangesAsync();
    }

    public async Task<decimal> GetTotalCommissionAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.CommissionLedgers.AsQueryable();
        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(l => l.CreatedAt < to.Value.Date.AddDays(1));

        return await query.SumAsync(l => l.CommissionAmount);
    }

    public async Task BackfillMissingCommissionsAsync()
    {
        var commissionedOrderIds = await _context.CommissionLedgers
            .Select(l => l.OrderId)
            .Distinct()
            .ToListAsync();

        var orders = await _context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled && !commissionedOrderIds.Contains(o.Id))
            .ToListAsync();

        foreach (var order in orders)
            await ApplyCommissionAsync(order);
    }
}
