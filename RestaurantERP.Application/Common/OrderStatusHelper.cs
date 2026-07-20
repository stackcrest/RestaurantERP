using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Common;

public static class OrderStatusHelper
{
    public static string GetLabel(OrderStatus status) => status switch
    {
        OrderStatus.Placed => "Order Placed",
        OrderStatus.Confirmed => "Confirmed",
        OrderStatus.Preparing => "Preparing",
        OrderStatus.Ready => "Ready",
        OrderStatus.OutForDelivery => "Out for Delivery",
        OrderStatus.Served => "Served",
        OrderStatus.Delivered => "Delivered",
        OrderStatus.Completed => "Completed",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string GetBadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Placed => "secondary",
        OrderStatus.Confirmed => "primary",
        OrderStatus.Preparing => "warning",
        OrderStatus.Ready => "info",
        OrderStatus.OutForDelivery => "info",
        OrderStatus.Served or OrderStatus.Delivered => "success",
        OrderStatus.Completed => "success",
        OrderStatus.Cancelled => "danger",
        _ => "secondary"
    };

    public static IReadOnlyList<OrderStatus> GetTimelineSteps(OrderType orderType)
    {
        if (orderType == OrderType.Delivery)
        {
            return
            [
                OrderStatus.Placed,
                OrderStatus.Confirmed,
                OrderStatus.Preparing,
                OrderStatus.Ready,
                OrderStatus.OutForDelivery,
                OrderStatus.Delivered,
                OrderStatus.Completed
            ];
        }

        return
        [
            OrderStatus.Placed,
            OrderStatus.Confirmed,
            OrderStatus.Preparing,
            OrderStatus.Ready,
            OrderStatus.Served,
            OrderStatus.Completed
        ];
    }

    public static IReadOnlyList<OrderStatus> GetNextStatuses(OrderType orderType, OrderStatus current) => current switch
    {
        OrderStatus.Placed => [OrderStatus.Confirmed, OrderStatus.Cancelled],
        OrderStatus.Confirmed => [OrderStatus.Preparing, OrderStatus.Cancelled],
        OrderStatus.Preparing => [OrderStatus.Ready],
        OrderStatus.Ready when orderType == OrderType.Delivery => [OrderStatus.OutForDelivery],
        OrderStatus.Ready => [OrderStatus.Served],
        OrderStatus.OutForDelivery => [OrderStatus.Delivered],
        OrderStatus.Served or OrderStatus.Delivered => [OrderStatus.Completed],
        _ => []
    };

    public static bool CanTransition(OrderType orderType, OrderStatus current, OrderStatus next) =>
        GetNextStatuses(orderType, current).Contains(next);

    public static int GetProgressPercent(OrderType orderType, OrderStatus status)
    {
        if (status == OrderStatus.Cancelled) return 0;
        var steps = GetTimelineSteps(orderType);
        var idx = steps.ToList().IndexOf(status);
        if (idx < 0) return 0;
        return (int)Math.Round((idx + 1) * 100.0 / steps.Count);
    }
}
