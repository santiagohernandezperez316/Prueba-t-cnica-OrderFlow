using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Persistence;

namespace OrderFlow.Inventory.Worker.Features;

internal static class GetProductsEndpoint
{
    public static void MapGetProducts(this IEndpointRouteBuilder app) =>
        app.MapGet("/", async (InventoryDbContext db, CancellationToken cancellationToken) =>
            TypedResults.Ok(await db.Products
                .AsNoTracking()
                .OrderBy(product => product.Sku)
                .Select(product => new ProductResponse(product.Sku, product.Name, product.Stock))
                .ToListAsync(cancellationToken)));
}

internal sealed record ProductResponse(
    string Sku,
    [property: JsonPropertyName("nombre")] string Name,
    [property: JsonPropertyName("disponible")] int Stock);
