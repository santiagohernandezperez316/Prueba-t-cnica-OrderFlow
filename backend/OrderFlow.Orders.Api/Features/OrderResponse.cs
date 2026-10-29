using System.Text.Json.Serialization;
using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Api.Features;

internal sealed record OrderResponse(
    Guid Id,
    [property: JsonPropertyName("clienteNombre")] string CustomerName,
    string Sku,
    [property: JsonPropertyName("cantidad")] int Quantity,
    [property: JsonPropertyName("estado")] OrderStatus Status,
    [property: JsonPropertyName("creadoEn")] DateTimeOffset CreatedOn,
    [property: JsonPropertyName("motivoRechazo")] string? RejectionReason)
{
    public static OrderResponse From(Order order) =>
        new(order.Id, order.CustomerName, order.Sku, order.Quantity, order.Status, order.CreatedOn, order.RejectionReason);
}
