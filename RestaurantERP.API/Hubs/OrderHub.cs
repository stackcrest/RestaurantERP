using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RestaurantERP.API.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public const string HubPath = "/hubs/orders";

    public async Task JoinOrderGroup(string orderId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId));

    public async Task LeaveOrderGroup(string orderId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, OrderGroup(orderId));

    public async Task JoinKitchenGroup() =>
        await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");

    public async Task JoinAdminGroup() =>
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin-orders");

    public static string OrderGroup(string orderId) => $"order-{orderId}";
}
