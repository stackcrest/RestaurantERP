using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class OrderTrackingService : IOrderTrackingService
{
    private readonly IApplicationDbContext _context;

    public OrderTrackingService(IApplicationDbContext context) => _context = context;

    public async Task RecordStatusAsync(Guid orderId, OrderStatus status, Guid? userId, string? userName, string? note = null)
    {
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            Status = status,
            Note = note,
            UpdatedByUserId = userId,
            UpdatedByName = userName
        });
        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message)> UpdateStatusAsync(
        Guid orderId, OrderStatus newStatus, Guid? userId, string? userName, string? note = null)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return (false, "Order not found.");

        if (!OrderStatusHelper.CanTransition(order.OrderType, order.Status, newStatus))
            return (false, $"Cannot change status from {order.Status} to {newStatus}.");

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            Status = newStatus,
            Note = note,
            UpdatedByUserId = userId,
            UpdatedByName = userName
        });

        await _context.SaveChangesAsync();
        return (true, $"Order status updated to {OrderStatusHelper.GetLabel(newStatus)}.");
    }

    public async Task<List<OrderTrackingStep>> GetTimelineAsync(Guid orderId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return [];

        var history = await _context.OrderStatusHistories
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

        if (!history.Any())
        {
            history =
            [
                new OrderStatusHistory
                {
                    Status = order.Status,
                    CreatedAt = order.UpdatedAt > order.CreatedAt ? order.UpdatedAt : order.CreatedAt
                }
            ];
        }

        var steps = OrderStatusHelper.GetTimelineSteps(order.OrderType);
        var historyByStatus = history.GroupBy(h => h.Status).ToDictionary(g => g.Key, g => g.Last());

        if (order.Status == OrderStatus.Cancelled)
        {
            var cancelled = history.LastOrDefault(h => h.Status == OrderStatus.Cancelled);
            return
            [
                new OrderTrackingStep
                {
                    Status = OrderStatus.Cancelled,
                    Label = OrderStatusHelper.GetLabel(OrderStatus.Cancelled),
                    IsCompleted = true,
                    IsCurrent = true,
                    Timestamp = cancelled?.CreatedAt ?? order.UpdatedAt,
                    UpdatedBy = cancelled?.UpdatedByName,
                    Note = cancelled?.Note
                }
            ];
        }

        var currentIdx = steps.ToList().IndexOf(order.Status);

        return steps.Select((status, idx) =>
        {
            historyByStatus.TryGetValue(status, out var entry);
            return new OrderTrackingStep
            {
                Status = status,
                Label = OrderStatusHelper.GetLabel(status),
                IsCompleted = idx <= currentIdx,
                IsCurrent = idx == currentIdx,
                Timestamp = entry?.CreatedAt,
                UpdatedBy = entry?.UpdatedByName,
                Note = entry?.Note
            };
        }).ToList();
    }

    public async Task BackfillMissingHistoryAsync()
    {
        var orderIdsWithHistory = await _context.OrderStatusHistories.Select(h => h.OrderId).Distinct().ToListAsync();
        var ordersWithoutHistory = await _context.Orders
            .Where(o => !orderIdsWithHistory.Contains(o.Id))
            .ToListAsync();

        foreach (var order in ordersWithoutHistory)
        {
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                CreatedAt = order.UpdatedAt > order.CreatedAt ? order.UpdatedAt : order.CreatedAt,
                UpdatedByName = "System"
            });
        }

        if (ordersWithoutHistory.Any())
            await _context.SaveChangesAsync();
    }
}
