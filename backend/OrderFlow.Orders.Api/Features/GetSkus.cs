using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Persistence;

namespace OrderFlow.Orders.Api.Features;

internal static class GetSkusEndpoint
{
    public static void MapGetSkus(this IEndpointRouteBuilder app) =>
        app.MapGet("/", async (OrdersDbContext db, CancellationToken cancellationToken) =>
            TypedResults.Ok(await db.KnownSkus
                .AsNoTracking()
                .OrderBy(knownSku => knownSku.Sku)
                .Select(knownSku => knownSku.Sku)
                .ToListAsync(cancellationToken)))
        .WithSummary("Get known SKUs");
}
