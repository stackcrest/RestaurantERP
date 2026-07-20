using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Interfaces;

public class OrderTrackingStep
{
    public OrderStatus Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Note { get; set; }
}

public interface IOrderTrackingService
{
    Task RecordStatusAsync(Guid orderId, OrderStatus status, Guid? userId, string? userName, string? note = null);
    Task<(bool Success, string Message)> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, Guid? userId, string? userName, string? note = null);
    Task<List<OrderTrackingStep>> GetTimelineAsync(Guid orderId);
    Task BackfillMissingHistoryAsync();
}
