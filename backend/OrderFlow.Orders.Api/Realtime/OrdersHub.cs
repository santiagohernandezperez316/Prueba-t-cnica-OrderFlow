using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Api.Realtime;

internal sealed class OrdersHub : Hub
{
    public const string OrderStatusChangedMethod = "OrderStatusChanged";
}

internal sealed record OrderStatusChangedNotification(
    Guid Id,
    [property: JsonPropertyName("estado")] OrderStatus Status,
    [property: JsonPropertyName("motivoRechazo")] string? RejectionReason);

internal static class OrdersHubContextExtensions
{
    public static Task NotifyStatusChangedAsync(this IHubContext<OrdersHub> hubContext, Order order, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync(
            OrdersHub.OrderStatusChangedMethod,
            new OrderStatusChangedNotification(order.Id, order.Status, order.RejectionReason),
            cancellationToken);
}
