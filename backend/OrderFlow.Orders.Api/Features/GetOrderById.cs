using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Persistence;

namespace OrderFlow.Orders.Api.Features;

internal static class GetOrderByIdEndpoint
{
    public static void MapGetOrderById(this IEndpointRouteBuilder app) =>
        app.MapGet("/{id:guid}", async Task<Results<Ok<OrderResponse>, NotFound>> (Guid id, OrdersDbContext db, CancellationToken cancellationToken) =>
        {
            var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
            return order is null ? TypedResults.NotFound() : TypedResults.Ok(OrderResponse.From(order));
        })
        .WithSummary("Get order by id");
}
