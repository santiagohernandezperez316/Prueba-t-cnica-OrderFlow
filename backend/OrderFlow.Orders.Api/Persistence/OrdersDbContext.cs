using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Api.Persistence;

internal sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public const string Schema = "orders";

    private static readonly string[] SeedSkus = ["SKU-001", "SKU-002", "SKU-003"];

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<KnownSku> KnownSkus => Set<KnownSku>();

    public static async Task SeedAsync(DbContext context, bool _, CancellationToken cancellationToken)
    {
        var skus = context.Set<KnownSku>();
        if (await skus.AnyAsync(cancellationToken))
        {
            return;
        }

        skus.AddRange(SeedSkus.Select(sku => new KnownSku { Sku = sku }));
        await context.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
