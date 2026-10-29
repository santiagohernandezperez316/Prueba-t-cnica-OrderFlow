using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Persistence;

namespace OrderFlow.Orders.Api.Features;

internal static class GetOrdersEndpoint
{
    public static void MapGetOrders(this IEndpointRouteBuilder app) =>
        app.MapGet("/", async (OrdersDbContext db, CancellationToken cancellationToken) =>
        {
            var orders = await db.Orders
                .AsNoTracking()
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync(cancellationToken);

            return TypedResults.Ok(orders.Select(OrderResponse.From).ToList());
        })
        .WithSummary("Get all orders");
}
